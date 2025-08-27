using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using A3sist.Core.Configuration;
using A3sist.Shared.Messaging;

namespace A3sist.Core.LLM
{
    /// <summary>
    /// Orchestrates multiple MCP servers and coordinates their interactions
    /// </summary>
    public class MCPOrchestrator : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MCPOrchestrator> _logger;
        private readonly A3sistOptions _options;
        private readonly Dictionary<string, MCPServerInfo> _mcpServers;
        private readonly SemaphoreSlim _orchestrationLock;

        public MCPOrchestrator(
            HttpClient httpClient,
            ILogger<MCPOrchestrator> logger,
            IOptions<A3sistOptions> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _mcpServers = new Dictionary<string, MCPServerInfo>();
            _orchestrationLock = new SemaphoreSlim(1, 1);

            InitializeMCPServers();
        }

        /// <summary>
        /// Initializes all MCP servers based on configuration
        /// </summary>
        private void InitializeMCPServers()
        {
            var mcpConfig = _options.LLM.MCP;
            if (!mcpConfig.Enabled)
            {
                _logger.LogInformation("MCP is disabled in configuration");
                return;
            }

            // Register MCP servers from configuration
            if (mcpConfig.Servers != null)
            {
                foreach (var serverConfig in mcpConfig.Servers)
                {
                    RegisterMCPServer(serverConfig.Name, serverConfig.Endpoint, serverConfig.Tools.ToArray());
                }
            }
            else
            {
                // Fallback to default configuration if none provided
                RegisterMCPServer("core-development", "http://localhost:3001", 
                    new[] { "code_analysis", "code_refactor", "code_validation", "language_conversion" });
                
                RegisterMCPServer("vs-integration", "http://localhost:3002", 
                    new[] { "project_analysis", "solution_management", "nuget_operations", "msbuild_operations", "extension_integration" });
                
                RegisterMCPServer("knowledge", "http://localhost:3003", 
                    new[] { "documentation_search", "best_practices", "code_examples", "knowledge_update" });
                
                RegisterMCPServer("git-devops", "http://localhost:3004", 
                    new[] { "git_operations", "ci_cd_integration", "deployment_analysis" });
                
                RegisterMCPServer("testing-quality", "http://localhost:3005", 
                    new[] { "test_generation", "quality_metrics", "performance_analysis" });
            }

            _logger.LogInformation("Initialized {ServerCount} MCP servers", _mcpServers.Count);
        }

        /// <summary>
        /// Registers an MCP server with the orchestrator
        /// </summary>
        private void RegisterMCPServer(string name, string endpoint, string[] tools)
        {
            _mcpServers[name] = new MCPServerInfo
            {
                Name = name,
                Endpoint = endpoint,
                Tools = tools.ToList(),
                IsHealthy = true,
                LastHealthCheck = DateTime.UtcNow
            };

            _logger.LogDebug("Registered MCP server: {ServerName} at {Endpoint} with tools: {Tools}",
                name, endpoint, string.Join(", ", tools));
        }

        /// <summary>
        /// Processes a request by intelligently routing to appropriate MCP servers
        /// </summary>
        public async Task<MCPOrchestratedResult> ProcessRequestAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            await _orchestrationLock.WaitAsync(cancellationToken);

            try
            {
                _logger.LogInformation("Processing orchestrated request: {RequestId}", request.Id);

                // Analyze request to determine required tools and servers
                var toolAnalysis = await AnalyzeRequiredToolsAsync(request);
                
                // Execute tools across multiple servers
                var toolResults = await ExecuteDistributedToolsAsync(toolAnalysis.RequiredTools, request, cancellationToken);
                
                // Synthesize results from multiple servers
                var synthesizedResult = await SynthesizeResultsAsync(toolResults, request);

                _logger.LogInformation("Orchestrated processing completed for request: {RequestId} using {ServerCount} servers",
                    request.Id, toolResults.Keys.Count);

                return new MCPOrchestratedResult
                {
                    Success = synthesizedResult.Success,
                    Content = synthesizedResult.Content,
                    ToolResults = toolResults,
                    ServersUsed = toolResults.Keys.ToList(),
                    ProcessingTimeMs = toolResults.Values.Sum(r => r.ProcessingTimeMs)
                };
            }
            finally
            {
                _orchestrationLock.Release();
            }
        }

        /// <summary>
        /// Analyzes the request to determine which tools and servers are needed
        /// </summary>
        private async Task<MCPToolAnalysis> AnalyzeRequiredToolsAsync(AgentRequest request)
        {
            var analysis = new MCPToolAnalysis();
            var prompt = request.Prompt?.ToLowerInvariant() ?? "";
            var content = request.Content ?? "";

            // Core Development Server tools
            if (prompt.Contains("analyze") || prompt.Contains("refactor") || prompt.Contains("validate") || content.Length > 100)
            {
                analysis.RequiredTools.AddRange(new[] { "core-development:code_analysis" });
                
                if (prompt.Contains("refactor"))
                    analysis.RequiredTools.Add("core-development:code_refactor");
                
                if (prompt.Contains("validate") || prompt.Contains("compile"))
                    analysis.RequiredTools.Add("core-development:code_validation");
            }

            // VS Integration Server tools
            if (!string.IsNullOrEmpty(request.FilePath) || prompt.Contains("project") || prompt.Contains("solution"))
            {
                analysis.RequiredTools.Add("vs-integration:project_analysis");
                
                if (prompt.Contains("build") || prompt.Contains("compile"))
                    analysis.RequiredTools.Add("vs-integration:msbuild_operations");
                
                if (prompt.Contains("nuget") || prompt.Contains("package"))
                    analysis.RequiredTools.Add("vs-integration:nuget_operations");
            }

            // Knowledge Server tools
            if (prompt.Contains("documentation") || prompt.Contains("help") || prompt.Contains("best practice"))
            {
                analysis.RequiredTools.Add("knowledge:documentation_search");
                
                if (prompt.Contains("best practice") || prompt.Contains("pattern"))
                    analysis.RequiredTools.Add("knowledge:best_practices");
                
                if (prompt.Contains("example"))
                    analysis.RequiredTools.Add("knowledge:code_examples");
            }

            // Git & DevOps Server tools
            if (prompt.Contains("git") || prompt.Contains("commit") || prompt.Contains("deploy"))
            {
                analysis.RequiredTools.Add("git-devops:git_operations");
                
                if (prompt.Contains("deploy") || prompt.Contains("ci") || prompt.Contains("cd"))
                    analysis.RequiredTools.Add("git-devops:deployment_analysis");
            }

            // Testing & Quality Server tools
            if (prompt.Contains("test") || prompt.Contains("quality") || prompt.Contains("performance"))
            {
                if (prompt.Contains("test") || prompt.Contains("unit test"))
                    analysis.RequiredTools.Add("testing-quality:test_generation");
                
                if (prompt.Contains("quality") || prompt.Contains("metric"))
                    analysis.RequiredTools.Add("testing-quality:quality_metrics");
                
                if (prompt.Contains("performance"))
                    analysis.RequiredTools.Add("testing-quality:performance_analysis");
            }

            _logger.LogDebug("Tool analysis completed for request {RequestId}: {ToolCount} tools required",
                request.Id, analysis.RequiredTools.Count);

            return analysis;
        }

        /// <summary>
        /// Executes tools across multiple MCP servers
        /// </summary>
        private async Task<Dictionary<string, MCPToolExecutionResult>> ExecuteDistributedToolsAsync(
            List<string> requiredTools, AgentRequest request, CancellationToken cancellationToken)
        {
            var results = new Dictionary<string, MCPToolExecutionResult>();
            var tasks = new List<Task<(string ServerName, MCPToolExecutionResult Result)>>();

            // Group tools by server
            var toolsByServer = requiredTools
                .GroupBy(tool => tool.Split(':')[0])
                .ToDictionary(g => g.Key, g => g.Select(tool => tool.Split(':')[1]).ToList());

            foreach (var serverTools in toolsByServer)
            {
                var serverName = serverTools.Key;
                var tools = serverTools.Value;

                if (_mcpServers.TryGetValue(serverName, out var serverInfo))
                {
                    var task = ExecuteServerToolsAsync(serverInfo, tools, request, cancellationToken);
                    tasks.Add(task);
                }
            }

            var executionResults = await Task.WhenAll(tasks);

            foreach (var (serverName, result) in executionResults)
            {
                results[serverName] = result;
            }

            return results;
        }

        /// <summary>
        /// Executes tools on a specific MCP server
        /// </summary>
        private async Task<(string ServerName, MCPToolExecutionResult Result)> ExecuteServerToolsAsync(
            MCPServerInfo serverInfo, List<string> tools, AgentRequest request, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            var result = new MCPToolExecutionResult
            {
                ServerName = serverInfo.Name,
                ToolsExecuted = tools,
                Success = true,
                Results = new Dictionary<string, object>()
            };

            try
            {
                foreach (var tool in tools)
                {
                    var toolResult = await ExecuteSingleToolAsync(serverInfo, tool, request, cancellationToken);
                    result.Results[tool] = toolResult;
                }

                result.ProcessingTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                _logger.LogDebug("Successfully executed {ToolCount} tools on server {ServerName}",
                    tools.Count, serverInfo.Name);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                
                _logger.LogError(ex, "Failed to execute tools on server {ServerName}", serverInfo.Name);
            }

            return (serverInfo.Name, result);
        }

        /// <summary>
        /// Executes a single tool on an MCP server
        /// </summary>
        private async Task<object> ExecuteSingleToolAsync(MCPServerInfo serverInfo, string toolName, 
            AgentRequest request, CancellationToken cancellationToken)
        {
            var mcpRequest = new
            {
                method = "tools/execute",
                @params = new
                {
                    name = toolName,
                    parameters = CreateToolParameters(toolName, request)
                }
            };

            var requestJson = JsonSerializer.Serialize(mcpRequest);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{serverInfo.Endpoint}/mcp")
            {
                Content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json")
            };

            var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            httpResponse.EnsureSuccessStatusCode();

            var responseJson = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            var response = JsonSerializer.Deserialize<JsonElement>(responseJson);

            return response.GetProperty("result").GetString() ?? "";
        }

        /// <summary>
        /// Creates appropriate parameters for a tool based on the request
        /// </summary>
        private object CreateToolParameters(string toolName, AgentRequest request)
        {
            return toolName switch
            {
                "code_analysis" => new { code = request.Content, language = DetectLanguage(request), analysisLevel = "full" },
                "code_refactor" => new { code = request.Content, language = DetectLanguage(request), refactorType = "optimize" },
                "project_analysis" => new { projectPath = request.FilePath, analysisType = "full" },
                "documentation_search" => new { query = ExtractSearchQuery(request.Prompt), scope = "general" },
                "git_operations" => new { operation = "status", repositoryPath = GetRepositoryPath(request.FilePath) },
                "test_generation" => new { code = request.Content, language = DetectLanguage(request), testFramework = "xunit" },
                _ => new { }
            };
        }

        /// <summary>
        /// Synthesizes results from multiple servers into a coherent response
        /// </summary>
        private async Task<AgentResult> SynthesizeResultsAsync(Dictionary<string, MCPToolExecutionResult> toolResults, AgentRequest request)
        {
            var synthesizedContent = new List<string>();
            var allSuccessful = toolResults.Values.All(r => r.Success);

            foreach (var serverResult in toolResults.Values)
            {
                if (serverResult.Success)
                {
                    synthesizedContent.Add($"## Results from {serverResult.ServerName}:");
                    
                    foreach (var toolResult in serverResult.Results)
                    {
                        synthesizedContent.Add($"### {toolResult.Key}:");
                        synthesizedContent.Add(toolResult.Value?.ToString() ?? "No result");
                    }
                }
                else
                {
                    synthesizedContent.Add($"## Error from {serverResult.ServerName}: {serverResult.Error}");
                }
            }

            var finalContent = string.Join("\n\n", synthesizedContent);

            return allSuccessful 
                ? AgentResult.CreateSuccess("Orchestrated MCP processing completed successfully", finalContent)
                : AgentResult.CreateFailure("Some MCP servers encountered errors", null, "MCPOrchestrator");
        }

        /// <summary>
        /// Performs health checks on all MCP servers
        /// </summary>
        public async Task<Dictionary<string, bool>> PerformHealthChecksAsync()
        {
            var healthResults = new Dictionary<string, bool>();

            foreach (var server in _mcpServers.Values)
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, $"{server.Endpoint}/mcp")
                    {
                        Content = new StringContent(JsonSerializer.Serialize(new { method = "tools/list" }), 
                            System.Text.Encoding.UTF8, "application/json")
                    };

                    var response = await _httpClient.SendAsync(request);
                    server.IsHealthy = response.IsSuccessStatusCode;
                    server.LastHealthCheck = DateTime.UtcNow;
                    
                    healthResults[server.Name] = server.IsHealthy;
                }
                catch (Exception ex)
                {
                    server.IsHealthy = false;
                    server.LastHealthCheck = DateTime.UtcNow;
                    healthResults[server.Name] = false;
                    
                    _logger.LogWarning(ex, "Health check failed for MCP server: {ServerName}", server.Name);
                }
            }

            return healthResults;
        }

        private string DetectLanguage(AgentRequest request)
        {
            if (!string.IsNullOrEmpty(request.FilePath))
            {
                var extension = System.IO.Path.GetExtension(request.FilePath).ToLowerInvariant();
                return extension switch
                {
                    ".cs" => "csharp",
                    ".js" => "javascript",
                    ".ts" => "typescript",
                    ".py" => "python",
                    _ => "unknown"
                };
            }
            return "unknown";
        }

        private string ExtractSearchQuery(string? prompt)
        {
            if (string.IsNullOrEmpty(prompt)) return "";
            
            // Simple extraction - could be enhanced with NLP
            var words = prompt.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", words.Take(5));
        }

        private string GetRepositoryPath(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return Environment.CurrentDirectory;
            
            var directory = System.IO.Path.GetDirectoryName(filePath);
            return directory ?? Environment.CurrentDirectory;
        }

        public void Dispose()
        {
            _orchestrationLock?.Dispose();
        }
    }

    #region Supporting Classes

    public class MCPServerInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public List<string> Tools { get; set; } = new();
        public bool IsHealthy { get; set; }
        public DateTime LastHealthCheck { get; set; }
    }

    public class MCPToolAnalysis
    {
        public List<string> RequiredTools { get; set; } = new();
    }

    public class MCPToolExecutionResult
    {
        public string ServerName { get; set; } = string.Empty;
        public List<string> ToolsExecuted { get; set; } = new();
        public bool Success { get; set; }
        public string? Error { get; set; }
        public Dictionary<string, object> Results { get; set; } = new();
        public double ProcessingTimeMs { get; set; }
    }

    public class MCPOrchestratedResult
    {
        public bool Success { get; set; }
        public string? Content { get; set; }
        public Dictionary<string, MCPToolExecutionResult> ToolResults { get; set; } = new();
        public List<string> ServersUsed { get; set; } = new();
        public double ProcessingTimeMs { get; set; }
    }

    #endregion
}