"""Bounded offline Kokoro probe; never changes installed packages or model files."""

import argparse
import ctypes
from ctypes import wintypes
import hashlib
import importlib.metadata
import json
import os
from pathlib import Path
import queue
import subprocess
import sys
import threading
import time
import wave
from datetime import datetime, timezone
from kokoro_probe_metrics import WorkerSampler


def save(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding="utf-8")


class Memory(ctypes.Structure):
    _fields_ = [("cb", wintypes.DWORD), ("faults", wintypes.DWORD)] + [
        (name, ctypes.c_size_t) for name in
        ("peak_ws", "ws", "peak_pool", "pool", "peak_nonpaged", "nonpaged", "pagefile", "peak_pagefile")
    ]


def memory(process):
    value = Memory()
    value.cb = ctypes.sizeof(value)
    fn = ctypes.windll.psapi.GetProcessMemoryInfo
    fn.argtypes = [wintypes.HANDLE, ctypes.POINTER(Memory), wintypes.DWORD]
    if fn(wintypes.HANDLE(int(process._handle)), ctypes.byref(value), value.cb):
        return {"peakWorkingSetBytes": value.peak_ws, "workingSetBytes": value.ws,
                "privateBytes": value.pagefile, "peakPrivateBytes": value.peak_pagefile}
    return {}


def probe(args, language, iteration):
    name = f"{args.variant}_{language}_{iteration}"
    target = args.output / name
    target.mkdir(parents=True, exist_ok=False)
    artifact = "model-kokoro-ru-sveta" if language == "ru" else "model-kokoro-82m-en-af-heart"
    card_path = Path(os.environ["LOCALAPPDATA"]) / "AI_HUB/ModelLibrary/Entries" / (artifact + ".json")
    card = json.loads(card_path.read_text(encoding="utf-8-sig"))
    worker = args.root / "Исходники/AIHub/Tools/kokoro_tts_worker.py"
    env = dict(os.environ)
    env.update(HF_HUB_OFFLINE="1", TRANSFORMERS_OFFLINE="1", HF_DATASETS_OFFLINE="1", PYTHONIOENCODING="utf-8")
    command = [sys.executable, "-X", "faulthandler", "-u", str(worker)]
    if args.variant != "baseline":
        command = [sys.executable, "-X", "faulthandler", "-u", str(Path(__file__).with_name("kokoro_probe_child.py")),
                   str(worker), args.variant]
    stdout_queue = queue.Queue()
    record = {"startedUtc": datetime.now(timezone.utc).isoformat(), "command": command,
              "modelDirectory": card["installDirectory"], "revision": card.get("revision"),
              "workerSha256": hashlib.sha256(worker.read_bytes()).hexdigest(), "requests": []}
    with (target / "stderr.txt").open("w", encoding="utf-8") as err:
        process = subprocess.Popen(command, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=err,
                                   text=True, encoding="utf-8", env=env, cwd=args.root,
                                   creationflags=subprocess.CREATE_NO_WINDOW)
        record["pid"] = process.pid
        sampler = WorkerSampler(process)
        save(target / "result.json", record)

        def drain():
            for line in process.stdout:
                stdout_queue.put(line)
            stdout_queue.put(None)

        reader = threading.Thread(target=drain, daemon=True)
        reader.start()
        try:
            operations = [("load", {"command": "load", "languageCode": language,
                                     "modelDirectory": card["installDirectory"]})]
            texts = [("short", "Проверка русской речи. За окном светит солнце." if language == "ru" else
                      "This is an English speech test. The sun is shining outside.")]
            if args.input:
                supplied = json.loads(args.input.read_text(encoding="utf-8-sig"))
                texts = [(item["name"], item["text"]) for item in supplied[language]]
            for text_name, text in texts:
                for repeat in range(args.warm_repeats):
                    label = f"{text_name}_{repeat + 1}"
                    operations.append((label, {"command": "synthesize", "text": text,
                                               "outputPath": str(target / (label + ".wav")),
                                               "volume": 1.0, "speed": 1.0}))
            for request_id, (label, payload) in enumerate(operations, 1):
                if time.time() >= args.deadline:
                    record["stoppedByBudget"] = True
                    break
                request = {"id": request_id, "label": label, "payload": payload,
                           "startedUtc": datetime.now(timezone.utc).isoformat()}
                record["requests"].append(request)
                save(target / "result.json", record)
                start = time.monotonic()
                process.stdin.write(json.dumps({"id": request_id, "payload": payload}, ensure_ascii=False) + "\n")
                process.stdin.flush()
                wait = min(args.timeout, args.deadline - time.time())
                try:
                    line = stdout_queue.get(timeout=max(0.01, wait))
                except queue.Empty:
                    request["outcome"] = "timeout"
                    process.kill()
                    break
                request["seconds"] = round(time.monotonic() - start, 3)
                # Windows venv python.exe is a launcher; processSamples captures the real child.
                request["launcherMemory"] = memory(process)
                if line is None:
                    request["outcome"] = "eof"
                    break
                request["response"] = json.loads(line)
                request["outcome"] = "ok" if request["response"].get("success") else "python_error"
                wav_path = payload.get("outputPath")
                if wav_path and Path(wav_path).is_file():
                    with wave.open(wav_path) as audio:
                        request["wav"] = {"frames": audio.getnframes(), "rate": audio.getframerate(),
                                          "channels": audio.getnchannels(), "bytesPerSample": audio.getsampwidth()}
                    request["wav"]["sha256"] = hashlib.sha256(Path(wav_path).read_bytes()).hexdigest()
                save(target / "result.json", record)
                print(name, label, request["outcome"], request["seconds"], flush=True)
                if request["outcome"] != "ok":
                    break
        finally:
            if process.poll() is None:
                try:
                    process.stdin.close()
                    process.wait(timeout=10)
                except (OSError, subprocess.TimeoutExpired):
                    record["forcedCleanup"] = True
                    process.kill()
            code = process.wait()
            record["processSamples"] = sampler.stop()
            record["exitCode"] = code
            record["exitHex"] = f"0x{code & 0xffffffff:08x}"
            record["endedUtc"] = datetime.now(timezone.utc).isoformat()
            save(target / "result.json", record)
            reader.join(timeout=2)
            print(name, "EXIT", record["exitHex"], flush=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--input", type=Path)
    parser.add_argument("--variant", default="baseline", choices=(
        "baseline", "trace", "tokenizer-only",
        "tokenizer-after-espeak", "tokenizer-after-onnx", "tokenizer-bypass-only", "decoder-bypass", "decoder-control"))
    parser.add_argument("--languages", default="ru,en")
    parser.add_argument("--cold-repeats", type=int, default=1)
    parser.add_argument("--warm-repeats", type=int, default=2)
    parser.add_argument("--timeout", type=int, default=120)
    parser.add_argument("--budget-seconds", type=int, default=600)
    args = parser.parse_args()
    if not 1 <= args.timeout <= 300 or not 1 <= args.budget_seconds <= 1800:
        parser.error("timeout must be 1..300 seconds; total budget 1..1800 seconds")
    if not 1 <= args.cold_repeats <= 10 or not 0 <= args.warm_repeats <= 10:
        parser.error("cold repeats must be 1..10; warm repeats 0..10")
    if any(language not in ("ru", "en") for language in args.languages.split(",")):
        parser.error("languages must be ru and/or en")
    if args.variant.startswith("tokenizer-") and (args.languages != "ru" or args.warm_repeats != 0):
        parser.error("isolated tokenizer modes require --languages ru --warm-repeats 0")
    args.output = args.output.resolve()
    args.output.mkdir(parents=True, exist_ok=True)
    args.deadline = time.time() + args.budget_seconds
    packages = {}
    for name in ("torch", "tokenizers", "transformers", "onnxruntime", "kokoro", "misaki", "ruaccent", "numpy", "phonemizer-fork", "espeakng-loader", "spacy"):
        try:
            packages[name] = importlib.metadata.version(name)
        except importlib.metadata.PackageNotFoundError:
            packages[name] = None
    keys = ("OMP_NUM_THREADS", "MKL_NUM_THREADS", "TOKENIZERS_PARALLELISM", "RAYON_NUM_THREADS", "CUDA_VISIBLE_DEVICES")
    save(args.output / f"environment_{args.variant}.json", {"python": sys.version, "executable": sys.executable,
         "packages": packages, "threadEnvironment": {k: os.environ.get(k) for k in keys},
         "logicalCpuCount": os.cpu_count()})
    for language in args.languages.split(","):
        for iteration in range(1, args.cold_repeats + 1):
            if time.time() >= args.deadline:
                return
            probe(args, language, iteration)


if __name__ == "__main__":
    main()
