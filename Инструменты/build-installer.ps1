param(
    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "== $Message ==" -ForegroundColor Cyan
}

function Find-InnoCompiler {
    $command = Get-Command 'iscc.exe' -ErrorAction SilentlyContinue
    if ($command -and (Test-Path -LiteralPath $command.Source)) {
        return $command.Source
    }

    $candidates = @(
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 5\ISCC.exe",
        "$env:ProgramFiles(x86)\Inno Setup 5\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 5\ISCC.exe"
    )

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return $candidate
        }
    }

    return $null
}

function Escape-InnoDefineValue {
    param([string]$Value)
    return $Value
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'Исходники\AIHub\AIHub.csproj'
$versionPath = Join-Path $repoRoot 'VERSION'
$publishDir = Join-Path $repoRoot 'Runtime\Publish\AIHub-win-x64'
$installerDir = Join-Path $repoRoot 'Тесты\Установщики'
$innoScriptPath = Join-Path $repoRoot 'Инструменты\Installer\AI_HUB.iss'
$iconPath = Join-Path $repoRoot 'Исходники\AIHub\Assets\AppIcon.ico'
$backendDir = Join-Path $repoRoot 'Runtime\Backends\llama.cpp\b9442\win-cuda-12.4-x64'
$chatLlmBackendDir = Join-Path $repoRoot 'Runtime\Backends\chatllm.cpp\v24\win-x64'

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Не найден проект: $projectPath"
}

if (-not (Test-Path -LiteralPath $versionPath)) {
    throw "Не найден файл версии: $versionPath"
}

if (-not (Test-Path -LiteralPath $innoScriptPath)) {
    throw "Не найден Inno Setup сценарий: $innoScriptPath"
}

if (-not (Test-Path -LiteralPath $iconPath)) {
    throw "Не найдена иконка установщика: $iconPath"
}

if (-not (Test-Path -LiteralPath (Join-Path $backendDir 'llama-server.exe'))) {
    throw "Не найден llama.cpp backend для установщика: $backendDir"
}

if (-not (Test-Path -LiteralPath (Join-Path $chatLlmBackendDir 'server.exe'))) {
    throw "Не найден chatllm.cpp backend для установщика: $chatLlmBackendDir"
}

if (-not (Test-Path -LiteralPath (Join-Path $chatLlmBackendDir 'imagemagick\magick.exe'))) {
    throw "Не найден приватный ImageMagick для chatllm.cpp: $chatLlmBackendDir"
}

$version = (Get-Content -LiteralPath $versionPath -Raw).Trim()
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Файл VERSION пустой."
}

New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
New-Item -ItemType Directory -Path $installerDir -Force | Out-Null

Write-Host "AI_HUB: build test installer."
Write-Host "Version: $version"
Write-Host "Output: $installerDir"

if (-not $SkipPublish) {
    Write-Step "Publishing AI HUB"
    dotnet publish $projectPath `
        --configuration Release `
        --runtime win-x64 `
        --self-contained true `
        --output $publishDir `
        -p:PublishSingleFile=false `
        -p:PublishReadyToRun=false
}
else {
    Write-Step "Publish skipped"
}

$exePath = Join-Path $publishDir 'AIHub.exe'
if (-not (Test-Path -LiteralPath $exePath)) {
    throw "После publish не найден AIHub.exe: $exePath"
}

$iscc = Find-InnoCompiler
if (-not $iscc) {
    Write-Host ""
    Write-Host "Inno Setup Compiler не найден." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Чтобы сборка установщика заработала, установите Inno Setup 6."
    Write-Host "Самый простой вариант через winget:"
    Write-Host ""
    Write-Host "  winget install --id JRSoftware.InnoSetup -e"
    Write-Host ""
    Write-Host "После установки снова запустите Собрать_установщик_AI_HUB.cmd."
    exit 2
}

Write-Step "Building installer with Inno Setup"
Write-Host "ISCC: $iscc"

$arguments = @(
    "/DAppVersion=$version",
    "/DPublishDir=$(Escape-InnoDefineValue $publishDir)",
    "/DBackendDir=$(Escape-InnoDefineValue $backendDir)",
    "/DChatLlmBackendDir=$(Escape-InnoDefineValue $chatLlmBackendDir)",
    "/DOutputDir=$(Escape-InnoDefineValue $installerDir)",
    "/DSetupIconFile=$(Escape-InnoDefineValue $iconPath)",
    $innoScriptPath
)

& $iscc @arguments

$setupPath = Join-Path $installerDir "AI_HUB_Setup_$version.exe"
if (-not (Test-Path -LiteralPath $setupPath)) {
    throw "Сборка завершилась, но ожидаемый установщик не найден: $setupPath"
}

Write-Host ""
Write-Host "Готово: $setupPath" -ForegroundColor Green
