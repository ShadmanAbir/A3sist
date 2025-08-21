using A3sist.Shared.Attributes;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace A3sist.Core.Services
{
    /// <summary>
    /// Service for automatic agent discovery and registration
    /// </summary>
    public class AgentDiscoveryService : IAgentDiscoveryService
    {
        private readonly ILogger<AgentDiscoveryService> _logger;
        private readonly Dictionary<Type, AgentMetadata> _agentMetadataCache;

        public AgentDiscoveryService(ILogger<AgentDiscoveryService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _agentMetadataCache = new Dictionary<Type, AgentMetadata>();
        }

        /// <summary>
        /// Discovers all agent types in the specified assembly
        /// </summary>
        public async Task<IEnumerable<Type>> DiscoverAgentsAsync(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            await Task.CompletedTask; // Make async for consistency

            try
            {
                _logger.LogDebug("Discovering agents in assembly {AssemblyName}", assembly.FullName);

                var agentTypes = assembly.GetTypes()
                    .Where(type => typeof(IAgent).IsAssignableFrom(type) && 
                                   !type.IsAbstract && 
                                   !type.IsInterface)
                    .ToList();

                _logger.LogInformation("Discovered {AgentCount} agent types in assembly {AssemblyName}", 
                    agentTypes.Count, assembly.GetName().Name);

                return agentTypes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discovering agents in assembly {AssemblyName}", assembly.FullName);
                return Enumerable.Empty<Type>();
            }
        }

        /// <summary>
        /// Discovers all agent types in the current application domain
        /// </summary>
        public async Task<IEnumerable<Type>> DiscoverAllAgentsAsync()
        {
            var allAgentTypes = new List<Type>();

            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(assembly => !assembly.IsDynamic && 
                                      !string.IsNullOrEmpty(assembly.Location))
                    .ToList();

                _logger.LogDebug("Scanning {AssemblyCount} assemblies for agents", assemblies.Count);

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var agentTypes = await DiscoverAgentsAsync(assembly);
                        allAgentTypes.AddRange(agentTypes);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to discover agents in assembly {AssemblyName}", 
                            assembly.GetName().Name);
                    }
                }

                _logger.LogInformation("Discovered {TotalAgentCount} total agent types", allAgentTypes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during agent discovery");
            }

            return allAgentTypes;
        }

        /// <summary>
        /// Gets agent metadata for a specific type
        /// </summary>
        public async Task<AgentMetadata?> GetAgentMetadataAsync(Type agentType)
        {
            if (agentType == null)
                return null;

            await Task.CompletedTask; // Make async for consistency

            // Check cache first
            if (_agentMetadataCache.TryGetValue(agentType, out var cachedMetadata))
            {
                return cachedMetadata;
            }

            try
            {
                var metadata = CreateAgentMetadata(agentType);
                _agentMetadataCache[agentType] = metadata;
                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating metadata for agent type {AgentType}", agentType.Name);
                return null;
            }
        }

        /// <summary>
        /// Gets all agent metadata for discovered agents
        /// </summary>
        public async Task<IEnumerable<AgentMetadata>> GetAllAgentMetadataAsync()
        {
            var agentTypes = await DiscoverAllAgentsAsync();
            var metadataList = new List<AgentMetadata>();

            foreach (var agentType in agentTypes)
            {
                var metadata = await GetAgentMetadataAsync(agentType);
                if (metadata != null)
                {
                    metadataList.Add(metadata);
                }
            }

            return metadataList;
        }

        /// <summary>
        /// Validates that an agent type is properly implemented
        /// </summary>
        public async Task<AgentValidationResult> ValidateAgentTypeAsync(Type agentType)
        {
            if (agentType == null)
                return AgentValidationResult.Failure(null!, "Agent type cannot be null");

            await Task.CompletedTask; // Make async for consistency

            var errors = new List<string>();
            var warnings = new List<string>();

            try
            {
                // Check if it implements IAgent
                if (!typeof(IAgent).IsAssignableFrom(agentType))
                {
                    errors.Add($"Type {agentType.Name} does not implement IAgent interface");
                }

                // Check if it's not abstract
                if (agentType.IsAbstract)
                {
                    errors.Add($"Type {agentType.Name} is abstract and cannot be instantiated");
                }

                // Check if it's not an interface
                if (agentType.IsInterface)
                {
                    errors.Add($"Type {agentType.Name} is an interface and cannot be instantiated");
                }

                // Check if it has a public constructor
                var constructors = agentType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                if (constructors.Length == 0)
                {
                    errors.Add($"Type {agentType.Name} has no public constructors");
                }

                // Check for required dependencies in constructors
                foreach (var constructor in constructors)
                {
                    var parameters = constructor.GetParameters();
                    foreach (var parameter in parameters)
                    {
                        if (parameter.ParameterType.IsInterface && 
                            !parameter.HasDefaultValue && 
                            parameter.ParameterType != typeof(ILogger) &&
                            !parameter.ParametType.Name.StartsWith("ILogger"))
                        {
                            warnings.Add($"Constructor parameter {parameter.Name} of type {parameter.ParameterType.Name} should be registered in DI container");
                        }
                    }
                }

                // Check for agent capabilities
                var capabilities = agentType.GetCustomAttributes<AgentCapabilityAttribute>();
                if (!capabilities.Any())
                {
                    warnings.Add($"Type {agentType.Name} has no defined capabilities");
                }

                if (errors.Any())
                {
                    return AgentValidationResult.Failure(agentType, errors.ToArray());
                }

                if (warnings.Any())
                {
                    return AgentValidationResult.Warning(agentType, warnings.ToArray());
                }

                return AgentValidationResult.Success(agentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating agent type {AgentType}", agentType.Name);
                return AgentValidationResult.Failure(agentType, $"Validation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Automatically registers discovered agents with the factory
        /// </summary>
        public async Task AutoRegisterAgentsAsync(IAgentFactory agentFactory, Assembly? assembly = null)
        {
            if (agentFactory == null)
                throw new ArgumentNullException(nameof(agentFactory));

            IEnumerable<Type> agentTypes;
            
            if (assembly != null)
            {
                agentTypes = await DiscoverAgentsAsync(assembly);
            }
            else
            {
                agentTypes = await DiscoverAllAgentsAsync();
            }

            var registeredCount = 0;
            var failedCount = 0;

            foreach (var agentType in agentTypes)
            {
                try
                {
                    // Validate the agent type first
                    var validationResult = await ValidateAgentTypeAsync(agentType);
                    if (!validationResult.IsValid)
                    {
                        _logger.LogWarning("Skipping invalid agent type {AgentType}: {Errors}", 
                            agentType.Name, string.Join(", ", validationResult.Errors));
                        failedCount++;
                        continue;
                    }

                    // Register the agent
                    await agentFactory.RegisterAgentTypeAsync(agentType);
                    registeredCount++;

                    _logger.LogDebug("Auto-registered agent type {AgentType}", agentType.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to auto-register agent type {AgentType}", agentType.Name);
                    failedCount++;
                }
            }

            _logger.LogInformation("Auto-registration completed: {RegisteredCount} registered, {FailedCount} failed", 
                registeredCount, failedCount);
        }

        /// <summary>
        /// Creates agent metadata from a type
        /// </summary>
        private AgentMetadata CreateAgentMetadata(Type agentType)
        {
            var metadata = new AgentMetadata
            {
                AgentType = agentType,
                Name = agentType.Name,
                FullTypeName = agentType.FullName ?? agentType.Name,
                Namespace = agentType.Namespace ?? string.Empty,
                AssemblyName = agentType.Assembly.GetName().Name ?? string.Empty,
                IsAbstract = agentType.IsAbstract
            };

            // Get capabilities
            var capabilities = agentType.GetCustomAttributes<AgentCapabilityAttribute>().ToList();
            metadata.Capabilities = capabilities;

            // Extract information from capabilities
            foreach (var capability in capabilities)
            {
                if (!string.IsNullOrEmpty(capability.FileExtensions))
                {
                    var extensions = capability.FileExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(ext => ext.Trim())
                        .Where(ext => !string.IsNullOrEmpty(ext));
                    metadata.SupportedFileExtensions.AddRange(extensions);
                }

                if (!string.IsNullOrEmpty(capability.Keywords))
                {
                    var keywords = capability.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(kw => kw.Trim())
                        .Where(kw => !string.IsNullOrEmpty(kw));
                    metadata.Keywords.AddRange(keywords);
                }

                if (capability.Priority > metadata.Priority)
                {
                    metadata.Priority = capability.Priority;
                }

                metadata.Type = capability.AgentType;
            }

            // Get constructor dependencies
            var constructors = agentType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                foreach (var parameter in parameters)
                {
                    if (parameter.ParameterType.IsInterface)
                    {
                        metadata.Dependencies.Add(parameter.ParameterType);
                    }
                }
            }

            // Remove duplicates
            metadata.SupportedFileExtensions = metadata.SupportedFileExtensions.Distinct().ToList();
            metadata.Keywords = metadata.Keywords.Distinct().ToList();
            metadata.Dependencies = metadata.Dependencies.Distinct().ToList();

            return metadata;
        }
    }
}