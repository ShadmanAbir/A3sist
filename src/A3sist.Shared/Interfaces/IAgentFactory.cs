using A3sist.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for creating agent instances
    /// </summary>
    public interface IAgentFactory
    {
        /// <summary>
        /// Creates an agent instance by name
        /// </summary>
        /// <param name="agentName">Name of the agent to create</param>
        /// <returns>Agent instance if found, null otherwise</returns>
        Task<IAgent?> CreateAgentAsync(string agentName);

        /// <summary>
        /// Creates an agent instance by type
        /// </summary>
        /// <param name="agentType">Type of agent to create</param>
        /// <returns>Agent instance if found, null otherwise</returns>
        Task<IAgent?> CreateAgentAsync(AgentType agentType);

        /// <summary>
        /// Creates an agent instance by type name
        /// </summary>
        /// <param name="agentTypeName">Full type name of the agent</param>
        /// <returns>Agent instance if found, null otherwise</returns>
        Task<IAgent?> CreateAgentByTypeAsync(string agentTypeName);

        /// <summary>
        /// Gets all available agent types
        /// </summary>
        /// <returns>Collection of available agent types</returns>
        Task<IEnumerable<Type>> GetAvailableAgentTypesAsync();

        /// <summary>
        /// Gets all registered agent names
        /// </summary>
        /// <returns>Collection of registered agent names</returns>
        Task<IEnumerable<string>> GetRegisteredAgentNamesAsync();

        /// <summary>
        /// Registers an agent type with the factory
        /// </summary>
        /// <param name="agentType">Type of agent to register</param>
        /// <param name="agentName">Optional custom name for the agent</param>
        Task RegisterAgentTypeAsync(Type agentType, string? agentName = null);

        /// <summary>
        /// Unregisters an agent type from the factory
        /// </summary>
        /// <param name="agentName">Name of the agent to unregister</param>
        Task UnregisterAgentTypeAsync(string agentName);

        /// <summary>
        /// Checks if an agent type is registered
        /// </summary>
        /// <param name="agentName">Name of the agent to check</param>
        /// <returns>True if registered, false otherwise</returns>
        Task<bool> IsAgentRegisteredAsync(string agentName);
    }
}