using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace A3sist.Core.Services;

/// <summary>
/// Service locator for accessing services in Visual Studio extension context
/// </summary>
public static class ServiceLocator
{
    private static IServiceProvider? _serviceProvider;
    private static readonly object _lock = new();

    /// <summary>
    /// Initializes the service locator with a service provider
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    public static void Initialize(IServiceProvider serviceProvider)
    {
        lock (_lock)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
    }

    /// <summary>
    /// Gets a service of the specified type
    /// </summary>
    /// <typeparam name="T">The type of service to get</typeparam>
    /// <returns>The service instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when service locator is not initialized</exception>
    public static T GetService<T>() where T : notnull
    {
        EnsureInitialized();
        return _serviceProvider!.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets a service of the specified type, or null if not found
    /// </summary>
    /// <typeparam name="T">The type of service to get</typeparam>
    /// <returns>The service instance or null</returns>
    public static T? GetServiceOrNull<T>() where T : class
    {
        if (_serviceProvider == null)
            return null;
            
        return _serviceProvider.GetService<T>();
    }

    /// <summary>
    /// Gets all services of the specified type
    /// </summary>
    /// <typeparam name="T">The type of services to get</typeparam>
    /// <returns>An enumerable of service instances</returns>
    public static IEnumerable<T> GetServices<T>()
    {
        EnsureInitialized();
        return _serviceProvider!.GetServices<T>();
    }

    /// <summary>
    /// Creates a new scope for scoped services
    /// </summary>
    /// <returns>A new service scope</returns>
    public static IServiceScope CreateScope()
    {
        EnsureInitialized();
        return _serviceProvider!.CreateScope();
    }

    /// <summary>
    /// Checks if the service locator is initialized
    /// </summary>
    /// <returns>True if initialized, false otherwise</returns>
    public static bool IsInitialized => _serviceProvider != null;

    /// <summary>
    /// Resets the service locator (primarily for testing)
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            if (_serviceProvider is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    // Log the exception if we have a logger available
                    var loggerFactory = _serviceProvider?.GetService<ILoggerFactory>();
                    var logger = loggerFactory?.CreateLogger("ServiceLocator");
                    logger?.LogError(ex, "Error disposing service provider during reset");
                }
            }
            
            _serviceProvider = null;
        }
    }

    private static void EnsureInitialized()
    {
        if (_serviceProvider == null)
        {
            throw new InvalidOperationException(
                "ServiceLocator has not been initialized. Call ServiceLocator.Initialize() first.");
        }
    }
}