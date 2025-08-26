namespace A3sist.Shared.Models
{
    /// <summary>
    /// Represents a performance metric
    /// </summary>
    public class PerformanceMetric
    {
        /// <summary>
        /// Unique identifier for the metric
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Name of the metric
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Type of the metric
        /// </summary>
        public MetricType Type { get; set; }

        /// <summary>
        /// Value of the metric
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Unit of measurement
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// When the metric was recorded
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Tags associated with the metric
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = new();

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Source component that generated the metric
        /// </summary>
        public string Source { get; set; } = string.Empty;
    }

    /// <summary>
    /// Types of performance metrics
    /// </summary>
    public enum MetricType
    {
        /// <summary>
        /// Counter metric - monotonically increasing value
        /// </summary>
        Counter,

        /// <summary>
        /// Gauge metric - current value that can go up or down
        /// </summary>
        Gauge,

        /// <summary>
        /// Histogram metric - distribution of values
        /// </summary>
        Histogram,

        /// <summary>
        /// Timer metric - duration measurements
        /// </summary>
        Timer,

        /// <summary>
        /// Summary metric - statistical summary of observations
        /// </summary>
        Summary
    }
}