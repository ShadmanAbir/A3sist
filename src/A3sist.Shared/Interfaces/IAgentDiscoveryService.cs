using A3sist.Shared.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for automatic agent discovery and registration
    /// </summary>
    public interface IAgentDiscoveryService
    {
        /// <summary>
        /// Discovers all agent types in the specified assembly
        /// </summary>
        /// <param name="assembly">Assembly to scan for agents</param>
        /// <returns>Collection of discovered agent types</returns>
        Task<IEnumerable<Type>> DiscoverAgentsAsync(Assembly assembly);

        /// <summary>
        /// Discovers all agent types in the current application domain
        /// </summary>
        /// <returns>Collection of discovered agent types</returns>
        Task<IEnumerable<Type>> DiscoverAllAgentsAsync();

        /// <summary>
        /// Gets agent metadata for a specific type
        /// </summary>
        /// <param name="agentType">Type of agent</param>
        /// <returns>Agent metadata if found, null otherwise</returns>
        Task<AgentMetadata?> GetAgentMetadataAsync(Type agentType);

        /// <summary>
        /// Gets all agent metadata for discovered agents
        /// </summary>
        /// <returns>Collection of agent metadata</returns>
        Task<IEnumerable<AgentMetadata>> GetAllAgentMetadataAsync();

        /// <summary>
        /// Validates that an agent type is properly implemented
        /// </summary>
        /// <param name="agentType">Type to validate</param>
        /// <returns>Validation result</returns>
        Task<AgentValidationResult> ValidateAgentTypeAsync(Type agentType);

        /// <summary>
        /// Automatically registers discovered agents with the factory
        /// </summary>
        /// <param name="agentFactory">Factory to register agents with</param>
        /// <param name="assembly">Optional assembly to scan (null for all)</param>
        Task AutoRegisterAgentsAsync(IAgentFactory agentFactory, Assembly? assembly = null);
    }
}