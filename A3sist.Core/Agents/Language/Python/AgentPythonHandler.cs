using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using A3sist.Core.Agents.Base;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Enums;
using A3sist.Shared.Attributes;
using A3sist.Core.Agents.Language.Python.Services;
using Microsoft.Extensions.Logging;

namespace A3sist.Core.Agents.Language.Python
{
    /// <summary>
    /// Python language-specific agent that provides code analysis, refactoring, and package management
    /// </summary>
    [AgentCapability("python", AgentType = AgentType.Language, FileExtensions = ".py,.pyw,.pyi", Keywords = "analyze,refactor,pip,venv")]
    [AgentCapability("pip-management", Description = "Manages pip packages and dependencies")]
    [AgentCapability("python-refactoring", Description = "Refactors Python code")]
    [AgentCapability("virtual-environment", Description = "Manages Python virtual environments")]
    public class PythonAgent : BaseAgent
    {
        private readonly PythonAnalyzer _analyzer;
        private readonly PythonRefactorEngine _refactorEngine;
        private readonly PipPackageManager _packageManager;
        private readonly VirtualEnvironmentManager _venvManager;

        public override string Name => "PythonAgent";
        public override AgentType Type => AgentType.Language;

        public PythonAgent(ILogger<PythonAgent> logger, IAgentConfiguration configuration)
            : base(logger, configuration)
        {
            _analyzer = new PythonAnalyzer();
            _refactorEngine = new PythonRefactorEngine();
            _packageManager = new PipPackageManager();
            _venvManager = new VirtualEnvironmentManager();
        }

        protected override System.Threading.Tasks.Task InitializeAgentAsync()
        {
            Logger.LogInformation("Initializing Python agent services");
            
            await Task.WhenAll(
                _analyzer.InitializeAsync(),
                _refactorEngine.InitializeAsync(),
                _packageManager.InitializeAsync(),
                _venvManager.InitializeAsync()
            );
            
            Logger.LogInformation("Python agent services initialized successfully");
        }

        protected override async Task<AgentResult> HandleRequestAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
                return AgentResult.CreateFailure("Request cannot be null", agentName: Name);

            Logger.LogDebug("Processing Python request: {RequestType}", GetRequestType(request));

            try
            {
                var requestType = GetRequestType(request);
                
                return requestType.ToLower() switch
                {
                    "analyze" => await AnalyzeCodeAsync(request, cancellationToken),
                    "refactor" => await RefactorCodeAsync(request, cancellationToken),
                    "pip" => await ManagePipPackagesAsync(request, cancellationToken),
                    "venv" => await ManageVirtualEnvironmentAsync(request, cancellationToken),
                    "lint" => await LintCodeAsync(request, cancellationToken),
                    "format" => await FormatCodeAsync(request, cancellationToken),
                    "test" => await RunTestsAsync(request, cancellationToken),
                    _ => AgentResult.CreateFailure($"Unsupported request type: {requestType}", agentName: Name)
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Logger.LogWarning("Python agent operation was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing Python request");
                return AgentResult.CreateFailure($"Error processing request: {ex.Message}", ex, Name);
            }
        }

        protected override async Task<bool> CanHandleRequestAsync(AgentRequest request)
        {
            if (request == null)
                return false;

            // Check if this is a Python related request
            var requestType = GetRequestType(request);
            var supportedTypes = new[] { "analyze", "refactor", "pip", "venv", "lint", "format", "test" };
            
            if (!supportedTypes.Contains(requestType.ToLower()))
                return false;

            // Check if the file path indicates Python code
            if (!string.IsNullOrEmpty(request.FilePath))
            {
                var extension = System.IO.Path.GetExtension(request.FilePath).ToLower();
                var pythonExtensions = new[] { ".py", ".pyw", ".pyi", ".pyx" };
                if (pythonExtensions.Contains(extension))
                    return true;
            }

            // Check if the content appears to be Python code
            if (!string.IsNullOrEmpty(request.Content))
            {
                return await IsLikelyPythonCodeAsync(request.Content);
            }

            // Check context for Python indicators
            if (request.Context?.ContainsKey("language") == true)
            {
                var language = request.Context["language"]?.ToString();
                return string.Equals(language, "python", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(language, "py", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        protected override System.Threading.Tasks.Task ShutdownAgentAsync()
        {
            Logger.LogInformation("Shutting down Python agent services");
            
            await Task.WhenAll(
                _analyzer.ShutdownAsync(),
                _refactorEngine.ShutdownAsync(),
                _packageManager.ShutdownAsync(),
                _venvManager.ShutdownAsync()
            );
            
            Logger.LogInformation("Python agent services shut down successfully");
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
                    ["analysisType"] = "python-ast",
                    ["codeLength"] = code.Length,
                    ["timestamp"] = DateTime.UtcNow
                };

                return AgentResult.CreateSuccess(analysisResult, metadata, Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error analyzing Python code");
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
                    ["refactoringType"] = "python-automatic",
                    ["originalLength"] = code.Length,
                    ["refactoredLength"] = refactoredCode.Length,
                    ["timestamp"] = DateTime.UtcNow
                };

                return AgentResult.CreateSuccess(refactoredCode, metadata, Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error refactoring Python code");
                return AgentResult.CreateFailure($"Code refactoring failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> ManagePipPackagesAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var command = request.Context?.GetValueOrDefault("pipCommand")?.ToString() ?? "list";
                var packageName = request.Context?.GetValueOrDefault("packageName")?.ToString();
                
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
                Logger.LogError(ex, "Error managing pip packages");
                return AgentResult.CreateFailure($"Pip package management failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> ManageVirtualEnvironmentAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var command = request.Context?.GetValueOrDefault("venvCommand")?.ToString() ?? "status";
                var envName = request.Context?.GetValueOrDefault("envName")?.ToString();
                
                var result = await _venvManager.ExecuteCommandAsync(command, envName);
                
                var metadata = new Dictionary<string, object>
                {
                    ["command"] = command,
                    ["envName"] = envName ?? "N/A",
                    ["timestamp"] = DateTime.UtcNow
                };

                return AgentResult.CreateSuccess(result, metadata, Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error managing virtual environment");
                return AgentResult.CreateFailure($"Virtual environment management failed: {ex.Message}", ex, Name);
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
                    ["lintType"] = "pylint-style",
                    ["codeLength"] = code.Length,
                    ["timestamp"] = DateTime.UtcNow
                };

                return AgentResult.CreateSuccess(lintResult, metadata, Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error linting Python code");
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
                    ["formatType"] = "black-style",
                    ["originalLength"] = code.Length,
                    ["formattedLength"] = formattedCode.Length,
                    ["timestamp"] = DateTime.UtcNow
                };

                return AgentResult.CreateSuccess(formattedCode, metadata, Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error formatting Python code");
                return AgentResult.CreateFailure($"Code formatting failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> RunTestsAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var testPattern = request.Context?.GetValueOrDefault("testPattern")?.ToString() ?? "test_*.py";
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
                Logger.LogError(ex, "Error running Python tests");
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
                if (prompt.Contains("pip") || prompt.Contains("package")) return "pip";
                if (prompt.Contains("venv") || prompt.Contains("virtual")) return "venv";
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

        private async Task<bool> IsLikelyPythonCodeAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            // Simple heuristics to detect Python code
            var pythonKeywords = new[] { "def ", "class ", "import ", "from ", "if __name__", "print(", "return" };
            var pythonPatterns = new[] { "def ", "class ", "import ", "from ", "if __name__ == '__main__':" };

            var keywordCount = pythonKeywords.Count(keyword => content.Contains(keyword));
            var patternCount = pythonPatterns.Count(pattern => content.Contains(pattern));

            // If we find multiple Python keywords/patterns, it's likely Python code
            return await Task.FromResult(keywordCount >= 2 || patternCount >= 1);
        }

        public new void Dispose()
        {
            _analyzer?.Dispose();
            _refactorEngine?.Dispose();
            _packageManager?.Dispose();
            _venvManager?.Dispose();
            base.Dispose();
        }
    }
}