namespace A3sist.Shared.Models
{
    /// <summary>
    /// Represents a detailed error report
    /// </summary>
    public class ErrorReport
    {
        /// <summary>
        /// Unique identifier for the error report
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// When the error occurred
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Detailed error description
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Exception information if available
        /// </summary>
        public ExceptionInfo? Exception { get; set; }

        /// <summary>
        /// Severity level of the error
        /// </summary>
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;

        /// <summary>
        /// Category of the error
        /// </summary>
        public ErrorCategory Category { get; set; } = ErrorCategory.Application;

        /// <summary>
        /// Component where the error occurred
        /// </summary>
        public string? Component { get; set; }

        /// <summary>
        /// User ID if available
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Session ID if available
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// Request ID if available
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Additional context information
        /// </summary>
        public Dictionary<string, object> Context { get; set; } = new();

        /// <summary>
        /// System information at the time of error
        /// </summary>
        public SystemContext SystemContext { get; set; } = new();

        /// <summary>
        /// Stack trace if available
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Inner exception information
        /// </summary>
        public ExceptionInfo? InnerException { get; set; }

        /// <summary>
        /// Tags for categorization and filtering
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Whether this error has been resolved
        /// </summary>
        public bool IsResolved { get; set; }

        /// <summary>
        /// Resolution notes if resolved
        /// </summary>
        public string? ResolutionNotes { get; set; }

        /// <summary>
        /// Hash of the error for deduplication
        /// </summary>
        public string ErrorHash { get; set; } = string.Empty;
    }

    /// <summary>
    /// Exception information
    /// </summary>
    public class ExceptionInfo
    {
        /// <summary>
        /// Type of the exception
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Exception message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Stack trace
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Source of the exception
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Target site information
        /// </summary>
        public string? TargetSite { get; set; }

        /// <summary>
        /// Exception data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();

        /// <summary>
        /// Inner exception information
        /// </summary>
        public ExceptionInfo? InnerException { get; set; }
    }

    /// <summary>
    /// System context at the time of error
    /// </summary>
    public class SystemContext
    {
        /// <summary>
        /// Machine name
        /// </summary>
        public string MachineName { get; set; } = Environment.MachineName;

        /// <summary>
        /// Operating system version
        /// </summary>
        public string OSVersion { get; set; } = Environment.OSVersion.ToString();

        /// <summary>
        /// .NET runtime version
        /// </summary>
        public string RuntimeVersion { get; set; } = Environment.Version.ToString();

        /// <summary>
        /// Application version
        /// </summary>
        public string? ApplicationVersion { get; set; }

        /// <summary>
        /// Current working directory
        /// </summary>
        public string WorkingDirectory { get; set; } = Environment.CurrentDirectory;

        /// <summary>
        /// Current user name
        /// </summary>
        public string UserName { get; set; } = Environment.UserName;

        /// <summary>
        /// Process ID
        /// </summary>
        public int ProcessId { get; set; } = System.Diagnostics.Process.GetCurrentProcess().Id;

        /// <summary>
        /// Thread ID
        /// </summary>
        public int ThreadId { get; set; } = Environment.CurrentManagedThreadId;

        /// <summary>
        /// Available memory in bytes
        /// </summary>
        public long AvailableMemory { get; set; }

        /// <summary>
        /// CPU usage percentage
        /// </summary>
        public double CpuUsage { get; set; }
    }

    /// <summary>
    /// Error severity levels
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// Low severity - informational
        /// </summary>
        Info,

        /// <summary>
        /// Warning - potential issue
        /// </summary>
        Warning,

        /// <summary>
        /// Error - functional issue
        /// </summary>
        Error,

        /// <summary>
        /// Critical - system failure
        /// </summary>
        Critical,

        /// <summary>
        /// Fatal - application crash
        /// </summary>
        Fatal
    }

    /// <summary>
    /// Error categories
    /// </summary>
    public enum ErrorCategory
    {
        /// <summary>
        /// General application error
        /// </summary>
        Application,

        /// <summary>
        /// System or infrastructure error
        /// </summary>
        System,

        /// <summary>
        /// Network or connectivity error
        /// </summary>
        Network,

        /// <summary>
        /// Database or data access error
        /// </summary>
        Database,

        /// <summary>
        /// External service error
        /// </summary>
        ExternalService,

        /// <summary>
        /// User input or validation error
        /// </summary>
        Validation,

        /// <summary>
        /// Security-related error
        /// </summary>
        Security,

        /// <summary>
        /// Configuration error
        /// </summary>
        Configuration,

        /// <summary>
        /// Performance-related error
        /// </summary>
        Performance,

        /// <summary>
        /// Agent-specific error
        /// </summary>
        Agent
    }

    /// <summary>
    /// Export formats for reports and data
    /// </summary>
    public enum ExportFormat
    {
        /// <summary>
        /// JSON format
        /// </summary>
        Json,

        /// <summary>
        /// CSV format
        /// </summary>
        Csv,

        /// <summary>
        /// XML format
        /// </summary>
        Xml,

        /// <summary>
        /// Parquet format
        /// </summary>
        Parquet,

        /// <summary>
        /// JSON Lines format
        /// </summary>
        Jsonl,

        /// <summary>
        /// Avro format
        /// </summary>
        Avro
    }
}