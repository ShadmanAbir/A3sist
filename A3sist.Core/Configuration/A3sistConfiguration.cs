using System.ComponentModel.DataAnnotations;

namespace A3sist.Core.Configuration;

/// <summary>
/// Main configuration class for A3sist application
/// </summary>
public class A3sistConfiguration
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "A3sist";

    /// <summary>
    /// Agent configuration settings
    /// </summary>
    public AgentsConfiguration Agents { get; set; } = new();

    /// <summary>
    /// LLM configuration settings
    /// </summary>
    public LLMConfiguration LLM { get; set; } = new();

    /// <summary>
    /// Logging configuration settings
    /// </summary>
    public LoggingConfiguration Logging { get; set; } = new();
}

/// <summary>
/// Configuration for agents
/// </summary>
public class AgentsConfiguration
{
    /// <summary>
    /// Orchestrator agent configuration
    /// </summary>
    public AgentConfiguration Orchestrator { get; set; } = new()
    {
        Enabled = true,
        MaxConcurrentTasks = 5,
        Timeout = TimeSpan.FromMinutes(5)
    };

    /// <summary>
    /// C# agent configuration
    /// </summary>
    public CSharpAgentConfiguration CSharpAgent { get; set; } = new()
    {
        Enabled = true,
        AnalysisLevel = "Full"
    };
}

/// <summary>
/// Base agent configuration
/// </summary>
public class AgentConfiguration
{
    /// <summary>
    /// Whether the agent is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of concurrent tasks
    /// </summary>
    [Range(1, 100)]
    public int MaxConcurrentTasks { get; set; } = 3;

    /// <summary>
    /// Agent timeout
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Retry policy configuration
    /// </summary>
    public RetryPolicyConfiguration RetryPolicy { get; set; } = new();
}

/// <summary>
/// C# agent specific configuration
/// </summary>
public class CSharpAgentConfiguration : AgentConfiguration
{
    /// <summary>
    /// Analysis level (Basic, Full, Deep)
    /// </summary>
    public string AnalysisLevel { get; set; } = "Full";
}

/// <summary>
/// Retry policy configuration
/// </summary>
public class RetryPolicyConfiguration
{
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Base delay between retries
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum delay between retries
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// LLM configuration
/// </summary>
public class LLMConfiguration
{
    /// <summary>
    /// LLM provider (OpenAI, Azure, etc.)
    /// </summary>
    public string Provider { get; set; } = "OpenAI";

    /// <summary>
    /// Model name
    /// </summary>
    public string Model { get; set; } = "gpt-4";

    /// <summary>
    /// Maximum tokens
    /// </summary>
    [Range(1, 32000)]
    public int MaxTokens { get; set; } = 4000;

    /// <summary>
    /// API key (should be stored securely)
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// API endpoint URL
    /// </summary>
    public string? ApiEndpoint { get; set; }
}

/// <summary>
/// Logging configuration
/// </summary>
public class LoggingConfiguration
{
    /// <summary>
    /// Log level
    /// </summary>
    public string Level { get; set; } = "Information";

    /// <summary>
    /// Log output path
    /// </summary>
    public string OutputPath { get; set; } = "%TEMP%/A3sist/logs";

    /// <summary>
    /// Whether to enable file logging
    /// </summary>
    public bool EnableFileLogging { get; set; } = true;

    /// <summary>
    /// Whether to enable console logging
    /// </summary>
    public bool EnableConsoleLogging { get; set; } = true;
}