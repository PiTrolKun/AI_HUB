param(
    [string]$PythonPath = ""
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($PythonPath)) {
    $PythonPath = Join-Path $projectRoot "Runtime\Python\reranker\.venv\Scripts\python.exe"
}

if (-not (Test-Path -LiteralPath $PythonPath -PathType Leaf)) {
    throw "Python runtime was not found: $PythonPath"
}

& $PythonPath -m pip install `
    "kokoro==0.9.4" `
    "misaki[en]==0.9.4" `
    "ruaccent==1.5.8.3"

if ($LASTEXITCODE -ne 0) {
    throw "Kokoro runtime preparation failed with exit code $LASTEXITCODE."
}

& $PythonPath -c "import kokoro, misaki, ruaccent, onnxruntime; print('Kokoro runtime is ready.')"
if ($LASTEXITCODE -ne 0) {
    throw "Kokoro runtime import check failed with exit code $LASTEXITCODE."
}
