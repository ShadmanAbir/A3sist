using A3sist.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Core.Services
{
    /// <summary>
    /// Core refactoring service that coordinates language-specific providers
    /// </summary>
    public class RefactoringService : IRefactoringService
    {
        private readonly ILogger<RefactoringService> _logger;
        private readonly ICodeAnalysisService _codeAnalysisService;
        private readonly Dictionary<string, ILanguageRefactoringProvider> _languageProviders;

        public RefactoringService(
            ILogger<RefactoringService> logger,
            ICodeAnalysisService codeAnalysisService,
            IEnumerable<ILanguageRefactoringProvider> languageProviders)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _codeAnalysisService = codeAnalysisService ?? throw new ArgumentNullException(nameof(codeAnalysisService));
            _languageProviders = languageProviders?.ToDictionary(lp => lp.Language, lp => lp) ?? new Dictionary<string, ILanguageRefactoringProvider>();
        }

        public async Task<IEnumerable<RefactoringSuggestion>> AnalyzeCodeAsync(string code, string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(code))
                return Enumerable.Empty<RefactoringSuggestion>();

            try
            {
                var language = DetermineLanguage(filePath);
                var suggestions = new List<RefactoringSuggestion>();

                // Get general code analysis
                var analysisResult = await _codeAnalysisService.AnalyzeCodeAsync(code, filePath, cancellationToken);
                
                // Convert code smells to refactoring suggestions
                foreach (var codeSmell in analysisResult.CodeSmells)
                {
                    suggestions.AddRange(ConvertCodeSmellToSuggestions(codeSmell));
                }

                // Get language-specific suggestions
                if (_languageProviders.TryGetValue(language, out var provider))
                {
                    var languageSpecificSuggestions = await provider.AnalyzeCodeAsync(code, filePath, cancellationToken);
                    suggestions.AddRange(languageSpecificSuggestions);
                }

                // Add general refactoring suggestions based on complexity
                if (analysisResult.Complexity != null)
                {
                    suggestions.AddRange(GenerateComplexityBasedSuggestions(analysisResult.Complexity, code));
                }

                return suggestions.OrderByDescending(s => s.Severity).ThenBy(s => s.StartLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing code for refactoring suggestions");
                return Enumerable.Empty<RefactoringSuggestion>();
            }
        }

        public async Task<RefactoringResult> ApplyRefactoringAsync(string code, RefactoringType refactoringType, Dictionary<string, object> parameters = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(code))
            {
                return new RefactoringResult
                {
                    Success = false,
                    Message = "Code cannot be empty",
                    RefactoredCode = code
                };
            }

            try
            {
                var language = DetermineLanguage(parameters?.GetValueOrDefault("filePath")?.ToString() ?? "");
                
                // Try language-specific provider first
                if (_languageProviders.TryGetValue(language, out var provider) && provider.CanHandleRefactoring(refactoringType))
                {
                    return await provider.ApplyRefactoringAsync(code, refactoringType, parameters, cancellationToken);
                }

                // Fall back to generic refactoring
                return await ApplyGenericRefactoringAsync(code, refactoringType, parameters, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying refactoring {RefactoringType}", refactoringType);
                return new RefactoringResult
                {
                    Success = false,
                    Message = $"Failed to apply refactoring: {ex.Message}",
                    RefactoredCode = code
                };
            }
        }

        public async Task<RefactoringValidationResult> ValidateRefactoringAsync(string originalCode, string refactoredCode, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(originalCode) || string.IsNullOrEmpty(refactoredCode))
            {
                return new RefactoringValidationResult
                {
                    IsValid = false,
                    IsSafe = false,
                    Errors = new[] { "Both original and refactored code must be provided" },
                    ConfidenceScore = 0.0
                };
            }

            try
            {
                var errors = new List<string>();
                var warnings = new List<string>();
                var suggestions = new List<string>();

                // Basic validation checks
                var basicValidation = await PerformBasicValidationAsync(originalCode, refactoredCode, cancellationToken);
                errors.AddRange(basicValidation.Errors);
                warnings.AddRange(basicValidation.Warnings);

                // Syntax validation
                var syntaxValidation = await ValidateSyntaxAsync(refactoredCode, cancellationToken);
                errors.AddRange(syntaxValidation.Errors);
                warnings.AddRange(syntaxValidation.Warnings);

                // Semantic validation
                var semanticValidation = await ValidateSemanticsAsync(originalCode, refactoredCode, cancellationToken);
                errors.AddRange(semanticValidation.Errors);
                warnings.AddRange(semanticValidation.Warnings);
                suggestions.AddRange(semanticValidation.Suggestions);

                // Calculate confidence score
                var confidenceScore = CalculateConfidenceScore(errors, warnings, originalCode, refactoredCode);

                return new RefactoringValidationResult
                {
                    IsValid = !errors.Any(),
                    IsSafe = !errors.Any() && warnings.Count <= 2,
                    Errors = errors,
                    Warnings = warnings,
                    Suggestions = suggestions,
                    ConfidenceScore = confidenceScore
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating refactoring");
                return new RefactoringValidationResult
                {
                    IsValid = false,
                    IsSafe = false,
                    Errors = new[] { $"Validation failed: {ex.Message}" },
                    ConfidenceScore = 0.0
                };
            }
        }

        public async Task<IEnumerable<RefactoringType>> GetAvailableRefactoringsAsync(string code, string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(code))
                return Enumerable.Empty<RefactoringType>();

            try
            {
                var language = DetermineLanguage(filePath);
                var availableRefactorings = new HashSet<RefactoringType>();

                // Add general refactorings
                availableRefactorings.UnionWith(GetGeneralRefactorings());

                // Add language-specific refactorings
                if (_languageProviders.TryGetValue(language, out var provider))
                {
                    availableRefactorings.UnionWith(provider.SupportedRefactorings);
                }

                // Filter based on code analysis
                var analysisResult = await _codeAnalysisService.AnalyzeCodeAsync(code, filePath, cancellationToken);
                var contextualRefactorings = GetContextualRefactorings(analysisResult);
                
                return availableRefactorings.Intersect(contextualRefactorings).OrderBy(r => r.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available refactorings");
                return GetGeneralRefactorings();
            }
        }

        #region Private Methods

        private string DetermineLanguage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "unknown";

            var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            
            return extension switch
            {
                ".cs" => "csharp",
                ".js" => "javascript",
                ".ts" => "typescript",
                ".py" => "python",
                ".java" => "java",
                ".cpp" or ".cc" or ".cxx" => "cpp",
                ".c" => "c",
                _ => "unknown"
            };
        }

        private IEnumerable<RefactoringSuggestion> ConvertCodeSmellToSuggestions(CodeSmell codeSmell)
        {
            var suggestions = new List<RefactoringSuggestion>();

            foreach (var refactoringType in codeSmell.SuggestedRefactorings)
            {
                suggestions.Add(new RefactoringSuggestion
                {
                    Type = refactoringType,
                    Title = GetRefactoringTitle(refactoringType, codeSmell),
                    Description = $"Address {codeSmell.Name}: {codeSmell.Description}",
                    StartLine = codeSmell.StartLine,
                    EndLine = codeSmell.EndLine,
                    StartColumn = codeSmell.StartColumn,
                    EndColumn = codeSmell.EndColumn,
                    Severity = MapCodeSmellSeverityToRefactoringSeverity(codeSmell.Severity),
                    Parameters = new Dictionary<string, object>
                    {
                        ["codeSmellType"] = codeSmell.Type.ToString(),
                        ["suggestion"] = codeSmell.Suggestion
                    }
                });
            }

            return suggestions;
        }

        private IEnumerable<RefactoringSuggestion> GenerateComplexityBasedSuggestions(ComplexityMetrics complexity, string code)
        {
            var suggestions = new List<RefactoringSuggestion>();

            if (complexity.CyclomaticComplexity > 10)
            {
                suggestions.Add(new RefactoringSuggestion
                {
                    Type = RefactoringType.ExtractMethod,
                    Title = "Extract Method to Reduce Complexity",
                    Description = $"Cyclomatic complexity is {complexity.CyclomaticComplexity}. Consider extracting methods to improve readability.",
                    Severity = RefactoringSeverity.Warning,
                    Parameters = new Dictionary<string, object>
                    {
                        ["complexityScore"] = complexity.CyclomaticComplexity
                    }
                });
            }

            if (complexity.LinesOfCode > 100)
            {
                suggestions.Add(new RefactoringSuggestion
                {
                    Type = RefactoringType.ExtractMethod,
                    Title = "Extract Methods from Large Code Block",
                    Description = $"Code has {complexity.LinesOfCode} lines. Consider breaking it into smaller methods.",
                    Severity = RefactoringSeverity.Suggestion,
                    Parameters = new Dictionary<string, object>
                    {
                        ["linesOfCode"] = complexity.LinesOfCode
                    }
                });
            }

            if (complexity.LackOfCohesion > 0.8)
            {
                suggestions.Add(new RefactoringSuggestion
                {
                    Type = RefactoringType.ExtractInterface,
                    Title = "Extract Interface to Improve Cohesion",
                    Description = $"Low cohesion detected ({complexity.LackOfCohesion:F2}). Consider extracting interfaces or splitting classes.",
                    Severity = RefactoringSeverity.Warning,
                    Parameters = new Dictionary<string, object>
                    {
                        ["cohesionScore"] = complexity.LackOfCohesion
                    }
                });
            }

            return suggestions;
        }

        private async Task<RefactoringResult> ApplyGenericRefactoringAsync(string code, RefactoringType refactoringType, Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            // Basic generic refactorings that work across languages
            return refactoringType switch
            {
                RefactoringType.RemoveUnusedUsings => await RemoveUnusedImportsAsync(code, cancellationToken),
                RefactoringType.SimplifyExpression => await SimplifyExpressionsAsync(code, cancellationToken),
                RefactoringType.RemoveRedundantCode => await RemoveRedundantCodeAsync(code, cancellationToken),
                _ => new RefactoringResult
                {
                    Success = false,
                    Message = $"Generic refactoring for {refactoringType} is not implemented",
                    RefactoredCode = code
                }
            };
        }

        private async Task<RefactoringResult> RemoveUnusedImportsAsync(string code, CancellationToken cancellationToken)
        {
            // Simple implementation - remove lines that look like unused imports
            var lines = code.Split('\n');
            var filteredLines = new List<string>();
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Skip lines that look like unused imports (very basic heuristic)
                if (trimmedLine.StartsWith("using ") && !IsImportUsed(trimmedLine, code))
                {
                    continue; // Skip this line
                }
                
                filteredLines.Add(line);
            }

            var refactoredCode = string.Join("\n", filteredLines);
            
            return new RefactoringResult
            {
                Success = true,
                RefactoredCode = refactoredCode,
                AppliedRefactoring = RefactoringType.RemoveUnusedUsings,
                Message = "Removed unused import statements"
            };
        }

        private async Task<RefactoringResult> SimplifyExpressionsAsync(string code, CancellationToken cancellationToken)
        {
            // Basic expression simplification
            var simplifiedCode = code
                .Replace(" == true", "")
                .Replace(" == false", " == false") // Keep this for now
                .Replace("  ", " ") // Remove double spaces
                .Replace("\t\t", "\t"); // Remove double tabs

            return new RefactoringResult
            {
                Success = true,
                RefactoredCode = simplifiedCode,
                AppliedRefactoring = RefactoringType.SimplifyExpression,
                Message = "Simplified expressions"
            };
        }

        private async Task<RefactoringResult> RemoveRedundantCodeAsync(string code, CancellationToken cancellationToken)
        {
            // Basic redundant code removal
            var lines = code.Split('\n');
            var filteredLines = new List<string>();
            var previousLine = "";
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Skip duplicate empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine) && string.IsNullOrWhiteSpace(previousLine))
                {
                    continue;
                }
                
                filteredLines.Add(line);
                previousLine = trimmedLine;
            }

            var refactoredCode = string.Join("\n", filteredLines);
            
            return new RefactoringResult
            {
                Success = true,
                RefactoredCode = refactoredCode,
                AppliedRefactoring = RefactoringType.RemoveRedundantCode,
                Message = "Removed redundant code"
            };
        }

        private bool IsImportUsed(string importLine, string code)
        {
            // Very basic check - extract the imported namespace/module and see if it's used
            // This is a simplified implementation
            var parts = importLine.Split(' ', ';');
            if (parts.Length > 1)
            {
                var importedName = parts[1].Trim();
                return code.Contains(importedName) && code.IndexOf(importedName) != code.IndexOf(importLine);
            }
            
            return true; // Conservative approach - assume it's used if we can't determine
        }

        private async Task<(List<string> Errors, List<string> Warnings)> PerformBasicValidationAsync(string originalCode, string refactoredCode, CancellationToken cancellationToken)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // Check if refactored code is significantly different
            if (string.Equals(originalCode, refactoredCode, StringComparison.Ordinal))
            {
                warnings.Add("No changes detected in refactored code");
            }

            // Check for significant size changes
            var originalLength = originalCode.Length;
            var refactoredLength = refactoredCode.Length;
            var changeRatio = Math.Abs(refactoredLength - originalLength) / (double)originalLength;

            if (changeRatio > 0.5)
            {
                warnings.Add($"Significant size change detected: {changeRatio:P0}");
            }

            // Check for empty result
            if (string.IsNullOrWhiteSpace(refactoredCode))
            {
                errors.Add("Refactored code is empty");
            }

            return (errors, warnings);
        }

        private async Task<(List<string> Errors, List<string> Warnings)> ValidateSyntaxAsync(string code, CancellationToken cancellationToken)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // Basic syntax checks (language-agnostic)
            var openBraces = code.Count(c => c == '{');
            var closeBraces = code.Count(c => c == '}');
            
            if (openBraces != closeBraces)
            {
                errors.Add($"Mismatched braces: {openBraces} open, {closeBraces} close");
            }

            var openParens = code.Count(c => c == '(');
            var closeParens = code.Count(c => c == ')');
            
            if (openParens != closeParens)
            {
                errors.Add($"Mismatched parentheses: {openParens} open, {closeParens} close");
            }

            return (errors, warnings);
        }

        private async Task<(List<string> Errors, List<string> Warnings, List<string> Suggestions)> ValidateSemanticsAsync(string originalCode, string refactoredCode, CancellationToken cancellationToken)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            var suggestions = new List<string>();

            // Basic semantic validation
            // This would be much more sophisticated in a real implementation
            
            // Check for potential breaking changes
            var originalMethods = ExtractMethodNames(originalCode);
            var refactoredMethods = ExtractMethodNames(refactoredCode);
            
            var removedMethods = originalMethods.Except(refactoredMethods).ToList();
            var addedMethods = refactoredMethods.Except(originalMethods).ToList();
            
            if (removedMethods.Any())
            {
                warnings.Add($"Methods potentially removed: {string.Join(", ", removedMethods)}");
            }
            
            if (addedMethods.Any())
            {
                suggestions.Add($"New methods added: {string.Join(", ", addedMethods)}");
            }

            return (errors, warnings, suggestions);
        }

        private double CalculateConfidenceScore(List<string> errors, List<string> warnings, string originalCode, string refactoredCode)
        {
            var baseScore = 1.0;
            
            // Reduce score for errors
            baseScore -= errors.Count * 0.3;
            
            // Reduce score for warnings
            baseScore -= warnings.Count * 0.1;
            
            // Reduce score for significant changes
            var changeRatio = Math.Abs(refactoredCode.Length - originalCode.Length) / (double)originalCode.Length;
            if (changeRatio > 0.3)
            {
                baseScore -= 0.2;
            }
            
            return Math.Max(0.0, Math.Min(1.0, baseScore));
        }

        private IEnumerable<RefactoringType> GetGeneralRefactorings()
        {
            return new[]
            {
                RefactoringType.ExtractMethod,
                RefactoringType.ExtractVariable,
                RefactoringType.ExtractConstant,
                RefactoringType.InlineMethod,
                RefactoringType.InlineVariable,
                RefactoringType.RenameSymbol,
                RefactoringType.SimplifyExpression,
                RefactoringType.RemoveUnusedUsings,
                RefactoringType.RemoveRedundantCode,
                RefactoringType.OptimizePerformance
            };
        }

        private IEnumerable<RefactoringType> GetContextualRefactorings(CodeAnalysisResult analysisResult)
        {
            var refactorings = new HashSet<RefactoringType>(GetGeneralRefactorings());
            
            // Add contextual refactorings based on analysis
            if (analysisResult.Complexity?.CyclomaticComplexity > 10)
            {
                refactorings.Add(RefactoringType.ExtractMethod);
            }
            
            if (analysisResult.CodeSmells.Any(cs => cs.Type == CodeSmellType.LongMethod))
            {
                refactorings.Add(RefactoringType.ExtractMethod);
            }
            
            if (analysisResult.CodeSmells.Any(cs => cs.Type == CodeSmellType.DuplicatedCode))
            {
                refactorings.Add(RefactoringType.ExtractMethod);
                refactorings.Add(RefactoringType.ExtractVariable);
            }
            
            return refactorings;
        }

        private string GetRefactoringTitle(RefactoringType refactoringType, CodeSmell codeSmell)
        {
            return refactoringType switch
            {
                RefactoringType.ExtractMethod => $"Extract Method to Address {codeSmell.Name}",
                RefactoringType.ExtractVariable => $"Extract Variable to Address {codeSmell.Name}",
                RefactoringType.ExtractConstant => $"Extract Constant to Address {codeSmell.Name}",
                RefactoringType.SimplifyExpression => $"Simplify Expression to Address {codeSmell.Name}",
                RefactoringType.RenameSymbol => $"Rename Symbol to Address {codeSmell.Name}",
                _ => $"Apply {refactoringType} to Address {codeSmell.Name}"
            };
        }

        private RefactoringSeverity MapCodeSmellSeverityToRefactoringSeverity(CodeSmellSeverity severity)
        {
            return severity switch
            {
                CodeSmellSeverity.Info => RefactoringSeverity.Info,
                CodeSmellSeverity.Minor => RefactoringSeverity.Suggestion,
                CodeSmellSeverity.Major => RefactoringSeverity.Warning,
                CodeSmellSeverity.Critical => RefactoringSeverity.Error,
                CodeSmellSeverity.Blocker => RefactoringSeverity.Critical,
                _ => RefactoringSeverity.Info
            };
        }

        private IEnumerable<string> ExtractMethodNames(string code)
        {
            // Very basic method name extraction - would be much more sophisticated in real implementation
            var methods = new List<string>();
            var lines = code.Split('\n');
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Contains("(") && trimmed.Contains(")") && 
                    (trimmed.Contains("public") || trimmed.Contains("private") || trimmed.Contains("protected")))
                {
                    var parts = trimmed.Split('(')[0].Split(' ');
                    if (parts.Length > 0)
                    {
                        var methodName = parts.Last().Trim();
                        if (!string.IsNullOrEmpty(methodName))
                        {
                            methods.Add(methodName);
                        }
                    }
                }
            }
            
            return methods.Distinct();
        }

        #endregion
    }
}