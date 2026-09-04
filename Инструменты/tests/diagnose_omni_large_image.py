"""Opt-in local reproduction; original JSONL worker, bounded request deadline.

No downloads, application changes, image resizing or generation-setting changes.
The child instruments the original worker in memory; only the owned child is stopped.
"""
import argparse
import faulthandler
import gc
import importlib.util
import json
import os
from pathlib import Path
import queue
import re
import subprocess
import sys
import threading
import time

ROOT = Path(__file__).resolve().parents[2]
WORKER = ROOT / "Исходники/AIHub/Tools/qwen25_omni_worker.py"


def child(args):
    for key in ("HF_HUB_OFFLINE", "TRANSFORMERS_OFFLINE", "HF_DATASETS_OFFLINE", "PYTHONNOUSERSITE"):
        os.environ[key] = "1"
    spec = importlib.util.spec_from_file_location("original_omni_worker", WORKER)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    import torch
    phase = {"id": 0, "seen": set(), "sdpaCalls": 0}

    def mark(event, **data):
        data.update(event=event, id=phase["id"], monotonic=time.perf_counter(),
                    allocated=torch.cuda.memory_allocated(), reserved=torch.cuda.memory_reserved(),
                    peakAllocated=torch.cuda.max_memory_allocated())
        print("DIAG " + json.dumps(data, ensure_ascii=False), file=sys.stderr, flush=True)

    original_sdpa = torch.nn.functional.scaled_dot_product_attention

    def sdpa(query, key, value, *args, **kwargs):
        phase["sdpaCalls"] += 1
        original_key_shape = list(key.shape)
        if args.variant == "repeat-kv" and kwargs.get("enable_gqa", False):
            # Same grouped attention, explicitly materialized KV heads; no new weights.
            from omni_attention_experiment import expand_grouped_kv
            key, value = expand_grouped_kv(query, key, value)
            kwargs["enable_gqa"] = False
        mask = kwargs.get("attn_mask", args[0] if args else None)
        signature = (tuple(query.shape), tuple(key.shape), str(query.dtype), mask is not None)
        log = signature not in phase["seen"] and len(phase["seen"]) < 8
        if log:
            phase["seen"].add(signature)
            backend = torch._fused_sdp_choice(query, key, value, *args, **kwargs)
            mark("sdpa_enter", query=list(query.shape), key=list(key.shape), dtype=str(query.dtype),
                 maskShape=list(mask.shape) if mask is not None else None,
                 causal=kwargs.get("is_causal"), strides=list(query.stride()),
                 selectedBackend=int(backend), originalKey=original_key_shape,
                 enableGqa=kwargs.get("enable_gqa", False), call=phase["sdpaCalls"])
        result = original_sdpa(query, key, value, *args, **kwargs)
        if log:
            mark("sdpa_return")
        return result

    torch.nn.functional.scaled_dot_product_attention = sdpa
    original_load = mod.OmniWorker._load_profile

    def load(self, profile):
        result = original_load(self, profile)
        if not getattr(self.model, "_diagnostic_hooked", False):
            original_generate = self.model.generate

            def generate(*args, **kwargs):
                mark("generate_enter", tensors={k: list(v.shape) for k, v in kwargs.items() if torch.is_tensor(v)})
                result = original_generate(*args, **kwargs)
                mark("generate_return", outputType=type(result).__name__)
                return result

            self.model.generate = generate
            self.model._diagnostic_hooked = True
            config = self.model.generation_config.to_dict()
            mark("runtime", variant=args.variant, clearCache=args.clear_cache, torch=torch.__version__,
                 flashBuilt=torch.backends.cuda.is_flash_attention_available(),
                 flashEnabled=torch.backends.cuda.flash_sdp_enabled(), generationConfig=config)
        return result

    mod.OmniWorker._load_profile = load
    original_text = mod.OmniWorker.generate_text

    def text(self, request_id, image_path, messages):
        phase.update(id=request_id, seen=set(), sdpaCalls=0)
        torch.cuda.reset_peak_memory_stats()
        mark("request_enter", turns=len(messages), imageMessages=sum(bool(m.get("includesImage")) for m in messages))
        if args.clear_cache:
            gc.collect()
            torch.cuda.empty_cache()
            mark("unused_cache_released")
        faulthandler.dump_traceback_later(min(90, args.deadline - 1), repeat=True, file=sys.stderr)
        try:
            result = original_text(self, request_id, image_path, messages)
            mark("request_return", sdpaCalls=phase["sdpaCalls"])
            return result
        finally:
            faulthandler.cancel_dump_traceback_later()

    mod.OmniWorker.generate_text = text
    mod.main()


def parent(args):
    output = Path(args.output).resolve()
    output.mkdir(parents=True, exist_ok=False)
    session = json.loads(Path(args.session).read_text(encoding="utf-8-sig"))
    image_path = args.image or session["file"]["sourcePath"]
    template = (ROOT / "Исходники/AIHub/Services/ImageAnalysisOmniPromptBuilder.cs").read_text(encoding="utf-8-sig")
    compose = re.search(r'private const string ComposeRussian = """\n(.*?)\n""";', template, re.S)[1]
    # Exact current builder substitutions for the saved large-cat test settings.
    assert session["settings"] == {"languageCode": "ru", "accuracy": "strict", "style": "atmospheric", "length": "brief", "form": "with_title", "wishes": ""}
    substitutions = {"output_language": "русский", "accuracy": "строгая; предпочитать непосредственно видимые факты и явно отмечать неопределённость",
                     "style": "атмосферный", "length": "краткий, но содержательный", "form": "текст с коротким заголовком", "wishes_or_none": "нет"}
    for key, value in substitutions.items():
        compose = compose.replace("{{" + key + "}}", value)
    messages = [session["hiddenConversation"][0]]
    env = dict(os.environ, PYTHONIOENCODING="utf-8", PYTHONUNBUFFERED="1", HF_HUB_OFFLINE="1")
    with (output / "stderr.log").open("w", encoding="utf-8") as errors, (output / "protocol.jsonl").open("w", encoding="utf-8") as protocol:
        child_args = [sys.executable, "-u", __file__, "--child", "--variant", args.variant,
                      "--deadline", str(args.deadline)]
        if args.clear_cache:
            child_args.append("--clear-cache")
        proc = subprocess.Popen(child_args, stdin=subprocess.PIPE,
                                stdout=subprocess.PIPE, stderr=errors, text=True, encoding="utf-8", env=env)
        responses = queue.Queue()

        def reader():
            for line in proc.stdout:
                responses.put(line)
            responses.put(None)

        threading.Thread(target=reader, daemon=True).start()

        def record(**item):
            item["time"] = time.time()
            protocol.write(json.dumps(item, ensure_ascii=False) + "\n")
            protocol.flush()

        def request(request_id, payload):
            record(direction="request", id=request_id, payload=payload)
            proc.stdin.write(json.dumps({"id": request_id, "payload": payload}, ensure_ascii=False) + "\n")
            proc.stdin.flush()
            start = time.monotonic()
            next_sample = start
            fragments = 0
            first = None
            print(f"START {payload['command']} pid={proc.pid}", flush=True)
            while time.monotonic() - start < args.deadline:
                if time.monotonic() >= next_sample:
                    sample = subprocess.run(["nvidia-smi", "--query-gpu=memory.used,memory.free,utilization.gpu", "--format=csv,noheader,nounits"], capture_output=True, text=True, timeout=3)
                    record(event="gpu", elapsed=time.monotonic()-start, value=sample.stdout.strip())
                    print(f"{payload['command']} {time.monotonic()-start:.1f}s GPU {sample.stdout.strip()} fragments={fragments}", flush=True)
                    next_sample = time.monotonic() + 15
                try:
                    line = responses.get(timeout=min(0.25, max(0.01, args.deadline-(time.monotonic()-start))))
                except queue.Empty:
                    continue
                if line is None:
                    raise RuntimeError(f"Worker exited: {proc.poll()}")
                result = json.loads(line)
                record(direction="response", value=result)
                if result.get("event") == "stream":
                    if result.get("text"):
                        fragments += 1
                        if first is None:
                            first = time.monotonic()-start
                            print(f"FIRST TEXT {first:.3f}s", flush=True)
                    continue
                compact = {k: v for k, v in result.items() if k not in {"content", "traceback", "deviceMap"}}
                print(json.dumps({"command": payload["command"], "wallSeconds": time.monotonic()-start, "result": compact}, ensure_ascii=False), flush=True)
                if not result.get("success"):
                    raise RuntimeError(result.get("error"))
                return result
            record(event="deadline", id=request_id, seconds=args.deadline, fragments=fragments)
            raise TimeoutError(f"{payload['command']} stopped at the {args.deadline}-second deadline")

        try:
            request(1, {"command": "warmup", "modelDirectory": args.model, "cpuBudgetBytes": 60*1024**3, "gpuBudgetBytes": 20*1024**3})
            for iteration in range(args.repeat):
                first_message = dict(session["hiddenConversation"][0])
                if args.simple_prompt:
                    first_message["content"] = "Опиши, что видишь на изображении."
                messages = [first_message]
                visual = request(2 + iteration*2, {"command": "analyze", "imagePath": image_path, "messages": messages})
                messages.extend([{"role": "assistant", "content": visual["content"], "includesImage": False},
                                 {"role": "user", "content": compose, "includesImage": False}])
                request(3 + iteration*2, {"command": "compose", "imagePath": image_path, "messages": messages})
            request(2 + args.repeat*2, {"command": "shutdown"})
        finally:
            if proc.poll() is None:
                # Windows venv python.exe is a launcher with a real Python child.
                # Killing only the launcher can leave CUDA allocations alive.
                if os.name == "nt":
                    subprocess.run(["taskkill", "/PID", str(proc.pid), "/T", "/F"],
                                   capture_output=True, timeout=10, creationflags=subprocess.CREATE_NO_WINDOW)
                if proc.poll() is None:
                    proc.kill()
            proc.wait(timeout=10)
            proc.stdin.close()
            proc.stdout.close()
            print(f"Owned diagnostic worker stopped; logs: {output}", flush=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--child", action="store_true")
    parser.add_argument("--session")
    parser.add_argument("--model")
    parser.add_argument("--output")
    parser.add_argument("--image")
    parser.add_argument("--variant", choices=("baseline", "repeat-kv"), default="baseline")
    parser.add_argument("--deadline", type=int, default=300)
    parser.add_argument("--repeat", type=int, default=1)
    parser.add_argument("--clear-cache", action="store_true")
    parser.add_argument("--simple-prompt", action="store_true")
    options = parser.parse_args()
    if options.child:
        child(options)
    else:
        parent(options)
