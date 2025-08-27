using A3sist.Core.Services;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security;

namespace A3sist.Core.Extensions
{
    /// <summary>
    /// Extension methods for registering error reporting services
    /// </summary>
    public static class ErrorReportingExtensions
    {
        /// <summary>
        /// Adds error reporting services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddErrorReporting(this IServiceCollection services)
        {
            services.AddSingleton<IErrorReportingService, ErrorReportingService>();
            return services;
        }

        /// <summary>
        /// Adds error reporting services with a custom implementation
        /// </summary>
        /// <typeparam name="TImplementation">The implementation type</typeparam>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddErrorReporting<TImplementation>(this IServiceCollection services)
            where TImplementation : class, IErrorReportingService
        {
            services.AddSingleton<IErrorReportingService, TImplementation>();
            return services;
        }

        /// <summary>
        /// Adds error reporting services with a factory
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="factory">Factory function to create the service</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddErrorReporting(this IServiceCollection services,
            Func<IServiceProvider, IErrorReportingService> factory)
        {
            services.AddSingleton(factory);
            return services;
        }
    }

    /// <summary>
    /// Extension methods for ILogger to easily report errors
    /// </summary>
    public static class LoggerErrorReportingExtensions
    {
        /// <summary>
        /// Reports an exception through both logging and error reporting
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="errorReportingService">The error reporting service</param>
        /// <param name="exception">The exception to report</param>
        /// <param name="message">Additional message</param>
        /// <param name="context">Additional context</param>
        /// <param name="component">Component where the error occurred</param>
        public static async Task LogAndReportExceptionAsync(this ILogger logger,
            IErrorReportingService errorReportingService,
            Exception exception, string? message = null,
            Dictionary<string, object>? context = null, string? component = null)
        {
            // Log the exception
            logger.LogError(exception, message ?? exception.Message);

            // Report the exception
            try
            {
                var severity = DetermineSeverity(exception);
                await errorReportingService.ReportExceptionAsync(exception, context, severity, component);
            }
            catch (Exception reportingException)
            {
                logger.LogWarning(reportingException, "Failed to report exception to error reporting service");
            }
        }

        /// <summary>
        /// Reports an error through both logging and error reporting
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="errorReportingService">The error reporting service</param>
        /// <param name="message">Error message</param>
        /// <param name="category">Error category</param>
        /// <param name="severity">Error severity</param>
        /// <param name="context">Additional context</param>
        /// <param name="component">Component where the error occurred</param>
        public static async Task LogAndReportErrorAsync(this ILogger logger,
            IErrorReportingService errorReportingService,
            string message, ErrorCategory category = ErrorCategory.Application,
            ErrorSeverity severity = ErrorSeverity.Error,
            Dictionary<string, object>? context = null, string? component = null)
        {
            // Log the error
            var logLevel = ConvertSeverityToLogLevel(severity);
            logger.Log(logLevel, message);

            // Report the error
            try
            {
                await errorReportingService.ReportErrorAsync(message, category, severity, context, component);
            }
            catch (Exception reportingException)
            {
                logger.LogWarning(reportingException, "Failed to report error to error reporting service");
            }
        }

        /// <summary>
        /// Creates an error reporting scope that automatically reports exceptions
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="errorReportingService">The error reporting service</param>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="component">Component performing the operation</param>
        /// <param name="context">Additional context</param>
        /// <returns>A disposable error reporting scope</returns>
        public static IDisposable CreateErrorReportingScope(this ILogger logger,
            IErrorReportingService errorReportingService,
            string operationName, string? component = null,
            Dictionary<string, object>? context = null)
        {
            return new ErrorReportingScope(logger, errorReportingService, operationName, component, context);
        }

        private static ErrorSeverity DetermineSeverity(Exception exception)
        {
            return exception switch
            {
                OutOfMemoryException or StackOverflowException => ErrorSeverity.Fatal,
                UnauthorizedAccessException or SecurityException => ErrorSeverity.Critical,
                ArgumentException or ArgumentNullException => ErrorSeverity.Warning,
                _ => ErrorSeverity.Error
            };
        }

        private static LogLevel ConvertSeverityToLogLevel(ErrorSeverity severity)
        {
            return severity switch
            {
                ErrorSeverity.Info => LogLevel.Information,
                ErrorSeverity.Warning => LogLevel.Warning,
                ErrorSeverity.Error => LogLevel.Error,
                ErrorSeverity.Critical => LogLevel.Critical,
                ErrorSeverity.Fatal => LogLevel.Critical,
                _ => LogLevel.Error
            };
        }
    }

    /// <summary>
    /// Error reporting scope that automatically reports exceptions
    /// </summary>
    internal class ErrorReportingScope : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IErrorReportingService _errorReportingService;
        private readonly string _operationName;
        private readonly string? _component;
        private readonly Dictionary<string, object>? _context;
        private readonly DateTime _startTime;
        private bool _disposed;

        public ErrorReportingScope(ILogger logger, IErrorReportingService errorReportingService,
            string operationName, string? component, Dictionary<string, object>? context)
        {
            _logger = logger;
            _errorReportingService = errorReportingService;
            _operationName = operationName;
            _component = component;
            _context = context;
            _startTime = DateTime.UtcNow;

            _logger.LogDebug("Starting error reporting scope for operation {OperationName}", _operationName);
        }

        public void ReportException(Exception exception)
        {
            if (_disposed)
                return;

            var enhancedContext = new Dictionary<string, object>(_context ?? new Dictionary<string, object>())
            {
                ["OperationName"] = _operationName,
                ["OperationStartTime"] = _startTime,
                ["OperationDuration"] = DateTime.UtcNow - _startTime
            };

            _ = Task.Run(async () =>
            {
                try
                {
                    await _errorReportingService.ReportExceptionAsync(exception, enhancedContext, 
                        ErrorSeverity.Error, _component);
                }
                catch (Exception reportingException)
                {
                    _logger.LogWarning(reportingException, "Failed to report exception in error reporting scope");
                }
            });
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            var duration = DateTime.UtcNow - _startTime;
            _logger.LogDebug("Completed error reporting scope for operation {OperationName} in {Duration}ms",
                _operationName, duration.TotalMilliseconds);

            _disposed = true;
        }
    }

    /// <summary>
    /// Extension methods for Exception to add error reporting capabilities
    /// </summary>
    public static class ExceptionErrorReportingExtensions
    {
        /// <summary>
        /// Reports an exception to the error reporting service
        /// </summary>
        /// <param name="exception">The exception to report</param>
        /// <param name="errorReportingService">The error reporting service</param>
        /// <param name="context">Additional context</param>
        /// <param name="severity">Error severity</param>
        /// <param name="component">Component where the error occurred</param>
        public static async Task ReportAsync(this Exception exception,
            IErrorReportingService errorReportingService,
            Dictionary<string, object>? context = null,
            ErrorSeverity severity = ErrorSeverity.Error,
            string? component = null)
        {
            await errorReportingService.ReportExceptionAsync(exception, context, severity, component);
        }

        /// <summary>
        /// Adds context information to an exception for error reporting
        /// </summary>
        /// <param name="exception">The exception to enhance</param>
        /// <param name="key">Context key</param>
        /// <param name="value">Context value</param>
        /// <returns>The same exception for chaining</returns>
        public static T AddContext<T>(this T exception, string key, object value) where T : Exception
        {
            exception.Data[key] = value;
            return exception;
        }

        /// <summary>
        /// Adds multiple context items to an exception for error reporting
        /// </summary>
        /// <param name="exception">The exception to enhance</param>
        /// <param name="context">Context dictionary</param>
        /// <returns>The same exception for chaining</returns>
        public static T AddContext<T>(this T exception, Dictionary<string, object> context) where T : Exception
        {
            foreach (var kvp in context)
            {
                exception.Data[kvp.Key] = kvp.Value;
            }
            return exception;
        }
    }
}