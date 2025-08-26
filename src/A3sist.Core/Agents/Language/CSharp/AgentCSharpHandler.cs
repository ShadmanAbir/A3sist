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
using A3sist.Orchastrator.Agents.CSharp.Services;
using Microsoft.Extensions.Logging;

namespace A3sist.Orchastrator.Agents.CSharp
{
    /// <summary>
    /// C# language-specific agent that provides code analysis, refactoring, and XAML validation capabilities
    /// </summary>
    [AgentCapability("csharp", AgentType = AgentType.Language, FileExtensions = ".cs,.xaml", Keywords = "analyze,refactor,validate")]
    [AgentCapability("code-analysis", Description = "Analyzes C# code for issues and metrics")]
    [AgentCapability("refactoring", Description = "Refactors C# code for better quality")]
    [AgentCapability("xaml-validation", Description = "Validates XAML markup")]
    public class CSharpAgent : BaseAgent
    {
        private readonly Analyzer _analyzer;
        private readonly RefactorEngine _refactorEngine;
        private readonly XamlValidator _xamlValidator;

        public override string Name => "CSharpAgent";
        public override AgentType Type => AgentType.Language;

        public CSharpAgent(ILogger<CSharpAgent> logger, IAgentConfiguration configuration)
            : base(logger, configuration)
        {
            _analyzer = new Analyzer();
            _refactorEngine = new RefactorEngine();
            _xamlValidator = new XamlValidator();
        }

        protected override Task InitializeAgentAsync()
        {
            Logger.LogInformation("Initializing C# agent services");
            
            await Task.WhenAll(
                _analyzer.InitializeAsync(),
                _refactorEngine.InitializeAsync(),
                _xamlValidator.InitializeAsync()
            );
            
            Logger.LogInformation("C# agent services initialized successfully");
        }

        protected override async Task<AgentResult> HandleRequestAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
                return AgentResult.CreateFailure("Request cannot be null", agentName: Name);

            Logger.LogDebug("Processing C# request: {RequestType}", GetRequestType(request));

            try
            {
                var requestType = GetRequestType(request);
                
                return requestType.ToLower() switch
                {
                    "analyze" => await AnalyzeCodeAsync(request, cancellationToken),
                    "refactor" => await RefactorCodeAsync(request, cancellationToken),
                    "validatexaml" => await ValidateXamlAsync(request, cancellationToken),
                    "generate" => await GenerateCodeAsync(request, cancellationToken),
                    "fix" => await FixCodeAsync(request, cancellationToken),
                    _ => AgentResult.CreateFailure($"Unsupported request type: {requestType}", agentName: Name)
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Logger.LogWarning("C# agent operation was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing C# request");
                return AgentResult.CreateFailure($"Error processing request: {ex.Message}", ex, Name);
            }
        }

        protected override async Task<bool> CanHandleRequestAsync(AgentRequest request)
        {
            if (request == null)
                return false;

            // Check if this is a C# related request
            var requestType = GetRequestType(request);
            var supportedTypes = new[] { "analyze", "refactor", "validatexaml", "generate", "fix" };
            
            if (!supportedTypes.Contains(requestType.ToLower()))
                return false;

            // Check if the file path indicates C# code
            if (!string.IsNullOrEmpty(request.FilePath))
            {
                var extension = System.IO.Path.GetExtension(request.FilePath).ToLower();
                if (extension == ".cs" || extension == ".xaml")
                    return true;
            }

            // Check if the content appears to be C# code
            if (!string.IsNullOrEmpty(request.Content))
            {
                return await IsLikelyCSharpCodeAsync(request.Content);
            }

            // Check context for C# indicators
            if (request.Context?.ContainsKey("language") == true)
            {
                var language = request.Context["language"]?.ToString();
                return string.Equals(language, "csharp", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(language, "c#", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        protected override Task ShutdownAgentAsync()
        {
            Logger.LogInformation("Shutting down C# agent services");
            
            await Task.WhenAll(
                _analyzer.ShutdownAsync(),
                _refactorEngine.ShutdownAsync(),
                _xamlValidator.ShutdownAsync()
            );
            
            Logger.LogInformation("C# agent services shut down successfully");
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
                    ["analysisType"] = "syntax-semantic",
                    ["codeLength"] = code.Length,
                    ["timestamp"] = DateTime.UtcNow
                };

                return AgentResult.CreateSuccess(analysisResult, metadata, Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error analyzing C# code");
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
                    ["refactoringType"] = "automatic",
                    ["originalLength"] = code.Length,
                    ["refactoredLength"] = refactoredCode.Length,
                    ["timestamp"] = DateTime.UtcNow
                };

                return AgentResult.CreateSuccess(refactoredCode, metadata, Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error refactoring C# code");
                return AgentResult.CreateFailure($"Code refactoring failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> ValidateXamlAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            var xaml = GetCodeFromRequest(request);
            if (string.IsNullOrEmpty(xaml))
                return AgentResult.CreateFailure("No XAML provided for validation", agentName: Name);

            try
            {
                var validationResult = await _xamlValidator.ValidateXamlAsync(xaml);
                
                var metadata = new Dictionary<string, object>
                {
                    ["validationType"] = "xaml-syntax",
                    ["xamlLength"] = xaml.Length,
                    ["timestamp"] = DateTime.UtcNow
                };

                return AgentResult.CreateSuccess(validationResult, metadata, Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error validating XAML");
                return AgentResult.CreateFailure($"XAML validation failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> GenerateCodeAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            // This is a placeholder for code generation functionality
            // In a real implementation, this would use templates or AI to generate C# code
            
            var prompt = request.Prompt ?? "Generate C# code";
            Logger.LogInformation("Generating C# code for prompt: {Prompt}", prompt);

            try
            {
                // Placeholder implementation - in reality this would integrate with code generation services
                var generatedCode = $@"// Generated C# code for: {prompt}
// Generated at: {DateTime.UtcNow}

using System;

namespace GeneratedCode
{{
    public class GeneratedClass
    {{
        public void GeneratedMethod()
        {{
            // TODO: Implement generated functionality
            Console.WriteLine(""Generated method called"");
        }}
    }}
}}";

                var metadata = new Dictionary<string, object>
                {
                    ["generationType"] = "template-based",
                    ["prompt"] = prompt,
                    ["generatedLength"] = generatedCode.Length,
                    ["timestamp"] = DateTime.UtcNow
                };

                return AgentResult.CreateSuccess(generatedCode, metadata, Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error generating C# code");
                return AgentResult.CreateFailure($"Code generation failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> FixCodeAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            var code = GetCodeFromRequest(request);
            if (string.IsNullOrEmpty(code))
                return AgentResult.CreateFailure("No code provided for fixing", agentName: Name);

            try
            {
                // First analyze the code to find issues
                var analysisResult = await _analyzer.AnalyzeCodeAsync(code);
                
                // Then attempt to refactor/fix the code
                var fixedCode = await _refactorEngine.RefactorCodeAsync(code);
                
                var metadata = new Dictionary<string, object>
                {
                    ["fixType"] = "automatic-refactoring",
                    ["originalLength"] = code.Length,
                    ["fixedLength"] = fixedCode.Length,
                    ["analysisResult"] = analysisResult,
                    ["timestamp"] = DateTime.UtcNow
                };

                return AgentResult.CreateSuccess(fixedCode, metadata, Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error fixing C# code");
                return AgentResult.CreateFailure($"Code fixing failed: {ex.Message}", ex, Name);
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
                if (prompt.Contains("validate") && prompt.Contains("xaml")) return "validatexaml";
                if (prompt.Contains("generate")) return "generate";
                if (prompt.Contains("fix")) return "fix";
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

        private async Task<bool> IsLikelyCSharpCodeAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            // Simple heuristics to detect C# code
            var csharpKeywords = new[] { "using", "namespace", "class", "public", "private", "void", "string", "int" };
            var csharpPatterns = new[] { "using System", "namespace ", "public class", "private ", "public " };

            var keywordCount = csharpKeywords.Count(keyword => content.Contains(keyword));
            var patternCount = csharpPatterns.Count(pattern => content.Contains(pattern));

            // If we find multiple C# keywords/patterns, it's likely C# code
            return await Task.FromResult(keywordCount >= 2 || patternCount >= 1);
        }

        public new void Dispose()
        {
            _analyzer?.Dispose();
            _refactorEngine?.Dispose();
            base.Dispose();
        }
    }
}