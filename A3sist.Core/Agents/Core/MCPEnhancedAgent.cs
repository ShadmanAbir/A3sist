using A3sist.Core.Agents.Base;
using A3sist.Core.LLM;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace A3sist.Core.Agents.Core
{
    /// <summary>
    /// Enhanced agent that can use MCP tools for extended capabilities
    /// </summary>
    public class MCPEnhancedAgent : BaseAgent
    {
        private readonly MCPLLMClient _mcpClient;
        private readonly IEnumerable<MCPTool> _availableTools;

        public override string Name => "MCPEnhancedAgent";
        public override AgentType Type => AgentType.Reasoning;

        public MCPEnhancedAgent(
            MCPLLMClient mcpClient,
            ILogger<MCPEnhancedAgent> logger,
            IAgentConfiguration configuration,
            IValidationService? validationService = null,
            IPerformanceMonitoringService? performanceService = null)
            : base(logger, configuration, validationService, performanceService)
        {
            _mcpClient = mcpClient ?? throw new ArgumentNullException(nameof(mcpClient));
        }

        /// <summary>
        /// Initializes the agent and loads available MCP tools
        /// </summary>
        protected override async System.Threading.Tasks.Task InitializeAgentAsync()
        {
            try
            {
                var tools = await _mcpClient.GetAvailableToolsAsync();
                Logger.LogInformation("Loaded {ToolCount} MCP tools: {Tools}", 
                    tools.Count(), string.Join(", ", tools.Select(t => t.Name)));
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to load MCP tools, continuing without tool support");
            }
        }

        /// <summary>
        /// Handles requests with MCP tool integration
        /// </summary>
        protected override async Task<AgentResult> HandleRequestAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogInformation("Processing request with MCP enhancement: {RequestId}", request.Id);

                // Analyze request to determine if tools are needed
                var toolAnalysis = await AnalyzeToolRequirementsAsync(request);
                
                if (toolAnalysis.RequiresTools)
                {
                    Logger.LogInformation("Request requires tools: {Tools}", 
                        string.Join(", ", toolAnalysis.RequiredTools));
                    
                    return await ProcessWithToolsAsync(request, toolAnalysis.RequiredTools, cancellationToken);
                }
                else
                {
                    // Standard LLM processing without tools
                    return await ProcessWithLLMAsync(request, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing MCP-enhanced request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"MCP processing failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Analyzes the request to determine what MCP tools might be needed
        /// </summary>
        private async Task<ToolAnalysis> AnalyzeToolRequirementsAsync(AgentRequest request)
        {
            var analysis = new ToolAnalysis();
            var prompt = request.Prompt?.ToLowerInvariant() ?? "";
            var content = request.Content?.ToLowerInvariant() ?? "";

            // Analyze request content for tool requirements
            if (prompt.Contains("analyze") || prompt.Contains("review") || content.Length > 1000)
            {
                analysis.RequiredTools.Add("code_analysis");
            }

            if (prompt.Contains("documentation") || prompt.Contains("docs") || prompt.Contains("help"))
            {
                analysis.RequiredTools.Add("documentation_search");
            }

            if (prompt.Contains("git") || prompt.Contains("commit") || prompt.Contains("branch"))
            {
                analysis.RequiredTools.Add("git_operations");
            }

            if (prompt.Contains("file") || prompt.Contains("read") || prompt.Contains("write") || 
                !string.IsNullOrEmpty(request.FilePath))
            {
                analysis.RequiredTools.Add("file_operations");
            }

            // Use LLM to make more sophisticated tool selection
            if (analysis.RequiredTools.Any())
            {
                var toolSelectionPrompt = $@"
Given this coding request, determine what tools would be most helpful:
Request: {request.Prompt}
File: {request.FilePath}
Available tools: code_analysis, documentation_search, git_operations, file_operations

Respond with a JSON array of tool names that would be most useful.
";

                try
                {
                    var toolResponse = await _mcpClient.GetResponseAsync(toolSelectionPrompt);
                    // Parse response and refine tool selection
                    // Implementation would parse JSON response and update analysis
                    Logger.LogDebug("LLM tool selection response: {Response}", toolResponse);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to get LLM tool selection, using heuristic selection");
                }
            }

            analysis.RequiresTools = analysis.RequiredTools.Any();
            return analysis;
        }

        /// <summary>
        /// Processes request using MCP tools and LLM
        /// </summary>
        private async Task<AgentResult> ProcessWithToolsAsync(AgentRequest request, List<string> requiredTools, CancellationToken cancellationToken)
        {
            var toolResults = new Dictionary<string, string>();

            // Execute required tools
            foreach (var toolName in requiredTools)
            {
                try
                {
                    var toolResult = await ExecuteToolAsync(toolName, request, cancellationToken);
                    toolResults[toolName] = toolResult;
                    Logger.LogDebug("Tool {ToolName} executed successfully", toolName);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Tool {ToolName} execution failed, continuing without it", toolName);
                    toolResults[toolName] = $"Tool execution failed: {ex.Message}";
                }
            }

            // Enhance request with tool results
            var enhancedPrompt = BuildEnhancedPrompt(request, toolResults);
            
            // Process with LLM using enhanced context
            var enhancedRequest = new AgentRequest
            {
                Id = request.Id,
                Prompt = enhancedPrompt,
                FilePath = request.FilePath,
                Content = request.Content,
                Context = new Dictionary<string, object>(request.Context ?? new Dictionary<string, object>())
                {
                    ["MCPToolResults"] = toolResults,
                    ["UsedTools"] = requiredTools
                },
                PreferredAgentType = request.PreferredAgentType,
                CreatedAt = request.CreatedAt,
                UserId = request.UserId
            };

            var result = await ProcessWithLLMAsync(enhancedRequest, cancellationToken);
            
            // Add tool metadata to result
            if (result.Metadata == null)
                result.Metadata = new Dictionary<string, object>();
            
            result.Metadata["MCPToolsUsed"] = requiredTools;
            result.Metadata["ToolResults"] = toolResults;

            return result;
        }

        /// <summary>
        /// Executes a specific MCP tool with request context
        /// </summary>
        private async Task<string> ExecuteToolAsync(string toolName, AgentRequest request, CancellationToken cancellationToken)
        {
            var parameters = toolName switch
            {
                "code_analysis" => (object)new
                {
                    code = request.Content,
                    language = DetectLanguage(request.FilePath),
                    analysis_type = "comprehensive"
                },
                "documentation_search" => (object)new
                {
                    query = ExtractKeywords(request.Prompt),
                    context = "programming",
                    max_results = 5
                },
                "git_operations" => (object)new
                {
                    operation = "status",
                    repository_path = GetRepositoryPath(request.FilePath)
                },
                "file_operations" => (object)new
                {
                    operation = "read",
                    file_path = request.FilePath,
                    encoding = "utf-8"
                },
                _ => throw new NotSupportedException($"Tool {toolName} is not supported")
            };

            return await _mcpClient.ExecuteToolAsync(toolName, parameters);
        }

        /// <summary>
        /// Processes request using standard LLM without tools
        /// </summary>
        private async Task<AgentResult> ProcessWithLLMAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            var response = await _mcpClient.GetResponseAsync(request.Prompt);
            
            return AgentResult.CreateSuccess("Request processed successfully", response);
        }

        /// <summary>
        /// Builds an enhanced prompt that includes tool results
        /// </summary>
        private string BuildEnhancedPrompt(AgentRequest request, Dictionary<string, string> toolResults)
        {
            var promptBuilder = new System.Text.StringBuilder();
            
            promptBuilder.AppendLine("# Enhanced Request with Tool Results");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("## Original Request:");
            promptBuilder.AppendLine(request.Prompt);
            promptBuilder.AppendLine();
            
            if (toolResults.Any())
            {
                promptBuilder.AppendLine("## Tool Analysis Results:");
                foreach (var toolResult in toolResults)
                {
                    promptBuilder.AppendLine($"### {toolResult.Key}:");
                    promptBuilder.AppendLine(toolResult.Value);
                    promptBuilder.AppendLine();
                }
            }
            
            promptBuilder.AppendLine("## Instructions:");
            promptBuilder.AppendLine("Please provide a comprehensive response considering both the original request and the tool analysis results above.");
            
            return promptBuilder.ToString();
        }

        /// <summary>
        /// Detects programming language from file path
        /// </summary>
        private string DetectLanguage(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "unknown";

            var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".cs" => "csharp",
                ".js" => "javascript",
                ".ts" => "typescript",
                ".py" => "python",
                ".java" => "java",
                ".cpp" or ".cc" => "cpp",
                _ => "unknown"
            };
        }

        /// <summary>
        /// Extracts keywords from prompt for documentation search
        /// </summary>
        private string ExtractKeywords(string prompt)
        {
            // Simple keyword extraction - could be enhanced with NLP
            var keywords = prompt.Split(' ', (char)StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length > 3)
                .Take(5);
            
            return string.Join(" ", keywords);
        }

        /// <summary>
        /// Gets repository path from file path
        /// </summary>
        private string GetRepositoryPath(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return Environment.CurrentDirectory;

            var directory = System.IO.Path.GetDirectoryName(filePath);
            // Logic to find Git repository root would go here
            return directory ?? Environment.CurrentDirectory;
        }

        /// <summary>
        /// Tool analysis result
        /// </summary>
        private class ToolAnalysis
        {
            public bool RequiresTools { get; set; }
            public List<string> RequiredTools { get; set; } = new();
        }
    }
}