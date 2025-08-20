using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using A3sist.Agents.AutoCompleter;

namespace A3sist.Agents.AutoCompleter.Services
{
    public class CodeCompletionService
    {
        private readonly Dictionary<string, List<CompletionItem>> _languageSpecificCompletions = new Dictionary<string, List<CompletionItem>>();

        public async Task InitializeAsync()
        {
            // Initialize language-specific completions
            InitializeLanguageCompletions();
        }

        private void InitializeLanguageCompletions()
        {
            // C# completions
            _languageSpecificCompletions["C#"] = new List<CompletionItem>
            {
                new CompletionItem { DisplayText = "public", InsertText = "public ", Kind = CompletionKind.Keyword },
                new CompletionItem { DisplayText = "private", InsertText = "private ", Kind = CompletionKind.Keyword },
                new CompletionItem { DisplayText = "protected", InsertText = "protected ", Kind = CompletionKind.Keyword },
                new CompletionItem { DisplayText = "internal", InsertText = "internal ", Kind = CompletionKind.Keyword },
                new CompletionItem { DisplayText = "static", InsertText = "static ", Kind = CompletionKind.Keyword },
                new CompletionItem { DisplayText = "void", InsertText = "void ", Kind = CompletionKind.Keyword },
                new CompletionItem { DisplayText = "class", InsertText = "class ", Kind = CompletionKind.Keyword },
                new CompletionItem { DisplayText = "interface", InsertText = "interface ", Kind = CompletionKind.Keyword },
                new CompletionItem { DisplayText = "namespace", InsertText = "namespace ", Kind = CompletionKind.Keyword },
                new CompletionItem { DisplayText = "using", InsertText = "using ", Kind = CompletionKind.Keyword },
                new CompletionItem { DisplayText = "if", InsertText = "if ()\n{\n\t\n}", Kind = CompletionKind.Snippet },
                new CompletionItem { DisplayText = "else", InsertText = "else\n{\n\t\n}", Kind = CompletionKind.Snippet },
                new CompletionItem { DisplayText = "for", InsertText = "for (int i = 0; i < length; i++)\n{\n\t\n}", Kind = CompletionKind.Snippet },
                new CompletionItem { DisplayText = "foreach", InsertText = "foreach (var item in collection)\n{\n\t\n}", Kind = CompletionKind.Snippet },
                new CompletionItem { DisplayText = "while", InsertText = "while (true)\n{\n\t\n}", Kind = CompletionKind.Snippet },
                new CompletionItem { DisplayText = "try", InsertText = "try\n{\n\t\n}\ncatch (Exception ex)\n{\n\t\n}", Kind = CompletionKind.Snippet },
                new CompletionItem { DisplayText = "switch", InsertText = "switch (expression)\n{\n\tcase :\n\t\tbreak;\n\tdefault:\n\t\tbreak;\n}", Kind = CompletionKind.Snippet }
            };

            // JavaScript completions
            _languageSpecificCompletions["JavaScript"] = new List<CompletionItem>
            {
                new CompletionItem { DisplayText = "function", InsertText = "function ", Kind = CompletionKind.Keyword },
                new CompletionItem { DisplayText = "const", InsertText = "const ", Kind = CompletionKind.Keyword },
                new CompletionItem { DisplayText = "let", InsertText = "let ", Kind = CompletionKind.Keyword },
                new CompletionItem { DisplayText = "var", InsertText = "var ", Kind = CompletionKind.Keyword },
                new CompletionItem { DisplayText = "if", InsertText = "if ()\n{\n\t\n}", Kind = CompletionKind.Snippet },
                new CompletionItem { DisplayText = "else", InsertText = "else\n{\n\t\n}", Kind = CompletionKind.Snippet },
                new CompletionItem { DisplayText = "for", InsertText = "for (let i = 0; i < array.length; i++)\n{\n\t\n}", Kind = CompletionKind.Snippet },
                new CompletionItem { DisplayText = "forEach", InsertText = "array.forEach(element => {\n\t\n});", Kind = CompletionKind.Snippet },
                new CompletionItem { DisplayText = "while", InsertText = "while (true)\n{\n\t\n}", Kind = CompletionKind.Snippet },
                new CompletionItem { DisplayText = "try", InsertText = "try\n{\n\t\n}\ncatch (error)\n{\n\t\n}", Kind = CompletionKind.Snippet },
                new CompletionItem { DisplayText = "console.log", InsertText = "console.log();", Kind = CompletionKind.Method },
                new CompletionItem { DisplayText = "document.getElementById", InsertText = "document.getElementById('');", Kind = CompletionKind.Method }
            };

            // Python completions
            _languageSpecificCompletions["Python"] = new List<CompletionItem>
            {
                new CompletionItem { DisplayText = "def", InsertText = "def ", Kind = CompletionKind.Keyword },
                new CompletionItem { DisplayText = "class", InsertText = "class ", Kind = CompletionKind.Keyword },
                new CompletionItem { DisplayText = "import", InsertText = "import ", Kind = CompletionKind.Keyword },
                new CompletionItem { DisplayText = "from", InsertText = "from ", Kind = CompletionKind.Keyword },
                new CompletionItem { DisplayText = "if", InsertText = "if :\n\t\n", Kind = CompletionKind.Snippet },
                new CompletionItem { DisplayText = "else", InsertText = "else:\n\t\n", Kind = CompletionKind.Snippet },
                new CompletionItem { DisplayText = "elif", InsertText = "elif :\n\t\n", Kind = CompletionKind.Snippet },
                new CompletionItem { DisplayText = "for", InsertText = "for in :\n\t\n", Kind = CompletionKind.Snippet },
                new CompletionItem { DisplayText = "while", InsertText = "while :\n\t\n", Kind = CompletionKind.Snippet },
                new CompletionItem { DisplayText = "try", InsertText = "try:\n\t\n\nexcept Exception as e:\n\t\n", Kind = CompletionKind.Snippet },
                new CompletionItem { DisplayText = "print", InsertText = "print()", Kind = CompletionKind.Method },
                new CompletionItem { DisplayText = "len", InsertText = "len()", Kind = CompletionKind.Method },
                new CompletionItem { DisplayText = "range", InsertText = "range()", Kind = CompletionKind.Method }
            };
        }

        public async Task<List<CompletionItem>> GetCompletionsAsync(CompletionContext context)
        {
            var completions = new List<CompletionItem>();

            // Get language-specific completions
            if (_languageSpecificCompletions.TryGetValue(context.Language, out var languageCompletions))
            {
                completions.AddRange(languageCompletions);
            }

            // Get context-aware completions
            var contextCompletions = await GetContextAwareCompletionsAsync(context);
            completions.AddRange(contextCompletions);

            // Filter and rank completions
            return FilterAndRankCompletions(completions, context);
        }

        private async Task<List<CompletionItem>> GetContextAwareCompletionsAsync(CompletionContext context)
        {
            var completions = new List<CompletionItem>();

            // Create a syntax tree from the code
            var syntaxTree = CSharpSyntaxTree.ParseText(context.Code);
            var root = await syntaxTree.GetRootAsync();

            // Get the token at the cursor position
            var token = root.FindToken(context.CursorPosition);

            // Get the semantic model
            var compilation = CSharpCompilation.Create("Completion")
                .AddSyntaxTrees(syntaxTree)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            // Get completions based on context
            if (token.Parent is ExpressionSyntax)
            {
                // Get member access completions
                var memberAccessCompletions = await GetMemberAccessCompletionsAsync(semanticModel, token.Parent);
                completions.AddRange(memberAccessCompletions);
            }
            else if (token.Parent is InvocationExpressionSyntax)
            {
                // Get method argument completions
                var argumentCompletions = await GetMethodArgumentCompletionsAsync(semanticModel, token.Parent);
                completions.AddRange(argumentCompletions);
            }
            else if (token.Parent is VariableDeclarationSyntax)
            {
                // Get type completions
                var typeCompletions = await GetTypeCompletionsAsync(semanticModel, token.Parent);
                completions.AddRange(typeCompletions);
            }

            return completions;
        }

        private async Task<List<CompletionItem>> GetMemberAccessCompletionsAsync(SemanticModel semanticModel, SyntaxNode node)
        {
            var completions = new List<CompletionItem>();

            // Get the type of the expression
            var typeInfo = semanticModel.GetTypeInfo(node);

            if (typeInfo.Type != null)
            {
                // Get members of the type
                var members = typeInfo.Type.GetMembers();

                foreach (var member in members)
                {
                    completions.Add(new CompletionItem
                    {
                        DisplayText = member.Name,
                        InsertText = member.Name,
                        Kind = GetCompletionKind(member)
                    });
                }
            }

            return completions;
        }

        private async Task<List<CompletionItem>> GetMethodArgumentCompletionsAsync(SemanticModel semanticModel, SyntaxNode node)
        {
            var completions = new List<CompletionItem>();

            if (node is InvocationExpressionSyntax invocation)
            {
                // Get the method symbol
                var methodSymbol = semanticModel.GetSymbolInfo(invocation.Expression).Symbol as IMethodSymbol;

                if (methodSymbol != null)
                {
                    // Get parameters of the method
                    foreach (var parameter in methodSymbol.Parameters)
                    {
                        completions.Add(new CompletionItem
                        {
                            DisplayText = parameter.Name,
                            InsertText = parameter.Name,
                            Kind = CompletionKind.Parameter
                        });
                    }
                }
            }

            return completions;
        }

        private async Task<List<CompletionItem>> GetTypeCompletionsAsync(SemanticModel semanticModel, SyntaxNode node)
        {
            var completions = new List<CompletionItem>();

            if (node is VariableDeclarationSyntax declaration)
            {
                // Get available types in the current context
                var types = semanticModel.LookupSymbols(declaration.Span.Start, name: null, includeReducedExtensionMethods: false)
                    .OfType<INamedTypeSymbol>();

                foreach (var type in types)
                {
                    completions.Add(new CompletionItem
                    {
                        DisplayText = type.Name,
                        InsertText = type.Name,
                        Kind = CompletionKind.Type
                    });
                }
            }

            return completions;
        }

        private List<CompletionItem> FilterAndRankCompletions(List<CompletionItem> completions, CompletionContext context)
        {
            // Filter completions based on context
            var filteredCompletions = completions
                .Where(c => c.DisplayText.StartsWith(context.Code.Substring(context.CursorPosition - 1), StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Rank completions based on relevance
            var rankedCompletions = filteredCompletions
                .OrderByDescending(c => c.DisplayText.Length) // Longer matches first
                .ThenBy(c => c.DisplayText) // Alphabetical order
                .Take(10) // Return top 10 suggestions
                .ToList();

            return rankedCompletions;
        }

        private CompletionKind GetCompletionKind(ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                    return CompletionKind.Method;
                case SymbolKind.Property:
                    return CompletionKind.Property;
                case SymbolKind.Field:
                    return CompletionKind.Field;
                case SymbolKind.Event:
                    return CompletionKind.Event;
                case SymbolKind.NamedType:
                    return CompletionKind.Type;
                case SymbolKind.Parameter:
                    return CompletionKind.Parameter;
                default:
                    return CompletionKind.Unknown;
            }
        }

        public async Task ShutdownAsync()
        {
            // Clean up resources
            _languageSpecificCompletions.Clear();
        }
    }

    public class CompletionItem
    {
        public string DisplayText { get; set; }
        public string InsertText { get; set; }
        public CompletionKind Kind { get; set; }
    }

    public enum CompletionKind
    {
        Unknown,
        Keyword,
        Method,
        Property,
        Field,
        Event,
        Type,
        Parameter,
        Snippet
    }
}