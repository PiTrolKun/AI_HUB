"""Replay saved requests through the real worker, without attention monkeypatches."""
import argparse
import json
import os
from pathlib import Path
import queue
import subprocess
import sys
import threading
import time

ROOT = Path(__file__).resolve().parents[2]


def run(args):
    source = [json.loads(line) for line in Path(args.protocol).read_text(encoding="utf-8-sig").splitlines()]
    requests = [row["payload"] for row in source if row.get("direction") == "request"]
    warm = next(p for p in requests if p["command"] == "warmup")
    observe = next(p for p in requests if p["command"] == "analyze")
    compose = next(p for p in requests if p["command"] == "compose")
    if args.prompts:
        prompts = json.loads(Path(args.prompts).read_text(encoding="utf-8-sig"))
        observe["messages"][0]["content"] = prompts["observe"]
        compose["messages"][-1]["content"] = prompts["compose"]
    if args.simple:
        observe["messages"][0]["content"] = "Опиши, что видишь на изображении."
    output = Path(args.output)
    output.mkdir(parents=True, exist_ok=False)
    with (output / "protocol.jsonl").open("w", encoding="utf-8") as log, (output / "stderr.log").open("w", encoding="utf-8") as err:
        env = dict(os.environ, PYTHONIOENCODING="utf-8", HF_HUB_OFFLINE="1", TRANSFORMERS_OFFLINE="1")
        proc = subprocess.Popen([sys.executable, "-u", str(ROOT / "Исходники/AIHub/Tools/qwen25_omni_worker.py")],
                                stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=err,
                                text=True, encoding="utf-8", env=env)
        responses = queue.Queue()

        def read():
            for line in proc.stdout:
                responses.put(line)
            responses.put(None)
        threading.Thread(target=read, daemon=True).start()

        def record(**row):
            log.write(json.dumps(dict(row, time=time.time()), ensure_ascii=False) + "\n")
            log.flush()

        def request(index, payload):
            record(direction="request", id=index, payload=payload)
            proc.stdin.write(json.dumps(dict(id=index, payload=payload), ensure_ascii=False) + "\n")
            proc.stdin.flush()
            start = time.monotonic()
            print(f"START {payload['command']} pid={proc.pid}", flush=True)
            while time.monotonic() - start < args.deadline:
                try:
                    line = responses.get(timeout=min(1, max(.01, args.deadline - (time.monotonic() - start))))
                except queue.Empty:
                    continue
                if line is None:
                    raise RuntimeError("Worker exited")
                result = json.loads(line)
                record(direction="response", value=result)
                if result["id"] != index:
                    raise RuntimeError("Out-of-order response")
                if result.get("event"):
                    continue
                print(json.dumps({k: v for k, v in result.items() if k not in ("content", "deviceMap", "attentionTelemetry")}, ensure_ascii=False), flush=True)
                if not result.get("success"):
                    raise RuntimeError(result.get("error"))
                return result
            record(event="deadline", id=index, seconds=args.deadline)
            raise TimeoutError(f"{payload['command']} exceeded {args.deadline}s")

        try:
            request(1, warm)
            for i in range(args.repeat):
                visual = request(2 + i * 2, observe)
                compose["messages"] = [observe["messages"][0],
                    dict(role="assistant", content=visual["content"], includesImage=False), compose["messages"][-1]]
                request(3 + i * 2, compose)
            request(2 + args.repeat * 2, dict(command="shutdown"))
        finally:
            if proc.poll() is None:
                subprocess.run(["taskkill", "/PID", str(proc.pid), "/T", "/F"],
                               capture_output=True, timeout=10, creationflags=subprocess.CREATE_NO_WINDOW)
            proc.wait(timeout=10)
            proc.stdin.close()
            proc.stdout.close()


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--protocol", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--deadline", type=int, default=300)
    parser.add_argument("--repeat", type=int, default=1)
    parser.add_argument("--simple", action="store_true")
    parser.add_argument("--prompts")
    run(parser.parse_args())
