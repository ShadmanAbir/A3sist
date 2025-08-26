using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;

namespace A3sist.Core.Services
{
    /// <summary>
    /// RAG (Retrieval-Augmented Generation) Service for knowledge retrieval and prompt augmentation
    /// </summary>
    public class RAGService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly IKnowledgeRepository _knowledgeRepository;
        private readonly ILogger<RAGService> _logger;
        private readonly MemoryCache _cache;
        private bool _disposed;

        public RAGService(
            HttpClient httpClient,
            IKnowledgeRepository knowledgeRepository,
            ILogger<RAGService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _knowledgeRepository = knowledgeRepository ?? throw new ArgumentNullException(nameof(knowledgeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 1000,
                CompactionPercentage = 0.25
            });
        }

        /// <summary>
        /// Retrieves relevant knowledge context for a given request
        /// </summary>
        public async Task<RAGContext> RetrieveContextAsync(AgentRequest request)
        {
            var cacheKey = GenerateCacheKey(request);
            
            if (_cache.TryGetValue(cacheKey, out RAGContext? cachedContext) && cachedContext != null)
            {
                _logger.LogDebug("Retrieved context from cache for request: {RequestId}", request.Id);
                return cachedContext;
            }

            var context = new RAGContext
            {
                Language = DetectLanguage(request.FilePath),
                ProjectType = DetectProjectType(request.FilePath),
                Intent = ClassifyIntent(request.Prompt)
            };

            try
            {
                // Parallel retrieval from multiple sources
                var retrievalTasks = new List<Task<IEnumerable<KnowledgeEntry>>>
                {
                    RetrieveFromInternalAsync(request, context),
                    RetrieveFromMCPKnowledgeAsync(request, context),
                    RetrieveFromDocumentationAsync(request, context)
                };

                var results = await Task.WhenAll(retrievalTasks);

                context.KnowledgeEntries = results
                    .SelectMany(r => r)
                    .OrderByDescending(e => e.Relevance)
                    .Take(10) // Top 10 most relevant entries
                    .ToList();

                // Cache the context
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
                    Size = 1
                };
                _cache.Set(cacheKey, context, cacheOptions);

                _logger.LogInformation("Retrieved {Count} knowledge entries for request: {RequestId}",
                    context.KnowledgeEntries.Count, request.Id);

                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving RAG context for request: {RequestId}", request.Id);
                
                // Return context with empty knowledge entries on error
                return context;
            }
        }

        /// <summary>
        /// Augments the original prompt with retrieved knowledge context
        /// </summary>
        public string AugmentPrompt(string originalPrompt, RAGContext context)
        {
            if (!context.KnowledgeEntries.Any())
                return originalPrompt;

            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("# Enhanced Request with Retrieved Knowledge");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("## Original Request:");
            promptBuilder.AppendLine(originalPrompt);
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("## Relevant Knowledge Context:");

            foreach (var entry in context.KnowledgeEntries.Take(5)) // Top 5 entries
            {
                promptBuilder.AppendLine($"### {entry.Title} (Relevance: {entry.Relevance:F2})");
                var contentPreview = entry.Content.Length > 500 
                    ? entry.Content.Substring(0, 500) + "..."
                    : entry.Content;
                promptBuilder.AppendLine(contentPreview);
                promptBuilder.AppendLine();
            }

            promptBuilder.AppendLine("## Instructions:");
            promptBuilder.AppendLine("Please provide a comprehensive response considering both the original request and the retrieved knowledge context above. Include relevant examples and best practices.");

            return promptBuilder.ToString();
        }

        private async Task<IEnumerable<KnowledgeEntry>> RetrieveFromInternalAsync(AgentRequest request, RAGContext context)
        {
            try
            {
                return await _knowledgeRepository.SearchAsync(request.Prompt, context.Language, context.ProjectType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve from internal knowledge base");
                return Enumerable.Empty<KnowledgeEntry>();
            }
        }

        private async Task<IEnumerable<KnowledgeEntry>> RetrieveFromMCPKnowledgeAsync(AgentRequest request, RAGContext context)
        {
            try
            {
                var mcpRequest = new
                {
                    method = "tools/call",
                    parameters = new
                    {
                        name = "documentation_search",
                        arguments = new
                        {
                            query = request.Prompt,
                            scope = context.Language,
                            doc_type = "reference",
                            max_results = 5
                        }
                    }
                };

                var response = await _httpClient.PostAsJsonAsync("http://localhost:3003/mcp", mcpRequest);
                var content = await response.Content.ReadAsStringAsync();
                var mcpResult = JsonSerializer.Deserialize<MCPResponse>(content);

                return mcpResult?.Result?.Results?.Select(r => new KnowledgeEntry
                {
                    Title = r.Title ?? "MCP Result",
                    Content = r.Content ?? "",
                    Source = "MCP Knowledge Server",
                    Relevance = r.Relevance,
                    Url = r.Url
                }) ?? Enumerable.Empty<KnowledgeEntry>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve from MCP knowledge server");
                return Enumerable.Empty<KnowledgeEntry>();
            }
        }

        private async Task<IEnumerable<KnowledgeEntry>> RetrieveFromDocumentationAsync(AgentRequest request, RAGContext context)
        {
            try
            {
                var examplesRequest = new
                {
                    method = "tools/call",
                    parameters = new
                    {
                        name = "code_examples",
                        arguments = new
                        {
                            language = context.Language,
                            pattern = ExtractCodePattern(request.Prompt),
                            max_results = 3
                        }
                    }
                };

                var response = await _httpClient.PostAsJsonAsync("http://localhost:3003/mcp", examplesRequest);
                var content = await response.Content.ReadAsStringAsync();
                var mcpResult = JsonSerializer.Deserialize<MCPResponse>(content);

                return mcpResult?.Result?.Examples?.Select(e => new KnowledgeEntry
                {
                    Title = $"Code Example: {e.Name}",
                    Content = e.Code ?? "",
                    Source = "Code Examples",
                    Relevance = 0.8f,
                    Metadata = new Dictionary<string, object>
                    {
                        ["Type"] = "CodeExample",
                        ["Language"] = context.Language
                    }
                }) ?? Enumerable.Empty<KnowledgeEntry>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve code examples");
                return Enumerable.Empty<KnowledgeEntry>();
            }
        }

        private string DetectLanguage(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "general";

            var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".cs" => "csharp",
                ".js" => "javascript",
                ".ts" => "typescript",
                ".py" => "python",
                ".xaml" => "xaml",
                ".json" => "json",
                _ => "general"
            };
        }

        private string DetectProjectType(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "general";

            if (filePath.Contains(".csproj") || filePath.Contains(".sln"))
                return "dotnet";
            if (filePath.Contains("package.json"))
                return "nodejs";
            if (filePath.Contains("requirements.txt") || filePath.Contains("pyproject.toml"))
                return "python";

            return "general";
        }

        private string ClassifyIntent(string prompt)
        {
            var promptLower = prompt.ToLowerInvariant();

            if (promptLower.Contains("analyze") || promptLower.Contains("review"))
                return "analysis";
            if (promptLower.Contains("refactor") || promptLower.Contains("improve"))
                return "refactoring";
            if (promptLower.Contains("fix") || promptLower.Contains("error") || promptLower.Contains("bug"))
                return "fixing";
            if (promptLower.Contains("explain") || promptLower.Contains("how"))
                return "explanation";
            if (promptLower.Contains("create") || promptLower.Contains("generate"))
                return "generation";

            return "general";
        }

        private string ExtractCodePattern(string prompt)
        {
            // Simple pattern extraction - could be enhanced with NLP
            var patterns = new[] { "async", "await", "interface", "class", "method", "property", "enum" };
            return patterns.FirstOrDefault(p => prompt.Contains(p, StringComparison.OrdinalIgnoreCase)) ?? "general";
        }

        private string GenerateCacheKey(AgentRequest request)
        {
            var keyData = $"{request.Prompt}|{request.FilePath}|{request.Content?.GetHashCode()}";
            var keyBytes = Encoding.UTF8.GetBytes(keyData);
            return Convert.ToBase64String(keyBytes);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cache?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Context information for RAG processing
    /// </summary>
    public class RAGContext
    {
        public string Language { get; set; } = string.Empty;
        public string ProjectType { get; set; } = string.Empty;
        public string Intent { get; set; } = string.Empty;
        public List<KnowledgeEntry> KnowledgeEntries { get; set; } = new();
    }

    /// <summary>
    /// Represents a knowledge entry from various sources
    /// </summary>
    public class KnowledgeEntry
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public float Relevance { get; set; }
        public string? Url { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    // Supporting DTOs for MCP responses
    internal class MCPResponse
    {
        public MCPResult? Result { get; set; }
    }

    internal class MCPResult
    {
        public List<MCPSearchResult>? Results { get; set; }
        public List<MCPCodeExample>? Examples { get; set; }
    }

    internal class MCPSearchResult
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public float Relevance { get; set; }
        public string? Url { get; set; }
    }

    internal class MCPCodeExample
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
    }
}