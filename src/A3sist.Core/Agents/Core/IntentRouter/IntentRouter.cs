using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Enums;
using A3sist.Orchastrator.Agents.IntentRouter.Services;
using Task = System.Threading.Tasks.Task;

namespace A3sist.Orchastrator.Agents.IntentRouter
{
    public class IntentRouter : IAgent
    {
        private readonly IntentClassifier _classifier;
        private readonly AgentRegistry _agentRegistry;
        private readonly FailureAnalyzer _failureAnalyzer;

        public string Name => "IntentRouter";
        public AgentType Type => AgentType.IntentRouter;
        public WorkStatus Status { get; private set; }

        public IntentRouter()
        {
            _classifier = new IntentClassifier();
            _agentRegistry = new AgentRegistry();
            _failureAnalyzer = new FailureAnalyzer();
            Status = WorkStatus.Pending;
        }

        public async Task InitializeAsync()
        {
            Status = WorkStatus.InProgress;
            await Task.WhenAll(
                _classifier.InitializeAsync(),
                _agentRegistry.InitializeAsync(),
                _failureAnalyzer.InitializeAsync()
            );
            Status = WorkStatus.Completed;
        }

        public async Task<AgentResult> HandleAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                Status = WorkStatus.InProgress;

                // Classify the intent
                var classification = await _classifier.ClassifyIntentAsync(request.Context?.ToString() ?? request.Prompt);

                // Check for past failures
                var failureAnalysis = await _failureAnalyzer.AnalyzeFailureAsync(request.Context?.ToString() ?? request.Prompt);

                // Determine the best agent
                var routingDecision = await DetermineBestAgentAsync(classification, failureAnalysis);

                Status = WorkStatus.Completed;
                return AgentResult.CreateSuccess("Intent routing completed", JsonSerializer.Serialize(routingDecision), Name);
            }
            catch (Exception ex)
            {
                Status = WorkStatus.Failed;
                return AgentResult.CreateFailure($"IntentRouter error: {ex.Message}", ex, Name);
            }
        }

        public async Task<bool> CanHandleAsync(AgentRequest request)
        {
            if (request?.Prompt == null) return false;

            var prompt = request.Prompt.ToLowerInvariant();
            var routingKeywords = new[] { "route", "intent", "classify", "analyze", "determine", "which", "what", "how" };
            
            return routingKeywords.Any(keyword => prompt.Contains(keyword)) || 
                   request.Context?.ContainsKey("requiresRouting") == true;
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
            Status = WorkStatus.InProgress;
            await Task.WhenAll(
                _classifier.ShutdownAsync(),
                _agentRegistry.ShutdownAsync(),
                _failureAnalyzer.ShutdownAsync()
            );
            Status = WorkStatus.Completed;
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