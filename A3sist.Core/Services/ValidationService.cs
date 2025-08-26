using A3sist.Shared.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace A3sist.Core.Services
{
    /// <summary>
    /// Interface for validation services
    /// </summary>
    public interface IValidationService
    {
        Task<ValidationResult> ValidateRequestAsync(AgentRequest request);
        Task<ValidationResult> ValidateFilePathAsync(string filePath);
        Task<ValidationResult> ValidateContentAsync(string content, string? language = null);
        Task<ValidationResult> ValidatePromptAsync(string prompt);
    }

    /// <summary>
    /// Comprehensive validation result
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();

        public static ValidationResult Success() => new() { IsValid = true };
        public static ValidationResult Failure(params string[] errors) => new() { IsValid = false, Errors = errors.ToList() };
    }

    /// <summary>
    /// Robust validation service for security and data integrity
    /// </summary>
    public class ValidationService : IValidationService
    {
        private readonly ILogger<ValidationService> _logger;
        private static readonly HashSet<string> AllowedFileExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".js", ".ts", ".py", ".java", ".cpp", ".c", ".h", ".hpp",
            ".html", ".css", ".json", ".xml", ".yml", ".yaml", ".md", ".txt"
        };

        private static readonly HashSet<string> DangerousPatterns = new(StringComparer.OrdinalIgnoreCase)
        {
            "eval(", "exec(", "system(", "shell_exec(", "passthru(",
            "Process.Start", "ProcessStartInfo", "cmd.exe", "powershell.exe",
            "__import__", "importlib", "subprocess", "os.system"
        };

        private static readonly Regex SqlInjectionPattern = new(
            @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|EXECUTE)\b)|('(''|[^'])*')|(\-\-)|(/\*.*?\*/)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ScriptInjectionPattern = new(
            @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>|javascript:|vbscript:|on\w+\s*=",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public ValidationService(ILogger<ValidationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validates an agent request comprehensively
        /// </summary>
        public async Task<ValidationResult> ValidateRequestAsync(AgentRequest request)
        {
            if (request == null)
                return ValidationResult.Failure("Request cannot be null");

            var result = new ValidationResult { IsValid = true };

            // Validate required fields
            if (request.Id == Guid.Empty)
                result.Errors.Add("Request ID is required");

            if (string.IsNullOrWhiteSpace(request.Prompt))
                result.Errors.Add("Prompt is required and cannot be empty");

            if (string.IsNullOrWhiteSpace(request.UserId))
                result.Errors.Add("User ID is required");

            // Validate prompt
            var promptValidation = await ValidatePromptAsync(request.Prompt);
            if (!promptValidation.IsValid)
                result.Errors.AddRange(promptValidation.Errors);

            // Validate file path if provided
            if (!string.IsNullOrWhiteSpace(request.FilePath))
            {
                var fileValidation = await ValidateFilePathAsync(request.FilePath);
                if (!fileValidation.IsValid)
                    result.Errors.AddRange(fileValidation.Errors);
            }

            // Validate content if provided
            if (!string.IsNullOrWhiteSpace(request.Content))
            {
                var language = DetectLanguageFromPath(request.FilePath);
                var contentValidation = await ValidateContentAsync(request.Content, language);
                if (!contentValidation.IsValid)
                    result.Errors.AddRange(contentValidation.Errors);
                result.Warnings.AddRange(contentValidation.Warnings);
            }

            // Validate context data
            if (request.Context != null)
            {
                foreach (var kvp in request.Context)
                {
                    if (string.IsNullOrWhiteSpace(kvp.Key))
                        result.Errors.Add("Context keys cannot be null or empty");

                    if (kvp.Value is string strValue && ContainsDangerousPatterns(strValue))
                        result.Errors.Add($"Potentially dangerous content detected in context key: {kvp.Key}");
                }
            }

            // Check request size limits
            var requestSize = EstimateRequestSize(request);
            if (requestSize > 10 * 1024 * 1024) // 10MB limit
            {
                result.Errors.Add($"Request size ({requestSize / 1024 / 1024}MB) exceeds maximum allowed size (10MB)");
            }
            else if (requestSize > 5 * 1024 * 1024) // 5MB warning
            {
                result.Warnings.Add($"Request size ({requestSize / 1024 / 1024}MB) is large and may impact performance");
            }

            result.IsValid = !result.Errors.Any();
            result.Metadata["RequestSize"] = requestSize;
            result.Metadata["ValidationTimestamp"] = DateTime.UtcNow;

            if (!result.IsValid)
            {
                _logger.LogWarning("Request validation failed for {RequestId}: {Errors}", 
                    request.Id, string.Join(", ", result.Errors));
            }

            return result;
        }

        /// <summary>
        /// Validates file paths for security
        /// </summary>
        public async Task<ValidationResult> ValidateFilePathAsync(string filePath)
        {
            await Task.CompletedTask; // For async consistency

            if (string.IsNullOrWhiteSpace(filePath))
                return ValidationResult.Failure("File path cannot be null or empty");

            var result = new ValidationResult { IsValid = true };

            try
            {
                // Check for path traversal attempts
                var normalizedPath = Path.GetFullPath(filePath);
                if (filePath.Contains("..") || filePath.Contains("~"))
                {
                    result.Errors.Add("Path traversal detected in file path");
                }

                // Check file extension
                var extension = Path.GetExtension(filePath);
                if (!AllowedFileExtensions.Contains(extension))
                {
                    result.Warnings.Add($"File extension '{extension}' is not in the list of commonly supported extensions");
                }

                // Check for suspicious file names
                var fileName = Path.GetFileName(filePath);
                if (fileName.Contains("passwd") || fileName.Contains("shadow") || 
                    fileName.Contains("hosts") || fileName.Contains(".env"))
                {
                    result.Errors.Add("Potentially sensitive file detected");
                }

                // Check path length
                if (filePath.Length > 260) // Windows MAX_PATH
                {
                    result.Warnings.Add("File path exceeds recommended maximum length");
                }

                result.Metadata["Extension"] = extension;
                result.Metadata["FileName"] = fileName;
                result.Metadata["NormalizedPath"] = normalizedPath;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Invalid file path format: {ex.Message}");
                _logger.LogWarning(ex, "Error validating file path: {FilePath}", filePath);
            }

            result.IsValid = !result.Errors.Any();
            return result;
        }

        /// <summary>
        /// Validates content for security issues
        /// </summary>
        public async Task<ValidationResult> ValidateContentAsync(string content, string? language = null)
        {
            await Task.CompletedTask; // For async consistency

            if (string.IsNullOrEmpty(content))
                return ValidationResult.Success();

            var result = new ValidationResult { IsValid = true };

            // Check content size
            var contentSize = System.Text.Encoding.UTF8.GetByteCount(content);
            if (contentSize > 5 * 1024 * 1024) // 5MB limit
            {
                result.Errors.Add($"Content size ({contentSize / 1024 / 1024}MB) exceeds maximum allowed size (5MB)");
            }

            // Check for dangerous patterns
            if (ContainsDangerousPatterns(content))
            {
                result.Errors.Add("Potentially dangerous code patterns detected");
            }

            // Check for SQL injection patterns
            if (SqlInjectionPattern.IsMatch(content))
            {
                result.Warnings.Add("Content contains SQL-like patterns that may need review");
            }

            // Check for script injection patterns
            if (ScriptInjectionPattern.IsMatch(content))
            {
                result.Warnings.Add("Content contains script-like patterns that may need review");
            }

            // Language-specific validation
            if (!string.IsNullOrEmpty(language))
            {
                ValidateLanguageSpecificContent(content, language, result);
            }

            result.Metadata["ContentSize"] = contentSize;
            result.Metadata["Language"] = language ?? "unknown";
            result.Metadata["LineCount"] = content.Split('\n').Length;

            result.IsValid = !result.Errors.Any();
            return result;
        }

        /// <summary>
        /// Validates prompts for safety and effectiveness
        /// </summary>
        public async Task<ValidationResult> ValidatePromptAsync(string prompt)
        {
            await Task.CompletedTask; // For async consistency

            if (string.IsNullOrWhiteSpace(prompt))
                return ValidationResult.Failure("Prompt cannot be null or empty");

            var result = new ValidationResult { IsValid = true };

            // Check prompt length
            if (prompt.Length < 3)
                result.Errors.Add("Prompt is too short to be meaningful");
            else if (prompt.Length > 10000)
                result.Warnings.Add("Prompt is very long and may be truncated");

            // Check for potentially harmful instructions
            var harmfulPatterns = new[]
            {
                "ignore previous instructions",
                "disregard safety",
                "bypass security",
                "execute system command",
                "delete file",
                "format drive"
            };

            foreach (var pattern in harmfulPatterns)
            {
                if (prompt.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors.Add($"Potentially harmful instruction detected: {pattern}");
                }
            }

            // Check for injection attempts
            if (ContainsDangerousPatterns(prompt))
            {
                result.Errors.Add("Potentially dangerous patterns detected in prompt");
            }

            result.Metadata["PromptLength"] = prompt.Length;
            result.Metadata["WordCount"] = prompt.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

            result.IsValid = !result.Errors.Any();
            return result;
        }

        /// <summary>
        /// Checks for dangerous patterns in text
        /// </summary>
        private bool ContainsDangerousPatterns(string text)
        {
            return DangerousPatterns.Any(pattern => 
                text.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Performs language-specific content validation
        /// </summary>
        private void ValidateLanguageSpecificContent(string content, string language, ValidationResult result)
        {
            switch (language.ToLowerInvariant())
            {
                case "csharp":
                case "cs":
                    ValidateCSharpContent(content, result);
                    break;
                case "javascript":
                case "js":
                    ValidateJavaScriptContent(content, result);
                    break;
                case "python":
                case "py":
                    ValidatePythonContent(content, result);
                    break;
            }
        }

        /// <summary>
        /// Validates C# specific content
        /// </summary>
        private void ValidateCSharpContent(string content, ValidationResult result)
        {
            var dangerousPatterns = new[]
            {
                "System.Diagnostics.Process",
                "System.IO.File.Delete",
                "Registry.SetValue",
                "Marshal.GetDelegateForFunctionPointer"
            };

            foreach (var pattern in dangerousPatterns)
            {
                if (content.Contains(pattern))
                {
                    result.Warnings.Add($"Potentially dangerous C# pattern detected: {pattern}");
                }
            }
        }

        /// <summary>
        /// Validates JavaScript specific content
        /// </summary>
        private void ValidateJavaScriptContent(string content, ValidationResult result)
        {
            var dangerousPatterns = new[]
            {
                "document.write",
                "innerHTML",
                "document.cookie",
                "localStorage",
                "sessionStorage"
            };

            foreach (var pattern in dangerousPatterns)
            {
                if (content.Contains(pattern))
                {
                    result.Warnings.Add($"Potentially risky JavaScript pattern detected: {pattern}");
                }
            }
        }

        /// <summary>
        /// Validates Python specific content
        /// </summary>
        private void ValidatePythonContent(string content, ValidationResult result)
        {
            var dangerousPatterns = new[]
            {
                "os.system",
                "subprocess.call",
                "subprocess.run",
                "__import__",
                "compile("
            };

            foreach (var pattern in dangerousPatterns)
            {
                if (content.Contains(pattern))
                {
                    result.Warnings.Add($"Potentially dangerous Python pattern detected: {pattern}");
                }
            }
        }

        /// <summary>
        /// Detects programming language from file path
        /// </summary>
        private string? DetectLanguageFromPath(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".cs" => "csharp",
                ".js" => "javascript",
                ".ts" => "typescript",
                ".py" => "python",
                ".java" => "java",
                ".cpp" or ".cc" or ".cxx" => "cpp",
                _ => null
            };
        }

        /// <summary>
        /// Estimates the size of a request in bytes
        /// </summary>
        private long EstimateRequestSize(AgentRequest request)
        {
            long size = 0;
            
            size += System.Text.Encoding.UTF8.GetByteCount(request.Prompt ?? "");
            size += System.Text.Encoding.UTF8.GetByteCount(request.Content ?? "");
            size += System.Text.Encoding.UTF8.GetByteCount(request.FilePath ?? "");
            size += System.Text.Encoding.UTF8.GetByteCount(request.UserId ?? "");

            // Estimate context and metadata size
            if (request.Context != null)
            {
                foreach (var kvp in request.Context)
                {
                    size += System.Text.Encoding.UTF8.GetByteCount(kvp.Key ?? "");
                    size += System.Text.Encoding.UTF8.GetByteCount(kvp.Value?.ToString() ?? "");
                }
            }

            return size;
        }
    }
}