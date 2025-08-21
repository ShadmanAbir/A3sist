using A3sist.Core.Configuration;
using A3sist.Core.Services;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace A3sist.Core.Extensions
{
    /// <summary>
    /// Extension methods for registering logging services
    /// </summary>
    public static class LoggingServiceExtensions
    {
        /// <summary>
        /// Adds A3sist logging services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddA3sistLogging(this IServiceCollection services, IConfiguration configuration)
        {
            // Register logging configuration provider
            services.AddSingleton<LoggingConfigurationProvider>();
            
            // Load logging configuration
            var configProvider = new LoggingConfigurationProvider(configuration);
            var loggingConfig = configProvider.LoadConfiguration();
            services.AddSingleton(loggingConfig);

            // Register logging service
            services.AddSingleton<ILoggingService>(provider =>
            {
                var config = provider.GetRequiredService<LoggingConfiguration>();
                return new LoggingService(config);
            });

            // Configure Microsoft.Extensions.Logging to use our logging service
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddProvider(new A3sistLoggerProvider(services.BuildServiceProvider().GetRequiredService<ILoggingService>()));
            });

            return services;
        }

        /// <summary>
        /// Adds A3sist logging services with a custom configuration
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="loggingConfiguration">The logging configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddA3sistLogging(this IServiceCollection services, LoggingConfiguration loggingConfiguration)
        {
            services.AddSingleton(loggingConfiguration);

            // Register logging service
            services.AddSingleton<ILoggingService>(provider =>
            {
                var config = provider.GetRequiredService<LoggingConfiguration>();
                return new LoggingService(config);
            });

            // Configure Microsoft.Extensions.Logging to use our logging service
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddProvider(new A3sistLoggerProvider(services.BuildServiceProvider().GetRequiredService<ILoggingService>()));
            });

            return services;
        }

        /// <summary>
        /// Adds A3sist logging services with default configuration
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddA3sistLogging(this IServiceCollection services)
        {
            var defaultConfig = LoggingConfigurationProvider.CreateDefault();
            return services.AddA3sistLogging(defaultConfig);
        }
    }

    /// <summary>
    /// Logger provider that integrates with A3sist logging service
    /// </summary>
    internal class A3sistLoggerProvider : ILoggerProvider
    {
        private readonly ILoggingService _loggingService;
        private bool _disposed;

        public A3sistLoggerProvider(ILoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(A3sistLoggerProvider));

            return _loggingService.CreateLogger(categoryName);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_loggingService is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _disposed = true;
        }
    }
}