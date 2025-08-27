using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;

namespace A3sist.UI.Services
{
    /// <summary>
    /// Provides code suggestions for the editor
    /// </summary>
    public class SuggestionProvider : ISuggestionService
    {
        private readonly IOrchestrator _orchestrator;
        private readonly ICodeAnalysisService _codeAnalysisService;
        private readonly ILogger<SuggestionProvider> _logger;
        private readonly Dictionary<string, List<CodeSuggestion>> _suggestionCache = new();
        private readonly Dictionary<string, DateTime> _lastSuggestionTime = new();
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(2);

        public SuggestionProvider(
            IOrchestrator orchestrator,
            ICodeAnalysisService codeAnalysisService,
            ILogger<SuggestionProvider> logger)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _codeAnalysisService = codeAnalysisService ?? throw new ArgumentNullException(nameof(codeAnalysisService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<CodeSuggestion>> GetSuggestionsAsync(string filePath, int lineNumber)
        {
            try
            {
                _logger.LogDebug("Getting suggestions for file: {FilePath}, line: {LineNumber}", filePath, lineNumber);

                var cacheKey = $"{filePath}:{lineNumber}";

                // Check cache first
                if (IsCacheValid(cacheKey))
                {
                    _logger.LogDebug("Returning cached suggestions for {CacheKey}", cacheKey);
                    return _suggestionCache[cacheKey];
                }

                var suggestions = new List<CodeSuggestion>();

                // Get file content
                var content = await GetFileContentAsync(filePath);
                if (string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning("Could not retrieve content for file: {FilePath}", filePath);
                    return suggestions;
                }

                // Get code analysis
                var analysisResult = await _codeAnalysisService.AnalyzeCodeAsync(content, filePath);

                // Generate suggestions based on analysis
                suggestions.AddRange(await GenerateSuggestionsFromAnalysis(analysisResult, filePath, lineNumber));

                // Get agent-specific suggestions
                suggestions.AddRange(await GetAgentSuggestions(content, filePath, lineNumber));

                // Sort suggestions by confidence and relevance
                suggestions = suggestions
                    .OrderByDescending(s => s.Confidence)
                    .ThenBy(s => Math.Abs(s.StartLine - lineNumber))
                    .ToList();

                // Cache the results
                _suggestionCache[cacheKey] = suggestions;
                _lastSuggestionTime[cacheKey] = DateTime.UtcNow;

                _logger.LogDebug("Generated {Count} suggestions for {CacheKey}", suggestions.Count, cacheKey);
                return suggestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting suggestions for file: {FilePath}, line: {LineNumber}", filePath, lineNumber);
                return new List<CodeSuggestion>();
            }
        }

        public async Task<List<CodeSuggestion>> GetAlternativeSuggestionsAsync(CodeSuggestion originalSuggestion)
        {
            try
            {
                _logger.LogDebug("Getting alternative suggestions for: {SuggestionId}", originalSuggestion.Id);

                var request = new AgentRequest
                {
                    Prompt = $"Provide alternative suggestions for: {originalSuggestion.Description}",
                    FilePath = originalSuggestion.FilePath,
                    Content = originalSuggestion.OriginalText,
                    PreferredAgentType = AgentType.Refactor,
                    Context = new Dictionary<string, object>
                    {
                        ["OriginalSuggestion"] = originalSuggestion,
                        ["RequestType"] = "AlternativeSuggestions",
                        ["MaxAlternatives"] = 3
                    }
                };

                var result = await _orchestrator.ProcessRequestAsync(request);
                
                if (result.Success && result.Metadata.ContainsKey("AlternativeSuggestions"))
                {
                    var alternatives = result.Metadata["AlternativeSuggestions"] as List<CodeSuggestion>;
                    if (alternatives != null)
                    {
                        _logger.LogDebug("Generated {Count} alternative suggestions", alternatives.Count);
                        return alternatives;
                    }
                }

                // Fallback: generate basic alternatives
                return GenerateBasicAlternatives(originalSuggestion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alternative suggestions for: {SuggestionId}", originalSuggestion.Id);
                return new List<CodeSuggestion>();
            }
        }

        public async Task<bool> ApplySuggestionAsync(CodeSuggestion suggestion)
        {
            try
            {
                _logger.LogDebug("Applying suggestion: {SuggestionId}", suggestion.Id);

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Get the text document
                var textDocument = await GetTextDocumentAsync(suggestion.FilePath);
                if (textDocument == null)
                {
                    _logger.LogWarning("Could not get text document for file: {FilePath}", suggestion.FilePath);
                    return false;
                }

                // Apply the suggestion
                using (var edit = textDocument.CreateEdit())
                {
                    var startPosition = GetPosition(textDocument, suggestion.StartLine, suggestion.StartColumn);
                    var endPosition = GetPosition(textDocument, suggestion.EndLine, suggestion.EndColumn);
                    
                    var span = new Span(startPosition, endPosition - startPosition);
                    
                    if (!edit.Replace(span, suggestion.SuggestedText))
                    {
                        _logger.LogWarning("Failed to replace text for suggestion: {SuggestionId}", suggestion.Id);
                        return false;
                    }

                    if (!edit.Apply())
                    {
                        _logger.LogWarning("Failed to apply edit for suggestion: {SuggestionId}", suggestion.Id);
                        return false;
                    }
                }

                // Clear cache for this file
                ClearCacheForFile(suggestion.FilePath);

                _logger.LogInformation("Successfully applied suggestion: {SuggestionId}", suggestion.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying suggestion: {SuggestionId}", suggestion.Id);
                return false;
            }
        }

        private async Task<List<CodeSuggestion>> GenerateSuggestionsFromAnalysis(CodeAnalysisResult analysisResult, string filePath, int lineNumber)
        {
            var suggestions = new List<CodeSuggestion>();

            // Generate suggestions from code smells
            foreach (var codeSmell in analysisResult.CodeSmells ?? Enumerable.Empty<CodeSmell>())
            {
                if (IsNearLine(codeSmell.StartLine, lineNumber, 5))
                {
                    suggestions.Add(new CodeSuggestion
                    {
                        FilePath = filePath,
                        StartLine = codeSmell.StartLine,
                        StartColumn = codeSmell.StartColumn,
                        EndLine = codeSmell.EndLine,
                        EndColumn = codeSmell.EndColumn,
                        Title = $"Fix {codeSmell.Name}",
                        Description = codeSmell.Description,
                        Type = MapCodeSmellToSuggestionType(codeSmell.Type),
                        Severity = MapCodeSmellSeverity(codeSmell.Severity),
                        Confidence = 0.8,
                        AgentName = "CodeAnalysisProvider",
                        Category = "Code Quality",
                        CanAutoApply = codeSmell.Severity <= CodeSmellSeverity.Minor,
                        RequiresConfirmation = codeSmell.Severity > CodeSmellSeverity.Minor
                    });
                }
            }

            // Generate suggestions from complexity metrics
            if (analysisResult.Complexity != null && analysisResult.Complexity.CyclomaticComplexity > 10)
            {
                suggestions.Add(new CodeSuggestion
                {
                    FilePath = filePath,
                    StartLine = lineNumber,
                    StartColumn = 1,
                    EndLine = lineNumber,
                    EndColumn = 1,
                    Title = "Reduce Complexity",
                    Description = $"This code has high cyclomatic complexity ({analysisResult.Complexity.CyclomaticComplexity}). Consider breaking it into smaller methods.",
                    Type = SuggestionType.Refactoring,
                    Severity = SuggestionSeverity.Warning,
                    Confidence = 0.7,
                    AgentName = "CodeAnalysisProvider",
                    Category = "Complexity",
                    CanAutoApply = false,
                    RequiresConfirmation = true
                });
            }

            return suggestions;
        }

        private async Task<List<CodeSuggestion>> GetAgentSuggestions(string content, string filePath, int lineNumber)
        {
            var suggestions = new List<CodeSuggestion>();

            try
            {
                // Get suggestions from different agent types
                var agentTypes = new[] { AgentType.Analyzer, AgentType.Refactor, AgentType.Validator };

                foreach (var agentType in agentTypes)
                {
                    var request = new AgentRequest
                    {
                        Prompt = $"Analyze code around line {lineNumber} and provide suggestions",
                        FilePath = filePath,
                        Content = content,
                        PreferredAgentType = agentType,
                        Context = new Dictionary<string, object>
                        {
                            ["TargetLine"] = lineNumber,
                            ["RequestType"] = "CodeSuggestions",
                            ["MaxSuggestions"] = 3
                        }
                    };

                    var result = await _orchestrator.ProcessRequestAsync(request);
                    
                    if (result.Success && result.Metadata.ContainsKey("Suggestions"))
                    {
                        var agentSuggestions = result.Metadata["Suggestions"] as List<CodeSuggestion>;
                        if (agentSuggestions != null)
                        {
                            suggestions.AddRange(agentSuggestions);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agent suggestions for file: {FilePath}, line: {LineNumber}", filePath, lineNumber);
            }

            return suggestions;
        }

        private List<CodeSuggestion> GenerateBasicAlternatives(CodeSuggestion originalSuggestion)
        {
            var alternatives = new List<CodeSuggestion>();

            // Generate basic alternatives based on suggestion type
            switch (originalSuggestion.Type)
            {
                case SuggestionType.Naming:
                    alternatives.AddRange(GenerateNamingAlternatives(originalSuggestion));
                    break;
                case SuggestionType.Refactoring:
                    alternatives.AddRange(GenerateRefactoringAlternatives(originalSuggestion));
                    break;
                case SuggestionType.StyleImprovement:
                    alternatives.AddRange(GenerateStyleAlternatives(originalSuggestion));
                    break;
            }

            return alternatives;
        }

        private List<CodeSuggestion> GenerateNamingAlternatives(CodeSuggestion original)
        {
            // Basic naming alternatives - in a real implementation, this would use more sophisticated logic
            return new List<CodeSuggestion>
            {
                new CodeSuggestion
                {
                    FilePath = original.FilePath,
                    StartLine = original.StartLine,
                    StartColumn = original.StartColumn,
                    EndLine = original.EndLine,
                    EndColumn = original.EndColumn,
                    Title = "Alternative Naming Convention",
                    Description = "Use camelCase naming convention",
                    Type = SuggestionType.Naming,
                    Severity = SuggestionSeverity.Suggestion,
                    Confidence = 0.6,
                    AgentName = "SuggestionProvider",
                    Category = "Naming"
                }
            };
        }

        private List<CodeSuggestion> GenerateRefactoringAlternatives(CodeSuggestion original)
        {
            return new List<CodeSuggestion>
            {
                new CodeSuggestion
                {
                    FilePath = original.FilePath,
                    StartLine = original.StartLine,
                    StartColumn = original.StartColumn,
                    EndLine = original.EndLine,
                    EndColumn = original.EndColumn,
                    Title = "Extract Method",
                    Description = "Extract this code into a separate method",
                    Type = SuggestionType.Refactoring,
                    Severity = SuggestionSeverity.Suggestion,
                    Confidence = 0.7,
                    AgentName = "SuggestionProvider",
                    Category = "Refactoring"
                }
            };
        }

        private List<CodeSuggestion> GenerateStyleAlternatives(CodeSuggestion original)
        {
            return new List<CodeSuggestion>
            {
                new CodeSuggestion
                {
                    FilePath = original.FilePath,
                    StartLine = original.StartLine,
                    StartColumn = original.StartColumn,
                    EndLine = original.EndLine,
                    EndColumn = original.EndColumn,
                    Title = "Format Code",
                    Description = "Apply consistent code formatting",
                    Type = SuggestionType.StyleImprovement,
                    Severity = SuggestionSeverity.Info,
                    Confidence = 0.9,
                    AgentName = "SuggestionProvider",
                    Category = "Style"
                }
            };
        }

        private async Task<string> GetFileContentAsync(string filePath)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                // Try to get content from open document first
                var textDocument = await GetTextDocumentAsync(filePath);
                if (textDocument != null)
                {
                    return textDocument.GetText();
                }

                // Fallback to reading from file system
                if (System.IO.File.Exists(filePath))
                {
                    return System.IO.File.ReadAllText(filePath);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file content for: {FilePath}", filePath);
                return null;
            }
        }

        private async Task<ITextDocument> GetTextDocumentAsync(string filePath)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                // This is a simplified implementation - in a real scenario, you'd use VS services
                // to get the actual text document from the editor
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting text document for: {FilePath}", filePath);
                return null;
            }
        }

        private int GetPosition(ITextDocument textDocument, int line, int column)
        {
            // Convert line/column to absolute position
            var snapshot = textDocument.TextBuffer.CurrentSnapshot;
            if (line <= 0 || line > snapshot.LineCount)
                return 0;

            var lineSnapshot = snapshot.GetLineFromLineNumber(line - 1);
            return lineSnapshot.Start.Position + Math.Min(column - 1, lineSnapshot.Length);
        }

        private bool IsCacheValid(string cacheKey)
        {
            if (!_lastSuggestionTime.ContainsKey(cacheKey) || !_suggestionCache.ContainsKey(cacheKey))
                return false;

            return DateTime.UtcNow - _lastSuggestionTime[cacheKey] < _cacheTimeout;
        }

        private bool IsNearLine(int targetLine, int currentLine, int threshold)
        {
            return Math.Abs(targetLine - currentLine) <= threshold;
        }

        private SuggestionType MapCodeSmellToSuggestionType(CodeSmellType codeSmellType)
        {
            return codeSmellType switch
            {
                CodeSmellType.LongMethod or CodeSmellType.LargeClass => SuggestionType.Refactoring,
                CodeSmellType.MagicNumbers or CodeSmellType.UncommunicativeNames => SuggestionType.Naming,
                CodeSmellType.DeadCode or CodeSmellType.DuplicatedCode => SuggestionType.CodeFix,
                CodeSmellType.CyclomaticComplexity => SuggestionType.PerformanceOptimization,
                _ => SuggestionType.CodeFix
            };
        }

        private SuggestionSeverity MapCodeSmellSeverity(CodeSmellSeverity codeSmellSeverity)
        {
            return codeSmellSeverity switch
            {
                CodeSmellSeverity.Info => SuggestionSeverity.Info,
                CodeSmellSeverity.Minor => SuggestionSeverity.Suggestion,
                CodeSmellSeverity.Major => SuggestionSeverity.Warning,
                CodeSmellSeverity.Critical => SuggestionSeverity.Error,
                CodeSmellSeverity.Blocker => SuggestionSeverity.Critical,
                _ => SuggestionSeverity.Info
            };
        }

        private void ClearCacheForFile(string filePath)
        {
            var keysToRemove = _suggestionCache.Keys.Where(k => k.StartsWith(filePath + ":")).ToList();
            foreach (var key in keysToRemove)
            {
                _suggestionCache.Remove(key);
                _lastSuggestionTime.Remove(key);
            }
        }

        public void ClearCache()
        {
            _suggestionCache.Clear();
            _lastSuggestionTime.Clear();
        }
    }
}