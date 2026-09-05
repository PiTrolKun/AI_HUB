@echo off
setlocal

set "LOPATA_ROOT=%~dp0"

echo LOPATA: build test installer.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command "$root=$env:LOPATA_ROOT; $script=Get-ChildItem -LiteralPath $root -Directory | ForEach-Object { Join-Path $_.FullName 'build-installer.ps1' } | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1; if (-not $script) { Write-Host 'build-installer.ps1 not found.'; exit 1 }; & $script"
set "EXIT_CODE=%ERRORLEVEL%"

echo.
if "%EXIT_CODE%"=="0" (
    echo Installer build completed.
) else if "%EXIT_CODE%"=="2" (
    echo Installer was not built because Inno Setup is not installed.
) else (
    echo Installer build failed: %EXIT_CODE%
)

echo.
pause
exit /b %EXIT_CODE%
