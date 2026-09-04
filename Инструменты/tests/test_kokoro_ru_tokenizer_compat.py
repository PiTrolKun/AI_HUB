"""Offline boundary tests; no model weights or external packages required."""
import importlib.util
import json
from pathlib import Path
import sys
import subprocess
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

TOOLS = Path(__file__).resolve().parents[2] / "Исходники/AIHub/Tools"
sys.path.insert(0, str(TOOLS))
import kokoro_ru_tokenizer_compat as compat


class FakeTokenizer:
    @classmethod
    def from_pretrained(cls, path, *args, **kwargs):
        return path, args, kwargs


class CompatibilityTests(unittest.TestCase):
    def setUp(self):
        self.accent = Path(tempfile.gettempdir()) / "kokoro-test-accent"
        self.target = self.accent / "nn/nn_omograph/turbo3.1"
        self.descriptor = FakeTokenizer.__dict__["from_pretrained"]
        self.modules = patch.dict(sys.modules, {"transformers": SimpleNamespace(AutoTokenizer=FakeTokenizer)})
        self.modules.start()
        self.addCleanup(self.modules.stop)
        self.versions = patch.object(compat, "version", side_effect=compat._VERSIONS.__getitem__)
        self.versions.start()
        self.addCleanup(self.versions.stop)

    def test_only_exact_target_and_unmodified_arguments_are_patched(self):
        with patch.object(compat, "_validate_checkpoint"), compat.compatible_ru_tokenizer(self.accent) as state:
            self.assertEqual(FakeTokenizer.from_pretrained(self.target)[2], {"added_tokens_decoder": {}})
            self.assertIn("applied", state["status"])
            self.assertEqual(FakeTokenizer.from_pretrained(self.accent / "other/turbo3.1")[2], {})
            self.assertEqual(FakeTokenizer.from_pretrained(self.target, use_fast=False)[2], {"use_fast": False})
            self.assertEqual(FakeTokenizer.from_pretrained(self.target, added_tokens_decoder={1: "x"})[2],
                             {"added_tokens_decoder": {1: "x"}})
        self.assertIs(FakeTokenizer.__dict__["from_pretrained"], self.descriptor)

    def test_loader_exception_restores_original_descriptor(self):
        with self.assertRaisesRegex(RuntimeError, "load failed"):
            with patch.object(compat, "_validate_checkpoint"), compat.compatible_ru_tokenizer(self.accent):
                raise RuntimeError("load failed")
        self.assertIs(FakeTokenizer.__dict__["from_pretrained"], self.descriptor)

    def test_unknown_version_skips_without_touching_tokenizer(self):
        with patch.object(compat, "version", return_value="unknown"), compat.compatible_ru_tokenizer(self.accent) as state:
            self.assertIn("skipped", state["status"])
            self.assertIs(FakeTokenizer.__dict__["from_pretrained"], self.descriptor)

    def test_changed_checkpoint_skips_without_discarding_tokens(self):
        with patch.object(compat, "_validate_checkpoint", side_effect=ValueError("changed")):
            with compat.compatible_ru_tokenizer(self.accent) as state:
                self.assertIn("skipped", state["status"])
                self.assertEqual(FakeTokenizer.from_pretrained(self.target)[2], {})

    def test_file_hash_mismatch_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "tokenizer.json").write_text("{}", encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "unreviewed checkpoint"):
                compat._validate_checkpoint(root)

    def test_mismatched_legacy_ids_are_rejected_even_with_reviewed_hashes(self):
        from hashlib import sha256
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            payloads = {"tokenizer.json": {"model": {"vocab": {"a": 0}}, "added_tokens": []},
                        "tokenizer_config.json": {}, "added_tokens.json": {"a": 2}}
            hashes = {}
            for name, payload in payloads.items():
                content = json.dumps(payload).encode()
                (root / name).write_bytes(content)
                hashes[name] = sha256(content).hexdigest()
            with patch.object(compat, "_CHECKPOINT_HASHES", hashes):
                with self.assertRaisesRegex(ValueError, "legacy token IDs"):
                    compat._validate_checkpoint(root)

    def test_existing_token_type_shim_forwards_internals_and_preserves_ids(self):
        spec = importlib.util.spec_from_file_location("kokoro_test_worker", TOOLS / "kokoro_tts_worker.py")
        module = importlib.util.module_from_spec(spec)
        with patch.object(sys, "stdout", sys.stdout):
            spec.loader.exec_module(module)
        class Tokenizer:
            backend_tokenizer = object()
            def __call__(self, **kwargs):
                return dict(kwargs)
        inner = Tokenizer()
        shim = module._CompatibleTokenTypeIdsShim(inner)
        with patch.dict(sys.modules, {"numpy": SimpleNamespace(zeros_like=lambda value: [0] * len(value))}):
            self.assertEqual(shim(input_ids=[5, 8])["token_type_ids"], [0, 0])
            self.assertEqual(shim(input_ids=[5], token_type_ids=[2])["token_type_ids"], [2])
        self.assertIs(shim.backend_tokenizer, inner.backend_tokenizer)

    def test_python_error_keeps_structured_protocol_and_process_alive(self):
        commands = [
            {"id": 1, "payload": {"command": "synthesize", "text": "test"}},
            {"id": 2, "payload": {"command": "unknown"}},
        ]
        result = subprocess.run(
            [sys.executable, "-u", str(TOOLS / "kokoro_tts_worker.py")],
            input="".join(json.dumps(command) + "\n" for command in commands),
            text=True, encoding="utf-8", capture_output=True, timeout=10,
            creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0),
        )
        self.assertEqual(result.returncode, 0)
        responses = [json.loads(line) for line in result.stdout.splitlines()]
        self.assertEqual([item["id"] for item in responses], [1, 2])
        self.assertEqual(responses[0]["errorType"], "RuntimeError")
        self.assertEqual(responses[0]["errorStage"], "synthesize")
        self.assertEqual(responses[1]["errorType"], "ValueError")
        self.assertIn("pythonPid=", result.stderr)
        self.assertIn("Traceback", responses[0]["traceback"])


if __name__ == "__main__":
    unittest.main()
