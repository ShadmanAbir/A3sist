using A3sist.Core.Agents.Core;
using A3sist.Core.LLM;
using A3sist.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace A3sist.Examples
{
    /// <summary>
    /// Practical example showing how to use MCP integration in Visual Studio extension
    /// </summary>
    public class MCPUsageExample
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MCPUsageExample> _logger;

        public MCPUsageExample(IServiceProvider serviceProvider, ILogger<MCPUsageExample> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Example 1: Code Analysis with MCP Tools
        /// </summary>
        public async Task<string> AnalyzeCodeWithMCPAsync(string sourceCode, string filePath)
        {
            try
            {
                // Get the MCP-enhanced agent
                var mcpAgent = _serviceProvider.GetRequiredService<MCPEnhancedAgent>();

                // Create a request for code analysis
                var request = new AgentRequest
                {
                    Prompt = @"Analyze this C# code and provide:
                    1. Potential bugs or issues
                    2. Performance improvement suggestions  
                    3. Code quality recommendations
                    4. Best practice violations
                    5. Refactoring opportunities",
                    FilePath = filePath,
                    Content = sourceCode,
                    Context = new Dictionary<string, object>
                    {
                        ["AnalysisType"] = "Comprehensive",
                        ["IncludePerformance"] = true,
                        ["IncludeSecurity"] = true
                    }
                };

                // Process with MCP tools - agent will automatically:
                // 1. Detect this needs code_analysis tool
                // 2. Execute the tool with the code
                // 3. Enhance the prompt with analysis results
                // 4. Generate comprehensive suggestions
                var result = await mcpAgent.HandleAsync(request);

                if (result.Success)
                {
                    _logger.LogInformation("MCP code analysis completed successfully");
                    return result.Content;
                }
                else
                {
                    _logger.LogWarning("MCP code analysis failed: {Error}", result.Message);
                    return $"Analysis failed: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during MCP code analysis");
                throw;
            }
        }

        /// <summary>
        /// Example 2: Intelligent Documentation Search
        /// </summary>
        public async Task<string> SearchDocumentationAsync(string query, string context = "")
        {
            try
            {
                var mcpAgent = _serviceProvider.GetRequiredService<MCPEnhancedAgent>();

                var request = new AgentRequest
                {
                    Prompt = $@"I need help with: {query}
                    
                    Context: {context}
                    
                    Please search relevant documentation and provide:
                    1. Direct answers to my question
                    2. Code examples if applicable
                    3. Best practices
                    4. Related topics I should know about",
                    Context = new Dictionary<string, object>
                    {
                        ["SearchScope"] = "Documentation",
                        ["IncludeExamples"] = true
                    }
                };

                // Agent will use documentation_search tool automatically
                var result = await mcpAgent.HandleAsync(request);
                
                return result.Success ? result.Content : $"Search failed: {result.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during documentation search");
                throw;
            }
        }

        /// <summary>
        /// Example 3: Git Operations Integration
        /// </summary>
        public async Task<string> AnalyzeGitChangesAsync(string repositoryPath)
        {
            try
            {
                var mcpAgent = _serviceProvider.GetRequiredService<MCPEnhancedAgent>();

                var request = new AgentRequest
                {
                    Prompt = @"Analyze the current Git changes and provide:
                    1. Summary of changes made
                    2. Potential impact analysis
                    3. Suggestions for commit message
                    4. Code review points to consider
                    5. Any breaking changes detected",
                    FilePath = repositoryPath,
                    Context = new Dictionary<string, object>
                    {
                        ["GitOperation"] = "AnalyzeChanges",
                        ["IncludeHistory"] = true
                    }
                };

                // Agent will use git_operations tool
                var result = await mcpAgent.HandleAsync(request);
                
                return result.Success ? result.Content : $"Git analysis failed: {result.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Git analysis");
                throw;
            }
        }

        /// <summary>
        /// Example 4: Direct MCP Tool Usage
        /// </summary>
        public async Task<string> ExecuteSpecificToolAsync()
        {
            try
            {
                // Get the MCP client directly for specific tool usage
                var mcpClient = _serviceProvider.GetRequiredService<MCPLLMClient>();

                // Execute a specific tool with custom parameters
                var toolResult = await mcpClient.ExecuteToolAsync("code_analysis", new
                {
                    code = "public class Example { public void Method() { } }",
                    language = "csharp",
                    analysisLevel = "detailed",
                    checkSecurity = true,
                    checkPerformance = true
                });

                _logger.LogInformation("Direct tool execution completed");
                return toolResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during direct tool execution");
                throw;
            }
        }

        /// <summary>
        /// Example 5: Multi-Provider Failover Demo
        /// </summary>
        public async Task<string> DemoProviderFailoverAsync(string prompt)
        {
            try
            {
                var mcpClient = _serviceProvider.GetRequiredService<MCPLLMClient>();

                // This will automatically try fallback providers if primary fails
                var response = await mcpClient.GetResponseAsync(prompt);
                
                _logger.LogInformation("Response received with potential provider failover");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "All providers failed");
                throw;
            }
        }

        /// <summary>
        /// Example 6: Available Tools Discovery
        /// </summary>
        public async Task<List<string>> DiscoverAvailableToolsAsync()
        {
            try
            {
                var mcpClient = _serviceProvider.GetRequiredService<MCPLLMClient>();
                
                var tools = await mcpClient.GetAvailableToolsAsync();
                var toolNames = new List<string>();

                foreach (var tool in tools)
                {
                    toolNames.Add($"{tool.Name}: {tool.Description}");
                    _logger.LogInformation("Available tool: {ToolName} - {Description}", 
                        tool.Name, tool.Description);
                }

                return toolNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discovering tools");
                throw;
            }
        }

        /// <summary>
        /// Example 7: Complex Workflow with Multiple Tools
        /// </summary>
        public async Task<string> ComplexWorkflowExampleAsync(string projectPath, string className)
        {
            try
            {
                var mcpAgent = _serviceProvider.GetRequiredService<MCPEnhancedAgent>();

                var request = new AgentRequest
                {
                    Prompt = $@"I need comprehensive help with the class '{className}' in my project:
                    
                    1. Analyze the current implementation for issues
                    2. Search documentation for best practices related to this type of class
                    3. Check Git history to understand recent changes
                    4. Suggest improvements and generate updated documentation
                    5. Provide unit test suggestions
                    
                    Please provide a complete analysis and actionable recommendations.",
                    FilePath = projectPath,
                    Context = new Dictionary<string, object>
                    {
                        ["ClassName"] = className,
                        ["WorkflowType"] = "Comprehensive",
                        ["RequireMultipleTools"] = true
                    }
                };

                // Agent will intelligently use multiple tools:
                // - file_operations to read the class
                // - code_analysis to analyze it
                // - documentation_search for best practices
                // - git_operations for history
                var result = await mcpAgent.HandleAsync(request);

                if (result.Success && result.Metadata?.ContainsKey("MCPToolsUsed") == true)
                {
                    var toolsUsed = result.Metadata["MCPToolsUsed"];
                    _logger.LogInformation("Complex workflow completed using tools: {Tools}", toolsUsed);
                }

                return result.Success ? result.Content : $"Workflow failed: {result.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during complex workflow");
                throw;
            }
        }
    }

    /// <summary>
    /// Visual Studio Integration Example
    /// Shows how to integrate MCP with VS extension commands
    /// </summary>
    public class VSIntegrationExample
    {
        /// <summary>
        /// Example VS command that uses MCP for code assistance
        /// </summary>
        public static async Task AnalyzeCurrentFile()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                // Get current document (pseudo-code - actual implementation varies)
                var dte = Package.GetGlobalService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE)) as EnvDTE.DTE;
                var activeDocument = dte?.ActiveDocument;
                
                if (activeDocument?.Selection is EnvDTE.TextSelection selection)
                {
                    var selectedText = selection.Text;
                    var filePath = activeDocument.FullName;

                    // Get A3sist service (configured in package initialization)
                    var serviceProvider = /* your service provider */;
                    var mcpExample = new MCPUsageExample(serviceProvider, 
                        serviceProvider.GetRequiredService<ILogger<MCPUsageExample>>());

                    // Analyze with MCP
                    var analysis = await mcpExample.AnalyzeCodeWithMCPAsync(selectedText, filePath);

                    // Display results in VS output window or tool window
                    ShowResults(analysis);
                }
            }
            catch (Exception ex)
            {
                // Handle error appropriately
                System.Diagnostics.Debug.WriteLine($"MCP Analysis error: {ex}");
            }
        }

        private static void ShowResults(string results)
        {
            // Implementation to show results in VS UI
            // Could be output window, tool window, or notification
        }
    }
}