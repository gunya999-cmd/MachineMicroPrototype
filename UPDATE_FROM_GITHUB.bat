@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "NO_PAUSE=0"
if /I "%~1"=="--no-pause" set "NO_PAUSE=1"

echo ==============================================
echo   MachineMicroPrototype - GitHub Update
echo ==============================================
echo.

if not exist "Assets" (
    echo ERROR: Put this file in the root of the Unity project.
    echo The same folder must contain Assets, Packages and ProjectSettings.
    if "%NO_PAUSE%"=="0" pause
    exit /b 1
)

if not exist "Packages" (
    echo ERROR: Packages folder was not found.
    if "%NO_PAUSE%"=="0" pause
    exit /b 1
)

if not exist "ProjectSettings" (
    echo ERROR: ProjectSettings folder was not found.
    if "%NO_PAUSE%"=="0" pause
    exit /b 1
)

set "ZIP_FILE=%TEMP%\MachineMicroPrototype_main_%RANDOM%_%RANDOM%.zip"
set "UPDATE_DIR=%TEMP%\MachineMicroPrototype_update_%RANDOM%_%RANDOM%"
set "SOURCE_DIR=%UPDATE_DIR%\MachineMicroPrototype-main"

echo Downloading latest main from GitHub...

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop'; Invoke-WebRequest -UseBasicParsing -Uri 'https://github.com/gunya999-cmd/MachineMicroPrototype/archive/refs/heads/main.zip' -OutFile $env:ZIP_FILE; New-Item -ItemType Directory -Force -Path $env:UPDATE_DIR | Out-Null; Expand-Archive -LiteralPath $env:ZIP_FILE -DestinationPath $env:UPDATE_DIR -Force"

if errorlevel 1 (
    echo.
    echo ERROR: Could not download or unpack the project from GitHub.
    echo Check the internet connection and try again.
    if exist "%ZIP_FILE%" del /q "%ZIP_FILE%" >nul 2>&1
    if exist "%UPDATE_DIR%" rmdir /s /q "%UPDATE_DIR%" >nul 2>&1
    if "%NO_PAUSE%"=="0" pause
    exit /b 1
)

if not exist "%SOURCE_DIR%\Assets" (
    echo.
    echo ERROR: Downloaded archive has an unexpected structure.
    if exist "%ZIP_FILE%" del /q "%ZIP_FILE%" >nul 2>&1
    if exist "%UPDATE_DIR%" rmdir /s /q "%UPDATE_DIR%" >nul 2>&1
    if "%NO_PAUSE%"=="0" pause
    exit /b 1
)

echo Updating Unity project files...

robocopy "%SOURCE_DIR%" "%~dp0" /E /R:2 /W:1 /NFL /NDL /NJH /NJS /NP ^
    /XD ".git" "Library" "Temp" "Obj" "Logs" "UserSettings" "MemoryCaptures" "Records" "Build" "Builds" ".vs" ^
    /XF "UPDATE_FROM_GITHUB.bat"

set "ROBOCOPY_CODE=%ERRORLEVEL%"

if %ROBOCOPY_CODE% GEQ 8 (
    echo.
    echo ERROR: Some project files could not be copied. Robocopy code: %ROBOCOPY_CODE%
    if exist "%ZIP_FILE%" del /q "%ZIP_FILE%" >nul 2>&1
    if exist "%UPDATE_DIR%" rmdir /s /q "%UPDATE_DIR%" >nul 2>&1
    if "%NO_PAUSE%"=="0" pause
    exit /b 1
)

if exist "%ZIP_FILE%" del /q "%ZIP_FILE%" >nul 2>&1
if exist "%UPDATE_DIR%" rmdir /s /q "%UPDATE_DIR%" >nul 2>&1

echo.
echo SUCCESS: Project files were updated from GitHub main.
echo Return to Unity. Unity will import and compile the changed files.
echo.
if "%NO_PAUSE%"=="0" pause
