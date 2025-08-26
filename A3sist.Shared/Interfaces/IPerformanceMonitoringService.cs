using A3sist.Shared.Models;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Service for monitoring and collecting performance metrics
    /// </summary>
    public interface IPerformanceMonitoringService
    {
        /// <summary>
        /// Records a performance metric
        /// </summary>
        /// <param name="metric">The metric to record</param>
        Task RecordMetricAsync(PerformanceMetric metric);

        /// <summary>
        /// Records a counter metric (incremental value)
        /// </summary>
        /// <param name="name">The metric name</param>
        /// <param name="value">The value to add</param>
        /// <param name="tags">Optional tags for the metric</param>
        Task RecordCounterAsync(string name, double value = 1, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Records a gauge metric (current value)
        /// </summary>
        /// <param name="name">The metric name</param>
        /// <param name="value">The current value</param>
        /// <param name="tags">Optional tags for the metric</param>
        Task RecordGaugeAsync(string name, double value, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Records a histogram metric (distribution of values)
        /// </summary>
        /// <param name="name">The metric name</param>
        /// <param name="value">The value to record</param>
        /// <param name="tags">Optional tags for the metric</param>
        Task RecordHistogramAsync(string name, double value, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Records a timing metric
        /// </summary>
        /// <param name="name">The metric name</param>
        /// <param name="duration">The duration to record</param>
        /// <param name="tags">Optional tags for the metric</param>
        Task RecordTimingAsync(string name, TimeSpan duration, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Creates a timer for measuring operation duration
        /// </summary>
        /// <param name="name">The metric name</param>
        /// <param name="tags">Optional tags for the metric</param>
        /// <returns>A disposable timer that records the duration when disposed</returns>
        IDisposable StartTimer(string name, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Gets performance metrics for a specific time range
        /// </summary>
        /// <param name="metricName">The metric name (optional)</param>
        /// <param name="startTime">Start time for the query</param>
        /// <param name="endTime">End time for the query</param>
        /// <param name="tags">Optional tags to filter by</param>
        /// <returns>Collection of performance metrics</returns>
        Task<IEnumerable<PerformanceMetric>> GetMetricsAsync(string? metricName = null, 
            DateTime? startTime = null, DateTime? endTime = null, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Gets aggregated performance statistics
        /// </summary>
        /// <param name="metricName">The metric name</param>
        /// <param name="startTime">Start time for the query</param>
        /// <param name="endTime">End time for the query</param>
        /// <param name="tags">Optional tags to filter by</param>
        /// <returns>Aggregated statistics</returns>
        Task<PerformanceStatistics> GetStatisticsAsync(string metricName, 
            DateTime? startTime = null, DateTime? endTime = null, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Gets system health metrics
        /// </summary>
        /// <returns>Current system health metrics</returns>
        Task<SystemHealthMetrics> GetSystemHealthAsync();

        /// <summary>
        /// Clears old metrics based on retention policy
        /// </summary>
        Task CleanupMetricsAsync();

        /// <summary>
        /// Starts tracking an operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        void StartOperation(string operationName);

        /// <summary>
        /// Records agent execution metrics
        /// </summary>
        /// <param name="agentName">Name of the agent</param>
        /// <param name="duration">Execution duration</param>
        /// <param name="success">Whether the execution was successful</param>
        void RecordAgentExecution(string agentName, TimeSpan duration, bool success);

        /// <summary>
        /// Ends tracking of an operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="success">Whether the operation was successful</param>
        void EndOperation(string operationName, bool success);
    }
}