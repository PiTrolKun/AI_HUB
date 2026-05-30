$ErrorActionPreference = 'Stop'

$project = Get-ChildItem -LiteralPath $PSScriptRoot -Recurse -Filter 'AIHub.csproj' |
    Where-Object { $_.FullName -like '*AIHub*' } |
    Select-Object -First 1

if ($null -eq $project) {
    Write-Error 'AIHub.csproj was not found.'
}

Write-Host 'Building AI_HUB...'
dotnet build $project.FullName

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$exe = Join-Path $project.Directory.FullName 'bin\Debug\net10.0-windows\AIHub.exe'

if (-not (Test-Path -LiteralPath $exe)) {
    Write-Error "AIHub.exe was not found: $exe"
}

Write-Host 'Starting AI_HUB...'
Start-Process -FilePath $exe
