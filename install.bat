@echo off
setlocal enabledelayedexpansion

:: A3sist Installer Script
:: This script installs the A3sist API as a Windows service and installs the Visual Studio extension

echo ==========================================
echo A3sist AI Assistant Installer
echo ==========================================
echo.

:: Check for administrator privileges
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: This installer requires administrator privileges.
    echo Please run as administrator.
    pause
    exit /b 1
)

:: Set installation paths
set "INSTALL_DIR=C:\Program Files\A3sist"
set "API_DIR=%INSTALL_DIR%\API"
set "SERVICE_NAME=A3sistAPI"
set "SERVICE_DISPLAY=A3sist API Service"
set "API_PORT=8341"

echo Step 1: Checking prerequisites...
echo ==========================================

:: Check if .NET 9 is installed
dotnet --version | findstr /r "^9\." >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET 9 runtime is required but not found.
    echo Please install .NET 9 runtime from: https://dotnet.microsoft.com/download/dotnet/9.0
    pause
    exit /b 1
)
echo ✓ .NET 9 runtime found

:: Check if Visual Studio 2022 is installed
echo Detecting Visual Studio 2022 installations...
echo.

:: Use the comprehensive VS detection script
if exist "detect_vs.bat" (
    call "detect_vs.bat"
    if %VS_FOUND% equ 1 (
        echo ✓ Visual Studio detection completed successfully
        goto :vs_detection_complete
    ) else (
        echo ✗ Visual Studio detection failed
        goto :vs_detection_failed
    )
) else (
    echo ⚠ detect_vs.bat not found, using fallback detection...
    call :FallbackVSDetection
)

:vs_detection_complete
echo ✓ Using Visual Studio %VS_EDITION% at: %VS_PATH%
echo   VSIXInstaller: %VSIX_INSTALLER%
goto :vs_check_complete

:vs_detection_failed
echo.
echo ✗ ERROR: Visual Studio 2022 not found.
echo.
echo Please ensure Visual Studio 2022 is installed with the following:
echo   - Any edition: Community, Professional, or Enterprise
echo   - Visual Studio extension development workload
echo   - .NET Framework 4.7.2 or later
echo.
echo Download from: https://visualstudio.microsoft.com/downloads/
echo.
set /p continue="Continue installation without extension? (y/N): "
if /i "!continue!" neq "y" (
    echo Installation cancelled.
    pause
    exit /b 1
)
echo ⚠ Continuing without Visual Studio extension installation
set "SKIP_EXTENSION=1"
goto :vs_check_complete

:FallbackVSDetection
:: Fallback detection if detect_vs.bat is not available
set "VS_FOUND=0"
set "VS_PATH="
set "VS_EDITION="
set "VSIX_INSTALLER="

:: Try vswhere.exe first
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" (
    for /f "tokens=*" %%i in ('"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -version [17.0,18.0) -property installationPath -latest 2^>nul') do (
        if exist "%%i\Common7\IDE\devenv.exe" (
            set "VS_PATH=%%i"
            set "VS_FOUND=1"
            set "VSIX_INSTALLER=%%i\Common7\IDE\VSIXInstaller.exe"
            :: Extract edition from path
            echo %%i | findstr /i "Enterprise" >nul && set "VS_EDITION=Enterprise"
            echo %%i | findstr /i "Professional" >nul && set "VS_EDITION=Professional"
            echo %%i | findstr /i "Community" >nul && set "VS_EDITION=Community"
            if "!VS_EDITION!"=="" set "VS_EDITION=Unknown"
            goto :eof
        )
    )
)

:: Fallback to standard paths
if %VS_FOUND% equ 0 (
    for %%B in (
        "%ProgramFiles%\Microsoft Visual Studio\2022"
        "%ProgramFiles(x86)%\Microsoft Visual Studio\2022"
    ) do (
        for %%E in (Enterprise Professional Community) do (
            if exist "%%~B\%%E\Common7\IDE\devenv.exe" (
                set "VS_PATH=%%~B\%%E"
                set "VS_EDITION=%%E"
                set "VS_FOUND=1"
                set "VSIX_INSTALLER=%%~B\%%E\Common7\IDE\VSIXInstaller.exe"
                goto :eof
            )
        )
    )
)
goto :eof

:vs_check_complete

echo.
echo Step 2: Installing A3sist API Service...
echo ==========================================

:: Stop existing service if running
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorlevel% equ 0 (
    echo Stopping existing A3sist API service...
    sc stop "%SERVICE_NAME%" >nul 2>&1
    timeout /t 5 /nobreak >nul
    sc delete "%SERVICE_NAME%" >nul 2>&1
)

:: Create installation directory
if not exist "%INSTALL_DIR%" (
    mkdir "%INSTALL_DIR%"
)
if not exist "%API_DIR%" (
    mkdir "%API_DIR%"
)

:: Copy API files
echo Copying API files...
xcopy /E /I /Y "A3sist.API\bin\Release\net9.0\*" "%API_DIR%\" >nul
if %errorlevel% neq 0 (
    echo ERROR: Failed to copy API files. Make sure the API is built in Release mode.
    pause
    exit /b 1
)
echo ✓ API files copied to %API_DIR%

:: Create service configuration
echo Creating service configuration...
(
echo ^<?xml version="1.0" encoding="utf-8"?^>
echo ^<configuration^>
echo   ^<appSettings^>
echo     ^<add key="ServiceName" value="%SERVICE_NAME%" /^>
echo     ^<add key="ServiceDisplayName" value="%SERVICE_DISPLAY%" /^>
echo     ^<add key="ServiceDescription" value="A3sist AI Assistant API Service" /^>
echo     ^<add key="ApiPort" value="%API_PORT%" /^>
echo   ^</appSettings^>
echo ^</configuration^>
) > "%API_DIR%\service.config"

:: Create service wrapper script
(
echo @echo off
echo cd /d "%API_DIR%"
echo dotnet A3sist.API.dll --urls "http://localhost:%API_PORT%"
) > "%API_DIR%\start-api.bat"

:: Install as Windows service using sc command
echo Installing Windows service...
sc create "%SERVICE_NAME%" binPath= "cmd.exe /c \"%API_DIR%\start-api.bat\"" DisplayName= "%SERVICE_DISPLAY%" start= auto >nul
if %errorlevel% neq 0 (
    echo ERROR: Failed to create Windows service.
    pause
    exit /b 1
)

:: Configure service to restart on failure
sc failure "%SERVICE_NAME%" reset= 86400 actions= restart/5000/restart/5000/restart/5000 >nul

:: Start the service
echo Starting A3sist API service...
sc start "%SERVICE_NAME%" >nul
if %errorlevel% neq 0 (
    echo WARNING: Service created but failed to start. You may need to start it manually.
) else (
    echo ✓ A3sist API service started successfully
)

:: Wait a moment for service to start
timeout /t 3 /nobreak >nul

:: Test API endpoint
echo Testing API endpoint...
curl -s http://localhost:%API_PORT%/api/health >nul 2>&1
if %errorlevel% equ 0 (
    echo ✓ API service is responding
) else (
    echo WARNING: API service may not be responding yet
)

echo.
echo Step 3: Installing Visual Studio Extension...
echo ==========================================

if defined SKIP_EXTENSION (
    echo Skipping Visual Studio extension installation as requested.
    goto :skip_extension
)

echo ✓ Using Visual Studio %VS_EDITION% at: %VS_PATH%

:: Validate VSIX file exists and get additional info
set "VSIX_PATH=A3sist.UI\bin\Release\A3sist.UI.vsix"
if not exist "%VSIX_PATH%" (
    echo ✗ ERROR: A3sist.UI.vsix not found at %VSIX_PATH%
    echo.
    echo Please build the A3sist.UI project in Release mode first:
    echo   1. Open A3sist.sln in Visual Studio
    echo   2. Set configuration to Release
    echo   3. Build the A3sist.UI project
    echo   4. Or run: msbuild A3sist.UI\A3sist.UI.csproj /p:Configuration=Release
    echo.
    pause
    exit /b 1
)

:: Get VSIX file info
for %%F in ("%VSIX_PATH%") do (
    echo ✓ VSIX file found: %%~nxF
    echo   Size: %%~zF bytes
    echo   Date: %%~tF
)

:: Install VSIX extension
echo Installing Visual Studio extension...
echo Command: "%VSIX_INSTALLER%" /quiet "%VSIX_PATH%"

"%VSIX_INSTALLER%" /quiet "%VSIX_PATH%"
set "VSIX_EXIT_CODE=%errorlevel%"

if %VSIX_EXIT_CODE% equ 0 (
    echo ✓ Visual Studio extension installed successfully
) else if %VSIX_EXIT_CODE% equ 1001 (
    echo ✓ Extension already installed (updated)
) else if %VSIX_EXIT_CODE% equ 2003 (
    echo ⚠ Extension installed but requires Visual Studio restart
) else (
    echo ⚠ Extension installation returned exit code: %VSIX_EXIT_CODE%
    echo.
    echo Common exit codes:
    echo   1001 - Extension already installed
    echo   2003 - Installation succeeded but requires restart
    echo   2005 - Visual Studio is running (close VS and retry)
    echo   3000 - Invalid VSIX package
    echo.
    echo You can manually install by:
    echo   1. Double-clicking: %VSIX_PATH%
    echo   2. Or using Extensions ^> Manage Extensions in Visual Studio
)

:skip_extension

echo.
echo Step 4: Creating configuration...
echo ==========================================

:: Create user configuration directory
set "CONFIG_DIR=%APPDATA%\A3sist"
if not exist "%CONFIG_DIR%" (
    mkdir "%CONFIG_DIR%"
)

:: Create default configuration
(
echo {
echo   "ApiUrl": "http://localhost:%API_PORT%",
echo   "AutoStartApi": true,
echo   "AutoCompleteEnabled": true,
echo   "RequestTimeout": 30,
echo   "EnableLogging": true,
echo   "StreamResponses": true,
echo   "RealTimeAnalysis": true,
echo   "ShowSuggestions": true,
echo   "ShowCodeIssues": true,
echo   "AutoStartAgent": false,
echo   "BackgroundAnalysis": true,
echo   "AnalysisInterval": 30,
echo   "AutoIndexWorkspace": true,
echo   "IndexDependencies": false,
echo   "MaxSearchResults": 10,
echo   "MaxSuggestions": 10
echo }
) > "%CONFIG_DIR%\config.json"

echo ✓ Default configuration created at %CONFIG_DIR%\config.json

echo.
echo Step 5: Creating shortcuts and utilities...
echo ==========================================

:: Create desktop shortcuts
set "DESKTOP=%USERPROFILE%\Desktop"

:: API management shortcut
(
echo @echo off
echo echo A3sist API Service Management
echo echo =============================
echo echo 1. Start Service
echo echo 2. Stop Service  
echo echo 3. Restart Service
echo echo 4. Check Status
echo echo 5. View Logs
echo echo 6. Exit
echo echo.
echo set /p choice="Choose an option (1-6): "
echo if "%%choice%%"=="1" sc start "%SERVICE_NAME%"
echo if "%%choice%%"=="2" sc stop "%SERVICE_NAME%"
echo if "%%choice%%"=="3" (sc stop "%SERVICE_NAME%" ^& timeout /t 5 /nobreak ^>nul ^& sc start "%SERVICE_NAME%")
echo if "%%choice%%"=="4" sc query "%SERVICE_NAME%"
echo if "%%choice%%"=="5" start http://localhost:%API_PORT%/swagger
echo if "%%choice%%"=="6" exit
echo pause
) > "%DESKTOP%\A3sist API Manager.bat"

:: Configuration shortcut
(
echo @echo off
echo start notepad "%CONFIG_DIR%\config.json"
) > "%DESKTOP%\A3sist Configuration.bat"

echo ✓ Desktop shortcuts created

:: Create uninstaller
(
echo @echo off
echo echo A3sist Uninstaller
echo echo ==================
echo echo This will remove A3sist from your system.
echo set /p confirm="Are you sure? (y/N): "
echo if /i "%%confirm%%" neq "y" exit /b 0
echo.
echo echo Stopping and removing service...
echo sc stop "%SERVICE_NAME%" ^>nul 2^>^&1
echo sc delete "%SERVICE_NAME%" ^>nul 2^>^&1
echo.
echo echo Removing files...
echo rmdir /s /q "%INSTALL_DIR%" ^>nul 2^>^&1
echo rmdir /s /q "%CONFIG_DIR%" ^>nul 2^>^&1
echo.
echo echo Removing shortcuts...
echo del "%DESKTOP%\A3sist API Manager.bat" ^>nul 2^>^&1
echo del "%DESKTOP%\A3sist Configuration.bat" ^>nul 2^>^&1
echo del "%DESKTOP%\Uninstall A3sist.bat" ^>nul 2^>^&1
echo.
echo echo A3sist has been removed from your system.
echo echo You may need to manually uninstall the Visual Studio extension.
echo pause
) > "%DESKTOP%\Uninstall A3sist.bat"

echo.
echo Step 6: Configuring Windows Firewall...
echo ==========================================

:: Add firewall rule for API port
netsh advfirewall firewall show rule name="A3sist API" >nul 2>&1
if %errorlevel% neq 0 (
    echo Adding firewall rule for port %API_PORT%...
    netsh advfirewall firewall add rule name="A3sist API" dir=in action=allow protocol=TCP localport=%API_PORT% >nul
    if %errorlevel% equ 0 (
        echo ✓ Firewall rule added for port %API_PORT%
    ) else (
        echo WARNING: Failed to add firewall rule. You may need to manually allow port %API_PORT%
    )
) else (
    echo ✓ Firewall rule already exists
)

echo.
echo ==========================================
echo Installation Complete!
echo ==========================================
echo.
echo ✓ A3sist API service installed and running on port %API_PORT%
if not defined SKIP_EXTENSION (
    echo ✓ Visual Studio extension installed
) else (
    echo ⚠ Visual Studio extension installation was skipped
)
echo ✓ Configuration files created
echo ✓ Desktop shortcuts created
echo.
echo Next Steps:
if not defined SKIP_EXTENSION (
    echo 1. Restart Visual Studio 2022
    echo 2. Go to View → Other Windows → A3sist Assistant
    echo 3. The extension should automatically connect to the API
) else (
    echo 1. Install Visual Studio 2022 if not already installed
    echo 2. Manually install the extension: %VSIX_PATH%
    echo 3. Restart Visual Studio 2022
    echo 4. Go to View → Other Windows → A3sist Assistant
)
echo.
echo Management Tools:
echo • A3sist API Manager.bat - Manage the API service
echo • A3sist Configuration.bat - Edit configuration
echo • Uninstall A3sist.bat - Remove A3sist from system
echo.
echo API Endpoints:
echo • Health Check: http://localhost:%API_PORT%/api/health
echo • Swagger UI: http://localhost:%API_PORT%/swagger
echo.
echo For help and documentation, see:
echo • BUILD_AND_DEPLOYMENT.md
echo • TESTING_PLAN.md  
echo • README.md
echo • Visual Studio troubleshooting: HOW_TO_FIND_SIDEBAR.md
echo.
echo Enjoy using A3sist AI Assistant!
echo.
pause