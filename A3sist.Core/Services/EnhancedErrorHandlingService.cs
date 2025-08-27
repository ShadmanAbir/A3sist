using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;

namespace A3sist.Core.Services
{
    /// <summary>
    /// Interface for enhanced error handling services
    /// </summary>
    public interface IEnhancedErrorHandlingService
    {
        Task<ErrorHandlingResult> HandleErrorAsync(Exception exception, string context, string? agentName = null);
        Task<ErrorClassification> ClassifyErrorAsync(Exception exception);
        Task<List<ErrorPattern>> GetErrorPatternsAsync();
        Task<ErrorSummaryReport> GenerateErrorReportAsync(TimeSpan? period = null);
        void RecordError(Exception exception, string context, string? agentName = null);
    }

    /// <summary>
    /// Error handling result
    /// </summary>
    public class ErrorHandlingResult
    {
        public bool CanRecover { get; set; }
        public string RecoveryAction { get; set; } = string.Empty;
        public ErrorSeverity Severity { get; set; }
        public string ErrorId { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Error classification result
    /// </summary>
    public class ErrorClassification
    {
        public ErrorCategory Category { get; set; }
        public ErrorSeverity Severity { get; set; }
        public bool IsTransient { get; set; }
        public bool IsRetryable { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> SuggestedActions { get; set; } = new();
    }

    /// <summary>
    /// Error pattern for analysis
    /// </summary>
    public class ErrorPattern
    {
        public string Pattern { get; set; } = string.Empty;
        public ErrorCategory Category { get; set; }
        public int Occurrences { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public List<string> AffectedAgents { get; set; } = new();
    }

    /// <summary>
    /// Comprehensive error summary report
    /// </summary>
    public class ErrorSummaryReport
    {
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan Period { get; set; }
        public int TotalErrors { get; set; }
        public Dictionary<ErrorCategory, int> ErrorsByCategory { get; set; } = new();
        public Dictionary<string, int> ErrorsByAgent { get; set; } = new();
        public List<ErrorPattern> TopErrorPatterns { get; set; } = new();
        public double ErrorRate { get; set; }
        public List<string> Recommendations { get; set; } = new();
    }



    /// <summary>
    /// Enhanced error handling service with pattern analysis and recovery suggestions
    /// </summary>
    public class EnhancedErrorHandlingService : IEnhancedErrorHandlingService
    {
        private readonly ILogger<EnhancedErrorHandlingService> _logger;
        private readonly ConcurrentDictionary<string, ErrorPattern> _errorPatterns;
        private readonly ConcurrentQueue<ErrorRecord> _recentErrors;
        private readonly object _lockObject = new();

        public EnhancedErrorHandlingService(ILogger<EnhancedErrorHandlingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorPatterns = new ConcurrentDictionary<string, ErrorPattern>();
            _recentErrors = new ConcurrentQueue<ErrorRecord>();
        }

        /// <summary>
        /// Handles an error with classification and recovery suggestions
        /// </summary>
        public async Task<ErrorHandlingResult> HandleErrorAsync(Exception exception, string context, string? agentName = null)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            var errorId = GenerateErrorId();
            _logger.LogError(exception, "Handling error {ErrorId} in context: {Context}, Agent: {AgentName}", 
                errorId, context, agentName ?? "Unknown");

            // Record the error
            RecordError(exception, context, agentName);

            // Classify the error
            var classification = await ClassifyErrorAsync(exception);

            // Determine recovery action
            var recoveryAction = DetermineRecoveryAction(exception, classification);

            var result = new ErrorHandlingResult
            {
                CanRecover = classification.IsRetryable,
                RecoveryAction = recoveryAction,
                Severity = classification.Severity,
                ErrorId = errorId,
                Metadata = new Dictionary<string, object>
                {
                    ["ExceptionType"] = exception.GetType().Name,
                    ["Message"] = exception.Message,
                    ["Context"] = context,
                    ["AgentName"] = agentName ?? "Unknown",
                    ["Classification"] = classification,
                    ["Timestamp"] = DateTime.UtcNow
                }
            };

            _logger.LogInformation("Error {ErrorId} handled - CanRecover: {CanRecover}, Severity: {Severity}, Action: {Action}",
                errorId, result.CanRecover, result.Severity, result.RecoveryAction);

            return result;
        }

        /// <summary>
        /// Classifies an error into category and severity
        /// </summary>
        public async Task<ErrorClassification> ClassifyErrorAsync(Exception exception)
        {
            await Task.CompletedTask; // For async consistency

            var classification = new ErrorClassification();

            // Classify by exception type
            classification.Category = exception switch
            {
                ArgumentException or ArgumentNullException or ArgumentOutOfRangeException => ErrorCategory.Validation,
                UnauthorizedAccessException or System.Security.SecurityException => ErrorCategory.Security,
                TimeoutException or TaskCanceledException => ErrorCategory.Network,
                OutOfMemoryException or System.IO.IOException => ErrorCategory.System,
                NotSupportedException or NotImplementedException => ErrorCategory.Configuration,
                HttpRequestException or System.Net.NetworkInformation.NetworkInformationException => ErrorCategory.Network,
                _ => ErrorCategory.Application
            };

            // Determine severity
            classification.Severity = exception switch
            {
                OutOfMemoryException or StackOverflowException => ErrorSeverity.Critical,
                UnauthorizedAccessException or System.Security.SecurityException => ErrorSeverity.Critical,
                TimeoutException or TaskCanceledException => ErrorSeverity.Warning,
                ArgumentException or ArgumentNullException => ErrorSeverity.Info,
                _ => ErrorSeverity.Warning
            };

            // Determine if transient/retryable
            classification.IsTransient = exception is TimeoutException or TaskCanceledException or HttpRequestException;
            classification.IsRetryable = classification.IsTransient && classification.Severity != ErrorSeverity.Critical;

            // Generate description and suggestions
            classification.Description = GenerateErrorDescription(exception, classification);
            classification.SuggestedActions = GenerateSuggestedActions(exception, classification);

            return classification;
        }

        /// <summary>
        /// Gets current error patterns for analysis
        /// </summary>
        public async Task<List<ErrorPattern>> GetErrorPatternsAsync()
        {
            await Task.CompletedTask; // For async consistency
            return _errorPatterns.Values.OrderByDescending(p => p.Occurrences).ToList();
        }

        /// <summary>
        /// Generates a comprehensive error report
        /// </summary>
        public async Task<ErrorSummaryReport> GenerateErrorReportAsync(TimeSpan? period = null)
        {
            await Task.CompletedTask; // For async consistency

            var reportPeriod = period ?? TimeSpan.FromHours(24);
            var cutoffTime = DateTime.UtcNow - reportPeriod;

            lock (_lockObject)
            {
                var recentErrorsList = _recentErrors.Where(e => e.Timestamp >= cutoffTime).ToList();
                
                var report = new ErrorSummaryReport
                {
                    Period = reportPeriod,
                    TotalErrors = recentErrorsList.Count
                };

                // Group by category
                report.ErrorsByCategory = recentErrorsList
                    .GroupBy(e => e.Category)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Group by agent
                report.ErrorsByAgent = recentErrorsList
                    .Where(e => !string.IsNullOrEmpty(e.AgentName))
                    .GroupBy(e => e.AgentName!)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Top error patterns
                report.TopErrorPatterns = _errorPatterns.Values
                    .Where(p => p.LastSeen >= cutoffTime)
                    .OrderByDescending(p => p.Occurrences)
                    .Take(10)
                    .ToList();

                // Calculate error rate (errors per hour)
                report.ErrorRate = reportPeriod.TotalHours > 0 ? report.TotalErrors / reportPeriod.TotalHours : 0;

                // Generate recommendations
                report.Recommendations = GenerateRecommendations(report);

                return report;
            }
        }

        /// <summary>
        /// Records an error for pattern analysis
        /// </summary>
        public void RecordError(Exception exception, string context, string? agentName = null)
        {
            var errorRecord = new ErrorRecord
            {
                ExceptionType = exception.GetType().Name,
                Message = exception.Message,
                Context = context,
                AgentName = agentName,
                Timestamp = DateTime.UtcNow,
                Category = ClassifyErrorSync(exception)
            };

            _recentErrors.Enqueue(errorRecord);

            // Keep only recent errors (last 1000)
            while (_recentErrors.Count > 1000)
            {
                _recentErrors.TryDequeue(out _);
            }

            // Update error patterns
            UpdateErrorPatterns(exception, agentName);
        }

        /// <summary>
        /// Updates error patterns for analysis
        /// </summary>
        private void UpdateErrorPatterns(Exception exception, string? agentName)
        {
            var patternKey = $"{exception.GetType().Name}:{exception.Message.Take(100)}";
            
            _errorPatterns.AddOrUpdate(patternKey,
                new ErrorPattern
                {
                    Pattern = patternKey,
                    Category = ClassifyErrorSync(exception),
                    Occurrences = 1,
                    FirstSeen = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow,
                    AffectedAgents = agentName != null ? new List<string> { agentName } : new List<string>()
                },
                (key, existing) =>
                {
                    existing.Occurrences++;
                    existing.LastSeen = DateTime.UtcNow;
                    if (agentName != null && !existing.AffectedAgents.Contains(agentName))
                    {
                        existing.AffectedAgents.Add(agentName);
                    }
                    return existing;
                });
        }

        /// <summary>
        /// Synchronous error classification for internal use
        /// </summary>
        private ErrorCategory ClassifyErrorSync(Exception exception)
        {
            return exception switch
            {
                ArgumentException or ArgumentNullException => ErrorCategory.Validation,
                UnauthorizedAccessException => ErrorCategory.Security,
                TimeoutException or TaskCanceledException => ErrorCategory.Network,
                OutOfMemoryException => ErrorCategory.System,
                NotSupportedException => ErrorCategory.Configuration,
                HttpRequestException => ErrorCategory.ExternalService,
                _ => ErrorCategory.Application
            };
        }

        /// <summary>
        /// Determines appropriate recovery action for an error
        /// </summary>
        private string DetermineRecoveryAction(Exception exception, ErrorClassification classification)
        {
            return exception switch
            {
                TimeoutException or TaskCanceledException => "Retry with exponential backoff",
                ArgumentException or ArgumentNullException => "Validate input parameters",
                UnauthorizedAccessException => "Check authentication credentials",
                OutOfMemoryException => "Reduce memory usage or restart service",
                HttpRequestException => "Check network connectivity and retry",
                NotSupportedException => "Review configuration settings",
                _ => classification.IsRetryable ? "Retry operation" : "Manual intervention required"
            };
        }

        /// <summary>
        /// Generates human-readable error description
        /// </summary>
        private string GenerateErrorDescription(Exception exception, ErrorClassification classification)
        {
            var description = new StringBuilder();
            description.AppendLine($"Error Type: {exception.GetType().Name}");
            description.AppendLine($"Category: {classification.Category}");
            description.AppendLine($"Severity: {classification.Severity}");
            description.AppendLine($"Message: {exception.Message}");
            
            if (classification.IsRetryable)
                description.AppendLine("This error is typically transient and can be retried.");
            
            return description.ToString();
        }

        /// <summary>
        /// Generates suggested actions based on error type
        /// </summary>
        private List<string> GenerateSuggestedActions(Exception exception, ErrorClassification classification)
        {
            var actions = new List<string>();

            switch (classification.Category)
            {
                case ErrorCategory.Network:
                    actions.Add("Check network connectivity");
                    actions.Add("Verify service endpoints");
                    actions.Add("Consider increasing timeout values");
                    break;
                case ErrorCategory.Authentication:
                    actions.Add("Verify API keys and credentials");
                    actions.Add("Check token expiration");
                    actions.Add("Review access permissions");
                    break;
                case ErrorCategory.Validation:
                    actions.Add("Validate input parameters");
                    actions.Add("Check data format and structure");
                    actions.Add("Review API documentation");
                    break;
                case ErrorCategory.Resource:
                    actions.Add("Monitor system resources");
                    actions.Add("Consider scaling up resources");
                    actions.Add("Implement resource cleanup");
                    break;
                case ErrorCategory.Configuration:
                    actions.Add("Review configuration settings");
                    actions.Add("Check environment variables");
                    actions.Add("Verify feature flags");
                    break;
            }

            if (classification.IsRetryable)
                actions.Add("Retry the operation with exponential backoff");

            return actions;
        }

        /// <summary>
        /// Generates recommendations based on error report
        /// </summary>
        private List<string> GenerateRecommendations(ErrorSummaryReport report)
        {
            var recommendations = new List<string>();

            if (report.ErrorRate > 10)
                recommendations.Add("High error rate detected - consider investigating root causes");

            if (report.ErrorsByCategory.ContainsKey(ErrorCategory.Network) && 
                report.ErrorsByCategory[ErrorCategory.Network] > report.TotalErrors * 0.3)
                recommendations.Add("High network error rate - check connectivity and service health");

            if (report.ErrorsByCategory.ContainsKey(ErrorCategory.Resource) &&
                report.ErrorsByCategory[ErrorCategory.Resource] > 0)
                recommendations.Add("Resource errors detected - monitor system resources");

            var topAgent = report.ErrorsByAgent.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
            if (!string.IsNullOrEmpty(topAgent.Key) && topAgent.Value > report.TotalErrors * 0.5)
                recommendations.Add($"Agent '{topAgent.Key}' has high error rate - investigate agent-specific issues");

            return recommendations;
        }

        /// <summary>
        /// Generates a unique error ID
        /// </summary>
        private string GenerateErrorId()
        {
            return $"ERR_{DateTime.UtcNow:yyyyMMdd}_{Guid.NewGuid():N}";
        }

        /// <summary>
        /// Internal error record for tracking
        /// </summary>
        private class ErrorRecord
        {
            public string ExceptionType { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string Context { get; set; } = string.Empty;
            public string? AgentName { get; set; }
            public DateTime Timestamp { get; set; }
            public ErrorCategory Category { get; set; }
        }
    }
}