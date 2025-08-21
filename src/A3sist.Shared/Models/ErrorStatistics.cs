namespace A3sist.Shared.Models
{
    /// <summary>
    /// Statistical information about errors
    /// </summary>
    public class ErrorStatistics
    {
        /// <summary>
        /// Time period for the statistics
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time for the statistics
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Total number of errors
        /// </summary>
        public int TotalErrors { get; set; }

        /// <summary>
        /// Errors by severity level
        /// </summary>
        public Dictionary<ErrorSeverity, int> ErrorsBySeverity { get; set; } = new();

        /// <summary>
        /// Errors by category
        /// </summary>
        public Dictionary<ErrorCategory, int> ErrorsByCategory { get; set; } = new();

        /// <summary>
        /// Errors by component
        /// </summary>
        public Dictionary<string, int> ErrorsByComponent { get; set; } = new();

        /// <summary>
        /// Error rate per hour
        /// </summary>
        public double ErrorRatePerHour { get; set; }

        /// <summary>
        /// Most common error types
        /// </summary>
        public List<ErrorTypeSummary> MostCommonErrors { get; set; } = new();

        /// <summary>
        /// Error trends over time
        /// </summary>
        public List<ErrorTrendPoint> ErrorTrends { get; set; } = new();

        /// <summary>
        /// Resolution statistics
        /// </summary>
        public ResolutionStatistics ResolutionStats { get; set; } = new();
    }

    /// <summary>
    /// Summary of a specific error type
    /// </summary>
    public class ErrorTypeSummary
    {
        /// <summary>
        /// Error message or type
        /// </summary>
        public string ErrorType { get; set; } = string.Empty;

        /// <summary>
        /// Number of occurrences
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Percentage of total errors
        /// </summary>
        public double Percentage { get; set; }

        /// <summary>
        /// First occurrence
        /// </summary>
        public DateTime FirstOccurrence { get; set; }

        /// <summary>
        /// Last occurrence
        /// </summary>
        public DateTime LastOccurrence { get; set; }

        /// <summary>
        /// Most common severity for this error type
        /// </summary>
        public ErrorSeverity MostCommonSeverity { get; set; }

        /// <summary>
        /// Components affected by this error type
        /// </summary>
        public List<string> AffectedComponents { get; set; } = new();
    }

    /// <summary>
    /// Error trend data point
    /// </summary>
    public class ErrorTrendPoint
    {
        /// <summary>
        /// Time point
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Number of errors at this time point
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Error rate (errors per minute/hour)
        /// </summary>
        public double ErrorRate { get; set; }
    }

    /// <summary>
    /// Statistics about error resolution
    /// </summary>
    public class ResolutionStatistics
    {
        /// <summary>
        /// Total number of resolved errors
        /// </summary>
        public int ResolvedErrors { get; set; }

        /// <summary>
        /// Total number of unresolved errors
        /// </summary>
        public int UnresolvedErrors { get; set; }

        /// <summary>
        /// Resolution rate percentage
        /// </summary>
        public double ResolutionRate { get; set; }

        /// <summary>
        /// Average time to resolution
        /// </summary>
        public TimeSpan AverageResolutionTime { get; set; }

        /// <summary>
        /// Resolution rate by severity
        /// </summary>
        public Dictionary<ErrorSeverity, double> ResolutionRateBySeverity { get; set; } = new();
    }

    /// <summary>
    /// Summary of frequently occurring errors
    /// </summary>
    public class FrequentErrorSummary
    {
        /// <summary>
        /// Error hash for deduplication
        /// </summary>
        public string ErrorHash { get; set; } = string.Empty;

        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Exception type if applicable
        /// </summary>
        public string? ExceptionType { get; set; }

        /// <summary>
        /// Number of occurrences
        /// </summary>
        public int Occurrences { get; set; }

        /// <summary>
        /// First occurrence
        /// </summary>
        public DateTime FirstSeen { get; set; }

        /// <summary>
        /// Last occurrence
        /// </summary>
        public DateTime LastSeen { get; set; }

        /// <summary>
        /// Most common severity
        /// </summary>
        public ErrorSeverity Severity { get; set; }

        /// <summary>
        /// Most common category
        /// </summary>
        public ErrorCategory Category { get; set; }

        /// <summary>
        /// Components where this error occurs
        /// </summary>
        public List<string> Components { get; set; } = new();

        /// <summary>
        /// Whether this error pattern is resolved
        /// </summary>
        public bool IsResolved { get; set; }

        /// <summary>
        /// Trend direction (increasing, decreasing, stable)
        /// </summary>
        public TrendDirection Trend { get; set; }
    }

    /// <summary>
    /// Trend direction for error patterns
    /// </summary>
    public enum TrendDirection
    {
        /// <summary>
        /// Error frequency is increasing
        /// </summary>
        Increasing,

        /// <summary>
        /// Error frequency is decreasing
        /// </summary>
        Decreasing,

        /// <summary>
        /// Error frequency is stable
        /// </summary>
        Stable,

        /// <summary>
        /// Not enough data to determine trend
        /// </summary>
        Unknown
    }
}