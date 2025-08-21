using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace A3sist.Orchastrator.Agents.CSharp.Services
{
    /// <summary>
    /// Provides comprehensive code analysis capabilities for C# code using Roslyn
    /// </summary>
    public class Analyzer : IDisposable
    {
        private bool _disposed = false;
        private List<DiagnosticAnalyzer> _analyzers;
        private ImmutableArray<MetadataReference> _references;

        /// <summary>
        /// Initializes a new instance of the Analyzer class.
        /// </summary>
        public Analyzer()
        {
            _analyzers = new List<DiagnosticAnalyzer>();
            _references = ImmutableArray<MetadataReference>.Empty;
        }

        /// <summary>
        /// Initializes the analyzers asynchronously.
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

            // Initialize custom analyzers
            _analyzers = new List<DiagnosticAnalyzer>
            {
                // Add built-in analyzers or custom ones here
                // Example: new EmptyMethodAnalyzer()
            };

            await Task.CompletedTask;
        }

        /// <summary>
        /// Analyzes the provided C# code asynchronously with comprehensive diagnostics
        /// </summary>
        /// <param name="code">The C# code to analyze.</param>
        /// <returns>The analysis results with detailed information.</returns>
        /// <exception cref="ArgumentNullException">Thrown when code is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when analysis fails.</exception>
        public async Task<string> AnalyzeCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentNullException(nameof(code), "Code cannot be null or empty.");
            }

            try
            {
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = await tree.GetRootAsync();

                // Create compilation
                var compilation = CSharpCompilation.Create("Analysis")
                    .AddSyntaxTrees(tree)
                    .AddReferences(_references)
                    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                // Get semantic model
                var semanticModel = compilation.GetSemanticModel(tree);

                // Perform various analyses
                var results = new List<string>();

                // 1. Syntax analysis
                var syntaxDiagnostics = tree.GetDiagnostics();
                if (syntaxDiagnostics.Any())
                {
                    results.Add("=== Syntax Issues ===");
                    results.AddRange(syntaxDiagnostics
                        .Where(d => d.Severity >= DiagnosticSeverity.Warning)
                        .Select(d => $"  {d.Severity}: {d.GetMessage()} at {d.Location.GetLineSpan()}"));
                }

                // 2. Semantic analysis
                var semanticDiagnostics = compilation.GetDiagnostics();
                if (semanticDiagnostics.Any())
                {
                    results.Add("=== Semantic Issues ===");
                    results.AddRange(semanticDiagnostics
                        .Where(d => d.Severity >= DiagnosticSeverity.Warning)
                        .Select(d => $"  {d.Severity}: {d.GetMessage()} at {d.Location.GetLineSpan()}"));
                }

                // 3. Code structure analysis
                results.Add("=== Code Structure ===");
                var structureInfo = await AnalyzeCodeStructureAsync(root, semanticModel);
                results.AddRange(structureInfo);

                // 4. Code quality metrics
                results.Add("=== Code Metrics ===");
                var metricsInfo = await AnalyzeCodeMetricsAsync(root);
                results.AddRange(metricsInfo);

                // 5. Custom analyzer results
                if (_analyzers.Any())
                {
                    var compilationWithAnalyzers = compilation.WithAnalyzers(_analyzers.ToImmutableArray());
                    var analyzerDiagnostics = await compilationWithAnalyzers.GetAllDiagnosticsAsync();
                    
                    if (analyzerDiagnostics.Any())
                    {
                        results.Add("=== Custom Analyzer Results ===");
                        results.AddRange(analyzerDiagnostics
                            .Where(d => d.Severity >= DiagnosticSeverity.Info)
                            .Select(d => $"  {d.Severity}: {d.GetMessage()} at {d.Location.GetLineSpan()}"));
                    }
                }

                return results.Any() ? string.Join(Environment.NewLine, results) : "No issues found. Code analysis completed successfully.";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to analyze code.", ex);
            }
        }

        /// <summary>
        /// Analyzes the structure of the code
        /// </summary>
        private async Task<List<string>> AnalyzeCodeStructureAsync(SyntaxNode root, SemanticModel semanticModel)
        {
            var results = new List<string>();

            // Count different types of declarations
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Count();
            var interfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Count();
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Count();
            var properties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().Count();
            var fields = root.DescendantNodes().OfType<FieldDeclarationSyntax>().Count();

            results.Add($"  Classes: {classes}");
            results.Add($"  Interfaces: {interfaces}");
            results.Add($"  Methods: {methods}");
            results.Add($"  Properties: {properties}");
            results.Add($"  Fields: {fields}");

            // Analyze namespaces
            var namespaces = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>()
                .Select(n => n.Name.ToString()).Distinct().ToList();
            
            if (namespaces.Any())
            {
                results.Add($"  Namespaces: {string.Join(", ", namespaces)}");
            }

            // Analyze using directives
            var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
                .Select(u => u.Name?.ToString()).Where(n => n != null).Distinct().Count();
            
            results.Add($"  Using directives: {usings}");

            return await Task.FromResult(results);
        }

        /// <summary>
        /// Analyzes code metrics
        /// </summary>
        private async Task<List<string>> AnalyzeCodeMetricsAsync(SyntaxNode root)
        {
            var results = new List<string>();

            // Calculate lines of code
            var totalLines = root.GetText().Lines.Count;
            var codeLines = root.GetText().Lines.Count(line => !string.IsNullOrWhiteSpace(line.ToString()));
            
            results.Add($"  Total lines: {totalLines}");
            results.Add($"  Code lines: {codeLines}");

            // Calculate cyclomatic complexity for methods
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            var complexMethods = new List<string>();

            foreach (var method in methods)
            {
                var complexity = CalculateCyclomaticComplexity(method);
                if (complexity > 10) // High complexity threshold
                {
                    complexMethods.Add($"    {method.Identifier.ValueText}: {complexity}");
                }
            }

            if (complexMethods.Any())
            {
                results.Add("  High complexity methods:");
                results.AddRange(complexMethods);
            }

            return await Task.FromResult(results);
        }

        /// <summary>
        /// Calculates cyclomatic complexity for a method
        /// </summary>
        private int CalculateCyclomaticComplexity(MethodDeclarationSyntax method)
        {
            var complexity = 1; // Base complexity

            // Count decision points
            var decisionNodes = method.DescendantNodes().Where(node =>
                node is IfStatementSyntax ||
                node is WhileStatementSyntax ||
                node is ForStatementSyntax ||
                node is ForEachStatementSyntax ||
                node is SwitchStatementSyntax ||
                node is ConditionalExpressionSyntax ||
                node is CatchClauseSyntax);

            complexity += decisionNodes.Count();

            // Count case labels in switch statements
            var caseLabels = method.DescendantNodes().OfType<CaseSwitchLabelSyntax>().Count();
            complexity += Math.Max(0, caseLabels - 1); // Subtract 1 because switch already counted

            return complexity;
        }

        /// <summary>
        /// Shuts down the analyzer asynchronously.
        /// </summary>
        public async Task ShutdownAsync()
        {
            // Clean up resources
            _analyzers.Clear();
            _references = ImmutableArray<MetadataReference>.Empty;
            await Task.CompletedTask;
        }

        /// <summary>
        /// Disposes the analyzer and releases resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the analyzer and releases resources.
        /// </summary>
        /// <param name="disposing">Indicates whether the method is called from Dispose or the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here
                    _analyzers.Clear();
                }

                // Dispose unmanaged resources here

                _disposed = true;
            }
        }
    }
}