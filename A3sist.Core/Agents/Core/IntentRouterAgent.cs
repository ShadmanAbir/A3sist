using A3sist.Core.Agents.Base;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Core.Agents.Core
{
    /// <summary>
    /// Enhanced intent router agent that analyzes requests and routes them to appropriate agents
    /// </summary>
    public class IntentRouterAgent : BaseAgent
    {
        private readonly IIntentClassifier _intentClassifier;
        private readonly IRoutingRuleService _routingRuleService;
        private readonly IAgentManager _agentManager;

        public override string Name => "IntentRouter";
        public override AgentType Type => AgentType.IntentRouter;

        public IntentRouterAgent(
            IIntentClassifier intentClassifier,
            IRoutingRuleService routingRuleService,
            IAgentManager agentManager,
            ILogger<IntentRouterAgent> logger,
            IAgentConfiguration configuration)
            : base(logger, configuration)
        {
            _intentClassifier = intentClassifier ?? throw new ArgumentNullException(nameof(intentClassifier));
            _routingRuleService = routingRuleService ?? throw new ArgumentNullException(nameof(routingRuleService));
            _agentManager = agentManager ?? throw new ArgumentNullException(nameof(agentManager));
        }

        /// <summary>
        /// Determines if this agent can handle the request
        /// </summary>
        protected override async Task<bool> CanHandleRequestAsync(AgentRequest request)
        {
            if (request == null)
                return false;

            // IntentRouter can handle any request for intent classification and routing
            await Task.CompletedTask;
            return true;
        }

        /// <summary>
        /// Handles the intent classification and routing request
        /// </summary>
        protected override async Task<AgentResult> HandleRequestAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            Logger.LogInformation("Processing intent classification request {RequestId}", request.Id);

            try
            {
                // Step 1: Classify the intent
                var classification = await ClassifyIntentAsync(request, cancellationToken);
                
                Logger.LogDebug("Intent classified as '{Intent}' with confidence {Confidence:F2} for request {RequestId}", 
                    classification.Intent, classification.Confidence, request.Id);

                // Step 2: Get available agents
                var availableAgents = await _agentManager.GetAgentsAsync();
                
                if (!availableAgents.Any())
                {
                    Logger.LogWarning("No agents available for routing request {RequestId}", request.Id);
                    return AgentResult.CreateFailure("No agents available for routing");
                }

                // Step 3: Apply routing rules to determine the best agent
                var routingDecision = await _routingRuleService.EvaluateRulesAsync(
                    classification, availableAgents, cancellationToken);

                Logger.LogInformation("Routing decision made for request {RequestId}: target agent '{TargetAgent}' with confidence {Confidence:F2}", 
                    request.Id, routingDecision.TargetAgent, routingDecision.Confidence);

                // Step 4: Validate the routing decision
                var validationResult = await ValidateRoutingDecisionAsync(routingDecision, availableAgents);
                if (!validationResult.IsValid)
                {
                    Logger.LogWarning("Routing decision validation failed for request {RequestId}: {Reason}", 
                        request.Id, validationResult.Reason);
                    
                    // Try to find an alternative
                    var fallbackDecision = await FindFallbackAgentAsync(classification, availableAgents, cancellationToken);
                    if (fallbackDecision != null)
                    {
                        routingDecision = fallbackDecision;
                        Logger.LogInformation("Using fallback routing decision for request {RequestId}: target agent '{TargetAgent}'", 
                            request.Id, routingDecision.TargetAgent);
                    }
                    else
                    {
                        return AgentResult.CreateFailure($"No suitable agent found: {validationResult.Reason}");
                    }
                }

                // Step 5: Create the result with routing information
                var result = AgentResult.CreateSuccess("Intent classification and routing completed");
                result.Metadata = new Dictionary<string, object>
                {
                    ["Classification"] = classification,
                    ["RoutingDecision"] = routingDecision,
                    ["AvailableAgentCount"] = availableAgents.Count()
                };

                // Add follow-up question if confidence is low
                if (classification.Confidence < _intentClassifier.ConfidenceThreshold)
                {
                    result.Metadata["FollowUpQuestion"] = GenerateFollowUpQuestion(classification);
                }

                Logger.LogInformation("Intent routing completed successfully for request {RequestId}", request.Id);
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing intent classification request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Intent routing failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Classifies the intent of the request
        /// </summary>
        private async Task<IntentClassification> ClassifyIntentAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                return await _intentClassifier.ClassifyAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error classifying intent for request {RequestId}", request.Id);
                
                // Return a fallback classification
                return new IntentClassification
                {
                    Intent = "unknown",
                    Confidence = 0.1,
                    Language = DetectLanguageFromFilePath(request.FilePath),
                    SuggestedAgentType = AgentType.Unknown,
                    Context = new Dictionary<string, object> { ["Error"] = ex.Message }
                };
            }
        }

        /// <summary>
        /// Validates that the routing decision is feasible
        /// </summary>
        private async Task<ValidationResult> ValidateRoutingDecisionAsync(RoutingDecision decision, IEnumerable<IAgent> availableAgents)
        {
            await Task.CompletedTask;

            if (string.IsNullOrWhiteSpace(decision.TargetAgent))
            {
                return new ValidationResult { IsValid = false, Reason = "No target agent specified" };
            }

            var targetAgent = availableAgents.FirstOrDefault(a => a.Name == decision.TargetAgent);
            if (targetAgent == null)
            {
                return new ValidationResult { IsValid = false, Reason = $"Target agent '{decision.TargetAgent}' not found" };
            }

            if (decision.Confidence < 0.3)
            {
                return new ValidationResult { IsValid = false, Reason = "Routing confidence too low" };
            }

            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// Finds a fallback agent when the primary routing fails
        /// </summary>
        private async Task<RoutingDecision?> FindFallbackAgentAsync(IntentClassification classification, 
            IEnumerable<IAgent> availableAgents, CancellationToken cancellationToken)
        {
            Logger.LogDebug("Looking for fallback agent for intent '{Intent}'", classification.Intent);

            // Try to find an agent of the suggested type
            var agentsByType = availableAgents.Where(a => a.Type == classification.SuggestedAgentType).ToList();
            if (agentsByType.Any())
            {
                var fallbackAgent = agentsByType.First();
                return new RoutingDecision
                {
                    TargetAgent = fallbackAgent.Name,
                    TargetAgentType = fallbackAgent.Type,
                    Intent = classification.Intent,
                    Confidence = 0.5, // Medium confidence for fallback
                    Reason = "Fallback routing based on agent type",
                    IsFallback = true
                };
            }

            // If no specific type match, try language-based routing
            if (!string.IsNullOrEmpty(classification.Language))
            {
                var languageAgent = GetAgentForLanguage(classification.Language, availableAgents);
                if (languageAgent != null)
                {
                    return new RoutingDecision
                    {
                        TargetAgent = languageAgent.Name,
                        TargetAgentType = languageAgent.Type,
                        Intent = classification.Intent,
                        Confidence = 0.4, // Lower confidence for language-based fallback
                        Reason = $"Fallback routing based on language: {classification.Language}",
                        IsFallback = true
                    };
                }
            }

            Logger.LogWarning("No fallback agent found for intent '{Intent}'", classification.Intent);
            return null;
        }

        /// <summary>
        /// Gets an appropriate agent for a programming language
        /// </summary>
        private IAgent? GetAgentForLanguage(string language, IEnumerable<IAgent> availableAgents)
        {
            var languageLower = language.ToLowerInvariant();
            
            return languageLower switch
            {
                "c#" or "csharp" => availableAgents.FirstOrDefault(a => a.Type == AgentType.CSharp),
                "javascript" or "js" or "typescript" or "ts" => availableAgents.FirstOrDefault(a => a.Type == AgentType.JavaScript),
                "python" or "py" => availableAgents.FirstOrDefault(a => a.Type == AgentType.Python),
                _ => null
            };
        }

        /// <summary>
        /// Detects programming language from file path
        /// </summary>
        private string DetectLanguageFromFilePath(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return "unknown";

            var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            
            return extension switch
            {
                ".cs" => "csharp",
                ".js" => "javascript",
                ".ts" => "typescript",
                ".py" => "python",
                ".java" => "java",
                ".cpp" or ".cc" or ".cxx" => "cpp",
                ".c" => "c",
                ".go" => "go",
                ".rs" => "rust",
                _ => "unknown"
            };
        }

        /// <summary>
        /// Generates a follow-up question for low-confidence classifications
        /// </summary>
        private string GenerateFollowUpQuestion(IntentClassification classification)
        {
            if (classification.Alternatives.Any())
            {
                var alternatives = string.Join(", ", classification.Alternatives.Take(3).Select(a => a.Intent));
                return $"I'm not entirely sure about your intent. Did you mean: {alternatives}? Please clarify what you'd like me to help you with.";
            }

            return "Could you please provide more details about what you'd like me to help you with?";
        }

        /// <summary>
        /// Validation result for routing decisions
        /// </summary>
        private class ValidationResult
        {
            public bool IsValid { get; set; }
            public string Reason { get; set; } = string.Empty;
        }
    }
}