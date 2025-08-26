using A3sist.Shared.Enums;
using System;
using System.Collections.Generic;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Rule for routing requests to agents
    /// </summary>
    public class RoutingRule
    {
        /// <summary>
        /// Unique identifier for the rule
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the rule
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of what the rule does
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Priority of the rule (higher numbers = higher priority)
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Whether the rule is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Conditions that must be met for the rule to apply
        /// </summary>
        public List<RoutingCondition> Conditions { get; set; } = new();

        /// <summary>
        /// Target agent type when rule matches
        /// </summary>
        public AgentType TargetAgentType { get; set; }

        /// <summary>
        /// Specific agent name (optional, overrides agent type)
        /// </summary>
        public string? TargetAgentName { get; set; }

        /// <summary>
        /// Confidence boost to apply when this rule matches
        /// </summary>
        public double ConfidenceBoost { get; set; }

        /// <summary>
        /// When the rule was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the rule was last modified
        /// </summary>
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Number of times this rule has been applied
        /// </summary>
        public long UsageCount { get; set; }

        /// <summary>
        /// Success rate of this rule (0.0 to 1.0)
        /// </summary>
        public double SuccessRate { get; set; } = 1.0;
    }

    /// <summary>
    /// Condition for a routing rule
    /// </summary>
    public class RoutingCondition
    {
        /// <summary>
        /// Field to check (e.g., "Intent", "Language", "Prompt")
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// Operator for the condition
        /// </summary>
        public ConditionOperator Operator { get; set; }

        /// <summary>
        /// Value to compare against
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Whether the condition is case sensitive
        /// </summary>
        public bool CaseSensitive { get; set; }
    }

    /// <summary>
    /// Operators for routing conditions
    /// </summary>
    public enum ConditionOperator
    {
        /// <summary>
        /// Exact match
        /// </summary>
        Equals,

        /// <summary>
        /// Contains substring
        /// </summary>
        Contains,

        /// <summary>
        /// Starts with
        /// </summary>
        StartsWith,

        /// <summary>
        /// Ends with
        /// </summary>
        EndsWith,

        /// <summary>
        /// Regular expression match
        /// </summary>
        Regex,

        /// <summary>
        /// Greater than (for numeric values)
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Less than (for numeric values)
        /// </summary>
        LessThan,

        /// <summary>
        /// In list of values
        /// </summary>
        In,

        /// <summary>
        /// Not in list of values
        /// </summary>
        NotIn
    }
}