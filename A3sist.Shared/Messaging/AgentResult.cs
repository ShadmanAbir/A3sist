using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace A3sist.Shared.Messaging
{
    /// <summary>
    /// Enhanced agent result with detailed success/failure information
    /// </summary>
    public class AgentResult
    {
        /// <summary>
        /// Indicates whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Human-readable message describing the result
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The content produced by the agent (if applicable)
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Additional metadata about the result
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// Exception that occurred during processing (if any)
        /// </summary>
        [JsonIgnore]
        public Exception Exception { get; set; }

        /// <summary>
        /// Time taken to process the request
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }

        /// <summary>
        /// Name of the agent that produced this result
        /// </summary>
        public string AgentName { get; set; }

        /// <summary>
        /// When the result was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Confidence level of the result (0.0 to 1.0)
        /// </summary>
        public double? Confidence { get; set; }

        /// <summary>
        /// Whether the result requires human review
        /// </summary>
        public bool RequiresReview { get; set; }

        /// <summary>
        /// Suggestions for follow-up actions
        /// </summary>
        public List<string> Suggestions { get; set; }

        /// <summary>
        /// Error code for failed operations
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// Detailed error information for debugging
        /// </summary>
        public string ErrorDetails { get; set; }

        public AgentResult()
        {
            CreatedAt = DateTime.UtcNow;
            Metadata = new Dictionary<string, object>();
            Suggestions = new List<string>();
        }

        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static AgentResult CreateSuccess(string message, string? content = null, string? agentName = null)
        {
            return new AgentResult
            {
                Success = true,
                Message = message,
                Content = content,
                AgentName = agentName
            };
        }

        /// <summary>
        /// Creates a failed result
        /// </summary>
        public static AgentResult CreateFailure(string message, Exception? exception = null, string? agentName = null, string? errorCode = null)
        {
            return new AgentResult
            {
                Success = false,
                Message = message,
                Exception = exception,
                AgentName = agentName,
                ErrorCode = errorCode,
                ErrorDetails = exception?.ToString()
            };
        }

        /// <summary>
        /// Creates a result that requires review
        /// </summary>
        public static AgentResult CreateForReview(string message, string content, string? agentName = null)
        {
            return new AgentResult
            {
                Success = true,
                Message = message,
                Content = content,
                AgentName = agentName,
                RequiresReview = true
            };
        }

        /// <summary>
        /// Creates an error result (alias for CreateFailure)
        /// </summary>
        public static AgentResult Error(string message, string? agentName = null, Exception? exception = null)
        {
            return CreateFailure(message, exception, agentName);
        }
    }
}