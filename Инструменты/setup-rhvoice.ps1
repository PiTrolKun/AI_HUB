param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

$runtimeRoot = Join-Path $ProjectRoot 'Runtime\Voice\RHVoice\installers'
$packages = @(
    @{
        Name = 'RHVoice-voice-Russian-Aleksandr-v4.2.2017.22-setup.exe'
        Url = 'https://github.com/RHVoice/aleksandr-rus/releases/download/4.2/RHVoice-voice-Russian-Aleksandr-v4.2.2017.22-setup.exe'
        Sha256 = '6F89681EEF32D9D0F05F05592953904A7AF938AB2C7926827AE4F7A8D806F593'
    },
    @{
        Name = 'RHVoice-voice-English-Slt-v4.1.2017.22-setup.exe'
        Url = 'https://github.com/RHVoice/slt-eng/releases/download/4.1/RHVoice-voice-English-Slt-v4.1.2017.22-setup.exe'
        Sha256 = 'BB7198123FBD29E45BCFB08A4C3C7783360BAF5B04DB1D6D18CC20C834DFA962'
    }
)

New-Item -ItemType Directory -Force -Path $runtimeRoot | Out-Null

foreach ($package in $packages) {
    $path = Join-Path $runtimeRoot $package.Name
    if (-not (Test-Path -LiteralPath $path)) {
        Invoke-WebRequest -Uri $package.Url -OutFile $path
    }

    $actualSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash
    if ($actualSha256 -ne $package.Sha256) {
        throw "RHVoice package checksum mismatch for $($package.Name). Expected $($package.Sha256), got $actualSha256."
    }

    $process = Start-Process -FilePath $path -ArgumentList '/S' -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 0) {
        throw "RHVoice setup failed for $($package.Name) with exit code $($process.ExitCode)."
    }
}

$sapi = New-Object -ComObject SAPI.SpVoice
$installed = @()
for ($index = 0; $index -lt $sapi.GetVoices().Count; $index++) {
    $installed += $sapi.GetVoices().Item($index).GetDescription()
}

foreach ($required in @('Aleksandr', 'Slt')) {
    if ($installed -notcontains $required) {
        throw "RHVoice setup completed, but SAPI voice '$required' is not available."
    }
}

Write-Host 'RHVoice is ready: Aleksandr (Russian) and Slt (English).'
