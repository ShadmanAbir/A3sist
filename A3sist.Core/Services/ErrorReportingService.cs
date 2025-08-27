using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using A3sist.Shared.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ErrorReport = A3sist.Shared.Models.ErrorReport;

namespace A3sist.Core.Services
{
    /// <summary>
    /// Service for collecting, categorizing, and reporting errors
    /// </summary>
    public class ErrorReportingService : IErrorReportingService, IDisposable
    {
        private readonly ILogger<ErrorReportingService> _logger;
        private readonly IConfiguration? _configuration;
        private readonly ConcurrentQueue<ErrorReport> _errorQueue;
        private readonly ConcurrentDictionary<string, List<ErrorReport>> _errorStorage;
        private readonly Timer _cleanupTimer;
        private readonly DateTime _startTime;
        private bool _disposed;

        // Configuration
        private readonly TimeSpan _errorRetentionPeriod = TimeSpan.FromDays(30);
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6);
        private readonly int _maxErrorsPerHash = 1000;
        private readonly int _maxTotalErrors = 50000;

        public ErrorReportingService(ILogger<ErrorReportingService> logger, IConfiguration? configuration = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration;
            _errorQueue = new ConcurrentQueue<ErrorReport>();
            _errorStorage = new ConcurrentDictionary<string, List<ErrorReport>>();
            _startTime = DateTime.UtcNow;

            // Start background cleanup timer
            _cleanupTimer = new Timer(async _ => await CleanupErrorReportsAsync(), null, _cleanupInterval, _cleanupInterval);

            _logger.LogInformation("Error reporting service initialized");
        }

        public async Task ReportErrorAsync(ErrorReport errorReport)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ErrorReportingService));

            if (errorReport == null)
                throw new ArgumentNullException(nameof(errorReport));

            // Enrich the error report
            await EnrichErrorReportAsync(errorReport);

            // Generate error hash for deduplication
            errorReport.ErrorHash = GenerateErrorHash(errorReport);

            _errorQueue.Enqueue(errorReport);
            await ProcessQueuedErrorsAsync();

            _logger.LogDebug("Error report queued: {ErrorId} - {Message}", errorReport.Id, errorReport.Message);
        }

        public async Task ReportExceptionAsync(Exception exception, Dictionary<string, object>? context = null,
            ErrorSeverity severity = ErrorSeverity.Error, string? component = null)
        {
            var errorReport = new ErrorReport
            {
                Message = exception.Message,
                Details = exception.ToString(),
                Exception = CreateExceptionInfo(exception),
                Severity = severity,
                Category = CategorizeException(exception),
                Component = component,
                Context = context ?? new Dictionary<string, object>(),
                StackTrace = exception.StackTrace
            };

            await ReportErrorAsync(errorReport);
        }

        public async Task ReportErrorAsync(string message, ErrorCategory category = ErrorCategory.Application,
            ErrorSeverity severity = ErrorSeverity.Error, Dictionary<string, object>? context = null,
            string? component = null)
        {
            var errorReport = new ErrorReport
            {
                Message = message,
                Severity = severity,
                Category = category,
                Component = component,
                Context = context ?? new Dictionary<string, object>()
            };

            await ReportErrorAsync(errorReport);
        }

        public async Task<IEnumerable<ErrorReport>> GetErrorReportsAsync(DateTime? startTime = null, DateTime? endTime = null,
            ErrorSeverity? severity = null, ErrorCategory? category = null, string? component = null, int limit = 100)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ErrorReportingService));

            await ProcessQueuedErrorsAsync();

            var allErrors = new List<ErrorReport>();
            foreach (var kvp in _errorStorage)
            {
                allErrors.AddRange(kvp.Value);
            }

            // Apply filters
            var filteredErrors = allErrors.AsEnumerable();

            if (startTime.HasValue)
                filteredErrors = filteredErrors.Where(e => e.Timestamp >= startTime.Value);

            if (endTime.HasValue)
                filteredErrors = filteredErrors.Where(e => e.Timestamp <= endTime.Value);

            if (severity.HasValue)
                filteredErrors = filteredErrors.Where(e => e.Severity == severity.Value);

            if (category.HasValue)
                filteredErrors = filteredErrors.Where(e => e.Category == category.Value);

            if (!string.IsNullOrEmpty(component))
                filteredErrors = filteredErrors.Where(e => e.Component == component);

            return filteredErrors
                .OrderByDescending(e => e.Timestamp)
                .Take(limit);
        }

        public async Task<ErrorStatistics> GetErrorStatisticsAsync(DateTime? startTime = null, DateTime? endTime = null)
        {
            var errors = await GetErrorReportsAsync(startTime, endTime, limit: int.MaxValue);
            var errorList = errors.ToList();

            var stats = new ErrorStatistics
            {
                StartTime = startTime ?? errorList.OrderBy(e => e.Timestamp).FirstOrDefault()?.Timestamp ?? DateTime.UtcNow,
                EndTime = endTime ?? errorList.OrderByDescending(e => e.Timestamp).FirstOrDefault()?.Timestamp ?? DateTime.UtcNow,
                TotalErrors = errorList.Count
            };

            // Calculate time span for rate calculation
            var timeSpan = stats.EndTime - stats.StartTime;
            stats.ErrorRatePerHour = timeSpan.TotalHours > 0 ? stats.TotalErrors / timeSpan.TotalHours : 0;

            // Group by severity
            stats.ErrorsBySeverity = errorList
                .GroupBy(e => e.Severity)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by category
            stats.ErrorsByCategory = errorList
                .GroupBy(e => e.Category)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by component
            stats.ErrorsByComponent = errorList
                .Where(e => !string.IsNullOrEmpty(e.Component))
                .GroupBy(e => e.Component!)
                .ToDictionary(g => g.Key, g => g.Count());

            // Most common errors
            stats.MostCommonErrors = errorList
                .GroupBy(e => e.ErrorHash)
                .Select(g => new ErrorTypeSummary
                {
                    ErrorType = g.First().Message,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / stats.TotalErrors * 100,
                    FirstOccurrence = g.Min(e => e.Timestamp),
                    LastOccurrence = g.Max(e => e.Timestamp),
                    MostCommonSeverity = g.GroupBy(e => e.Severity).OrderByDescending(sg => sg.Count()).First().Key,
                    AffectedComponents = g.Where(e => !string.IsNullOrEmpty(e.Component)).Select(e => e.Component!).Distinct().ToList()
                })
                .OrderByDescending(s => s.Count)
                .Take(10)
                .ToList();

            // Resolution statistics
            var resolvedErrors = errorList.Count(e => e.IsResolved);
            stats.ResolutionStats = new ResolutionStatistics
            {
                ResolvedErrors = resolvedErrors,
                UnresolvedErrors = stats.TotalErrors - resolvedErrors,
                ResolutionRate = stats.TotalErrors > 0 ? (double)resolvedErrors / stats.TotalErrors * 100 : 0
            };

            return stats;
        }



        public async Task<IEnumerable<FrequentErrorSummary>> GetFrequentErrorsAsync(DateTime? startTime = null,
            DateTime? endTime = null, int limit = 10)
        {
            var errors = await GetErrorReportsAsync(startTime, endTime, limit: int.MaxValue);
            
            return errors
                .GroupBy(e => e.ErrorHash)
                .Where(g => g.Count() > 1) // Only frequent errors
                .Select(g => new FrequentErrorSummary
                {
                    ErrorHash = g.Key,
                    Message = g.First().Message,
                    ExceptionType = g.First().Exception?.Type,
                    Occurrences = g.Count(),
                    FirstSeen = g.Min(e => e.Timestamp),
                    LastSeen = g.Max(e => e.Timestamp),
                    Severity = g.GroupBy(e => e.Severity).OrderByDescending(sg => sg.Count()).First().Key,
                    Category = g.GroupBy(e => e.Category).OrderByDescending(cg => cg.Count()).First().Key,
                    Components = g.Where(e => !string.IsNullOrEmpty(e.Component)).Select(e => e.Component!).Distinct().ToList(),
                    IsResolved = g.All(e => e.IsResolved),
                    Trend = CalculateTrend(g.OrderBy(e => e.Timestamp).ToList())
                })
                .OrderByDescending(s => s.Occurrences)
                .Take(limit);
        }

        public async Task<DiagnosticInfo> CollectDiagnosticInfoAsync()
        {
            var diagnostics = new DiagnosticInfo();

            try
            {
                // Application diagnostics
                diagnostics.Application = await CollectApplicationDiagnosticsAsync();

                // System diagnostics
                diagnostics.System = await CollectSystemDiagnosticsAsync();

                // Performance diagnostics
                diagnostics.Performance = await CollectPerformanceDiagnosticsAsync();

                // Configuration diagnostics
                diagnostics.Configuration = await CollectConfigurationDiagnosticsAsync();

                // Error diagnostics
                diagnostics.Errors = await CollectErrorDiagnosticsAsync();

                // Environment variables (filtered)
                diagnostics.EnvironmentVariables = CollectFilteredEnvironmentVariables();

                // Loaded assemblies
                diagnostics.LoadedAssemblies = CollectAssemblyInfo();

                // Network diagnostics
                diagnostics.Network = await CollectNetworkDiagnosticsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect some diagnostic information");
            }

            return diagnostics;
        }

        public async Task ExportErrorReportsAsync(string filePath, ExportFormat format = ExportFormat.Json,
            DateTime? startTime = null, DateTime? endTime = null)
        {
            var errors = await GetErrorReportsAsync(startTime, endTime, limit: int.MaxValue);

            switch (format)
            {
                case ExportFormat.Json:
                    await ExportAsJsonAsync(errors, filePath);
                    break;
                case ExportFormat.Csv:
                    await ExportAsCsvAsync(errors, filePath);
                    break;
                case ExportFormat.Xml:
                    await ExportAsXmlAsync(errors, filePath);
                    break;
                default:
                    throw new ArgumentException($"Unsupported export format: {format}");
            }

            _logger.LogInformation("Exported {ErrorCount} error reports to {FilePath} in {Format} format",
                errors.Count(), filePath, format);
        }

        public async Task CleanupErrorReportsAsync()
        {
            if (_disposed)
                return;

            try
            {
                var cutoffTime = DateTime.UtcNow - _errorRetentionPeriod;
                var removedCount = 0;
                var totalErrors = 0;

                foreach (var kvp in _errorStorage.ToList())
                {
                    var errors = kvp.Value;
                    var originalCount = errors.Count;
                    totalErrors += originalCount;

                    // Remove old errors
                    errors.RemoveAll(e => e.Timestamp < cutoffTime);

                    // Limit errors per hash
                    if (errors.Count > _maxErrorsPerHash)
                    {
                        errors.RemoveRange(0, errors.Count - _maxErrorsPerHash);
                    }

                    removedCount += originalCount - errors.Count;

                    // Remove empty entries
                    if (!errors.Any())
                    {
                        _errorStorage.TryRemove(kvp.Key, out _);
                    }
                }

                // If total errors exceed limit, remove oldest errors
                if (totalErrors > _maxTotalErrors)
                {
                    var allErrors = new List<(string hash, ErrorReport error)>();
                    foreach (var kvp in _errorStorage)
                    {
                        foreach (var error in kvp.Value)
                        {
                            allErrors.Add((kvp.Key, error));
                        }
                    }

                    var sortedErrors = allErrors.OrderBy(e => e.error.Timestamp).ToList();
                    var errorsToRemove = sortedErrors.Take(totalErrors - _maxTotalErrors);

                    foreach (var (hash, error) in errorsToRemove)
                    {
                        if (_errorStorage.TryGetValue(hash, out var errorList))
                        {
                            errorList.Remove(error);
                            removedCount++;
                        }
                    }
                }

                if (removedCount > 0)
                {
                    _logger.LogInformation("Cleaned up {RemovedCount} old error reports", removedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup error reports");
            }

            await Task.CompletedTask;
        }

        public async Task<ErrorAnalysis> AnalyzeErrorPatternsAsync(DateTime? startTime = null, DateTime? endTime = null)
        {
            var errors = await GetErrorReportsAsync(startTime, endTime, limit: int.MaxValue);
            var errorList = errors.ToList();

            var analysis = new ErrorAnalysis
            {
                StartTime = startTime ?? errorList.OrderBy(e => e.Timestamp).FirstOrDefault()?.Timestamp ?? DateTime.UtcNow,
                EndTime = endTime ?? errorList.OrderByDescending(e => e.Timestamp).FirstOrDefault()?.Timestamp ?? DateTime.UtcNow
            };

            // Analyze patterns
            analysis.ErrorPatterns = AnalyzePatterns(errorList);

            // Generate insights
            analysis.KeyInsights = GenerateInsights(errorList, analysis.ErrorPatterns);

            // Generate recommendations
            analysis.Recommendations = GenerateRecommendations(errorList, analysis.ErrorPatterns);

            // Assess risk
            analysis.RiskAssessment = AssessRisk(errorList, analysis.ErrorPatterns);

            return analysis;
        }

        private async Task EnrichErrorReportAsync(ErrorReport errorReport)
        {
            // Set timestamp if not already set
            if (errorReport.Timestamp == default)
                errorReport.Timestamp = DateTime.UtcNow;

            // Collect system context
            errorReport.SystemContext = new SystemContext
            {
                MachineName = Environment.MachineName,
                OSVersion = Environment.OSVersion.ToString(),
                RuntimeVersion = Environment.Version.ToString(),
                WorkingDirectory = Environment.CurrentDirectory,
                UserName = Environment.UserName,
                ProcessId = Process.GetCurrentProcess().Id,
                ThreadId = Environment.CurrentManagedThreadId
            };

            // Try to get application version
            try
            {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                errorReport.SystemContext.ApplicationVersion = assembly.GetName().Version?.ToString();
            }
            catch
            {
                // Ignore if we can't get version
            }

            await Task.CompletedTask;
        }

        private ExceptionInfo CreateExceptionInfo(Exception exception)
        {
            var info = new ExceptionInfo
            {
                Type = exception.GetType().FullName ?? exception.GetType().Name,
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                Source = exception.Source,
                TargetSite = exception.TargetSite?.ToString()
            };

            // Copy exception data
            foreach (var key in exception.Data.Keys)
            {
                try
                {
                    info.Data[key.ToString() ?? ""] = exception.Data[key]?.ToString() ?? "";
                }
                catch
                {
                    // Ignore if we can't serialize the data
                }
            }

            // Handle inner exception
            if (exception.InnerException != null)
            {
                info.InnerException = CreateExceptionInfo(exception.InnerException);
            }

            return info;
        }

        private ErrorCategory CategorizeException(Exception exception)
        {
            return exception switch
            {
                ArgumentException or ArgumentNullException or ArgumentOutOfRangeException => ErrorCategory.Validation,
                UnauthorizedAccessException or System.Security.SecurityException => ErrorCategory.Security,
                System.Net.NetworkInformation.NetworkInformationException or System.Net.WebException => ErrorCategory.Network,
                System.Data.Common.DbException => ErrorCategory.Database,
                System.Configuration.ConfigurationException => ErrorCategory.Configuration,
                OutOfMemoryException or StackOverflowException => ErrorCategory.Performance,
                _ => ErrorCategory.Application
            };
        }

        private string GenerateErrorHash(ErrorReport errorReport)
        {
            var hashInput = $"{errorReport.Message}|{errorReport.Exception?.Type}|{errorReport.Component}|{errorReport.Category}";
            
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
            var hexString = BitConverter.ToString(hashBytes).Replace("-", "");
            return hexString.Substring(0, Math.Min(16, hexString.Length)); // Use first 16 characters
        }

        private async Task ProcessQueuedErrorsAsync()
        {
            while (_errorQueue.TryDequeue(out var errorReport))
            {
                var errorList = _errorStorage.GetOrAdd(errorReport.ErrorHash, _ => new List<ErrorReport>());
                
                lock (errorList)
                {
                    errorList.Add(errorReport);
                    
                    // Limit errors per hash
                    if (errorList.Count > _maxErrorsPerHash)
                    {
                        errorList.RemoveAt(0);
                    }
                }
            }

            await Task.CompletedTask;
        }

        private TrendDirection CalculateTrend(List<ErrorReport> errors)
        {
            if (errors.Count < 2)
                return TrendDirection.Unknown;

            var midpoint = errors.Count / 2;
            var firstHalf = errors.Take(midpoint).Count();
            var secondHalf = errors.Skip(midpoint).Count();

            if (secondHalf > firstHalf * 1.2)
                return TrendDirection.Increasing;
            else if (secondHalf < firstHalf * 0.8)
                return TrendDirection.Decreasing;
            else
                return TrendDirection.Stable;
        }

        // Additional helper methods for diagnostic collection would be implemented here...
        // For brevity, I'll include just the signatures and basic implementations

        private async Task<ApplicationDiagnostics> CollectApplicationDiagnosticsAsync()
        {
            return new ApplicationDiagnostics
            {
                StartupTime = _startTime,
                Uptime = DateTime.UtcNow - _startTime,
                CommandLineArgs = Environment.GetCommandLineArgs()
            };
        }

        private async Task<SystemDiagnostics> CollectSystemDiagnosticsAsync()
        {
            return new SystemDiagnostics
            {
                SystemBootTime = DateTime.UtcNow - TimeSpan.FromMilliseconds(Environment.TickCount)
            };
        }

        private async Task<PerformanceDiagnostics> CollectPerformanceDiagnosticsAsync()
        {
            var process = Process.GetCurrentProcess();
            return new PerformanceDiagnostics
            {
                CurrentProcess = new ProcessInfo
                {
                    Id = process.Id,
                    Name = process.ProcessName,
                    StartTime = process.StartTime,
                    WorkingSet = process.WorkingSet64,
                    ThreadCount = process.Threads.Count
                }
            };
        }

        private async Task<ConfigurationDiagnostics> CollectConfigurationDiagnosticsAsync()
        {
            return new ConfigurationDiagnostics();
        }

        private async Task<ErrorDiagnostics> CollectErrorDiagnosticsAsync()
        {
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var oneDayAgo = DateTime.UtcNow.AddDays(-1);
            
            var recentErrors = await GetErrorReportsAsync(oneDayAgo, limit: 10);
            
            return new ErrorDiagnostics
            {
                ErrorsLastHour = (await GetErrorReportsAsync(oneHourAgo, limit: int.MaxValue)).Count(),
                ErrorsLastDay = recentErrors.Count(),
                RecentErrors = recentErrors.Take(5).Select(e => new RecentErrorSummary
                {
                    Timestamp = e.Timestamp,
                    Message = e.Message,
                    Severity = e.Severity,
                    Component = e.Component
                }).ToList()
            };
        }

        private Dictionary<string, string> CollectFilteredEnvironmentVariables()
        {
            var filtered = new Dictionary<string, string>();
            
            // Get sensitive patterns from configuration, with fallback to default
            var sensitiveKeys = _configuration?.GetSection("A3sist:Security:SensitiveDataPatterns")
                .Get<string[]>() ?? new[] { "password", "secret", "key", "token", "credential" };
            
            foreach (var kvp in Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>())
            {
                var key = kvp.Key.ToString() ?? "";
                var value = kvp.Value?.ToString() ?? "";
                
                if (sensitiveKeys.Any(s => key.ToLowerInvariant().Contains(s)))
                {
                    value = "***FILTERED***";
                }
                
                filtered[key] = value;
            }
            
            return filtered;
        }

        private List<AssemblyInfo> CollectAssemblyInfo()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => new AssemblyInfo
                {
                    Name = a.GetName().Name ?? "",
                    Version = a.GetName().Version?.ToString() ?? "",
                    Location = a.IsDynamic ? "Dynamic" : a.Location
                })
                .ToList();
        }

        private async Task<NetworkDiagnostics> CollectNetworkDiagnosticsAsync()
        {
            return new NetworkDiagnostics
            {
                IsNetworkAvailable = NetworkInterface.GetIsNetworkAvailable()
            };
        }

        private List<A3sist.Shared.Models.ErrorPattern> AnalyzePatterns(List<ErrorReport> errors)
        {
            // Basic pattern analysis - could be enhanced with ML
            return new List<A3sist.Shared.Models.ErrorPattern>();
        }

        private List<string> GenerateInsights(List<ErrorReport> errors, List<A3sist.Shared.Models.ErrorPattern> patterns)
        {
            var insights = new List<string>();
            
            if (errors.Any())
            {
                var mostCommonSeverity = errors.GroupBy(e => e.Severity).OrderByDescending(g => g.Count()).First();
                insights.Add($"Most common error severity: {mostCommonSeverity.Key} ({mostCommonSeverity.Count()} occurrences)");
            }
            
            return insights;
        }

        private List<string> GenerateRecommendations(List<ErrorReport> errors, List<A3sist.Shared.Models.ErrorPattern> patterns)
        {
            var recommendations = new List<string>();
            
            if (errors.Count(e => e.Severity >= ErrorSeverity.Error) > 10)
            {
                recommendations.Add("Consider implementing more robust error handling and validation");
            }
            
            return recommendations;
        }

        private RiskAssessment AssessRisk(List<ErrorReport> errors, List<A3sist.Shared.Models.ErrorPattern> patterns)
        {
            var criticalErrors = errors.Count(e => e.Severity >= ErrorSeverity.Critical);
            var riskLevel = criticalErrors > 5 ? RiskLevel.High : 
                           criticalErrors > 1 ? RiskLevel.Medium : RiskLevel.Low;
            
            return new RiskAssessment
            {
                OverallRisk = riskLevel
            };
        }

        private async Task ExportAsJsonAsync(IEnumerable<ErrorReport> errors, string filePath)
        {
            var json = JsonSerializer.Serialize(errors, new JsonSerializerOptions { WriteIndented = true });
            await Task.Run(() => File.WriteAllText(filePath, json));
        }

        private async Task ExportAsCsvAsync(IEnumerable<ErrorReport> errors, string filePath)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Timestamp,Message,Severity,Category,Component");
            
            foreach (var error in errors)
            {
                csv.AppendLine($"{error.Timestamp:yyyy-MM-dd HH:mm:ss},{error.Message},{error.Severity},{error.Category},{error.Component}");
            }
            
            await Task.Run(() => File.WriteAllText(filePath, csv.ToString()));
        }

        private async Task ExportAsXmlAsync(IEnumerable<ErrorReport> errors, string filePath)
        {
            // Basic XML export - could be enhanced
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            xml.AppendLine("<ErrorReports>");
            
            foreach (var error in errors)
            {
                xml.AppendLine($"  <ErrorReport>");
                xml.AppendLine($"    <Timestamp>{error.Timestamp:yyyy-MM-dd HH:mm:ss}</Timestamp>");
                xml.AppendLine($"    <Message>{System.Security.SecurityElement.Escape(error.Message)}</Message>");
                xml.AppendLine($"    <Severity>{error.Severity}</Severity>");
                xml.AppendLine($"    <Category>{error.Category}</Category>");
                xml.AppendLine($"    <Component>{error.Component}</Component>");
                xml.AppendLine($"  </ErrorReport>");
            }
            
            xml.AppendLine("</ErrorReports>");
            await Task.Run(() => File.WriteAllText(filePath, xml.ToString()));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _cleanupTimer?.Dispose();
            _disposed = true;
            _logger.LogInformation("Error reporting service disposed");
        }
    }
}