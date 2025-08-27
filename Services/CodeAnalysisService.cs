using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using A3sist.Models;

namespace A3sist.Services
{
    public class CodeAnalysisService : ICodeAnalysisService
    {
        private Dictionary<string, string> _extensionToLanguageMap;
        private Dictionary<string, Func<string, Task<A3sist.Models.SyntaxTree>>> _languageParsers;

        public CodeAnalysisService()
        {
            InitializeLanguageMappings();
            InitializeParsers();
        }

        public async Task<string> DetectLanguageAsync(string code, string fileName = null)
        {
            // First try to detect by file extension if available
            if (!string.IsNullOrEmpty(fileName))
            {
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                if (_extensionToLanguageMap.TryGetValue(extension, out var languageByExtension))
                {
                    return languageByExtension;
                }
            }

            // Then try to detect by code content patterns
            return await DetectLanguageByContentAsync(code);
        }

        public async Task<CodeContext> ExtractContextAsync(string code, int position)
        {
            var language = await DetectLanguageAsync(code);
            
            var context = new CodeContext
            {
                Language = language,
                UsingStatements = new List<string>(),
                Metadata = new Dictionary<string, object>()
            };

            try
            {
                if (language == "csharp")
                {
                    await ExtractCSharpContextAsync(code, position, context);
                }
                else if (language == "javascript" || language == "typescript")
                {
                    await ExtractJavaScriptContextAsync(code, position, context);
                }
                else
                {
                    await ExtractGenericContextAsync(code, position, context);
                }
            }
            catch
            {
                // Fallback to generic context extraction
                await ExtractGenericContextAsync(code, position, context);
            }

            return context;
        }

        public async Task<IEnumerable<CodeIssue>> AnalyzeCodeAsync(string code, string language)
        {
            var issues = new List<CodeIssue>();

            try
            {
                if (language == "csharp")
                {
                    issues.AddRange(await AnalyzeCSharpCodeAsync(code));
                }
                else
                {
                    issues.AddRange(await AnalyzeGenericCodeAsync(code, language));
                }
            }
            catch
            {
                // If analysis fails, return empty list
            }

            return issues;
        }

        public async Task<A3sist.Models.SyntaxTree> GetSyntaxTreeAsync(string code, string language)
        {
            try
            {
                if (_languageParsers.TryGetValue(language, out var parser))
                {
                    return await parser(code);
                }
            }
            catch
            {
                // If parsing fails, return a generic syntax tree
            }

            return await CreateGenericSyntaxTreeAsync(code, language);
        }

        public async Task<IEnumerable<string>> GetSupportedLanguagesAsync()
        {
            return _extensionToLanguageMap.Values.Distinct().ToList();
        }

        private void InitializeLanguageMappings()
        {
            _extensionToLanguageMap = new Dictionary<string, string>
            {
                // .NET languages
                [".cs"] = "csharp",
                [".csx"] = "csharp",
                [".vb"] = "vbnet",
                [".fs"] = "fsharp",
                [".fsx"] = "fsharp",

                // Web languages
                [".js"] = "javascript",
                [".jsx"] = "javascript",
                [".ts"] = "typescript",
                [".tsx"] = "typescript",
                [".html"] = "html",
                [".htm"] = "html",
                [".css"] = "css",
                [".scss"] = "scss",
                [".sass"] = "sass",
                [".less"] = "less",

                // Other popular languages
                [".py"] = "python",
                [".pyx"] = "python",
                [".pyi"] = "python",
                [".java"] = "java",
                [".kt"] = "kotlin",
                [".scala"] = "scala",
                [".cpp"] = "cpp",
                [".c"] = "c",
                [".cc"] = "cpp",
                [".cxx"] = "cpp",
                [".h"] = "c",
                [".hpp"] = "cpp",
                [".rs"] = "rust",
                [".go"] = "go",
                [".rb"] = "ruby",
                [".php"] = "php",
                [".swift"] = "swift",

                // Data formats
                [".json"] = "json",
                [".xml"] = "xml",
                [".yaml"] = "yaml",
                [".yml"] = "yaml",
                [".sql"] = "sql",

                // Documentation
                [".md"] = "markdown",
                [".txt"] = "text",
                [".rst"] = "restructuredtext",
                [".adoc"] = "asciidoc",

                // Shell scripts
                [".sh"] = "bash",
                [".bash"] = "bash",
                [".ps1"] = "powershell",
                [".bat"] = "batch",
                [".cmd"] = "batch"
            };
        }

        private void InitializeParsers()
        {
            _languageParsers = new Dictionary<string, Func<string, Task<A3sist.Models.SyntaxTree>>>
            {
                ["csharp"] = ParseCSharpAsync,
                // Other language parsers can be added here
            };
        }

        private async Task<string> DetectLanguageByContentAsync(string code)
        {
            // C# detection patterns
            if (Regex.IsMatch(code, @"\b(using\s+System|namespace\s+\w+|public\s+class\s+\w+|private\s+\w+\s+\w+)"))
                return "csharp";

            // JavaScript/TypeScript patterns
            if (Regex.IsMatch(code, @"\b(function\s+\w+|const\s+\w+\s*=|let\s+\w+|var\s+\w+|=>|console\.log)"))
            {
                if (Regex.IsMatch(code, @"\b(interface\s+\w+|type\s+\w+\s*=|:\s*\w+\s*[=;]|\w+:\s*\w+)"))
                    return "typescript";
                return "javascript";
            }

            // Python patterns
            if (Regex.IsMatch(code, @"\b(def\s+\w+|import\s+\w+|from\s+\w+\s+import|class\s+\w+\(|print\()"))
                return "python";

            // Java patterns
            if (Regex.IsMatch(code, @"\b(public\s+class\s+\w+|package\s+\w+|import\s+java\.|System\.out\.println)"))
                return "java";

            // SQL patterns
            if (Regex.IsMatch(code, @"\b(SELECT\s+|INSERT\s+INTO|UPDATE\s+|DELETE\s+FROM|CREATE\s+TABLE)", RegexOptions.IgnoreCase))
                return "sql";

            // JSON pattern
            if (Regex.IsMatch(code, @"^\s*[\{\[].*[\}\]]\s*$", RegexOptions.Singleline | RegexOptions.IgnoreCase))
                return "json";

            // XML pattern
            if (Regex.IsMatch(code, @"^\s*<\?xml|<\w+.*>.*</\w+>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
                return "xml";

            // Default to text
            return "text";
        }

        private async Task ExtractCSharpContextAsync(string code, int position, CodeContext context)
        {
            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var root = await syntaxTree.GetRootAsync();

                // Extract using statements
                var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>();
                context.UsingStatements.AddRange(usingDirectives.Select(u => u.ToString()));

                // Find the node at the position
                var nodeAtPosition = root.FindNode(new Microsoft.CodeAnalysis.Text.TextSpan(position, 0));

                // Find containing method
                var methodDeclaration = nodeAtPosition.FirstAncestorOrSelf<MethodDeclarationSyntax>();
                if (methodDeclaration != null)
                {
                    context.CurrentMethod = methodDeclaration.Identifier.ValueText;
                }

                // Find containing class
                var classDeclaration = nodeAtPosition.FirstAncestorOrSelf<ClassDeclarationSyntax>();
                if (classDeclaration != null)
                {
                    context.CurrentClass = classDeclaration.Identifier.ValueText;
                }

                // Extract surrounding code (within method or class)
                var surroundingNode = methodDeclaration ?? classDeclaration as Microsoft.CodeAnalysis.SyntaxNode ?? root;
                context.SurroundingCode = surroundingNode.ToString();

                // Add metadata
                context.Metadata["hasErrors"] = root.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error);
                context.Metadata["nodeType"] = nodeAtPosition.GetType().Name;
            }
            catch
            {
                await ExtractGenericContextAsync(code, position, context);
            }
        }

        private async Task ExtractJavaScriptContextAsync(string code, int position, CodeContext context)
        {
            try
            {
                var lines = code.Split('\n');
                var currentLineIndex = GetLineFromPosition(code, position);

                // Find function context
                for (int i = currentLineIndex; i >= 0; i--)
                {
                    var line = lines[i];
                    var functionMatch = Regex.Match(line, @"function\s+(\w+)|(\w+)\s*:\s*function|(\w+)\s*=>\s*|const\s+(\w+)\s*=.*function");
                    if (functionMatch.Success)
                    {
                        context.CurrentMethod = functionMatch.Groups.Cast<Group>()
                            .Skip(1)
                            .FirstOrDefault(g => g.Success)?.Value;
                        break;
                    }
                }

                // Find class context
                for (int i = currentLineIndex; i >= 0; i--)
                {
                    var line = lines[i];
                    var classMatch = Regex.Match(line, @"class\s+(\w+)|function\s+(\w+)\s*\(.*\)\s*\{");
                    if (classMatch.Success)
                    {
                        context.CurrentClass = classMatch.Groups.Cast<Group>()
                            .Skip(1)
                            .FirstOrDefault(g => g.Success)?.Value;
                        break;
                    }
                }

                // Extract imports
                var importMatches = Regex.Matches(code, @"import\s+.*from\s+['""].*['""]|const\s+.*=\s+require\s*\(");
                context.UsingStatements.AddRange(importMatches.Cast<Match>().Select(m => m.Value));

                // Get surrounding context
                var startLine = Math.Max(0, currentLineIndex - 5);
                var endLine = Math.Min(lines.Length - 1, currentLineIndex + 5);
                context.SurroundingCode = string.Join("\n", lines.Skip(startLine).Take(endLine - startLine + 1));
            }
            catch
            {
                await ExtractGenericContextAsync(code, position, context);
            }
        }

        private async Task ExtractGenericContextAsync(string code, int position, CodeContext context)
        {
            try
            {
                var lines = code.Split('\n');
                var currentLineIndex = GetLineFromPosition(code, position);

                // Get surrounding context (5 lines before and after)
                var startLine = Math.Max(0, currentLineIndex - 5);
                var endLine = Math.Min(lines.Length - 1, currentLineIndex + 5);
                context.SurroundingCode = string.Join("\n", lines.Skip(startLine).Take(endLine - startLine + 1));

                // Extract basic metadata
                context.Metadata["totalLines"] = lines.Length;
                context.Metadata["currentLine"] = currentLineIndex + 1;
                context.Metadata["isEmpty"] = string.IsNullOrWhiteSpace(code);
            }
            catch
            {
                context.SurroundingCode = code;
            }
        }

        private async Task<IEnumerable<CodeIssue>> AnalyzeCSharpCodeAsync(string code)
        {
            var issues = new List<CodeIssue>();

            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var root = await syntaxTree.GetRootAsync();

                // Get syntax errors
                var diagnostics = root.GetDiagnostics();
                foreach (var diagnostic in diagnostics)
                {
                    var lineSpan = diagnostic.Location.GetLineSpan();
                    issues.Add(new CodeIssue
                    {
                        Id = diagnostic.Id,
                        Message = diagnostic.GetMessage(),
                        Severity = MapSeverity(diagnostic.Severity),
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Column = lineSpan.StartLinePosition.Character + 1
                    });
                }

                // Add custom analysis rules
                issues.AddRange(await AnalyzeCSharpCustomRulesAsync(root));
            }
            catch
            {
                // If C# analysis fails, return empty list
            }

            return issues;
        }

        private async Task<IEnumerable<CodeIssue>> AnalyzeCSharpCustomRulesAsync(Microsoft.CodeAnalysis.SyntaxNode root)
        {
            var issues = new List<CodeIssue>();

            // Check for unused using statements
            var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToList();
            var identifierNames = root.DescendantNodes().OfType<IdentifierNameSyntax>().Select(i => i.Identifier.ValueText).ToHashSet();

            foreach (var usingDirective in usingDirectives)
            {
                var namespaceName = usingDirective.Name.ToString();
                var lastPart = namespaceName.Split('.').LastOrDefault();
                
                if (!string.IsNullOrEmpty(lastPart) && !identifierNames.Contains(lastPart))
                {
                    issues.Add(new CodeIssue
                    {
                        Id = "A3SIST001",
                        Message = $"Unused using directive: {namespaceName}",
                        Severity = IssueSeverity.Info,
                        Line = usingDirective.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        Column = usingDirective.GetLocation().GetLineSpan().StartLinePosition.Character + 1,
                        SuggestedFix = "Remove unused using directive"
                    });
                }
            }

            // Check for missing braces
            var ifStatements = root.DescendantNodes().OfType<IfStatementSyntax>();
            foreach (var ifStatement in ifStatements)
            {
                if (!(ifStatement.Statement is BlockSyntax))
                {
                    issues.Add(new CodeIssue
                    {
                        Id = "A3SIST002",
                        Message = "Consider using braces for if statement",
                        Severity = IssueSeverity.Warning,
                        Line = ifStatement.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        Column = ifStatement.GetLocation().GetLineSpan().StartLinePosition.Character + 1,
                        SuggestedFix = "Add braces around if statement body"
                    });
                }
            }

            return issues;
        }

        private async Task<IEnumerable<CodeIssue>> AnalyzeGenericCodeAsync(string code, string language)
        {
            var issues = new List<CodeIssue>();

            // Generic code analysis rules
            var lines = code.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Check for very long lines
                if (line.Length > 120)
                {
                    issues.Add(new CodeIssue
                    {
                        Id = "A3SIST100",
                        Message = "Line is too long (consider breaking into multiple lines)",
                        Severity = IssueSeverity.Info,
                        Line = i + 1,
                        Column = 121,
                        SuggestedFix = "Break long line into multiple lines"
                    });
                }

                // Check for trailing whitespace
                if (line.EndsWith(" ") || line.EndsWith("\t"))
                {
                    issues.Add(new CodeIssue
                    {
                        Id = "A3SIST101",
                        Message = "Trailing whitespace detected",
                        Severity = IssueSeverity.Info,
                        Line = i + 1,
                        Column = line.Length,
                        SuggestedFix = "Remove trailing whitespace"
                    });
                }
            }

            return issues;
        }

        private async Task<A3sist.Models.SyntaxTree> ParseCSharpAsync(string code)
        {
            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var root = await syntaxTree.GetRootAsync();

                return new A3sist.Models.SyntaxTree
                {
                    Root = ConvertSingleSyntaxNode(root),
                    Language = "csharp",
                    Nodes = ConvertToSyntaxNodes(root)
                };
            }
            catch
            {
                return await CreateGenericSyntaxTreeAsync(code, "csharp");
            }
        }

        private async Task<A3sist.Models.SyntaxTree> CreateGenericSyntaxTreeAsync(string code, string language)
        {
            var lines = code.Split('\n');
            var nodes = new List<A3sist.Models.SyntaxNode>();
            var currentPosition = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                var lineLength = lines[i].Length;
                nodes.Add(new A3sist.Models.SyntaxNode
                {
                    Type = "Line",
                    Text = lines[i],
                    Start = currentPosition,
                    End = currentPosition + lineLength,
                    Children = new List<A3sist.Models.SyntaxNode>()
                });
                currentPosition += lineLength + 1; // +1 for newline character
            }

            return new A3sist.Models.SyntaxTree
            {
                Root = new A3sist.Models.SyntaxNode
                {
                    Type = "Root",
                    Text = code,
                    Start = 0,
                    End = code.Length,
                    Children = nodes
                },
                Language = language,
                Nodes = nodes
            };
        }

        private A3sist.Models.SyntaxNode ConvertSingleSyntaxNode(Microsoft.CodeAnalysis.SyntaxNode node)
        {
            return new A3sist.Models.SyntaxNode
            {
                Type = node.GetType().Name,
                Text = node.ToString(),
                Start = node.SpanStart,
                End = node.Span.End,
                Children = node.ChildNodes().Select(ConvertSingleSyntaxNode).ToList()
            };
        }

        private List<A3sist.Models.SyntaxNode> ConvertToSyntaxNodes(Microsoft.CodeAnalysis.SyntaxNode root)
        {
            var nodes = new List<A3sist.Models.SyntaxNode>();

            void AddNode(Microsoft.CodeAnalysis.SyntaxNode node)
            {
                var syntaxNode = new A3sist.Models.SyntaxNode
                {
                    Type = node.GetType().Name,
                    Text = node.ToString(),
                    Start = node.SpanStart,
                    End = node.Span.End,
                    Children = new List<A3sist.Models.SyntaxNode>()
                };

                foreach (var child in node.ChildNodes())
                {
                    AddNode(child);
                }

                nodes.Add(syntaxNode);
            }

            AddNode(root);
            return nodes;
        }

        private IssueSeverity MapSeverity(DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case DiagnosticSeverity.Error:
                    return IssueSeverity.Error;
                case DiagnosticSeverity.Warning:
                    return IssueSeverity.Warning;
                case DiagnosticSeverity.Info:
                    return IssueSeverity.Info;
                default:
                    return IssueSeverity.Info;
            }
        }

        private string MapContentTypeToLanguage(string contentType)
        {
            switch (contentType.ToLowerInvariant())
            {
                case "csharp":
                    return "csharp";
                case "basic":
                    return "vbnet";
                case "javascript":
                    return "javascript";
                case "typescript":
                    return "typescript";
                default:
                    return "text";
            }
        }

        private int GetLineFromPosition(string code, int position)
        {
            if (position <= 0) return 0;
            if (position >= code.Length) return code.Count(c => c == '\n');

            return code.Take(position).Count(c => c == '\n');
        }
    }
}