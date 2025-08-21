using System;
using System.Collections.Generic;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Result of settings validation
    /// </summary>
    public class SettingsValidationResult
    {
        /// <summary>
        /// Whether the settings are valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// List of validation errors
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// List of validation warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Settings that were automatically corrected
        /// </summary>
        public Dictionary<string, object> CorrectedSettings { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Whether any settings were automatically corrected
        /// </summary>
        public bool HasCorrections => CorrectedSettings.Count > 0;

        /// <summary>
        /// Whether there are any validation issues
        /// </summary>
        public bool HasIssues => Errors.Count > 0 || Warnings.Count > 0;
    }
}