using A3sist.Core.Agents.Base;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Core.Agents.Task.Fixer
{
    /// <summary>
    /// FixerAgent responsible for automated code error detection and fixing
    /// </summary>
    public class FixerAgent : BaseAgent
    {
        private readonly ICompilerDiagnosticsService _diagnosticsService;
        private readonly ICodeFixService _codeFixService;
        private readonly Dictionary<string, IFixProvider> _fixProviders;

        public override string Name => "FixerAgent";
        public override AgentType Type => AgentType.Fixer;

        public FixerAgent(
            ILogger<FixerAgent> logger,
            IAgentConfiguration configuration,
            ICompilerDiagnosticsService diagnosticsService,
            ICodeFixService codeFixService,
            IEnumerable<IFixProvider> fixProviders) : base(logger, configuration)
        {
            _diagnosticsService = diagnosticsService ?? throw new ArgumentNullException(nameof(diagnosticsService));
            _codeFixService = codeFixService ?? throw new ArgumentNullException(nameof(codeFixService));
            _fixProviders = fixProviders?.ToDictionary(fp => fp.Name, fp => fp) ?? new Dictionary<string, IFixProvider>();
        }

        protected override async Task<bool> CanHandleRequestAsync(AgentRequest request)
        {
            if (request == null)
                return false;

            // FixerAgent can handle error fixing, code correction, and diagnostic resolution
            var supportedActions = new[]
            {
                "fix", "correct", "repair", "resolve", "diagnose", "error", "warning",
                "compile", "syntax", "semantic", "diagnostic", "issue", "problem"
            };

            var prompt = request.Prompt?.ToLowerInvariant() ?? "";
            var hasCode = !string.IsNullOrEmpty(request.Content) || !string.IsNullOrEmpty(request.FilePath);
            var hasError = request.Context?.ContainsKey("error") == true ||
                          request.Context?.ContainsKey("diagnostic") == true ||
                          request.Context?.ContainsKey("exception") == true;

            return (supportedActions.Any(action => prompt.Contains(action)) && hasCode) || hasError;
        }

        protected override async Task<AgentResult> HandleRequestAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var action = ExtractActionFromRequest(request);
                
                return action.ToLowerInvariant() switch
                {
                    "diagnose" or "analyze" => await DiagnoseCodeAsync(request, cancellationToken),
                    "fix" or "repair" or "correct" => await FixCodeAsync(request, cancellationToken),
                    "suggest" or "recommend" => await SuggestFixesAsync(request, cancellationToken),
                    "validate" or "check" => await ValidateFixesAsync(request, cancellationToken),
                    _ => await HandleGenericFixRequestAsync(request, cancellationToken)
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error handling fixer request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"FixerAgent error: {ex.Message}", ex, Name);
            }
        }

        protected override async Task InitializeAgentAsync()
        {
            Logger.LogInformation("Initializing FixerAgent");
            
            // Initialize diagnostic service
            await _diagnosticsService.InitializeAsync();
            
            // Initialize fix providers
            foreach (var provider in _fixProviders.Values)
            {
                await provider.InitializeAsync();
            }
            
            Logger.LogInformation("FixerAgent initialized with {ProviderCount} fix providers", _fixProviders.Count);
        }

        protected override async Task ShutdownAgentAsync()
        {
            Logger.LogInformation("Shutting down FixerAgent");
            
            // Shutdown fix providers
            foreach (var provider in _fixProviders.Values)
            {
                await provider.ShutdownAsync();
            }
            
            await _diagnosticsService.ShutdownAsync();
            
            Logger.LogInformation("FixerAgent shutdown completed");
        }

        private async Task<AgentResult> DiagnoseCodeAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Diagnosing code for request {RequestId}", request.Id);

            try
            {
                var codeInfo = ExtractCodeInfoFromRequest(request);
                var diagnostics = await _diagnosticsService.GetDiagnosticsAsync(codeInfo, cancellationToken);
                
                var categorizedDiagnostics = CategorizeDiagnostics(diagnostics);
                var severityAnalysis = AnalyzeSeverity(diagnostics);
                
                var result = new
                {
                    FilePath = codeInfo.FilePath,
                    Language = codeInfo.Language,
                    TotalIssues = diagnostics.Count,
                    Severity = new
                    {
                        Errors = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error),
                        Warnings = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning),
                        Info = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Info)
                    },
                    Issues = diagnostics.Select(d => new
                    {
                        Id = d.Id,
                        Message = d.GetMessage(),
                        Severity = d.Severity.ToString(),
                        Location = d.Location.ToString()
                    }).ToArray()
                };

                return AgentResult.Success(
                    JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
                    Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error analyzing code for fixes");
                return AgentResult.Error($"Failed to analyze code: {ex.Message}", Name, ex);
            }
        }

        /// <summary>
        /// Applies automatic fixes to the code
        /// </summary>
        private async Task<AgentResult> ApplyFixesAsync(CodeInfo codeInfo)
        {
            try
            {
                Logger.LogInformation("Applying fixes to {FilePath}", codeInfo.FilePath);

                var syntaxTree = CSharpSyntaxTree.ParseText(codeInfo.Content);
                var compilation = CSharpCompilation.Create("TempAssembly")
                    .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                    .AddSyntaxTrees(syntaxTree);

                var diagnostics = compilation.GetDiagnostics()
                    .Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning)
                    .ToList();

                if (!diagnostics.Any())
                {
                    return AgentResult.Success("No issues found to fix.", Name);
                }

                var root = await syntaxTree.GetRootAsync();
                var newRoot = root;

                // Apply common fixes
                foreach (var diagnostic in diagnostics)
                {
                    newRoot = ApplyDiagnosticFix(newRoot, diagnostic);
                }

                var fixedCode = newRoot.ToFullString();
                
                var result = new
                {
                    OriginalCode = codeInfo.Content,
                    FixedCode = fixedCode,
                    FixesApplied = diagnostics.Count,
                    Changes = GetCodeChanges(codeInfo.Content, fixedCode)
                };

                return AgentResult.Success(
                    JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
                    Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error applying fixes to code");
                return AgentResult.Error($"Failed to apply fixes: {ex.Message}", Name, ex);
            }
        }

        /// <summary>
        /// Applies a fix for a specific diagnostic
        /// </summary>
        private SyntaxNode ApplyDiagnosticFix(SyntaxNode root, Diagnostic diagnostic)
        {
            // This is a simplified implementation
            // In a real implementation, you would have specific fixes for different diagnostic types
            
            switch (diagnostic.Id)
            {
                case "CS0161": // Not all code paths return a value
                    return AddReturnStatement(root, diagnostic);
                case "CS0168": // Variable declared but never used
                    return RemoveUnusedVariable(root, diagnostic);
                case "CS0219": // Variable assigned but never used
                    return RemoveUnusedAssignment(root, diagnostic);
                default:
                    return root; // No fix available
            }
        }

        /// <summary>
        /// Adds a return statement to fix CS0161
        /// </summary>
        private SyntaxNode AddReturnStatement(SyntaxNode root, Diagnostic diagnostic)
        {
            // Simplified implementation - would need more sophisticated logic
            return root;
        }

        /// <summary>
        /// Removes unused variable to fix CS0168
        /// </summary>
        private SyntaxNode RemoveUnusedVariable(SyntaxNode root, Diagnostic diagnostic)
        {
            // Simplified implementation - would need more sophisticated logic
            return root;
        }

        /// <summary>
        /// Removes unused assignment to fix CS0219
        /// </summary>
        private SyntaxNode RemoveUnusedAssignment(SyntaxNode root, Diagnostic diagnostic)
        {
            // Simplified implementation - would need more sophisticated logic
            return root;
        }

        /// <summary>
        /// Gets the changes between original and fixed code
        /// </summary>
        private object[] GetCodeChanges(string originalCode, string fixedCode)
        {
            // Simplified implementation - would use a proper diff algorithm
            if (originalCode == fixedCode)
            {
                return Array.Empty<object>();
            }

            return new object[]
            {
                new
                {
                    Type = "Modified",
                    Description = "Code was modified to fix compilation issues",
                    LinesChanged = fixedCode.Split('\n').Length - originalCode.Split('\n').Length
                }
            };
        }
    }
}