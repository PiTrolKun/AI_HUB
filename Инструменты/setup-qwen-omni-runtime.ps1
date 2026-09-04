param(
    [string]$BasePython = "",
    [string]$TorchIndexUrl = "https://download.pytorch.org/whl/cu130",
    [string]$TorchVersion = "2.11.0",
    [string]$TorchVisionVersion = "0.26.0",
    [string]$TorchAudioVersion = "2.11.0"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$runtimeRoot = Join-Path $projectRoot "Runtime\Python\qwen3-omni"
$venvRoot = Join-Path $runtimeRoot ".venv"
$runtimePython = Join-Path $venvRoot "Scripts\python.exe"

if ([string]::IsNullOrWhiteSpace($BasePython)) {
    $launcher = Get-Command py.exe -ErrorAction SilentlyContinue
    if ($null -eq $launcher) {
        throw "Python launcher py.exe was not found. Pass -BasePython with a 64-bit Python 3.12 executable."
    }
    & $launcher.Source -3.12 -m venv $venvRoot
}
else {
    if (-not (Test-Path -LiteralPath $BasePython -PathType Leaf)) {
        throw "Base Python was not found: $BasePython"
    }
    & $BasePython -m venv $venvRoot
}

if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $runtimePython -PathType Leaf)) {
    throw "The isolated Qwen2.5-Omni virtual environment could not be created."
}

& $runtimePython -m pip install --upgrade pip "setuptools<82" wheel
if ($LASTEXITCODE -ne 0) {
    throw "pip bootstrap failed with exit code $LASTEXITCODE."
}

& $runtimePython -m pip install --index-url $TorchIndexUrl `
    "torch==$TorchVersion" `
    "torchvision==$TorchVisionVersion" `
    "torchaudio==$TorchAudioVersion"
if ($LASTEXITCODE -ne 0) {
    throw "CUDA PyTorch installation failed with exit code $LASTEXITCODE. Check the selected Torch index against the installed NVIDIA driver."
}

& $runtimePython -m pip install `
    "transformers==5.16.1" `
    "accelerate==1.14.0" `
    "qwen-omni-utils==0.0.9" `
    "numpy==2.5.2" `
    "soundfile==0.14.0" `
    "audioread==3.1.0"
if ($LASTEXITCODE -ne 0) {
    throw "Qwen2.5-Omni dependency installation failed with exit code $LASTEXITCODE."
}

& $runtimePython -c "import torch, transformers, accelerate, qwen_omni_utils, soundfile; from transformers import Qwen2_5OmniForConditionalGeneration, Qwen2_5OmniProcessor; assert torch.cuda.is_available(), 'CUDA is unavailable'; print('Qwen2.5-Omni isolated runtime is ready'); print('torch=' + torch.__version__); print('cuda=' + str(torch.version.cuda)); print('transformers=' + transformers.__version__); print('accelerate=' + accelerate.__version__)"
if ($LASTEXITCODE -ne 0) {
    throw "Qwen2.5-Omni runtime import/CUDA probe failed with exit code $LASTEXITCODE."
}

Write-Host "Runtime: $runtimeRoot"
