$ErrorActionPreference = 'Stop'

$projectPath = Join-Path $PSScriptRoot 'Исходники\AIHub\AIHub.csproj'

if (-not (Test-Path -LiteralPath $projectPath)) {
    Write-Error "AIHub.csproj was not found: $projectPath"
}

Write-Host 'Building AI_HUB...'
dotnet build $projectPath

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$exe = Join-Path $PSScriptRoot 'Исходники\AIHub\bin\Debug\net10.0-windows\AIHub.exe'

if (-not (Test-Path -LiteralPath $exe)) {
    Write-Error "AIHub.exe was not found: $exe"
}

Write-Host 'Starting AI_HUB...'
Start-Process -FilePath $exe
