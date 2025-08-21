using System;
using System.Collections.Generic;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Result of agent type validation
    /// </summary>
    public class AgentValidationResult
    {
        /// <summary>
        /// Whether the agent type is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// The agent type that was validated
        /// </summary>
        public Type? AgentType { get; set; }

        /// <summary>
        /// Validation errors
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Validation warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Additional validation information
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();

        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        public static AgentValidationResult Success(Type agentType)
        {
            return new AgentValidationResult
            {
                IsValid = true,
                AgentType = agentType
            };
        }

        /// <summary>
        /// Creates a failed validation result
        /// </summary>
        public static AgentValidationResult Failure(Type agentType, params string[] errors)
        {
            return new AgentValidationResult
            {
                IsValid = false,
                AgentType = agentType,
                Errors = new List<string>(errors)
            };
        }

        /// <summary>
        /// Creates a validation result with warnings
        /// </summary>
        public static AgentValidationResult Warning(Type agentType, params string[] warnings)
        {
            return new AgentValidationResult
            {
                IsValid = true,
                AgentType = agentType,
                Warnings = new List<string>(warnings)
            };
        }
    }
}