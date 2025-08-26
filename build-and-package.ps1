# A3sist Extension Build and Package Script
# This script builds the extension and creates a VSIX package for deployment

param(
    [Parameter(HelpMessage="Build configuration (Debug/Release)")]
    [string]$Configuration = "Release",
    
    [Parameter(HelpMessage="Skip running tests")]
    [switch]$SkipTests,
    
    [Parameter(HelpMessage="Skip creating VSIX package")]
    [switch]$SkipPackage,
    
    [Parameter(HelpMessage="Output directory for artifacts")]
    [string]$OutputDir = ".\artifacts"
)

# Script configuration
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SolutionPath = Join-Path $ScriptDir "A3sist.sln"
$ProjectPath = Join-Path $ScriptDir "src\A3sist.UI.VSIX\A3sist.UI.VSIX.csproj"

Write-Host "🚀 A3sist Extension Build and Package Script" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# Create output directory
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Function to check if command exists
function Test-Command($command) {
    $null = Get-Command $command -ErrorAction SilentlyContinue
    return $?
}

# Check prerequisites
Write-Host "🔍 Checking prerequisites..." -ForegroundColor Yellow

if (!(Test-Command "dotnet")) {
    Write-Error "❌ .NET CLI not found. Please install .NET 6.0 SDK or later."
    exit 1
}

if (!(Test-Command "msbuild")) {
    Write-Host "⚠️  MSBuild not found in PATH. Attempting to locate..." -ForegroundColor Yellow
    
    # Try to find MSBuild in common locations
    $msbuildPaths = @(
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
    )
    
    $msbuild = $null
    foreach ($path in $msbuildPaths) {
        if (Test-Path $path) {
            $msbuild = $path
            break
        }
    }
    
    if ($null -eq $msbuild) {
        Write-Error "❌ MSBuild not found. Please install Visual Studio 2019/2022 with MSBuild."
        exit 1
    }
    
    Write-Host "✅ Found MSBuild: $msbuild" -ForegroundColor Green
} else {
    $msbuild = "msbuild"
    Write-Host "✅ MSBuild found in PATH" -ForegroundColor Green
}

Write-Host "✅ .NET CLI version: $(dotnet --version)" -ForegroundColor Green

# Step 1: Clean solution
Write-Host ""
Write-Host "🧹 Cleaning solution..." -ForegroundColor Yellow
dotnet clean $SolutionPath --configuration $Configuration --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Clean failed"
    exit $LASTEXITCODE
}
Write-Host "✅ Solution cleaned" -ForegroundColor Green

# Step 2: Restore packages
Write-Host ""
Write-Host "📦 Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore $SolutionPath --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Package restore failed"
    exit $LASTEXITCODE
}
Write-Host "✅ Packages restored" -ForegroundColor Green

# Step 3: Build solution
Write-Host ""
Write-Host "🔨 Building solution ($Configuration)..." -ForegroundColor Yellow
dotnet build $SolutionPath --configuration $Configuration --no-restore --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Build failed"
    exit $LASTEXITCODE
}
Write-Host "✅ Solution built successfully" -ForegroundColor Green

# Step 4: Run tests (if not skipped)
if (!$SkipTests) {
    Write-Host ""
    Write-Host "🧪 Running tests..." -ForegroundColor Yellow
    
    $testProjects = Get-ChildItem -Path "tests" -Recurse -Name "*.csproj" -ErrorAction SilentlyContinue
    if ($testProjects) {
        dotnet test $SolutionPath --configuration $Configuration --no-build --verbosity minimal --logger "console;verbosity=normal"
        if ($LASTEXITCODE -ne 0) {
            Write-Error "❌ Tests failed"
            exit $LASTEXITCODE
        }
        Write-Host "✅ All tests passed" -ForegroundColor Green
    } else {
        Write-Host "ℹ️  No test projects found" -ForegroundColor Gray
    }
} else {
    Write-Host "⏭️  Skipping tests" -ForegroundColor Gray
}

# Step 5: Create VSIX package (if not skipped)
if (!$SkipPackage) {
    Write-Host ""
    Write-Host "📦 Creating VSIX package..." -ForegroundColor Yellow
    
    # Build the VSIX using MSBuild
    $vsixArgs = @(
        $ProjectPath,
        "/p:Configuration=$Configuration",
        "/p:Platform=`"Any CPU`"",
        "/p:OutputPath=`"$OutputDir`"",
        "/t:CreateVsixContainer",
        "/verbosity:minimal"
    )
    
    & $msbuild @vsixArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ VSIX package creation failed"
        exit $LASTEXITCODE
    }
    
    # Find the created VSIX file
    $vsixFiles = Get-ChildItem -Path $OutputDir -Name "*.vsix" -ErrorAction SilentlyContinue
    if ($vsixFiles) {
        $vsixFile = $vsixFiles[0]
        $vsixPath = Join-Path $OutputDir $vsixFile
        $vsixSize = (Get-Item $vsixPath).Length
        $vsixSizeMB = [math]::Round($vsixSize / 1MB, 2)
        
        Write-Host "✅ VSIX package created: $vsixFile ($vsixSizeMB MB)" -ForegroundColor Green
        Write-Host "📁 Package location: $vsixPath" -ForegroundColor Cyan
    } else {
        Write-Warning "⚠️  VSIX file not found in output directory"
    }
} else {
    Write-Host "⏭️  Skipping VSIX package creation" -ForegroundColor Gray
}

# Step 6: Generate build summary
Write-Host ""
Write-Host "📋 Build Summary" -ForegroundColor Cyan
Write-Host "=================" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor White
Write-Host "Output Directory: $OutputDir" -ForegroundColor White
Write-Host "Build Time: $(Get-Date)" -ForegroundColor White

# List all artifacts
$artifacts = Get-ChildItem -Path $OutputDir -ErrorAction SilentlyContinue
if ($artifacts) {
    Write-Host ""
    Write-Host "📁 Generated Artifacts:" -ForegroundColor Cyan
    foreach ($artifact in $artifacts) {
        $size = if ($artifact.PSIsContainer) { "" } else { " ($([math]::Round($artifact.Length / 1KB, 1)) KB)" }
        Write-Host "  📄 $($artifact.Name)$size" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "🎉 Build completed successfully!" -ForegroundColor Green
Write-Host ""

# Step 7: Marketplace deployment instructions
if (!$SkipPackage) {
    Write-Host "🚀 Deployment Instructions:" -ForegroundColor Yellow
    Write-Host "============================" -ForegroundColor Yellow
    Write-Host "1. Go to Visual Studio Marketplace Publisher Portal:" -ForegroundColor White
    Write-Host "   https://marketplace.visualstudio.com/manage" -ForegroundColor Cyan
    Write-Host "2. Sign in with your publisher account" -ForegroundColor White
    Write-Host "3. Click 'New extension' > 'Visual Studio'" -ForegroundColor White
    Write-Host "4. Upload the VSIX file: $vsixPath" -ForegroundColor Cyan
    Write-Host "5. Fill in the marketplace metadata and publish" -ForegroundColor White
    Write-Host ""
    Write-Host "📖 For detailed publishing guide, visit:" -ForegroundColor White
    Write-Host "   https://docs.microsoft.com/en-us/azure/devops/extend/publish/overview" -ForegroundColor Cyan
}

Write-Host ""