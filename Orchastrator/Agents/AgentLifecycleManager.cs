using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeAssist.Agents
{
    public class AgentLifecycleManager
    {
        private readonly Dictionary<string, BaseAgent> _agents = new Dictionary<string, BaseAgent>();
        private readonly AgentCommunication _communication;

        public AgentLifecycleManager(AgentCommunication communication)
        {
            _communication = communication ?? throw new ArgumentNullException(nameof(communication));
        }

        public async Task RegisterAgentAsync(BaseAgent agent)
        {
            if (agent == null)
                throw new ArgumentNullException(nameof(agent));

            _agents[agent.AgentName] = agent;
            _communication.RegisterAgent(agent);

            await agent.InitializeAsync();
        }

        public async Task UnregisterAgentAsync(string agentName)
        {
            if (string.IsNullOrEmpty(agentName))
                throw new ArgumentNullException(nameof(agentName));

            if (_agents.TryGetValue(agentName, out var agent))
            {
            await agent.ShutdownAsync();
                _agents.Remove(agentName);
                _communication.UnregisterAgent(agentName);
            }
        }

        public async Task ExecuteAgentAsync(string agentName)
        {
            if (string.IsNullOrEmpty(agentName))
                throw new ArgumentNullException(nameof(agentName));

            if (_agents.TryGetValue(agentName, out var agent))
            {
            await agent.ExecuteAsync();
            }
            else
            {
                throw new ArgumentException($"Agent {agentName} not found.");
            }
        }

        public IEnumerable<string> GetRegisteredAgents()
        {
            return _agents.Keys;
        }

        public BaseAgent.AgentStatus GetAgentStatus(string agentName)
        {
            if (string.IsNullOrEmpty(agentName))
                throw new ArgumentNullException(nameof(agentName));

            if (_agents.TryGetValue(agentName, out var agent))
            {
                return agent.Status;
            }

            throw new ArgumentException($"Agent {agentName} not found.");
        }
    }
}