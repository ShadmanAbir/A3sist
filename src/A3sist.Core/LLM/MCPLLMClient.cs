using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using A3sist.Core.Configuration;
using A3sist.Core.Services;
using A3sist.Shared.Interfaces;

namespace A3sist.Core.LLM
{
    /// <summary>
    /// MCP-based LLM client that communicates with an MCP server for AI model access
    /// </summary>
    public class MCPLLMClient : ILLMClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MCPLLMClient> _logger;
        private readonly A3sistOptions _options;
        private readonly ICacheService _cacheService;

        public MCPLLMClient(
            HttpClient httpClient,
            ILogger<MCPLLMClient> logger,
            IOptions<A3sistOptions> options,
            ICacheService cacheService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));

            ConfigureHttpClient();
        }

        /// <summary>
        /// Gets a completion from the LLM via MCP protocol
        /// </summary>
        public async Task<bool> GetCompletionAsync(object prompt, object lLMOptions)
        {
            var request = new MCPRequest
            {
                Method = "llm/completion",
                Params = new
                {
                    prompt = prompt,
                    options = lLMOptions,
                    model = _options.LLM.Model,
                    provider = _options.LLM.Provider
                }
            };

            try
            {
                var response = await SendMCPRequestAsync<MCPCompletionResponse>(request);
                return response?.Success ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get completion via MCP");
                return false;
            }
        }

        /// <summary>
        /// Gets a response from the LLM via MCP protocol with caching support
        /// </summary>
        public async Task<string> GetResponseAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));

            // Check cache first
            var cacheKey = _cacheService.GenerateKey("mcp-response", prompt, _options.LLM.Model);
            var cachedResponse = await _cacheService.GetAsync<string>(cacheKey);
            if (cachedResponse != null)
            {
                _logger.LogDebug("Cache hit for MCP prompt: {PromptPrefix}", prompt.Substring(0, Math.Min(50, prompt.Length)));
                return cachedResponse;
            }

            var request = new MCPRequest
            {
                Method = "llm/chat",
                Params = new
                {
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    model = _options.LLM.Model,
                    provider = _options.LLM.Provider,
                    max_tokens = _options.LLM.MaxTokens,
                    temperature = 0.7,
                    // MCP-specific parameters
                    tools = GetAvailableTools(),
                    context_strategy = "adaptive",
                    fallback_providers = GetFallbackProviders()
                }
            };

            try
            {
                var response = await SendMCPRequestAsync<MCPChatResponse>(request);
                
                if (response?.Success == true && !string.IsNullOrEmpty(response.Content))
                {
                    // Cache the response
                    await _cacheService.SetAsync(cacheKey, response.Content, _options.LLM.CacheExpiration);
                    
                    _logger.LogInformation("Successfully received MCP response via {Provider} model {Model}", 
                        response.ActualProvider ?? _options.LLM.Provider, 
                        response.ActualModel ?? _options.LLM.Model);
                    
                    return response.Content;
                }

                throw new InvalidOperationException($"MCP request failed: {response?.Error ?? "Unknown error"}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get response via MCP for prompt: {PromptPrefix}", 
                    prompt.Substring(0, Math.Min(100, prompt.Length)));
                throw;
            }
        }

        /// <summary>
        /// Gets available tools that can be used by the LLM via MCP
        /// </summary>
        public async Task<IEnumerable<MCPTool>> GetAvailableToolsAsync()
        {
            var request = new MCPRequest
            {
                Method = "tools/list",
                Params = new { }
            };

            try
            {
                var response = await SendMCPRequestAsync<MCPToolsResponse>(request);
                return response?.Tools ?? new List<MCPTool>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available tools via MCP");
                return new List<MCPTool>();
            }
        }

        /// <summary>
        /// Executes a tool via MCP protocol
        /// </summary>
        public async Task<string> ExecuteToolAsync(string toolName, object parameters)
        {
            var request = new MCPRequest
            {
                Method = "tools/execute",
                Params = new
                {
                    name = toolName,
                    parameters = parameters
                }
            };

            try
            {
                var response = await SendMCPRequestAsync<MCPToolExecutionResponse>(request);
                
                if (response?.Success == true)
                {
                    _logger.LogInformation("Successfully executed tool {ToolName} via MCP", toolName);
                    return response.Result ?? "";
                }

                throw new InvalidOperationException($"Tool execution failed: {response?.Error ?? "Unknown error"}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute tool {ToolName} via MCP", toolName);
                throw;
            }
        }

        /// <summary>
        /// Sends an MCP request and returns the typed response
        /// </summary>
        private async Task<T?> SendMCPRequestAsync<T>(MCPRequest request) where T : class
        {
            var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp")
            {
                Content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json")
            };

            // Add MCP-specific headers
            httpRequest.Headers.Add("MCP-Version", "1.0");
            httpRequest.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());

            var httpResponse = await _httpClient.SendAsync(httpRequest);
            httpResponse.EnsureSuccessStatusCode();

            var responseJson = await httpResponse.Content.ReadAsStringAsync();
            
            _logger.LogTrace("MCP Request: {Request}", requestJson);
            _logger.LogTrace("MCP Response: {Response}", responseJson);

            return JsonSerializer.Deserialize<T>(responseJson, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
        }

        /// <summary>
        /// Gets available tools for the current request context
        /// </summary>
        private object[] GetAvailableTools()
        {
            return new object[]
            {
                new { name = "code_analysis", description = "Analyze code for issues and improvements" },
                new { name = "documentation_search", description = "Search documentation and knowledge bases" },
                new { name = "git_operations", description = "Perform Git operations" },
                new { name = "file_operations", description = "Read and write files safely" }
            };
        }

        /// <summary>
        /// Gets fallback LLM providers for resilience
        /// </summary>
        private string[] GetFallbackProviders()
        {
            return new[] { "OpenAI", "Anthropic", "Codestral" };
        }

        /// <summary>
        /// Configures the HTTP client for MCP communication
        /// </summary>
        private void ConfigureHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_options.LLM.ApiEndpoint);
            _httpClient.Timeout = _options.LLM.RequestTimeout;
            
            // Add authentication if configured
            if (!string.IsNullOrEmpty(_options.LLM.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.LLM.ApiKey);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    #region MCP Data Models

    /// <summary>
    /// MCP request structure
    /// </summary>
    public class MCPRequest
    {
        public string Method { get; set; } = string.Empty;
        public object? Params { get; set; }
        public string Id { get; set; } = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Base MCP response
    /// </summary>
    public class MCPResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? Id { get; set; }
    }

    /// <summary>
    /// MCP completion response
    /// </summary>
    public class MCPCompletionResponse : MCPResponse
    {
        public string? Completion { get; set; }
        public string? ActualProvider { get; set; }
        public string? ActualModel { get; set; }
        public int? TokensUsed { get; set; }
    }

    /// <summary>
    /// MCP chat response
    /// </summary>
    public class MCPChatResponse : MCPResponse
    {
        public string? Content { get; set; }
        public string? ActualProvider { get; set; }
        public string? ActualModel { get; set; }
        public int? TokensUsed { get; set; }
        public string? FinishReason { get; set; }
    }

    /// <summary>
    /// MCP tool definition
    /// </summary>
    public class MCPTool
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public object? Parameters { get; set; }
        public bool RequiresAuth { get; set; }
    }

    /// <summary>
    /// MCP tools list response
    /// </summary>
    public class MCPToolsResponse : MCPResponse
    {
        public List<MCPTool> Tools { get; set; } = new();
    }

    /// <summary>
    /// MCP tool execution response
    /// </summary>
    public class MCPToolExecutionResponse : MCPResponse
    {
        public string? Result { get; set; }
        public object? Metadata { get; set; }
    }

    #endregion
}