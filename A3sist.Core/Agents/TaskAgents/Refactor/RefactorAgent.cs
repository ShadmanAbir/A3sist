using A3sist.Core.Agents.Base;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace A3sist.Core.Agents.TaskAgents.Refactor
{
    /// <summary>
    /// RefactorAgent responsible for code refactoring analysis and suggestions
    /// </summary>
    public class RefactorAgent : BaseAgent
    {
        private readonly IRefactoringService _refactoringService;
        private readonly ICodeAnalysisService _codeAnalysisService;
        private readonly Dictionary<string, ILanguageRefactoringProvider> _languageProviders;

        public override string Name => "RefactorAgent";
        public override AgentType Type => AgentType.Refactor;

        public RefactorAgent(
            ILogger<RefactorAgent> logger,
            IAgentConfiguration configuration,
            IRefactoringService refactoringService,
            ICodeAnalysisService codeAnalysisService,
            IEnumerable<ILanguageRefactoringProvider> languageProviders) : base(logger, configuration)
        {
            _refactoringService = refactoringService ?? throw new ArgumentNullException(nameof(refactoringService));
            _codeAnalysisService = codeAnalysisService ?? throw new ArgumentNullException(nameof(codeAnalysisService));
            _languageProviders = languageProviders?.ToDictionary(lp => lp.Language, lp => lp) ?? new Dictionary<string, ILanguageRefactoringProvider>();
        }

        protected override async Task<bool> CanHandleRequestAsync(AgentRequest request)
        {
            if (request == null)
                return false;

            // RefactorAgent can handle refactoring, code improvement, and optimization requests
            var supportedActions = new[]
            {
                "refactor", "improve", "optimize", "restructure", "reorganize", "simplify",
                "extract", "inline", "rename", "move", "encapsulate", "clean", "cleanup",
                "modernize", "upgrade", "enhance", "beautify", "format", "style"
            };

            var prompt = request.Prompt?.ToLowerInvariant() ?? "";
            var hasCode = !string.IsNullOrEmpty(request.Content) || !string.IsNullOrEmpty(request.FilePath);
            var hasRefactoringContext = request.Context?.ContainsKey("refactoring") == true ||
                                      request.Context?.ContainsKey("improvement") == true ||
                                      request.Context?.ContainsKey("optimization") == true;

            return (supportedActions.Any(action => prompt.Contains(action)) && hasCode) || hasRefactoringContext;
        }

        protected override async Task<AgentResult> HandleRequestAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var action = ExtractActionFromRequest(request);
                
                return action.ToLowerInvariant() switch
                {
                    "analyze" or "suggest" => await AnalyzeCodeForRefactoringAsync(request, cancellationToken),
                    "refactor" or "apply" => await ApplyRefactoringAsync(request, cancellationToken),
                    "validate" or "check" => await ValidateRefactoringAsync(request, cancellationToken),
                    "preview" or "show" => await PreviewRefactoringAsync(request, cancellationToken),
                    "optimize" or "improve" => await OptimizeCodeAsync(request, cancellationToken),
                    "extract" => await ExtractCodeElementAsync(request, cancellationToken),
                    "inline" => await InlineCodeElementAsync(request, cancellationToken),
                    "rename" => await RenameSymbolAsync(request, cancellationToken),
                    "move" => await MoveCodeElementAsync(request, cancellationToken),
                    _ => await HandleGenericRefactoringRequestAsync(request, cancellationToken)
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error handling refactoring request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"RefactorAgent error: {ex.Message}", ex, Name);
            }
        }

        protected override async System.Threading.Tasks.Task InitializeAgentAsync()
        {
            Logger.LogInformation("Initializing RefactorAgent");
            
            // Initialize language providers
            foreach (var provider in _languageProviders.Values)
            {
                await provider.InitializeAsync();
            }
            
            Logger.LogInformation("RefactorAgent initialized with {ProviderCount} language providers", _languageProviders.Count);
        }

        protected override async System.Threading.Tasks.Task ShutdownAgentAsync()
        {
            Logger.LogInformation("Shutting down RefactorAgent");
            
            // Shutdown language providers
            foreach (var provider in _languageProviders.Values)
            {
                await provider.ShutdownAsync();
            }
            
            Logger.LogInformation("RefactorAgent shutdown completed");
        }

        private async Task<AgentResult> AnalyzeCodeForRefactoringAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Analyzing code for refactoring opportunities for request {RequestId}", request.Id);

            try
            {
                var codeInfo = ExtractCodeInfoFromRequest(request);
                
                // Perform code analysis
                var analysisResult = await _codeAnalysisService.AnalyzeCodeAsync(codeInfo.Content, codeInfo.FilePath, cancellationToken);
                
                // Get refactoring suggestions
                var suggestions = await _refactoringService.AnalyzeCodeAsync(codeInfo.Content, codeInfo.FilePath, cancellationToken);
                
                // Get available refactorings
                var availableRefactorings = await _refactoringService.GetAvailableRefactoringsAsync(codeInfo.Content, codeInfo.FilePath, cancellationToken);

                var result = new
                {
                    FilePath = codeInfo.FilePath,
                    Language = codeInfo.Language,
                    Analysis = new
                    {
                        Complexity = analysisResult.Complexity,
                        CodeSmells = analysisResult.CodeSmells.Select(cs => new
                        {
                            cs.Name,
                            cs.Description,
                            cs.Type,
                            cs.Severity,
                            Location = new { cs.StartLine, cs.EndLine, cs.StartColumn, cs.EndColumn },
                            cs.Suggestion,
                            SuggestedRefactorings = cs.SuggestedRefactorings.Select(r => r.ToString())
                        }),
                        Elements = analysisResult.Elements.Select(e => new
                        {
                            e.Name,
                            e.Type,
                            Location = new { e.StartLine, e.EndLine, e.StartColumn, e.EndColumn },
                            e.Signature
                        }),
                        Dependencies = analysisResult.Dependencies
                    },
                    Suggestions = suggestions.Select(s => new
                    {
                        s.Type,
                        s.Title,
                        s.Description,
                        Location = new { s.StartLine, s.EndLine, s.StartColumn, s.EndColumn },
                        s.Severity,
                        s.PreviewCode
                    }),
                    AvailableRefactorings = availableRefactorings.Select(r => r.ToString()),
                    Summary = new
                    {
                        TotalSuggestions = suggestions.Count(),
                        CriticalIssues = suggestions.Count(s => s.Severity == RefactoringSeverity.Critical),
                        WarningIssues = suggestions.Count(s => s.Severity == RefactoringSeverity.Warning),
                        InfoSuggestions = suggestions.Count(s => s.Severity == RefactoringSeverity.Info),
                        ComplexityScore = analysisResult.Complexity?.CyclomaticComplexity ?? 0,
                        MaintainabilityIndex = analysisResult.Complexity?.MaintainabilityIndex ?? 0
                    }
                };

                var jsonResult = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
                
                return AgentResult.CreateSuccess(
                    "Code analysis completed successfully",
                    jsonResult,
                    Name,
                    new Dictionary<string, object>
                    {
                        ["analysisType"] = "refactoring",
                        ["suggestionsCount"] = suggestions.Count(),
                        ["language"] = codeInfo.Language
                    });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error analyzing code for refactoring in request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Failed to analyze code: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> ApplyRefactoringAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Applying refactoring for request {RequestId}", request.Id);

            try
            {
                var codeInfo = ExtractCodeInfoFromRequest(request);
                var refactoringType = ExtractRefactoringTypeFromRequest(request);
                var parameters = ExtractParametersFromRequest(request);

                // Apply the refactoring
                var refactoringResult = await _refactoringService.ApplyRefactoringAsync(
                    codeInfo.Content, refactoringType, parameters, cancellationToken);

                if (!refactoringResult.Success)
                {
                    return AgentResult.CreateFailure(
                        $"Refactoring failed: {refactoringResult.Message}",
                        agentName: Name);
                }

                // Validate the refactoring
                var validationResult = await _refactoringService.ValidateRefactoringAsync(
                    codeInfo.Content, refactoringResult.RefactoredCode, cancellationToken);

                var result = new
                {
                    Success = refactoringResult.Success,
                    RefactoredCode = refactoringResult.RefactoredCode,
                    AppliedRefactoring = refactoringResult.AppliedRefactoring.ToString(),
                    Message = refactoringResult.Message,
                    Warnings = refactoringResult.Warnings,
                    Validation = new
                    {
                        validationResult.IsValid,
                        validationResult.IsSafe,
                        validationResult.ConfidenceScore,
                        validationResult.Errors,
                        validationResult.Warnings,
                        validationResult.Suggestions
                    },
                    Metadata = refactoringResult.Metadata
                };

                var jsonResult = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

                return AgentResult.CreateSuccess(
                    $"Refactoring '{refactoringType}' applied successfully",
                    jsonResult,
                    Name,
                    new Dictionary<string, object>
                    {
                        ["refactoringType"] = refactoringType.ToString(),
                        ["isValid"] = validationResult.IsValid,
                        ["isSafe"] = validationResult.IsSafe,
                        ["confidenceScore"] = validationResult.ConfidenceScore
                    });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error applying refactoring in request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Failed to apply refactoring: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> ValidateRefactoringAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Validating refactoring for request {RequestId}", request.Id);

            try
            {
                var originalCode = request.Content;
                var refactoredCode = request.Context?.TryGetValue("refactoredCode", out var refactoredCodeObj) ? refactoredCodeObj?.ToString() : null;

                if (string.IsNullOrEmpty(originalCode) || string.IsNullOrEmpty(refactoredCode))
                {
                    return AgentResult.CreateFailure(
                        "Both original and refactored code must be provided for validation",
                        agentName: Name);
                }

                var validationResult = await _refactoringService.ValidateRefactoringAsync(
                    originalCode, refactoredCode, cancellationToken);

                var result = new
                {
                    validationResult.IsValid,
                    validationResult.IsSafe,
                    validationResult.ConfidenceScore,
                    validationResult.Errors,
                    validationResult.Warnings,
                    validationResult.Suggestions,
                    Recommendation = GetValidationRecommendation(validationResult)
                };

                var jsonResult = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

                return AgentResult.CreateSuccess(
                    $"Refactoring validation completed - Valid: {validationResult.IsValid}, Safe: {validationResult.IsSafe}",
                    jsonResult,
                    Name,
                    new Dictionary<string, object>
                    {
                        ["isValid"] = validationResult.IsValid,
                        ["isSafe"] = validationResult.IsSafe,
                        ["confidenceScore"] = validationResult.ConfidenceScore
                    });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error validating refactoring in request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Failed to validate refactoring: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> PreviewRefactoringAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Previewing refactoring for request {RequestId}", request.Id);

            try
            {
                var codeInfo = ExtractCodeInfoFromRequest(request);
                var refactoringType = ExtractRefactoringTypeFromRequest(request);
                var parameters = ExtractParametersFromRequest(request);

                // Apply the refactoring to get preview
                var refactoringResult = await _refactoringService.ApplyRefactoringAsync(
                    codeInfo.Content, refactoringType, parameters, cancellationToken);

                if (!refactoringResult.Success)
                {
                    return AgentResult.CreateFailure(
                        $"Failed to generate preview: {refactoringResult.Message}",
                        agentName: Name);
                }

                // Generate diff or comparison
                var preview = GenerateRefactoringPreview(codeInfo.Content, refactoringResult.RefactoredCode);

                var result = new
                {
                    RefactoringType = refactoringType.ToString(),
                    Preview = preview,
                    OriginalCode = codeInfo.Content,
                    RefactoredCode = refactoringResult.RefactoredCode,
                    Changes = CalculateChanges(codeInfo.Content, refactoringResult.RefactoredCode),
                    Warnings = refactoringResult.Warnings
                };

                var jsonResult = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

                return AgentResult.CreateSuccess(
                    $"Refactoring preview generated for '{refactoringType}'",
                    jsonResult,
                    Name,
                    new Dictionary<string, object>
                    {
                        ["refactoringType"] = refactoringType.ToString(),
                        ["hasChanges"] = !string.Equals(codeInfo.Content, refactoringResult.RefactoredCode)
                    });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error generating refactoring preview in request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Failed to generate preview: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> OptimizeCodeAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Optimizing code for request {RequestId}", request.Id);

            try
            {
                var codeInfo = ExtractCodeInfoFromRequest(request);
                
                // Get optimization suggestions
                var suggestions = await _refactoringService.AnalyzeCodeAsync(codeInfo.Content, codeInfo.FilePath, cancellationToken);
                var optimizationSuggestions = suggestions.Where(s => IsOptimizationRefactoring(s.Type)).ToList();

                if (!optimizationSuggestions.Any())
                {
                    return AgentResult.CreateSuccess(
                        "No optimization opportunities found",
                        JsonSerializer.Serialize(new { Message = "Code appears to be well-optimized" }),
                        Name);
                }

                // Apply optimization refactorings
                var optimizedCode = codeInfo.Content;
                var appliedOptimizations = new List<string>();

                foreach (var suggestion in optimizationSuggestions.OrderBy(s => s.Severity))
                {
                    try
                    {
                        var result = await _refactoringService.ApplyRefactoringAsync(
                            optimizedCode, suggestion.Type, suggestion.Parameters, cancellationToken);

                        if (result.Success)
                        {
                            var validation = await _refactoringService.ValidateRefactoringAsync(
                                optimizedCode, result.RefactoredCode, cancellationToken);

                            if (validation.IsValid && validation.IsSafe && validation.ConfidenceScore > 0.8)
                            {
                                optimizedCode = result.RefactoredCode;
                                appliedOptimizations.Add(suggestion.Type.ToString());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Failed to apply optimization {OptimizationType}", suggestion.Type);
                    }
                }

                var optimizationResult = new
                {
                    OriginalCode = codeInfo.Content,
                    OptimizedCode = optimizedCode,
                    AppliedOptimizations = appliedOptimizations,
                    TotalOptimizations = appliedOptimizations.Count,
                    SkippedSuggestions = optimizationSuggestions.Count - appliedOptimizations.Count,
                    Changes = CalculateChanges(codeInfo.Content, optimizedCode)
                };

                var jsonResult = JsonSerializer.Serialize(optimizationResult, new JsonSerializerOptions { WriteIndented = true });

                return AgentResult.CreateSuccess(
                    $"Code optimization completed - Applied {appliedOptimizations.Count} optimizations",
                    jsonResult,
                    Name,
                    new Dictionary<string, object>
                    {
                        ["optimizationsApplied"] = appliedOptimizations.Count,
                        ["hasChanges"] = !string.Equals(codeInfo.Content, optimizedCode)
                    });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error optimizing code in request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Failed to optimize code: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> ExtractCodeElementAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Extracting code element for request {RequestId}", request.Id);

            try
            {
                var codeInfo = ExtractCodeInfoFromRequest(request);
                var extractionType = DetermineExtractionType(request);
                
                var parameters = new Dictionary<string, object>
                {
                    ["extractionType"] = extractionType,
                    ["startLine"] = request.Context?.TryGetValue("startLine", out var startLineObj) ? startLineObj : null,
                    ["endLine"] = request.Context?.TryGetValue("endLine", out var endLineObj) ? endLineObj : null,
                    ["newName"] = request.Context?.TryGetValue("newName", out var newNameObj) ? newNameObj : "ExtractedElement"
                };

                var refactoringType = extractionType switch
                {
                    "method" => RefactoringType.ExtractMethod,
                    "variable" => RefactoringType.ExtractVariable,
                    "constant" => RefactoringType.ExtractConstant,
                    "interface" => RefactoringType.ExtractInterface,
                    _ => RefactoringType.ExtractMethod
                };

                var result = await _refactoringService.ApplyRefactoringAsync(
                    codeInfo.Content, refactoringType, parameters, cancellationToken);

                if (!result.Success)
                {
                    return AgentResult.CreateFailure(
                        $"Failed to extract {extractionType}: {result.Message}",
                        agentName: Name);
                }

                var extractionResult = new
                {
                    ExtractionType = extractionType,
                    Success = result.Success,
                    RefactoredCode = result.RefactoredCode,
                    Message = result.Message,
                    Warnings = result.Warnings
                };

                var jsonResult = JsonSerializer.Serialize(extractionResult, new JsonSerializerOptions { WriteIndented = true });

                return AgentResult.CreateSuccess(
                    $"Successfully extracted {extractionType}",
                    jsonResult,
                    Name,
                    new Dictionary<string, object>
                    {
                        ["extractionType"] = extractionType,
                        ["refactoringType"] = refactoringType.ToString()
                    });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error extracting code element in request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Failed to extract code element: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> InlineCodeElementAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Inlining code element for request {RequestId}", request.Id);

            try
            {
                var codeInfo = ExtractCodeInfoFromRequest(request);
                var inlineType = DetermineInlineType(request);
                
                var parameters = new Dictionary<string, object>
                {
                    ["inlineType"] = inlineType,
                    ["targetName"] = request.Context?.TryGetValue("targetName", out var targetNameObj) ? targetNameObj : null,
                    ["startLine"] = request.Context?.TryGetValue("startLine", out var startLineObj2) ? startLineObj2 : null,
                    ["endLine"] = request.Context?.TryGetValue("endLine", out var endLineObj2) ? endLineObj2 : null
                };

                var refactoringType = inlineType switch
                {
                    "method" => RefactoringType.InlineMethod,
                    "variable" => RefactoringType.InlineVariable,
                    _ => RefactoringType.InlineMethod
                };

                var result = await _refactoringService.ApplyRefactoringAsync(
                    codeInfo.Content, refactoringType, parameters, cancellationToken);

                if (!result.Success)
                {
                    return AgentResult.CreateFailure(
                        $"Failed to inline {inlineType}: {result.Message}",
                        agentName: Name);
                }

                var inlineResult = new
                {
                    InlineType = inlineType,
                    Success = result.Success,
                    RefactoredCode = result.RefactoredCode,
                    Message = result.Message,
                    Warnings = result.Warnings
                };

                var jsonResult = JsonSerializer.Serialize(inlineResult, new JsonSerializerOptions { WriteIndented = true });

                return AgentResult.CreateSuccess(
                    $"Successfully inlined {inlineType}",
                    jsonResult,
                    Name,
                    new Dictionary<string, object>
                    {
                        ["inlineType"] = inlineType,
                        ["refactoringType"] = refactoringType.ToString()
                    });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error inlining code element in request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Failed to inline code element: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> RenameSymbolAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Renaming symbol for request {RequestId}", request.Id);

            try
            {
                var codeInfo = ExtractCodeInfoFromRequest(request);
                var oldName = request.Context?.TryGetValue("oldName", out var oldNameObj) ? oldNameObj?.ToString() : null;
                var newName = request.Context?.TryGetValue("newName", out var newNameObj2) ? newNameObj2?.ToString() : null;

                if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
                {
                    return AgentResult.CreateFailure(
                        "Both oldName and newName must be provided for renaming",
                        agentName: Name);
                }

                var parameters = new Dictionary<string, object>
                {
                    ["oldName"] = oldName,
                    ["newName"] = newName
                };

                var result = await _refactoringService.ApplyRefactoringAsync(
                    codeInfo.Content, RefactoringType.RenameSymbol, parameters, cancellationToken);

                if (!result.Success)
                {
                    return AgentResult.CreateFailure(
                        $"Failed to rename symbol: {result.Message}",
                        agentName: Name);
                }

                var renameResult = new
                {
                    OldName = oldName,
                    NewName = newName,
                    Success = result.Success,
                    RefactoredCode = result.RefactoredCode,
                    Message = result.Message,
                    Warnings = result.Warnings
                };

                var jsonResult = JsonSerializer.Serialize(renameResult, new JsonSerializerOptions { WriteIndented = true });

                return AgentResult.CreateSuccess(
                    $"Successfully renamed '{oldName}' to '{newName}'",
                    jsonResult,
                    Name,
                    new Dictionary<string, object>
                    {
                        ["oldName"] = oldName,
                        ["newName"] = newName,
                        ["refactoringType"] = RefactoringType.RenameSymbol.ToString()
                    });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error renaming symbol in request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Failed to rename symbol: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> MoveCodeElementAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Moving code element for request {RequestId}", request.Id);

            try
            {
                var codeInfo = ExtractCodeInfoFromRequest(request);
                var moveType = DetermineMoveType(request);
                var targetLocation = request.Context?.TryGetValue("targetLocation", out var targetLocationObj) ? targetLocationObj?.ToString() : null;

                if (string.IsNullOrEmpty(targetLocation))
                {
                    return AgentResult.CreateFailure(
                        "Target location must be provided for moving code elements",
                        agentName: Name);
                }

                var parameters = new Dictionary<string, object>
                {
                    ["moveType"] = moveType,
                    ["targetLocation"] = targetLocation,
                    ["elementName"] = request.Context?.TryGetValue("elementName", out var elementNameObj) ? elementNameObj : null
                };

                var refactoringType = moveType switch
                {
                    "method" => RefactoringType.MoveMethod,
                    "field" => RefactoringType.MoveField,
                    _ => RefactoringType.MoveMethod
                };

                var result = await _refactoringService.ApplyRefactoringAsync(
                    codeInfo.Content, refactoringType, parameters, cancellationToken);

                if (!result.Success)
                {
                    return AgentResult.CreateFailure(
                        $"Failed to move {moveType}: {result.Message}",
                        agentName: Name);
                }

                var moveResult = new
                {
                    MoveType = moveType,
                    TargetLocation = targetLocation,
                    Success = result.Success,
                    RefactoredCode = result.RefactoredCode,
                    Message = result.Message,
                    Warnings = result.Warnings
                };

                var jsonResult = JsonSerializer.Serialize(moveResult, new JsonSerializerOptions { WriteIndented = true });

                return AgentResult.CreateSuccess(
                    $"Successfully moved {moveType} to {targetLocation}",
                    jsonResult,
                    Name,
                    new Dictionary<string, object>
                    {
                        ["moveType"] = moveType,
                        ["targetLocation"] = targetLocation,
                        ["refactoringType"] = refactoringType.ToString()
                    });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error moving code element in request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Failed to move code element: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> HandleGenericRefactoringRequestAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Handling generic refactoring request {RequestId}", request.Id);

            try
            {
                // For generic requests, analyze the code and suggest the most appropriate refactorings
                var analysisResult = await AnalyzeCodeForRefactoringAsync(request, cancellationToken);
                
                if (!analysisResult.Success)
                {
                    return analysisResult;
                }

                // Extract suggestions from analysis result
                var analysisData = JsonSerializer.Deserialize<dynamic>(analysisResult.Content);
                
                return AgentResult.CreateSuccess(
                    "Generic refactoring analysis completed. Review suggestions and apply specific refactorings as needed.",
                    analysisResult.Content,
                    Name,
                    analysisResult.Metadata);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error handling generic refactoring request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Failed to handle refactoring request: {ex.Message}", ex, Name);
            }
        }

        #region Helper Methods

        private string ExtractActionFromRequest(AgentRequest request)
        {
            var prompt = request.Prompt?.ToLowerInvariant() ?? "";
            
            if (request.Context?.ContainsKey("action") == true)
                return request.Context["action"].ToString();

            // Extract action from prompt
            var actionKeywords = new Dictionary<string, string>
            {
                ["analyze"] = "analyze",
                ["suggest"] = "suggest",
                ["refactor"] = "refactor",
                ["apply"] = "apply",
                ["validate"] = "validate",
                ["check"] = "validate",
                ["preview"] = "preview",
                ["show"] = "preview",
                ["optimize"] = "optimize",
                ["improve"] = "optimize",
                ["extract"] = "extract",
                ["inline"] = "inline",
                ["rename"] = "rename",
                ["move"] = "move"
            };

            foreach (var keyword in actionKeywords)
            {
                if (prompt.Contains(keyword.Key))
                    return keyword.Value;
            }

            return "analyze"; // Default action
        }

        private RefactoringType ExtractRefactoringTypeFromRequest(AgentRequest request)
        {
            if (request.Context?.ContainsKey("refactoringType") == true)
            {
                var typeString = request.Context["refactoringType"].ToString();
                if (Enum.TryParse<RefactoringType>(typeString, true, out var type))
                    return type;
            }

            var prompt = request.Prompt?.ToLowerInvariant() ?? "";
            
            // Map common phrases to refactoring types
            var refactoringMap = new Dictionary<string, RefactoringType>
            {
                ["extract method"] = RefactoringType.ExtractMethod,
                ["extract variable"] = RefactoringType.ExtractVariable,
                ["extract constant"] = RefactoringType.ExtractConstant,
                ["inline method"] = RefactoringType.InlineMethod,
                ["inline variable"] = RefactoringType.InlineVariable,
                ["rename"] = RefactoringType.RenameSymbol,
                ["move method"] = RefactoringType.MoveMethod,
                ["move field"] = RefactoringType.MoveField,
                ["encapsulate"] = RefactoringType.EncapsulateField,
                ["simplify"] = RefactoringType.SimplifyExpression,
                ["optimize"] = RefactoringType.OptimizePerformance,
                ["modernize"] = RefactoringType.ConvertToAsyncAwait
            };

            foreach (var mapping in refactoringMap)
            {
                if (prompt.Contains(mapping.Key))
                    return mapping.Value;
            }

            return RefactoringType.SimplifyExpression; // Default
        }

        private Dictionary<string, object> ExtractParametersFromRequest(AgentRequest request)
        {
            var parameters = new Dictionary<string, object>();

            if (request.Context != null)
            {
                foreach (var kvp in request.Context)
                {
                    if (kvp.Key != "action" && kvp.Key != "refactoringType")
                    {
                        parameters[kvp.Key] = kvp.Value;
                    }
                }
            }

            return parameters;
        }

        private (string Content, string FilePath, string Language) ExtractCodeInfoFromRequest(AgentRequest request)
        {
            var content = request.Content ?? "";
            var filePath = request.FilePath ?? "";
            var language = DetermineLanguageFromFilePath(filePath);

            return (content, filePath, language);
        }

        private string DetermineLanguageFromFilePath(string filePath)
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
                ".h" or ".hpp" => "header",
                ".xml" or ".xaml" => "xml",
                ".json" => "json",
                ".html" => "html",
                ".css" => "css",
                ".sql" => "sql",
                _ => "unknown"
            };
        }

        private bool IsOptimizationRefactoring(RefactoringType type)
        {
            var optimizationTypes = new[]
            {
                RefactoringType.SimplifyExpression,
                RefactoringType.RemoveUnusedUsings,
                RefactoringType.ConvertToLinq,
                RefactoringType.SimplifyLinqExpression,
                RefactoringType.ConvertToStringInterpolation,
                RefactoringType.UsePatternMatching,
                RefactoringType.ConvertToExpressionBodiedMember,
                RefactoringType.UseVarKeyword,
                RefactoringType.UseCollectionInitializer,
                RefactoringType.UseObjectInitializer,
                RefactoringType.ConvertToNameof,
                RefactoringType.UseThrowExpression,
                RefactoringType.UseConditionalAccess,
                RefactoringType.SimplifyBooleanExpression,
                RefactoringType.RemoveRedundantCode,
                RefactoringType.OptimizePerformance
            };

            return optimizationTypes.Contains(type);
        }

        private string DetermineExtractionType(AgentRequest request)
        {
            var prompt = request.Prompt?.ToLowerInvariant() ?? "";
            
            if (prompt.Contains("method"))
                return "method";
            if (prompt.Contains("variable"))
                return "variable";
            if (prompt.Contains("constant"))
                return "constant";
            if (prompt.Contains("interface"))
                return "interface";
                
            return "method"; // Default
        }

        private string DetermineInlineType(AgentRequest request)
        {
            var prompt = request.Prompt?.ToLowerInvariant() ?? "";
            
            if (prompt.Contains("method"))
                return "method";
            if (prompt.Contains("variable"))
                return "variable";
                
            return "method"; // Default
        }

        private string DetermineMoveType(AgentRequest request)
        {
            var prompt = request.Prompt?.ToLowerInvariant() ?? "";
            
            if (prompt.Contains("method"))
                return "method";
            if (prompt.Contains("field"))
                return "field";
                
            return "method"; // Default
        }

        private string GetValidationRecommendation(RefactoringValidationResult validation)
        {
            if (!validation.IsValid)
                return "Refactoring is not valid and should not be applied.";
            
            if (!validation.IsSafe)
                return "Refactoring may not be safe. Review carefully before applying.";
            
            if (validation.ConfidenceScore < 0.7)
                return "Low confidence in refactoring safety. Manual review recommended.";
            
            if (validation.ConfidenceScore < 0.9)
                return "Moderate confidence in refactoring. Consider testing after application.";
            
            return "High confidence in refactoring safety. Safe to apply.";
        }

        private string GenerateRefactoringPreview(string originalCode, string refactoredCode)
        {
            // Simple diff-like preview
            var originalLines = originalCode.Split('\n');
            var refactoredLines = refactoredCode.Split('\n');
            
            var preview = new List<string>();
            var maxLines = Math.Max(originalLines.Length, refactoredLines.Length);
            
            for (int i = 0; i < maxLines; i++)
            {
                var originalLine = i < originalLines.Length ? originalLines[i] : "";
                var refactoredLine = i < refactoredLines.Length ? refactoredLines[i] : "";
                
                if (originalLine != refactoredLine)
                {
                    if (!string.IsNullOrEmpty(originalLine))
                        preview.Add($"- {originalLine}");
                    if (!string.IsNullOrEmpty(refactoredLine))
                        preview.Add($"+ {refactoredLine}");
                }
                else if (!string.IsNullOrEmpty(originalLine))
                {
                    preview.Add($"  {originalLine}");
                }
            }
            
            return string.Join("\n", preview);
        }

        private object CalculateChanges(string originalCode, string refactoredCode)
        {
            var originalLines = originalCode.Split('\n');
            var refactoredLines = refactoredCode.Split('\n');
            
            var addedLines = 0;
            var removedLines = 0;
            var modifiedLines = 0;
            
            var maxLines = Math.Max(originalLines.Length, refactoredLines.Length);
            
            for (int i = 0; i < maxLines; i++)
            {
                var originalLine = i < originalLines.Length ? originalLines[i] : null;
                var refactoredLine = i < refactoredLines.Length ? refactoredLines[i] : null;
                
                if (originalLine == null && refactoredLine != null)
                    addedLines++;
                else if (originalLine != null && refactoredLine == null)
                    removedLines++;
                else if (originalLine != refactoredLine)
                    modifiedLines++;
            }
            
            return new
            {
                AddedLines = addedLines,
                RemovedLines = removedLines,
                ModifiedLines = modifiedLines,
                TotalChanges = addedLines + removedLines + modifiedLines,
                OriginalLineCount = originalLines.Length,
                RefactoredLineCount = refactoredLines.Length
            };
        }

        #endregion
    }
}