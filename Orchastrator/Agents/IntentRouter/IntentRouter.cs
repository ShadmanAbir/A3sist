using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Enums;
using A3sist.Agents.IntentRouter.Services;

namespace A3sist.Agents.IntentRouter
{
    public class IntentRouter : IAgent
    {
        private readonly IntentClassifier _classifier;
        private readonly AgentRegistry _agentRegistry;
        private readonly FailureAnalyzer _failureAnalyzer;

        public string Name => "IntentRouter";
        public AgentType Type => AgentType.IntentRouter;
        public TaskStatus Status { get; private set; }

        public IntentRouter()
        {
            _classifier = new IntentClassifier();
            _agentRegistry = new AgentRegistry();
            _failureAnalyzer = new FailureAnalyzer();
            Status = TaskStatus.Pending;
        }

        public async Task InitializeAsync()
        {
            Status = TaskStatus.InProgress;
            await Task.WhenAll(
                _classifier.InitializeAsync(),
                _agentRegistry.InitializeAsync(),
                _failureAnalyzer.InitializeAsync()
            );
            Status = TaskStatus.Completed;
        }

        public async Task<AgentResponse> ExecuteAsync(AgentRequest request)
        {
            var response = new AgentResponse
            {
                RequestId = request.RequestId,
                AgentName = Name,
                TaskName = request.TaskName
            };

            try
            {
                Status = TaskStatus.InProgress;

                if (request.TaskName.ToLower() != "routeintent")
                {
                    throw new NotSupportedException($"Task {request.TaskName} is not supported by this agent");
                }

                // Classify the intent
                var classification = await _classifier.ClassifyIntentAsync(request.Context);

                // Check for past failures
                var failureAnalysis = await _failureAnalyzer.AnalyzeFailureAsync(request.Context);

                // Determine the best agent
                var routingDecision = await DetermineBestAgentAsync(classification, failureAnalysis);

                // Prepare the response
                response.Result = JsonSerializer.Serialize(routingDecision);
                response.IsSuccess = true;
                Status = TaskStatus.Completed;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessage = ex.Message;
                Status = TaskStatus.Failed;
            }

            return response;
        }

        private async Task<RoutingDecision> DetermineBestAgentAsync(IntentClassification classification, FailureAnalysis failureAnalysis)
        {
            // Get all available agents
            var availableAgents = await _agentRegistry.GetAvailableAgentsAsync();

            // Filter agents by language
            var languageAgents = availableAgents
                .Where(a => a.SupportedLanguages.Contains(classification.Language))
                .ToList();

            // Find the best matching agent
            var bestAgent = languageAgents
                .OrderByDescending(a => a.Capabilities.Count(c =>
                    c.Name.Equals(classification.Intent, StringComparison.OrdinalIgnoreCase)))
                .FirstOrDefault();

            if (bestAgent == null)
            {
                throw new InvalidOperationException($"No suitable agent found for intent: {classification.Intent}");
            }

            // Check for past failures with this agent
            if (failureAnalysis.HasFailedBefore && failureAnalysis.FailedAgent == bestAgent.Name)
            {
                // Fall back to a different agent if available
                var fallbackAgent = languageAgents
                    .Where(a => a.Name != bestAgent.Name)
                    .OrderByDescending(a => a.Capabilities.Count(c =>
                        c.Name.Equals(classification.Intent, StringComparison.OrdinalIgnoreCase)))
                    .FirstOrDefault();

                if (fallbackAgent != null)
                {
                    bestAgent = fallbackAgent;
                }
            }

            return new RoutingDecision
            {
                TargetAgent = bestAgent.Name,
                Intent = classification.Intent,
                Confidence = classification.Confidence,
                FollowUp = classification.Confidence < 0.8f ?
                    "Could you please clarify the task or provide more context?" : null
            };
        }

        public async Task ShutdownAsync()
        {
            Status = TaskStatus.InProgress;
            await Task.WhenAll(
                _classifier.ShutdownAsync(),
                _agentRegistry.ShutdownAsync(),
                _failureAnalyzer.ShutdownAsync()
            );
            Status = TaskStatus.Completed;
        }

        public async Task<AgentResponse> HandleMessageAsync(TaskMessage message)
        {
            // Handle incoming messages (e.g., from other agents)
            return await Task.FromResult(new AgentResponse
            {
                RequestId = message.MessageId,
                AgentName = Name,
                TaskName = "MessageHandling",
                Result = $"Message from {message.Sender} received",
                IsSuccess = true
            });
        }
    }

    public class RoutingDecision
    {
        public string TargetAgent { get; set; }
        public string Intent { get; set; }
        public float Confidence { get; set; }
        public string FollowUp { get; set; }
    }
}