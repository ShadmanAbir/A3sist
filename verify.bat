@echo off
setlocal enabledelayedexpansion

echo ==========================================
echo A3sist Installation Verification
echo ==========================================
echo This script checks if A3sist is properly installed and running.
echo.

set "SERVICE_NAME=A3sistAPI"
set "API_PORT=8341"
set "INSTALL_DIR=C:\Program Files\A3sist"
set "CONFIG_DIR=%APPDATA%\A3sist"

echo Checking A3sist API Service...
echo ==========================================

:: Check if service exists
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorlevel% equ 0 (
    echo ✓ A3sist API service is installed
    
    :: Check service status
    for /f "tokens=3" %%a in ('sc query "%SERVICE_NAME%" ^| findstr "STATE"') do set "SERVICE_STATE=%%a"
    
    if "!SERVICE_STATE!"=="RUNNING" (
        echo ✓ A3sist API service is running
    ) else (
        echo ⚠ A3sist API service is installed but not running (State: !SERVICE_STATE!)
        echo   You can start it with: sc start "%SERVICE_NAME%"
    )
) else (
    echo ✗ A3sist API service is not installed
)

echo.
echo Checking API Endpoint...
echo ==========================================

:: Test API endpoint
curl -s --connect-timeout 5 "http://localhost:%API_PORT%/api/health" >nul 2>&1
if %errorlevel% equ 0 (
    echo ✓ API endpoint is responding at http://localhost:%API_PORT%
) else (
    echo ✗ API endpoint is not responding
    echo   Make sure the service is running and port %API_PORT% is not blocked
)

echo.
echo Checking Installation Files...
echo ==========================================

:: Check API installation
if exist "%INSTALL_DIR%\API\A3sist.API.dll" (
    echo ✓ API files are installed at %INSTALL_DIR%\API\
) else (
    echo ✗ API files not found at %INSTALL_DIR%\API\
)

:: Check configuration
if exist "%CONFIG_DIR%\config.json" (
    echo ✓ Configuration file exists at %CONFIG_DIR%\config.json
) else (
    echo ⚠ Configuration file not found at %CONFIG_DIR%\config.json
)

echo.
echo Checking Visual Studio Extension...
echo ==========================================

:: Check for VSIX in common VS installation paths
set "EXTENSION_FOUND=0"
for %%d in (Community Professional Enterprise) do (
    set "VS_EXTENSIONS=%LOCALAPPDATA%\Microsoft\VisualStudio\17.0_*\Extensions"
    if exist "!VS_EXTENSIONS!" (
        dir /s /b "!VS_EXTENSIONS!\*A3sist*" >nul 2>&1
        if !errorlevel! equ 0 (
            set "EXTENSION_FOUND=1"
        )
    )
)

if %EXTENSION_FOUND% equ 1 (
    echo ✓ A3sist extension appears to be installed in Visual Studio
) else (
    echo ⚠ A3sist extension not detected
    echo   You may need to install it manually: A3sist.UI\bin\Release\A3sist.UI.vsix
)

echo.
echo Checking Network Configuration...
echo ==========================================

:: Check if port is in use
netstat -an | findstr ":%API_PORT%" >nul 2>&1
if %errorlevel% equ 0 (
    echo ✓ Port %API_PORT% is in use (likely by A3sist API)
) else (
    echo ⚠ Port %API_PORT% is not in use
)

:: Check Windows Firewall (simplified)
netsh advfirewall firewall show rule name="A3sist API" >nul 2>&1
if %errorlevel% equ 0 (
    echo ✓ Windows Firewall rule exists for A3sist API
) else (
    echo ⚠ Windows Firewall rule not found for A3sist API
)

echo.
echo ==========================================
echo Verification Summary
echo ==========================================
echo.
echo If everything shows ✓, A3sist is properly installed and ready to use.
echo If you see ⚠ or ✗, please refer to the troubleshooting section in README.md
echo.
echo To use A3sist:
echo 1. Open Visual Studio 2022
echo 2. Go to View → Other Windows → A3sist Assistant
echo 3. Start coding with AI assistance!
echo.
pause