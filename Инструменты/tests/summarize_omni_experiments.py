"""Summarize direct-worker artifacts without running models or changing evidence."""
import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image


def summarize(root):
    rows = []
    for folder in sorted(Path(root).iterdir()):
        protocol = folder / "protocol.jsonl"
        if not protocol.is_file():
            continue
        events = [json.loads(line) for line in protocol.read_text(encoding="utf-8").splitlines() if line.strip()]
        diag = [json.loads(line[5:]) for line in (folder/"stderr.log").read_text(encoding="utf-8", errors="replace").splitlines() if line.startswith("DIAG ")]
        runtime = next((d for d in diag if d["event"] == "runtime"), {})
        operator_stop_path = folder / "operator-stop.json"
        operator_stop = json.loads(operator_stop_path.read_text(encoding="utf-8")) if operator_stop_path.is_file() else {}
        for event in events:
            payload = event.get("payload", {})
            if event.get("direction") != "request" or payload.get("command") not in ("analyze", "compose"):
                continue
            request_id = event["id"]
            result = next((e["value"] for e in events if e.get("direction") == "response" and e["value"]["id"] == request_id and "success" in e["value"]), {})
            request_diag = [d for d in diag if d.get("id") == request_id]
            attention = next((d for d in request_diag if d["event"] == "sdpa_enter" and d["query"][-1] == 128 and d["query"][-2] > 1), {})
            generate = next((d for d in request_diag if d["event"] == "generate_enter"), {})
            deadline = next((e for e in events if e.get("event") == "deadline" and e.get("id") == request_id), {})
            image = Path(payload["imagePath"])
            with Image.open(image) as source:
                width, height = source.size
            content = result.get("content", "")
            stream_events = [e for e in events if e.get("direction") == "response" and e["value"]["id"] == request_id and e["value"].get("event") == "stream" and e["value"].get("text")]
            parsed = None
            parse_error = None
            if payload["command"] == "compose" and content:
                try:
                    start = content.find("{")
                    if start < 0:
                        raise ValueError("No JSON object")
                    # Match the application's acceptance of Markdown fences.
                    parsed, end = json.JSONDecoder().raw_decode(content[start:])
                    if "{" in content[start+end:]:
                        raise ValueError("More than one JSON object")
                except ValueError as exc:
                    parse_error = str(exc)
            rows.append({"case": folder.name, "variant": runtime.get("variant"), "stage": payload["command"],
                         "clearCache": runtime.get("clearCache", False),
                         "firstPrompt": payload["messages"][0]["content"],
                         "requestId": request_id, "image": image.name, "width": width, "height": height,
                         "imageSha256": hashlib.sha256(image.read_bytes()).hexdigest(),
                         "success": result.get("success", False), "timeoutSeconds": deadline.get("seconds"),
                         "operatorStop": operator_stop.get("reason") if operator_stop.get("requestId") == request_id else None,
                         "streamTextCharacters": sum(len(e["value"]["text"]) for e in stream_events),
                         "firstStreamMs": round((stream_events[0]["time"]-event["time"])*1000) if stream_events else None,
                         "visualPatchCount": generate.get("tensors", {}).get("pixel_values", [None])[0],
                         "inputTokens": result.get("inputTokens", generate.get("tensors", {}).get("input_ids", [None, None])[-1]),
                         "generatedTokens": result.get("generatedTokens"), "elapsedMs": result.get("elapsedMilliseconds"),
                         "ttftMs": result.get("timeToFirstTokenMilliseconds"), "finishReason": result.get("finishReason"),
                         "textBackend": attention.get("selectedBackend"),
                         "peakAllocatedBytesRecorded": max((d["peakAllocated"] for d in request_diag), default=None),
                         "peakReservedBytesRecorded": max((d["reserved"] for d in request_diag), default=None),
                         "jsonValid": parsed is not None, "jsonParseError": parse_error,
                         "title": parsed.get("title") if isinstance(parsed, dict) else None,
                         "paragraphCount": len(parsed.get("paragraphs", [])) if isinstance(parsed, dict) else None,
                         "reviewCount": len(parsed.get("review_items", [])) if isinstance(parsed, dict) else None,
                         "content": content})
    return rows


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("root")
    parser.add_argument("--output")
    options = parser.parse_args()
    result = summarize(options.root)
    serialized = json.dumps(result, ensure_ascii=False, indent=2)
    if options.output:
        with Path(options.output).open("x", encoding="utf-8") as destination:
            destination.write(serialized + "\n")
    else:
        for row in result:
            print(json.dumps({k: v for k, v in row.items() if k != "content"}, ensure_ascii=False))
