"""Check the isolated candidate on small tensors; no model or downloads."""
import argparse
import json

import torch
from omni_attention_experiment import expand_grouped_kv


def check(device):
    torch.manual_seed(917)
    records = []
    for dtype in (torch.float32, torch.bfloat16):
        for tokens, causal in ((1, False), (64, True), (127, False)):
            query = torch.randn(1, 16, tokens, 128, device=device, dtype=dtype)
            key = torch.randn(1, 2, max(tokens, 64), 128, device=device, dtype=dtype)
            value = torch.randn_like(key)
            expected = torch.nn.functional.scaled_dot_product_attention(
                query, key, value, is_causal=causal, enable_gqa=True)
            expanded_key, expanded_value = expand_grouped_kv(query, key, value)
            actual = torch.nn.functional.scaled_dot_product_attention(
                query, expanded_key, expanded_value, is_causal=causal)
            tolerance = 0.02 if dtype == torch.bfloat16 else 0.00001
            torch.testing.assert_close(actual, expected, atol=tolerance, rtol=tolerance)
            records.append({"device": device, "dtype": str(dtype), "queryTokens": tokens,
                            "causal": causal, "maxAbsDifference": (actual.float()-expected.float()).abs().max().item(),
                            "meanAbsDifference": (actual.float()-expected.float()).abs().mean().item(),
                            "atolRtol": tolerance})
    print(json.dumps(records, indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--device", choices=("cpu", "cuda"), default="cpu")
    check(parser.parse_args().device)
