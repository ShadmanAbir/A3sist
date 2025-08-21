using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace A3sist.Orchastrator.Agents.CSharp.Services
{
    /// <summary>
    /// Provides code analysis capabilities for C# code.
    /// </summary>
    public class Analyzer : IDisposable
    {
        private bool _disposed = false;
        private List<DiagnosticAnalyzer> _analyzers;

        /// <summary>
        /// Initializes a new instance of the Analyzer class.
        /// </summary>
        public Analyzer()
        {
            _analyzers = new List<DiagnosticAnalyzer>();
        }

        /// <summary>
        /// Initializes the analyzers asynchronously.
        /// </summary>
        public async Task InitializeAsync()
        {
            // Initialize analyzers
            _analyzers = new List<DiagnosticAnalyzer>
            {
                // Add your custom analyzers here
                // Example: new EmptyMethodAnalyzer()
            };

            await Task.CompletedTask;
        }

        /// <summary>
        /// Analyzes the provided C# code asynchronously.
        /// </summary>
        /// <param name="code">The C# code to analyze.</param>
        /// <returns>The analysis results.</returns>
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
                var compilation = CSharpCompilation.Create("Analysis")
                    .AddSyntaxTrees(tree)
                    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                    .WithAnalyzers(_analyzers.ToImmutableArray());

                var diagnostics = await compilation.GetAllDiagnosticsAsync();

                // Filter and format diagnostics
                var results = diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning)
                    .Select(d => $"{d.Id}: {d.GetMessage()} at {d.Location.GetLineSpan()}")
                    .ToList();

                return string.Join(Environment.NewLine, results);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to analyze code.", ex);
            }
        }

        /// <summary>
        /// Shuts down the analyzer asynchronously.
        /// </summary>
        public async Task ShutdownAsync()
        {
            // Clean up resources
            _analyzers.Clear();
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