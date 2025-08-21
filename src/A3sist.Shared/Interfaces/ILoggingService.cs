using Microsoft.Extensions.Logging;
using A3sist.Shared.Models;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Service for managing application logging configuration and operations
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// Creates a logger for the specified category
        /// </summary>
        /// <typeparam name="T">The type to create a logger for</typeparam>
        /// <returns>A logger instance</returns>
        ILogger<T> CreateLogger<T>();

        /// <summary>
        /// Creates a logger for the specified category name
        /// </summary>
        /// <param name="categoryName">The category name for the logger</param>
        /// <returns>A logger instance</returns>
        ILogger CreateLogger(string categoryName);

        /// <summary>
        /// Updates the logging configuration
        /// </summary>
        /// <param name="configuration">The new logging configuration</param>
        Task UpdateConfigurationAsync(LoggingConfiguration configuration);

        /// <summary>
        /// Gets the current logging configuration
        /// </summary>
        /// <returns>The current logging configuration</returns>
        LoggingConfiguration GetConfiguration();

        /// <summary>
        /// Performs log file cleanup based on retention policies
        /// </summary>
        Task CleanupLogsAsync();

        /// <summary>
        /// Gets log file information
        /// </summary>
        /// <returns>Information about current log files</returns>
        Task<IEnumerable<LogFileInfo>> GetLogFilesAsync();
    }
}