using System.Collections.Generic;
using System.Linq;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Result of a validation operation
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Whether the validation passed
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
        /// Additional metadata about the validation
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// Creates a failed validation result with errors
        /// </summary>
        public static ValidationResult Failure(params string[] errors)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = errors.ToList()
            };
        }

        /// <summary>
        /// Creates a successful validation result with warnings
        /// </summary>
        public static ValidationResult SuccessWithWarnings(params string[] warnings)
        {
            return new ValidationResult
            {
                IsValid = true,
                Warnings = warnings.ToList()
            };
        }

        /// <summary>
        /// Adds an error to the validation result
        /// </summary>
        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        /// <summary>
        /// Adds a warning to the validation result
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }
}