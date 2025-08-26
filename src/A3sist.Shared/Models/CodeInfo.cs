using System.Collections.Generic;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Information about code to be analyzed or fixed
    /// </summary>
    public class CodeInfo
    {
        /// <summary>
        /// The source code content
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// File path of the code
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Programming language of the code
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// Project context information
        /// </summary>
        public string? ProjectPath { get; set; }

        /// <summary>
        /// Additional metadata about the code
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}