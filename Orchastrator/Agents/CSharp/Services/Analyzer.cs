using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeAssist.Agents.CSharp.Services
{
    public class Analyzer
    {
        private List<DiagnosticAnalyzer> _analyzers;

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

        public async Task<string> AnalyzeCodeAsync(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Analysis")
                .AddSyntaxTrees(tree)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var diagnostics = await compilation.GetAllDiagnosticsAsync();

            // Filter and format diagnostics
            var results = new List<string>();
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error || diagnostic.Severity == DiagnosticSeverity.Warning)
                {
                    results.Add($"{diagnostic.Id}: {diagnostic.GetMessage()} at {diagnostic.Location.GetLineSpan()}");
                }
            }

            return string.Join(Environment.NewLine, results);
        }

        public async Task ShutdownAsync()
        {
            // Clean up resources
            _analyzers.Clear();
            await Task.CompletedTask;
        }
    }
}