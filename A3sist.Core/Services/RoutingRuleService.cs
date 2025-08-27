using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Core.Services
{
    /// <summary>
    /// Service for managing and evaluating routing rules
    /// </summary>
    public class RoutingRuleService : IRoutingRuleService
    {
        private readonly ILogger<RoutingRuleService> _logger;
        private readonly ConcurrentDictionary<string, RoutingRule> _rules;

        public RoutingRuleService(ILogger<RoutingRuleService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _rules = new ConcurrentDictionary<string, RoutingRule>();
            
            InitializeDefaultRules();
        }

        /// <summary>
        /// Evaluates routing rules for a classification
        /// </summary>
        public async Task<RoutingDecision> EvaluateRulesAsync(IntentClassification classification, 
            IEnumerable<IAgent> availableAgents, CancellationToken cancellationToken = default)
        {
            if (classification == null)
                throw new ArgumentNullException(nameof(classification));
            if (availableAgents == null)
                throw new ArgumentNullException(nameof(availableAgents));

            _logger.LogDebug("Evaluating routing rules for intent '{Intent}' with confidence {Confidence:F2}", 
                classification.Intent, classification.Confidence);

            var agentList = availableAgents.ToList();
            if (!agentList.Any())
            {
                throw new InvalidOperationException("No agents available for routing");
            }

            try
            {
                // Get applicable rules sorted by priority
                var applicableRules = await GetApplicableRulesAsync(classification);
                
                _logger.LogDebug("Found {RuleCount} applicable routing rules", applicableRules.Count);

                // Try each rule in priority order
                foreach (var rule in applicableRules)
                {
                    var decision = await EvaluateRuleAsync(rule, classification, agentList, cancellationToken);
                    if (decision != null)
                    {
                        // Update rule usage statistics
                        await UpdateRuleUsageAsync(rule.Id);
                        
                        _logger.LogDebug("Routing rule '{RuleName}' matched, targeting agent '{TargetAgent}'", 
                            rule.Name, decision.TargetAgent);
                        
                        return decision;
                    }
                }

                // No rules matched, use default routing logic
                _logger.LogDebug("No routing rules matched, using default routing logic");
                return await CreateDefaultRoutingDecisionAsync(classification, agentList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating routing rules for intent '{Intent}'", classification.Intent);
                
                // Fallback to simple routing
                return await CreateFallbackRoutingDecisionAsync(classification, agentList);
            }
        }

        /// <summary>
        /// Adds a new routing rule
        /// </summary>
        public async Task AddRuleAsync(RoutingRule rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            if (string.IsNullOrWhiteSpace(rule.Id))
                rule.Id = Guid.NewGuid().ToString();

            rule.ModifiedAt = DateTime.UtcNow;

            _rules.AddOrUpdate(rule.Id, rule, (key, existing) =>
            {
                _logger.LogInformation("Updating existing routing rule '{RuleName}' ({RuleId})", rule.Name, rule.Id);
                return rule;
            });

            _logger.LogInformation("Added routing rule '{RuleName}' ({RuleId}) with priority {Priority}", 
                rule.Name, rule.Id, rule.Priority);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Removes a routing rule
        /// </summary>
        public async Task RemoveRuleAsync(string ruleId)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
                return;

            if (_rules.TryRemove(ruleId, out var removedRule))
            {
                _logger.LogInformation("Removed routing rule '{RuleName}' ({RuleId})", removedRule.Name, ruleId);
            }
            else
            {
                _logger.LogWarning("Attempted to remove non-existent routing rule ({RuleId})", ruleId);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Initializes default routing rules
        /// </summary>
        private void InitializeDefaultRules()
        {
            // Add all default rules
            var defaultRules = new[] { refactorRule, csharpRule, jsRule, pythonRule };
            foreach (var rule in defaultRules)
            {
                _rules.TryAdd(rule.Id, rule);
            }

            _logger.LogInformation("Initialized {RuleCount} default routing rules", defaultRules.Length);
        }

        /// <summary>
        /// Gets applicable rules for a classification
        /// </summary>
        private async Task<List<RoutingRule>> GetApplicableRulesAsync(IntentClassification classification)
        {
            await Task.CompletedTask;

            var applicableRules = new List<RoutingRule>();

            foreach (var rule in _rules.Values.Where(r => r.IsEnabled))
            {
                if (await IsRuleApplicableAsync(rule, classification))
                {
                    applicableRules.Add(rule);
                }
            }

            // Sort by priority (descending) then by success rate (descending)
            return applicableRules
                .OrderByDescending(r => r.Priority)
                .ThenByDescending(r => r.SuccessRate)
                .ToList();
        }

        /// <summary>
        /// Checks if a rule is applicable to a classification
        /// </summary>
        private async Task<bool> IsRuleApplicableAsync(RoutingRule rule, IntentClassification classification)
        {
            await Task.CompletedTask;

            if (!rule.Conditions.Any())
                return false;

            // All conditions must be met for the rule to apply
            foreach (var condition in rule.Conditions)
            {
                if (!EvaluateCondition(condition, classification))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Evaluates a single routing condition
        /// </summary>
        private bool EvaluateCondition(RoutingCondition condition, IntentClassification classification)
        {
            var fieldValue = GetFieldValue(condition.Field, classification);
            if (fieldValue == null)
                return false;

            var comparison = condition.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            return condition.Operator switch
            {
                ConditionOperator.Equals => string.Equals(fieldValue, condition.Value, comparison),
                ConditionOperator.Contains => fieldValue.Contains(condition.Value, comparison),
                ConditionOperator.StartsWith => fieldValue.StartsWith(condition.Value, comparison),
                ConditionOperator.EndsWith => fieldValue.EndsWith(condition.Value, comparison),
                ConditionOperator.Regex => EvaluateRegexCondition(fieldValue, condition.Value),
                ConditionOperator.In => EvaluateInCondition(fieldValue, condition.Value, comparison),
                ConditionOperator.NotIn => !EvaluateInCondition(fieldValue, condition.Value, comparison),
                ConditionOperator.GreaterThan => EvaluateNumericCondition(fieldValue, condition.Value, (a, b) => a > b),
                ConditionOperator.LessThan => EvaluateNumericCondition(fieldValue, condition.Value, (a, b) => a < b),
                _ => false
            };
        }

        /// <summary>
        /// Gets the field value from the classification
        /// </summary>
        private string? GetFieldValue(string field, IntentClassification classification)
        {
            return field.ToLowerInvariant() switch
            {
                "intent" => classification.Intent,
                "language" => classification.Language,
                "confidence" => classification.Confidence.ToString(),
                _ => classification.Context.TryGetValue(field, out var value) ? value?.ToString() : null
            };
        }

        /// <summary>
        /// Evaluates a regex condition
        /// </summary>
        private bool EvaluateRegexCondition(string fieldValue, string pattern)
        {
            try
            {
                return Regex.IsMatch(fieldValue, pattern, RegexOptions.IgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error evaluating regex pattern '{Pattern}'", pattern);
                return false;
            }
        }

        /// <summary>
        /// Evaluates an "in" condition
        /// </summary>
        private bool EvaluateInCondition(string fieldValue, string valueList, StringComparison comparison)
        {
            var values = valueList.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Trim());
            
            return values.Any(v => string.Equals(fieldValue, v, comparison));
        }

        /// <summary>
        /// Evaluates a numeric condition
        /// </summary>
        private bool EvaluateNumericCondition(string fieldValue, string conditionValue, Func<double, double, bool> comparer)
        {
            if (double.TryParse(fieldValue, out var fieldNum) && double.TryParse(conditionValue, out var conditionNum))
            {
                return comparer(fieldNum, conditionNum);
            }
            return false;
        }

        /// <summary>
        /// Evaluates a single rule and returns a routing decision if it matches
        /// </summary>
        private async Task<RoutingDecision?> EvaluateRuleAsync(RoutingRule rule, IntentClassification classification, 
            List<IAgent> availableAgents, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            // Find target agent
            IAgent? targetAgent = null;

            if (!string.IsNullOrWhiteSpace(rule.TargetAgentName))
            {
                targetAgent = availableAgents.FirstOrDefault(a => a.Name == rule.TargetAgentName);
            }
            else
            {
                targetAgent = availableAgents.FirstOrDefault(a => a.Type == rule.TargetAgentType);
            }

            if (targetAgent == null)
            {
                _logger.LogDebug("Target agent not found for rule '{RuleName}'", rule.Name);
                return null;
            }

            // Calculate confidence with boost
            var confidence = Math.Min(classification.Confidence + rule.ConfidenceBoost, 1.0);

            return new RoutingDecision
            {
                TargetAgent = targetAgent.Name,
                TargetAgentType = targetAgent.Type,
                Intent = classification.Intent,
                Confidence = confidence,
                Reason = $"Matched routing rule: {rule.Name}",
                Metadata = new Dictionary<string, object>
                {
                    ["RuleId"] = rule.Id,
                    ["RuleName"] = rule.Name,
                    ["RulePriority"] = rule.Priority,
                    ["OriginalConfidence"] = classification.Confidence,
                    ["ConfidenceBoost"] = rule.ConfidenceBoost
                }
            };
        }

        /// <summary>
        /// Creates a default routing decision when no rules match
        /// </summary>
        private async Task<RoutingDecision> CreateDefaultRoutingDecisionAsync(IntentClassification classification, List<IAgent> availableAgents)
        {
            await Task.CompletedTask;

            // Try to find an agent of the suggested type
            var suggestedAgent = availableAgents.FirstOrDefault(a => a.Type == classification.SuggestedAgentType);
            if (suggestedAgent != null)
            {
                return new RoutingDecision
                {
                    TargetAgent = suggestedAgent.Name,
                    TargetAgentType = suggestedAgent.Type,
                    Intent = classification.Intent,
                    Confidence = classification.Confidence * 0.9, // Slightly lower confidence
                    Reason = "Default routing based on suggested agent type"
                };
            }

            // Fallback to first available agent
            var fallbackAgent = availableAgents.First();
            return new RoutingDecision
            {
                TargetAgent = fallbackAgent.Name,
                TargetAgentType = fallbackAgent.Type,
                Intent = classification.Intent,
                Confidence = 0.5, // Medium confidence for fallback
                Reason = "Fallback routing to first available agent",
                IsFallback = true
            };
        }

        /// <summary>
        /// Creates a fallback routing decision when rule evaluation fails
        /// </summary>
        private async Task<RoutingDecision> CreateFallbackRoutingDecisionAsync(IntentClassification classification, List<IAgent> availableAgents)
        {
            await Task.CompletedTask;

            var fallbackAgent = availableAgents.First();
            return new RoutingDecision
            {
                TargetAgent = fallbackAgent.Name,
                TargetAgentType = fallbackAgent.Type,
                Intent = classification.Intent,
                Confidence = 0.3, // Low confidence for error fallback
                Reason = "Error fallback routing",
                IsFallback = true,
                Metadata = new Dictionary<string, object>
                {
                    ["Error"] = "Rule evaluation failed"
                }
            };
        }

        /// <summary>
        /// Updates rule usage statistics
        /// </summary>
        private async Task UpdateRuleUsageAsync(string ruleId)
        {
            await Task.CompletedTask;

            if (_rules.TryGetValue(ruleId, out var rule))
            {
                rule.UsageCount++;
                rule.ModifiedAt = DateTime.UtcNow;
                
                _logger.LogTrace("Updated usage count for rule '{RuleName}' to {UsageCount}", rule.Name, rule.UsageCount);
            }
        }
    }
}