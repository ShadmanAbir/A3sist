using Microsoft.Extensions.Logging;

namespace A3sist.Core.Logging
{
    /// <summary>
    /// Helper for creating logging scopes with structured data
    /// </summary>
    public static class LoggingScope
    {
        /// <summary>
        /// Creates a scope for agent operations
        /// </summary>
        public static IDisposable ForAgent(ILogger logger, string agentName, string requestId)
        {
            return logger.BeginScope(new Dictionary<string, object>
            {
                ["AgentName"] = agentName,
                ["RequestId"] = requestId,
                ["OperationId"] = Guid.NewGuid().ToString("N")[..8]
            });
        }

        /// <summary>
        /// Creates a scope for orchestrator operations
        /// </summary>
        public static IDisposable ForOrchestrator(ILogger logger, string requestId, string operation)
        {
            return logger.BeginScope(new Dictionary<string, object>
            {
                ["Component"] = "Orchestrator",
                ["RequestId"] = requestId,
                ["Operation"] = operation,
                ["OperationId"] = Guid.NewGuid().ToString("N")[..8]
            });
        }

        /// <summary>
        /// Creates a scope for user operations
        /// </summary>
        public static IDisposable ForUser(ILogger logger, string? userId = null, string? sessionId = null)
        {
            return logger.BeginScope(new Dictionary<string, object>
            {
                ["UserId"] = userId ?? "Anonymous",
                ["SessionId"] = sessionId ?? "Unknown",
                ["OperationId"] = Guid.NewGuid().ToString("N")[..8]
            });
        }

        /// <summary>
        /// Creates a scope for performance monitoring
        /// </summary>
        public static IDisposable ForPerformance(ILogger logger, string operationName)
        {
            return logger.BeginScope(new Dictionary<string, object>
            {
                ["PerformanceOperation"] = operationName,
                ["StartTime"] = DateTimeOffset.UtcNow,
                ["OperationId"] = Guid.NewGuid().ToString("N")[..8]
            });
        }

        /// <summary>
        /// Creates a scope for external service calls
        /// </summary>
        public static IDisposable ForExternalService(ILogger logger, string serviceName, string operation)
        {
            return logger.BeginScope(new Dictionary<string, object>
            {
                ["ExternalService"] = serviceName,
                ["ServiceOperation"] = operation,
                ["CallId"] = Guid.NewGuid().ToString("N")[..8]
            });
        }

        /// <summary>
        /// Creates a scope with custom properties
        /// </summary>
        public static IDisposable WithProperties(ILogger logger, Dictionary<string, object> properties)
        {
            return logger.BeginScope(properties);
        }

        /// <summary>
        /// Creates a scope with a single property
        /// </summary>
        public static IDisposable WithProperty(ILogger logger, string key, object value)
        {
            return logger.BeginScope(new Dictionary<string, object> { [key] = value });
        }
    }

    /// <summary>
    /// Disposable wrapper for timing operations with automatic logging
    /// </summary>
    public class TimedOperation : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _operationName;
        private readonly LogLevel _logLevel;
        private readonly DateTime _startTime;
        private readonly IDisposable? _scope;
        private bool _disposed;

        public TimedOperation(ILogger logger, string operationName, LogLevel logLevel = LogLevel.Information, IDisposable? scope = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _operationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            _logLevel = logLevel;
            _startTime = DateTime.UtcNow;
            _scope = scope;

            _logger.Log(_logLevel, "Starting operation: {OperationName}", _operationName);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            var duration = DateTime.UtcNow - _startTime;
            _logger.Log(_logLevel, "Completed operation: {OperationName} in {Duration}ms", 
                _operationName, duration.TotalMilliseconds);

            _scope?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Extension methods for creating timed operations
    /// </summary>
    public static class TimedOperationExtensions
    {
        /// <summary>
        /// Creates a timed operation that logs start and completion
        /// </summary>
        public static TimedOperation TimeOperation(this ILogger logger, string operationName, LogLevel logLevel = LogLevel.Information)
        {
            return new TimedOperation(logger, operationName, logLevel);
        }

        /// <summary>
        /// Creates a timed operation with a logging scope
        /// </summary>
        public static TimedOperation TimeOperationWithScope(this ILogger logger, string operationName, 
            Dictionary<string, object> scopeProperties, LogLevel logLevel = LogLevel.Information)
        {
            var scope = logger.BeginScope(scopeProperties);
            return new TimedOperation(logger, operationName, logLevel, scope);
        }
    }
}