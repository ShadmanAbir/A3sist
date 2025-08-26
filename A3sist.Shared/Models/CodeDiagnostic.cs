using A3sist.Shared.Enums;
using System.Collections.Generic;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Represents a diagnostic message from code analysis
    /// </summary>
    public class CodeDiagnostic
    {
        /// <summary>
        /// Unique identifier for the diagnostic
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The diagnostic message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Severity level of the diagnostic
        /// </summary>
        public DiagnosticSeverity Severity { get; set; }

        /// <summary>
        /// Category of the diagnostic
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Start line number (1-based)
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// End line number (1-based)
        /// </summary>
        public int EndLine { get; set; }

        /// <summary>
        /// Start column (1-based)
        /// </summary>
        public int StartColumn { get; set; }

        /// <summary>
        /// End column (1-based)
        /// </summary>
        public int EndColumn { get; set; }

        /// <summary>
        /// Source file path
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Additional metadata for the diagnostic
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}