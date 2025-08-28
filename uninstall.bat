@echo off
setlocal enabledelayedexpansion

:: A3sist Uninstaller Script
:: This script completely removes A3sist API service and Visual Studio extension

echo ==========================================
echo A3sist AI Assistant Uninstaller
echo ==========================================
echo This will completely remove A3sist from your system:
echo • Stop and remove the A3sist API Windows service
echo • Remove installation files
echo • Remove configuration files
echo • Remove desktop shortcuts
echo • Remove firewall rules
echo.

:: Check for administrator privileges
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: This uninstaller requires administrator privileges.
    echo Please run as administrator.
    pause
    exit /b 1
)

set /p confirm="Are you sure you want to uninstall A3sist? (y/N): "
if /i "%confirm%" neq "y" (
    echo Uninstallation cancelled.
    pause
    exit /b 0
)

set "SERVICE_NAME=A3sistAPI"
set "INSTALL_DIR=C:\Program Files\A3sist"
set "CONFIG_DIR=%APPDATA%\A3sist"

echo.
echo Step 1: Stopping and removing A3sist API service...
echo ==========================================

:: Check if service exists
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorlevel% equ 0 (
    echo Stopping A3sist API service...
    sc stop "%SERVICE_NAME%" >nul 2>&1
    timeout /t 5 /nobreak >nul
    
    echo Removing A3sist API service...
    sc delete "%SERVICE_NAME%" >nul 2>&1
    if %errorlevel% equ 0 (
        echo ✓ A3sist API service removed successfully
    ) else (
        echo ⚠ Failed to remove service (may need manual cleanup)
    )
) else (
    echo ℹ A3sist API service was not installed
)

echo.
echo Step 2: Removing installation files...
echo ==========================================

if exist "%INSTALL_DIR%" (
    echo Removing installation directory: %INSTALL_DIR%
    rmdir /s /q "%INSTALL_DIR%" >nul 2>&1
    if exist "%INSTALL_DIR%" (
        echo ⚠ Could not remove all files in %INSTALL_DIR%
        echo   Some files may be in use. Please restart and run uninstaller again.
    ) else (
        echo ✓ Installation files removed successfully
    )
) else (
    echo ℹ Installation directory not found
)

echo.
echo Step 3: Removing configuration files...
echo ==========================================

if exist "%CONFIG_DIR%" (
    echo Removing configuration directory: %CONFIG_DIR%
    rmdir /s /q "%CONFIG_DIR%" >nul 2>&1
    if exist "%CONFIG_DIR%" (
        echo ⚠ Could not remove configuration directory
    ) else (
        echo ✓ Configuration files removed successfully
    )
) else (
    echo ℹ Configuration directory not found
)

echo.
echo Step 4: Removing desktop shortcuts...
echo ==========================================

set "DESKTOP=%USERPROFILE%\Desktop"

if exist "%DESKTOP%\A3sist API Manager.lnk" (
    del "%DESKTOP%\A3sist API Manager.lnk" >nul 2>&1
    echo ✓ Removed A3sist API Manager shortcut
)

if exist "%DESKTOP%\A3sist Configuration.lnk" (
    del "%DESKTOP%\A3sist Configuration.lnk" >nul 2>&1
    echo ✓ Removed A3sist Configuration shortcut
)

if exist "%DESKTOP%\A3sist Documentation.lnk" (
    del "%DESKTOP%\A3sist Documentation.lnk" >nul 2>&1
    echo ✓ Removed A3sist Documentation shortcut
)

echo.
echo Step 5: Removing firewall rules...
echo ==========================================

netsh advfirewall firewall delete rule name="A3sist API" >nul 2>&1
if %errorlevel% equ 0 (
    echo ✓ Removed A3sist API firewall rule
) else (
    echo ℹ No A3sist API firewall rule found
)

echo.
echo Step 6: Visual Studio Extension cleanup...
echo ==========================================
echo.
echo NOTE: The Visual Studio extension cannot be automatically uninstalled.
echo To remove it manually:
echo 1. Open Visual Studio 2022
echo 2. Go to Extensions → Manage Extensions
echo 3. Find "A3sist AI Assistant" in the Installed tab
echo 4. Click Uninstall
echo 5. Restart Visual Studio
echo.

echo.
echo ==========================================
echo Uninstallation Complete!
echo ==========================================
echo.
echo A3sist has been removed from your system.
echo.
echo What was removed:
echo ✓ A3sist API Windows service
echo ✓ Installation files (%INSTALL_DIR%)
echo ✓ Configuration files (%CONFIG_DIR%)
echo ✓ Desktop shortcuts
echo ✓ Windows Firewall rules
echo.
echo Manual steps required:
echo • Remove Visual Studio extension (see instructions above)
echo.
echo Thank you for using A3sist AI Assistant!
echo.
pause