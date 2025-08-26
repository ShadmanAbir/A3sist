namespace A3sist.Shared.Models
{
    /// <summary>
    /// Aggregated performance statistics for a metric
    /// </summary>
    public class PerformanceStatistics
    {
        /// <summary>
        /// Name of the metric
        /// </summary>
        public string MetricName { get; set; } = string.Empty;

        /// <summary>
        /// Number of data points
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Minimum value
        /// </summary>
        public double Min { get; set; }

        /// <summary>
        /// Maximum value
        /// </summary>
        public double Max { get; set; }

        /// <summary>
        /// Average value
        /// </summary>
        public double Average { get; set; }

        /// <summary>
        /// Median value
        /// </summary>
        public double Median { get; set; }

        /// <summary>
        /// 95th percentile value
        /// </summary>
        public double P95 { get; set; }

        /// <summary>
        /// 99th percentile value
        /// </summary>
        public double P99 { get; set; }

        /// <summary>
        /// Standard deviation
        /// </summary>
        public double StandardDeviation { get; set; }

        /// <summary>
        /// Sum of all values
        /// </summary>
        public double Sum { get; set; }

        /// <summary>
        /// Rate per second (for counters)
        /// </summary>
        public double Rate { get; set; }

        /// <summary>
        /// Time range for the statistics
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time for the statistics
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Unit of measurement
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Tags that were used to filter the data
        /// </summary>
        public Dictionary<string, string> FilterTags { get; set; } = new();
    }
}