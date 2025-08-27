using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace A3sist.Core.Services
{
    /// <summary>
    /// Service for monitoring and tracking agent status
    /// </summary>
    public class AgentStatusService : IAgentStatusService
    {
        private readonly ILogger<AgentStatusService> _logger;
        private readonly ConcurrentDictionary<string, AgentStatus> _agentStatuses;

        /// <summary>
        /// Event raised when agent status changes
        /// </summary>
        public event EventHandler<AgentStatusChangedEventArgs>? AgentStatusChanged;

        public AgentStatusService(ILogger<AgentStatusService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _agentStatuses = new ConcurrentDictionary<string, AgentStatus>();
        }

        /// <summary>
        /// Gets the status of a specific agent
        /// </summary>
        public async Task<AgentStatus?> GetAgentStatusAsync(string agentName)
        {
            if (string.IsNullOrWhiteSpace(agentName))
                return null;

            await Task.CompletedTask;
            return _agentStatuses.TryGetValue(agentName, out var status) ? status : null;
        }

        /// <summary>
        /// Gets the status of all agents
        /// </summary>
        public async Task<IEnumerable<AgentStatus>> GetAllAgentStatusesAsync()
        {
            await Task.CompletedTask;
            return _agentStatuses.Values.ToList();
        }

        /// <summary>
        /// Updates the status of an agent
        /// </summary>
        public async Task UpdateAgentStatusAsync(string agentName, AgentStatus status)
        {
            if (string.IsNullOrWhiteSpace(agentName) || status == null)
                return;

            var previousStatus = _agentStatuses.TryGetValue(agentName, out var existing) ? existing.Status : WorkStatus.Unknown;
            
            _agentStatuses.AddOrUpdate(agentName, status, (key, oldStatus) =>
            {
                // Preserve some existing values if not provided in new status
                if (status.TasksProcessed == 0 && oldStatus.TasksProcessed > 0)
                    status.TasksProcessed = oldStatus.TasksProcessed;
                if (status.TasksSucceeded == 0 && oldStatus.TasksSucceeded > 0)
                    status.TasksSucceeded = oldStatus.TasksSucceeded;
                if (status.TasksFailed == 0 && oldStatus.TasksFailed > 0)
                    status.TasksFailed = oldStatus.TasksFailed;
                
                return status;
            });

            _logger.LogDebug("Updated status for agent {AgentName}: {Status}", agentName, status.Status);

            // Raise event if status changed
            if (previousStatus != status.Status)
            {
                AgentStatusChanged?.Invoke(this, new AgentStatusChangedEventArgs(
                    agentName, 
                    previousStatus, 
                    status.Status,
                    HealthStatus.Unknown,
                    HealthStatus.Unknown));
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Records agent activity
        /// </summary>
        public async Task RecordAgentActivityAsync(string agentName, bool success, TimeSpan processingTime)
        {
            if (string.IsNullOrWhiteSpace(agentName))
                return;

            _agentStatuses.AddOrUpdate(agentName, 
                // Create new status if agent doesn't exist
                new AgentStatus
                {
                    Name = agentName,
                    Status = WorkStatus.Active,
                    LastActivity = DateTime.UtcNow,
                    TasksProcessed = 1,
                    TasksSucceeded = success ? 1 : 0,
                    TasksFailed = success ? 0 : 1,
                    AverageProcessingTime = processingTime
                },
                // Update existing status
                (key, existingStatus) =>
                {
                    existingStatus.LastActivity = DateTime.UtcNow;
                    existingStatus.TasksProcessed++;
                    
                    if (success)
                    {
                        existingStatus.TasksSucceeded++;
                        existingStatus.Status = WorkStatus.Active;
                    }
                    else
                    {
                        existingStatus.TasksFailed++;
                        // Don't change status to failed immediately, let the agent manager handle that
                    }

                    // Update average processing time using exponential moving average
                    if (existingStatus.AverageProcessingTime == TimeSpan.Zero)
                    {
                        existingStatus.AverageProcessingTime = processingTime;
                    }
                    else
                    {
                        var alpha = 0.1; // Smoothing factor
                        var newAverage = TimeSpan.FromMilliseconds(
                            alpha * processingTime.TotalMilliseconds + 
                            (1 - alpha) * existingStatus.AverageProcessingTime.TotalMilliseconds);
                        existingStatus.AverageProcessingTime = newAverage;
                    }

                    return existingStatus;
                });

            _logger.LogTrace("Recorded activity for agent {AgentName}: success={Success}, processingTime={ProcessingTimeMs}ms", 
                agentName, success, processingTime.TotalMilliseconds);

            await Task.CompletedTask;
        }
    }
}