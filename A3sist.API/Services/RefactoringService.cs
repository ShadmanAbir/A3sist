using A3sist.API.Models;
using A3sist.API.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace A3sist.API.Services;

public class RefactoringService : IRefactoringService, IDisposable
{
    private readonly ILogger<RefactoringService> _logger;
    private readonly ICodeAnalysisService _codeAnalysisService;
    private readonly ConcurrentDictionary<string, RefactoringSuggestion> _suggestionCache;
    private readonly SemaphoreSlim _semaphore;
    private bool _disposed;

    public RefactoringService(
        ILogger<RefactoringService> logger, 
        ICodeAnalysisService codeAnalysisService)
    {
        _logger = logger;
        _codeAnalysisService = codeAnalysisService;
        _suggestionCache = new ConcurrentDictionary<string, RefactoringSuggestion>();
        _semaphore = new SemaphoreSlim(1, 1);
    }

    public async Task<IEnumerable<RefactoringSuggestion>> GetRefactoringSuggestionsAsync(string code, string language)
    {
        try
        {
            if (string.IsNullOrEmpty(code))
                return Enumerable.Empty<RefactoringSuggestion>();

            _logger.LogDebug("Getting refactoring suggestions for {Language} code", language);

            var suggestions = new List<RefactoringSuggestion>();

            if (language.Equals("csharp", StringComparison.OrdinalIgnoreCase))
            {
                suggestions.AddRange(await GetCSharpRefactoringSuggestionsAsync(code));
            }
            else
            {
                suggestions.AddRange(await GetGenericRefactoringSuggestionsAsync(code, language));
            }

            // Sort by priority and confidence
            suggestions = suggestions
                .OrderByDescending(s => s.Priority)
                .ThenByDescending(s => s.ConfidenceScore)
                .ToList();

            _logger.LogDebug("Found {Count} refactoring suggestions", suggestions.Count);
            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refactoring suggestions for {Language}", language);
            return Enumerable.Empty<RefactoringSuggestion>();
        }
    }

    public async Task<RefactoringResult> ApplyRefactoringAsync(string suggestionId, string code)
    {
        try
        {
            if (string.IsNullOrEmpty(suggestionId) || string.IsNullOrEmpty(code))
            {
                return new RefactoringResult
                {
                    Id = suggestionId,
                    Success = false,
                    Error = "Invalid suggestion ID or code",
                    ModifiedCode = code,
                    ChangedFiles = new List<string>()
                };
            }

            if (!_suggestionCache.TryGetValue(suggestionId, out var suggestion))
            {
                return new RefactoringResult
                {
                    Id = suggestionId,
                    Success = false,
                    Error = "Suggestion not found",
                    ModifiedCode = code,
                    ChangedFiles = new List<string>()
                };
            }

            _logger.LogInformation("Applying refactoring: {Title}", suggestion.Title);

            var modifiedCode = await ApplyRefactoringInternalAsync(suggestion, code);

            return new RefactoringResult
            {
                Id = suggestionId,
                Success = true,
                ModifiedCode = modifiedCode,
                ChangedFiles = new List<string> { "current_file" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying refactoring {SuggestionId}", suggestionId);
            return new RefactoringResult
            {
                Id = suggestionId,
                Success = false,
                Error = ex.Message,
                ModifiedCode = code,
                ChangedFiles = new List<string>()
            };
        }
    }

    public async Task<RefactoringPreview> PreviewRefactoringAsync(string suggestionId, string code)
    {
        try
        {
            if (!_suggestionCache.TryGetValue(suggestionId, out var suggestion))
            {
                return new RefactoringPreview
                {
                    Id = suggestionId,
                    OriginalCode = code,
                    PreviewCode = code,
                    Changes = new List<CodeChange>()
                };
            }

            var modifiedCode = await ApplyRefactoringInternalAsync(suggestion, code);
            var changes = await GenerateChangeListAsync(code, modifiedCode);

            return new RefactoringPreview
            {
                Id = suggestionId,
                OriginalCode = code,
                PreviewCode = modifiedCode,
                Changes = changes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing refactoring {SuggestionId}", suggestionId);
            return new RefactoringPreview
            {
                Id = suggestionId,
                OriginalCode = code,
                PreviewCode = code,
                Changes = new List<CodeChange>()
            };
        }
    }

    public async Task<IEnumerable<CodeCleanupSuggestion>> GetCleanupSuggestionsAsync(string code, string language)
    {
        try
        {
            if (string.IsNullOrEmpty(code))
                return Enumerable.Empty<CodeCleanupSuggestion>();

            _logger.LogDebug("Getting cleanup suggestions for {Language} code", language);

            var suggestions = new List<CodeCleanupSuggestion>();

            if (language.Equals("csharp", StringComparison.OrdinalIgnoreCase))
            {
                suggestions.AddRange(await GetCSharpCleanupSuggestionsAsync(code));
            }

            suggestions.AddRange(await GetGenericCleanupSuggestionsAsync(code));

            // Sort by impact score
            suggestions = suggestions
                .OrderByDescending(s => s.ImpactScore)
                .ToList();

            _logger.LogDebug("Found {Count} cleanup suggestions", suggestions.Count);
            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cleanup suggestions for {Language}", language);
            return Enumerable.Empty<CodeCleanupSuggestion>();
        }
    }

    private async Task<List<RefactoringSuggestion>> GetCSharpRefactoringSuggestionsAsync(string code)
    {
        var suggestions = new List<RefactoringSuggestion>();

        try
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var root = await syntaxTree.GetRootAsync();

            // Extract method suggestions
            suggestions.AddRange(await GetExtractMethodSuggestionsAsync(root, code));

            // Rename variable suggestions
            suggestions.AddRange(await GetRenameVariableSuggestionsAsync(root, code));

            // Simplify expression suggestions
            suggestions.AddRange(await GetSimplifyExpressionSuggestionsAsync(root, code));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing C# code for refactoring");
        }

        return suggestions;
    }

    private async Task<List<RefactoringSuggestion>> GetExtractMethodSuggestionsAsync(SyntaxNode root, string code)
    {
        await Task.CompletedTask;
        var suggestions = new List<RefactoringSuggestion>();

        try
        {
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                var statements = method.Body?.Statements;
                if (statements == null || statements.Value.Count < 10)
                    continue;

                // Look for potential extract method opportunities
                var lineSpan = method.GetLocation().GetLineSpan();
                var lineCount = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line + 1;

                if (lineCount > 20)
                {
                    var suggestionId = Guid.NewGuid().ToString();
                    var suggestion = new RefactoringSuggestion
                    {
                        Id = suggestionId,
                        Title = $"Extract method from '{method.Identifier.ValueText}'",
                        Description = $"Method '{method.Identifier.ValueText}' is {lineCount} lines long. Consider extracting parts into smaller methods.",
                        Type = RefactoringType.ExtractMethod,
                        OriginalCode = method.ToString(),
                        RefactoredCode = await GenerateExtractMethodCodeAsync(method),
                        StartLine = lineSpan.StartLinePosition.Line + 1,
                        EndLine = lineSpan.EndLinePosition.Line + 1,
                        ConfidenceScore = 0.8,
                        Priority = lineCount > 50 ? 5 : 3
                    };

                    _suggestionCache.TryAdd(suggestionId, suggestion);
                    suggestions.Add(suggestion);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting extract method suggestions");
        }

        return suggestions;
    }

    private async Task<List<RefactoringSuggestion>> GetRenameVariableSuggestionsAsync(SyntaxNode root, string code)
    {
        await Task.CompletedTask;
        var suggestions = new List<RefactoringSuggestion>();

        try
        {
            var variableDeclarators = root.DescendantNodes().OfType<VariableDeclaratorSyntax>();

            foreach (var variable in variableDeclarators)
            {
                var variableName = variable.Identifier.ValueText;

                // Check for poor naming conventions
                if (variableName.Length < 3 && !IsAcceptableShortName(variableName))
                {
                    var suggestionId = Guid.NewGuid().ToString();
                    var betterName = SuggestBetterVariableName(variableName, variable);

                    var suggestion = new RefactoringSuggestion
                    {
                        Id = suggestionId,
                        Title = $"Rename variable '{variableName}' to '{betterName}'",
                        Description = $"Variable name '{variableName}' is too short. Consider using a more descriptive name.",
                        Type = RefactoringType.RenameVariable,
                        OriginalCode = variable.ToString(),
                        RefactoredCode = variable.ToString().Replace(variableName, betterName),
                        StartLine = variable.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        EndLine = variable.GetLocation().GetLineSpan().EndLinePosition.Line + 1,
                        ConfidenceScore = 0.7,
                        Priority = 2
                    };

                    _suggestionCache.TryAdd(suggestionId, suggestion);
                    suggestions.Add(suggestion);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rename variable suggestions");
        }

        return suggestions;
    }

    private async Task<List<RefactoringSuggestion>> GetSimplifyExpressionSuggestionsAsync(SyntaxNode root, string code)
    {
        await Task.CompletedTask;
        var suggestions = new List<RefactoringSuggestion>();

        try
        {
            // Look for string concatenation that could use string interpolation
            var binaryExpressions = root.DescendantNodes().OfType<BinaryExpressionSyntax>()
                .Where(b => b.OperatorToken.IsKind(SyntaxKind.PlusToken));

            foreach (var expr in binaryExpressions)
            {
                if (ContainsStringLiterals(expr))
                {
                    var suggestionId = Guid.NewGuid().ToString();
                    var interpolatedString = ConvertToStringInterpolation(expr);

                    var suggestion = new RefactoringSuggestion
                    {
                        Id = suggestionId,
                        Title = "Use string interpolation",
                        Description = "Replace string concatenation with string interpolation for better readability.",
                        Type = RefactoringType.SimplifyExpression,
                        OriginalCode = expr.ToString(),
                        RefactoredCode = interpolatedString,
                        StartLine = expr.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        EndLine = expr.GetLocation().GetLineSpan().EndLinePosition.Line + 1,
                        ConfidenceScore = 0.9,
                        Priority = 1
                    };

                    _suggestionCache.TryAdd(suggestionId, suggestion);
                    suggestions.Add(suggestion);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting simplify expression suggestions");
        }

        return suggestions;
    }

    private async Task<List<RefactoringSuggestion>> GetGenericRefactoringSuggestionsAsync(string code, string language)
    {
        await Task.CompletedTask;
        var suggestions = new List<RefactoringSuggestion>();

        try
        {
            var lines = code.Split('\n');

            // Look for long lines that could be broken up
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Length > 120)
                {
                    var suggestionId = Guid.NewGuid().ToString();
                    var suggestion = new RefactoringSuggestion
                    {
                        Id = suggestionId,
                        Title = "Break long line",
                        Description = $"Line {i + 1} is {line.Length} characters long. Consider breaking it up.",
                        Type = RefactoringType.SimplifyExpression,
                        OriginalCode = line.Trim(),
                        RefactoredCode = BreakLongLine(line.Trim(), language),
                        StartLine = i + 1,
                        EndLine = i + 1,
                        ConfidenceScore = 0.6,
                        Priority = 1
                    };

                    _suggestionCache.TryAdd(suggestionId, suggestion);
                    suggestions.Add(suggestion);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting generic refactoring suggestions");
        }

        return suggestions;
    }

    private async Task<List<CodeCleanupSuggestion>> GetCSharpCleanupSuggestionsAsync(string code)
    {
        var suggestions = new List<CodeCleanupSuggestion>();

        try
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var root = await syntaxTree.GetRootAsync();

            // Check for unused using statements
            var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToList();
            var usedNamespaces = GetUsedNamespaces(root);

            foreach (var usingDirective in usingDirectives)
            {
                var namespaceName = usingDirective.Name?.ToString();
                if (!string.IsNullOrEmpty(namespaceName) && !IsNamespaceUsed(namespaceName, usedNamespaces))
                {
                    var lineSpan = usingDirective.GetLocation().GetLineSpan();
                    suggestions.Add(new CodeCleanupSuggestion
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = "Remove unused using",
                        Description = $"Remove unused using directive: {namespaceName}",
                        Type = CleanupType.RemoveUnusedUsings,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        OriginalCode = usingDirective.ToString(),
                        CleanedCode = "",
                        ImpactScore = 0.3
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting C# cleanup suggestions");
        }

        return suggestions;
    }

    private async Task<List<CodeCleanupSuggestion>> GetGenericCleanupSuggestionsAsync(string code)
    {
        await Task.CompletedTask;
        var suggestions = new List<CodeCleanupSuggestion>();

        try
        {
            var lines = code.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Check for trailing whitespace
                if (line.EndsWith(" ") || line.EndsWith("\t"))
                {
                    suggestions.Add(new CodeCleanupSuggestion
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = "Remove trailing whitespace",
                        Description = "Remove trailing whitespace from line",
                        Type = CleanupType.FormatCode,
                        Line = i + 1,
                        OriginalCode = line,
                        CleanedCode = line.TrimEnd(),
                        ImpactScore = 0.1
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting generic cleanup suggestions");
        }

        return suggestions;
    }

    private async Task<string> ApplyRefactoringInternalAsync(RefactoringSuggestion suggestion, string code)
    {
        await Task.CompletedTask;

        try
        {
            switch (suggestion.Type)
            {
                case RefactoringType.RenameVariable:
                case RefactoringType.SimplifyExpression:
                    return code.Replace(suggestion.OriginalCode, suggestion.RefactoredCode);
                
                case RefactoringType.ExtractMethod:
                    return await ApplyExtractMethodAsync(suggestion, code);
                
                default:
                    return code;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying refactoring internally");
            return code;
        }
    }

    private async Task<string> ApplyExtractMethodAsync(RefactoringSuggestion suggestion, string code)
    {
        await Task.CompletedTask;
        // Simplified extract method implementation
        return code.Replace(suggestion.OriginalCode, suggestion.RefactoredCode);
    }

    private async Task<List<CodeChange>> GenerateChangeListAsync(string originalCode, string modifiedCode)
    {
        await Task.CompletedTask;
        var changes = new List<CodeChange>();

        if (originalCode != modifiedCode)
        {
            changes.Add(new CodeChange
            {
                Type = ChangeType.Modification,
                StartLine = 1,
                EndLine = originalCode.Split('\n').Length,
                OriginalText = originalCode,
                NewText = modifiedCode
            });
        }

        return changes;
    }

    private async Task<string> GenerateExtractMethodCodeAsync(MethodDeclarationSyntax method)
    {
        await Task.CompletedTask;
        // Simplified implementation - in real scenario, this would analyze the method
        // and suggest specific code blocks to extract
        return $"// Consider extracting parts of {method.Identifier.ValueText} into smaller methods\n{method}";
    }

    private bool IsAcceptableShortName(string name)
    {
        var acceptableShort = new[] { "i", "j", "k", "x", "y", "z", "id", "ex" };
        return acceptableShort.Contains(name.ToLowerInvariant());
    }

    private string SuggestBetterVariableName(string originalName, VariableDeclaratorSyntax variable)
    {
        // Simple name suggestion logic
        return originalName switch
        {
            "i" => "index",
            "j" => "innerIndex", 
            "k" => "outerIndex",
            "x" => "xCoordinate",
            "y" => "yCoordinate",
            "s" => "text",
            "n" => "count",
            _ => $"{originalName}Value"
        };
    }

    private bool ContainsStringLiterals(BinaryExpressionSyntax expr)
    {
        return expr.DescendantNodes().OfType<LiteralExpressionSyntax>()
                   .Any(lit => lit.Token.IsKind(SyntaxKind.StringLiteralToken));
    }

    private string ConvertToStringInterpolation(BinaryExpressionSyntax expr)
    {
        // Simplified conversion - in real scenario, this would properly parse and convert
        return $"$\"{expr.ToString().Replace("\"", "").Replace(" + ", " ")}\"";
    }

    private string BreakLongLine(string line, string language)
    {
        // Simple line breaking logic
        if (line.Contains(","))
        {
            return line.Replace(",", ",\n    ");
        }
        
        if (line.Contains("&&") || line.Contains("||"))
        {
            return line.Replace("&&", "\n    &&").Replace("||", "\n    ||");
        }

        return line;
    }

    private HashSet<string> GetUsedNamespaces(SyntaxNode root)
    {
        var usedNamespaces = new HashSet<string>();
        
        // Simplified namespace usage detection
        var identifiers = root.DescendantTokens()
            .Where(t => t.IsKind(SyntaxKind.IdentifierToken))
            .Select(t => t.ValueText)
            .ToHashSet();

        return usedNamespaces;
    }

    private bool IsNamespaceUsed(string namespaceName, HashSet<string> usedNamespaces)
    {
        // Simplified usage check
        var lastPart = namespaceName.Split('.').LastOrDefault();
        return !string.IsNullOrEmpty(lastPart) && usedNamespaces.Contains(lastPart);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _semaphore.Dispose();
            _disposed = true;
        }
    }
}