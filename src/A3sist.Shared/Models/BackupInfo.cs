using System;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Information about a settings backup
    /// </summary>
    public class BackupInfo
    {
        /// <summary>
        /// Path to the backup file
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Date and time when the backup was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Size of the backup file in bytes
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Version of the settings schema when backup was created
        /// </summary>
        public string SchemaVersion { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of the backup
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Whether this backup is valid and can be restored
        /// </summary>
        public bool IsValid { get; set; } = true;
    }
}