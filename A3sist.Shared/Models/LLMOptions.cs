using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Configuration options for LLM requests
    /// </summary>
    public class LLMOptions
    {
        /// <summary>
        /// Maximum number of tokens to generate
        /// </summary>
        [Range(1, 32000)]
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Temperature for response generation (0.0 to 2.0)
        /// </summary>
        [Range(0.0, 2.0)]
        public double? Temperature { get; set; }

        /// <summary>
        /// Top-p sampling parameter (0.0 to 1.0)
        /// </summary>
        [Range(0.0, 1.0)]
        public double? TopP { get; set; }

        /// <summary>
        /// Frequency penalty (-2.0 to 2.0)
        /// </summary>
        [Range(-2.0, 2.0)]
        public double? FrequencyPenalty { get; set; }

        /// <summary>
        /// Presence penalty (-2.0 to 2.0)
        /// </summary>
        [Range(-2.0, 2.0)]
        public double? PresencePenalty { get; set; }

        /// <summary>
        /// Stop sequences for generation
        /// </summary>
        public List<string> Stop { get; set; }

        /// <summary>
        /// Model to use for the request
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Whether to stream the response
        /// </summary>
        public bool Stream { get; set; }

        public LLMOptions()
        {
            Stop = new List<string>();
            Stream = false;
        }
    }
}