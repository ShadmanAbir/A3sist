using System;
using System.Collections.Generic;
using System.Management.Instrumentation;
using System.Threading.Tasks;

namespace A3sist.Orchastrator.Services
{
    public class ContextRouter
    {
        private readonly Dictionary<string, Type> _agentRegistry = new Dictionary<string, Type>();
        private readonly Dictionary<string, List<string>> _contextAgentMap = new Dictionary<string, List<string>>();
        private readonly ContextSerializer _serializer;
        private readonly ContextValidator _validator;

        public ContextRouter(ContextSerializer serializer, ContextValidator validator)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public void RegisterAgent(string contextType, Type agentType)
        {
            if (string.IsNullOrEmpty(contextType))
                throw new ArgumentNullException(nameof(contextType));

            if (agentType == null)
                throw new ArgumentNullException(nameof(agentType));

            if (!typeof(BaseEvent).IsAssignableFrom(agentType))
                throw new ArgumentException("Agent type must inherit from BaseAgent");

            _agentRegistry[contextType] = agentType;

            if (!_contextAgentMap.ContainsKey(contextType))
            {
                _contextAgentMap[contextType] = new List<string>();
            }

            _contextAgentMap[contextType].Add(agentType.Name);
        }

        public async Task RouteContextAsync(string contextType, string serializedContext)
        {
            if (string.IsNullOrEmpty(contextType))
                throw new ArgumentNullException(nameof(contextType));

            if (string.IsNullOrEmpty(serializedContext))
                throw new ArgumentNullException(nameof(serializedContext));

            // Validate the context
            if (!_validator.ValidateContext(contextType, serializedContext))
            {
                throw new InvalidOperationException("Invalid context data");
            }

            // Deserialize the context
            var context = _serializer.DeserializeContext(contextType, serializedContext);

            // Get appropriate agents for this context
            if (_contextAgentMap.TryGetValue(contextType, out var agentNames))
            {
                foreach (var agentName in agentNames)
                {
                    if (_agentRegistry.TryGetValue(agentName, out var agentType))
                    {
                        // In a real implementation, we would create and execute the agent here
                        Console.WriteLine($"Routing context to agent: {agentName}");
                        await Task.Delay(100); // Simulate processing delay
                    }
                }
            }
            else
            {
                throw new InvalidOperationException($"No agents registered for context type: {contextType}");
            }
        }

        public IEnumerable<string> GetRegisteredContextTypes()
        {
            return _contextAgentMap.Keys;
        }

        public IEnumerable<string> GetAgentsForContext(string contextType)
        {
            if (string.IsNullOrEmpty(contextType))
                throw new ArgumentNullException(nameof(contextType));

            if (_contextAgentMap.TryGetValue(contextType, out var agents))
            {
                return agents;
            }

            return new List<string>();
        }
    }
}