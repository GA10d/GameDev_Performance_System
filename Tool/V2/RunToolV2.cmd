@echo off
set "PLAYER=%~dp0Build\ASTRA Performance Tool V2.exe"
if not exist "%PLAYER%" (
  echo Player is not built. Run: powershell -ExecutionPolicy Bypass -File "%~dp0Rebuild.ps1"
  pause
  exit /b 1
)
start "" "%PLAYER%"
