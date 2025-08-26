# A3sist Extension Deployment Configuration
# Configuration file for automated deployment and CI/CD pipelines

# Extension Metadata
$ExtensionConfig = @{
    # Basic Information
    Name = "A3sist - AI-Powered Development Assistant"
    Publisher = "A3sist"
    Version = "1.0.0"
    
    # Visual Studio Marketplace
    MarketplaceId = "A3sist.AI.Assistant"
    Categories = @("AI Tools", "Productivity", "Code Analysis", "Refactoring")
    
    # Build Configuration
    Configuration = "Release"
    Platform = "Any CPU"
    TargetVSVersion = "17.9"
    
    # Artifacts
    OutputDirectory = ".\artifacts"
    VSIXFileName = "A3sist-v{VERSION}.vsix"
    
    # Dependencies
    RequiredVSComponents = @(
        "Microsoft.VisualStudio.Component.CoreEditor",
        "Microsoft.VisualStudio.Workload.ManagedDesktop",
        "Microsoft.VisualStudio.Component.Roslyn.Compiler"
    )
    
    # Quality Gates
    MinimumTestCoverage = 80
    RequiredTestPassing = $true
    StaticAnalysisRequired = $true
    
    # Deployment Targets
    Environments = @{
        Development = @{
            AutoDeploy = $true
            RequireApproval = $false
            VSIXSigning = $false
        }
        Staging = @{
            AutoDeploy = $false
            RequireApproval = $true
            VSIXSigning = $true
        }
        Production = @{
            AutoDeploy = $false
            RequireApproval = $true
            VSIXSigning = $true
            MarketplacePublish = $true
        }
    }
}

# Export configuration for use in scripts
Export-ModuleMember -Variable ExtensionConfig