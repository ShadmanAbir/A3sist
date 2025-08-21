namespace A3sist.Shared.Models
{
    /// <summary>
    /// Information about a log file
    /// </summary>
    public class LogFileInfo
    {
        /// <summary>
        /// Full path to the log file
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Size of the log file in bytes
        /// </summary>
        public long SizeBytes { get; set; }

        /// <summary>
        /// When the log file was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the log file was last modified
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Whether the log file is currently being written to
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Number of log entries in the file (if available)
        /// </summary>
        public int? EntryCount { get; set; }
    }
}