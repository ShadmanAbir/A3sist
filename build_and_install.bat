@echo off
echo Building A3sist Visual Studio Extension...
echo.

REM Change to the project directory
cd /d "%~dp0"

REM Check for Visual Studio installation
set MSBUILD_PATH=""
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
) else (
    echo ERROR: Could not find MSBuild. Please make sure Visual Studio 2022 is installed.
    pause
    exit /b 1
)

echo Using MSBuild: %MSBUILD_PATH%
echo.

REM Clean and build the solution
echo Cleaning solution...
%MSBUILD_PATH% A3sist.sln /t:Clean /p:Configuration=Debug
if %ERRORLEVEL% neq 0 (
    echo ERROR: Clean failed
    pause
    exit /b 1
)

echo Building solution...
%MSBUILD_PATH% A3sist.sln /t:Build /p:Configuration=Debug
if %ERRORLEVEL% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo.
echo Build completed successfully!
echo.
echo The VSIX file should be located in: bin\Debug\A3sist.vsix
echo.
echo To install:
echo 1. Close all instances of Visual Studio
echo 2. Double-click the A3sist.vsix file to install
echo 3. Open Visual Studio
echo 4. Look for "A3sist AI Assistant" in the View menu
echo.
echo For more help, see HOW_TO_FIND_SIDEBAR.md
echo.
pause