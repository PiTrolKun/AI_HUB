import json
import math
import sys

import torch
from transformers import AutoModelForSequenceClassification, AutoTokenizer


def main() -> int:
    payload = json.loads(sys.stdin.buffer.read().decode("utf-8-sig"))
    model_dir = payload["model_dir"]
    query = payload["query"]
    documents = payload["documents"]

    tokenizer = AutoTokenizer.from_pretrained(model_dir, local_files_only=True)
    model = AutoModelForSequenceClassification.from_pretrained(model_dir, local_files_only=True)
    device = "cuda" if torch.cuda.is_available() else "cpu"
    model.to(device)
    model.eval()

    pairs = [[query, item["text"][:4000]] for item in documents]
    with torch.no_grad():
        inputs = tokenizer(
            pairs,
            padding=True,
            truncation=True,
            return_tensors="pt",
            max_length=512,
        )
        inputs = {key: value.to(device) for key, value in inputs.items()}
        logits = model(**inputs, return_dict=True).logits.view(-1).float().cpu().tolist()

    scores = []
    for index, raw_score in enumerate(logits):
        normalized = 1.0 / (1.0 + math.exp(-raw_score))
        scores.append(
            {
                "index": index,
                "score": normalized,
                "raw_score": raw_score,
            }
        )

    json.dump({"mode": f"python-transformers-{device}", "scores": scores}, sys.stdout, ensure_ascii=False)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
