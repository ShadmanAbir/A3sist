@echo off
setlocal enabledelayedexpansion

echo ==========================================
echo A3sist Build Script
echo ==========================================
echo This script builds both A3sist.API and A3sist.UI projects
echo in Release mode, preparing them for installation.
echo.

:: Check for .NET 9 SDK
echo Checking for .NET 9 SDK...
dotnet --version | findstr /r "^9\." >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET 9 SDK is required but not found.
    echo Please install .NET 9 SDK from: https://dotnet.microsoft.com/download/dotnet/9.0
    pause
    exit /b 1
)
echo ✓ .NET 9 SDK found

:: Check for MSBuild (Visual Studio)
echo Checking for MSBuild...
set "MSBUILD_PATH="
for %%d in (Enterprise Professional Community) do (
    if exist "%ProgramFiles%\Microsoft Visual Studio\2022\%%d\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=%ProgramFiles%\Microsoft Visual Studio\2022\%%d\MSBuild\Current\Bin\MSBuild.exe"
        goto :found_msbuild
    )
    if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2022\%%d\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=%ProgramFiles(x86)%\Microsoft Visual Studio\2022\%%d\MSBuild\Current\Bin\MSBuild.exe"
        goto :found_msbuild
    )
)

:found_msbuild
if "%MSBUILD_PATH%"=="" (
    echo ERROR: MSBuild not found. Please install Visual Studio 2022.
    pause
    exit /b 1
)
echo ✓ MSBuild found

echo.
echo Building A3sist.API (Release)...
echo ==========================================
cd A3sist.API
dotnet clean --configuration Release
dotnet restore
dotnet build --configuration Release --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to build A3sist.API
    pause
    exit /b 1
)
echo ✓ A3sist.API build successful
cd ..

echo.
echo Building A3sist.UI (Release)...
echo ==========================================
cd A3sist.UI
"%MSBUILD_PATH%" A3sist.UI.csproj /p:Configuration=Release /p:Platform="Any CPU" /t:Clean;Rebuild
if %errorlevel% neq 0 (
    echo ERROR: Failed to build A3sist.UI
    pause
    exit /b 1
)
echo ✓ A3sist.UI build successful
cd ..

echo.
echo Verifying build outputs...
echo ==========================================

:: Check API output
if exist "A3sist.API\bin\Release\net9.0\A3sist.API.dll" (
    echo ✓ API binaries found
) else (
    echo ERROR: API binaries not found
    pause
    exit /b 1
)

:: Check UI output
if exist "A3sist.UI\bin\Release\A3sist.UI.vsix" (
    echo ✓ Extension VSIX found
) else (
    echo ERROR: Extension VSIX not found
    pause
    exit /b 1
)

echo.
echo ==========================================
echo Build Complete!
echo ==========================================
echo.
echo Both projects have been successfully built in Release mode:
echo • A3sist.API\bin\Release\net9.0\
echo • A3sist.UI\bin\Release\A3sist.UI.vsix
echo.
echo You can now run the installer:
echo • install.bat (for basic installation)
echo • install.ps1 (for advanced installation with PowerShell)
echo.
pause