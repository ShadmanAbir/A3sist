using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace A3sist.UI.Services
{
    /// <summary>
    /// Service registration for editor integration components
    /// </summary>
    public static class EditorServiceRegistration
    {
        private static IServiceProvider? _serviceProvider;

        /// <summary>
        /// Initializes the service locator with the provided service provider
        /// </summary>
        /// <param name="serviceProvider">The service provider to use</param>
        public static void InitializeServiceLocator(IServiceProvider? serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            var logger = _serviceProvider?.GetService<ILogger<EditorServiceRegistration>>();
            logger?.LogInformation("Editor service locator initialized");
        }

        /// <summary>
        /// Gets a service from the service locator
        /// </summary>
        /// <typeparam name="T">The service type</typeparam>
        /// <returns>The service instance or null if not found</returns>
        public static T? GetService<T>() where T : class
        {
            return _serviceProvider?.GetService<T>();
        }

        /// <summary>
        /// Gets a required service from the service locator
        /// </summary>
        /// <typeparam name="T">The service type</typeparam>
        /// <returns>The service instance</returns>
        /// <exception cref="InvalidOperationException">Thrown if service is not found</exception>
        public static T GetRequiredService<T>() where T : class
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("Service locator not initialized");
                
            return _serviceProvider.GetRequiredService<T>();
        }
    }
}
