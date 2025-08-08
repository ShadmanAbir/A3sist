using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeAssist.Agents
{
    public class AgentCommunication
    {
        private readonly Dictionary<string, BaseAgent> _agents = new Dictionary<string, BaseAgent>();
        private readonly Dictionary<string, List<string>> _subscriptions = new Dictionary<string, List<string>>();

        public void RegisterAgent(BaseAgent agent)
        {
            if (agent == null)
                throw new ArgumentNullException(nameof(agent));

            _agents[agent.AgentName] = agent;
        }

        public void UnregisterAgent(string agentName)
        {
            if (string.IsNullOrEmpty(agentName))
                throw new ArgumentNullException(nameof(agentName));

            _agents.Remove(agentName);
        }

        public async Task SendMessageAsync(string sender, string receiver, string message)
        {
            if (!_agents.ContainsKey(receiver))
                throw new ArgumentException($"Receiver agent {receiver} not found.");

            // In a real implementation, this would be more sophisticated
            // For now, we'll just simulate sending a message
            await Task.Delay(100); // Simulate network delay

            // Notify subscribers about the message
            if (_subscriptions.ContainsKey(receiver))
            {
                foreach (var subscriber in _subscriptions[receiver])
                {
                    if (_agents.ContainsKey(subscriber))
                    {
                        // In a real implementation, we would call a method on the subscriber
                        // For now, we'll just log it
                        Console.WriteLine($"Message from {sender} to {receiver} via {subscriber}: {message}");
                    }
                }
            }
        }

        public async Task SendAnalysisRequestAsync(string sender, string analyzerName, string codeToAnalyze)
        {
            if (!_agents.ContainsKey(analyzerName))
                throw new ArgumentException($"Analyzer agent {analyzerName} not found.");

            // In a real implementation, this would be more sophisticated
            // For now, we'll just simulate sending an analysis request
            await Task.Delay(100); // Simulate network delay

            // Notify the analyzer about the analysis request
            Console.WriteLine($"Analysis request from {sender} to {analyzerName}: {codeToAnalyze}");

            // In a real implementation, we would call a method on the analyzer
            // For now, we'll just log it
            Console.WriteLine($"Sending analysis request to {analyzerName}");
        }

        public async Task SendAnalysisResultsAsync(string sender, string receiver, string results)
        {
            if (!_agents.ContainsKey(receiver))
                throw new ArgumentException($"Receiver agent {receiver} not found.");

            // In a real implementation, this would be more sophisticated
            // For now, we'll just simulate sending analysis results
            await Task.Delay(100); // Simulate network delay

            // Notify the receiver about the analysis results
            Console.WriteLine($"Analysis results from {sender} to {receiver}: {results}");

            // In a real implementation, we would call a method on the receiver
            // For now, we'll just log it
            Console.WriteLine($"Sending analysis results to {receiver}");
        }

        public void Subscribe(string subscriber, string publisher)
        {
            if (!_agents.ContainsKey(subscriber))
                throw new ArgumentException($"Subscriber agent {subscriber} not found.");

            if (!_agents.ContainsKey(publisher))
                throw new ArgumentException($"Publisher agent {publisher} not found.");

            if (!_subscriptions.ContainsKey(publisher))
            {
                _subscriptions[publisher] = new List<string>();
            }

            if (!_subscriptions[publisher].Contains(subscriber))
            {
                _subscriptions[publisher].Add(subscriber);
            }
        }

        public void Unsubscribe(string subscriber, string publisher)
        {
            if (_subscriptions.ContainsKey(publisher))
            {
                _subscriptions[publisher].Remove(subscriber);

                if (_subscriptions[publisher].Count == 0)
                {
                    _subscriptions.Remove(publisher);
                }
            }
        }
    }
}