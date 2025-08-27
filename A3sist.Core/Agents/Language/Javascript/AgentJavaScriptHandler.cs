using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using A3sist.Core.Agents.Base;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Enums;
using A3sist.Shared.Attributes;
using A3sist.Orchastrator.Agents.JavaScript.Services;
using Microsoft.Extensions.Logging;

namespace A3sist.Orchastrator.Agents.JavaScript
{
    /// <summary>
    /// JavaScript/TypeScript language-specific agent that provides code analysis, refactoring, and npm package management
    /// </summary>
    [AgentCapability("javascript", AgentType = AgentType.Language, FileExtensions = ".js,.ts,.jsx,.tsx,.json", Keywords = "analyze,refactor,npm")]
    [AgentCapability("typescript", Description = "Analyzes TypeScript code")]
    [AgentCapability("npm-management", Description = "Manages npm packages and dependencies")]
    [AgentCapability("js-refactoring", Description = "Refactors JavaScript/TypeScript code")]
    public class JavaScriptAgent : BaseAgent
    {
        private readonly JsAgentLoader _loader;
        private readonly JavaScriptAnalyzer _analyzer;
        private readonly JavaScriptRefactorEngine _refactorEngine;
        private readonly NpmPackageManager _packageManager;

        public override string Name => "JavaScriptAgent";
        public override AgentType Type => AgentType.Language;

        public JavaScriptAgent(ILogger<JavaScriptAgent> logger, IAgentConfiguration configuration)
            : base(logger, configuration)
        {
            _loader = new JsAgentLoader();
            _analyzer = new JavaScriptAnalyzer();
            _refactorEngine = new JavaScriptRefactorEngine();
            _packageManager = new NpmPackageManager();
        }

        protected override async Task InitializeAgentAsync()
        {
            Logger.LogInformation("Initializing JavaScript agent services");
            
            await Task.WhenAll(
                _loader.InitializeAsync(),
                _analyzer.InitializeAsync(),
                _refactorEngine.InitializeAsync(),
                _packageManager.InitializeAsync()
            );
            
            Logger.LogInformation("JavaScript agent services initialized successfully");
        }

        protected override async Task<AgentResult> HandleRequestAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
                return AgentResult.CreateFailure("Request cannot be null", agentName: Name);

            Logger.LogDebug("Processing JavaScript request: {RequestType}", GetRequestType(request));

            try
            {
                var requestType = GetRequestType(request);
                
                return requestType.ToLower() switch
                {
                    "analyze" => await AnalyzeCodeAsync(request, cancellationToken),
                    "refactor" => await RefactorCodeAsync(request, cancellationToken),
                    "npm" => await ManageNpmPackagesAsync(request, cancellationToken),
                    "lint" => await LintCodeAsync(request, cancellationToken),
                    "format" => await FormatCodeAsync(request, cancellationToken),
                    "test" => await RunTestsAsync(request, cancellationToken),
                    _ => AgentResult.CreateFailure($"Unsupported request type: {requestType}", agentName: Name)
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Logger.LogWarning("JavaScript agent operation was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing JavaScript request");
                return AgentResult.CreateFailure($"Error processing request: {ex.Message}", ex, Name);
            }
        }

        protected override async Task<bool> CanHandleRequestAsync(AgentRequest request)
        {
            if (request == null)
                return false;

            // Check if this is a JavaScript/TypeScript related request
            var requestType = GetRequestType(request);
            var supportedTypes = new[] { "analyze", "refactor", "npm", "lint", "format", "test" };
            
            if (!supportedTypes.Contains(requestType.ToLower()))
                return false;

            // Check if the file path indicates JavaScript/TypeScript code
            if (!string.IsNullOrEmpty(request.FilePath))
            {
                var extension = System.IO.Path.GetExtension(request.FilePath).ToLower();
                var jsExtensions = new[] { ".js", ".ts", ".jsx", ".tsx", ".json", ".mjs", ".cjs" };
                if (jsExtensions.Contains(extension))
                    return true;
            }

            // Check if the content appears to be JavaScript/TypeScript code
            if (!string.IsNullOrEmpty(request.Content))
            {
                return await IsLikelyJavaScriptCodeAsync(request.Content);
            }

            // Check context for JavaScript indicators
            if (request.Context?.ContainsKey("language") == true)
            {
                var language = request.Context["language"]?.ToString();
                return string.Equals(language, "javascript", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(language, "typescript", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(language, "js", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(language, "ts", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        protected override Task ShutdownAgentAsync()
        {
            Logger.LogInformation("Shutting down JavaScript agent services");
            
            await Task.WhenAll(
                _loader.ShutdownAsync(),
                _analyzer.ShutdownAsync(),
                _refactorEngine.ShutdownAsync(),
                _packageManager.ShutdownAsync()
            );
            
            Logger.LogInformation("JavaScript agent services shut down successfully");
        }

        private async Task<AgentResult> AnalyzeCodeAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            var code = GetCodeFromRequest(request);
            if (string.IsNullOrEmpty(code))
                return AgentResult.CreateFailure("No code provided for analysis", agentName: Name);

            try
            {
                var analysisResult = await _analyzer.AnalyzeCodeAsync(code);
                
                var metadata = new Dictionary<string, object>
                {
                    ["analysisType"] = "javascript-typescript",
                    ["codeLength"] = code.Length,
                    ["timestamp"] = DateTime.UtcNow
                };

                return AgentResult.CreateSuccess(analysisResult, metadata, Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error analyzing JavaScript code");
                return AgentResult.CreateFailure($"Code analysis failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> RefactorCodeAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            var code = GetCodeFromRequest(request);
            if (string.IsNullOrEmpty(code))
                return AgentResult.CreateFailure("No code provided for refactoring", agentName: Name);

            try
            {
                var refactoredCode = await _refactorEngine.RefactorCodeAsync(code);
                
                var metadata = new Dictionary<string, object>
                {
                    ["refactoringType"] = "javascript-automatic",
                    ["originalLength"] = code.Length,
                    ["refactoredLength"] = refactoredCode.Length,
                    ["timestamp"] = DateTime.UtcNow
                };

                return AgentResult.CreateSuccess(refactoredCode, metadata, Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error refactoring JavaScript code");
                return AgentResult.CreateFailure($"Code refactoring failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> ManageNpmPackagesAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var command = request.Context?.TryGetValue("npmCommand", out var npmCommandObj) == true ? npmCommandObj?.ToString() : null ?? "list";
                var packageName = request.Context?.TryGetValue("packageName", out var packageNameObj) == true ? packageNameObj?.ToString() : null;
                
                var result = await _packageManager.ExecuteCommandAsync(command, packageName);
                
                var metadata = new Dictionary<string, object>
                {
                    ["command"] = command,
                    ["packageName"] = packageName ?? "N/A",
                    ["timestamp"] = DateTime.UtcNow
                };

                return AgentResult.CreateSuccess(result, metadata, Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error managing npm packages");
                return AgentResult.CreateFailure($"NPM package management failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> LintCodeAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            var code = GetCodeFromRequest(request);
            if (string.IsNullOrEmpty(code))
                return AgentResult.CreateFailure("No code provided for linting", agentName: Name);

            try
            {
                var lintResult = await _analyzer.LintCodeAsync(code);
                
                var metadata = new Dictionary<string, object>
                {
                    ["lintType"] = "eslint-style",
                    ["codeLength"] = code.Length,
                    ["timestamp"] = DateTime.UtcNow
                };

                return AgentResult.CreateSuccess(lintResult, metadata, Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error linting JavaScript code");
                return AgentResult.CreateFailure($"Code linting failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> FormatCodeAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            var code = GetCodeFromRequest(request);
            if (string.IsNullOrEmpty(code))
                return AgentResult.CreateFailure("No code provided for formatting", agentName: Name);

            try
            {
                var formattedCode = await _refactorEngine.FormatCodeAsync(code);
                
                var metadata = new Dictionary<string, object>
                {
                    ["formatType"] = "prettier-style",
                    ["originalLength"] = code.Length,
                    ["formattedLength"] = formattedCode.Length,
                    ["timestamp"] = DateTime.UtcNow
                };

                return AgentResult.CreateSuccess(formattedCode, metadata, Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error formatting JavaScript code");
                return AgentResult.CreateFailure($"Code formatting failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> RunTestsAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var testPattern = request.Context?.TryGetValue("testPattern", out var testPatternObj) == true ? testPatternObj?.ToString() : null ?? "**/*.test.js";
                var testResult = await _packageManager.RunTestsAsync(testPattern);
                
                var metadata = new Dictionary<string, object>
                {
                    ["testPattern"] = testPattern,
                    ["timestamp"] = DateTime.UtcNow
                };

                return AgentResult.CreateSuccess(testResult, metadata, Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error running JavaScript tests");
                return AgentResult.CreateFailure($"Test execution failed: {ex.Message}", ex, Name);
            }
        }

        private string GetRequestType(AgentRequest request)
        {
            // Try to determine request type from various sources
            if (request.Context?.ContainsKey("requestType") == true)
                return request.Context["requestType"]?.ToString() ?? "analyze";

            if (!string.IsNullOrEmpty(request.Prompt))
            {
                var prompt = request.Prompt.ToLower();
                if (prompt.Contains("refactor")) return "refactor";
                if (prompt.Contains("analyze")) return "analyze";
                if (prompt.Contains("npm") || prompt.Contains("package")) return "npm";
                if (prompt.Contains("lint")) return "lint";
                if (prompt.Contains("format")) return "format";
                if (prompt.Contains("test")) return "test";
            }

            // Default to analyze
            return "analyze";
        }

        private string GetCodeFromRequest(AgentRequest request)
        {
            // Try to get code from various sources
            if (!string.IsNullOrEmpty(request.Content))
                return request.Content;

            if (request.Context?.ContainsKey("code") == true)
                return request.Context["code"]?.ToString() ?? string.Empty;

            if (!string.IsNullOrEmpty(request.FilePath) && System.IO.File.Exists(request.FilePath))
                return System.IO.File.ReadAllText(request.FilePath);

            return string.Empty;
        }

        private async Task<bool> IsLikelyJavaScriptCodeAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            // Simple heuristics to detect JavaScript/TypeScript code
            var jsKeywords = new[] { "function", "const", "let", "var", "class", "import", "export", "require" };
            var jsPatterns = new[] { "function ", "const ", "let ", "var ", "class ", "import ", "export ", "require(" };

            var keywordCount = jsKeywords.Count(keyword => content.Contains(keyword));
            var patternCount = jsPatterns.Count(pattern => content.Contains(pattern));

            // If we find multiple JS keywords/patterns, it's likely JavaScript code
            return await Task.FromResult(keywordCount >= 2 || patternCount >= 1);
        }

        public new void Dispose()
        {
            _loader?.Dispose();
            _analyzer?.Dispose();
            _refactorEngine?.Dispose();
            _packageManager?.Dispose();
            base.Dispose();
        }
    }
}
