"""One-image, independent RU versus EN->RU requests to the unchanged worker."""
import json
import os
from pathlib import Path
import queue
import subprocess
import sys
import threading
import time

ROOT = Path(__file__).resolve().parents[2]
PROJECTS = ROOT / "Тесты/1/AI_HUB/Scenarios/ImageAnalysis/Projects"
OUTPUT = ROOT / "Тесты/Omni_ru_en_prompt_pair_20260904"


def main():
    OUTPUT.mkdir(exist_ok=False)
    prompts = {}
    for label, session, prefix, marker in [
        ("ru", "ba7dc8be55764916ad24e7a403815e63",
         "Создай готовое пользовательское описание изображения. Перед ответом молча сверь все существенные утверждения с изображением.\n\n",
         "Требования к результату:"),
        ("en_to_ru", "b2328216cf704622bd5355304e481dc0",
         "Create the final user-facing description of the image. Before answering, silently verify every material claim against the image.\n\n",
         "Result requirements:"),
    ]:
        artifact = next((PROJECTS / session / "OmniResponses").glob("*compose*.json"))
        saved = json.loads(artifact.read_text(encoding="utf-8-sig"))
        original = saved["conversation"][-1]["content"]
        prompt = prefix + original[original.index(marker):]
        if label == "en_to_ru":
            prompt = prompt.replace("- language: English;", "- language: Russian;")
            prompt += "\nTranslate the final answer into Russian. Output only the Russian version; keep the JSON field names unchanged."
        prompts[label] = prompt
    (OUTPUT / "prompts.json").write_text(json.dumps(prompts, ensure_ascii=False, indent=2), encoding="utf-8")
    baseline = ROOT / "Тесты/Omni_stabilization_20260904/final_baseline_01_small/protocol.jsonl"
    rows = [json.loads(line) for line in baseline.read_text(encoding="utf-8").splitlines()]
    warm = next(r["payload"] for r in rows if r.get("direction") == "request" and r["payload"]["command"] == "warmup")
    image_path = next(r["payload"]["imagePath"] for r in rows if r.get("direction") == "request" and r["payload"]["command"] == "analyze")
    with (OUTPUT / "protocol.jsonl").open("w", encoding="utf-8") as log, (OUTPUT / "stderr.log").open("w", encoding="utf-8") as err:
        proc = subprocess.Popen(
            [sys.executable, "-u", str(ROOT / "Исходники/AIHub/Tools/qwen25_omni_worker.py")],
            stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=err, text=True, encoding="utf-8",
            env=dict(os.environ, PYTHONIOENCODING="utf-8", HF_HUB_OFFLINE="1", TRANSFORMERS_OFFLINE="1"),
            creationflags=subprocess.CREATE_NO_WINDOW)
        pending = queue.Queue()

        def read():
            for line in proc.stdout:
                pending.put(line)
            pending.put(None)

        threading.Thread(target=read, daemon=True).start()

        def record(row):
            log.write(json.dumps(dict(row, time=time.time()), ensure_ascii=False) + "\n")
            log.flush()

        def request(index, label, payload):
            record(dict(direction="request", id=index, label=label, payload=payload))
            proc.stdin.write(json.dumps(dict(id=index, payload=payload), ensure_ascii=False) + "\n")
            proc.stdin.flush()
            started = time.monotonic()
            print(f"START {label} pid={proc.pid}", flush=True)
            while time.monotonic() - started < 300:
                try:
                    line = pending.get(timeout=1)
                except queue.Empty:
                    continue
                if line is None:
                    raise RuntimeError("Worker exited")
                result = json.loads(line)
                record(dict(direction="response", label=label, value=result))
                if result["id"] != index:
                    raise RuntimeError("Unexpected response ID")
                if result.get("event"):
                    continue
                (OUTPUT / f"{label}.json").write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
                print(json.dumps({k: result.get(k) for k in ("success", "elapsedMilliseconds", "requestWallMilliseconds", "generatedTokens", "finishReason", "error")}), flush=True)
                if not result.get("success"):
                    raise RuntimeError(result.get("error"))
                return
            record(dict(event="deadline", label=label, seconds=300))
            raise TimeoutError(label)

        try:
            request(1, "warmup", warm)
            for index, (label, prompt) in enumerate(prompts.items(), 2):
                request(index, label, dict(command="compose", imagePath=image_path,
                    messages=[dict(role="user", content=prompt, includesImage=True)]))
            request(4, "shutdown", dict(command="shutdown"))
        finally:
            if proc.poll() is None:
                try:
                    proc.wait(timeout=3)
                except subprocess.TimeoutExpired:
                    subprocess.run(["taskkill", "/PID", str(proc.pid), "/T", "/F"],
                        capture_output=True, timeout=10, creationflags=subprocess.CREATE_NO_WINDOW)
            proc.wait(timeout=10)
            proc.stdin.close()
            proc.stdout.close()


if __name__ == "__main__":
    main()
