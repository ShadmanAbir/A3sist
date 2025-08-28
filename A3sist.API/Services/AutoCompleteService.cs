using A3sist.API.Models;
using A3sist.API.Services;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace A3sist.API.Services;

public class AutoCompleteService : IAutoCompleteService, IDisposable
{
    private readonly ILogger<AutoCompleteService> _logger;
    private readonly IModelManagementService _modelService;
    private readonly ICodeAnalysisService _codeAnalysisService;
    private readonly ConcurrentDictionary<string, List<CompletionItem>> _completionCache;
    private readonly SemaphoreSlim _semaphore;
    private CompletionSettings _settings;
    private bool _disposed;

    private static readonly Dictionary<string, CompletionItemKind> KeywordMappings = new()
    {
        { "class", CompletionItemKind.Class },
        { "interface", CompletionItemKind.Interface },
        { "method", CompletionItemKind.Method },
        { "function", CompletionItemKind.Function },
        { "property", CompletionItemKind.Property },
        { "field", CompletionItemKind.Field },
        { "variable", CompletionItemKind.Variable },
        { "enum", CompletionItemKind.Enum },
        { "namespace", CompletionItemKind.Module }
    };

    public AutoCompleteService(
        ILogger<AutoCompleteService> logger,
        IModelManagementService modelService,
        ICodeAnalysisService codeAnalysisService)
    {
        _logger = logger;
        _modelService = modelService;
        _codeAnalysisService = codeAnalysisService;
        _completionCache = new ConcurrentDictionary<string, List<CompletionItem>>();
        _semaphore = new SemaphoreSlim(1, 1);
        
        // Initialize default settings
        _settings = new CompletionSettings
        {
            IsEnabled = true,
            MaxSuggestions = 20,
            TriggerDelay = 300,
            TriggerCharacters = new List<string> { ".", "(", "[", "<", " " },
            ShowDocumentation = true,
            EnableAICompletion = true
        };
    }

    public async Task<IEnumerable<CompletionItem>> GetCompletionSuggestionsAsync(string code, int position, string language)
    {
        try
        {
            if (!_settings.IsEnabled || string.IsNullOrEmpty(code) || position < 0 || position > code.Length)
                return Enumerable.Empty<CompletionItem>();

            _logger.LogDebug("Getting completion suggestions for {Language} at position {Position}", language, position);

            var completions = new List<CompletionItem>();

            // Get basic language completions
            completions.AddRange(await GetBasicCompletionsAsync(code, position, language));

            // Get context-aware completions
            completions.AddRange(await GetContextAwareCompletionsAsync(code, position, language));

            // Get AI-powered completions if enabled
            if (_settings.EnableAICompletion)
            {
                completions.AddRange(await GetAICompletionsAsync(code, position, language));
            }

            // Remove duplicates and sort
            completions = completions
                .GroupBy(c => c.Label)
                .Select(g => g.OrderByDescending(c => c.Priority).First())
                .OrderByDescending(c => c.Priority)
                .ThenByDescending(c => c.Confidence)
                .ThenBy(c => c.Label)
                .Take(_settings.MaxSuggestions)
                .ToList();

            _logger.LogDebug("Returning {Count} completion suggestions", completions.Count);
            return completions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completion suggestions");
            return Enumerable.Empty<CompletionItem>();
        }
    }

    public async Task<bool> IsAutoCompleteEnabledAsync()
    {
        return await Task.FromResult(_settings.IsEnabled);
    }

    public async Task<bool> SetAutoCompleteEnabledAsync(bool enabled)
    {
        try
        {
            _settings.IsEnabled = enabled;
            _logger.LogInformation("AutoComplete {Status}", enabled ? "enabled" : "disabled");
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting autocomplete enabled status");
            return false;
        }
    }

    public async Task<CompletionSettings> GetSettingsAsync()
    {
        return await Task.FromResult(_settings);
    }

    public async Task<bool> UpdateSettingsAsync(CompletionSettings settings)
    {
        try
        {
            if (settings == null)
                return false;

            await _semaphore.WaitAsync();
            try
            {
                _settings = settings;
                
                // Clear cache when settings change
                _completionCache.Clear();
                
                _logger.LogInformation("AutoComplete settings updated");
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating autocomplete settings");
            return false;
        }
    }

    private async Task<List<CompletionItem>> GetBasicCompletionsAsync(string code, int position, string language)
    {
        var completions = new List<CompletionItem>();

        try
        {
            switch (language.ToLowerInvariant())
            {
                case "csharp":
                    completions.AddRange(await GetCSharpBasicCompletionsAsync(code, position));
                    break;
                case "javascript":
                case "typescript":
                    completions.AddRange(await GetJavaScriptBasicCompletionsAsync(code, position));
                    break;
                case "python":
                    completions.AddRange(await GetPythonBasicCompletionsAsync(code, position));
                    break;
                default:
                    completions.AddRange(await GetGenericBasicCompletionsAsync(code, position, language));
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting basic completions for {Language}", language);
        }

        return completions;
    }

    private async Task<List<CompletionItem>> GetCSharpBasicCompletionsAsync(string code, int position)
    {
        await Task.CompletedTask;
        var completions = new List<CompletionItem>();

        try
        {
            var keywords = new[]
            {
                "public", "private", "protected", "internal", "static", "virtual", "override", "abstract",
                "class", "interface", "struct", "enum", "namespace", "using", "if", "else", "for",
                "foreach", "while", "do", "switch", "case", "default", "break", "continue", "return",
                "try", "catch", "finally", "throw", "new", "this", "base", "var", "string", "int",
                "bool", "double", "float", "decimal", "char", "byte", "long", "short", "object",
                "void", "async", "await", "Task", "List", "Dictionary", "IEnumerable"
            };

            var currentWord = GetCurrentWord(code, position);
            if (string.IsNullOrEmpty(currentWord) || currentWord.Length < 1)
                return completions;

            foreach (var keyword in keywords)
            {
                if (keyword.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                {
                    completions.Add(new CompletionItem
                    {
                        Label = keyword,
                        Detail = $"C# keyword: {keyword}",
                        Documentation = GetKeywordDocumentation(keyword),
                        Kind = GetCompletionKind(keyword),
                        InsertText = keyword,
                        SortOrder = GetKeywordPriority(keyword),
                        Priority = GetKeywordPriority(keyword),
                        Confidence = 0.9
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting C# basic completions");
        }

        return completions;
    }

    private async Task<List<CompletionItem>> GetJavaScriptBasicCompletionsAsync(string code, int position)
    {
        await Task.CompletedTask;
        var completions = new List<CompletionItem>();

        try
        {
            var keywords = new[]
            {
                "function", "var", "let", "const", "if", "else", "for", "while", "do", "switch",
                "case", "default", "break", "continue", "return", "try", "catch", "finally",
                "throw", "new", "this", "true", "false", "null", "undefined", "typeof",
                "instanceof", "class", "extends", "constructor", "static", "async", "await",
                "import", "export", "from", "default", "console", "document", "window"
            };

            var currentWord = GetCurrentWord(code, position);
            if (string.IsNullOrEmpty(currentWord) || currentWord.Length < 1)
                return completions;

            foreach (var keyword in keywords)
            {
                if (keyword.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                {
                    completions.Add(new CompletionItem
                    {
                        Label = keyword,
                        Detail = $"JavaScript keyword: {keyword}",
                        Documentation = GetJavaScriptKeywordDocumentation(keyword),
                        Kind = GetCompletionKind(keyword),
                        InsertText = keyword,
                        Priority = GetKeywordPriority(keyword),
                        Confidence = 0.9
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting JavaScript basic completions");
        }

        return completions;
    }

    private async Task<List<CompletionItem>> GetPythonBasicCompletionsAsync(string code, int position)
    {
        await Task.CompletedTask;
        var completions = new List<CompletionItem>();

        try
        {
            var keywords = new[]
            {
                "def", "class", "if", "elif", "else", "for", "while", "try", "except", "finally",
                "import", "from", "as", "with", "return", "yield", "break", "continue", "pass",
                "and", "or", "not", "in", "is", "True", "False", "None", "self", "print",
                "len", "range", "str", "int", "float", "bool", "list", "dict", "tuple", "set"
            };

            var currentWord = GetCurrentWord(code, position);
            if (string.IsNullOrEmpty(currentWord) || currentWord.Length < 1)
                return completions;

            foreach (var keyword in keywords)
            {
                if (keyword.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                {
                    completions.Add(new CompletionItem
                    {
                        Label = keyword,
                        Detail = $"Python keyword: {keyword}",
                        Documentation = GetPythonKeywordDocumentation(keyword),
                        Kind = GetCompletionKind(keyword),
                        InsertText = keyword,
                        Priority = GetKeywordPriority(keyword),
                        Confidence = 0.9
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Python basic completions");
        }

        return completions;
    }

    private async Task<List<CompletionItem>> GetGenericBasicCompletionsAsync(string code, int position, string language)
    {
        await Task.CompletedTask;
        var completions = new List<CompletionItem>();

        try
        {
            // Extract words from the current document for basic word completion
            var words = ExtractWordsFromCode(code);
            var currentWord = GetCurrentWord(code, position);

            if (string.IsNullOrEmpty(currentWord) || currentWord.Length < 2)
                return completions;

            foreach (var word in words.Where(w => w.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase) && 
                                                  !w.Equals(currentWord, StringComparison.OrdinalIgnoreCase)))
            {
                completions.Add(new CompletionItem
                {
                    Label = word,
                    Detail = "Word from document",
                    Kind = CompletionItemKind.Text,
                    InsertText = word,
                    Priority = 1,
                    Confidence = 0.5
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting generic basic completions");
        }

        return completions;
    }

    private async Task<List<CompletionItem>> GetContextAwareCompletionsAsync(string code, int position, string language)
    {
        var completions = new List<CompletionItem>();

        try
        {
            var context = await _codeAnalysisService.ExtractContextAsync(code, position);
            
            // Add available symbols from context
            if (context.AvailableSymbols != null)
            {
                var currentWord = GetCurrentWord(code, position);
                
                foreach (var symbol in context.AvailableSymbols)
                {
                    if (string.IsNullOrEmpty(currentWord) || 
                        symbol.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                    {
                        completions.Add(new CompletionItem
                        {
                            Label = symbol,
                            Detail = "Available symbol",
                            Kind = CompletionItemKind.Variable,
                            InsertText = symbol,
                            Priority = 5,
                            Confidence = 0.8
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting context-aware completions");
        }

        return completions;
    }

    private async Task<List<CompletionItem>> GetAICompletionsAsync(string code, int position, string language)
    {
        var completions = new List<CompletionItem>();

        try
        {
            var activeModel = await _modelService.GetActiveModelAsync();
            if (activeModel == null || !activeModel.IsAvailable)
                return completions;

            // Create a focused prompt for code completion
            var context = GetCompletionContext(code, position);
            var prompt = CreateCompletionPrompt(context, language);

            var request = new ModelRequest
            {
                Prompt = prompt,
                MaxTokens = 100,
                Temperature = 0.3,
                Parameters = new Dictionary<string, object>
                {
                    ["stop"] = new[] { "\n\n", "```" }
                }
            };

            var response = await _modelService.SendRequestAsync(request);
            
            if (response.Success && !string.IsNullOrEmpty(response.Content))
            {
                var aiCompletions = ParseAICompletions(response.Content, language);
                completions.AddRange(aiCompletions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI completions");
        }

        return completions;
    }

    private string GetCurrentWord(string code, int position)
    {
        if (position <= 0 || position > code.Length)
            return "";

        var start = position - 1;
        var end = position;

        // Find start of word
        while (start >= 0 && (char.IsLetterOrDigit(code[start]) || code[start] == '_'))
        {
            start--;
        }
        start++;

        // Find end of word
        while (end < code.Length && (char.IsLetterOrDigit(code[end]) || code[end] == '_'))
        {
            end++;
        }

        return start < end ? code.Substring(start, end - start) : "";
    }

    private HashSet<string> ExtractWordsFromCode(string code)
    {
        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var matches = Regex.Matches(code, @"\b[a-zA-Z_][a-zA-Z0-9_]{2,}\b");
        
        foreach (Match match in matches)
        {
            words.Add(match.Value);
        }

        return words;
    }

    private CompletionItemKind GetCompletionKind(string keyword)
    {
        if (KeywordMappings.TryGetValue(keyword, out var kind))
            return kind;

        return CompletionItemKind.Keyword;
    }

    private int GetKeywordPriority(string keyword)
    {
        var highPriority = new[] { "public", "private", "class", "interface", "function", "def", "if", "for" };
        var mediumPriority = new[] { "string", "int", "bool", "var", "let", "const" };

        if (highPriority.Contains(keyword))
            return 10;
        if (mediumPriority.Contains(keyword))
            return 7;
        
        return 5;
    }

    private string GetKeywordDocumentation(string keyword)
    {
        return keyword switch
        {
            "public" => "Accessible from any code",
            "private" => "Accessible only within the same class",
            "class" => "Defines a reference type",
            "interface" => "Defines a contract for classes",
            "async" => "Enables asynchronous programming",
            "await" => "Waits for an async operation to complete",
            _ => $"C# keyword: {keyword}"
        };
    }

    private string GetJavaScriptKeywordDocumentation(string keyword)
    {
        return keyword switch
        {
            "function" => "Declares a function",
            "const" => "Declares a read-only named constant",
            "let" => "Declares a block-scoped local variable",
            "async" => "Declares an asynchronous function",
            "await" => "Waits for a Promise to resolve",
            _ => $"JavaScript keyword: {keyword}"
        };
    }

    private string GetPythonKeywordDocumentation(string keyword)
    {
        return keyword switch
        {
            "def" => "Defines a function",
            "class" => "Defines a class",
            "if" => "Conditional statement",
            "for" => "Iteration statement",
            "import" => "Imports a module",
            _ => $"Python keyword: {keyword}"
        };
    }

    private string GetCompletionContext(string code, int position)
    {
        // Get surrounding lines for context
        var lines = code.Split('\n');
        var (currentLine, _) = GetLineAndColumn(code, position);
        
        var startLine = Math.Max(0, currentLine - 3);
        var endLine = Math.Min(lines.Length - 1, currentLine + 1);
        
        return string.Join("\n", lines.Skip(startLine).Take(endLine - startLine + 1));
    }

    private (int line, int column) GetLineAndColumn(string code, int position)
    {
        var lines = code.Split('\n');
        var charCount = 0;
        
        for (int i = 0; i < lines.Length; i++)
        {
            if (charCount + lines[i].Length >= position)
            {
                return (i, position - charCount);
            }
            charCount += lines[i].Length + 1;
        }
        
        return (lines.Length - 1, 0);
    }

    private string CreateCompletionPrompt(string context, string language)
    {
        return $@"Complete the following {language} code. Provide only the most likely next few words or tokens:

{context}

Completion:";
    }

    private List<CompletionItem> ParseAICompletions(string aiResponse, string language)
    {
        var completions = new List<CompletionItem>();

        try
        {
            var suggestions = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                      .Take(5)
                                      .Select(s => s.Trim())
                                      .Where(s => !string.IsNullOrEmpty(s))
                                      .ToList();

            foreach (var suggestion in suggestions)
            {
                completions.Add(new CompletionItem
                {
                    Label = suggestion,
                    Detail = "AI suggestion",
                    Documentation = $"AI-generated completion for {language}",
                    Kind = CompletionItemKind.Text,
                    InsertText = suggestion,
                    Priority = 8,
                    Confidence = 0.7,
                    IsSnippet = false
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing AI completions");
        }

        return completions;
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