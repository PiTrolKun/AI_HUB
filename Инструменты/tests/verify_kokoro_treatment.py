"""Compare the installed production RU frontend against local research evidence."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import sys


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--research", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    os.environ.update(HF_HUB_OFFLINE="1", TRANSFORMERS_OFFLINE="1", HF_DATASETS_OFFLINE="1")
    root = Path(__file__).resolve().parents[2]
    worker_path = root / "Исходники/AIHub/Tools/kokoro_tts_worker.py"
    sys.path.insert(0, str(worker_path.parent))
    spec = importlib.util.spec_from_file_location("kokoro_validation_worker", worker_path)
    module = importlib.util.module_from_spec(spec)
    stdout = sys.stdout
    try:
        spec.loader.exec_module(module)
    finally:
        sys.stdout = stdout
    card = read(Path(os.environ["LOCALAPPDATA"]) / "AI_HUB/ModelLibrary/Entries/model-kokoro-ru-sveta.json")
    worker = module.KokoroWorker()
    already, elapsed = worker.load("ru", card["installDirectory"])
    assert not already
    assert "ruTokenizerCompat=applied" in worker.last_diagnostics
    wrapper = worker.g2p._accent.omograph_model.tokenizer
    tokenizer = wrapper._wrapped if isinstance(wrapper, module._CompatibleTokenTypeIdsShim) else wrapper
    checked_shims = []
    for name in ("accent_model", "omograph_model", "stress_usage_predictor"):
        model = getattr(worker.g2p._accent, name, None)
        session = getattr(model, "session", None)
        if session is not None and "token_type_ids" in {item.name for item in session.get_inputs()}:
            assert isinstance(model.tokenizer, module._CompatibleTokenTypeIdsShim)
            checked_shims.append(name)
    baseline = read(args.research / "tokenizer_equivalence_baseline/tokenizer-only_ru_1/result.json")["requests"][0]["response"]
    sample = ["Это старый замок.", "Дверной <w>замок</w> закрыт.", "человек очень большие глаза огромный язык",
              "Черный кот. Котел с зеленым дымом.", "Пьерами. Байденом. Зеленским.", "Ёлка, всё, мука и мука́."]
    current = {
        "tokens": len(tokenizer), "addedTokens": len(tokenizer.added_tokens_decoder),
        "backendSha256": hashlib.sha256(tokenizer.backend_tokenizer.to_str().encode()).hexdigest(),
        "samples": dict(tokenizer(sample, padding=True, return_offsets_mapping=True)),
    }
    # JSON evidence stores offsets as lists; Transformers returns tuples in memory.
    current = json.loads(json.dumps(current, ensure_ascii=False))
    differences = [key for key, value in current.items() if value != baseline[key]]
    assert not differences, f"tokenizer changed: {differences}"
    trace = args.research / "candidate_sessions/decoder-bypass_ru_1/stderr.txt"
    expected = [json.loads(line)["sha256"] for line in trace.read_text(encoding="utf-8").splitlines()
                if '"stage": "phoneme_fingerprint"' in line]
    inputs = read(args.research / "session_inputs.json")["ru"]
    hashes = []
    for index, item in enumerate(inputs):
        chunks = module.split_text(item["text"])
        assert len(chunks) == 1
        phonemes = worker._phonemize(chunks[0])
        value = hashlib.sha256(json.dumps(phonemes, ensure_ascii=False).encode()).hexdigest()
        assert value == expected[index * 2], "phonemes changed"
        hashes.append(value)
    assert worker.load("ru", card["installDirectory"]) == (True, 0)
    result = {
        "loadMilliseconds": elapsed, "backendSha256": current["backendSha256"],
        "tokenizerEqual": True, "phonemeHashes": hashes, "phonemesEqual": True,
        "warmReuse": True, "tokenTypeShimPreserved": True, "shimModels": checked_shims,
        "workerSha256": hashlib.sha256(worker_path.read_bytes()).hexdigest(),
        "helperSha256": hashlib.sha256((worker_path.parent / "kokoro_ru_tokenizer_compat.py").read_bytes()).hexdigest(),
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(json.dumps(result))


if __name__ == "__main__":
    main()
