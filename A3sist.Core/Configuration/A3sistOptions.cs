using System;
using System.ComponentModel.DataAnnotations;

namespace A3sist.Core.Configuration
{
    /// <summary>
    /// Strongly-typed configuration options for A3sist
    /// </summary>
    public class A3sistOptions
    {
        public const string SectionName = "A3sist";

        /// <summary>
        /// Agent configuration settings
        /// </summary>
        public AgentOptions Agents { get; set; } = new();

        /// <summary>
        /// LLM provider configuration
        /// </summary>
        public LLMOptions LLM { get; set; } = new();

        /// <summary>
        /// Logging configuration
        /// </summary>
        public LoggingOptions Logging { get; set; } = new();

        /// <summary>
        /// Performance and monitoring settings
        /// </summary>
        public PerformanceOptions Performance { get; set; } = new();
    }

    public class AgentOptions
    {
        /// <summary>
        /// Default timeout for agent operations
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Maximum number of concurrent agents
        /// </summary>
        [Range(1, 50)]
        public int MaxConcurrentAgents { get; set; } = 10;

        /// <summary>
        /// Health check interval for agents
        /// </summary>
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Circuit breaker failure threshold
        /// </summary>
        [Range(1, 20)]
        public int CircuitBreakerThreshold { get; set; } = 5;
    }

    public class LLMOptions
    {
        /// <summary>
        /// LLM provider (OpenAI, Codestral, etc.)
        /// </summary>
        [Required]
        public string Provider { get; set; } = "OpenAI";

        /// <summary>
        /// Model to use for LLM requests
        /// </summary>
        [Required]
        public string Model { get; set; } = "gpt-4";

        /// <summary>
        /// Maximum tokens per request
        /// </summary>
        [Range(100, 32000)]
        public int MaxTokens { get; set; } = 4000;

        /// <summary>
        /// API endpoint URL
        /// </summary>
        [Required]
        [Url]
        public string ApiEndpoint { get; set; } = "https://api.openai.com/v1";

        /// <summary>
        /// API key for authentication
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Request timeout for LLM calls
        /// </summary>
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Enable response caching
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Cache expiration time
        /// </summary>
        public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// MCP (Model Context Protocol) configuration
        /// </summary>
        public MCPOptions MCP { get; set; } = new();
    }

    public class LoggingOptions
    {
        /// <summary>
        /// Minimum log level
        /// </summary>
        public string Level { get; set; } = "Information";

        /// <summary>
        /// Log output path
        /// </summary>
        public string OutputPath { get; set; } = "%TEMP%/A3sist/logs";

        /// <summary>
        /// Enable file logging
        /// </summary>
        public bool EnableFileLogging { get; set; } = true;

        /// <summary>
        /// Enable console logging
        /// </summary>
        public bool EnableConsoleLogging { get; set; } = true;

        /// <summary>
        /// Enable structured logging
        /// </summary>
        public bool EnableStructuredLogging { get; set; } = true;
    }

    public class PerformanceOptions
    {
        /// <summary>
        /// Enable performance monitoring
        /// </summary>
        public bool EnableMonitoring { get; set; } = true;

        /// <summary>
        /// Performance metrics collection interval
        /// </summary>
        public TimeSpan MetricsInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Maximum memory usage threshold (MB)
        /// </summary>
        [Range(100, 4096)]
        public int MaxMemoryUsageMB { get; set; } = 1024;

        /// <summary>
        /// Enable automatic garbage collection
        /// </summary>
        public bool EnableAutoGC { get; set; } = false;
    }

    /// <summary>
    /// MCP (Model Context Protocol) configuration options for A3sist ecosystem
    /// </summary>
    public class MCPOptions
    {
        /// <summary>
        /// Enable MCP protocol support
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// MCP protocol version
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Enable tool execution via MCP
        /// </summary>
        public bool EnableTools { get; set; } = true;

        /// <summary>
        /// Maximum number of tool executions per request
        /// </summary>
        [Range(1, 20)]
        public int MaxToolExecutions { get; set; } = 10;

        /// <summary>
        /// Tool execution timeout
        /// </summary>
        public TimeSpan ToolTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Fallback providers when MCP is unavailable
        /// </summary>
        public string[] FallbackProviders { get; set; } = { "OpenAI", "Anthropic", "Codestral" };

        /// <summary>
        /// Enable automatic provider failover
        /// </summary>
        public bool EnableFailover { get; set; } = true;

        /// <summary>
        /// Enable orchestrated multi-server processing
        /// </summary>
        public bool EnableOrchestration { get; set; } = true;

        /// <summary>
        /// Health check interval for MCP servers
        /// </summary>
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Connection retry policy
        /// </summary>
        public MCPRetryPolicy RetryPolicy { get; set; } = new();

        /// <summary>
        /// A3sist MCP servers configuration
        /// </summary>
        public MCPServersOptions Servers { get; set; } = new();
    }

    /// <summary>
    /// MCP retry policy configuration
    /// </summary>
    public class MCPRetryPolicy
    {
        /// <summary>
        /// Maximum retry attempts
        /// </summary>
        [Range(0, 10)]
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Initial retry delay
        /// </summary>
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Maximum retry delay
        /// </summary>
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Use exponential backoff
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;
    }

    /// <summary>
    /// Configuration for all A3sist MCP servers
    /// </summary>
    public class MCPServersOptions
    {
        /// <summary>
        /// Core Development MCP Server configuration
        /// </summary>
        public MCPServerConfig CoreDevelopment { get; set; } = new()
        {
            Enabled = true,
            Endpoint = "http://localhost:3001",
            Tools = new[] { "code_analysis", "code_refactor", "code_validation", "language_conversion" }
        };

        /// <summary>
        /// Visual Studio Integration MCP Server configuration
        /// </summary>
        public MCPServerConfig VSIntegration { get; set; } = new()
        {
            Enabled = true,
            Endpoint = "http://localhost:3002",
            Tools = new[] { "project_analysis", "solution_management", "nuget_operations", "msbuild_operations", "extension_integration" }
        };

        /// <summary>
        /// Knowledge & Documentation MCP Server configuration
        /// </summary>
        public MCPServerConfig Knowledge { get; set; } = new()
        {
            Enabled = true,
            Endpoint = "http://localhost:3003",
            Tools = new[] { "documentation_search", "best_practices", "code_examples", "knowledge_update" }
        };

        /// <summary>
        /// Git & DevOps MCP Server configuration
        /// </summary>
        public MCPServerConfig GitDevOps { get; set; } = new()
        {
            Enabled = true,
            Endpoint = "http://localhost:3004",
            Tools = new[] { "git_operations", "ci_cd_integration", "deployment_analysis" }
        };

        /// <summary>
        /// Testing & Quality Assurance MCP Server configuration
        /// </summary>
        public MCPServerConfig TestingQuality { get; set; } = new()
        {
            Enabled = true,
            Endpoint = "http://localhost:3005",
            Tools = new[] { "test_generation", "quality_metrics", "performance_analysis" }
        };
    }

    /// <summary>
    /// Individual MCP server configuration
    /// </summary>
    public class MCPServerConfig
    {
        /// <summary>
        /// Enable this MCP server
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Server endpoint URL
        /// </summary>
        [Url]
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Available tools on this server
        /// </summary>
        public string[] Tools { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Server-specific timeout
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Priority for tool execution (higher = preferred)
        /// </summary>
        [Range(1, 10)]
        public int Priority { get; set; } = 5;

        /// <summary>
        /// Health check enabled for this server
        /// </summary>
        public bool EnableHealthCheck { get; set; } = true;
    }
}