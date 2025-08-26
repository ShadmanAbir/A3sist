using Microsoft.Extensions.Logging;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Configuration for the logging system
    /// </summary>
    public class LoggingConfiguration
    {
        /// <summary>
        /// Minimum log level to write
        /// </summary>
        public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Path where log files should be written
        /// </summary>
        public string LogFilePath { get; set; } = Path.Combine(Path.GetTempPath(), "A3sist", "logs");

        /// <summary>
        /// Maximum size of a single log file in MB
        /// </summary>
        public int MaxFileSizeMB { get; set; } = 10;

        /// <summary>
        /// Number of log files to retain
        /// </summary>
        public int RetainedFileCountLimit { get; set; } = 10;

        /// <summary>
        /// Whether to write logs to console
        /// </summary>
        public bool WriteToConsole { get; set; } = true;

        /// <summary>
        /// Whether to write logs to file
        /// </summary>
        public bool WriteToFile { get; set; } = true;

        /// <summary>
        /// Log message template
        /// </summary>
        public string OutputTemplate { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Whether to include scopes in log output
        /// </summary>
        public bool IncludeScopes { get; set; } = true;

        /// <summary>
        /// Log level overrides for specific categories
        /// </summary>
        public Dictionary<string, LogLevel> LogLevelOverrides { get; set; } = new();

        /// <summary>
        /// Whether to enable structured logging
        /// </summary>
        public bool EnableStructuredLogging { get; set; } = true;

        /// <summary>
        /// Additional properties to include in all log entries
        /// </summary>
        public Dictionary<string, object> GlobalProperties { get; set; } = new();
    }
}