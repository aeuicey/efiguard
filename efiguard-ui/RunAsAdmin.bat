@echo off
:: EfiGuard UI - Administrator Launcher
:: This script relaunches EfiGuard UI with elevated privileges.

cd /d "%~dp0"

:: Check if already running as admin
net session >nul 2>&1
if %errorLevel% == 0 (
    echo Already running as Administrator.
    start "" "EfiGuard UI.exe"
    exit /b
)

echo Requesting Administrator privileges...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~dp0EfiGuard UI.exe' -Verb RunAs"
