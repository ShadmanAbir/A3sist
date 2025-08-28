#Requires -RunAsAdministrator
<#
.SYNOPSIS
    A3sist AI Assistant Installer
.DESCRIPTION
    Installs the A3sist API as a Windows service and the Visual Studio extension
.NOTES
    Requires Administrator privileges
#>

param(
    [string]$InstallPath = "C:\Program Files\A3sist",
    [int]$ApiPort = 8341,
    [switch]$SkipFirewall,
    [switch]$SkipService,
    [switch]$SkipExtension
)

# Color output functions
function Write-Success { param($Message) Write-Host "✓ $Message" -ForegroundColor Green }
function Write-Warning { param($Message) Write-Host "⚠ $Message" -ForegroundColor Yellow }
function Write-Error { param($Message) Write-Host "✗ $Message" -ForegroundColor Red }
function Write-Info { param($Message) Write-Host "ℹ $Message" -ForegroundColor Cyan }
function Write-Step { param($Step, $Message) Write-Host "`n$Step $Message" -ForegroundColor Magenta; Write-Host ("=" * 50) -ForegroundColor Gray }

# Global variables
$ServiceName = "A3sistAPI"
$ServiceDisplayName = "A3sist API Service"
$ApiDir = Join-Path $InstallPath "API"
$ConfigDir = Join-Path $env:APPDATA "A3sist"

function Test-Prerequisites {
    Write-Step "Step 1:" "Checking prerequisites..."
    
    # Check .NET 9
    try {
        $dotnetVersion = & dotnet --version 2>$null
        if ($dotnetVersion -match "^9\.") {
            Write-Success ".NET 9 runtime found (version $dotnetVersion)"
        } else {
            Write-Error ".NET 9 runtime is required but not found."
            Write-Info "Please install .NET 9 runtime from: https://dotnet.microsoft.com/download/dotnet/9.0"
            return $false
        }
    } catch {
        Write-Error ".NET runtime not found or not accessible."
        return $false
    }
    
    # Check Visual Studio 2022
    $vsPath = Get-VisualStudioPath
    if ($vsPath) {
        Write-Success "Visual Studio 2022 found at: $vsPath"
        
        # Validate required components
        $vsixInstaller = Join-Path $vsPath "Common7\IDE\VSIXInstaller.exe"
        if (Test-Path $vsixInstaller) {
            Write-Success "VSIXInstaller.exe found - extension installation supported"
        } else {
            Write-Warning "VSIXInstaller.exe not found - extension installation may fail"
        }
    } else {
        Write-Warning "Visual Studio 2022 not found"
        Write-Info "Please ensure Visual Studio 2022 is installed with:"
        Write-Info "  - Any edition: Community, Professional, or Enterprise"
        Write-Info "  - Visual Studio extension development workload"
        Write-Info "  - .NET Framework 4.7.2 or later"
        Write-Info "Download from: https://visualstudio.microsoft.com/downloads/"
        
        if (-not $SkipExtension) {
            $continue = Read-Host "Continue without extension installation? (y/N)"
            if ($continue -ne 'y' -and $continue -ne 'Y') {
                return $false
            }
            $script:SkipExtension = $true
            Write-Warning "Extension installation will be skipped"
        }
    }
    
    # Check if API is built
    $apiPath = "A3sist.API\bin\Release\net9.0"
    if (-not (Test-Path $apiPath)) {
        Write-Error "API binaries not found at $apiPath"
        Write-Info "Please build the A3sist.API project in Release mode first:"
        Write-Info "  cd A3sist.API"
        Write-Info "  dotnet build --configuration Release"
        return $false
    }
    Write-Success "API binaries found"
    
    # Check if extension is built (if not skipping)
    if (-not $SkipExtension) {
        $vsixPath = "A3sist.UI\bin\Release\A3sist.UI.vsix"
        if (-not (Test-Path $vsixPath)) {
            Write-Error "Extension VSIX not found at $vsixPath"
            Write-Info "Please build the A3sist.UI project in Release mode first:"
            Write-Info "  Method 1: Open A3sist.sln in Visual Studio and build in Release mode"
            Write-Info "  Method 2: Run 'msbuild A3sist.UI\A3sist.UI.csproj /p:Configuration=Release'"
            Write-Info "  Method 3: Run 'dotnet build A3sist.UI /c Release' (if using .NET SDK)"
            return $false
        }
        
        # Get VSIX file info
        $vsixInfo = Get-Item $vsixPath
        Write-Success "Extension VSIX found: $($vsixInfo.Name)"
        Write-Info "  Size: $([math]::Round($vsixInfo.Length / 1KB, 2)) KB"
        Write-Info "  Date: $($vsixInfo.LastWriteTime)"
    }
    
    return $true
}

function Get-VisualStudioPath {
    Write-Info "Searching for Visual Studio 2022 installations..."
    
    # Method 1: Use vswhere.exe (most reliable)
    $vswherePaths = @(
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\Installer\vswhere.exe"
    )
    
    foreach ($vswherePath in $vswherePaths) {
        if (Test-Path $vswherePath) {
            Write-Info "Using vswhere.exe to detect Visual Studio installations"
            try {
                $installations = & $vswherePath -version "[17.0,18.0)" -requires "Microsoft.VisualStudio.Component.VSSDK" -format json | ConvertFrom-Json
                
                if ($installations) {
                    if ($installations -is [array]) {
                        $installation = $installations | Sort-Object installationVersion -Descending | Select-Object -First 1
                    } else {
                        $installation = $installations
                    }
                    
                    $vsPath = $installation.installationPath
                    $displayName = $installation.displayName
                    $version = $installation.installationVersion
                    
                    if (Test-Path "$vsPath\Common7\IDE\devenv.exe") {
                        Write-Success "Found $displayName (v$version) at: $vsPath"
                        return $vsPath
                    }
                }
            } catch {
                Write-Warning "vswhere.exe failed: $($_.Exception.Message)"
            }
        }
    }
    
    # Method 2: Check standard installation paths
    Write-Info "Checking standard installation paths..."
    $editions = @('Enterprise', 'Professional', 'Community')
    $basePaths = @(
        "${env:ProgramFiles}\Microsoft Visual Studio\2022",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022"
    )
    
    foreach ($basePath in $basePaths) {
        foreach ($edition in $editions) {
            $vsPath = Join-Path $basePath $edition
            $devenvPath = Join-Path $vsPath "Common7\IDE\devenv.exe"
            $vsixInstallerPath = Join-Path $vsPath "Common7\IDE\VSIXInstaller.exe"
            
            if ((Test-Path $devenvPath) -and (Test-Path $vsixInstallerPath)) {
                # Get version info
                try {
                    $versionInfo = (Get-Item $devenvPath).VersionInfo
                    $version = $versionInfo.ProductVersion
                    Write-Success "Found Visual Studio $edition 2022 (v$version) at: $vsPath"
                    return $vsPath
                } catch {
                    Write-Success "Found Visual Studio $edition 2022 at: $vsPath"
                    return $vsPath
                }
            }
        }
    }
    
    # Method 3: Check registry
    Write-Info "Checking registry for Visual Studio installations..."
    try {
        $regKeys = @(
            "HKLM:\SOFTWARE\Microsoft\VisualStudio\Setup\Reboot",
            "HKLM:\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\Setup\Reboot"
        )
        
        foreach ($regKey in $regKeys) {
            if (Test-Path $regKey) {
                $subKeys = Get-ChildItem $regKey -ErrorAction SilentlyContinue
                foreach ($subKey in $subKeys) {
                    $installDir = (Get-ItemProperty $subKey.PSPath -Name "InstallDir" -ErrorAction SilentlyContinue).InstallDir
                    if ($installDir -and (Test-Path "$installDir\devenv.exe")) {
                        $vsPath = Split-Path (Split-Path $installDir) -Parent
                        if (Test-Path "$vsPath\Common7\IDE\VSIXInstaller.exe") {
                            Write-Success "Found Visual Studio 2022 via registry at: $vsPath"
                            return $vsPath
                        }
                    }
                }
            }
        }
    } catch {
        Write-Warning "Registry search failed: $($_.Exception.Message)"
    }
    
    # Method 4: Check for any VS 2022 installation in common locations
    Write-Info "Performing final scan for any Visual Studio 2022 installation..."
    $searchPaths = @(
        "${env:ProgramFiles}\Microsoft Visual Studio",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio",
        "C:\VS", "D:\VS", "E:\VS"  # Sometimes installed in custom locations
    )
    
    foreach ($searchPath in $searchPaths) {
        if (Test-Path $searchPath) {
            $vs2022Dirs = Get-ChildItem $searchPath -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -like "*2022*" }
            foreach ($vs2022Dir in $vs2022Dirs) {
                $possibleEditions = Get-ChildItem $vs2022Dir.FullName -Directory -ErrorAction SilentlyContinue
                foreach ($edition in $possibleEditions) {
                    $devenvPath = Join-Path $edition.FullName "Common7\IDE\devenv.exe"
                    if (Test-Path $devenvPath) {
                        Write-Success "Found Visual Studio 2022 at: $($edition.FullName)"
                        return $edition.FullName
                    }
                }
            }
        }
    }
    
    Write-Warning "No Visual Studio 2022 installation found"
    return $null
}

function Install-ApiService {
    if ($SkipService) {
        Write-Info "Skipping service installation"
        return $true
    }
    
    Write-Step "Step 2:" "Installing A3sist API Service..."
    
    try {
        # Stop existing service if running
        $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if ($existingService) {
            Write-Info "Stopping existing service..."
            Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 3
            
            # Remove existing service
            $service = Get-WmiObject -Class Win32_Service -Filter "Name='$ServiceName'"
            if ($service) {
                $service.Delete() | Out-Null
            }
        }
        
        # Create installation directory
        if (-not (Test-Path $InstallPath)) {
            New-Item -Path $InstallPath -ItemType Directory -Force | Out-Null
        }
        if (-not (Test-Path $ApiDir)) {
            New-Item -Path $ApiDir -ItemType Directory -Force | Out-Null
        }
        
        # Copy API files
        Write-Info "Copying API files..."
        $sourceDir = "A3sist.API\bin\Release\net9.0\*"
        Copy-Item -Path $sourceDir -Destination $ApiDir -Recurse -Force
        Write-Success "API files copied to $ApiDir"
        
        # Create service executable wrapper using NSSM (if available) or sc
        $serviceBinary = """$ApiDir\A3sist.API.exe"""
        $serviceArgs = "--urls `"http://localhost:$ApiPort`""
        
        # Try to use NSSM if available, otherwise use sc
        $nssmPath = Get-Command nssm -ErrorAction SilentlyContinue
        if ($nssmPath) {
            Write-Info "Using NSSM to create service..."
            & nssm install $ServiceName $serviceBinary $serviceArgs
            & nssm set $ServiceName DisplayName $ServiceDisplayName
            & nssm set $ServiceName Description "A3sist AI Assistant API Service providing AI-powered development assistance"
            & nssm set $ServiceName Start SERVICE_AUTO_START
        } else {
            Write-Info "Creating service with sc command..."
            $servicePath = "cmd.exe /c `"cd /d `"$ApiDir`" && dotnet A3sist.API.dll --urls `"http://localhost:$ApiPort`"`""
            & sc.exe create $ServiceName binPath= $servicePath DisplayName= $ServiceDisplayName start= auto
        }
        
        # Configure service recovery
        & sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/5000/restart/5000
        
        # Start the service
        Write-Info "Starting A3sist API service..."
        Start-Service -Name $ServiceName
        Start-Sleep -Seconds 5
        
        # Test service
        $service = Get-Service -Name $ServiceName
        if ($service.Status -eq 'Running') {
            Write-Success "A3sist API service started successfully"
            
            # Test API endpoint
            try {
                $response = Invoke-RestMethod -Uri "http://localhost:$ApiPort/api/health" -TimeoutSec 10
                Write-Success "API service is responding"
            } catch {
                Write-Warning "API service started but may not be responding yet"
            }
        } else {
            Write-Warning "Service created but failed to start"
        }
        
        return $true
    } catch {
        Write-Error "Failed to install API service: $($_.Exception.Message)"
        return $false
    }
}

function Install-VisualStudioExtension {
    if ($SkipExtension) {
        Write-Info "Skipping extension installation"
        return $true
    }
    
    Write-Step "Step 3:" "Installing Visual Studio Extension..."
    
    try {
        $vsPath = Get-VisualStudioPath
        if (-not $vsPath) {
            Write-Error "Visual Studio 2022 not found"
            return $false
        }
        
        $vsixInstaller = Join-Path $vsPath "Common7\IDE\VSIXInstaller.exe"
        $vsixPath = Resolve-Path "A3sist.UI\bin\Release\A3sist.UI.vsix"
        
        Write-Info "Installing extension using VSIXInstaller..."
        Write-Info "Command: `"$vsixInstaller`" /quiet `"$vsixPath`""
        
        $process = Start-Process -FilePath $vsixInstaller -ArgumentList "/quiet", "`"$vsixPath`"" -Wait -PassThru
        
        $exitCode = $process.ExitCode
        
        switch ($exitCode) {
            0 { 
                Write-Success "Visual Studio extension installed successfully"
            }
            1001 { 
                Write-Success "Extension already installed (updated successfully)"
            }
            2003 { 
                Write-Success "Extension installed successfully (restart Visual Studio required)"
            }
            2005 { 
                Write-Warning "Visual Studio is currently running - please close Visual Studio and retry"
                Write-Info "You can manually install by double-clicking: $vsixPath"
            }
            3000 { 
                Write-Error "Invalid VSIX package"
                Write-Info "Please rebuild the A3sist.UI project and try again"
            }
            default { 
                Write-Warning "Extension installation returned exit code: $exitCode"
                Write-Info "Common exit codes:"
                Write-Info "  1001 - Extension already installed"
                Write-Info "  2003 - Installation succeeded but requires restart"
                Write-Info "  2005 - Visual Studio is running (close VS and retry)"
                Write-Info "  3000 - Invalid VSIX package"
                Write-Info "You can manually install by:"
                Write-Info "  1. Double-clicking: $vsixPath"
                Write-Info "  2. Using Extensions > Manage Extensions in Visual Studio"
            }
        }
        
        return $true
    } catch {
        Write-Error "Failed to install extension: $($_.Exception.Message)"
        return $false
    }
}

function Create-Configuration {
    Write-Step "Step 4:" "Creating configuration..."
    
    try {
        # Create user configuration directory
        if (-not (Test-Path $ConfigDir)) {
            New-Item -Path $ConfigDir -ItemType Directory -Force | Out-Null
        }
        
        # Create default configuration
        $config = @{
            ApiUrl = "http://localhost:$ApiPort"
            AutoStartApi = $true
            AutoCompleteEnabled = $true
            RequestTimeout = 30
            EnableLogging = $true
            StreamResponses = $true
            RealTimeAnalysis = $true
            ShowSuggestions = $true
            ShowCodeIssues = $true
            AutoStartAgent = $false
            BackgroundAnalysis = $true
            AnalysisInterval = 30
            AutoIndexWorkspace = $true
            IndexDependencies = $false
            MaxSearchResults = 10
            MaxSuggestions = 10
        }
        
        $configPath = Join-Path $ConfigDir "config.json"
        $config | ConvertTo-Json -Depth 10 | Set-Content -Path $configPath -Encoding UTF8
        
        Write-Success "Default configuration created at $configPath"
        return $true
    } catch {
        Write-Error "Failed to create configuration: $($_.Exception.Message)"
        return $false
    }
}

function Create-Shortcuts {
    Write-Step "Step 5:" "Creating shortcuts and utilities..."
    
    try {
        $desktop = [Environment]::GetFolderPath("Desktop")
        
        # API Manager script
        $apiManagerPath = Join-Path $desktop "A3sist API Manager.ps1"
        $apiManagerContent = @"
# A3sist API Service Manager
Write-Host "A3sist API Service Management" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Gray
Write-Host "1. Start Service"
Write-Host "2. Stop Service"
Write-Host "3. Restart Service"
Write-Host "4. Check Status"
Write-Host "5. View Swagger UI"
Write-Host "6. View Logs"
Write-Host "7. Exit"
Write-Host

`$choice = Read-Host "Choose an option (1-7)"
switch (`$choice) {
    "1" { Start-Service -Name "$ServiceName"; Write-Host "Service started" -ForegroundColor Green }
    "2" { Stop-Service -Name "$ServiceName"; Write-Host "Service stopped" -ForegroundColor Yellow }
    "3" { Restart-Service -Name "$ServiceName"; Write-Host "Service restarted" -ForegroundColor Green }
    "4" { Get-Service -Name "$ServiceName" | Format-Table }
    "5" { Start-Process "http://localhost:$ApiPort/swagger" }
    "6" { Get-EventLog -LogName Application -Source "$ServiceName" -Newest 50 | Format-Table }
    "7" { exit }
}
Read-Host "Press Enter to continue"
"@
        Set-Content -Path $apiManagerPath -Value $apiManagerContent
        
        # Configuration editor script
        $configEditorPath = Join-Path $desktop "A3sist Configuration.ps1"
        $configEditorContent = @"
# A3sist Configuration Editor
`$configPath = "$ConfigDir\config.json"
if (Test-Path `$configPath) {
    Start-Process notepad `$configPath
} else {
    Write-Host "Configuration file not found at `$configPath" -ForegroundColor Red
    Read-Host "Press Enter to continue"
}
"@
        Set-Content -Path $configEditorPath -Value $configEditorContent
        
        # Uninstaller script
        $uninstallerPath = Join-Path $desktop "Uninstall A3sist.ps1"
        $uninstallerContent = @"
#Requires -RunAsAdministrator
# A3sist Uninstaller
Write-Host "A3sist Uninstaller" -ForegroundColor Red
Write-Host "==================" -ForegroundColor Gray
Write-Host "This will remove A3sist from your system."
`$confirm = Read-Host "Are you sure? (y/N)"
if (`$confirm -eq 'y' -or `$confirm -eq 'Y') {
    Write-Host "Stopping and removing service..."
    Stop-Service -Name "$ServiceName" -ErrorAction SilentlyContinue
    `$service = Get-WmiObject -Class Win32_Service -Filter "Name='$ServiceName'"
    if (`$service) { `$service.Delete() }
    
    Write-Host "Removing files..."
    Remove-Item -Path "$InstallPath" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "$ConfigDir" -Recurse -Force -ErrorAction SilentlyContinue
    
    Write-Host "Removing shortcuts..."
    Remove-Item -Path "$apiManagerPath" -ErrorAction SilentlyContinue
    Remove-Item -Path "$configEditorPath" -ErrorAction SilentlyContinue
    Remove-Item -Path "$uninstallerPath" -ErrorAction SilentlyContinue
    
    Write-Host "A3sist has been removed from your system." -ForegroundColor Green
    Write-Host "You may need to manually uninstall the Visual Studio extension."
}
Read-Host "Press Enter to continue"
"@
        Set-Content -Path $uninstallerPath -Value $uninstallerContent
        
        Write-Success "Desktop shortcuts created"
        return $true
    } catch {
        Write-Error "Failed to create shortcuts: $($_.Exception.Message)"
        return $false
    }
}

function Configure-Firewall {
    if ($SkipFirewall) {
        Write-Info "Skipping firewall configuration"
        return $true
    }
    
    Write-Step "Step 6:" "Configuring Windows Firewall..."
    
    try {
        $ruleName = "A3sist API"
        $existingRule = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
        
        if (-not $existingRule) {
            New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP -LocalPort $ApiPort -Action Allow | Out-Null
            Write-Success "Firewall rule added for port $ApiPort"
        } else {
            Write-Success "Firewall rule already exists"
        }
        
        return $true
    } catch {
        Write-Warning "Failed to configure firewall: $($_.Exception.Message)"
        Write-Info "You may need to manually allow port $ApiPort in Windows Firewall"
        return $true
    }
}

function Show-Summary {
    Write-Step "Installation Complete!" ""
    
    Write-Host ""
    Write-Success "A3sist API service installed and running on port $ApiPort"
    if (-not $SkipExtension) {
        Write-Success "Visual Studio extension installed"
    } else {
        Write-Warning "Visual Studio extension installation was skipped"
    }
    Write-Success "Configuration files created"
    Write-Success "Desktop shortcuts created"
    
    Write-Host "`nNext Steps:" -ForegroundColor Yellow
    if (-not $SkipExtension) {
        Write-Host "1. Restart Visual Studio 2022"
        Write-Host "2. Go to View → Other Windows → A3sist Assistant"
        Write-Host "3. The extension should automatically connect to the API"
    } else {
        Write-Host "1. Install Visual Studio 2022 if not already installed"
        Write-Host "2. Build the A3sist.UI project in Release mode"
        Write-Host "3. Manually install the extension: A3sist.UI\bin\Release\A3sist.UI.vsix"
        Write-Host "4. Restart Visual Studio 2022"
        Write-Host "5. Go to View → Other Windows → A3sist Assistant"
    }
    
    Write-Host "`nManagement Tools:" -ForegroundColor Yellow
    Write-Host "• A3sist API Manager.ps1 - Manage the API service"
    Write-Host "• A3sist Configuration.ps1 - Edit configuration"
    Write-Host "• Uninstall A3sist.ps1 - Remove A3sist from system"
    
    Write-Host "`nAPI Endpoints:" -ForegroundColor Yellow
    Write-Host "• Health Check: http://localhost:$ApiPort/api/health"
    Write-Host "• Swagger UI: http://localhost:$ApiPort/swagger"
    
    Write-Host "`nFor help and documentation, see:" -ForegroundColor Yellow
    Write-Host "• BUILD_AND_DEPLOYMENT.md"
    Write-Host "• TESTING_PLAN.md"
    Write-Host "• README.md"
    Write-Host "• Visual Studio troubleshooting: HOW_TO_FIND_SIDEBAR.md"
    
    Write-Host "`nEnjoy using A3sist AI Assistant! 🚀" -ForegroundColor Green
}

# Main installation process
try {
    Write-Host "A3sist AI Assistant Installer" -ForegroundColor Cyan
    Write-Host "=============================" -ForegroundColor Gray
    Write-Host ""
    
    if (-not (Test-Prerequisites)) {
        Write-Error "Prerequisites check failed. Installation aborted."
        exit 1
    }
    
    if (-not (Install-ApiService)) {
        Write-Error "API service installation failed. Installation aborted."
        exit 1
    }
    
    if (-not (Install-VisualStudioExtension)) {
        Write-Warning "Extension installation failed, but continuing..."
    }
    
    if (-not (Create-Configuration)) {
        Write-Warning "Configuration creation failed, but continuing..."
    }
    
    if (-not (Create-Shortcuts)) {
        Write-Warning "Shortcut creation failed, but continuing..."
    }
    
    Configure-Firewall | Out-Null
    
    Show-Summary
    
} catch {
    Write-Error "Installation failed: $($_.Exception.Message)"
    exit 1
}

Read-Host "`nPress Enter to exit"