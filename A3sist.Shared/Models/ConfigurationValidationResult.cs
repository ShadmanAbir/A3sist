using System.Collections.Generic;
using System.Linq;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Result of configuration validation
    /// </summary>
    public class ConfigurationValidationResult
    {
        /// <summary>
        /// Whether the configuration is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation errors
        /// </summary>
        public List<ValidationError> Errors { get; set; }

        /// <summary>
        /// Validation warnings
        /// </summary>
        public List<ValidationWarning> Warnings { get; set; }

        public ConfigurationValidationResult()
        {
            Errors = new List<ValidationError>();
            Warnings = new List<ValidationWarning>();
            IsValid = true;
        }

        /// <summary>
        /// Adds a validation error
        /// </summary>
        public void AddError(string property, string message)
        {
            Errors.Add(new ValidationError { Property = property, Message = message });
            IsValid = false;
        }

        /// <summary>
        /// Adds a validation warning
        /// </summary>
        public void AddWarning(string property, string message)
        {
            Warnings.Add(new ValidationWarning { Property = property, Message = message });
        }

        /// <summary>
        /// Gets all error messages
        /// </summary>
        public string[] GetErrorMessages() => Errors.Select(e => e.Message).ToArray();

        /// <summary>
        /// Gets all warning messages
        /// </summary>
        public string[] GetWarningMessages() => Warnings.Select(w => w.Message).ToArray();
    }

    /// <summary>
    /// Validation error details
    /// </summary>
    public class ValidationError
    {
        public string Property { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Validation warning details
    /// </summary>
    public class ValidationWarning
    {
        public string Property { get; set; }
        public string Message { get; set; }
    }
}