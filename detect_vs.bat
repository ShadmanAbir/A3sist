@echo off
setlocal enabledelayedexpansion

:: =====================================================
:: Visual Studio 2022 Detection Utility (Batch Version)
:: =====================================================
:: This script detects Visual Studio 2022 installations using multiple methods:
:: 1. vswhere.exe (Microsoft's official detection tool)
:: 2. Standard installation paths
:: 3. Registry entries
:: 4. Environment variables

:: Initialize variables
set "VS_FOUND=0"
set "VS_PATH="
set "VS_EDITION="
set "VS_VERSION="
set "VSIX_INSTALLER="
set "DETECTION_METHOD="

echo Searching for Visual Studio 2022 installations...
echo.

:: Method 1: Use vswhere.exe (most reliable)
call :DetectUsingVswhere
if %VS_FOUND% equ 1 goto :ValidationAndOutput

:: Method 2: Check standard installation paths
call :DetectUsingStandardPaths
if %VS_FOUND% equ 1 goto :ValidationAndOutput

:: Method 3: Check registry
call :DetectUsingRegistry
if %VS_FOUND% equ 1 goto :ValidationAndOutput

:: Method 4: Check environment variables
call :DetectUsingEnvironment
if %VS_FOUND% equ 1 goto :ValidationAndOutput

:: Method 5: Search common custom locations
call :DetectUsingCustomPaths
if %VS_FOUND% equ 1 goto :ValidationAndOutput

goto :NoVSFound

:DetectUsingVswhere
echo [Method 1] Trying vswhere.exe...
set "DETECTION_METHOD=vswhere.exe"

:: Try standard vswhere.exe locations
for %%P in (
    "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
    "%ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe"
) do (
    if exist "%%~P" (
        echo Found vswhere.exe at: %%~P
        
        :: Get VS 2022 installations with detailed info
        for /f "tokens=*" %%i in ('%%~P -version [17.0,18.0) -property installationPath displayName installationVersion -format value 2^>nul') do (
            set "line=%%i"
            if "!line:~0,16!"=="installationPath:" (
                set "TEMP_PATH=!line:~17!"
                if exist "!TEMP_PATH!\Common7\IDE\devenv.exe" (
                    set "VS_PATH=!TEMP_PATH!"
                    set "VS_FOUND=1"
                )
            )
            if "!line:~0,12!"=="displayName:" (
                set "TEMP_DISPLAY=!line:~13!"
                :: Extract edition from display name
                echo !TEMP_DISPLAY! | findstr /i "Enterprise" >nul && set "VS_EDITION=Enterprise"
                echo !TEMP_DISPLAY! | findstr /i "Professional" >nul && set "VS_EDITION=Professional"
                echo !TEMP_DISPLAY! | findstr /i "Community" >nul && set "VS_EDITION=Community"
                if "!VS_EDITION!"=="" set "VS_EDITION=Unknown"
            )
            if "!line:~0,19!"=="installationVersion:" (
                set "VS_VERSION=!line:~20!"
            )
        )
        
        if !VS_FOUND! equ 1 (
            echo ✓ Found via vswhere: !VS_EDITION! !VS_VERSION! at !VS_PATH!
            goto :eof
        )
    )
)

echo   vswhere.exe not found or no results
goto :eof

:DetectUsingStandardPaths
echo [Method 2] Checking standard installation paths...
set "DETECTION_METHOD=Standard Path"

:: Check both Program Files locations
for %%B in (
    "%ProgramFiles%\Microsoft Visual Studio\2022"
    "%ProgramFiles(x86)%\Microsoft Visual Studio\2022"
) do (
    if exist "%%~B" (
        echo Checking: %%~B
        
        :: Check each edition in priority order
        for %%E in (Enterprise Professional Community) do (
            set "TEST_PATH=%%~B\%%E"
            if exist "!TEST_PATH!\Common7\IDE\devenv.exe" (
                set "VS_PATH=!TEST_PATH!"
                set "VS_EDITION=%%E"
                set "VS_FOUND=1"
                
                :: Try to get version from devenv.exe
                for /f "tokens=*" %%V in ('powershell -NoProfile -Command "(Get-Item \"!TEST_PATH!\Common7\IDE\devenv.exe\").VersionInfo.ProductVersion" 2^>nul') do (
                    set "VS_VERSION=%%V"
                )
                if "!VS_VERSION!"=="" set "VS_VERSION=17.0"
                
                echo ✓ Found: %%E at !TEST_PATH!
                goto :eof
            )
        )
    )
)

echo   No installations found in standard paths
goto :eof

:DetectUsingRegistry
echo [Method 3] Checking registry...
set "DETECTION_METHOD=Registry"

:: Check various registry locations
for %%K in (
    "HKLM\SOFTWARE\Microsoft\VisualStudio\Setup\Reboot"
    "HKLM\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\Setup\Reboot"
) do (
    for /f "tokens=2*" %%R in ('reg query "%%K" /s /v InstallDir 2^>nul ^| findstr InstallDir 2^>nul') do (
        set "REG_PATH=%%S"
        if exist "!REG_PATH!\devenv.exe" (
            :: Navigate to VS root (InstallDir points to Common7\IDE)
            for %%P in ("!REG_PATH!\..\..") do set "VS_PATH=%%~fP"
            
            :: Determine edition from path
            echo !VS_PATH! | findstr /i "Enterprise" >nul && set "VS_EDITION=Enterprise"
            echo !VS_PATH! | findstr /i "Professional" >nul && set "VS_EDITION=Professional"
            echo !VS_PATH! | findstr /i "Community" >nul && set "VS_EDITION=Community"
            if "!VS_EDITION!"=="" set "VS_EDITION=Unknown"
            
            set "VS_VERSION=17.0"
            set "VS_FOUND=1"
            echo ✓ Found via registry: !VS_EDITION! at !VS_PATH!
            goto :eof
        )
    )
)

echo   No installations found in registry
goto :eof

:DetectUsingEnvironment
echo [Method 4] Checking environment variables...
set "DETECTION_METHOD=Environment Variable"

:: Check common environment variables
if defined VSINSTALLDIR (
    if exist "%VSINSTALLDIR%\Common7\IDE\devenv.exe" (
        set "VS_PATH=%VSINSTALLDIR%"
        set "VS_FOUND=1"
        set "VS_EDITION=Unknown"
        set "VS_VERSION=17.0"
        echo ✓ Found via VSINSTALLDIR: %VSINSTALLDIR%
        goto :eof
    )
)

if defined VS170COMNTOOLS (
    for %%P in ("%VS170COMNTOOLS%\..\..\..") do set "TEST_PATH=%%~fP"
    if exist "!TEST_PATH!\Common7\IDE\devenv.exe" (
        set "VS_PATH=!TEST_PATH!"
        set "VS_FOUND=1"
        set "VS_EDITION=Unknown"
        set "VS_VERSION=17.0"
        echo ✓ Found via VS170COMNTOOLS: !TEST_PATH!
        goto :eof
    )
)

echo   No installations found via environment variables
goto :eof

:DetectUsingCustomPaths
echo [Method 5] Searching custom installation locations...
set "DETECTION_METHOD=Custom Path"

:: Search common drives and custom paths
for %%D in (C D E F G) do (
    if exist "%%D:\" (
        for %%P in (
            "%%D:\VS"
            "%%D:\VisualStudio"
            "%%D:\Microsoft Visual Studio"
            "%%D:\Visual Studio 2022"
        ) do (
            if exist "%%P" (
                echo Checking custom path: %%P
                for %%E in (Enterprise Professional Community) do (
                    for %%S in (
                        "%%P\2022\%%E"
                        "%%P\%%E"
                        "%%P\2022\%%E"
                    ) do (
                        if exist "%%S\Common7\IDE\devenv.exe" (
                            set "VS_PATH=%%S"
                            set "VS_EDITION=%%E"
                            set "VS_FOUND=1"
                            set "VS_VERSION=17.0"
                            echo ✓ Found at custom location: %%S
                            goto :eof
                        )
                    )
                )
            )
        )
    )
)

echo   No installations found in custom locations
goto :eof

:ValidationAndOutput
echo.
echo ===== VALIDATION =====

:: Validate the found installation
if not exist "%VS_PATH%\Common7\IDE\devenv.exe" (
    echo ✗ ERROR: devenv.exe not found at %VS_PATH%\Common7\IDE\devenv.exe
    set "VS_FOUND=0"
    goto :NoVSFound
)

set "VSIX_INSTALLER=%VS_PATH%\Common7\IDE\VSIXInstaller.exe"
if not exist "%VSIX_INSTALLER%" (
    echo ✗ WARNING: VSIXInstaller.exe not found at %VSIX_INSTALLER%
    echo   Extension installation may not be possible
) else (
    echo ✓ VSIXInstaller.exe found
)

:: Check for VS SDK (optional but recommended for extension development)
if exist "%VS_PATH%\VSSDK" (
    echo ✓ Visual Studio SDK found
) else (
    echo ⚠ Visual Studio SDK not found (extension development may be limited)
)

echo.
echo ===== DETECTION RESULTS =====
echo ✓ Visual Studio 2022 Found
echo   Edition: %VS_EDITION%
echo   Version: %VS_VERSION%
echo   Path: %VS_PATH%
echo   Detection Method: %DETECTION_METHOD%
echo   VSIXInstaller: %VSIX_INSTALLER%
echo.

:: Set environment variables for other scripts to use
endlocal & (
    set "VS_FOUND=1"
    set "VS_PATH=%VS_PATH%"
    set "VS_EDITION=%VS_EDITION%"
    set "VS_VERSION=%VS_VERSION%"
    set "VSIX_INSTALLER=%VSIX_INSTALLER%"
    set "DETECTION_METHOD=%DETECTION_METHOD%"
)

goto :eof

:NoVSFound
echo.
echo ===== NO VISUAL STUDIO FOUND =====
echo ✗ Visual Studio 2022 not detected
echo.
echo Please ensure Visual Studio 2022 is installed with:
echo • Any edition: Community, Professional, or Enterprise
echo • Visual Studio extension development workload
echo • .NET Framework 4.7.2 or later
echo.
echo Download from: https://visualstudio.microsoft.com/downloads/
echo.
echo If Visual Studio is installed in a custom location, please ensure:
echo • devenv.exe exists in Common7\IDE subdirectory
echo • VSIXInstaller.exe exists in Common7\IDE subdirectory
echo.

:: Set environment variables to indicate failure
endlocal & (
    set "VS_FOUND=0"
    set "VS_PATH="
    set "VS_EDITION="
    set "VS_VERSION="
    set "VSIX_INSTALLER="
    set "DETECTION_METHOD="
)

exit /b 1