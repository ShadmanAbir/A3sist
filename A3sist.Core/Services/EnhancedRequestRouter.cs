using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Core.Agents.Core;
using A3sist.Core.LLM;

namespace A3sist.Core.Services
{
    /// <summary>
    /// Enhanced Request Router with RAG (Retrieval-Augmented Generation) integration
    /// Replaces complex orchestrator with simplified, RAG-enhanced routing
    /// </summary>
    public class EnhancedRequestRouter : IDisposable
    {
        private readonly RAGEnhancedCSharpAgent _csharpAgent;
        private readonly EnhancedMCPClient _mcpClient;
        private readonly RAGService _ragService;
        private readonly ILogger<EnhancedRequestRouter> _logger;
        private bool _disposed;

        public EnhancedRequestRouter(
            RAGEnhancedCSharpAgent csharpAgent,
            EnhancedMCPClient mcpClient,
            RAGService ragService,
            ILogger<EnhancedRequestRouter> logger)
        {
            _csharpAgent = csharpAgent ?? throw new ArgumentNullException(nameof(csharpAgent));
            _mcpClient = mcpClient ?? throw new ArgumentNullException(nameof(mcpClient));
            _ragService = ragService ?? throw new ArgumentNullException(nameof(ragService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes a request with RAG-enhanced context and intelligent routing
        /// </summary>
        public async Task<AgentResult> ProcessRequestAsync(AgentRequest request)
        {
            try
            {
                _logger.LogInformation("Processing request with RAG enhancement: {RequestId}", request.Id);

                // Step 1: Retrieve relevant knowledge context
                var ragContext = await _ragService.RetrieveContextAsync(request);
                _logger.LogDebug("Retrieved {Count} knowledge entries for request: {RequestId}", 
                    ragContext.KnowledgeEntries.Count, request.Id);

                // Step 2: Augment the prompt with retrieved knowledge
                var augmentedPrompt = _ragService.AugmentPrompt(request.Prompt, ragContext);
                var enhancedRequest = request with { Prompt = augmentedPrompt };

                // Step 3: Route based on language and context with intelligent fallback
                var language = DetectLanguage(request.FilePath, request.Content);
                _logger.LogDebug("Detected language: {Language} for request: {RequestId}", language, request.Id);

                var result = await RouteRequestAsync(enhancedRequest, ragContext, language);

                // Step 4: Enhance result with knowledge metadata
                EnhanceResultWithRAGMetadata(result, ragContext);

                _logger.LogInformation("Successfully processed request: {RequestId} with {SourcesUsed} knowledge sources",
                    request.Id, ragContext.KnowledgeEntries.Select(e => e.Source).Distinct().Count());

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RAG-enhanced request: {RequestId}", request.Id);

                // Fallback to simple processing without RAG
                return await FallbackProcessingAsync(request);
            }
        }

        private async Task<AgentResult> RouteRequestAsync(AgentRequest enhancedRequest, RAGContext ragContext, string language)
        {
            return language switch
            {
                "csharp" or "xaml" => await _csharpAgent.HandleAsync(enhancedRequest, ragContext),
                "javascript" or "typescript" or "python" => await _mcpClient.ProcessAsync(enhancedRequest, ragContext),
                _ => await _mcpClient.ProcessWithLLMAsync(enhancedRequest, ragContext)
            };
        }

        private async Task<AgentResult> FallbackProcessingAsync(AgentRequest request)
        {
            try
            {
                _logger.LogWarning("Using fallback processing without RAG for request: {RequestId}", request.Id);

                var language = DetectLanguage(request.FilePath, request.Content);
                return language switch
                {
                    "csharp" or "xaml" => await _csharpAgent.HandleAsync(request),
                    _ => await _mcpClient.ProcessWithLLMAsync(request)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallback processing also failed for request: {RequestId}", request.Id);
                return AgentResult.CreateFailure($"All processing methods failed: {ex.Message}", ex);
            }
        }

        private void EnhanceResultWithRAGMetadata(AgentResult result, RAGContext ragContext)
        {
            result.Metadata ??= new Dictionary<string, object>();
            
            result.Metadata["RAGContext"] = new
            {
                KnowledgeSourcesUsed = ragContext.KnowledgeEntries.Select(e => e.Source).Distinct().ToArray(),
                RelevantEntriesCount = ragContext.KnowledgeEntries.Count,
                TopRelevance = ragContext.KnowledgeEntries.FirstOrDefault()?.Relevance ?? 0f,
                Language = ragContext.Language,
                Intent = ragContext.Intent,
                ProjectType = ragContext.ProjectType
            };

            result.Metadata["Citations"] = ragContext.KnowledgeEntries
                .Where(e => !string.IsNullOrEmpty(e.Url))
                .Select(e => new { e.Title, e.Source, e.Url, e.Relevance })
                .ToArray();
        }

        /// <summary>
        /// Detects the programming language from file path and content
        /// </summary>
        private string DetectLanguage(string? filePath, string? content)
        {
            // Primary detection from file extension
            if (!string.IsNullOrEmpty(filePath))
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                var languageFromExtension = extension switch
                {
                    ".cs" => "csharp",
                    ".xaml" => "xaml",
                    ".js" => "javascript",
                    ".ts" => "typescript",
                    ".py" => "python",
                    ".json" => "json",
                    ".xml" => "xml",
                    ".html" => "html",
                    ".css" => "css",
                    _ => null
                };

                if (!string.IsNullOrEmpty(languageFromExtension))
                    return languageFromExtension;
            }

            // Secondary detection from content patterns
            if (!string.IsNullOrEmpty(content))
            {
                var contentLower = content.ToLowerInvariant();
                
                // C# patterns
                if (contentLower.Contains("using system") || 
                    contentLower.Contains("namespace ") || 
                    contentLower.Contains("public class"))
                    return "csharp";

                // XAML patterns
                if (contentLower.Contains("<window") || 
                    contentLower.Contains("<usercontrol") || 
                    contentLower.Contains("xmlns=\"http://schemas.microsoft.com"))
                    return "xaml";

                // JavaScript/TypeScript patterns
                if (contentLower.Contains("function ") || 
                    contentLower.Contains("const ") || 
                    contentLower.Contains("=> "))
                    return "javascript";

                // Python patterns
                if (contentLower.Contains("def ") || 
                    contentLower.Contains("import ") || 
                    contentLower.Contains("from "))
                    return "python";
            }

            return "general";
        }

        /// <summary>
        /// Gets routing statistics for monitoring and optimization
        /// </summary>
        public RoutingStatistics GetRoutingStatistics()
        {
            // This would be enhanced with actual metrics collection
            return new RoutingStatistics
            {
                TotalRequestsProcessed = 0, // Would track actual metrics
                SuccessfulRAGEnhancements = 0,
                FallbacksUsed = 0,
                AverageProcessingTime = TimeSpan.Zero,
                MostUsedLanguages = new Dictionary<string, int>(),
                TopKnowledgeSources = new Dictionary<string, int>()
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _ragService?.Dispose();
                _mcpClient?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Statistics for request routing and RAG performance
    /// </summary>
    public class RoutingStatistics
    {
        public int TotalRequestsProcessed { get; set; }
        public int SuccessfulRAGEnhancements { get; set; }
        public int FallbacksUsed { get; set; }
        public TimeSpan AverageProcessingTime { get; set; }
        public Dictionary<string, int> MostUsedLanguages { get; set; } = new();
        public Dictionary<string, int> TopKnowledgeSources { get; set; } = new();
    }
}