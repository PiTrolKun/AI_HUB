@echo off
setlocal

set "AI_HUB_ROOT=%~dp0"

echo AI_HUB: build test installer.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command "$root=$env:AI_HUB_ROOT; $script=Get-ChildItem -LiteralPath $root -Recurse -Filter 'build-installer.ps1' | Where-Object { $_.FullName -like '*\build-installer.ps1' } | Select-Object -First 1 -ExpandProperty FullName; if (-not $script) { Write-Host 'build-installer.ps1 not found.'; exit 1 }; & $script"
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
