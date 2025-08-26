using A3sist.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for agent-specific configuration management
    /// </summary>
    public interface IAgentConfiguration
    {
        /// <summary>
        /// Gets configuration for a specific agent
        /// </summary>
        /// <param name="agentName">Name of the agent</param>
        /// <returns>Agent configuration</returns>
        Task<AgentConfiguration> GetAgentConfigurationAsync(string agentName);

        /// <summary>
        /// Updates configuration for a specific agent
        /// </summary>
        /// <param name="agentName">Name of the agent</param>
        /// <param name="configuration">New configuration</param>
        Task UpdateAgentConfigurationAsync(string agentName, AgentConfiguration configuration);

        /// <summary>
        /// Gets all agent configurations
        /// </summary>
        /// <returns>Dictionary of agent configurations</returns>
        Task<Dictionary<string, AgentConfiguration>> GetAllAgentConfigurationsAsync();

        /// <summary>
        /// Validates agent configuration
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <returns>Validation result</returns>
        Task<ConfigurationValidationResult> ValidateConfigurationAsync(AgentConfiguration configuration);

        /// <summary>
        /// Event raised when configuration changes
        /// </summary>
        event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
    }
}