using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace A3sist.Orchastrator.Agents.CSharp.Services
{
    /// <summary>
    /// Provides comprehensive code refactoring capabilities for C# code using Roslyn
    /// </summary>
    public class RefactorEngine : IDisposable
    {
        private bool _disposed = false;
        private readonly CSharpCompilationOptions _compilationOptions;
        private ImmutableArray<MetadataReference> _references;

        /// <summary>
        /// Initializes a new instance of the RefactorEngine class.
        /// </summary>
        public RefactorEngine()
        {
            _compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSpecificDiagnosticOptions(new[]
                {
                    new KeyValuePair<string, ReportDiagnostic>("CS8019", ReportDiagnostic.Suppress) // Unnecessary using directive
                });
            
            _references = ImmutableArray<MetadataReference>.Empty;
        }

        /// <summary>
        /// Initializes the refactoring engine asynchronously.
        /// </summary>
        public async Task InitializeAsync()
        {
            // Initialize basic references
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
            };

            // Add .NET runtime references
            var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            if (!string.IsNullOrEmpty(runtimePath))
            {
                var systemReferences = new[]
                {
                    "System.Runtime.dll",
                    "System.Collections.dll",
                    "System.Linq.dll",
                    "System.Threading.Tasks.dll",
                    "System.Text.dll"
                };

                foreach (var refName in systemReferences)
                {
                    var refPath = Path.Combine(runtimePath, refName);
                    if (File.Exists(refPath))
                    {
                        references.Add(MetadataReference.CreateFromFile(refPath));
                    }
                }
            }

            _references = references.ToImmutableArray();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Refactors the provided C# code asynchronously with multiple refactoring techniques.
        /// </summary>
        /// <param name="code">The C# code to refactor.</param>
        /// <returns>The refactored code.</returns>
        /// <exception cref="ArgumentNullException">Thrown when code is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when refactoring fails.</exception>
        public async Task<string> RefactorCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentNullException(nameof(code));

            try
            {
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = await tree.GetRootAsync();

                // Create compilation for semantic analysis
                var compilation = CSharpCompilation.Create("Refactoring")
                    .AddSyntaxTrees(tree)
                    .AddReferences(_references)
                    .WithOptions(_compilationOptions);

                var semanticModel = compilation.GetSemanticModel(tree);

                // Apply various refactoring techniques
                root = await ApplyVariableTypeInferenceAsync(root, semanticModel);
                root = await RemoveUnnecessaryUsingDirectivesAsync(root, semanticModel);
                root = await SimplifyExpressionsAsync(root, semanticModel);
                root = await ImproveNamingConventionsAsync(root, semanticModel);
                root = await ExtractConstantsAsync(root, semanticModel);
                root = await SimplifyLinqExpressionsAsync(root, semanticModel);

                // Format the code
                var workspace = new AdhocWorkspace();
                root = Formatter.Format(root, workspace);

                return root.ToFullString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to refactor code.", ex);
            }
        }

        /// <summary>
        /// Applies variable type inference (var keyword) where appropriate
        /// </summary>
        private async Task<SyntaxNode> ApplyVariableTypeInferenceAsync(SyntaxNode root, SemanticModel semanticModel)
        {
            var localDeclarations = root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>().ToList();

            foreach (var declaration in localDeclarations)
            {
                if (declaration.Declaration.Variables.Count == 1)
                {
                    var variable = declaration.Declaration.Variables[0];
                    if (variable.Initializer != null)
                    {
                        var typeInfo = semanticModel.GetTypeInfo(variable.Initializer.Value);

                        if (typeInfo.Type != null && 
                            typeInfo.Type is not IErrorTypeSymbol &&
                            !typeInfo.Type.IsAnonymousType &&
                            CanUseVarKeyword(typeInfo.Type, variable.Initializer.Value))
                        {
                            var varType = SyntaxFactory.IdentifierName("var")
                                .WithTriviaFrom(declaration.Declaration.Type);

                            var newDeclaration = declaration.WithDeclaration(
                                declaration.Declaration.WithType(varType)
                            );

                            root = root.ReplaceNode(declaration, newDeclaration);
                        }
                    }
                }
            }

            return await Task.FromResult(root);
        }

        /// <summary>
        /// Removes unnecessary using directives
        /// </summary>
        private async Task<SyntaxNode> RemoveUnnecessaryUsingDirectivesAsync(SyntaxNode root, SemanticModel semanticModel)
        {
            var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToList();
            var usedNamespaces = new HashSet<string>();

            // Collect all used types and their namespaces
            var identifiers = root.DescendantNodes().OfType<IdentifierNameSyntax>();
            foreach (var identifier in identifiers)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(identifier);
                if (symbolInfo.Symbol != null)
                {
                    var containingNamespace = symbolInfo.Symbol.ContainingNamespace?.ToDisplayString();
                    if (!string.IsNullOrEmpty(containingNamespace) && containingNamespace != "<global namespace>")
                    {
                        usedNamespaces.Add(containingNamespace);
                    }
                }
            }

            // Remove unused using directives
            foreach (var usingDirective in usings)
            {
                var namespaceName = usingDirective.Name?.ToString();
                if (!string.IsNullOrEmpty(namespaceName) && !usedNamespaces.Contains(namespaceName))
                {
                    root = root.RemoveNode(usingDirective, SyntaxRemoveOptions.KeepNoTrivia);
                }
            }

            return await Task.FromResult(root);
        }

        /// <summary>
        /// Simplifies expressions where possible
        /// </summary>
        private async Task<SyntaxNode> SimplifyExpressionsAsync(SyntaxNode root, SemanticModel semanticModel)
        {
            // Simplify boolean expressions
            var binaryExpressions = root.DescendantNodes().OfType<BinaryExpressionSyntax>().ToList();
            
            foreach (var expression in binaryExpressions)
            {
                if (expression.IsKind(SyntaxKind.EqualsExpression))
                {
                    // Simplify == true to just the expression
                    if (expression.Right.IsKind(SyntaxKind.TrueLiteralExpression))
                    {
                        root = root.ReplaceNode(expression, expression.Left);
                    }
                    // Simplify == false to !expression
                    else if (expression.Right.IsKind(SyntaxKind.FalseLiteralExpression))
                    {
                        var negation = SyntaxFactory.PrefixUnaryExpression(
                            SyntaxKind.LogicalNotExpression,
                            SyntaxFactory.ParenthesizedExpression(expression.Left));
                        root = root.ReplaceNode(expression, negation);
                    }
                }
            }

            return await Task.FromResult(root);
        }

        /// <summary>
        /// Improves naming conventions
        /// </summary>
        private async Task<SyntaxNode> ImproveNamingConventionsAsync(SyntaxNode root, SemanticModel semanticModel)
        {
            // This is a simplified implementation - in practice, you'd want more sophisticated naming analysis
            var variableDeclarations = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().ToList();

            foreach (var variable in variableDeclarations)
            {
                var name = variable.Identifier.ValueText;
                
                // Check for common naming issues
                if (name.Length == 1 && char.IsLower(name[0]) && name != "i" && name != "j" && name != "k")
                {
                    // Suggest better names for single-letter variables (except common loop counters)
                    // This is just a placeholder - real implementation would need context analysis
                }
            }

            return await Task.FromResult(root);
        }

        /// <summary>
        /// Extracts magic numbers and strings to constants
        /// </summary>
        private async Task<SyntaxNode> ExtractConstantsAsync(SyntaxNode root, SemanticModel semanticModel)
        {
            // Find numeric literals that might be magic numbers
            var numericLiterals = root.DescendantNodes().OfType<LiteralExpressionSyntax>()
                .Where(l => l.IsKind(SyntaxKind.NumericLiteralExpression))
                .ToList();

            var magicNumbers = new Dictionary<string, int>();

            foreach (var literal in numericLiterals)
            {
                var value = literal.Token.ValueText;
                if (int.TryParse(value, out var intValue) && intValue > 1 && intValue != 0 && intValue != -1)
                {
                    if (magicNumbers.ContainsKey(value))
                        magicNumbers[value]++;
                    else
                        magicNumbers[value] = 1;
                }
            }

            // For numbers that appear multiple times, suggest extracting to constants
            // This is a simplified implementation - real refactoring would need more context

            return await Task.FromResult(root);
        }

        /// <summary>
        /// Simplifies LINQ expressions where possible
        /// </summary>
        private async Task<SyntaxNode> SimplifyLinqExpressionsAsync(SyntaxNode root, SemanticModel semanticModel)
        {
            var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();

            foreach (var invocation in invocations)
            {
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    var methodName = memberAccess.Name.Identifier.ValueText;
                    
                    // Simplify Where().Count() to Count()
                    if (methodName == "Count" && 
                        memberAccess.Expression is InvocationExpressionSyntax whereInvocation &&
                        whereInvocation.Expression is MemberAccessExpressionSyntax whereMember &&
                        whereMember.Name.Identifier.ValueText == "Where")
                    {
                        // Replace Where().Count() with Count(predicate)
                        if (whereInvocation.ArgumentList.Arguments.Count == 1)
                        {
                            var countWithPredicate = invocation.WithExpression(
                                memberAccess.WithExpression(whereMember.Expression))
                                .WithArgumentList(whereInvocation.ArgumentList);
                            
                            root = root.ReplaceNode(invocation, countWithPredicate);
                        }
                    }
                }
            }

            return await Task.FromResult(root);
        }

        /// <summary>
        /// Determines if var keyword can be used for a type
        /// </summary>
        private bool CanUseVarKeyword(ITypeSymbol type, ExpressionSyntax initializer)
        {
            // Don't use var for built-in types where the type isn't obvious from the initializer
            if (type.SpecialType != SpecialType.None)
            {
                // Allow var for obvious cases like new List<string>()
                if (initializer is ObjectCreationExpressionSyntax)
                    return true;
                
                // Don't use var for simple literals
                if (initializer is LiteralExpressionSyntax)
                    return false;
            }

            // Use var for complex generic types
            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
                return true;

            return false;
        }

        /// <summary>
        /// Gets type information for the specified expression asynchronously.
        /// </summary>
        /// <param name="tree">The syntax tree containing the expression.</param>
        /// <param name="expression">The expression to analyze.</param>
        /// <returns>Type information for the expression.</returns>
        private Task<TypeInfo> GetTypeInfoAsync(SyntaxTree tree, ExpressionSyntax expression)
        {
            if (tree == null)
                throw new ArgumentNullException(nameof(tree));

            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            var compilation = CSharpCompilation.Create("Analysis")
                .AddSyntaxTrees(tree)
                .AddReferences(_references)
                .WithOptions(_compilationOptions);

            var semanticModel = compilation.GetSemanticModel(tree);
            var typeInfo = semanticModel.GetTypeInfo(expression);

            return Task.FromResult(typeInfo);
        }

        /// <summary>
        /// Shuts down the refactoring engine asynchronously.
        /// </summary>
        public async Task ShutdownAsync()
        {
            // Clean up resources
            _references = ImmutableArray<MetadataReference>.Empty;
            await Task.CompletedTask;
        }

        /// <summary>
        /// Disposes the refactoring engine and releases resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the refactoring engine and releases resources.
        /// </summary>
        /// <param name="disposing">Indicates whether the method is called from Dispose or the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here
                }

                // Dispose unmanaged resources here

                _disposed = true;
            }
        }
    }
}