using A3sist.Shared.Enums;
using System;
using System.Collections.Generic;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Statistics about the task queue
    /// </summary>
    public class QueueStatistics
    {
        /// <summary>
        /// Total number of items in the queue
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Number of items by priority
        /// </summary>
        public Dictionary<TaskPriority, int> ItemsByPriority { get; set; } = new();

        /// <summary>
        /// Average wait time for items in the queue
        /// </summary>
        public TimeSpan AverageWaitTime { get; set; }

        /// <summary>
        /// Total number of items processed
        /// </summary>
        public long TotalProcessed { get; set; }

        /// <summary>
        /// Total number of items failed
        /// </summary>
        public long TotalFailed { get; set; }

        /// <summary>
        /// Average processing time
        /// </summary>
        public TimeSpan AverageProcessingTime { get; set; }

        /// <summary>
        /// Queue throughput (items per minute)
        /// </summary>
        public double ThroughputPerMinute { get; set; }

        /// <summary>
        /// When the statistics were last updated
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}