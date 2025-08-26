using A3sist.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Configuration model for agents with validation
    /// </summary>
    public class AgentConfiguration
    {
        /// <summary>
        /// Name of the agent
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Type of the agent
        /// </summary>
        public AgentType Type { get; set; }

        /// <summary>
        /// Whether the agent is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Agent-specific settings
        /// </summary>
        public Dictionary<string, object> Settings { get; set; }

        /// <summary>
        /// Maximum number of concurrent tasks the agent can handle
        /// </summary>
        [Range(1, 100)]
        public int MaxConcurrentTasks { get; set; } = 5;

        /// <summary>
        /// Timeout for agent operations
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Retry policy for failed operations
        /// </summary>
        public RetryPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Priority level for the agent
        /// </summary>
        public WorkflowPriority Priority { get; set; } = WorkflowPriority.Normal;

        /// <summary>
        /// Health check interval
        /// </summary>
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Whether to enable detailed logging for this agent
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;

        /// <summary>
        /// Custom properties for the agent
        /// </summary>
        public Dictionary<string, object> Properties { get; set; }

        /// <summary>
        /// Configuration version for migration support
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// When the configuration was last modified
        /// </summary>
        public DateTime LastModified { get; set; }

        public AgentConfiguration()
        {
            Settings = new Dictionary<string, object>();
            Properties = new Dictionary<string, object>();
            RetryPolicy = new RetryPolicy();
            LastModified = DateTime.UtcNow;
        }

        public AgentConfiguration(string name, AgentType type) : this()
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type;
        }
    }
}