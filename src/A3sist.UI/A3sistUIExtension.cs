using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Extensibility;
using A3sist.Core;
using A3sist.Core.Services;

namespace A3sist.UI;

/// <summary>
/// A3sist UI Visual Studio Extension
/// </summary>
[VisualStudioContribution]
public class A3sistUIExtension : Extension
{
    /// <summary>
    /// Gets the extension's configuration
    /// </summary>
    public override ExtensionConfiguration ExtensionConfiguration => new()
    {
        RequiresInProcessHosting = true
    };

    /// <summary>
    /// Configures services for the extension
    /// </summary>
    /// <param name="serviceCollection">The service collection to configure</param>
    protected override void InitializeServices(IServiceCollection serviceCollection)
    {
        base.InitializeServices(serviceCollection);
        
        try
        {
            // Ensure core services are initialized
            var coreServiceProvider = Startup.CreateServiceProvider();
            serviceCollection.AddSingleton(coreServiceProvider);
            
            // UI-specific service registration will be added in subsequent tasks
            // This includes:
            // - Tool window services
            // - Command services
            // - Editor integration services
            
            // Log successful initialization
            var logger = ServiceLocator.GetServiceOrNull<ILogger<A3sistUIExtension>>();
            logger?.LogInformation("A3sist UI extension initialized successfully");
        }
        catch (Exception ex)
        {
            // Log initialization failure
            System.Diagnostics.Debug.WriteLine($"A3sist UI extension initialization failed: {ex}");
            throw;
        }
    }
}