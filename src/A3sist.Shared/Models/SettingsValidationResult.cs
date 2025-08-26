using System.Collections.Generic;
using System.Linq;

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
        public bool IsValid { get; private set; } = true;

        /// <summary>
        /// List of validation errors
        /// </summary>
        public List<ValidationError> Errors { get; } = new List<ValidationError>();

        /// <summary>
        /// List of validation warnings
        /// </summary>
        public List<ValidationWarning> Warnings { get; } = new List<ValidationWarning>();

        /// <summary>
        /// Additional metadata about the validation
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Adds an error to the validation result
        /// </summary>
        public void AddError(string property, string message)
        {
            Errors.Add(new ValidationError { Property = property, Message = message });
            IsValid = false;
        }

        /// <summary>
        /// Adds a warning to the validation result
        /// </summary>
        public void AddWarning(string property, string message)
        {
            Warnings.Add(new ValidationWarning { Property = property, Message = message });
        }

        /// <summary>
        /// Gets whether there are any errors or warnings
        /// </summary>
        public bool HasIssues => Errors.Any() || Warnings.Any();

        /// <summary>
        /// Gets a summary of the validation result
        /// </summary>
        public string GetSummary()
        {
            if (IsValid && !Warnings.Any())
                return "Settings are valid with no issues.";
            
            if (IsValid && Warnings.Any())
                return $"Settings are valid with {Warnings.Count} warning(s).";
            
            return $"Settings validation failed with {Errors.Count} error(s) and {Warnings.Count} warning(s).";
        }
    }
}