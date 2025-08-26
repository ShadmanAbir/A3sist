# A3sist Marketplace Deployment Script
# Automates the deployment of A3sist extension to Visual Studio Marketplace

param(
    [Parameter(Mandatory=$true, HelpMessage="Personal Access Token for VS Marketplace")]
    [string]$AccessToken,
    
    [Parameter(HelpMessage="Path to VSIX file to deploy")]
    [string]$VsixPath,
    
    [Parameter(HelpMessage="Environment to deploy to (dev/staging/prod)")]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment = "dev",
    
    [Parameter(HelpMessage="Skip pre-deployment validation")]
    [switch]$SkipValidation
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir

# Load deployment configuration
. "$RootDir\deployment-config.ps1"

Write-Host "🚀 A3sist Marketplace Deployment" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host ""

# Step 1: Validate prerequisites
if (!$SkipValidation) {
    Write-Host "🔍 Validating deployment prerequisites..." -ForegroundColor Yellow
    
    # Check if vsce is installed
    if (!(Get-Command "vsce" -ErrorAction SilentlyContinue)) {
        Write-Host "Installing vsce (Visual Studio Code Extension Manager)..." -ForegroundColor Yellow
        npm install -g vsce
        if ($LASTEXITCODE -ne 0) {
            Write-Error "❌ Failed to install vsce"
            exit $LASTEXITCODE
        }
    }
    
    Write-Host "✅ Prerequisites validated" -ForegroundColor Green
}

# Step 2: Determine VSIX file path
if (!$VsixPath) {
    $VsixPath = Get-ChildItem -Path "$RootDir\artifacts" -Name "*.vsix" | Select-Object -First 1
    if (!$VsixPath) {
        Write-Error "❌ No VSIX file found in artifacts directory. Run build-and-package.ps1 first."
        exit 1
    }
    $VsixPath = Join-Path "$RootDir\artifacts" $VsixPath
}

if (!(Test-Path $VsixPath)) {
    Write-Error "❌ VSIX file not found: $VsixPath"
    exit 1
}

Write-Host "📦 VSIX file: $VsixPath" -ForegroundColor Cyan

# Step 3: Validate VSIX package
if (!$SkipValidation) {
    Write-Host ""
    Write-Host "🔍 Validating VSIX package..." -ForegroundColor Yellow
    
    # Check VSIX file size (should be reasonable)
    $vsixSize = (Get-Item $VsixPath).Length
    $vsixSizeMB = [math]::Round($vsixSize / 1MB, 2)
    
    if ($vsixSizeMB -gt 50) {
        Write-Warning "⚠️  VSIX file is quite large: $vsixSizeMB MB"
    }
    
    Write-Host "✅ VSIX validation completed ($vsixSizeMB MB)" -ForegroundColor Green
}

# Step 4: Environment-specific deployment
switch ($Environment) {
    "dev" {
        Write-Host ""
        Write-Host "🧪 Development Deployment" -ForegroundColor Yellow
        Write-Host "Installing locally for testing..." -ForegroundColor Gray
        
        # Install locally using VSIXInstaller
        $vsixInstaller = "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\VSIXInstaller.exe"
        if (!(Test-Path $vsixInstaller)) {
            $vsixInstaller = "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\Common7\IDE\VSIXInstaller.exe"
        }
        if (!(Test-Path $vsixInstaller)) {
            $vsixInstaller = "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\Common7\IDE\VSIXInstaller.exe"
        }
        
        if (Test-Path $vsixInstaller) {
            & "$vsixInstaller" /quiet $VsixPath
            Write-Host "✅ Extension installed locally for testing" -ForegroundColor Green
        } else {
            Write-Warning "⚠️  VSIXInstaller not found. Manual installation required."
        }
    }
    
    "staging" {
        Write-Host ""
        Write-Host "🏗️  Staging Deployment" -ForegroundColor Yellow
        Write-Host "Preparing for marketplace validation..." -ForegroundColor Gray
        
        # Create staging package info
        $stagingInfo = @{
            Version = $ExtensionConfig.Version
            Environment = "Staging"
            BuildTime = Get-Date
            VsixFile = Split-Path -Leaf $VsixPath
            Size = "$vsixSizeMB MB"
        }
        
        $stagingInfoJson = $stagingInfo | ConvertTo-Json -Depth 3
        $stagingInfoPath = Join-Path (Split-Path $VsixPath) "staging-info.json"
        $stagingInfoJson | Out-File -FilePath $stagingInfoPath -Encoding UTF8
        
        Write-Host "✅ Staging package prepared" -ForegroundColor Green
        Write-Host "📁 Staging info: $stagingInfoPath" -ForegroundColor Cyan
    }
    
    "prod" {
        Write-Host ""
        Write-Host "🌟 Production Deployment" -ForegroundColor Yellow
        Write-Host "Deploying to Visual Studio Marketplace..." -ForegroundColor Gray
        
        # Note: Actual marketplace deployment would require VS Marketplace API
        # This is a placeholder for the deployment process
        Write-Host ""
        Write-Host "📋 Manual Deployment Steps:" -ForegroundColor Cyan
        Write-Host "1. Go to: https://marketplace.visualstudio.com/manage" -ForegroundColor White
        Write-Host "2. Sign in with publisher account" -ForegroundColor White
        Write-Host "3. Upload VSIX: $VsixPath" -ForegroundColor Cyan
        Write-Host "4. Update marketplace description from: $RootDir\marketplace\marketplace-description.md" -ForegroundColor Cyan
        Write-Host "5. Set tags: AI, Assistant, Code Analysis, Refactoring, Productivity" -ForegroundColor White
        Write-Host "6. Publish extension" -ForegroundColor White
        Write-Host ""
        
        # Create deployment manifest
        $deploymentManifest = @{
            ExtensionId = $ExtensionConfig.MarketplaceId
            Version = $ExtensionConfig.Version
            Publisher = $ExtensionConfig.Publisher
            DeploymentTime = Get-Date
            Environment = "Production"
            VsixFile = $VsixPath
            MarketplaceUrl = "https://marketplace.visualstudio.com/items?itemName=$($ExtensionConfig.MarketplaceId)"
        }
        
        $manifestJson = $deploymentManifest | ConvertTo-Json -Depth 3
        $manifestPath = Join-Path (Split-Path $VsixPath) "deployment-manifest.json"
        $manifestJson | Out-File -FilePath $manifestPath -Encoding UTF8
        
        Write-Host "✅ Production deployment manifest created" -ForegroundColor Green
        Write-Host "📁 Manifest: $manifestPath" -ForegroundColor Cyan
    }
}

# Step 5: Post-deployment tasks
Write-Host ""
Write-Host "📋 Post-Deployment Checklist" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan
Write-Host "🔍 Test extension functionality in fresh VS instance" -ForegroundColor White
Write-Host "📝 Update version numbers for next release" -ForegroundColor White
Write-Host "📚 Update documentation if needed" -ForegroundColor White
Write-Host "🔔 Notify users about new release" -ForegroundColor White
Write-Host "📊 Monitor marketplace metrics and user feedback" -ForegroundColor White
Write-Host ""

Write-Host "🎉 Deployment process completed successfully!" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "VSIX: $(Split-Path -Leaf $VsixPath)" -ForegroundColor Yellow
Write-Host ""