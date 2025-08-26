using A3sist.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for agent status monitoring service
    /// </summary>
    public interface IAgentStatusService
    {
        /// <summary>
        /// Gets the status of a specific agent
        /// </summary>
        /// <param name="agentName">Name of the agent</param>
        /// <returns>Agent status information</returns>
        Task<AgentStatus?> GetAgentStatusAsync(string agentName);

        /// <summary>
        /// Gets the status of all agents
        /// </summary>
        /// <returns>Collection of agent statuses</returns>
        Task<IEnumerable<AgentStatus>> GetAllAgentStatusesAsync();

        /// <summary>
        /// Updates the status of an agent
        /// </summary>
        /// <param name="agentName">Name of the agent</param>
        /// <param name="status">New status</param>
        /// <returns>Task representing the update operation</returns>
        Task UpdateAgentStatusAsync(string agentName, AgentStatus status);

        /// <summary>
        /// Records agent activity
        /// </summary>
        /// <param name="agentName">Name of the agent</param>
        /// <param name="success">Whether the activity was successful</param>
        /// <param name="processingTime">Time taken to process</param>
        /// <returns>Task representing the record operation</returns>
        Task RecordAgentActivityAsync(string agentName, bool success, TimeSpan processingTime);

        /// <summary>
        /// Event raised when agent status changes
        /// </summary>
        event EventHandler<AgentStatusChangedEventArgs> AgentStatusChanged;
    }
}