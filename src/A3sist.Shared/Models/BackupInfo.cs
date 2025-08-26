using System;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Information about a settings backup file
    /// </summary>
    public class BackupInfo
    {
        /// <summary>
        /// Full path to the backup file
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Name of the backup file
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// When the backup was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Version of the settings when backup was created
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Size of the backup file in bytes
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Whether this backup is valid and can be restored
        /// </summary>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// Additional description or notes about the backup
        /// </summary>
        public string? Description { get; set; }
    }
}