using A3sist.Core.Services;
using A3sist.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace A3sist.Core.Extensions
{
    /// <summary>
    /// Extension methods for registering performance monitoring services
    /// </summary>
    public static class PerformanceMonitoringExtensions
    {
        /// <summary>
        /// Adds performance monitoring services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddPerformanceMonitoring(this IServiceCollection services)
        {
            services.AddSingleton<IPerformanceMonitoringService, PerformanceMonitoringService>();
            return services;
        }

        /// <summary>
        /// Adds performance monitoring services with a custom implementation
        /// </summary>
        /// <typeparam name="TImplementation">The implementation type</typeparam>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddPerformanceMonitoring<TImplementation>(this IServiceCollection services)
            where TImplementation : class, IPerformanceMonitoringService
        {
            services.AddSingleton<IPerformanceMonitoringService, TImplementation>();
            return services;
        }

        /// <summary>
        /// Adds performance monitoring services with a factory
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="factory">Factory function to create the service</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddPerformanceMonitoring(this IServiceCollection services, 
            Func<IServiceProvider, IPerformanceMonitoringService> factory)
        {
            services.AddSingleton(factory);
            return services;
        }
    }

    /// <summary>
    /// Extension methods for ILogger to easily record performance metrics
    /// </summary>
    public static class LoggerPerformanceExtensions
    {
        /// <summary>
        /// Records a performance metric through the logger
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="performanceService">The performance monitoring service</param>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="value">Value to record</param>
        /// <param name="unit">Unit of measurement</param>
        /// <param name="tags">Optional tags</param>
        public static async Task LogPerformanceMetricAsync(this ILogger logger, 
            IPerformanceMonitoringService performanceService,
            string metricName, double value, string unit = "", Dictionary<string, string>? tags = null)
        {
            try
            {
                await performanceService.RecordGaugeAsync(metricName, value, tags);
                logger.LogDebug("Recorded performance metric {MetricName}: {Value} {Unit}", metricName, value, unit);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to record performance metric {MetricName}", metricName);
            }
        }

        /// <summary>
        /// Records a timing metric through the logger
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="performanceService">The performance monitoring service</param>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="duration">Duration of the operation</param>
        /// <param name="tags">Optional tags</param>
        public static async Task LogOperationTimingAsync(this ILogger logger,
            IPerformanceMonitoringService performanceService,
            string operationName, TimeSpan duration, Dictionary<string, string>? tags = null)
        {
            try
            {
                await performanceService.RecordTimingAsync(operationName, duration, tags);
                logger.LogDebug("Recorded operation timing {OperationName}: {Duration}ms", 
                    operationName, duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to record operation timing {OperationName}", operationName);
            }
        }

        /// <summary>
        /// Creates a performance timer that logs when disposed
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="performanceService">The performance monitoring service</param>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="tags">Optional tags</param>
        /// <returns>A disposable timer</returns>
        public static IDisposable StartPerformanceTimer(this ILogger logger,
            IPerformanceMonitoringService performanceService,
            string operationName, Dictionary<string, string>? tags = null)
        {
            logger.LogDebug("Starting performance timer for {OperationName}", operationName);
            return new LoggingPerformanceTimer(logger, performanceService, operationName, tags);
        }
    }

    /// <summary>
    /// Performance timer that logs through ILogger
    /// </summary>
    internal class LoggingPerformanceTimer : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IPerformanceMonitoringService _performanceService;
        private readonly string _operationName;
        private readonly Dictionary<string, string>? _tags;
        private readonly DateTime _startTime;
        private readonly IDisposable _performanceTimer;
        private bool _disposed;

        public LoggingPerformanceTimer(ILogger logger, IPerformanceMonitoringService performanceService,
            string operationName, Dictionary<string, string>? tags)
        {
            _logger = logger;
            _performanceService = performanceService;
            _operationName = operationName;
            _tags = tags;
            _startTime = DateTime.UtcNow;
            _performanceTimer = performanceService.StartTimer(operationName, tags);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            var duration = DateTime.UtcNow - _startTime;
            _logger.LogDebug("Completed performance timer for {OperationName}: {Duration}ms", 
                _operationName, duration.TotalMilliseconds);

            _performanceTimer.Dispose();
            _disposed = true;
        }
    }
}