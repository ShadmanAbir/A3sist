using A3sist.Shared.Enums;
using System;
using System.Collections.Generic;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Decision made by the intent router for request routing
    /// </summary>
    public class RoutingDecision
    {
        /// <summary>
        /// Name of the target agent to handle the request
        /// </summary>
        public string TargetAgent { get; set; } = string.Empty;

        /// <summary>
        /// Type of the target agent
        /// </summary>
        public AgentType TargetAgentType { get; set; }

        /// <summary>
        /// The classified intent
        /// </summary>
        public string Intent { get; set; } = string.Empty;

        /// <summary>
        /// Confidence in the routing decision
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Reason for the routing decision
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Alternative routing options
        /// </summary>
        public List<AlternativeRoute> Alternatives { get; set; } = new();

        /// <summary>
        /// Whether fallback routing was used
        /// </summary>
        public bool IsFallback { get; set; }

        /// <summary>
        /// Follow-up question or clarification needed
        /// </summary>
        public string? FollowUpQuestion { get; set; }

        /// <summary>
        /// Additional metadata for the routing decision
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// When the routing decision was made
        /// </summary>
        public DateTime DecisionTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Alternative routing option
    /// </summary>
    public class AlternativeRoute
    {
        /// <summary>
        /// Name of the alternative agent
        /// </summary>
        public string AgentName { get; set; } = string.Empty;

        /// <summary>
        /// Type of the alternative agent
        /// </summary>
        public AgentType AgentType { get; set; }

        /// <summary>
        /// Confidence score for this alternative
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Reason for considering this alternative
        /// </summary>
        public string Reason { get; set; } = string.Empty;
    }
}