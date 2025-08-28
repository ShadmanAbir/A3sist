@echo off
setlocal enabledelayedexpansion

:: A3sist Master Setup Script
:: This script builds the projects and installs A3sist API + Extension

echo ==========================================
echo A3sist AI Assistant - Master Setup
echo ==========================================
echo This script will:
echo 1. Build A3sist.API (.NET 9)
echo 2. Build A3sist.UI (.NET Framework 4.7.2)
echo 3. Install API as Windows Service
echo 4. Install Visual Studio Extension
echo 5. Configure and start services
echo.

:: Check for administrator privileges
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: This installer requires administrator privileges.
    echo Please run as administrator.
    pause
    exit /b 1
)

echo Press any key to start the setup process...
pause >nul

echo.
echo ==========================================
echo PHASE 1: Building Projects
echo ==========================================
echo.

call "%~dp0build.bat"
if %errorlevel% neq 0 (
    echo ERROR: Build failed. Cannot proceed with installation.
    pause
    exit /b 1
)

echo.
echo ==========================================
echo PHASE 2: Installing A3sist
echo ==========================================
echo.

echo Choose installation method:
echo 1. Standard installation (Batch script)
echo 2. Advanced installation (PowerShell)
echo 3. Exit without installing
echo.
set /p choice="Enter your choice (1-3): "

if "%choice%"=="1" (
    echo Starting standard installation...
    call "%~dp0install.bat"
) else if "%choice%"=="2" (
    echo Starting advanced installation...
    powershell -ExecutionPolicy Bypass -File "%~dp0install.ps1"
) else if "%choice%"=="3" (
    echo Installation cancelled by user.
    goto :end
) else (
    echo Invalid choice. Defaulting to standard installation...
    call "%~dp0install.bat"
)

:end
echo.
echo ==========================================
echo Setup Complete!
echo ==========================================
echo.
echo Next steps:
echo 1. Open Visual Studio 2022
echo 2. Go to View → Other Windows → A3sist Assistant
echo 3. Configure API connection if needed
echo 4. Start using AI-powered development assistance!
echo.
echo For troubleshooting, see README.md
echo.
pause