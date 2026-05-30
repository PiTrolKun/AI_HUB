@echo off
setlocal

cd /d "%~dp0"

echo AI_HUB: build and start.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0start-aihub.ps1"
if errorlevel 1 (
    echo.
    echo AI_HUB start failed.
    pause
    exit /b 1
)
