import sys
from pathlib import Path


def allow_optional_flash_attention() -> None:
    from transformers import dynamic_module_utils
    from transformers.utils import is_flash_attn_2_available

    if is_flash_attn_2_available():
        return

    original_get_imports = dynamic_module_utils.get_imports

    def get_imports_without_optional_flash_attention(filename):
        imports = original_get_imports(filename)
        if Path(filename).name == "modeling_florence2.py":
            return [name for name in imports if name != "flash_attn"]
        return imports

    dynamic_module_utils.get_imports = get_imports_without_optional_flash_attention


def main() -> int:
    if len(sys.argv) != 2:
        print("The local Florence directory was not provided.", file=sys.stderr)
        return 2

    try:
        import numpy as np
        import PIL
        import einops
        import timm
        import torch
        import torchvision
        import transformers
        from transformers import AutoModelForCausalLM, AutoProcessor

        expected_versions = {
            "torch": "2.12.0",
            "torchvision": "0.27.0",
            "pillow": "12.3.0",
            "transformers": "4.41.2",
            "timm": "1.0.28",
            "einops": "0.8.2",
        }
        actual_versions = {
            "torch": torch.__version__.split("+")[0],
            "torchvision": torchvision.__version__.split("+")[0],
            "pillow": PIL.__version__,
            "transformers": transformers.__version__,
            "timm": timm.__version__,
            "einops": einops.__version__,
        }
        mismatches = [
            f"{name} {actual_versions[name]} (expected {expected})"
            for name, expected in expected_versions.items()
            if actual_versions[name] != expected
        ]
        if mismatches:
            raise RuntimeError(
                "Florence Python runtime version mismatch: " + ", ".join(mismatches)
            )

        allow_optional_flash_attention()
        model_path = sys.argv[1]
        processor = AutoProcessor.from_pretrained(
            model_path,
            trust_remote_code=True,
            local_files_only=True,
        )
        model = AutoModelForCausalLM.from_pretrained(
            model_path,
            trust_remote_code=True,
            local_files_only=True,
            torch_dtype=torch.float32,
        ).eval()
        image = np.zeros((64, 64, 3), dtype=np.uint8)
        inputs = processor(text="<CAPTION>", images=image, return_tensors="pt")
        with torch.inference_mode():
            generated = model.generate(
                input_ids=inputs["input_ids"],
                pixel_values=inputs["pixel_values"],
                max_new_tokens=4,
                num_beams=1,
                do_sample=False,
                early_stopping=False,
            )
        result = processor.batch_decode(generated, skip_special_tokens=False)[0]
        if not result.strip():
            raise RuntimeError("Florence returned an empty response.")
        print("ok")
        return 0
    except Exception as exc:
        print("AI_HUB_SMOKE_ERROR:" + str(exc)[:500], file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
