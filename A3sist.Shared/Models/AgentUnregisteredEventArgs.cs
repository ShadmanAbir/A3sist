using System;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Event arguments for agent unregistration events
    /// </summary>
    public class AgentUnregisteredEventArgs : EventArgs
    {
        /// <summary>
        /// Name of the agent that was unregistered
        /// </summary>
        public string AgentName { get; }

        /// <summary>
        /// When the agent was unregistered
        /// </summary>
        public DateTime UnregisteredAt { get; }

        /// <summary>
        /// Reason for unregistration
        /// </summary>
        public string? Reason { get; }

        public AgentUnregisteredEventArgs(string agentName, string? reason = null)
        {
            AgentName = agentName ?? throw new ArgumentNullException(nameof(agentName));
            Reason = reason;
            UnregisteredAt = DateTime.UtcNow;
        }
    }
}