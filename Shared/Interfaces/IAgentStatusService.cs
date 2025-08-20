using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    public interface IAgentStatusService
    {
         Task<AgentStatus> GetAgentStatusAsync(string agentId);
        Task UpdateAgentStatusAsync(string agentId, AgentStatus status);
    }
}