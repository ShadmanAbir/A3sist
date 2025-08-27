using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using A3sist.Models;
using A3sist.Services;

namespace A3sist.Agent
{
    public interface IAgentModeService
    {
        Task<bool> StartAnalysisAsync(string workspacePath);
        Task<bool> StopAnalysisAsync();
        Task<AgentAnalysisReport> GetCurrentReportAsync();
        Task<bool> IsAnalysisRunningAsync();
        event EventHandler<AgentProgressEventArgs> ProgressChanged;
        event EventHandler<AgentIssueFoundEventArgs> IssueFound;
        event EventHandler<AgentAnalysisCompletedEventArgs> AnalysisCompleted;
    }

    public class AgentModeService : IAgentModeService
    {
        private readonly ICodeAnalysisService _codeAnalysisService;
        private readonly IRefactoringService _refactoringService;
        private readonly IRAGEngineService _ragService;
        private readonly IModelManagementService _modelService;
        private readonly IA3sistConfigurationService _configService;

        private CancellationTokenSource _analysisToken;
        private bool _isAnalysisRunning;
        private AgentAnalysisReport _currentReport;
        private readonly object _lockObject = new object();

        public event EventHandler<AgentProgressEventArgs> ProgressChanged;
        public event EventHandler<AgentIssueFoundEventArgs> IssueFound;
        public event EventHandler<AgentAnalysisCompletedEventArgs> AnalysisCompleted;

        public AgentModeService(
            ICodeAnalysisService codeAnalysisService,
            IRefactoringService refactoringService,
            IRAGEngineService ragService,
            IModelManagementService modelService,
            IA3sistConfigurationService configService)
        {
            _codeAnalysisService = codeAnalysisService;
            _refactoringService = refactoringService;
            _ragService = ragService;
            _modelService = modelService;
            _configService = configService;
            _currentReport = new AgentAnalysisReport();
        }

        public async Task<bool> StartAnalysisAsync(string workspacePath)
        {
            if (_isAnalysisRunning)
                return false;

            if (string.IsNullOrEmpty(workspacePath) || !Directory.Exists(workspacePath))
                return false;

            lock (_lockObject)
            {
                _isAnalysisRunning = true;
                _analysisToken = new CancellationTokenSource();
                _currentReport = new AgentAnalysisReport
                {
                    StartTime = DateTime.UtcNow,
                    WorkspacePath = workspacePath,
                    Status = AgentAnalysisStatus.Running
                };
            }

            // Start analysis in background
            _ = Task.Run(async () => await PerformAnalysisAsync(workspacePath, _analysisToken.Token));

            return true;
        }

        public async Task<bool> StopAnalysisAsync()
        {
            if (!_isAnalysisRunning)
                return false;

            lock (_lockObject)
            {
                _isAnalysisRunning = false;
                _analysisToken?.Cancel();
                
                if (_currentReport != null)
                {
                    _currentReport.EndTime = DateTime.UtcNow;
                    _currentReport.Status = AgentAnalysisStatus.Cancelled;
                }
            }

            return true;
        }

        public async Task<AgentAnalysisReport> GetCurrentReportAsync()
        {
            lock (_lockObject)
            {
                return _currentReport?.Clone() ?? new AgentAnalysisReport();
            }
        }

        public async Task<bool> IsAnalysisRunningAsync()
        {
            lock (_lockObject)
            {
                return _isAnalysisRunning;
            }
        }

        private async Task PerformAnalysisAsync(string workspacePath, CancellationToken cancellationToken)
        {
            try
            {
                // Phase 1: Discover files
                var files = await DiscoverCodeFilesAsync(workspacePath);
                _currentReport.TotalFiles = files.Count;
                
                ReportProgress("Discovered files", 0, files.Count);

                // Phase 2: Analyze each file
                for (int i = 0; i < files.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    var file = files[i];
                    await AnalyzeFileAsync(file);
                    
                    _currentReport.AnalyzedFiles = i + 1;
                    ReportProgress($"Analyzing {Path.GetFileName(file)}", i + 1, files.Count);
                }

                // Phase 3: Generate workspace-level insights
                await GenerateWorkspaceInsightsAsync(cancellationToken);

                // Phase 4: Index for RAG if enabled
                var ragEnabled = await _configService.GetSettingAsync("agent.enableRAG", true);
                if (ragEnabled)
                {
                    ReportProgress("Indexing for knowledge base", _currentReport.TotalFiles, _currentReport.TotalFiles);
                    await _ragService.IndexWorkspaceAsync(workspacePath);
                }

                // Complete analysis
                lock (_lockObject)
                {
                    _currentReport.EndTime = DateTime.UtcNow;
                    _currentReport.Status = AgentAnalysisStatus.Completed;
                    _isAnalysisRunning = false;
                }

                AnalysisCompleted?.Invoke(this, new AgentAnalysisCompletedEventArgs
                {
                    Report = _currentReport.Clone()
                });
            }
            catch (Exception ex)
            {
                lock (_lockObject)
                {
                    _currentReport.EndTime = DateTime.UtcNow;
                    _currentReport.Status = AgentAnalysisStatus.Failed;
                    _currentReport.ErrorMessage = ex.Message;
                    _isAnalysisRunning = false;
                }

                AnalysisCompleted?.Invoke(this, new AgentAnalysisCompletedEventArgs
                {
                    Report = _currentReport.Clone(),
                    Error = ex.Message
                });
            }
        }

        private async Task<List<string>> DiscoverCodeFilesAsync(string workspacePath)
        {
            var supportedExtensions = new[]
            {
                ".cs", ".vb", ".fs", ".js", ".ts", ".jsx", ".tsx",
                ".py", ".java", ".cpp", ".c", ".h", ".hpp",
                ".go", ".rs", ".rb", ".php", ".swift", ".kt"
            };

            var files = new List<string>();

            foreach (var extension in supportedExtensions)
            {
                try
                {
                    var pattern = $"*{extension}";
                    var foundFiles = Directory.GetFiles(workspacePath, pattern, SearchOption.AllDirectories);
                    files.AddRange(foundFiles);
                }
                catch
                {
                    // Continue with next extension
                }
            }

            // Filter out common excluded directories
            var excludedPaths = new[] { "bin", "obj", "node_modules", ".git", ".vs", "packages", "target", "build", "dist" };
            return files.Where(f => !excludedPaths.Any(e => f.Contains($"{Path.DirectorySeparatorChar}{e}{Path.DirectorySeparatorChar}"))).ToList();
        }

        private async Task AnalyzeFileAsync(string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                var language = await _codeAnalysisService.DetectLanguageAsync(content, filePath);

                // Analyze for issues
                var issues = await _codeAnalysisService.AnalyzeCodeAsync(content, language);
                
                // Get refactoring suggestions for the file
                var suggestions = await _refactoringService.GetRefactoringSuggestionsAsync(content, language);

                // Create file analysis result
                var fileAnalysis = new AgentFileAnalysis
                {
                    FilePath = filePath,
                    Language = language,
                    LineCount = content.Split('\n').Length,
                    Issues = issues.ToList(),
                    Suggestions = suggestions.ToList(),
                    AnalyzedAt = DateTime.UtcNow
                };

                lock (_lockObject)
                {
                    _currentReport.FileAnalyses.Add(fileAnalysis);
                    _currentReport.TotalIssues += issues.Count();
                    _currentReport.TotalSuggestions += suggestions.Count();
                }

                // Report critical issues immediately
                var criticalIssues = issues.Where(i => i.Severity == IssueSeverity.Error).ToList();
                if (criticalIssues.Any())
                {
                    IssueFound?.Invoke(this, new AgentIssueFoundEventArgs
                    {
                        FilePath = filePath,
                        Issues = criticalIssues
                    });
                }
            }
            catch (Exception ex)
            {
                // Log error and continue with next file
                System.Diagnostics.Debug.WriteLine($"Error analyzing file {filePath}: {ex.Message}");
            }
        }

        private async Task GenerateWorkspaceInsightsAsync(CancellationToken cancellationToken)
        {
            try
            {
                ReportProgress("Generating AI insights", 0, 1);

                // Get top issues across the workspace
                var allIssues = _currentReport.FileAnalyses.SelectMany(f => f.Issues).ToList();
                var topIssueTypes = allIssues
                    .GroupBy(i => i.Message)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => new AgentInsight
                    {
                        Type = AgentInsightType.CommonIssue,
                        Title = $"Common Issue: {g.Key}",
                        Description = $"Found {g.Count()} occurrences across {g.Select(i => _currentReport.FileAnalyses.FirstOrDefault(f => f.Issues.Contains(i))?.FilePath).Distinct().Count()} files",
                        Severity = g.First().Severity,
                        Confidence = Math.Min(1.0, g.Count() / 10.0)
                    })
                    .ToList();

                // Language distribution insights
                var languageStats = _currentReport.FileAnalyses
                    .GroupBy(f => f.Language)
                    .Select(g => new AgentInsight
                    {
                        Type = AgentInsightType.ProjectStructure,
                        Title = $"{g.Key} files: {g.Count()}",
                        Description = $"Total lines: {g.Sum(f => f.LineCount)}, Issues: {g.Sum(f => f.Issues.Count)}",
                        Confidence = 1.0
                    })
                    .ToList();

                // Generate AI-powered project analysis if model is available
                var activeModel = await _modelService.GetActiveModelAsync();
                if (activeModel?.IsAvailable == true)
                {
                    var aiInsights = await GenerateAIProjectInsightsAsync();
                    topIssueTypes.AddRange(aiInsights);
                }

                lock (_lockObject)
                {
                    _currentReport.Insights.AddRange(topIssueTypes);
                    _currentReport.Insights.AddRange(languageStats);
                }

                ReportProgress("AI insights generated", 1, 1);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating insights: {ex.Message}");
            }
        }

        private async Task<List<AgentInsight>> GenerateAIProjectInsightsAsync()
        {
            try
            {
                var summary = GenerateProjectSummary();
                
                var prompt = $@"Analyze this software project and provide insights:

{summary}

Please provide insights about:
1. Code quality patterns
2. Potential architectural improvements
3. Security considerations
4. Performance optimization opportunities
5. Maintainability concerns

Format your response as actionable insights.";

                var modelRequest = new ModelRequest
                {
                    Prompt = prompt,
                    SystemMessage = "You are a senior software architect providing code analysis insights.",
                    MaxTokens = 1000,
                    Temperature = 0.3
                };

                var response = await _modelService.SendRequestAsync(modelRequest);
                
                if (response.Success)
                {
                    return ParseAIInsights(response.Content);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating AI insights: {ex.Message}");
            }

            return new List<AgentInsight>();
        }

        private string GenerateProjectSummary()
        {
            var summary = new System.Text.StringBuilder();
            
            summary.AppendLine($"Project Analysis Summary:");
            summary.AppendLine($"- Total files: {_currentReport.TotalFiles}");
            summary.AppendLine($"- Total issues: {_currentReport.TotalIssues}");
            summary.AppendLine($"- Total suggestions: {_currentReport.TotalSuggestions}");
            summary.AppendLine();
            
            summary.AppendLine("Languages:");
            var languageGroups = _currentReport.FileAnalyses.GroupBy(f => f.Language);
            foreach (var group in languageGroups)
            {
                summary.AppendLine($"- {group.Key}: {group.Count()} files, {group.Sum(f => f.LineCount)} lines");
            }
            
            summary.AppendLine();
            summary.AppendLine("Top Issues:");
            var topIssues = _currentReport.FileAnalyses
                .SelectMany(f => f.Issues)
                .GroupBy(i => i.Message)
                .OrderByDescending(g => g.Count())
                .Take(5);
                
            foreach (var issue in topIssues)
            {
                summary.AppendLine($"- {issue.Key}: {issue.Count()} occurrences");
            }

            return summary.ToString();
        }

        private List<AgentInsight> ParseAIInsights(string aiResponse)
        {
            var insights = new List<AgentInsight>();
            
            // Simple parsing - in a real implementation, you'd have more sophisticated parsing
            var lines = aiResponse.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines.Where(l => l.StartsWith("-") || l.StartsWith("•")))
            {
                var content = line.Substring(1).Trim();
                if (!string.IsNullOrEmpty(content))
                {
                    insights.Add(new AgentInsight
                    {
                        Type = AgentInsightType.AIRecommendation,
                        Title = "AI Recommendation",
                        Description = content,
                        Confidence = 0.8
                    });
                }
            }

            return insights;
        }

        private void ReportProgress(string message, int current, int total)
        {
            var progress = total > 0 ? (double)current / total * 100 : 0;
            
            lock (_lockObject)
            {
                _currentReport.CurrentProgress = progress;
                _currentReport.CurrentActivity = message;
            }

            ProgressChanged?.Invoke(this, new AgentProgressEventArgs
            {
                Message = message,
                Progress = progress,
                Current = current,
                Total = total
            });
        }
    }

    // Data models for Agent Mode
    public class AgentAnalysisReport
    {
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string WorkspacePath { get; set; }
        public AgentAnalysisStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public double CurrentProgress { get; set; }
        public string CurrentActivity { get; set; }
        
        public int TotalFiles { get; set; }
        public int AnalyzedFiles { get; set; }
        public int TotalIssues { get; set; }
        public int TotalSuggestions { get; set; }
        
        public List<AgentFileAnalysis> FileAnalyses { get; set; } = new List<AgentFileAnalysis>();
        public List<AgentInsight> Insights { get; set; } = new List<AgentInsight>();

        public AgentAnalysisReport Clone()
        {
            return new AgentAnalysisReport
            {
                StartTime = StartTime,
                EndTime = EndTime,
                WorkspacePath = WorkspacePath,
                Status = Status,
                ErrorMessage = ErrorMessage,
                CurrentProgress = CurrentProgress,
                CurrentActivity = CurrentActivity,
                TotalFiles = TotalFiles,
                AnalyzedFiles = AnalyzedFiles,
                TotalIssues = TotalIssues,
                TotalSuggestions = TotalSuggestions,
                FileAnalyses = new List<AgentFileAnalysis>(FileAnalyses),
                Insights = new List<AgentInsight>(Insights)
            };
        }
    }

    public class AgentFileAnalysis
    {
        public string FilePath { get; set; }
        public string Language { get; set; }
        public int LineCount { get; set; }
        public List<CodeIssue> Issues { get; set; } = new List<CodeIssue>();
        public List<RefactoringSuggestion> Suggestions { get; set; } = new List<RefactoringSuggestion>();
        public DateTime AnalyzedAt { get; set; }
    }

    public class AgentInsight
    {
        public AgentInsightType Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public IssueSeverity Severity { get; set; }
        public double Confidence { get; set; }
    }

    public enum AgentAnalysisStatus
    {
        NotStarted,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    public enum AgentInsightType
    {
        CommonIssue,
        ProjectStructure,
        PerformanceIssue,
        SecurityConcern,
        AIRecommendation,
        BestPractice
    }

    // Event argument classes
    public class AgentProgressEventArgs : EventArgs
    {
        public string Message { get; set; }
        public double Progress { get; set; }
        public int Current { get; set; }
        public int Total { get; set; }
    }

    public class AgentIssueFoundEventArgs : EventArgs
    {
        public string FilePath { get; set; }
        public List<CodeIssue> Issues { get; set; }
    }

    public class AgentAnalysisCompletedEventArgs : EventArgs
    {
        public AgentAnalysisReport Report { get; set; }
        public string Error { get; set; }
    }
}