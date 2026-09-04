"""Model-selected SDPA adapter. Never replaces torch or a built-in registry key."""
import torch
from transformers import AttentionInterface, AttentionMaskInterface
from transformers.integrations.sdpa_attention import create_position_bias_mask
from transformers.masking_utils import sdpa_mask

NAME = "aihub_omni_sdpa"
BACKENDS = {-1: "ERROR", 0: "MATH", 1: "FLASH_ATTENTION", 2: "EFFICIENT_ATTENTION", 3: "CUDNN_ATTENTION"}


def selected_backend(query, key, value, **kwargs):
    # Diagnostic selector is optional; lack of it must not masquerade as Flash.
    selector = getattr(torch, "_fused_sdp_choice", None)
    if selector is None:
        return None
    return int(selector(query, key, value, **kwargs))


def attention_forward(module, query, key, value, attention_mask, dropout=0.0,
                      scaling=None, is_causal=None, position_bias=None, **kwargs):
    if any(t.ndim != 4 for t in (query, key, value)):
        raise ValueError("Omni SDPA requires four-dimensional attention tensors.")
    qheads, kheads, vheads = query.shape[1], key.shape[1], value.shape[1]
    if kheads != vheads or qheads % kheads:
        raise ValueError("Omni SDPA received incompatible grouped heads.")
    causal = bool(query.shape[2] > 1 and attention_mask is None
                  and (getattr(module, "is_causal", True) if is_causal is None else is_causal))
    if causal and key.shape[2] > query.shape[2]:
        key, value = key[:, :, :query.shape[2], :], value[:, :, :query.shape[2], :]
        if position_bias is not None:
            position_bias = position_bias[:, :, :, :query.shape[2]]
    if position_bias is not None:
        attention_mask = create_position_bias_mask(position_bias, attention_mask, causal, query, key)
        causal = False
    options = dict(attn_mask=attention_mask, dropout_p=dropout, scale=scaling,
                   is_causal=causal, enable_gqa=qheads != kheads)
    original_backend = selected_backend(query, key, value, **options)
    backend = original_backend
    repeated = False
    if qheads != kheads and query.is_cuda and original_backend == 0:
        # Materialize only when native grouped SDPA chooses MATH. Native Flash
        # and cuDNN keep their original tensors and dispatch.
        expanded_key = key.repeat_interleave(qheads // kheads, dim=1)
        expanded_value = value.repeat_interleave(qheads // kheads, dim=1)
        candidate = dict(options, enable_gqa=False)
        candidate_backend = selected_backend(query, expanded_key, expanded_value, **candidate)
        if candidate_backend in (1, 2, 3):
            key, value, options = expanded_key, expanded_value, candidate
            backend, repeated = candidate_backend, True
        del expanded_key, expanded_value
    telemetry = getattr(module, "_aihub_attention_telemetry", None)
    if telemetry is not None:
        name = BACKENDS.get(backend, "UNAVAILABLE")
        telemetry["calls"][name] = telemetry["calls"].get(name, 0) + 1
        phase = "prefill" if query.shape[2] > 1 else "decode"
        signature = f"{phase}:{name}:{qheads}:{kheads}:{repeated}"
        if signature not in telemetry["selections"]:
            telemetry["selections"][signature] = dict(
                backend=name, originalBackend=BACKENDS.get(original_backend, "UNAVAILABLE"),
                repeatedKv=repeated, query=list(query.shape), key=list(key.shape),
                dtype=str(query.dtype), causal=causal,
                maskShape=list(attention_mask.shape) if attention_mask is not None else None)
    output = torch.nn.functional.scaled_dot_product_attention(query, key, value, **options)
    return output.transpose(1, 2).contiguous(), None


def register():
    AttentionInterface.register(NAME, attention_forward)
    # Without this registration custom attention silently loses padding/causal masks.
    AttentionMaskInterface.register(NAME, sdpa_mask)
    return NAME


def attach_telemetry(model, telemetry):
    for module in model.modules():
        if getattr(getattr(module, "config", None), "_attn_implementation", None) == NAME:
            module._aihub_attention_telemetry = telemetry
