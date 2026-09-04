"""Summarize probe evidence without copying source session text into the report."""

import argparse
import json
from pathlib import Path
import wave

import numpy as np


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("root", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    rows = []
    audio_checks = []
    for path in sorted(args.root.glob("**/result.json")):
        result = json.loads(path.read_text(encoding="utf-8-sig"))
        operations = result["requests"]
        workers = [sample for sample in result.get("processSamples", [])
                   if sample["name"] == "python.exe" and sample["pid"] != result["pid"]]
        worker = max(workers, key=lambda item: item["peakWorkingSetBytes"], default={})
        rows.append({"run": path.parent.relative_to(args.root).as_posix(),
                     "exitHex": result.get("exitHex"),
                     "loadSeconds": operations[0].get("seconds"),
                     "outcomes": [request.get("outcome") for request in operations],
                     "synthesisSeconds": [request.get("seconds") for request in operations[1:]],
                     "worker": worker})
        for request in operations:
            wav = request.get("payload", {}).get("outputPath")
            if not wav or not Path(wav).is_file():
                continue
            with wave.open(wav) as stream:
                audio = np.frombuffer(stream.readframes(stream.getnframes()), dtype="<i2").astype(np.float64) / 32768
                audio_checks.append({"run": rows[-1]["run"], "label": request["label"],
                                     "seconds": len(audio) / stream.getframerate(),
                                     "rate": stream.getframerate(), "channels": stream.getnchannels(),
                                     "peak": float(np.abs(audio).max()), "rms": float(np.sqrt(np.mean(audio ** 2))),
                                     "finite": bool(np.isfinite(audio).all())})
    baseline = json.loads((args.root / "tokenizer_equivalence_baseline/tokenizer-only_ru_1/result.json").read_text())
    candidate = json.loads((args.root / "tokenizer_equivalence_candidate/tokenizer-bypass-only_ru_1/result.json").read_text())
    left, right = baseline["requests"][0]["response"], candidate["requests"][0]["response"]
    equivalence = {key: left[key] == right[key] for key in ("tokens", "addedTokens", "backendSha256", "samples")}
    fingerprints = {}
    for path in args.root.glob("**/stderr.txt"):
        values = []
        for line in path.read_text(encoding="utf-8").splitlines():
            if line.startswith("{"):
                event = json.loads(line)
                if event.get("stage") == "phoneme_fingerprint":
                    values.append(event["sha256"])
        if values:
            fingerprints[path.parent.relative_to(args.root).as_posix()] = values
    summary = {"runs": rows, "tokenizerEquivalent": equivalence,
               "phonemeFingerprints": fingerprints, "audioChecks": audio_checks}
    args.output.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"runs": len(rows), "nonzeroExits": [r["run"] for r in rows if r["exitHex"] != "0x00000000"],
                      "tokenizerEquivalent": equivalence, "waveFiles": len(audio_checks),
                      "allAudioValid": all(a["finite"] and a["rms"] > 0 and a["rate"] == 24000 and a["channels"] == 1
                                           for a in audio_checks)}, indent=2))


if __name__ == "__main__":
    main()
