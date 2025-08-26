using Microsoft.Extensions.Logging;

namespace A3sist.Core.Logging
{
    /// <summary>
    /// Helper class for structured logging with predefined log templates
    /// </summary>
    public static class StructuredLogging
    {
        /// <summary>
        /// Logs agent execution start
        /// </summary>
        public static void LogAgentExecutionStart(this ILogger logger, string agentName, string requestId, string operation)
        {
            logger.LogInformation("Agent {AgentName} starting execution for request {RequestId} with operation {Operation}",
                agentName, requestId, operation);
        }

        /// <summary>
        /// Logs agent execution completion
        /// </summary>
        public static void LogAgentExecutionComplete(this ILogger logger, string agentName, string requestId, 
            string operation, TimeSpan duration, bool success)
        {
            if (success)
            {
                logger.LogInformation("Agent {AgentName} completed execution for request {RequestId} with operation {Operation} in {Duration}ms",
                    agentName, requestId, operation, duration.TotalMilliseconds);
            }
            else
            {
                logger.LogWarning("Agent {AgentName} failed execution for request {RequestId} with operation {Operation} after {Duration}ms",
                    agentName, requestId, operation, duration.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Logs agent execution error
        /// </summary>
        public static void LogAgentExecutionError(this ILogger logger, Exception exception, string agentName, 
            string requestId, string operation, TimeSpan duration)
        {
            logger.LogError(exception, "Agent {AgentName} encountered error during execution for request {RequestId} with operation {Operation} after {Duration}ms",
                agentName, requestId, operation, duration.TotalMilliseconds);
        }

        /// <summary>
        /// Logs orchestrator request processing
        /// </summary>
        public static void LogOrchestratorRequest(this ILogger logger, string requestId, string requestType, 
            int agentCount, string? preferredAgent = null)
        {
            logger.LogInformation("Orchestrator processing request {RequestId} of type {RequestType} with {AgentCount} available agents. Preferred agent: {PreferredAgent}",
                requestId, requestType, agentCount, preferredAgent ?? "None");
        }

        /// <summary>
        /// Logs configuration changes
        /// </summary>
        public static void LogConfigurationChange(this ILogger logger, string configurationSection, 
            string? changedBy = null, Dictionary<string, object>? changes = null)
        {
            logger.LogInformation("Configuration changed for section {ConfigurationSection} by {ChangedBy}. Changes: {@Changes}",
                configurationSection, changedBy ?? "System", changes ?? new Dictionary<string, object>());
        }

        /// <summary>
        /// Logs performance metrics
        /// </summary>
        public static void LogPerformanceMetric(this ILogger logger, string metricName, double value, 
            string unit, Dictionary<string, object>? tags = null)
        {
            logger.LogInformation("Performance metric {MetricName}: {Value} {Unit}. Tags: {@Tags}",
                metricName, value, unit, tags ?? new Dictionary<string, object>());
        }

        /// <summary>
        /// Logs system health check results
        /// </summary>
        public static void LogHealthCheck(this ILogger logger, string componentName, bool isHealthy, 
            TimeSpan responseTime, string? details = null)
        {
            var level = isHealthy ? LogLevel.Information : LogLevel.Warning;
            logger.Log(level, "Health check for {ComponentName}: {Status} (Response time: {ResponseTime}ms). Details: {Details}",
                componentName, isHealthy ? "Healthy" : "Unhealthy", responseTime.TotalMilliseconds, details ?? "None");
        }

        /// <summary>
        /// Logs resource usage
        /// </summary>
        public static void LogResourceUsage(this ILogger logger, string resourceType, double currentUsage, 
            double maxUsage, string unit)
        {
            var utilizationPercent = maxUsage > 0 ? (currentUsage / maxUsage) * 100 : 0;
            logger.LogInformation("Resource usage for {ResourceType}: {CurrentUsage}/{MaxUsage} {Unit} ({UtilizationPercent:F1}%)",
                resourceType, currentUsage, maxUsage, unit, utilizationPercent);
        }

        /// <summary>
        /// Logs user actions
        /// </summary>
        public static void LogUserAction(this ILogger logger, string action, string? userId = null, 
            Dictionary<string, object>? context = null)
        {
            logger.LogInformation("User action: {Action} by user {UserId}. Context: {@Context}",
                action, userId ?? "Anonymous", context ?? new Dictionary<string, object>());
        }

        /// <summary>
        /// Logs security events
        /// </summary>
        public static void LogSecurityEvent(this ILogger logger, string eventType, string description, 
            string? userId = null, string? ipAddress = null, bool isSuccessful = true)
        {
            var level = isSuccessful ? LogLevel.Information : LogLevel.Warning;
            logger.Log(level, "Security event: {EventType} - {Description}. User: {UserId}, IP: {IpAddress}, Success: {IsSuccessful}",
                eventType, description, userId ?? "Unknown", ipAddress ?? "Unknown", isSuccessful);
        }

        /// <summary>
        /// Logs external service calls
        /// </summary>
        public static void LogExternalServiceCall(this ILogger logger, string serviceName, string operation, 
            TimeSpan duration, bool success, int? statusCode = null, string? errorMessage = null)
        {
            if (success)
            {
                logger.LogInformation("External service call to {ServiceName}.{Operation} completed successfully in {Duration}ms. Status: {StatusCode}",
                    serviceName, operation, duration.TotalMilliseconds, statusCode);
            }
            else
            {
                logger.LogWarning("External service call to {ServiceName}.{Operation} failed after {Duration}ms. Status: {StatusCode}, Error: {ErrorMessage}",
                    serviceName, operation, duration.TotalMilliseconds, statusCode, errorMessage);
            }
        }
    }
}