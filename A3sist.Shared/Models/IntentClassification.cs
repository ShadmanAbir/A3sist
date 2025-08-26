using A3sist.Shared.Enums;
using System;
using System.Collections.Generic;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Result of intent classification
    /// </summary>
    public class IntentClassification
    {
        /// <summary>
        /// The classified intent
        /// </summary>
        public string Intent { get; set; } = string.Empty;

        /// <summary>
        /// Confidence score (0.0 to 1.0)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Detected programming language
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// Suggested agent type for handling the request
        /// </summary>
        public AgentType SuggestedAgentType { get; set; }

        /// <summary>
        /// Additional context extracted from the request
        /// </summary>
        public Dictionary<string, object> Context { get; set; } = new();

        /// <summary>
        /// Keywords that influenced the classification
        /// </summary>
        public List<string> Keywords { get; set; } = new();

        /// <summary>
        /// Alternative intents with their confidence scores
        /// </summary>
        public List<AlternativeIntent> Alternatives { get; set; } = new();

        /// <summary>
        /// Whether the classification is considered reliable
        /// </summary>
        public bool IsReliable => Confidence >= 0.7;
    }

    /// <summary>
    /// Alternative intent classification
    /// </summary>
    public class AlternativeIntent
    {
        /// <summary>
        /// The alternative intent
        /// </summary>
        public string Intent { get; set; } = string.Empty;

        /// <summary>
        /// Confidence score for this alternative
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Suggested agent type for this alternative
        /// </summary>
        public AgentType SuggestedAgentType { get; set; }
    }
}