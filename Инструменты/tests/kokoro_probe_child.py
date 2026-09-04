"""Diagnostic-only wrapper for the current production Kokoro worker."""

import faulthandler
import hashlib
import json
import os
from pathlib import Path
import runpy
import sys
import time


started = time.monotonic()
worker_path, variant = sys.argv[1:3]
sys.path.insert(0, str(Path(worker_path).resolve().parent))
faulthandler.enable()
faulthandler.dump_traceback_later(40, repeat=True)


def log(stage, **values):
    print(json.dumps({"probeSeconds": round(time.monotonic() - started, 3),
                      "stage": stage, **values}), file=sys.stderr, flush=True)


def profile(frame, event, arg):
    if event not in ("call", "return"):
        return
    path = frame.f_code.co_filename.replace("\\", "/")
    function = frame.f_code.co_name
    selected = (
        path.endswith("kokoro_tts_worker.py") and function in ("load", "synthesize", "_phonemize", "write_wave")
        or path.endswith("ru_g2p.py") and function in ("__init__", "accentuate", "_from_marked")
        or "/ruaccent/" in path and function in ("load", "process_all")
        or "tokenization_utils_fast.py" in path and function == "__init__"
        or "onnxruntime_inference_collection.py" in path and function in ("__init__", "_create_inference_session")
    )
    if selected:
        log(event, file=path, function=function)
        if event == "return" and function == "_phonemize" and isinstance(arg, list):
            log("phoneme_fingerprint", sha256=hashlib.sha256(json.dumps(arg, ensure_ascii=False).encode()).hexdigest(),
                lengths=[len(item) for item in arg])


if variant in ("decoder-bypass", "decoder-control", "tokenizer-bypass-only"):
    from transformers import AutoTokenizer
    original_tokenizer_load = AutoTokenizer.from_pretrained

    def load_without_duplicate_decoder(cls, path, *args, **kwargs):
        root = Path(path)
        if root.name == "turbo3.1" and root.parent.name == "nn_omograph":
            data = json.loads((root / "tokenizer.json").read_text(encoding="utf-8"))
            existing = {item["content"]: item["id"] for item in data["added_tokens"]}
            existing.update(data["model"]["vocab"])
            legacy = json.loads((root / "added_tokens.json").read_text(encoding="utf-8"))
            if not all(existing.get(token) == identifier for token, identifier in legacy.items()):
                raise RuntimeError("Duplicate decoder bypass requires all legacy token IDs in tokenizer.json")
            if variant != "decoder-control":
                kwargs["added_tokens_decoder"] = {}
            log("candidate" if variant != "decoder-control" else "control",
                change="omit duplicate added-token reconstruction" if variant != "decoder-control" else "unchanged decoder",
                entries=len(legacy))
        return original_tokenizer_load(path, *args, **kwargs)

    AutoTokenizer.from_pretrained = classmethod(load_without_duplicate_decoder)


sys.setprofile(profile)
try:
    if variant in ("tokenizer-only", "tokenizer-bypass-only", "tokenizer-after-espeak", "tokenizer-after-onnx"):
        for line in sys.stdin:
            envelope = json.loads(line)
            payload = envelope["payload"]
            from transformers import AutoTokenizer

            path = Path(payload["modelDirectory"]) / "ruaccent/nn/nn_omograph/turbo3.1"
            if variant == "tokenizer-after-espeak":
                from phonemizer.backend.espeak.wrapper import EspeakWrapper
                from misaki import espeak

                EspeakWrapper.set_data_path(str(Path(payload["modelDirectory"]) / "espeak-data"))
                frontend = espeak.EspeakG2P(language="ru")
                log("espeak_ready")
            if variant == "tokenizer-after-onnx":
                import onnxruntime as ort

                session = ort.InferenceSession(str(path / "model.onnx"), providers=["CPUExecutionProvider"])
                log("onnx_ready", providers=session.get_providers())
            log("isolated_tokenizer", path=str(path))
            tokenizer = AutoTokenizer.from_pretrained(str(path), local_files_only=True)
            sample = ["Это старый замок.", "Дверной <w>замок</w> закрыт.", "человек очень большие глаза огромный язык",
                      "Черный кот. Котел с зеленым дымом.", "Пьерами. Байденом. Зеленским.", "Ёлка, всё, мука и мука́."]
            print(json.dumps({"id": envelope["id"], "success": True,
                              "tokens": len(tokenizer), "addedTokens": len(tokenizer.added_tokens_decoder),
                              "backendSha256": hashlib.sha256(tokenizer.backend_tokenizer.to_str().encode()).hexdigest(),
                              "samples": dict(tokenizer(sample, padding=True, return_offsets_mapping=True))}), flush=True)
    else:
        runpy.run_path(worker_path, run_name="__main__")
finally:
    sys.setprofile(None)
    faulthandler.cancel_dump_traceback_later()
