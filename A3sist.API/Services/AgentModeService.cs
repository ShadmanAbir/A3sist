using A3sist.API.Models;
using A3sist.API.Services;
using System.Collections.Concurrent;

namespace A3sist.API.Services;

public class AgentModeService : IAgentModeService, IDisposable
{
    private readonly ILogger<AgentModeService> _logger;
    private readonly ICodeAnalysisService _codeAnalysisService;
    private readonly IModelManagementService _modelService;
    private readonly SemaphoreSlim _semaphore;
    private volatile bool _isAnalysisRunning;
    private CancellationTokenSource? _cancellationTokenSource;
    private AgentAnalysisReport? _currentReport;
    private bool _disposed;

    public event EventHandler<AgentProgressEventArgs>? ProgressChanged;
    public event EventHandler<AgentIssueFoundEventArgs>? IssueFound;
    public event EventHandler<AgentAnalysisCompletedEventArgs>? AnalysisCompleted;

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".vb", ".fs", ".cpp", ".c", ".h", ".hpp", ".java", ".py", ".js", ".ts",
        ".html", ".xml", ".json", ".yml", ".yaml", ".md", ".txt", ".sql", ".ps1"
    };

    public AgentModeService(
        ILogger<AgentModeService> logger,
        ICodeAnalysisService codeAnalysisService,
        IModelManagementService modelService)
    {
        _logger = logger;
        _codeAnalysisService = codeAnalysisService;
        _modelService = modelService;
        _semaphore = new SemaphoreSlim(1, 1);
        _isAnalysisRunning = false;
    }

    public async Task<bool> StartAnalysisAsync(string workspacePath)
    {
        if (string.IsNullOrEmpty(workspacePath) || !Directory.Exists(workspacePath))
        {
            _logger.LogWarning("Invalid workspace path: {WorkspacePath}", workspacePath);
            return false;
        }

        await _semaphore.WaitAsync();
        try
        {
            if (_isAnalysisRunning)
            {
                _logger.LogWarning("Analysis is already running");
                return false;
            }

            _logger.LogInformation("Starting agent analysis for workspace: {WorkspacePath}", workspacePath);

            _isAnalysisRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();

            // Initialize report
            _currentReport = new AgentAnalysisReport
            {
                Id = Guid.NewGuid().ToString(),
                StartTime = DateTime.UtcNow,
                WorkspacePath = workspacePath,
                Status = AgentAnalysisStatus.Running,
                FilesAnalyzed = 0,
                TotalFiles = 0,
                Findings = new List<AgentFinding>(),
                Recommendations = new List<AgentRecommendation>(),
                Statistics = new Dictionary<string, object>()
            };

            // Start analysis in background
            _ = Task.Run(async () => await RunAnalysisAsync(workspacePath, _cancellationTokenSource.Token));

            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> StopAnalysisAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!_isAnalysisRunning)
            {
                _logger.LogWarning("No analysis is currently running");
                return false;
            }

            _logger.LogInformation("Stopping agent analysis");

            _cancellationTokenSource?.Cancel();
            _isAnalysisRunning = false;

            if (_currentReport != null)
            {
                _currentReport.Status = AgentAnalysisStatus.Cancelled;
                _currentReport.EndTime = DateTime.UtcNow;
                _currentReport.TotalTime = _currentReport.EndTime.Value - _currentReport.StartTime;
            }

            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<AgentAnalysisReport> GetCurrentReportAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _currentReport ?? new AgentAnalysisReport
            {
                Status = AgentAnalysisStatus.NotStarted,
                Findings = new List<AgentFinding>(),
                Recommendations = new List<AgentRecommendation>(),
                Statistics = new Dictionary<string, object>()
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> IsAnalysisRunningAsync()
    {
        return await Task.FromResult(_isAnalysisRunning);
    }

    private async Task RunAnalysisAsync(string workspacePath, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Agent analysis started for workspace: {WorkspacePath}", workspacePath);

            // Get all files to analyze
            var files = GetFilesToAnalyze(workspacePath);
            if (_currentReport != null)
            {
                _currentReport.TotalFiles = files.Count;
            }

            var processedFiles = 0;
            var startTime = DateTime.UtcNow;

            foreach (var file in files)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Analysis cancelled");
                    break;
                }

                try
                {
                    await AnalyzeFileAsync(file, cancellationToken);
                    processedFiles++;

                    if (_currentReport != null)
                    {
                        _currentReport.FilesAnalyzed = processedFiles;
                    }

                    // Report progress
                    var progressPercentage = (double)processedFiles / files.Count * 100;
                    var elapsed = DateTime.UtcNow - startTime;
                    var remainingFiles = files.Count - processedFiles;
                    var estimatedTimeRemaining = remainingFiles > 0 && processedFiles > 0
                        ? TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds / processedFiles * remainingFiles)
                        : TimeSpan.Zero;

                    ProgressChanged?.Invoke(this, new AgentProgressEventArgs
                    {
                        CurrentFile = Path.GetFileName(file),
                        FilesProcessed = processedFiles,
                        TotalFiles = files.Count,
                        ProgressPercentage = progressPercentage,
                        Status = AgentAnalysisStatus.Running,
                        StatusMessage = $"Analyzing {Path.GetFileName(file)}"
                    });

                    // Small delay to prevent overwhelming the system
                    await Task.Delay(50, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error analyzing file: {FilePath}", file);
                }
            }

            // Complete analysis
            await CompleteAnalysisAsync(cancellationToken.IsCancellationRequested);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during agent analysis");
            await CompleteAnalysisAsync(true, ex.Message);
        }
    }

    private async Task AnalyzeFileAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(content))
                return;

            var language = await _codeAnalysisService.DetectLanguageAsync(content, filePath);
            
            // Analyze code issues
            var issues = await _codeAnalysisService.AnalyzeCodeAsync(content, language);
            
            // Convert issues to findings
            foreach (var issue in issues)
            {
                var finding = new AgentFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    FilePath = filePath,
                    Line = issue.Line,
                    Column = issue.Column,
                    Type = MapIssueToFindingType(issue),
                    Title = issue.Message,
                    Description = issue.Message,
                    Severity = MapIssueSeverity(issue.Severity),
                    Confidence = 0.8,
                    SuggestedActions = issue.SuggestedFixes ?? new List<string>()
                };

                _currentReport?.Findings?.Add(finding);

                // Raise issue found event
                IssueFound?.Invoke(this, new AgentIssueFoundEventArgs
                {
                    Finding = finding,
                    FilePath = filePath
                });
            }

            // Generate AI-powered insights if available
            await GenerateAIInsightsAsync(filePath, content, language, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing file: {FilePath}", filePath);
        }
    }

    private async Task GenerateAIInsightsAsync(string filePath, string content, string language, CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var activeModel = await _modelService.GetActiveModelAsync();
            if (activeModel == null || !activeModel.IsAvailable)
                return;

            // Create a focused prompt for code analysis
            var prompt = CreateAnalysisPrompt(content, language, Path.GetFileName(filePath));

            var request = new ModelRequest
            {
                Prompt = prompt,
                MaxTokens = 500,
                Temperature = 0.3,
                Parameters = new Dictionary<string, object>()
            };

            var response = await _modelService.SendRequestAsync(request);
            
            if (response.Success && !string.IsNullOrEmpty(response.Content))
            {
                var recommendations = ParseAIRecommendations(response.Content, filePath);
                
                if (_currentReport?.Recommendations != null)
                {
                    _currentReport.Recommendations.AddRange(recommendations);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI insights for file: {FilePath}", filePath);
        }
    }

    private async Task CompleteAnalysisAsync(bool cancelled, string? errorMessage = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            _isAnalysisRunning = false;
            
            if (_currentReport != null)
            {
                _currentReport.EndTime = DateTime.UtcNow;
                _currentReport.TotalTime = _currentReport.EndTime.Value - _currentReport.StartTime;
                _currentReport.Status = cancelled ? AgentAnalysisStatus.Cancelled : 
                                      !string.IsNullOrEmpty(errorMessage) ? AgentAnalysisStatus.Failed : 
                                      AgentAnalysisStatus.Completed;

                // Update statistics
                UpdateStatistics();

                // Generate final recommendations
                if (!cancelled && string.IsNullOrEmpty(errorMessage))
                {
                    await GenerateFinalRecommendationsAsync();
                }
            }

            // Raise completion event
            AnalysisCompleted?.Invoke(this, new AgentAnalysisCompletedEventArgs
            {
                Report = _currentReport,
                Success = !cancelled && string.IsNullOrEmpty(errorMessage),
                Error = errorMessage,
                Duration = _currentReport?.TotalTime ?? TimeSpan.Zero
            });

            _logger.LogInformation("Agent analysis completed. Status: {Status}, Files analyzed: {FilesAnalyzed}, Findings: {FindingsCount}",
                _currentReport?.Status, _currentReport?.FilesAnalyzed, _currentReport?.Findings?.Count);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private List<string> GetFilesToAnalyze(string workspacePath)
    {
        var files = new List<string>();

        try
        {
            var allFiles = Directory.GetFiles(workspacePath, "*", SearchOption.AllDirectories);
            
            foreach (var file in allFiles)
            {
                var extension = Path.GetExtension(file);
                if (SupportedExtensions.Contains(extension))
                {
                    // Skip large files (> 2MB)
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.Length <= 2 * 1024 * 1024)
                    {
                        files.Add(file);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting files from workspace: {WorkspacePath}", workspacePath);
        }

        return files;
    }

    private AgentFindingType MapIssueToFindingType(CodeIssue issue)
    {
        return issue.Category?.ToLowerInvariant() switch
        {
            "syntax" => AgentFindingType.PotentialBug,
            "style" => AgentFindingType.CodeSmell,
            "best practice" => AgentFindingType.BestPracticeViolation,
            "performance" => AgentFindingType.PerformanceIssue,
            "security" => AgentFindingType.SecurityIssue,
            "maintainability" => AgentFindingType.CodeSmell,
            _ => AgentFindingType.CodeSmell
        };
    }

    private AgentSeverity MapIssueSeverity(IssueSeverity severity)
    {
        return severity switch
        {
            IssueSeverity.Error => AgentSeverity.High,
            IssueSeverity.Warning => AgentSeverity.Medium,
            IssueSeverity.Info => AgentSeverity.Low,
            _ => AgentSeverity.Low
        };
    }

    private string CreateAnalysisPrompt(string content, string language, string fileName)
    {
        var codeSnippet = content.Length > 1000 ? content.Substring(0, 1000) + "..." : content;
        
        return $@"Analyze the following {language} code from file '{fileName}' and provide specific recommendations for improvement:

```{language}
{codeSnippet}
```

Focus on:
1. Code quality and maintainability
2. Performance optimizations
3. Best practices adherence
4. Potential security issues
5. Architecture improvements

Provide concrete, actionable recommendations:";
    }

    private List<AgentRecommendation> ParseAIRecommendations(string aiResponse, string filePath)
    {
        var recommendations = new List<AgentRecommendation>();

        try
        {
            var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                  .Where(line => !string.IsNullOrWhiteSpace(line))
                                  .ToList();

            foreach (var line in lines.Take(5)) // Limit to 5 recommendations per file
            {
                var cleanLine = line.Trim().TrimStart('-', '*', '1', '2', '3', '4', '5', '.', ' ');
                
                if (cleanLine.Length > 10) // Minimum length filter
                {
                    recommendations.Add(new AgentRecommendation
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = TruncateString(cleanLine, 100),
                        Description = cleanLine,
                        Type = DetermineRecommendationType(cleanLine),
                        Priority = DeterminePriority(cleanLine),
                        ImpactScore = CalculateImpactScore(cleanLine),
                        AffectedFiles = new List<string> { filePath },
                        ActionPlan = $"Review and implement suggestion in {Path.GetFileName(filePath)}"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing AI recommendations");
        }

        return recommendations;
    }

    private void UpdateStatistics()
    {
        if (_currentReport?.Statistics == null)
            return;

        try
        {
            var findings = _currentReport.Findings ?? new List<AgentFinding>();
            
            _currentReport.Statistics["TotalFindings"] = findings.Count;
            _currentReport.Statistics["CriticalFindings"] = findings.Count(f => f.Severity == AgentSeverity.Critical);
            _currentReport.Statistics["HighSeverityFindings"] = findings.Count(f => f.Severity == AgentSeverity.High);
            _currentReport.Statistics["MediumSeverityFindings"] = findings.Count(f => f.Severity == AgentSeverity.Medium);
            _currentReport.Statistics["LowSeverityFindings"] = findings.Count(f => f.Severity == AgentSeverity.Low);
            
            _currentReport.Statistics["CodeSmells"] = findings.Count(f => f.Type == AgentFindingType.CodeSmell);
            _currentReport.Statistics["SecurityIssues"] = findings.Count(f => f.Type == AgentFindingType.SecurityIssue);
            _currentReport.Statistics["PerformanceIssues"] = findings.Count(f => f.Type == AgentFindingType.PerformanceIssue);
            _currentReport.Statistics["PotentialBugs"] = findings.Count(f => f.Type == AgentFindingType.PotentialBug);
            
            _currentReport.Statistics["TotalRecommendations"] = _currentReport.Recommendations?.Count ?? 0;
            _currentReport.Statistics["AnalysisDuration"] = _currentReport.TotalTime.TotalMinutes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating statistics");
        }
    }

    private async Task GenerateFinalRecommendationsAsync()
    {
        await Task.CompletedTask;

        try
        {
            if (_currentReport?.Findings == null)
                return;

            var findings = _currentReport.Findings;
            
            // Generate summary recommendations based on findings
            if (findings.Count(f => f.Type == AgentFindingType.SecurityIssue) > 0)
            {
                _currentReport.Recommendations?.Add(new AgentRecommendation
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Security Review Required",
                    Description = "Multiple security issues detected. Conduct a thorough security review.",
                    Type = AgentRecommendationType.Security,
                    Priority = 5,
                    ImpactScore = 0.9,
                    AffectedFiles = findings.Where(f => f.Type == AgentFindingType.SecurityIssue)
                                          .Select(f => f.FilePath).Distinct().ToList(),
                    ActionPlan = "1. Review all security findings\n2. Implement fixes\n3. Security testing"
                });
            }

            if (findings.Count(f => f.Type == AgentFindingType.PerformanceIssue) > 5)
            {
                _currentReport.Recommendations?.Add(new AgentRecommendation
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Performance Optimization",
                    Description = "Multiple performance issues detected. Consider optimization review.",
                    Type = AgentRecommendationType.Performance,
                    Priority = 4,
                    ImpactScore = 0.7,
                    AffectedFiles = findings.Where(f => f.Type == AgentFindingType.PerformanceIssue)
                                          .Select(f => f.FilePath).Distinct().ToList(),
                    ActionPlan = "1. Profile application performance\n2. Optimize critical paths\n3. Monitor improvements"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating final recommendations");
        }
    }

    private AgentRecommendationType DetermineRecommendationType(string recommendation)
    {
        var lower = recommendation.ToLowerInvariant();
        
        if (lower.Contains("refactor") || lower.Contains("extract") || lower.Contains("simplify"))
            return AgentRecommendationType.Refactoring;
        if (lower.Contains("test") || lower.Contains("coverage"))
            return AgentRecommendationType.Testing;
        if (lower.Contains("document") || lower.Contains("comment"))
            return AgentRecommendationType.Documentation;
        if (lower.Contains("security") || lower.Contains("vulnerability"))
            return AgentRecommendationType.Security;
        if (lower.Contains("performance") || lower.Contains("optimize"))
            return AgentRecommendationType.Performance;
        if (lower.Contains("architecture") || lower.Contains("design"))
            return AgentRecommendationType.Architecture;
        
        return AgentRecommendationType.CodeOrganization;
    }

    private int DeterminePriority(string recommendation)
    {
        var lower = recommendation.ToLowerInvariant();
        
        if (lower.Contains("critical") || lower.Contains("security") || lower.Contains("vulnerability"))
            return 5;
        if (lower.Contains("important") || lower.Contains("performance") || lower.Contains("bug"))
            return 4;
        if (lower.Contains("should") || lower.Contains("recommend"))
            return 3;
        if (lower.Contains("consider") || lower.Contains("might"))
            return 2;
        
        return 1;
    }

    private double CalculateImpactScore(string recommendation)
    {
        var lower = recommendation.ToLowerInvariant();
        
        if (lower.Contains("critical") || lower.Contains("security"))
            return 0.9;
        if (lower.Contains("performance") || lower.Contains("bug"))
            return 0.7;
        if (lower.Contains("maintainability") || lower.Contains("readability"))
            return 0.5;
        
        return 0.3;
    }

    private string TruncateString(string input, int maxLength)
    {
        return input.Length <= maxLength ? input : input.Substring(0, maxLength) + "...";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _semaphore.Dispose();
            _disposed = true;
        }
    }
}