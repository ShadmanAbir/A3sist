using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace A3sist.Core.Services
{
    /// <summary>
    /// Factory for creating agent instances with dependency injection support
    /// </summary>
    public class AgentFactory : IAgentFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AgentFactory> _logger;
        private readonly ConcurrentDictionary<string, Type> _registeredAgents;
        private readonly ConcurrentDictionary<AgentType, Type> _agentTypeMap;

        public AgentFactory(IServiceProvider serviceProvider, ILogger<AgentFactory> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registeredAgents = new ConcurrentDictionary<string, Type>();
            _agentTypeMap = new ConcurrentDictionary<AgentType, Type>();
        }

        /// <summary>
        /// Creates an agent instance by name
        /// </summary>
        public async Task<IAgent?> CreateAgentAsync(string agentName)
        {
            if (string.IsNullOrWhiteSpace(agentName))
                return null;

            await Task.CompletedTask; // Make async for consistency

            if (!_registeredAgents.TryGetValue(agentName, out var agentType))
            {
                _logger.LogWarning("Agent {AgentName} is not registered", agentName);
                return null;
            }

            return CreateAgentInstance(agentType, agentName);
        }

        /// <summary>
        /// Creates an agent instance by type
        /// </summary>
        public async Task<IAgent?> CreateAgentAsync(AgentType agentType)
        {
            await Task.CompletedTask; // Make async for consistency

            if (!_agentTypeMap.TryGetValue(agentType, out var type))
            {
                _logger.LogWarning("No agent registered for type {AgentType}", agentType);
                return null;
            }

            return CreateAgentInstance(type, agentType.ToString());
        }

        /// <summary>
        /// Creates an agent instance by type name
        /// </summary>
        public async Task<IAgent?> CreateAgentByTypeAsync(string agentTypeName)
        {
            if (string.IsNullOrWhiteSpace(agentTypeName))
                return null;

            await Task.CompletedTask; // Make async for consistency

            var type = Type.GetType(agentTypeName);
            if (type == null)
            {
                _logger.LogWarning("Agent type {AgentTypeName} not found", agentTypeName);
                return null;
            }

            if (!typeof(IAgent).IsAssignableFrom(type))
            {
                _logger.LogError("Type {AgentTypeName} does not implement IAgent", agentTypeName);
                return null;
            }

            return CreateAgentInstance(type, type.Name);
        }

        /// <summary>
        /// Gets all available agent types
        /// </summary>
        public async Task<IEnumerable<Type>> GetAvailableAgentTypesAsync()
        {
            await Task.CompletedTask; // Make async for consistency
            return _registeredAgents.Values.Distinct();
        }

        /// <summary>
        /// Gets all registered agent names
        /// </summary>
        public async Task<IEnumerable<string>> GetRegisteredAgentNamesAsync()
        {
            await Task.CompletedTask; // Make async for consistency
            return _registeredAgents.Keys;
        }

        /// <summary>
        /// Registers an agent type with the factory
        /// </summary>
        public async Task RegisterAgentTypeAsync(Type agentType, string? agentName = null)
        {
            if (agentType == null)
                throw new ArgumentNullException(nameof(agentType));

            if (!typeof(IAgent).IsAssignableFrom(agentType))
                throw new ArgumentException($"Type {agentType.Name} does not implement IAgent", nameof(agentType));

            if (agentType.IsAbstract)
                throw new ArgumentException($"Cannot register abstract type {agentType.Name}", nameof(agentType));

            await Task.CompletedTask; // Make async for consistency

            // Use provided name or default to type name
            var name = agentName ?? agentType.Name;

            // Try to get the agent type enum value
            var agentTypeEnum = GetAgentTypeFromClass(agentType);

            // Register by name
            _registeredAgents.TryAdd(name, agentType);

            // Register by type enum if available
            if (agentTypeEnum.HasValue)
            {
                _agentTypeMap.TryAdd(agentTypeEnum.Value, agentType);
            }

            _logger.LogInformation("Registered agent type {AgentType} with name {AgentName}", 
                agentType.Name, name);
        }

        /// <summary>
        /// Unregisters an agent type from the factory
        /// </summary>
        public async Task UnregisterAgentTypeAsync(string agentName)
        {
            if (string.IsNullOrWhiteSpace(agentName))
                return;

            await Task.CompletedTask; // Make async for consistency

            if (_registeredAgents.TryRemove(agentName, out var agentType))
            {
                // Also remove from type map
                var agentTypeEnum = GetAgentTypeFromClass(agentType);
                if (agentTypeEnum.HasValue)
                {
                    _agentTypeMap.TryRemove(agentTypeEnum.Value, out _);
                }

                _logger.LogInformation("Unregistered agent {AgentName}", agentName);
            }
            else
            {
                _logger.LogWarning("Agent {AgentName} was not registered", agentName);
            }
        }

        /// <summary>
        /// Checks if an agent type is registered
        /// </summary>
        public async Task<bool> IsAgentRegisteredAsync(string agentName)
        {
            if (string.IsNullOrWhiteSpace(agentName))
                return false;

            await Task.CompletedTask; // Make async for consistency
            return _registeredAgents.ContainsKey(agentName);
        }

        /// <summary>
        /// Creates an agent instance using dependency injection
        /// </summary>
        private IAgent? CreateAgentInstance(Type agentType, string agentName)
        {
            try
            {
                _logger.LogDebug("Creating agent instance of type {AgentType}", agentType.Name);

                // Try to create using DI container first
                var agent = _serviceProvider.GetService(agentType) as IAgent;
                if (agent != null)
                {
                    _logger.LogDebug("Created agent {AgentName} using DI container", agentName);
                    return agent;
                }

                // Fallback to ActivatorUtilities for types not registered in DI
                agent = ActivatorUtilities.CreateInstance(_serviceProvider, agentType) as IAgent;
                if (agent != null)
                {
                    _logger.LogDebug("Created agent {AgentName} using ActivatorUtilities", agentName);
                    return agent;
                }

                _logger.LogError("Failed to create agent instance of type {AgentType}", agentType.Name);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating agent instance of type {AgentType}", agentType.Name);
                return null;
            }
        }

        /// <summary>
        /// Attempts to determine the AgentType enum value from a class
        /// </summary>
        private AgentType? GetAgentTypeFromClass(Type agentType)
        {
            try
            {
                // Try to create a temporary instance to get the Type property
                var tempInstance = ActivatorUtilities.CreateInstance(_serviceProvider, agentType) as IAgent;
                return tempInstance?.Type;
            }
            catch
            {
                // If we can't create an instance, try to infer from the class name
                var typeName = agentType.Name;
                
                // Remove common suffixes
                typeName = typeName.Replace("Agent", "").Replace("Handler", "");
                
                // Try to parse as enum
                if (Enum.TryParse<AgentType>(typeName, true, out var result))
                {
                    return result;
                }

                return null;
            }
        }
    }
}