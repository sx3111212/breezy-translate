@echo off
chcp 65001 >nul
if exist "%~dp0artifacts\native\Breezy.exe" (
    start "" "%~dp0artifacts\native\Breezy.exe"
) else (
    echo Build the native app first: powershell -File native\Build.ps1 -Check
    pause
)