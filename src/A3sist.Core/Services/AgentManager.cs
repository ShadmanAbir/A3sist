using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Core.Services
{
    /// <summary>
    /// Agent manager service for agent registration, discovery, and lifecycle management
    /// </summary>
    public class AgentManager : IAgentManager, IDisposable
    {
        private readonly ILogger<AgentManager> _logger;
        private readonly ConcurrentDictionary<string, IAgent> _agents;
        private readonly ConcurrentDictionary<string, AgentStatus> _agentStatuses;
        private readonly Timer _healthCheckTimer;
        private readonly SemaphoreSlim _operationSemaphore;
        private bool _disposed;

        /// <summary>
        /// Event raised when an agent is registered
        /// </summary>
        public event EventHandler<AgentRegisteredEventArgs>? AgentRegistered;

        /// <summary>
        /// Event raised when an agent is unregistered
        /// </summary>
        public event EventHandler<AgentUnregisteredEventArgs>? AgentUnregistered;

        /// <summary>
        /// Event raised when an agent's status changes
        /// </summary>
        public event EventHandler<AgentStatusChangedEventArgs>? AgentStatusChanged;

        public AgentManager(ILogger<AgentManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _agents = new ConcurrentDictionary<string, IAgent>();
            _agentStatuses = new ConcurrentDictionary<string, AgentStatus>();
            _operationSemaphore = new SemaphoreSlim(1, 1);
            
            // Start health check timer (every 30 seconds)
            _healthCheckTimer = new Timer(PerformPeriodicHealthCheck, null, 
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            _logger.LogInformation("AgentManager initialized");
        }

        /// <summary>
        /// Gets an agent by name
        /// </summary>
        public async Task<IAgent?> GetAgentAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            await Task.CompletedTask; // Make async for consistency
            return _agents.TryGetValue(name, out var agent) ? agent : null;
        }

        /// <summary>
        /// Gets the first agent of the specified type
        /// </summary>
        public async Task<IAgent?> GetAgentAsync(AgentType type)
        {
            await Task.CompletedTask; // Make async for consistency
            return _agents.Values.FirstOrDefault(a => a.Type == type);
        }

        /// <summary>
        /// Gets all agents matching the specified predicate
        /// </summary>
        public async Task<IEnumerable<IAgent>> GetAgentsAsync(Func<IAgent, bool>? predicate = null)
        {
            await Task.CompletedTask; // Make async for consistency
            var agents = _agents.Values.AsEnumerable();
            return predicate != null ? agents.Where(predicate) : agents;
        }

        /// <summary>
        /// Registers an agent with the manager
        /// </summary>
        public async Task RegisterAgentAsync(IAgent agent)
        {
            if (agent == null)
                throw new ArgumentNullException(nameof(agent));

            await _operationSemaphore.WaitAsync();
            try
            {
                if (_agents.ContainsKey(agent.Name))
                {
                    _logger.LogWarning("Agent {AgentName} is already registered", agent.Name);
                    return;
                }

                // Initialize the agent
                try
                {
                    await agent.InitializeAsync();
                    _logger.LogInformation("Agent {AgentName} initialized successfully", agent.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize agent {AgentName}", agent.Name);
                    throw;
                }

                // Add to collections
                _agents.TryAdd(agent.Name, agent);
                
                // Create initial status
                var status = CreateAgentStatus(agent);
                _agentStatuses.TryAdd(agent.Name, status);

                _logger.LogInformation("Agent {AgentName} of type {AgentType} registered successfully", 
                    agent.Name, agent.Type);

                // Raise event
                AgentRegistered?.Invoke(this, new AgentRegisteredEventArgs(agent));
            }
            finally
            {
                _operationSemaphore.Release();
            }
        }

        /// <summary>
        /// Unregisters an agent by name
        /// </summary>
        public async Task UnregisterAgentAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            await _operationSemaphore.WaitAsync();
            try
            {
                if (!_agents.TryRemove(name, out var agent))
                {
                    _logger.LogWarning("Agent {AgentName} not found for unregistration", name);
                    return;
                }

                // Shutdown the agent
                try
                {
                    await agent.ShutdownAsync();
                    _logger.LogInformation("Agent {AgentName} shut down successfully", name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error shutting down agent {AgentName}", name);
                }

                // Remove status
                _agentStatuses.TryRemove(name, out _);

                _logger.LogInformation("Agent {AgentName} unregistered successfully", name);

                // Raise event
                AgentUnregistered?.Invoke(this, new AgentUnregisteredEventArgs(name));
            }
            finally
            {
                _operationSemaphore.Release();
            }
        }

        /// <summary>
        /// Gets the status of a specific agent
        /// </summary>
        public async Task<AgentStatus?> GetAgentStatusAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            await Task.CompletedTask; // Make async for consistency

            if (!_agentStatuses.TryGetValue(name, out var status))
                return null;

            // Update status from agent if available
            if (_agents.TryGetValue(name, out var agent))
            {
                try
                {
                    var currentStatus = GetAgentCurrentStatus(agent);
                    UpdateAgentStatus(name, currentStatus);
                    return currentStatus;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting status for agent {AgentName}", name);
                }
            }

            return status;
        }

        /// <summary>
        /// Gets the status of all registered agents
        /// </summary>
        public async Task<IEnumerable<AgentStatus>> GetAllAgentStatusesAsync()
        {
            var statuses = new List<AgentStatus>();

            foreach (var kvp in _agents)
            {
                try
                {
                    var status = await GetAgentStatusAsync(kvp.Key);
                    if (status != null)
                        statuses.Add(status);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting status for agent {AgentName}", kvp.Key);
                }
            }

            return statuses;
        }

        /// <summary>
        /// Starts all registered agents
        /// </summary>
        public async Task StartAllAgentsAsync()
        {
            _logger.LogInformation("Starting all agents");

            var tasks = _agents.Values.Select(async agent =>
            {
                try
                {
                    await agent.InitializeAsync();
                    _logger.LogDebug("Agent {AgentName} started successfully", agent.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to start agent {AgentName}", agent.Name);
                }
            });

            await Task.WhenAll(tasks);
            _logger.LogInformation("All agents start process completed");
        }

        /// <summary>
        /// Stops all registered agents
        /// </summary>
        public async Task StopAllAgentsAsync()
        {
            _logger.LogInformation("Stopping all agents");

            var tasks = _agents.Values.Select(async agent =>
            {
                try
                {
                    await agent.ShutdownAsync();
                    _logger.LogDebug("Agent {AgentName} stopped successfully", agent.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to stop agent {AgentName}", agent.Name);
                }
            });

            await Task.WhenAll(tasks);
            _logger.LogInformation("All agents stop process completed");
        }

        /// <summary>
        /// Performs health checks on all agents
        /// </summary>
        public async Task<Dictionary<string, HealthStatus>> PerformHealthChecksAsync()
        {
            var healthStatuses = new Dictionary<string, HealthStatus>();

            foreach (var kvp in _agents)
            {
                try
                {
                    var status = await GetAgentStatusAsync(kvp.Key);
                    healthStatuses[kvp.Key] = status?.Health ?? HealthStatus.Unknown;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Health check failed for agent {AgentName}", kvp.Key);
                    healthStatuses[kvp.Key] = HealthStatus.Unhealthy;
                }
            }

            return healthStatuses;
        }

        /// <summary>
        /// Creates an agent status from an agent instance
        /// </summary>
        private AgentStatus CreateAgentStatus(IAgent agent)
        {
            return GetAgentCurrentStatus(agent);
        }

        /// <summary>
        /// Gets the current status from an agent
        /// </summary>
        private AgentStatus GetAgentCurrentStatus(IAgent agent)
        {
            // Try to get status from BaseAgent if possible
            if (agent is A3sist.Core.Agents.Base.BaseAgent baseAgent)
            {
                return baseAgent.GetStatus();
            }

            // Fallback to basic status
            return new AgentStatus
            {
                Name = agent.Name,
                Type = agent.Type,
                Status = WorkStatus.Pending,
                Health = HealthStatus.Unknown,
                LastActivity = DateTime.UtcNow,
                StartedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Updates the cached agent status and raises events if changed
        /// </summary>
        private void UpdateAgentStatus(string agentName, AgentStatus newStatus)
        {
            var previousStatus = _agentStatuses.GetValueOrDefault(agentName);
            _agentStatuses.AddOrUpdate(agentName, newStatus, (key, oldValue) => newStatus);

            // Raise status changed event if status actually changed
            if (previousStatus != null && 
                (previousStatus.Status != newStatus.Status || previousStatus.Health != newStatus.Health))
            {
                AgentStatusChanged?.Invoke(this, new AgentStatusChangedEventArgs(
                    agentName,
                    previousStatus.Status,
                    newStatus.Status,
                    previousStatus.Health,
                    newStatus.Health));
            }
        }

        /// <summary>
        /// Periodic health check callback
        /// </summary>
        private async void PerformPeriodicHealthCheck(object? state)
        {
            if (_disposed)
                return;

            try
            {
                _logger.LogDebug("Performing periodic health check on {AgentCount} agents", _agents.Count);
                await PerformHealthChecksAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic health check");
            }
        }

        /// <summary>
        /// Disposes the agent manager
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                _healthCheckTimer?.Dispose();
                _operationSemaphore?.Dispose();

                // Shutdown all agents
                var shutdownTask = StopAllAgentsAsync();
                shutdownTask.Wait(TimeSpan.FromSeconds(30)); // Wait up to 30 seconds

                _logger.LogInformation("AgentManager disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing AgentManager");
            }
        }
    }
}