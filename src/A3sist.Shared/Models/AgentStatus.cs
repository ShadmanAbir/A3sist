using A3sist.Shared.Enums;
using System;
using System.Collections.Generic;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Enhanced agent status model for monitoring and health checks
    /// </summary>
    public class AgentStatus
    {
        /// <summary>
        /// Name of the agent
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of the agent
        /// </summary>
        public AgentType Type { get; set; }

        /// <summary>
        /// Current status of the agent
        /// </summary>
        public WorkStatus Status { get; set; }

        /// <summary>
        /// Last time the agent was active
        /// </summary>
        public DateTime LastActivity { get; set; }

        /// <summary>
        /// Total number of tasks processed by the agent
        /// </summary>
        public int TasksProcessed { get; set; }

        /// <summary>
        /// Number of tasks that succeeded
        /// </summary>
        public int TasksSucceeded { get; set; }

        /// <summary>
        /// Number of tasks that failed
        /// </summary>
        public int TasksFailed { get; set; }

        /// <summary>
        /// Average time taken to process tasks
        /// </summary>
        public TimeSpan AverageProcessingTime { get; set; }

        /// <summary>
        /// Current health status of the agent
        /// </summary>
        public HealthStatus Health { get; set; }

        /// <summary>
        /// Error message if the agent is in an error state
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// When the agent was started
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Agent version information
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Additional properties for the agent status
        /// </summary>
        public Dictionary<string, object> Properties { get; set; }

        /// <summary>
        /// Current memory usage in bytes
        /// </summary>
        public long MemoryUsage { get; set; }

        /// <summary>
        /// CPU usage percentage (0-100)
        /// </summary>
        public double CpuUsage { get; set; }

        public AgentStatus()
        {
            Properties = new Dictionary<string, object>();
            Health = HealthStatus.Unknown;
            Status = WorkStatus.Pending;
            StartedAt = DateTime.UtcNow;
            LastActivity = DateTime.UtcNow;
        }

        /// <summary>
        /// Calculates the success rate of the agent
        /// </summary>
        public double SuccessRate => TasksProcessed > 0 ? (double)TasksSucceeded / TasksProcessed : 0.0;

        /// <summary>
        /// Calculates the failure rate of the agent
        /// </summary>
        public double FailureRate => TasksProcessed > 0 ? (double)TasksFailed / TasksProcessed : 0.0;

        /// <summary>
        /// Gets the uptime of the agent
        /// </summary>
        public TimeSpan Uptime => DateTime.UtcNow - StartedAt;
    }
}