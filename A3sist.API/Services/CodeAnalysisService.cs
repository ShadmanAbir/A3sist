using A3sist.API.Models;
using A3sist.API.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace A3sist.API.Services;

public class CodeAnalysisService : ICodeAnalysisService, IDisposable
{
    private readonly ILogger<CodeAnalysisService> _logger;
    private readonly ConcurrentDictionary<string, SyntaxTree> _syntaxTreeCache;
    private readonly SemaphoreSlim _semaphore;
    private bool _disposed;

    private static readonly Dictionary<string, string> FileExtensionToLanguage = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".cs", "csharp" }, { ".vb", "vb" }, { ".fs", "fsharp" }, { ".cpp", "cpp" }, { ".c", "c" },
        { ".h", "c" }, { ".java", "java" }, { ".py", "python" }, { ".js", "javascript" },
        { ".ts", "typescript" }, { ".html", "html" }, { ".xml", "xml" }, { ".json", "json" },
        { ".yml", "yaml" }, { ".md", "markdown" }, { ".sql", "sql" }, { ".ps1", "powershell" }
    };

    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "csharp", "vb", "fsharp", "cpp", "c", "java", "python", "javascript", "typescript",
        "html", "xml", "json", "yaml", "markdown", "text", "sql", "powershell"
    };

    public CodeAnalysisService(ILogger<CodeAnalysisService> logger)
    {
        _logger = logger;
        _syntaxTreeCache = new ConcurrentDictionary<string, SyntaxTree>();
        _semaphore = new SemaphoreSlim(1, 1);
    }

    public async Task<string> DetectLanguageAsync(string code, string? fileName = null)
    {
        try
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                var extension = Path.GetExtension(fileName);
                if (FileExtensionToLanguage.TryGetValue(extension, out var language))
                {
                    return language;
                }
            }

            return await DetectLanguageByContentAsync(code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting language");
            return "text";
        }
    }

    public async Task<CodeContext> ExtractContextAsync(string code, int position)
    {
        try
        {
            if (string.IsNullOrEmpty(code) || position < 0 || position > code.Length)
                return new CodeContext();

            var language = await DetectLanguageAsync(code);
            var lines = code.Split('\n');
            var (currentLine, currentColumn) = GetLineAndColumn(code, position);

            var context = new CodeContext
            {
                Language = language,
                LineNumber = currentLine,
                ColumnNumber = currentColumn,
                ImportStatements = new List<string>(),
                UsingStatements = new List<string>(),
                AvailableSymbols = new List<string>(),
                Metadata = new Dictionary<string, object>()
            };

            // Extract surrounding code
            var startLine = Math.Max(0, currentLine - 5);
            var endLine = Math.Min(lines.Length - 1, currentLine + 5);
            context.SurroundingCode = string.Join("\n", lines.Skip(startLine).Take(endLine - startLine + 1));

            if (language.Equals("csharp", StringComparison.OrdinalIgnoreCase))
            {
                await ExtractCSharpContextAsync(code, position, context);
            }

            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting context");
            return new CodeContext { Language = "text" };
        }
    }

    public async Task<IEnumerable<CodeIssue>> AnalyzeCodeAsync(string code, string language)
    {
        try
        {
            var issues = new List<CodeIssue>();
            
            if (string.IsNullOrEmpty(code))
                return issues;

            if (language.Equals("csharp", StringComparison.OrdinalIgnoreCase))
            {
                issues.AddRange(await AnalyzeCSharpCodeAsync(code));
            }
            
            issues.AddRange(await AnalyzeCommonIssuesAsync(code));
            return issues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing code");
            return Enumerable.Empty<CodeIssue>();
        }
    }

    public async Task<SyntaxTree> GetSyntaxTreeAsync(string code, string language)
    {
        try
        {
            var cacheKey = $"{language}:{code.GetHashCode()}";
            
            if (_syntaxTreeCache.TryGetValue(cacheKey, out var cachedTree))
                return cachedTree;

            await _semaphore.WaitAsync();
            try
            {
                if (_syntaxTreeCache.TryGetValue(cacheKey, out cachedTree))
                    return cachedTree;

                var syntaxTree = await CreateSyntaxTreeAsync(code, language);
                
                if (_syntaxTreeCache.Count < 100)
                {
                    _syntaxTreeCache.TryAdd(cacheKey, syntaxTree);
                }

                return syntaxTree;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating syntax tree");
            return CreateEmptySyntaxTree(code);
        }
    }

    public async Task<IEnumerable<string>> GetSupportedLanguagesAsync()
    {
        return await Task.FromResult(SupportedLanguages);
    }

    private async Task<string> DetectLanguageByContentAsync(string code)
    {
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(code))
            return "text";

        var codeLines = code.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                           .Select(line => line.Trim())
                           .Where(line => !string.IsNullOrEmpty(line))
                           .ToList();

        // C# detection
        if (codeLines.Any(line => 
            line.StartsWith("using ") && line.EndsWith(";") ||
            line.Contains("namespace ") ||
            line.Contains("public class ")))
        {
            return "csharp";
        }

        // JavaScript detection
        if (codeLines.Any(line =>
            line.StartsWith("const ") ||
            line.StartsWith("let ") ||
            line.Contains("function ") ||
            line.Contains("console.log")))
        {
            return "javascript";
        }

        // Python detection
        if (codeLines.Any(line =>
            line.StartsWith("def ") ||
            line.StartsWith("import ") ||
            line.Contains("print(")))
        {
            return "python";
        }

        // JSON detection
        if ((code.TrimStart().StartsWith("{") && code.TrimEnd().EndsWith("}")) ||
            (code.TrimStart().StartsWith("[") && code.TrimEnd().EndsWith("]")))
        {
            try
            {
                System.Text.Json.JsonDocument.Parse(code);
                return "json";
            }
            catch { }
        }

        return "text";
    }

    private (int line, int column) GetLineAndColumn(string code, int position)
    {
        var lines = code.Split('\n');
        var charCount = 0;
        
        for (int i = 0; i < lines.Length; i++)
        {
            if (charCount + lines[i].Length >= position)
            {
                return (i + 1, position - charCount + 1);
            }
            charCount += lines[i].Length + 1;
        }
        
        return (lines.Length, 1);
    }

    private async Task ExtractCSharpContextAsync(string code, int position, CodeContext context)
    {
        try
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var root = await syntaxTree.GetRootAsync();

            // Extract using statements
            var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>();
            context.UsingStatements = usingDirectives.Select(u => u.ToString().Trim()).ToList();

            // Find current context
            var textSpan = new Microsoft.CodeAnalysis.Text.TextSpan(position, 0);
            var node = root.FindNode(textSpan);

            var methodNode = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (methodNode != null)
            {
                context.CurrentMethod = methodNode.Identifier.ValueText;
            }

            var classNode = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classNode != null)
            {
                context.CurrentClass = classNode.Identifier.ValueText;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting C# context");
        }
    }

    private async Task<List<CodeIssue>> AnalyzeCSharpCodeAsync(string code)
    {
        var issues = new List<CodeIssue>();

        try
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var root = await syntaxTree.GetRootAsync();

            // Check for syntax errors
            var diagnostics = syntaxTree.GetDiagnostics();
            foreach (var diagnostic in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            {
                var lineSpan = diagnostic.Location.GetLineSpan();
                issues.Add(new CodeIssue
                {
                    Id = Guid.NewGuid().ToString(),
                    Message = diagnostic.GetMessage(),
                    Severity = IssueSeverity.Error,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    Code = diagnostic.Id,
                    Category = "Syntax",
                    SuggestedFixes = new List<string>()
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing C# code");
        }

        return issues;
    }

    private async Task<List<CodeIssue>> AnalyzeCommonIssuesAsync(string code)
    {
        await Task.CompletedTask;
        var issues = new List<CodeIssue>();
        var lines = code.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Check for trailing whitespace
            if (line.EndsWith(" ") || line.EndsWith("\t"))
            {
                issues.Add(new CodeIssue
                {
                    Id = Guid.NewGuid().ToString(),
                    Message = "Trailing whitespace",
                    Severity = IssueSeverity.Info,
                    Line = i + 1,
                    Column = line.Length,
                    Code = "WS001",
                    Category = "Style",
                    SuggestedFixes = new List<string> { "Remove trailing whitespace" }
                });
            }

            // Check for very long lines
            if (line.Length > 120)
            {
                issues.Add(new CodeIssue
                {
                    Id = Guid.NewGuid().ToString(),
                    Message = $"Line too long ({line.Length} chars)",
                    Severity = IssueSeverity.Info,
                    Line = i + 1,
                    Column = 121,
                    Code = "LL001",
                    Category = "Style",
                    SuggestedFixes = new List<string> { "Break line" }
                });
            }
        }

        return issues;
    }

    private async Task<SyntaxTree> CreateSyntaxTreeAsync(string code, string language)
    {
        await Task.CompletedTask;

        return new SyntaxTree
        {
            Language = language,
            SourceCode = code,
            Text = code,
            Root = new SyntaxNode
            {
                Type = "Root",
                Name = "Document",
                StartLine = 1,
                EndLine = code.Split('\n').Length,
                StartColumn = 1,
                EndColumn = 1,
                Text = code,
                Start = 0,
                End = code.Length,
                Children = new List<SyntaxNode>(),
                Properties = new Dictionary<string, object> { ["Language"] = language }
            },
            Nodes = new List<SyntaxNode>(),
            Issues = new List<CodeIssue>(),
            Start = 0,
            End = code.Length
        };
    }

    private SyntaxTree CreateEmptySyntaxTree(string code)
    {
        return new SyntaxTree
        {
            Language = "text",
            SourceCode = code,
            Text = code,
            Root = new SyntaxNode(),
            Nodes = new List<SyntaxNode>(),
            Issues = new List<CodeIssue>(),
            Start = 0,
            End = code.Length
        };
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