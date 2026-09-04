"""Scoped workaround for redundant legacy-token reconciliation in RUAccent.

The reviewed tokenizer.json already contains the complete added-token backend.
Keep this workaround opt-in to the exact tested files and dependency versions.
"""

from contextlib import contextmanager
from hashlib import sha256
from importlib.metadata import PackageNotFoundError, version
import json
from pathlib import Path
import sys


_VERSIONS = {"transformers": "4.41.2", "tokenizers": "0.19.1", "ruaccent": "1.5.8.3"}
_CHECKPOINT_HASHES = {
    "tokenizer.json": "eb84a77af38a91f6f327486c7827d8541f8d446250f8d408909999b6be817afa",
    "tokenizer_config.json": "f159df92d0b0be03e1ceb0768222aa4c21dcfd37c5763b6b395b6864e023c7fb",
    "added_tokens.json": "071c01df9ba59b64b9d0d9af0eaac5412a5fc558d540a214ada4ee7531d38096",
}


def _validate_checkpoint(root: Path) -> None:
    contents = {}
    for name, expected in _CHECKPOINT_HASHES.items():
        content = (root / name).read_bytes()
        if sha256(content).hexdigest() != expected:
            raise ValueError(f"unreviewed checkpoint file: {name}")
        contents[name] = json.loads(content)
    backend = contents["tokenizer.json"]
    tokens = dict(backend["model"]["vocab"])
    tokens.update({token["content"]: token["id"] for token in backend["added_tokens"]})
    legacy = contents["added_tokens.json"]
    if not legacy or any(tokens.get(token) != token_id for token, token_id in legacy.items()):
        raise ValueError("legacy token IDs are not preserved in tokenizer.json")


@contextmanager
def compatible_ru_tokenizer(accent_root: Path):
    """Patch only the known local tokenizer call; restore even if loading fails.

    The worker processes requests serially. No other thread may load a tokenizer
    while this short-lived classmethod patch is installed.
    """
    state = {"status": "not_requested"}
    try:
        actual = {name: version(name) for name in _VERSIONS}
        if actual != _VERSIONS:
            raise ValueError(f"unreviewed dependency versions: {actual}")
        target = (accent_root / "nn" / "nn_omograph" / "turbo3.1").resolve()
        _validate_checkpoint(target)
    except (OSError, ValueError, KeyError, TypeError, PackageNotFoundError) as exc:
        state["status"] = f"skipped: {exc}"
        print(f"ru_tokenizer_compat={state['status']}", file=sys.stderr, flush=True)
        yield state
        return

    from transformers import AutoTokenizer

    original_descriptor = AutoTokenizer.__dict__["from_pretrained"]
    original = AutoTokenizer.from_pretrained

    def load(cls, pretrained_model_name_or_path, *args, **kwargs):
        path = pretrained_model_name_or_path
        is_target = isinstance(path, (str, Path)) and Path(path).resolve() == target
        if is_target and not args and not kwargs:
            kwargs["added_tokens_decoder"] = {}
            state["status"] = "applied; legacy_tokens_preserved=true"
            print(f"ru_tokenizer_compat={state['status']}", file=sys.stderr, flush=True)
        return original(pretrained_model_name_or_path, *args, **kwargs)

    AutoTokenizer.from_pretrained = classmethod(load)
    try:
        yield state
    finally:
        AutoTokenizer.from_pretrained = original_descriptor
