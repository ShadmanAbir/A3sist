using A3sist.Core.Agents.Base;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace A3sist.Core.Agents.TaskAgents.FailureTracker
{
    /// <summary>
    /// FailureTracker agent responsible for error tracking, failure pattern recognition, and recovery suggestions
    /// </summary>
    public class FailureTrackerAgent : BaseAgent
    {
        private readonly ConcurrentDictionary<string, FailurePattern> _failurePatterns;
        private readonly ConcurrentDictionary<Guid, FailureRecord> _failureRecords;
        private readonly ConcurrentDictionary<string, RecoveryStrategy> _recoveryStrategies;
        private readonly Timer _analysisTimer;
        private readonly object _analysisLock = new();

        public override string Name => "FailureTracker";
        public override AgentType Type => AgentType.Fixer; // Using Fixer type as it's the closest match

        public FailureTrackerAgent(
            ILogger<FailureTrackerAgent> logger,
            IAgentConfiguration configuration) : base(logger, configuration)
        {
            _failurePatterns = new ConcurrentDictionary<string, FailurePattern>();
            _failureRecords = new ConcurrentDictionary<Guid, FailureRecord>();
            _recoveryStrategies = new ConcurrentDictionary<string, RecoveryStrategy>();
            
            // Initialize built-in recovery strategies
            InitializeRecoveryStrategies();
            
            // Start periodic analysis (every 5 minutes)
            _analysisTimer = new Timer(PerformPatternAnalysis, null, 
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        protected override async Task<bool> CanHandleRequestAsync(AgentRequest request)
        {
            if (request == null)
                return false;

            // FailureTracker can handle error tracking, failure analysis, and recovery suggestions
            var supportedActions = new[]
            {
                "track", "error", "failure", "analyze", "recover", "pattern", "suggest",
                "diagnose", "troubleshoot", "fix", "resolve", "investigate"
            };

            var prompt = request.Prompt?.ToLowerInvariant() ?? "";
            var hasException = request.Context?.ContainsKey("exception") == true ||
                              request.Context?.ContainsKey("error") == true;

            return supportedActions.Any(action => prompt.Contains(action)) || hasException;
        }

        protected override async Task<AgentResult> HandleRequestAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var action = ExtractActionFromRequest(request);
                
                return action.ToLowerInvariant() switch
                {
                    "track" or "record" => await TrackFailureAsync(request, cancellationToken),
                    "analyze" or "pattern" => await AnalyzeFailurePatternsAsync(request, cancellationToken),
                    "recover" or "suggest" => await SuggestRecoveryAsync(request, cancellationToken),
                    "diagnose" or "investigate" => await DiagnoseFailureAsync(request, cancellationToken),
                    "statistics" or "report" => await GenerateFailureReportAsync(request, cancellationToken),
                    _ => await HandleGenericFailureAsync(request, cancellationToken)
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error handling failure tracker request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"FailureTracker error: {ex.Message}", ex, Name);
            }
        }

        protected override async System.Threading.Tasks.Task InitializeAgentAsync()
        {
            Logger.LogInformation("Initializing FailureTracker agent");
            
            // Load any persisted failure patterns and recovery strategies
            await LoadPersistedDataAsync();
            
            Logger.LogInformation("FailureTracker agent initialized with {PatternCount} patterns and {StrategyCount} recovery strategies", 
                _failurePatterns.Count, _recoveryStrategies.Count);
        }

        protected override async System.Threading.Tasks.Task ShutdownAgentAsync()
        {
            Logger.LogInformation("Shutting down FailureTracker agent");
            
            // Persist current data
            await PersistDataAsync();
            
            _analysisTimer?.Dispose();
            
            Logger.LogInformation("FailureTracker agent shutdown completed");
        }

        private async Task<AgentResult> TrackFailureAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Tracking failure for request {RequestId}", request.Id);

            try
            {
                var failureRecord = CreateFailureRecord(request);
                _failureRecords.TryAdd(failureRecord.Id, failureRecord);
                
                // Update failure patterns
                await UpdateFailurePatternsAsync(failureRecord);
                
                var result = new
                {
                    FailureId = failureRecord.Id,
                    Category = failureRecord.Category,
                    Severity = failureRecord.Severity.ToString(),
                    Timestamp = failureRecord.Timestamp,
                    PatternMatches = failureRecord.PatternMatches,
                    SuggestedRecovery = failureRecord.SuggestedRecovery
                };

                return AgentResult.CreateSuccess(
                    $"Failure tracked successfully. Category: {failureRecord.Category}, Severity: {failureRecord.Severity}",
                    JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
                    Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to track failure for request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Failed to track failure: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> AnalyzeFailurePatternsAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Analyzing failure patterns for request {RequestId}", request.Id);

            try
            {
                var timeRange = ExtractTimeRangeFromRequest(request);
                var relevantFailures = GetFailuresInTimeRange(timeRange.Start, timeRange.End);
                
                var patternAnalysis = await AnalyzePatternsAsync(relevantFailures);
                
                var result = new
                {
                    AnalysisPeriod = new
                    {
                        Start = timeRange.Start,
                        End = timeRange.End
                    },
                    TotalFailures = relevantFailures.Count,
                    UniquePatterns = patternAnalysis.Patterns.Count,
                    TopPatterns = patternAnalysis.Patterns
                        .OrderByDescending(p => p.Frequency)
                        .Take(10)
                        .Select(p => new
                        {
                            p.Name,
                            p.Description,
                            p.Frequency,
                            p.Severity,
                            p.Category,
                            p.LastOccurrence,
                            p.TrendDirection,
                            RecoverySuccess = p.RecoveryAttempts > 0 ? 
                                (double)p.SuccessfulRecoveries / p.RecoveryAttempts : 0
                        }),
                    Recommendations = patternAnalysis.Recommendations
                };

                return AgentResult.CreateSuccess(
                    $"Pattern analysis completed. Found {patternAnalysis.Patterns.Count} unique patterns from {relevantFailures.Count} failures.",
                    JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
                    Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to analyze failure patterns for request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Pattern analysis failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> SuggestRecoveryAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Suggesting recovery for request {RequestId}", request.Id);

            try
            {
                var failureInfo = ExtractFailureInfoFromRequest(request);
                var matchingPatterns = FindMatchingPatterns(failureInfo);
                var recoveryOptions = GenerateRecoveryOptions(matchingPatterns, failureInfo);
                
                var result = new
                {
                    FailureInfo = new
                    {
                        failureInfo.ErrorMessage,
                        failureInfo.Category,
                        failureInfo.Context
                    },
                    MatchingPatterns = matchingPatterns.Select(p => new
                    {
                        p.Name,
                        p.Description,
                        p.Frequency,
                        ConfidenceScore = CalculatePatternConfidence(p, failureInfo)
                    }),
                    RecoveryOptions = recoveryOptions.Select(r => new
                    {
                        r.Name,
                        r.Description,
                        r.Steps,
                        r.SuccessRate,
                        r.EstimatedTime,
                        r.RiskLevel,
                        r.Prerequisites
                    }).OrderByDescending(r => r.SuccessRate),
                    AutoRecoveryAvailable = recoveryOptions.Any(r => r.CanAutoExecute),
                    RecommendedAction = recoveryOptions.OrderByDescending(r => r.SuccessRate).FirstOrDefault()?.Name ?? "Manual investigation required"
                };

                return AgentResult.CreateSuccess(
                    $"Found {recoveryOptions.Count} recovery options for the failure. Recommended: {result.RecommendedAction}",
                    JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
                    Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to suggest recovery for request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Recovery suggestion failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> DiagnoseFailureAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Diagnosing failure for request {RequestId}", request.Id);

            try
            {
                var failureInfo = ExtractFailureInfoFromRequest(request);
                var diagnosis = await PerformDiagnosisAsync(failureInfo);
                
                var result = new
                {
                    Diagnosis = new
                    {
                        diagnosis.PrimaryCategory,
                        diagnosis.Severity,
                        diagnosis.Confidence,
                        diagnosis.RootCause,
                        diagnosis.ContributingFactors
                    },
                    SimilarFailures = diagnosis.SimilarFailures.Select(f => new
                    {
                        f.Id,
                        f.Timestamp,
                        f.Category,
                        f.Resolution,
                        Similarity = CalculateSimilarity(failureInfo, f)
                    }),
                    ImpactAssessment = new
                    {
                        diagnosis.AffectedComponents,
                        diagnosis.BusinessImpact,
                        diagnosis.TechnicalImpact,
                        diagnosis.UserImpact
                    },
                    NextSteps = diagnosis.RecommendedActions
                };

                return AgentResult.CreateSuccess(
                    $"Diagnosis completed. Primary category: {diagnosis.PrimaryCategory}, Confidence: {diagnosis.Confidence:P0}",
                    JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
                    Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to diagnose failure for request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Failure diagnosis failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> GenerateFailureReportAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Generating failure report for request {RequestId}", request.Id);

            try
            {
                var timeRange = ExtractTimeRangeFromRequest(request);
                var failures = GetFailuresInTimeRange(timeRange.Start, timeRange.End);
                
                var report = new
                {
                    ReportPeriod = new
                    {
                        Start = timeRange.Start,
                        End = timeRange.End,
                        Duration = timeRange.End - timeRange.Start
                    },
                    Summary = new
                    {
                        TotalFailures = failures.Count,
                        UniqueCategories = failures.Select(f => f.Category).Distinct().Count(),
                        CriticalFailures = failures.Count(f => f.Severity == FailureSeverity.Critical),
                        HighFailures = failures.Count(f => f.Severity == FailureSeverity.High),
                        MediumFailures = failures.Count(f => f.Severity == FailureSeverity.Medium),
                        LowFailures = failures.Count(f => f.Severity == FailureSeverity.Low)
                    },
                    CategoryBreakdown = failures
                        .GroupBy(f => f.Category)
                        .Select(g => new
                        {
                            Category = g.Key,
                            Count = g.Count(),
                            Percentage = (double)g.Count() / failures.Count * 100,
                            AverageSeverity = g.Average(f => (int)f.Severity)
                        })
                        .OrderByDescending(c => c.Count),
                    TrendAnalysis = AnalyzeTrends(failures),
                    TopPatterns = _failurePatterns.Values
                        .Where(p => p.LastOccurrence >= timeRange.Start)
                        .OrderByDescending(p => p.Frequency)
                        .Take(5)
                        .Select(p => new
                        {
                            p.Name,
                            p.Frequency,
                            p.Severity,
                            p.TrendDirection
                        }),
                    RecoveryEffectiveness = CalculateRecoveryEffectiveness(failures)
                };

                return AgentResult.CreateSuccess(
                    $"Failure report generated for period {timeRange.Start:yyyy-MM-dd} to {timeRange.End:yyyy-MM-dd}. Total failures: {failures.Count}",
                    JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }),
                    Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to generate failure report for request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Report generation failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> HandleGenericFailureAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            // Default behavior: track the failure and provide basic analysis
            var trackResult = await TrackFailureAsync(request, cancellationToken);
            if (!trackResult.Success)
                return trackResult;

            var analyzeResult = await SuggestRecoveryAsync(request, cancellationToken);
            
            var combinedResult = new
            {
                FailureTracked = JsonSerializer.Deserialize<object>(trackResult.Content ?? "{}"),
                RecoverySuggestions = JsonSerializer.Deserialize<object>(analyzeResult.Content ?? "{}")
            };

            return AgentResult.CreateSuccess(
                "Failure tracked and recovery suggestions provided",
                JsonSerializer.Serialize(combinedResult, new JsonSerializerOptions { WriteIndented = true }),
                Name);
        }

        private FailureRecord CreateFailureRecord(AgentRequest request)
        {
            var failureInfo = ExtractFailureInfoFromRequest(request);
            var category = CategorizeFailure(failureInfo);
            var severity = DetermineSeverity(failureInfo);
            
            var record = new FailureRecord
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                RequestId = request.Id,
                Category = category,
                Severity = severity,
                ErrorMessage = failureInfo.ErrorMessage,
                StackTrace = failureInfo.StackTrace,
                Context = failureInfo.Context,
                Component = failureInfo.Component,
                PatternMatches = FindMatchingPatterns(failureInfo).Select(p => p.Name).ToList()
            };

            // Generate recovery suggestions
            var recoveryOptions = GenerateRecoveryOptions(FindMatchingPatterns(failureInfo), failureInfo);
            record.SuggestedRecovery = recoveryOptions.FirstOrDefault()?.Name ?? "Manual investigation required";

            return record;
        }

        private async System.Threading.Tasks.Task UpdateFailurePatternsAsync(FailureRecord failure)
        {
            var patternKey = GeneratePatternKey(failure);
            
            _failurePatterns.AddOrUpdate(patternKey, 
                new FailurePattern
                {
                    Name = patternKey,
                    Description = GeneratePatternDescription(failure),
                    Category = failure.Category,
                    Severity = failure.Severity,
                    Frequency = 1,
                    FirstOccurrence = failure.Timestamp,
                    LastOccurrence = failure.Timestamp,
                    TrendDirection = TrendDirection.Stable
                },
                (key, existing) =>
                {
                    existing.Frequency++;
                    existing.LastOccurrence = failure.Timestamp;
                    existing.TrendDirection = CalculateTrendDirection(existing);
                    return existing;
                });

            await Task.CompletedTask;
        }

        private void InitializeRecoveryStrategies()
        {
            var strategies = new[]
            {
                new RecoveryStrategy
                {
                    Name = "Retry Operation",
                    Description = "Retry the failed operation with exponential backoff",
                    ApplicableCategories = new[] { "Network", "Timeout", "Transient" },
                    Steps = new[] { "Wait for backoff period", "Retry operation", "Monitor result" },
                    SuccessRate = 0.75,
                    EstimatedTime = TimeSpan.FromMinutes(2),
                    RiskLevel = RiskLevel.Low,
                    CanAutoExecute = true
                },
                new RecoveryStrategy
                {
                    Name = "Restart Service",
                    Description = "Restart the affected service or component",
                    ApplicableCategories = new[] { "Service", "Memory", "Resource" },
                    Steps = new[] { "Stop service gracefully", "Clear resources", "Restart service", "Verify functionality" },
                    SuccessRate = 0.85,
                    EstimatedTime = TimeSpan.FromMinutes(5),
                    RiskLevel = RiskLevel.Medium,
                    CanAutoExecute = false
                },
                new RecoveryStrategy
                {
                    Name = "Fallback to Alternative",
                    Description = "Switch to alternative implementation or service",
                    ApplicableCategories = new[] { "Service", "Network", "External" },
                    Steps = new[] { "Identify alternative", "Configure fallback", "Test alternative", "Monitor performance" },
                    SuccessRate = 0.65,
                    EstimatedTime = TimeSpan.FromMinutes(10),
                    RiskLevel = RiskLevel.Medium,
                    CanAutoExecute = true
                },
                new RecoveryStrategy
                {
                    Name = "Manual Investigation",
                    Description = "Requires manual investigation and intervention",
                    ApplicableCategories = new[] { "Logic", "Data", "Configuration", "Unknown" },
                    Steps = new[] { "Analyze logs", "Review code", "Check configuration", "Apply fix", "Test solution" },
                    SuccessRate = 0.90,
                    EstimatedTime = TimeSpan.FromHours(1),
                    RiskLevel = RiskLevel.High,
                    CanAutoExecute = false
                }
            };

            foreach (var strategy in strategies)
            {
                _recoveryStrategies.TryAdd(strategy.Name, strategy);
            }
        }

        private FailureInfo ExtractFailureInfoFromRequest(AgentRequest request)
        {
            var failureInfo = new FailureInfo
            {
                ErrorMessage = request.Context?.GetValueOrDefault("error")?.ToString() ?? 
                              request.Context?.GetValueOrDefault("exception")?.ToString() ?? 
                              request.Prompt ?? "Unknown error",
                Context = request.Context ?? new Dictionary<string, object>(),
                Component = request.Context?.GetValueOrDefault("component")?.ToString() ?? "Unknown"
            };

            // Try to extract stack trace
            if (request.Context?.ContainsKey("stackTrace") == true)
            {
                failureInfo.StackTrace = request.Context["stackTrace"].ToString();
            }

            return failureInfo;
        }

        private string CategorizeFailure(FailureInfo failureInfo)
        {
            var message = failureInfo.ErrorMessage.ToLowerInvariant();
            
            if (message.Contains("timeout") || message.Contains("timed out"))
                return "Timeout";
            if (message.Contains("network") || message.Contains("connection"))
                return "Network";
            if (message.Contains("memory") || message.Contains("outofmemory"))
                return "Memory";
            if (message.Contains("null") || message.Contains("nullreference"))
                return "NullReference";
            if (message.Contains("argument") || message.Contains("parameter"))
                return "Argument";
            if (message.Contains("file") || message.Contains("directory"))
                return "FileSystem";
            if (message.Contains("database") || message.Contains("sql"))
                return "Database";
            if (message.Contains("permission") || message.Contains("access"))
                return "Security";
            if (message.Contains("configuration") || message.Contains("config"))
                return "Configuration";
            
            return "Unknown";
        }

        private FailureSeverity DetermineSeverity(FailureInfo failureInfo)
        {
            var message = failureInfo.ErrorMessage.ToLowerInvariant();
            
            if (message.Contains("critical") || message.Contains("fatal") || message.Contains("outofmemory"))
                return FailureSeverity.Critical;
            if (message.Contains("error") || message.Contains("exception") || message.Contains("failed"))
                return FailureSeverity.High;
            if (message.Contains("warning") || message.Contains("timeout"))
                return FailureSeverity.Medium;
            
            return FailureSeverity.Low;
        }

        private List<FailurePattern> FindMatchingPatterns(FailureInfo failureInfo)
        {
            return _failurePatterns.Values
                .Where(p => IsPatternMatch(p, failureInfo))
                .OrderByDescending(p => CalculatePatternConfidence(p, failureInfo))
                .ToList();
        }

        private bool IsPatternMatch(FailurePattern pattern, FailureInfo failureInfo)
        {
            // Simple pattern matching - in a real implementation, this would be more sophisticated
            return pattern.Category == CategorizeFailure(failureInfo) ||
                   pattern.Name.Contains(failureInfo.Component, StringComparison.OrdinalIgnoreCase);
        }

        private double CalculatePatternConfidence(FailurePattern pattern, FailureInfo failureInfo)
        {
            double confidence = 0.0;
            
            // Category match
            if (pattern.Category == CategorizeFailure(failureInfo))
                confidence += 0.4;
            
            // Component match
            if (pattern.Name.Contains(failureInfo.Component, StringComparison.OrdinalIgnoreCase))
                confidence += 0.3;
            
            // Frequency factor
            confidence += Math.Min(0.3, pattern.Frequency / 100.0);
            
            return confidence;
        }

        private List<RecoveryStrategy> GenerateRecoveryOptions(List<FailurePattern> matchingPatterns, FailureInfo failureInfo)
        {
            var category = CategorizeFailure(failureInfo);
            
            return _recoveryStrategies.Values
                .Where(s => s.ApplicableCategories.Contains(category) || s.ApplicableCategories.Contains("Unknown"))
                .OrderByDescending(s => s.SuccessRate)
                .ToList();
        }

        private string GeneratePatternKey(FailureRecord failure)
        {
            return $"{failure.Category}_{failure.Component}_{failure.ErrorMessage.GetHashCode():X}";
        }

        private string GeneratePatternDescription(FailureRecord failure)
        {
            return $"{failure.Category} error in {failure.Component}: {failure.ErrorMessage.Substring(0, Math.Min(100, failure.ErrorMessage.Length))}";
        }

        private TrendDirection CalculateTrendDirection(FailurePattern pattern)
        {
            // Simple trend calculation - in a real implementation, this would analyze historical data
            var recentOccurrences = pattern.Frequency;
            if (recentOccurrences > 10)
                return TrendDirection.Increasing;
            if (recentOccurrences < 3)
                return TrendDirection.Decreasing;
            
            return TrendDirection.Stable;
        }

        private (DateTime Start, DateTime End) ExtractTimeRangeFromRequest(AgentRequest request)
        {
            var end = DateTime.UtcNow;
            var start = end.AddDays(-7); // Default to last 7 days
            
            if (request.Context?.ContainsKey("startDate") == true)
            {
                if (DateTime.TryParse(request.Context["startDate"].ToString(), out var startDate))
                    start = startDate;
            }
            
            if (request.Context?.ContainsKey("endDate") == true)
            {
                if (DateTime.TryParse(request.Context["endDate"].ToString(), out var endDate))
                    end = endDate;
            }
            
            return (start, end);
        }

        private List<FailureRecord> GetFailuresInTimeRange(DateTime start, DateTime end)
        {
            return _failureRecords.Values
                .Where(f => f.Timestamp >= start && f.Timestamp <= end)
                .OrderByDescending(f => f.Timestamp)
                .ToList();
        }

        private async Task<PatternAnalysisResult> AnalyzePatternsAsync(List<FailureRecord> failures)
        {
            var patterns = _failurePatterns.Values.ToList();
            var recommendations = new List<string>();
            
            // Generate recommendations based on patterns
            var topPatterns = patterns.OrderByDescending(p => p.Frequency).Take(5);
            foreach (var pattern in topPatterns)
            {
                if (pattern.TrendDirection == TrendDirection.Increasing)
                {
                    recommendations.Add($"Pattern '{pattern.Name}' is increasing in frequency. Consider investigating root cause.");
                }
                
                if (pattern.Severity == FailureSeverity.Critical && pattern.Frequency > 5)
                {
                    recommendations.Add($"Critical pattern '{pattern.Name}' has occurred {pattern.Frequency} times. Immediate attention required.");
                }
            }
            
            return new PatternAnalysisResult
            {
                Patterns = patterns,
                Recommendations = recommendations
            };
        }

        private async Task<FailureDiagnosis> PerformDiagnosisAsync(FailureInfo failureInfo)
        {
            var category = CategorizeFailure(failureInfo);
            var severity = DetermineSeverity(failureInfo);
            var similarFailures = FindSimilarFailures(failureInfo);
            
            var diagnosis = new FailureDiagnosis
            {
                PrimaryCategory = category,
                Severity = severity,
                Confidence = CalculateDiagnosisConfidence(failureInfo, similarFailures),
                RootCause = DetermineRootCause(failureInfo, similarFailures),
                ContributingFactors = IdentifyContributingFactors(failureInfo),
                SimilarFailures = similarFailures.Take(5).ToList(),
                AffectedComponents = new[] { failureInfo.Component },
                BusinessImpact = AssessBusinessImpact(severity),
                TechnicalImpact = AssessTechnicalImpact(category, severity),
                UserImpact = AssessUserImpact(category, severity),
                RecommendedActions = GenerateRecommendedActions(category, severity)
            };
            
            return diagnosis;
        }

        private List<FailureRecord> FindSimilarFailures(FailureInfo failureInfo)
        {
            var category = CategorizeFailure(failureInfo);
            
            return _failureRecords.Values
                .Where(f => f.Category == category || f.Component == failureInfo.Component)
                .OrderByDescending(f => CalculateSimilarity(failureInfo, f))
                .Take(10)
                .ToList();
        }

        private double CalculateSimilarity(FailureInfo failureInfo, FailureRecord record)
        {
            double similarity = 0.0;
            
            if (CategorizeFailure(failureInfo) == record.Category)
                similarity += 0.4;
            
            if (failureInfo.Component == record.Component)
                similarity += 0.3;
            
            // Simple text similarity for error messages
            var commonWords = failureInfo.ErrorMessage.Split(' ')
                .Intersect(record.ErrorMessage.Split(' '), StringComparer.OrdinalIgnoreCase)
                .Count();
            
            similarity += Math.Min(0.3, commonWords / 10.0);
            
            return similarity;
        }

        private double CalculateDiagnosisConfidence(FailureInfo failureInfo, List<FailureRecord> similarFailures)
        {
            if (similarFailures.Count == 0)
                return 0.3; // Low confidence for unknown failures
            
            var avgSimilarity = similarFailures.Average(f => CalculateSimilarity(failureInfo, f));
            return Math.Min(0.95, 0.5 + avgSimilarity);
        }

        private string DetermineRootCause(FailureInfo failureInfo, List<FailureRecord> similarFailures)
        {
            var category = CategorizeFailure(failureInfo);
            
            return category switch
            {
                "Network" => "Network connectivity or configuration issue",
                "Timeout" => "Operation exceeded time limit, possibly due to resource contention",
                "Memory" => "Insufficient memory or memory leak",
                "NullReference" => "Uninitialized object or missing null check",
                "Database" => "Database connectivity or query issue",
                "Security" => "Insufficient permissions or authentication failure",
                "Configuration" => "Missing or incorrect configuration setting",
                _ => "Root cause requires further investigation"
            };
        }

        private List<string> IdentifyContributingFactors(FailureInfo failureInfo)
        {
            var factors = new List<string>();
            var message = failureInfo.ErrorMessage.ToLowerInvariant();
            
            if (message.Contains("load") || message.Contains("busy"))
                factors.Add("High system load");
            
            if (message.Contains("concurrent") || message.Contains("thread"))
                factors.Add("Concurrency issues");
            
            if (message.Contains("resource") || message.Contains("limit"))
                factors.Add("Resource constraints");
            
            if (factors.Count == 0)
                factors.Add("No obvious contributing factors identified");
            
            return factors;
        }

        private string AssessBusinessImpact(FailureSeverity severity)
        {
            return severity switch
            {
                FailureSeverity.Critical => "High - Service disruption affecting users",
                FailureSeverity.High => "Medium - Feature degradation or errors",
                FailureSeverity.Medium => "Low - Minor functionality issues",
                FailureSeverity.Low => "Minimal - Logging or non-critical features",
                _ => "Unknown impact"
            };
        }

        private string AssessTechnicalImpact(string category, FailureSeverity severity)
        {
            var baseImpact = category switch
            {
                "Database" => "Data access affected",
                "Network" => "Communication disrupted",
                "Memory" => "Performance degradation",
                "Security" => "Access restrictions",
                _ => "System functionality impacted"
            };
            
            return $"{baseImpact} - {severity} severity";
        }

        private string AssessUserImpact(string category, FailureSeverity severity)
        {
            return severity switch
            {
                FailureSeverity.Critical => "Users cannot access core functionality",
                FailureSeverity.High => "Users experience errors or failures",
                FailureSeverity.Medium => "Users may notice slower performance",
                FailureSeverity.Low => "Minimal user-visible impact",
                _ => "User impact unknown"
            };
        }

        private List<string> GenerateRecommendedActions(string category, FailureSeverity severity)
        {
            var actions = new List<string>();
            
            if (severity == FailureSeverity.Critical)
            {
                actions.Add("Immediate escalation to on-call team");
                actions.Add("Activate incident response procedures");
            }
            
            actions.AddRange(category switch
            {
                "Network" => new[] { "Check network connectivity", "Verify firewall rules", "Test DNS resolution" },
                "Database" => new[] { "Check database connectivity", "Review query performance", "Verify connection pool" },
                "Memory" => new[] { "Monitor memory usage", "Check for memory leaks", "Consider scaling resources" },
                "Security" => new[] { "Verify permissions", "Check authentication", "Review security logs" },
                _ => new[] { "Review application logs", "Check system resources", "Verify configuration" }
            });
            
            return actions;
        }

        private object AnalyzeTrends(List<FailureRecord> failures)
        {
            var dailyFailures = failures
                .GroupBy(f => f.Timestamp.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList();
            
            var trend = "Stable";
            if (dailyFailures.Count >= 2)
            {
                var recent = dailyFailures.Skip(Math.Max(0, dailyFailures.Count - 3)).Average(x => x.Count);
                var older = dailyFailures.Take(dailyFailures.Count - 3).Average(x => x.Count);
                
                if (recent > older * 1.2)
                    trend = "Increasing";
                else if (recent < older * 0.8)
                    trend = "Decreasing";
            }
            
            return new
            {
                Trend = trend,
                DailyFailures = dailyFailures,
                PeakDay = dailyFailures.OrderByDescending(x => x.Count).FirstOrDefault()
            };
        }

        private object CalculateRecoveryEffectiveness(List<FailureRecord> failures)
        {
            var totalRecoveryAttempts = _failurePatterns.Values.Sum(p => p.RecoveryAttempts);
            var successfulRecoveries = _failurePatterns.Values.Sum(p => p.SuccessfulRecoveries);
            
            return new
            {
                TotalAttempts = totalRecoveryAttempts,
                SuccessfulRecoveries = successfulRecoveries,
                SuccessRate = totalRecoveryAttempts > 0 ? (double)successfulRecoveries / totalRecoveryAttempts : 0,
                StrategiesBySuccess = _recoveryStrategies.Values
                    .OrderByDescending(s => s.SuccessRate)
                    .Select(s => new { s.Name, s.SuccessRate })
            };
        }

        private string ExtractActionFromRequest(AgentRequest request)
        {
            var prompt = request.Prompt?.ToLowerInvariant() ?? "";
            
            if (prompt.Contains("track") || prompt.Contains("record"))
                return "track";
            if (prompt.Contains("analyze") || prompt.Contains("pattern"))
                return "analyze";
            if (prompt.Contains("recover") || prompt.Contains("suggest"))
                return "recover";
            if (prompt.Contains("diagnose") || prompt.Contains("investigate"))
                return "diagnose";
            if (prompt.Contains("report") || prompt.Contains("statistics"))
                return "statistics";
            
            return "generic"; // Default action
        }

        private async System.Threading.Tasks.Task LoadPersistedDataAsync()
        {
            // In a real implementation, this would load from a database or file system
            await Task.CompletedTask;
        }

        private async System.Threading.Tasks.Task PersistDataAsync()
        {
            // In a real implementation, this would save to a database or file system
            await Task.CompletedTask;
        }

        private async void PerformPatternAnalysis(object? state)
        {
            try
            {
                Logger.LogTrace("Performing periodic pattern analysis");
                
                // Clean up old failure records (older than 30 days)
                var cutoffDate = DateTime.UtcNow.AddDays(-30);
                var oldRecords = _failureRecords.Values
                    .Where(f => f.Timestamp < cutoffDate)
                    .ToList();
                
                foreach (var record in oldRecords)
                {
                    _failureRecords.TryRemove(record.Id, out _);
                }
                
                // Update pattern trends
                foreach (var pattern in _failurePatterns.Values)
                {
                    pattern.TrendDirection = CalculateTrendDirection(pattern);
                }
                
                Logger.LogTrace("Pattern analysis completed. Cleaned up {OldRecordCount} old records", oldRecords.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during periodic pattern analysis");
            }
        }

        public override void Dispose()
        {
            _analysisTimer?.Dispose();
            base.Dispose();
        }
    }

    // Supporting classes and enums
    public class FailureRecord
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public Guid RequestId { get; set; }
        public string Category { get; set; } = "";
        public FailureSeverity Severity { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string? StackTrace { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
        public string Component { get; set; } = "";
        public List<string> PatternMatches { get; set; } = new();
        public string SuggestedRecovery { get; set; } = "";
        public string? Resolution { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public class FailurePattern
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public FailureSeverity Severity { get; set; }
        public int Frequency { get; set; }
        public DateTime FirstOccurrence { get; set; }
        public DateTime LastOccurrence { get; set; }
        public TrendDirection TrendDirection { get; set; }
        public int RecoveryAttempts { get; set; }
        public int SuccessfulRecoveries { get; set; }
    }

    public class RecoveryStrategy
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string[] ApplicableCategories { get; set; } = Array.Empty<string>();
        public string[] Steps { get; set; } = Array.Empty<string>();
        public double SuccessRate { get; set; }
        public TimeSpan EstimatedTime { get; set; }
        public RiskLevel RiskLevel { get; set; }
        public bool CanAutoExecute { get; set; }
        public string[] Prerequisites { get; set; } = Array.Empty<string>();
    }

    public class FailureInfo
    {
        public string ErrorMessage { get; set; } = "";
        public string? StackTrace { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
        public string Component { get; set; } = "";
        public object Category { get; internal set; }
    }

    public class PatternAnalysisResult
    {
        public List<FailurePattern> Patterns { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    public class FailureDiagnosis
    {
        public string PrimaryCategory { get; set; } = "";
        public FailureSeverity Severity { get; set; }
        public double Confidence { get; set; }
        public string RootCause { get; set; } = "";
        public List<string> ContributingFactors { get; set; } = new();
        public List<FailureRecord> SimilarFailures { get; set; } = new();
        public string[] AffectedComponents { get; set; } = Array.Empty<string>();
        public string BusinessImpact { get; set; } = "";
        public string TechnicalImpact { get; set; } = "";
        public string UserImpact { get; set; } = "";
        public List<string> RecommendedActions { get; set; } = new();
    }

    public enum FailureSeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public enum TrendDirection
    {
        Decreasing,
        Stable,
        Increasing
    }

    public enum RiskLevel
    {
        Low,
        Medium,
        High
    }
}