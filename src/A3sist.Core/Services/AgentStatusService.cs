using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace A3sist.Core.Services
{
    public class AgentStatusService : IAgentStatusService
    {
        private readonly ConcurrentDictionary<string, AgentStatus> _agentStatuses;

        public AgentStatusService()
        {
            _agentStatuses = new ConcurrentDictionary<string, AgentStatus>();
        }

        public Task<List<AgentStatus>> GetActiveAgentsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<AgentStatus> GetAgentStatusAsync(string agentId)
        {
            return Task.FromResult(_agentStatuses.GetOrAdd(agentId, new AgentStatus
            {
                AgentId = agentId,
                Status = "Unknown",
                LastUpdated = DateTime.UtcNow
            }));
        }

        public Task UpdateAgentStatusAsync(string agentId, AgentStatus status)
        {
            _agentStatuses[agentId] = status;
            return Task.CompletedTask;
        }
    }
}