using A3sist.Shared.Enums;
using A3sist.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for agent management including registration, discovery, and lifecycle management
    /// </summary>
    public interface IAgentManager
    {
        /// <summary>
        /// Gets an agent by name
        /// </summary>
        /// <param name="name">Name of the agent</param>
        /// <returns>The agent if found, null otherwise</returns>
        Task<IAgent?> GetAgentAsync(string name);

        /// <summary>
        /// Gets the first agent of the specified type
        /// </summary>
        /// <param name="type">Type of agent to find</param>
        /// <returns>The agent if found, null otherwise</returns>
        Task<IAgent?> GetAgentAsync(AgentType type);

        /// <summary>
        /// Gets all agents matching the specified predicate
        /// </summary>
        /// <param name="predicate">Optional predicate to filter agents</param>
        /// <returns>Collection of matching agents</returns>
        Task<IEnumerable<IAgent>> GetAgentsAsync(Func<IAgent, bool>? predicate = null);

        /// <summary>
        /// Registers an agent with the manager
        /// </summary>
        /// <param name="agent">Agent to register</param>
        Task RegisterAgentAsync(IAgent agent);

        /// <summary>
        /// Unregisters an agent by name
        /// </summary>
        /// <param name="name">Name of the agent to unregister</param>
        Task UnregisterAgentAsync(string name);

        /// <summary>
        /// Gets the status of a specific agent
        /// </summary>
        /// <param name="name">Name of the agent</param>
        /// <returns>Agent status if found, null otherwise</returns>
        Task<AgentStatus?> GetAgentStatusAsync(string name);

        /// <summary>
        /// Gets the status of all registered agents
        /// </summary>
        /// <returns>Collection of agent statuses</returns>
        Task<IEnumerable<AgentStatus>> GetAllAgentStatusesAsync();

        /// <summary>
        /// Starts all registered agents
        /// </summary>
        Task StartAllAgentsAsync();

        /// <summary>
        /// Stops all registered agents
        /// </summary>
        Task StopAllAgentsAsync();

        /// <summary>
        /// Performs health checks on all agents
        /// </summary>
        /// <returns>Dictionary of agent names and their health status</returns>
        Task<Dictionary<string, HealthStatus>> PerformHealthChecksAsync();

        /// <summary>
        /// Event raised when an agent is registered
        /// </summary>
        event EventHandler<AgentRegisteredEventArgs> AgentRegistered;

        /// <summary>
        /// Event raised when an agent is unregistered
        /// </summary>
        event EventHandler<AgentUnregisteredEventArgs> AgentUnregistered;

        /// <summary>
        /// Event raised when an agent's status changes
        /// </summary>
        event EventHandler<AgentStatusChangedEventArgs> AgentStatusChanged;
    }
}