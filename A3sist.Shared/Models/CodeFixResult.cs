using System.Collections.Generic;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Result of applying code fixes
    /// </summary>
    public class CodeFixResult
    {
        /// <summary>
        /// Whether the fix operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The original code before fixes
        /// </summary>
        public string OriginalCode { get; set; } = string.Empty;

        /// <summary>
        /// The code after applying fixes
        /// </summary>
        public string FixedCode { get; set; } = string.Empty;

        /// <summary>
        /// List of fixes that were applied
        /// </summary>
        public List<CodeFix> AppliedFixes { get; set; } = new List<CodeFix>();

        /// <summary>
        /// Any error messages from the fix operation
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Additional metadata about the fix operation
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}