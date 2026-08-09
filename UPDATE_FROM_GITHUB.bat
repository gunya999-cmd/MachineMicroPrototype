@echo off
setlocal
cd /d "%~dp0"

echo Updating MachineMicroPrototype from GitHub...

where git >nul 2>&1
if errorlevel 1 (
    echo ERROR: Git is not installed or not available in PATH.
    echo Install Git for Windows, then run this file again.
    pause
    exit /b 1
)

git rev-parse --is-inside-work-tree >nul 2>&1
if errorlevel 1 (
    echo ERROR: This file is not inside a Git repository.
    pause
    exit /b 1
)

git fetch origin main
if errorlevel 1 (
    echo ERROR: Could not fetch from GitHub.
    pause
    exit /b 1
)

git pull --ff-only origin main
if errorlevel 1 (
    echo ERROR: Update failed. Local changes may be blocking the pull.
    echo No files were overwritten automatically.
    pause
    exit /b 1
)

echo.
echo SUCCESS: Project is up to date.
echo Unity should detect the changed files automatically.
pause
