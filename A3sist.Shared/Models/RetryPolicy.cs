using System;
using System.ComponentModel.DataAnnotations;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Retry policy configuration for agent operations
    /// </summary>
    public class RetryPolicy
    {
        /// <summary>
        /// Maximum number of retry attempts
        /// </summary>
        [Range(0, 10)]
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Initial delay between retries
        /// </summary>
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Maximum delay between retries
        /// </summary>
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Backoff multiplier for exponential backoff
        /// </summary>
        [Range(1.0, 10.0)]
        public double BackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Whether to use exponential backoff
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Whether to add jitter to retry delays
        /// </summary>
        public bool UseJitter { get; set; } = true;

        /// <summary>
        /// Types of exceptions that should trigger a retry
        /// </summary>
        public string[] RetryableExceptions { get; set; }

        public RetryPolicy()
        {
            RetryableExceptions = new[]
            {
                "System.TimeoutException",
                "System.Net.Http.HttpRequestException",
                "System.IO.IOException"
            };
        }
    }
}