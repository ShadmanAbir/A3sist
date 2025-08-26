using A3sist.Shared.Enums;
using System;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Event arguments for agent status change events
    /// </summary>
    public class AgentStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Name of the agent whose status changed
        /// </summary>
        public string AgentName { get; }

        /// <summary>
        /// Previous status
        /// </summary>
        public WorkStatus PreviousStatus { get; }

        /// <summary>
        /// New status
        /// </summary>
        public WorkStatus NewStatus { get; }

        /// <summary>
        /// Previous health status
        /// </summary>
        public HealthStatus PreviousHealth { get; }

        /// <summary>
        /// New health status
        /// </summary>
        public HealthStatus NewHealth { get; }

        /// <summary>
        /// When the status changed
        /// </summary>
        public DateTime ChangedAt { get; }

        /// <summary>
        /// Additional details about the status change
        /// </summary>
        public string? Details { get; }

        public AgentStatusChangedEventArgs(
            string agentName, 
            WorkStatus previousStatus, 
            WorkStatus newStatus,
            HealthStatus previousHealth,
            HealthStatus newHealth,
            string? details = null)
        {
            AgentName = agentName ?? throw new ArgumentNullException(nameof(agentName));
            PreviousStatus = previousStatus;
            NewStatus = newStatus;
            PreviousHealth = previousHealth;
            NewHealth = newHealth;
            Details = details;
            ChangedAt = DateTime.UtcNow;
        }
    }
}