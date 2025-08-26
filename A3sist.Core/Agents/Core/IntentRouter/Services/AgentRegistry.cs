using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using A3sist.Shared.Attributes;

namespace A3sist.Orchastrator.Agents.IntentRouter.Services
{
    public class AgentRegistry
    {
        private List<AgentInfo> _agents = new List<AgentInfo>();

        public async Task InitializeAsync()
        {
            // Load agent configurations from the Agents folder
            var agentsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Agents");
            var agentFolders = Directory.GetDirectories(agentsPath);

            foreach (var folder in agentFolders)
            {
                var configPath = Path.Combine(folder, "agent.config.json");
                if (File.Exists(configPath))
                {
                    var configJson = File.ReadAllText(configPath);
                    var agentConfig = JsonSerializer.Deserialize<AgentConfig>(configJson);

                    if (agentConfig != null)
                    {
                        var agentInfo = new AgentInfo
                        {
                            Name = agentConfig.AgentName,
                            Type = agentConfig.AgentType,
                            SupportedLanguages = agentConfig.Capabilities
                                .SelectMany(c => c.SupportedLanguages)
                                .Distinct()
                                .ToList(),
                            Capabilities = agentConfig.Capabilities
                                .Select(c => new AgentCapability
                                {
                                    Name = c.Name,
                                    Description = c.Description,
                                    SupportedLanguages = c.SupportedLanguages,
                                    RequiredContextTypes = c.RequiredContextTypes
                                })
                                .ToList()
                        };

                        _agents.Add(agentInfo);
                    }
                }
            }
        }

        public async Task<List<AgentInfo>> GetAvailableAgentsAsync()
        {
            return await Task.FromResult(_agents);
        }

        public async Task<AgentInfo> GetAgentByNameAsync(string agentName)
        {
            return await Task.FromResult(_agents.FirstOrDefault(a => a.Name == agentName));
        }

        public async Task ShutdownAsync()
        {
            // Clean up resources
            _agents.Clear();
        }
    }
}