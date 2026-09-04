"""Small CPU/CUDA regressions for the production adapter; no model weights."""
import sys
import unittest
import importlib.util
from pathlib import Path
from types import SimpleNamespace

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "Исходники/AIHub/Tools"))
import torch
from transformers import AttentionMaskInterface, Qwen2Config, Qwen2ForCausalLM
from transformers.integrations.sdpa_attention import sdpa_attention_forward
from transformers.masking_utils import sdpa_mask
from omni_attention import NAME, register, attention_forward, attach_telemetry


class AttentionTests(unittest.TestCase):
    def test_worker_releases_temporary_resources_after_failure(self):
        path = Path(__file__).resolve().parents[2] / "Исходники/AIHub/Tools/qwen25_omni_worker.py"
        spec = importlib.util.spec_from_file_location("cleanup_worker", path)
        module = importlib.util.module_from_spec(spec)
        stdout = sys.stdout
        try:
            spec.loader.exec_module(module)
        finally:
            sys.stdout = stdout
        worker = module.OmniWorker()
        worker.model = object()
        original_model = worker.model
        cleanups = []
        worker._clear_unused_cache = lambda: cleanups.append(True)

        def fail(*args):
            raise RuntimeError("injected failure")
        worker._generate_text = fail
        with self.assertRaisesRegex(RuntimeError, "injected failure"):
            worker.generate_text(1, "unused", [])
        self.assertEqual(len(cleanups), 2)
        self.assertIs(worker.model, original_model)
        worker._generate_text = lambda *args: {"content": "next request works"}
        self.assertEqual(worker.generate_text(2, "unused", [])["content"], "next request works")
        self.assertEqual(len(cleanups), 4)

    def test_registry_preserves_builtin_and_masks(self):
        original = torch.nn.functional.scaled_dot_product_attention
        register()
        self.assertIs(AttentionMaskInterface()[NAME], sdpa_mask)
        self.assertIs(original, torch.nn.functional.scaled_dot_product_attention)

    def compare(self, device, dtype):
        torch.manual_seed(17)
        for qlen, klen, mask_type, causal in [(9, 9, None, True), (1, 9, None, True),
                                              (9, 9, "bool", True), (9, 9, "float", False),
                                              (5, 9, None, True), (3, 9, "bool", False)]:
            with self.subTest(device=device, dtype=dtype, shape=(qlen, klen), mask=mask_type):
                q = torch.randn(1, 16, qlen, 128, device=device, dtype=dtype)
                k = torch.randn(1, 2, klen, 128, device=device, dtype=dtype)
                v = torch.randn_like(k)
                mask = None
                if mask_type:
                    mask = torch.ones(1, 1, qlen, klen, dtype=torch.bool, device=device)
                    mask[..., -2:] = False
                    if mask_type == "float":
                        mask = torch.zeros_like(mask, dtype=dtype).masked_fill(~mask, -float("inf"))
                module = SimpleNamespace(num_key_value_groups=8, is_causal=causal)
                actual, _ = attention_forward(module, q, k, v, mask)
                expected, _ = sdpa_attention_forward(module, q, k, v, mask)
                tol = 0.02 if dtype == torch.bfloat16 else 1e-5
                torch.testing.assert_close(actual, expected, atol=tol, rtol=tol)

    def test_cpu(self):
        self.compare("cpu", torch.float32)

    @unittest.skipUnless(torch.cuda.is_available(), "CUDA unavailable")
    def test_cuda(self):
        self.compare("cuda", torch.float32)
        self.compare("cuda", torch.bfloat16)

    def test_tiny_model_padding_and_decode(self):
        register()
        config = Qwen2Config(vocab_size=32, hidden_size=32, intermediate_size=64,
                             num_hidden_layers=1, num_attention_heads=4,
                             num_key_value_heads=2)
        config._attn_implementation = "sdpa"
        model = Qwen2ForCausalLM(config).eval()
        tokens = torch.tensor([[0, 4, 5, 6], [1, 2, 3, 4]])
        mask = torch.tensor([[0, 1, 1, 1], [1, 1, 1, 1]])
        with torch.inference_mode():
            expected = model(tokens, attention_mask=mask).logits
            model.set_attn_implementation(NAME)
            telemetry = {"calls": {}, "selections": {}}
            attach_telemetry(model, telemetry)
            first = model(tokens, attention_mask=mask, use_cache=True)
            torch.testing.assert_close(first.logits, expected, atol=1e-5, rtol=1e-5)
            next_mask = torch.cat([mask, torch.ones(2, 1, dtype=mask.dtype)], dim=1)
            cached = model(torch.tensor([[7], [8]]), attention_mask=next_mask,
                           past_key_values=first.past_key_values).logits
            full = model(torch.cat([tokens, torch.tensor([[7], [8]])], dim=1),
                         attention_mask=next_mask).logits[:, -1:]
            torch.testing.assert_close(cached, full, atol=1e-5, rtol=1e-5)
            self.assertTrue(telemetry["calls"])


if __name__ == "__main__":
    unittest.main()
