using A3sist.Shared.Enums;
using System.Collections.Generic;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Represents a single code fix
    /// </summary>
    public class CodeFix
    {
        /// <summary>
        /// Unique identifier for the fix
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Title of the fix
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Description of what the fix does
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Category of the fix
        /// </summary>
        public FixCategory Category { get; set; }

        /// <summary>
        /// Severity of the issue being fixed
        /// </summary>
        public FixSeverity Severity { get; set; }

        /// <summary>
        /// The original code snippet
        /// </summary>
        public string OriginalCode { get; set; } = string.Empty;

        /// <summary>
        /// The fixed code snippet
        /// </summary>
        public string FixedCode { get; set; } = string.Empty;

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
        /// Whether this fix can be applied automatically
        /// </summary>
        public bool CanAutoApply { get; set; }

        /// <summary>
        /// Additional metadata for the fix
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}