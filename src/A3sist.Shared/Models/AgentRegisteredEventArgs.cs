using A3sist.Shared.Interfaces;
using System;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Event arguments for agent registration events
    /// </summary>
    public class AgentRegisteredEventArgs : EventArgs
    {
        /// <summary>
        /// The agent that was registered
        /// </summary>
        public IAgent Agent { get; }

        /// <summary>
        /// When the agent was registered
        /// </summary>
        public DateTime RegisteredAt { get; }

        public AgentRegisteredEventArgs(IAgent agent)
        {
            Agent = agent ?? throw new ArgumentNullException(nameof(agent));
            RegisteredAt = DateTime.UtcNow;
        }
    }
}