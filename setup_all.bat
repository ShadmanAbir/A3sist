@echo off
setlocal enabledelayedexpansion

:: ==========================================
:: A3sist Complete Setup Script
:: ==========================================
:: This is the ONLY setup script you need!
:: Handles everything automatically:
:: - Visual Studio detection
:: - .NET 9 SDK verification
:: - Building A3sist.API and A3sist.UI
:: - Installing the VSIX extension
:: - Starting the API service
:: ==========================================

echo.
echo ==========================================
echo        A3sist Complete Setup
echo ==========================================
echo This script will handle everything automatically:
echo  1. Detect Visual Studio installation
echo  2. Verify .NET 9 SDK
echo  3. Build both API and UI projects
echo  4. Install the Visual Studio extension
echo  5. Start the API service
echo.

set "SCRIPT_DIR=%~dp0"
set "API_DIR=%SCRIPT_DIR%A3sist.API"
set "UI_DIR=%SCRIPT_DIR%A3sist.UI"
set "VS_PATH="
set "MSBUILD_PATH="
set "DEVENV_PATH="

:: ==========================================
:: Step 1: Visual Studio Detection
:: ==========================================
echo [1/5] Detecting Visual Studio...

:: Try vswhere.exe first
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist "%VSWHERE%" (
    echo   ✓ Found vswhere.exe
    for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -products * -requires Microsoft.VisualStudio.Component.VSSDK -property installationPath`) do (
        set "VS_PATH=%%i"
    )
    
    if defined VS_PATH (
        echo   ✓ Visual Studio found at: !VS_PATH!
        set "MSBUILD_PATH=!VS_PATH!\MSBuild\Current\Bin\MSBuild.exe"
        set "DEVENV_PATH=!VS_PATH!\Common7\IDE\devenv.exe"
    )
)

:: Fallback to standard paths if vswhere failed
if not defined VS_PATH (
    echo   ⚠ vswhere detection failed, trying standard paths...
    for %%d in (Enterprise Professional Community) do (
        if exist "%ProgramFiles%\Microsoft Visual Studio\2022\%%d" (
            set "VS_PATH=%ProgramFiles%\Microsoft Visual Studio\2022\%%d"
            set "MSBUILD_PATH=!VS_PATH!\MSBuild\Current\Bin\MSBuild.exe"
            set "DEVENV_PATH=!VS_PATH!\Common7\IDE\devenv.exe"
            echo   ✓ Found Visual Studio %%d at: !VS_PATH!
            goto :vs_found
        )
        if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2022\%%d" (
            set "VS_PATH=%ProgramFiles(x86)%\Microsoft Visual Studio\2022\%%d"
            set "MSBUILD_PATH=!VS_PATH!\MSBuild\Current\Bin\MSBuild.exe"
            set "DEVENV_PATH=!VS_PATH!\Common7\IDE\devenv.exe"
            echo   ✓ Found Visual Studio %%d at: !VS_PATH!
            goto :vs_found
        )
    )
)

:vs_found
if not defined VS_PATH (
    echo   ❌ ERROR: Visual Studio 2022 not found!
    echo   Please install Visual Studio 2022 with Visual Studio SDK
    pause
    exit /b 1
)

:: Verify required files exist
if not exist "%MSBUILD_PATH%" (
    echo   ❌ ERROR: MSBuild not found at: %MSBUILD_PATH%
    pause
    exit /b 1
)

if not exist "%DEVENV_PATH%" (
    echo   ❌ ERROR: devenv.exe not found at: %DEVENV_PATH%
    pause
    exit /b 1
)

echo   ✓ MSBuild found at: %MSBUILD_PATH%
echo   ✓ devenv.exe found at: %DEVENV_PATH%

:: ==========================================
:: Step 2: .NET 9 SDK Verification
:: ==========================================
echo.
echo [2/5] Verifying .NET 9 SDK...

dotnet --version | findstr /r "^9\." >nul 2>&1
if %errorlevel% neq 0 (
    echo   ❌ ERROR: .NET 9 SDK is required but not found.
    echo   Please install .NET 9 SDK from: https://dotnet.microsoft.com/download/dotnet/9.0
    pause
    exit /b 1
)

for /f "tokens=*" %%v in ('dotnet --version') do set "DOTNET_VERSION=%%v"
echo   ✓ .NET SDK found: %DOTNET_VERSION%

:: ==========================================
:: Step 3: Build Projects
:: ==========================================
echo.
echo [3/5] Building projects...

:: Close any running Visual Studio instances to avoid file locks
echo   Checking for running Visual Studio instances...
powershell -Command "Get-Process -Name 'devenv' -ErrorAction SilentlyContinue | Measure-Object | Select-Object -ExpandProperty Count" > temp_count.txt
set /p VS_COUNT=<temp_count.txt
del temp_count.txt
if %VS_COUNT% gtr 0 (
    echo   ⚠ Visual Studio is running. Please close all Visual Studio instances and press any key to continue...
    pause >nul
)

:: Build A3sist.API
echo   Building A3sist.API (Release)...
cd "%API_DIR%"
dotnet clean --configuration Release >nul 2>&1
dotnet restore >nul 2>&1
dotnet build --configuration Release --no-restore
if %errorlevel% neq 0 (
    echo   ❌ ERROR: Failed to build A3sist.API
    cd "%SCRIPT_DIR%"
    pause
    exit /b 1
)
echo   ✓ A3sist.API build successful

:: Build A3sist.UI
echo   Building A3sist.UI (Release)...
cd "%UI_DIR%"
"%MSBUILD_PATH%" A3sist.UI.csproj /p:Configuration=Release /p:Platform="Any CPU" /t:Clean;Rebuild /nologo /verbosity:minimal
if %errorlevel% neq 0 (
    echo   ❌ ERROR: Failed to build A3sist.UI
    cd "%SCRIPT_DIR%"
    pause
    exit /b 1
)
echo   ✓ A3sist.UI build successful

cd "%SCRIPT_DIR%"

:: Verify build outputs
echo   Verifying build outputs...
if not exist "%API_DIR%\bin\Release\net9.0\A3sist.API.dll" (
    echo   ❌ ERROR: API binaries not found
    pause
    exit /b 1
)

if not exist "%UI_DIR%\bin\Release\A3sist.UI.vsix" (
    echo   ❌ ERROR: Extension VSIX not found
    pause
    exit /b 1
)

echo   ✓ All build outputs verified

:: ==========================================
:: Step 4: Install VSIX Extension
:: ==========================================
echo.
echo [4/5] Installing Visual Studio Extension...

set "VSIX_PATH=%UI_DIR%\bin\Release\A3sist.UI.vsix"

:: Check if extension is already installed
echo   Checking for existing installation...
"%DEVENV_PATH%" /rootsuffix Exp /command "Tools.ManageExtensions" >nul 2>&1

:: Install the VSIX
echo   Installing A3sist extension...
"%DEVENV_PATH%" /rootsuffix Exp "%VSIX_PATH%" /install /quiet
if %errorlevel% neq 0 (
    echo   ⚠ Trying alternative installation method...
    start "" "%VSIX_PATH%"
    echo   Please complete the VSIX installation manually and press any key to continue...
    pause >nul
) else (
    echo   ✓ Extension installed successfully
)

:: ==========================================
:: Step 5: Start API Service
:: ==========================================
echo.
echo [5/5] Starting A3sist API Service...

:: Check if port is in use using PowerShell since netstat might not be available
echo   Checking port availability...
powershell -Command "try { $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Any, 8342); $listener.Start(); $listener.Stop(); exit 0 } catch { exit 1 }" >nul 2>&1
if %errorlevel% equ 0 (
    set "API_PORT=8342"
) else (
    echo   ⚠ Port 8342 is already in use. Using port 8343...
    set "API_PORT=8343"
)

echo   Starting API service on port %API_PORT%...
cd "%API_DIR%"

:: Start API service in background with proper error handling
if "%API_PORT%"=="8343" (
    echo   Using alternate port due to conflict...
    start "A3sist API Service" /MIN cmd /c "echo Starting A3sist API on port 8343... && dotnet run --configuration Release --urls http://localhost:8343 && pause"
) else (
    start "A3sist API Service" /MIN cmd /c "echo Starting A3sist API on port 8342... && dotnet run --configuration Release && pause"
)

:: Wait for service to start
echo   Waiting for service to initialize...
timeout /t 3 /nobreak >nul

:: Verify service is running using PowerShell
echo   Verifying API service...
timeout /t 3 /nobreak >nul
powershell -Command "try { $response = Invoke-WebRequest -Uri 'http://localhost:%API_PORT%/api/health' -TimeoutSec 5 -UseBasicParsing; if ($response.StatusCode -eq 200) { exit 0 } else { exit 1 } } catch { exit 1 }" >nul 2>&1
if %errorlevel% equ 0 (
    echo   ✓ API service is running on http://localhost:%API_PORT%
) else (
    echo   ⚠ API service may still be starting...
)

cd "%SCRIPT_DIR%"

:: ==========================================
:: Setup Complete
:: ==========================================
echo.
echo ==========================================
echo           Setup Complete!
echo ==========================================
echo.
echo ✅ Visual Studio: %VS_PATH%
echo ✅ .NET SDK: %DOTNET_VERSION%
echo ✅ A3sist.API: Built and running on http://localhost:%API_PORT%
echo ✅ A3sist.UI: Built and installed as VSIX extension
echo.
echo 🔗 API Endpoints:
echo    • Health Check: http://localhost:%API_PORT%/api/health
echo    • Swagger UI: http://localhost:%API_PORT%/swagger
echo    • SignalR Hub: http://localhost:%API_PORT%/a3sistHub
echo.
echo 🎯 Next Steps:
echo 1. Open Visual Studio 2022
echo 2. Go to View → A3sist AI Assistant
echo 3. Configure your AI models in the settings
echo 4. Start using A3sist for AI-powered development!
echo.
echo 📁 Configuration stored in: %%AppData%%\A3sist\config.json
echo 📝 Logs available in: %%AppData%%\A3sist\logs\
echo.

:: Optional: Open Visual Studio and browser
set /p "open_vs=Open Visual Studio now? (y/n): "
if /i "%open_vs%"=="y" (
    echo Opening Visual Studio...
    start "" "%DEVENV_PATH%"
)

set /p "open_swagger=Open Swagger UI in browser? (y/n): "
if /i "%open_swagger%"=="y" (
    echo Opening Swagger UI...
    start "" "http://localhost:%API_PORT%/swagger"
)

echo.
echo Setup script completed. Press any key to exit...
pause >nul