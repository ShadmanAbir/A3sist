using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using A3sist.Shared.Models;

namespace A3sist.Shared.Interfaces
{
    public interface IAgentStatusService
    {
        Task<List<AgentStatus>> GetActiveAgentsAsync();
        Task<AgentStatus> GetAgentStatusAsync(string agentId);
        Task UpdateAgentStatusAsync(string agentId, AgentStatus status);
    }
}