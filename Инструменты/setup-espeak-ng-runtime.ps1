param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

$version = '1.52.0'
$expectedSha256 = '7F673C709EA5DD579D3B5EBB98688CC575328A6AB7438D2BC405B88CEDAEAFB9'
$downloadUrl = "https://github.com/espeak-ng/espeak-ng/releases/download/$version/espeak-ng.msi"
$runtimeRoot = Join-Path $ProjectRoot "Runtime\Voice\eSpeakNG\$version"
$downloadDirectory = Join-Path $runtimeRoot 'download'
$extractDirectory = Join-Path $runtimeRoot 'extracted'
$msiPath = Join-Path $downloadDirectory 'espeak-ng.msi'
$runtimeDirectory = Join-Path $extractDirectory 'eSpeak NG'

New-Item -ItemType Directory -Force -Path $downloadDirectory, $extractDirectory | Out-Null

if (-not (Test-Path -LiteralPath $msiPath)) {
    Invoke-WebRequest -Uri $downloadUrl -OutFile $msiPath
}

$actualSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $msiPath).Hash
if ($actualSha256 -ne $expectedSha256) {
    throw "eSpeak NG MSI checksum mismatch. Expected $expectedSha256, got $actualSha256."
}

$dllPath = Join-Path $runtimeDirectory 'libespeak-ng.dll'
$dataPath = Join-Path $runtimeDirectory 'espeak-ng-data'
if (-not (Test-Path -LiteralPath $dllPath) -or -not (Test-Path -LiteralPath $dataPath)) {
    $process = Start-Process -FilePath 'msiexec.exe' -ArgumentList @(
        '/a',
        ('"' + $msiPath + '"'),
        '/qn',
        ('TARGETDIR="' + $extractDirectory + '"')
    ) -Wait -PassThru -WindowStyle Hidden

    if ($process.ExitCode -ne 0) {
        throw "Administrative extraction of eSpeak NG failed with exit code $($process.ExitCode)."
    }
}

if (-not (Test-Path -LiteralPath $dllPath) -or -not (Test-Path -LiteralPath $dataPath)) {
    throw 'The extracted eSpeak NG runtime is incomplete.'
}

$dllSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $dllPath).Hash
Write-Host "eSpeak NG $version is ready."
Write-Host "Runtime: $runtimeDirectory"
Write-Host "MSI SHA-256: $actualSha256"
Write-Host "DLL SHA-256: $dllSha256"
