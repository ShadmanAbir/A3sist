using A3sist.Shared.Enums;
using A3sist.Shared.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace A3sist.Shared.Messaging
{
    /// <summary>
    /// Enhanced agent request with context and metadata support
    /// </summary>
    public class AgentRequest
    {
        /// <summary>
        /// Unique identifier for the request
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The prompt or instruction for the agent
        /// </summary>
        [Required]
        public string Prompt { get; set; }

        /// <summary>
        /// Path to the file being processed (if applicable)
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Content to be processed by the agent
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Additional context information for the request
        /// </summary>
        public Dictionary<string, object> Context { get; set; }

        /// <summary>
        /// Preferred agent type for handling this request
        /// </summary>
        public AgentType? PreferredAgentType { get; set; }

        /// <summary>
        /// LLM configuration options for this request
        /// </summary>
        public LLMOptions LLMOptions { get; set; }

        /// <summary>
        /// When the request was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User identifier who made the request
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Priority level of the request
        /// </summary>
        public WorkflowPriority Priority { get; set; }

        /// <summary>
        /// Timeout for the request processing
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Additional metadata for the request
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        public AgentRequest()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            Context = new Dictionary<string, object>();
            Metadata = new Dictionary<string, object>();
            Priority = WorkflowPriority.Normal;
        }

        public AgentRequest(string prompt) : this()
        {
            Prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
        }
    }
}