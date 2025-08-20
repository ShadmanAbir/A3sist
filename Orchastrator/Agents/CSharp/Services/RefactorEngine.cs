using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace A3sist.Agents.CSharp.Services
{
    /// <summary>
    /// Provides code refactoring capabilities for C# code.
    /// </summary>
    public class RefactorEngine : IDisposable
    {
        private bool _disposed = false;
        private readonly CSharpCompilationOptions _compilationOptions;

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
        }

        /// <summary>
        /// Initializes the refactoring engine asynchronously.
        /// </summary>
        public async Task InitializeAsync()
        {
            // Initialize refactoring engine
            await Task.CompletedTask;
        }

        /// <summary>
        /// Refactors the provided C# code asynchronously.
        /// </summary>
        /// <param name="code">The C# code to refactor.</param>
        /// <returns>The refactored code.</returns>
        /// <exception cref="ArgumentNullException">Thrown when code is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when refactoring fails.</exception>
        public async Task<string> RefactorCodeAsync(string code)
{
    if (string.IsNullOrWhiteSpace(code))
        throw new ArgumentNullException(nameof(code));

    var tree = CSharpSyntaxTree.ParseText(code);
    var root = await tree.GetRootAsync();

    var nodes = root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>();

    foreach (var node in nodes)
    {
        if (node.Declaration.Variables.Count == 1)
        {
            var variable = node.Declaration.Variables[0];
            if (variable.Initializer != null)
            {
                var typeInfo = await GetTypeInfoAsync(tree, variable.Initializer.Value);

if (typeInfo.Type is not null && typeInfo.Type is not IErrorTypeSymbol)
{
    var newDeclaration = node.WithDeclaration(
        node.Declaration.WithType(
            SyntaxFactory.ParseTypeName(typeInfo.Type.ToDisplayString())
                .WithTriviaFrom(node.Declaration.Type)
        )
    );

    root = root.ReplaceNode(node, newDeclaration);
}

            }
        }
    }

    return root.ToFullString();
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
        .AddReferences(
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
        )
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