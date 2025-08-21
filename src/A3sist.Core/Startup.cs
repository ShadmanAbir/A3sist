using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using A3sist.Core.Extensions;
using A3sist.Core.Services;
using System.Reflection;
using System.IO;

namespace A3sist.Core;

/// <summary>
/// Startup class for configuring services and the application pipeline
/// </summary>
public class Startup
{
    private readonly IConfiguration _configuration;
    private static IServiceProvider? _serviceProvider;
    private static readonly object _lock = new();

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Configures services for the application
    /// </summary>
    /// <param name="services">The service collection</param>
    public void ConfigureServices(IServiceCollection services)
    {
        // Add A3sist services
        services.AddA3sistServices(_configuration);
        
        // Validate service registration
        services.ValidateA3sistServices();
    }

    /// <summary>
    /// Creates a configured service provider (singleton pattern for extension context)
    /// </summary>
    /// <returns>The configured service provider</returns>
    public static IServiceProvider CreateServiceProvider()
    {
        if (_serviceProvider != null)
            return _serviceProvider;

        lock (_lock)
        {
            if (_serviceProvider != null)
                return _serviceProvider;

            var configuration = BuildConfiguration();
            var services = new ServiceCollection();
            
            var startup = new Startup(configuration);
            startup.ConfigureServices(services);
            
            _serviceProvider = services.BuildServiceProvider();
            
            // Initialize the service locator
            ServiceLocator.Initialize(_serviceProvider);
            
            // Log successful initialization
            var logger = _serviceProvider.GetService<ILogger<Startup>>();
            logger?.LogInformation("A3sist services initialized successfully");
            
            return _serviceProvider;
        }
    }

    /// <summary>
    /// Resets the service provider (primarily for testing)
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _serviceProvider = null;
            ServiceLocator.Reset();
        }
    }

    /// <summary>
    /// Builds the configuration from various sources
    /// </summary>
    /// <returns>The built configuration</returns>
    private static IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder();
        
        // Get the directory where the assembly is located
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        
        if (!string.IsNullOrEmpty(assemblyDirectory))
        {
            builder.SetBasePath(assemblyDirectory);
        }

        // Add configuration sources in priority order
        builder
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{GetEnvironment()}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables("A3SIST_");

        // Add user secrets only in development
        if (GetEnvironment().Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            builder.AddUserSecrets<Startup>(optional: true);
        }

        return builder.Build();
    }

    /// <summary>
    /// Gets the current environment name
    /// </summary>
    /// <returns>The environment name</returns>
    private static string GetEnvironment()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
               ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") 
               ?? "Production";
    }
}