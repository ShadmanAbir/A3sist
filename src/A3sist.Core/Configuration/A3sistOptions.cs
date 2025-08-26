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
}