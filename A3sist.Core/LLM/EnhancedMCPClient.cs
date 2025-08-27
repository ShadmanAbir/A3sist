using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Core.Services;

namespace A3sist.Core.LLM
{
    /// <summary>
    /// Enhanced MCP Client with multi-server support and RAG integration
    /// Handles JavaScript, TypeScript, Python, and general requests through MCP servers
    /// </summary>
    public class EnhancedMCPClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILLMClient _llmClient;
        private readonly ILogger<EnhancedMCPClient> _logger;
        private readonly Dictionary<string, string> _serverEndpoints;
        private bool _disposed;

        public EnhancedMCPClient(
            HttpClient httpClient,
            ILLMClient llmClient,
            ILogger<EnhancedMCPClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize MCP server endpoints
            _serverEndpoints = new Dictionary<string, string>
            {
                ["core-development"] = "http://localhost:3001/mcp",
                ["git-devops"] = "http://localhost:3002/mcp",
                ["knowledge"] = "http://localhost:3003/mcp",
                ["testing-quality"] = "http://localhost:3004/mcp",
                ["vs-integration"] = "http://localhost:3005/mcp"
            };
        }

        /// <summary>
        /// Processes a request through appropriate MCP server with RAG context
        /// </summary>
        public async Task<AgentResult> ProcessAsync(AgentRequest request, RAGContext? ragContext = null)
        {
            try
            {
                _logger.LogInformation("Processing MCP request: {RequestId} with RAG: {HasRAG}", 
                    request.Id, ragContext != null);

                var serverEndpoint = SelectMCPServer(request);
                var analysisType = DetermineAnalysisType(request.Prompt);

                _logger.LogDebug("Selected MCP server: {Server}, Analysis type: {Type}", 
                    serverEndpoint, analysisType);

                var mcpRequest = BuildMCPRequest(request, ragContext, analysisType);

                var json = JsonSerializer.Serialize(mcpRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(serverEndpoint, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    
                    return AgentResult.CreateSuccess(result, new Dictionary<string, object>
                    {
                        ["MCPServer"] = serverEndpoint,
                        ["AnalysisType"] = analysisType,
                        ["RAGEnhanced"] = ragContext != null,
                        ["Language"] = DetectLanguage(request.FilePath),
                        ["ServerResponse"] = true
                    });
                }
                else
                {
                    _logger.LogWarning("MCP server request failed with status: {StatusCode}", response.StatusCode);
                    return await FallbackToLLMAsync(request, ragContext, $"MCP server returned {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "MCP request failed for server, falling back to LLM");
                return await FallbackToLLMAsync(request, ragContext, "MCP server unavailable");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MCP request: {RequestId}", request.Id);
                return AgentResult.CreateFailure($"MCP processing failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Processes request directly with LLM and optional RAG context
        /// </summary>
        public async Task<AgentResult> ProcessWithLLMAsync(AgentRequest request, RAGContext? ragContext = null)
        {
            try
            {
                _logger.LogInformation("Processing LLM request: {RequestId} with RAG: {HasRAG}", 
                    request.Id, ragContext != null);

                var prompt = ragContext != null 
                    ? BuildRAGEnhancedPrompt(request.Prompt, request.Content, ragContext)
                    : BuildSimplePrompt(request.Prompt, request.Content);

                var response = await _llmClient.GetCompletionAsync(prompt);

                return AgentResult.CreateSuccess(response.Response, new Dictionary<string, object>
                {
                    ["ProcessingType"] = "Direct LLM",
                    ["RAGEnhanced"] = ragContext != null,
                    ["ProcessingTime"] = response.ProcessingTime,
                    ["TokensUsed"] = response.TokensUsed,
                    ["Language"] = DetectLanguage(request.FilePath)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing LLM request: {RequestId}", request.Id);
                return AgentResult.CreateFailure($"LLM processing failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the health status of all MCP servers
        /// </summary>
        public async Task<Dictionary<string, ServerHealth>> GetServerHealthAsync()
        {
            var healthResults = new Dictionary<string, ServerHealth>();

            var healthTasks = _serverEndpoints.Select(async kvp =>
            {
                try
                {
                    var healthRequest = new { method = "ping" };
                    var json = JsonSerializer.Serialize(healthRequest);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(kvp.Value, content);
                    
                    return new KeyValuePair<string, ServerHealth>(kvp.Key, new ServerHealth
                    {
                        IsHealthy = response.IsSuccessStatusCode,
                        ResponseTime = TimeSpan.FromMilliseconds(100), // Would measure actual response time
                        LastChecked = DateTime.UtcNow,
                        Endpoint = kvp.Value
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Health check failed for server: {Server}", kvp.Key);
                    return new KeyValuePair<string, ServerHealth>(kvp.Key, new ServerHealth
                    {
                        IsHealthy = false,
                        Error = ex.Message,
                        LastChecked = DateTime.UtcNow,
                        Endpoint = kvp.Value
                    });
                }
            });

            var results = await Task.WhenAll(healthTasks);
            
            foreach (var result in results)
            {
                healthResults[result.Key] = result.Value;
            }

            return healthResults;
        }

        private async Task<AgentResult> FallbackToLLMAsync(AgentRequest request, RAGContext? ragContext, string reason)
        {
            _logger.LogInformation("Falling back to LLM for request: {RequestId}, Reason: {Reason}", 
                request.Id, reason);

            try
            {
                var result = await ProcessWithLLMAsync(request, ragContext);
                result.Metadata ??= new Dictionary<string, object>();
                result.Metadata["FallbackReason"] = reason;
                result.Metadata["ProcessingType"] = "Fallback LLM";
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallback LLM processing also failed for request: {RequestId}", request.Id);
                return AgentResult.CreateFailure($"All processing methods failed. Original reason: {reason}. Fallback error: {ex.Message}", ex);
            }
        }

        private string SelectMCPServer(AgentRequest request)
        {
            var language = DetectLanguage(request.FilePath);
            var promptLower = request.Prompt.ToLowerInvariant();

            // Intelligent server selection based on request context
            if (promptLower.Contains("git") || promptLower.Contains("commit") || promptLower.Contains("branch"))
                return _serverEndpoints["git-devops"];
            
            if (promptLower.Contains("test") || promptLower.Contains("quality") || promptLower.Contains("coverage"))
                return _serverEndpoints["testing-quality"];
            
            if (promptLower.Contains("documentation") || promptLower.Contains("knowledge") || promptLower.Contains("search"))
                return _serverEndpoints["knowledge"];
            
            if (promptLower.Contains("visual studio") || promptLower.Contains("vs") || promptLower.Contains("extension"))
                return _serverEndpoints["vs-integration"];

            // Default to core-development for code-related requests
            return _serverEndpoints["core-development"];
        }

        private string DetermineAnalysisType(string prompt)
        {
            var promptLower = prompt.ToLowerInvariant();

            if (promptLower.Contains("analyze") || promptLower.Contains("review"))
                return "analysis";
            if (promptLower.Contains("refactor") || promptLower.Contains("improve"))
                return "refactoring";
            if (promptLower.Contains("test") || promptLower.Contains("unit test"))
                return "testing";
            if (promptLower.Contains("debug") || promptLower.Contains("fix"))
                return "debugging";
            if (promptLower.Contains("performance") || promptLower.Contains("optimize"))
                return "performance";
            if (promptLower.Contains("security") || promptLower.Contains("vulnerability"))
                return "security";

            return "general";
        }

        private object BuildMCPRequest(AgentRequest request, RAGContext? ragContext, string analysisType)
        {
            var arguments = new Dictionary<string, object>
            {
                ["code"] = request.Content ?? "",
                ["language"] = DetectLanguage(request.FilePath),
                ["analysis_type"] = analysisType,
                ["prompt"] = request.Prompt
            };

            // Add RAG context if available
            if (ragContext != null)
            {
                arguments["context"] = new
                {
                    knowledge_entries = ragContext.KnowledgeEntries.Take(3).Select(e => new
                    {
                        title = e.Title,
                        content = e.Content.Length > 200 ? e.Content.Substring(0, 200) + "..." : e.Content,
                        relevance = e.Relevance,
                        source = e.Source
                    }),
                    language = ragContext.Language,
                    intent = ragContext.Intent,
                    project_type = ragContext.ProjectType
                };
            }

            return new
            {
                method = "tools/call",
                parameters = new
                {
                    name = "code_analysis",
                    arguments = arguments
                }
            };
        }

        private string BuildRAGEnhancedPrompt(string originalPrompt, string? code, RAGContext ragContext)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("# Enhanced Request with Knowledge Context");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("## User Request:");
            promptBuilder.AppendLine(originalPrompt);
            promptBuilder.AppendLine();

            if (!string.IsNullOrEmpty(code))
            {
                promptBuilder.AppendLine("## Code Context:");
                promptBuilder.AppendLine($"```{ragContext.Language}");
                promptBuilder.AppendLine(code);
                promptBuilder.AppendLine("```");
                promptBuilder.AppendLine();
            }

            if (ragContext.KnowledgeEntries.Any())
            {
                promptBuilder.AppendLine("## Relevant Knowledge:");
                foreach (var entry in ragContext.KnowledgeEntries.Take(3))
                {
                    promptBuilder.AppendLine($"### {entry.Title} (Relevance: {entry.Relevance:F2})");
                    var content = entry.Content.Length > 400 ? entry.Content.Substring(0, 400) + "..." : entry.Content;
                    promptBuilder.AppendLine(content);
                    if (!string.IsNullOrEmpty(entry.Url))
                    {
                        promptBuilder.AppendLine($"Source: {entry.Url}");
                    }
                    promptBuilder.AppendLine();
                }
            }

            promptBuilder.AppendLine("## Instructions:");
            promptBuilder.AppendLine("Provide a comprehensive response that:");
            promptBuilder.AppendLine("1. Addresses the user's request directly");
            promptBuilder.AppendLine("2. Incorporates relevant knowledge from the context");
            promptBuilder.AppendLine("3. Provides practical examples when applicable");
            promptBuilder.AppendLine("4. Follows current best practices");
            promptBuilder.AppendLine("5. Cites sources when using external knowledge");

            return promptBuilder.ToString();
        }

        private string BuildSimplePrompt(string originalPrompt, string? code)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("## User Request:");
            promptBuilder.AppendLine(originalPrompt);
            promptBuilder.AppendLine();

            if (!string.IsNullOrEmpty(code))
            {
                promptBuilder.AppendLine("## Code Context:");
                promptBuilder.AppendLine("```");
                promptBuilder.AppendLine(code);
                promptBuilder.AppendLine("```");
                promptBuilder.AppendLine();
            }

            promptBuilder.AppendLine("Please provide a helpful response to the user's request.");

            return promptBuilder.ToString();
        }

        private string DetectLanguage(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "general";

            var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".js" => "javascript",
                ".ts" => "typescript",
                ".py" => "python",
                ".cs" => "csharp",
                ".java" => "java",
                ".cpp" or ".cc" or ".cxx" => "cpp",
                ".c" => "c",
                ".go" => "go",
                ".rs" => "rust",
                ".php" => "php",
                ".rb" => "ruby",
                ".swift" => "swift",
                ".kt" => "kotlin",
                ".scala" => "scala",
                ".html" => "html",
                ".css" => "css",
                ".json" => "json",
                ".xml" => "xml",
                ".yaml" or ".yml" => "yaml",
                _ => "general"
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Represents the health status of an MCP server
    /// </summary>
    public class ServerHealth
    {
        public bool IsHealthy { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public DateTime LastChecked { get; set; }
        public string? Error { get; set; }
        public string Endpoint { get; set; } = string.Empty;
    }
}