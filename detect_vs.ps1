#Requires -Version 5.1
<#
.SYNOPSIS
    Comprehensive Visual Studio 2022 Detection Utility
.DESCRIPTION
    This script uses multiple methods to detect Visual Studio 2022 installations:
    1. vswhere.exe (Microsoft's official VS detection tool)
    2. Standard installation paths
    3. Registry entries
    4. Custom installation locations
    
    Returns detailed information about found installations including edition, version, and validation status.
.PARAMETER ShowAll
    Show all found installations, not just the best one
.PARAMETER ValidateOnly
    Only return installations that have required components for extension development
.PARAMETER JsonOutput
    Output results in JSON format
.EXAMPLE
    .\detect_vs.ps1
    Finds the best Visual Studio 2022 installation
.EXAMPLE
    .\detect_vs.ps1 -ShowAll -JsonOutput
    Shows all installations in JSON format
#>

param(
    [switch]$ShowAll,
    [switch]$ValidateOnly = $true,
    [switch]$JsonOutput
)

# Color output functions
function Write-ColorText {
    param($Text, $Color = 'White')
    if (-not $JsonOutput) {
        Write-Host $Text -ForegroundColor $Color
    }
}

function Write-Success { param($Message) Write-ColorText "✓ $Message" Green }
function Write-Warning { param($Message) Write-ColorText "⚠ $Message" Yellow }
function Write-Error { param($Message) Write-ColorText "✗ $Message" Red }
function Write-Info { param($Message) Write-ColorText "ℹ $Message" Cyan }

class VisualStudioInstallation {
    [string]$InstallationPath
    [string]$Version
    [string]$Edition
    [string]$DisplayName
    [string]$ProductId
    [bool]$IsValid
    [bool]$HasVSSDK
    [bool]$HasVSIXInstaller
    [DateTime]$InstallDate
    [string]$DetectionMethod
    [hashtable]$RawData
    
    VisualStudioInstallation() {
        $this.RawData = @{}
    }
    
    [bool] Validate() {
        $this.HasVSIXInstaller = Test-Path (Join-Path $this.InstallationPath "Common7\IDE\VSIXInstaller.exe")
        
        # Check for VSSDK (required for extension development)
        $vssdkPath = Join-Path $this.InstallationPath "VSSDK"
        $this.HasVSSDK = Test-Path $vssdkPath
        
        # Basic validation: devenv.exe exists
        $devenvPath = Join-Path $this.InstallationPath "Common7\IDE\devenv.exe"
        $hasDevenv = Test-Path $devenvPath
        
        if ($this.ValidateOnly) {
            $this.IsValid = $hasDevenv -and $this.HasVSIXInstaller -and $this.HasVSSDK
        } else {
            $this.IsValid = $hasDevenv
        }
        
        return $this.IsValid
    }
    
    [hashtable] ToHashtable() {
        return @{
            InstallationPath = $this.InstallationPath
            Version = $this.Version
            Edition = $this.Edition
            DisplayName = $this.DisplayName
            ProductId = $this.ProductId
            IsValid = $this.IsValid
            HasVSSDK = $this.HasVSSDK
            HasVSIXInstaller = $this.HasVSIXInstaller
            InstallDate = $this.InstallDate.ToString('yyyy-MM-dd HH:mm:ss')
            DetectionMethod = $this.DetectionMethod
        }
    }
}

class VisualStudioDetector {
    [VisualStudioInstallation[]] FindInstallations() {
        $installations = @()
        
        # Method 1: Use vswhere.exe (most reliable)
        $installations += $this.FindUsingVsWhere()
        
        # Method 2: Check standard installation paths
        $installations += $this.FindUsingStandardPaths()
        
        # Method 3: Check registry
        $installations += $this.FindUsingRegistry()
        
        # Method 4: Search custom locations
        $installations += $this.FindUsingCustomPaths()
        
        # Remove duplicates by installation path
        $uniqueInstallations = @{}
        foreach ($installation in $installations) {
            $key = $installation.InstallationPath.ToLower()
            if (-not $uniqueInstallations.ContainsKey($key)) {
                $uniqueInstallations[$key] = $installation
            }
        }
        
        # Convert back to array and validate
        $result = @()
        foreach ($installation in $uniqueInstallations.Values) {
            if ($installation.Validate()) {
                $result += $installation
            }
        }
        
        # Sort by version (newest first), then by edition priority
        $editionPriority = @{
            'Enterprise' = 3
            'Professional' = 2
            'Community' = 1
            'Unknown' = 0
        }
        
        $result = $result | Sort-Object {
            try {
                [Version]$_.Version
            } catch {
                [Version]"0.0"
            }
        }, {
            $editionPriority[$_.Edition]
        } -Descending
        
        return $result
    }
    
    [VisualStudioInstallation[]] FindUsingVsWhere() {
        $installations = @()
        
        $vswherePaths = @(
            "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe",
            "${env:ProgramFiles}\Microsoft Visual Studio\Installer\vswhere.exe"
        )
        
        foreach ($vswherePath in $vswherePaths) {
            if (Test-Path $vswherePath) {
                try {
                    Write-Info "Using vswhere.exe at: $vswherePath"
                    
                    # Query for VS 2022 with VSSDK
                    $args = @(
                        '-version', '[17.0,18.0)',
                        '-products', '*',
                        '-format', 'json'
                    )
                    
                    $jsonOutput = & $vswherePath @args 2>$null
                    if ($jsonOutput) {
                        $vsInstances = $jsonOutput | ConvertFrom-Json
                        
                        foreach ($instance in $vsInstances) {
                            $installation = [VisualStudioInstallation]::new()
                            $installation.InstallationPath = $instance.installationPath
                            $installation.Version = $instance.installationVersion
                            $installation.DisplayName = $instance.displayName
                            $installation.ProductId = $instance.productId
                            $installation.Edition = $this.GetEditionFromDisplayName($instance.displayName)
                            $installation.DetectionMethod = "vswhere.exe"
                            $installation.RawData = $instance | ConvertTo-Json -Depth 10 | ConvertFrom-Json -AsHashtable
                            
                            if ($instance.installDate) {
                                try {
                                    $installation.InstallDate = [DateTime]::Parse($instance.installDate)
                                } catch {
                                    $installation.InstallDate = [DateTime]::MinValue
                                }
                            }
                            
                            $installations += $installation
                        }
                    }
                    
                    break # Use first working vswhere.exe
                } catch {
                    Write-Warning "vswhere.exe failed: $($_.Exception.Message)"
                }
            }
        }
        
        return $installations
    }
    
    [VisualStudioInstallation[]] FindUsingStandardPaths() {
        $installations = @()
        $editions = @('Enterprise', 'Professional', 'Community')
        $basePaths = @(
            "${env:ProgramFiles}\Microsoft Visual Studio\2022",
            "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022"
        )
        
        Write-Info "Checking standard installation paths..."
        
        foreach ($basePath in $basePaths) {
            if (Test-Path $basePath) {
                foreach ($edition in $editions) {
                    $installPath = Join-Path $basePath $edition
                    if (Test-Path $installPath) {
                        $installation = [VisualStudioInstallation]::new()
                        $installation.InstallationPath = $installPath
                        $installation.Edition = $edition
                        $installation.DisplayName = "Visual Studio $edition 2022"
                        $installation.DetectionMethod = "Standard Path"
                        $installation.InstallDate = (Get-Item $installPath).CreationTime
                        
                        # Try to get version from devenv.exe
                        $devenvPath = Join-Path $installPath "Common7\IDE\devenv.exe"
                        if (Test-Path $devenvPath) {
                            try {
                                $versionInfo = (Get-Item $devenvPath).VersionInfo
                                $installation.Version = $versionInfo.ProductVersion
                            } catch {
                                $installation.Version = "17.0"
                            }
                        }
                        
                        $installations += $installation
                    }
                }
            }
        }
        
        return $installations
    }
    
    [VisualStudioInstallation[]] FindUsingRegistry() {
        $installations = @()
        
        Write-Info "Checking registry for Visual Studio installations..."
        
        $regKeys = @(
            "HKLM:\SOFTWARE\Microsoft\VisualStudio\Setup\Reboot",
            "HKLM:\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\Setup\Reboot",
            "HKLM:\SOFTWARE\Microsoft\VisualStudio",
            "HKLM:\SOFTWARE\WOW6432Node\Microsoft\VisualStudio"
        )
        
        foreach ($regKey in $regKeys) {
            try {
                if (Test-Path $regKey) {
                    $subKeys = Get-ChildItem $regKey -ErrorAction SilentlyContinue
                    
                    foreach ($subKey in $subKeys) {
                        $props = Get-ItemProperty $subKey.PSPath -ErrorAction SilentlyContinue
                        
                        if ($props.InstallDir -and (Test-Path $props.InstallDir)) {
                            # Navigate up to get VS root directory
                            $installPath = $props.InstallDir
                            $vsRoot = Split-Path (Split-Path $installPath) -Parent
                            
                            if (Test-Path "$vsRoot\Common7\IDE\devenv.exe") {
                                $installation = [VisualStudioInstallation]::new()
                                $installation.InstallationPath = $vsRoot
                                $installation.Edition = $this.GetEditionFromPath($vsRoot)
                                $installation.Version = $props.Version -or "17.0"
                                $installation.DisplayName = "Visual Studio $($installation.Edition) 2022"
                                $installation.DetectionMethod = "Registry"
                                $installation.InstallDate = [DateTime]::MinValue
                                
                                $installations += $installation
                            }
                        }
                    }
                }
            } catch {
                # Continue with next registry key
            }
        }
        
        return $installations
    }
    
    [VisualStudioInstallation[]] FindUsingCustomPaths() {
        $installations = @()
        
        Write-Info "Searching custom installation locations..."
        
        # Common custom installation drives and paths
        $customPaths = @()
        
        # Check all drives for VS installations
        $drives = Get-PSDrive -PSProvider FileSystem | Where-Object { $_.Root -match '^[C-Z]:\\' }
        
        foreach ($drive in $drives) {
            $searchPaths = @(
                "$($drive.Root)VS",
                "$($drive.Root)VisualStudio",
                "$($drive.Root)Microsoft Visual Studio",
                "$($drive.Root)Program Files\Microsoft Visual Studio",
                "$($drive.Root)Program Files (x86)\Microsoft Visual Studio"
            )
            
            $customPaths += $searchPaths
        }
        
        foreach ($searchPath in $customPaths) {
            try {
                if (Test-Path $searchPath) {
                    # Look for 2022 subdirectories
                    $vs2022Dirs = Get-ChildItem $searchPath -Directory -ErrorAction SilentlyContinue | 
                        Where-Object { $_.Name -like "*2022*" }
                    
                    foreach ($vs2022Dir in $vs2022Dirs) {
                        # Look for edition subdirectories
                        $editionDirs = Get-ChildItem $vs2022Dir.FullName -Directory -ErrorAction SilentlyContinue
                        
                        foreach ($editionDir in $editionDirs) {
                            $devenvPath = Join-Path $editionDir.FullName "Common7\IDE\devenv.exe"
                            if (Test-Path $devenvPath) {
                                $installation = [VisualStudioInstallation]::new()
                                $installation.InstallationPath = $editionDir.FullName
                                $installation.Edition = $this.GetEditionFromPath($editionDir.FullName)
                                $installation.DisplayName = "Visual Studio $($installation.Edition) 2022"
                                $installation.DetectionMethod = "Custom Path"
                                $installation.InstallDate = $editionDir.CreationTime
                                
                                try {
                                    $versionInfo = (Get-Item $devenvPath).VersionInfo
                                    $installation.Version = $versionInfo.ProductVersion
                                } catch {
                                    $installation.Version = "17.0"
                                }
                                
                                $installations += $installation
                            }
                        }
                    }
                }
            } catch {
                # Continue searching
            }
        }
        
        return $installations
    }
    
    [string] GetEditionFromDisplayName([string]$displayName) {
        $editions = @('Enterprise', 'Professional', 'Community')
        foreach ($edition in $editions) {
            if ($displayName -like "*$edition*") {
                return $edition
            }
        }
        return 'Unknown'
    }
    
    [string] GetEditionFromPath([string]$installPath) {
        $editions = @('Enterprise', 'Professional', 'Community')
        foreach ($edition in $editions) {
            if ($installPath -like "*$edition*") {
                return $edition
            }
        }
        return 'Unknown'
    }
}

# Main execution
try {
    Write-Info "Visual Studio 2022 Detection Utility"
    Write-Info "===================================="
    
    $detector = [VisualStudioDetector]::new()
    $installations = $detector.FindInstallations()
    
    if ($installations.Count -eq 0) {
        Write-Error "No Visual Studio 2022 installations found"
        Write-Info ""
        Write-Info "Please ensure Visual Studio 2022 is installed with:"
        Write-Info "• Any edition: Community, Professional, or Enterprise"
        Write-Info "• Visual Studio extension development workload"
        Write-Info "• .NET Framework 4.7.2 or later"
        Write-Info ""
        Write-Info "Download from: https://visualstudio.microsoft.com/downloads/"
        
        if ($JsonOutput) {
            @{
                Success = $false
                Message = "No Visual Studio 2022 installations found"
                Installations = @()
            } | ConvertTo-Json -Depth 10
        }
        
        exit 1
    }
    
    if ($JsonOutput) {
        $result = @{
            Success = $true
            Count = $installations.Count
            Installations = @()
        }
        
        foreach ($installation in $installations) {
            $result.Installations += $installation.ToHashtable()
        }
        
        if (-not $ShowAll -and $installations.Count -gt 0) {
            $result.RecommendedInstallation = $installations[0].ToHashtable()
        }
        
        $result | ConvertTo-Json -Depth 10
    } else {
        Write-Success "Found $($installations.Count) Visual Studio 2022 installation$(if($installations.Count -ne 1){'s'})"
        Write-Info ""
        
        $displayInstallations = if ($ShowAll) { $installations } else { @($installations[0]) }
        
        foreach ($installation in $displayInstallations) {
            Write-Success "$($installation.DisplayName)"
            Write-Info "  Path: $($installation.InstallationPath)"
            Write-Info "  Version: $($installation.Version)"
            Write-Info "  Detection: $($installation.DetectionMethod)"
            Write-Info "  VSIX Installer: $(if($installation.HasVSIXInstaller){'✓ Available'}else{'✗ Missing'})"
            Write-Info "  VS SDK: $(if($installation.HasVSSDK){'✓ Available'}else{'✗ Missing'})"
            
            if ($installation.InstallDate -ne [DateTime]::MinValue) {
                Write-Info "  Install Date: $($installation.InstallDate.ToString('yyyy-MM-dd'))"
            }
            
            Write-Info ""
        }
        
        if (-not $ShowAll -and $installations.Count -gt 1) {
            Write-Info "Use -ShowAll to see all $($installations.Count) installations"
        }
    }
    
} catch {
    $errorMsg = "Detection failed: $($_.Exception.Message)"
    Write-Error $errorMsg
    
    if ($JsonOutput) {
        @{
            Success = $false
            Message = $errorMsg
            Installations = @()
        } | ConvertTo-Json -Depth 10
    }
    
    exit 1
}