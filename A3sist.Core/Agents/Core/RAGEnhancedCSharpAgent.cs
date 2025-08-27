using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using A3sist.Core.Services;

namespace A3sist.Core.Agents.Core
{
    /// <summary>
    /// RAG-Enhanced C# Agent that combines Roslyn analysis with knowledge-augmented responses
    /// </summary>
    public class RAGEnhancedCSharpAgent
    {
        private readonly ILLMClient _llmClient;
        private readonly ILogger<RAGEnhancedCSharpAgent> _logger;

        public RAGEnhancedCSharpAgent(
            ILLMClient llmClient,
            ILogger<RAGEnhancedCSharpAgent> logger)
        {
            _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles C# code requests with optional RAG context enhancement
        /// </summary>
        public async Task<AgentResult> HandleAsync(AgentRequest request, RAGContext? ragContext = null)
        {
            try
            {
                _logger.LogInformation("Processing C# request: {RequestId} with RAG: {HasRAG}", 
                    request.Id, ragContext != null);

                var operation = DetermineOperation(request.Prompt);
                _logger.LogDebug("Determined operation: {Operation} for request: {RequestId}", operation, request.Id);

                return operation switch
                {
                    "analyze" => await AnalyzeCodeWithRAGAsync(request.Content, ragContext),
                    "refactor" => await RefactorCodeWithRAGAsync(request.Content, ragContext),
                    "fix" => await FixCodeWithRAGAsync(request.Content, ragContext),
                    "validatexaml" => await ValidateXamlAsync(request.Content),
                    "generate" => await GenerateCodeWithRAGAsync(request.Prompt, ragContext),
                    _ => await GenerateResponseWithRAGAsync(request.Prompt, request.Content, ragContext)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing C# request: {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Failed to process C# request: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Analyzes C# code using Roslyn and enhances with RAG knowledge
        /// </summary>
        private async Task<AgentResult> AnalyzeCodeWithRAGAsync(string? code, RAGContext? ragContext)
        {
            if (string.IsNullOrEmpty(code))
            {
                return AgentResult.CreateFailure("No code provided for analysis");
            }

            try
            {
                // Direct Roslyn analysis
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("Analysis", new[] { syntaxTree });
                var diagnostics = compilation.GetDiagnostics();

                var analysisResult = new
                {
                    SyntaxErrors = diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error)
                        .Select(d => new
                        {
                            Message = d.GetMessage(),
                            Location = d.Location.GetLineSpan().StartLinePosition,
                            Severity = d.Severity.ToString(),
                            Id = d.Id
                        }).ToArray(),
                    Warnings = diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Warning)
                        .Select(d => new
                        {
                            Message = d.GetMessage(),
                            Location = d.Location.GetLineSpan().StartLinePosition,
                            Id = d.Id
                        }).ToArray(),
                    Info = diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Info)
                        .Select(d => new
                        {
                            Message = d.GetMessage(),
                            Location = d.Location.GetLineSpan().StartLinePosition,
                            Id = d.Id
                        }).ToArray()
                };

                // Enhance with RAG context if available
                if (ragContext?.KnowledgeEntries.Any() == true)
                {
                    var ragPrompt = BuildRAGAnalysisPrompt(code, analysisResult, ragContext);
                    var enhancedAnalysis = await _llmClient.GetCompletionAsync(ragPrompt, new LLMOptions());

                    return AgentResult.CreateSuccess(enhancedAnalysis.Response, new Dictionary<string, object>
                    {
                        ["RoslynAnalysis"] = analysisResult,
                        ["RAGEnhanced"] = true,
                        ["KnowledgeSourcesUsed"] = ragContext.KnowledgeEntries.Select(e => e.Source).Distinct().ToArray(),
                        ["ProcessingTime"] = enhancedAnalysis.ProcessingTime,
                        ["TokensUsed"] = enhancedAnalysis.TokensUsed
                    });
                }

                return AgentResult.CreateSuccess(JsonSerializer.Serialize(analysisResult, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                }), new Dictionary<string, object>
                {
                    ["RoslynAnalysis"] = analysisResult,
                    ["RAGEnhanced"] = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing C# code");
                return AgentResult.CreateFailure($"Code analysis failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Refactors C# code with RAG-enhanced suggestions
        /// </summary>
        private async Task<AgentResult> RefactorCodeWithRAGAsync(string? code, RAGContext? ragContext)
        {
            if (string.IsNullOrEmpty(code))
            {
                return AgentResult.CreateFailure("No code provided for refactoring");
            }

            try
            {
                var refactoringPrompt = BuildRAGRefactoringPrompt(code, ragContext);
                var refactoredResponse = await _llmClient.GetCompletionAsync(refactoringPrompt, new LLMOptions());

                return AgentResult.CreateSuccess(refactoredResponse.Response, new Dictionary<string, object>
                {
                    ["OriginalLength"] = code.Length,
                    ["RefactoringType"] = "RAG-Enhanced",
                    ["RAGContext"] = ragContext != null,
                    ["ProcessingTime"] = refactoredResponse.ProcessingTime,
                    ["TokensUsed"] = refactoredResponse.TokensUsed
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refactoring C# code");
                return AgentResult.CreateFailure($"Code refactoring failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Fixes C# code issues with RAG-enhanced solutions
        /// </summary>
        private async Task<AgentResult> FixCodeWithRAGAsync(string? code, RAGContext? ragContext)
        {
            if (string.IsNullOrEmpty(code))
            {
                return AgentResult.CreateFailure("No code provided for fixing");
            }

            try
            {
                // First, analyze for issues
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("Fix", new[] { syntaxTree });
                var diagnostics = compilation.GetDiagnostics()
                    .Where(d => d.Severity >= DiagnosticSeverity.Warning)
                    .ToArray();

                if (!diagnostics.Any())
                {
                    return AgentResult.CreateSuccess("No issues detected in the code.", new Dictionary<string, object>
                    {
                        ["IssuesFound"] = 0,
                        ["RAGEnhanced"] = ragContext != null
                    });
                }

                var fixPrompt = BuildRAGFixPrompt(code, diagnostics, ragContext);
                var fixResponse = await _llmClient.GetCompletionAsync(fixPrompt, new LLMOptions());

                return AgentResult.CreateSuccess(fixResponse.Response, new Dictionary<string, object>
                {
                    ["IssuesFound"] = diagnostics.Length,
                    ["IssueTypes"] = diagnostics.Select(d => d.Id).Distinct().ToArray(),
                    ["RAGEnhanced"] = ragContext != null,
                    ["ProcessingTime"] = fixResponse.ProcessingTime,
                    ["TokensUsed"] = fixResponse.TokensUsed
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing C# code");
                return AgentResult.CreateFailure($"Code fixing failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates XAML code structure
        /// </summary>
        private async Task<AgentResult> ValidateXamlAsync(string? xamlContent)
        {
            if (string.IsNullOrEmpty(xamlContent))
            {
                return AgentResult.CreateFailure("No XAML content provided for validation");
            }

            try
            {
                // Basic XAML validation (could be enhanced with actual XAML parser)
                var issues = new List<string>();

                if (!xamlContent.TrimStart().StartsWith("<"))
                    issues.Add("XAML content should start with an XML element");

                if (!xamlContent.Contains("xmlns"))
                    issues.Add("XAML should contain namespace declarations");

                var openTags = xamlContent.Count(c => c == '<');
                var closeTags = xamlContent.Count(c => c == '>');
                if (openTags != closeTags)
                    issues.Add("Mismatched XML tags detected");

                var validationResult = new
                {
                    IsValid = !issues.Any(),
                    Issues = issues.ToArray(),
                    Length = xamlContent.Length,
                    ElementCount = xamlContent.Split('<').Length - 1
                };

                return AgentResult.CreateSuccess(JsonSerializer.Serialize(validationResult, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                }), new Dictionary<string, object>
                {
                    ["ValidationType"] = "XAML",
                    ["IsValid"] = validationResult.IsValid,
                    ["IssueCount"] = issues.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating XAML");
                return AgentResult.CreateFailure($"XAML validation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generates C# code with RAG-enhanced context
        /// </summary>
        private async Task<AgentResult> GenerateCodeWithRAGAsync(string prompt, RAGContext? ragContext)
        {
            try
            {
                var generationPrompt = BuildRAGGenerationPrompt(prompt, ragContext);
                var generatedResponse = await _llmClient.GetCompletionAsync(generationPrompt, new LLMOptions());

                return AgentResult.CreateSuccess(generatedResponse.Response, new Dictionary<string, object>
                {
                    ["GenerationType"] = "C# Code",
                    ["RAGEnhanced"] = ragContext != null,
                    ["ProcessingTime"] = generatedResponse.ProcessingTime,
                    ["TokensUsed"] = generatedResponse.TokensUsed
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating C# code");
                return AgentResult.CreateFailure($"Code generation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generates general response with RAG enhancement
        /// </summary>
        private async Task<AgentResult> GenerateResponseWithRAGAsync(string prompt, string? code, RAGContext? ragContext)
        {
            try
            {
                var responsePrompt = BuildRAGResponsePrompt(prompt, code, ragContext);
                var response = await _llmClient.GetCompletionAsync(responsePrompt, new LLMOptions());

                return AgentResult.CreateSuccess(response.Response, new Dictionary<string, object>
                {
                    ["ResponseType"] = "General",
                    ["RAGEnhanced"] = ragContext != null,
                    ["ProcessingTime"] = response.ProcessingTime,
                    ["TokensUsed"] = response.TokensUsed
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating response");
                return AgentResult.CreateFailure($"Response generation failed: {ex.Message}", ex);
            }
        }

        private string DetermineOperation(string prompt)
        {
            var promptLower = prompt.ToLowerInvariant();

            if (promptLower.Contains("analyze") || promptLower.Contains("review") || promptLower.Contains("check"))
                return "analyze";
            if (promptLower.Contains("refactor") || promptLower.Contains("improve") || promptLower.Contains("optimize"))
                return "refactor";
            if (promptLower.Contains("fix") || promptLower.Contains("repair") || promptLower.Contains("correct"))
                return "fix";
            if (promptLower.Contains("xaml") && (promptLower.Contains("validate") || promptLower.Contains("check")))
                return "validatexaml";
            if (promptLower.Contains("generate") || promptLower.Contains("create") || promptLower.Contains("write"))
                return "generate";

            return "general";
        }

        private string BuildRAGAnalysisPrompt(string code, object analysisResult, RAGContext ragContext)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("# Enhanced C# Code Analysis");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("## Code to Analyze:");
            promptBuilder.AppendLine("```csharp");
            promptBuilder.AppendLine(code);
            promptBuilder.AppendLine("```");
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("## Roslyn Analysis Results:");
            promptBuilder.AppendLine(JsonSerializer.Serialize(analysisResult, new JsonSerializerOptions { WriteIndented = true }));
            promptBuilder.AppendLine();

            if (ragContext.KnowledgeEntries.Any())
            {
                promptBuilder.AppendLine("## Relevant Best Practices and Patterns:");
                foreach (var entry in ragContext.KnowledgeEntries.Take(3))
                {
                    promptBuilder.AppendLine($"### {entry.Title}");
                    var content = entry.Content.Length > 300 ? entry.Content.Substring(0, 300) + "..." : entry.Content;
                    promptBuilder.AppendLine(content);
                    promptBuilder.AppendLine();
                }
            }

            promptBuilder.AppendLine("## Instructions:");
            promptBuilder.AppendLine("Provide a comprehensive analysis that includes:");
            promptBuilder.AppendLine("1. Interpretation of Roslyn diagnostics");
            promptBuilder.AppendLine("2. Code quality assessment");
            promptBuilder.AppendLine("3. Best practice recommendations based on retrieved knowledge");
            promptBuilder.AppendLine("4. Security and performance considerations");
            promptBuilder.AppendLine("5. Specific improvement suggestions with examples");

            return promptBuilder.ToString();
        }

        private string BuildRAGRefactoringPrompt(string code, RAGContext? ragContext)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("# C# Code Refactoring with Best Practices");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("## Original Code:");
            promptBuilder.AppendLine("```csharp");
            promptBuilder.AppendLine(code);
            promptBuilder.AppendLine("```");
            promptBuilder.AppendLine();

            if (ragContext?.KnowledgeEntries.Any() == true)
            {
                promptBuilder.AppendLine("## Relevant Patterns and Examples:");
                var codeExamples = ragContext.KnowledgeEntries
                    .Where(e => e.Metadata?.ContainsKey("Type") == true && e.Metadata["Type"].ToString() == "CodeExample")
                    .Take(2);

                foreach (var example in codeExamples)
                {
                    promptBuilder.AppendLine($"### {example.Title}");
                    promptBuilder.AppendLine("```csharp");
                    promptBuilder.AppendLine(example.Content);
                    promptBuilder.AppendLine("```");
                    promptBuilder.AppendLine();
                }
            }

            promptBuilder.AppendLine("## Refactoring Instructions:");
            promptBuilder.AppendLine("1. Apply modern C# patterns and best practices");
            promptBuilder.AppendLine("2. Improve readability and maintainability");
            promptBuilder.AppendLine("3. Follow SOLID principles");
            promptBuilder.AppendLine("4. Add appropriate error handling");
            promptBuilder.AppendLine("5. Use async/await properly if applicable");
            promptBuilder.AppendLine("6. Apply relevant patterns from the provided examples");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Provide the refactored code with comments explaining the improvements.");

            return promptBuilder.ToString();
        }

        private string BuildRAGFixPrompt(string code, Diagnostic[] diagnostics, RAGContext? ragContext)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("# C# Code Issue Resolution");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("## Code with Issues:");
            promptBuilder.AppendLine("```csharp");
            promptBuilder.AppendLine(code);
            promptBuilder.AppendLine("```");
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("## Detected Issues:");
            foreach (var diagnostic in diagnostics.Take(10))
            {
                promptBuilder.AppendLine($"- **{diagnostic.Id}**: {diagnostic.GetMessage()}");
                promptBuilder.AppendLine($"  Location: Line {diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1}");
                promptBuilder.AppendLine($"  Severity: {diagnostic.Severity}");
                promptBuilder.AppendLine();
            }

            if (ragContext?.KnowledgeEntries.Any() == true)
            {
                promptBuilder.AppendLine("## Relevant Solutions and Best Practices:");
                foreach (var entry in ragContext.KnowledgeEntries.Take(2))
                {
                    promptBuilder.AppendLine($"### {entry.Title}");
                    var content = entry.Content.Length > 200 ? entry.Content.Substring(0, 200) + "..." : entry.Content;
                    promptBuilder.AppendLine(content);
                    promptBuilder.AppendLine();
                }
            }

            promptBuilder.AppendLine("## Instructions:");
            promptBuilder.AppendLine("1. Fix all identified issues");
            promptBuilder.AppendLine("2. Ensure the code compiles without errors");
            promptBuilder.AppendLine("3. Maintain the original functionality");
            promptBuilder.AppendLine("4. Apply best practices from the knowledge context");
            promptBuilder.AppendLine("5. Explain the fixes made");

            return promptBuilder.ToString();
        }

        private string BuildRAGGenerationPrompt(string prompt, RAGContext? ragContext)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("# C# Code Generation Request");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("## User Request:");
            promptBuilder.AppendLine(prompt);
            promptBuilder.AppendLine();

            if (ragContext?.KnowledgeEntries.Any() == true)
            {
                promptBuilder.AppendLine("## Relevant Examples and Patterns:");
                foreach (var entry in ragContext.KnowledgeEntries.Take(3))
                {
                    promptBuilder.AppendLine($"### {entry.Title}");
                    var content = entry.Content.Length > 300 ? entry.Content.Substring(0, 300) + "..." : entry.Content;
                    promptBuilder.AppendLine(content);
                    promptBuilder.AppendLine();
                }
            }

            promptBuilder.AppendLine("## Instructions:");
            promptBuilder.AppendLine("1. Generate clean, well-documented C# code");
            promptBuilder.AppendLine("2. Follow modern C# conventions and best practices");
            promptBuilder.AppendLine("3. Include appropriate error handling");
            promptBuilder.AppendLine("4. Use relevant patterns from the provided examples");
            promptBuilder.AppendLine("5. Add helpful comments explaining the code");

            return promptBuilder.ToString();
        }

        private string BuildRAGResponsePrompt(string prompt, string? code, RAGContext? ragContext)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("# C# Development Assistance");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("## Request:");
            promptBuilder.AppendLine(prompt);
            promptBuilder.AppendLine();

            if (!string.IsNullOrEmpty(code))
            {
                promptBuilder.AppendLine("## Code Context:");
                promptBuilder.AppendLine("```csharp");
                promptBuilder.AppendLine(code);
                promptBuilder.AppendLine("```");
                promptBuilder.AppendLine();
            }

            if (ragContext?.KnowledgeEntries.Any() == true)
            {
                promptBuilder.AppendLine("## Relevant Knowledge:");
                foreach (var entry in ragContext.KnowledgeEntries.Take(3))
                {
                    promptBuilder.AppendLine($"### {entry.Title} (Relevance: {entry.Relevance:F2})");
                    var content = entry.Content.Length > 400 ? entry.Content.Substring(0, 400) + "..." : entry.Content;
                    promptBuilder.AppendLine(content);
                    promptBuilder.AppendLine();
                }
            }

            promptBuilder.AppendLine("## Instructions:");
            promptBuilder.AppendLine("Provide a helpful response that:");
            promptBuilder.AppendLine("1. Addresses the user's request directly");
            promptBuilder.AppendLine("2. Incorporates relevant knowledge from the context");
            promptBuilder.AppendLine("3. Provides practical examples when applicable");
            promptBuilder.AppendLine("4. Follows current C# best practices");

            return promptBuilder.ToString();
        }
    }
}