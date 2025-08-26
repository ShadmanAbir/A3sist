using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Extensibility;

namespace A3sist.Core;

/// <summary>
/// A3sist Core Visual Studio Extension
/// </summary>
[VisualStudioContribution]
public class A3sistExtension : Extension
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
            // Initialize A3sist services using the startup configuration
            var serviceProvider = Startup.CreateServiceProvider();
            
            // Register the service provider for access throughout the extension
            serviceCollection.AddSingleton(serviceProvider);
            
            // Log successful initialization
            var logger = serviceProvider.GetService<ILogger<A3sistExtension>>();
            logger?.LogInformation("A3sist Core extension initialized successfully");
        }
        catch (Exception ex)
        {
            // Log initialization failure
            System.Diagnostics.Debug.WriteLine($"A3sist Core extension initialization failed: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Disposes the extension and cleans up resources
    /// </summary>
    /// <param name="disposing">True if disposing managed resources</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Reset the startup to clean up resources
            Startup.Reset();
        }
        
        base.Dispose(disposing);
    }
}