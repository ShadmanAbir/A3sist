using A3sist.Shared.Models;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Service for collecting, categorizing, and reporting errors
    /// </summary>
    public interface IErrorReportingService
    {
        /// <summary>
        /// Reports an error with full context information
        /// </summary>
        /// <param name="errorReport">The error report to record</param>
        Task ReportErrorAsync(ErrorReport errorReport);

        /// <summary>
        /// Reports an exception with automatic context gathering
        /// </summary>
        /// <param name="exception">The exception to report</param>
        /// <param name="context">Additional context information</param>
        /// <param name="severity">Severity level of the error</param>
        /// <param name="component">Component where the error occurred</param>
        Task ReportExceptionAsync(Exception exception, Dictionary<string, object>? context = null, 
            ErrorSeverity severity = ErrorSeverity.Error, string? component = null);

        /// <summary>
        /// Reports a custom error message
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="category">Error category</param>
        /// <param name="severity">Severity level</param>
        /// <param name="context">Additional context</param>
        /// <param name="component">Component where the error occurred</param>
        Task ReportErrorAsync(string message, ErrorCategory category = ErrorCategory.Application,
            ErrorSeverity severity = ErrorSeverity.Error, Dictionary<string, object>? context = null, 
            string? component = null);

        /// <summary>
        /// Gets error reports based on filter criteria
        /// </summary>
        /// <param name="startTime">Start time for the query</param>
        /// <param name="endTime">End time for the query</param>
        /// <param name="severity">Filter by severity level</param>
        /// <param name="category">Filter by error category</param>
        /// <param name="component">Filter by component</param>
        /// <param name="limit">Maximum number of results</param>
        /// <returns>Collection of error reports</returns>
        Task<IEnumerable<ErrorReport>> GetErrorReportsAsync(DateTime? startTime = null, DateTime? endTime = null,
            ErrorSeverity? severity = null, ErrorCategory? category = null, string? component = null, int limit = 100);

        /// <summary>
        /// Gets error statistics for a time period
        /// </summary>
        /// <param name="startTime">Start time for the query</param>
        /// <param name="endTime">End time for the query</param>
        /// <returns>Error statistics</returns>
        Task<ErrorStatistics> GetErrorStatisticsAsync(DateTime? startTime = null, DateTime? endTime = null);

        /// <summary>
        /// Gets the most frequent errors
        /// </summary>
        /// <param name="startTime">Start time for the query</param>
        /// <param name="endTime">End time for the query</param>
        /// <param name="limit">Maximum number of results</param>
        /// <returns>Collection of frequent error summaries</returns>
        Task<IEnumerable<FrequentErrorSummary>> GetFrequentErrorsAsync(DateTime? startTime = null, 
            DateTime? endTime = null, int limit = 10);

        /// <summary>
        /// Collects diagnostic information about the current system state
        /// </summary>
        /// <returns>Diagnostic information</returns>
        Task<DiagnosticInfo> CollectDiagnosticInfoAsync();

        /// <summary>
        /// Exports error reports to a file
        /// </summary>
        /// <param name="filePath">Path to export the file</param>
        /// <param name="format">Export format</param>
        /// <param name="startTime">Start time for the query</param>
        /// <param name="endTime">End time for the query</param>
        Task ExportErrorReportsAsync(string filePath, ExportFormat format = ExportFormat.Json,
            DateTime? startTime = null, DateTime? endTime = null);

        /// <summary>
        /// Clears old error reports based on retention policy
        /// </summary>
        Task CleanupErrorReportsAsync();

        /// <summary>
        /// Analyzes error patterns and provides insights
        /// </summary>
        /// <param name="startTime">Start time for analysis</param>
        /// <param name="endTime">End time for analysis</param>
        /// <returns>Error analysis results</returns>
        Task<ErrorAnalysis> AnalyzeErrorPatternsAsync(DateTime? startTime = null, DateTime? endTime = null);
    }
}