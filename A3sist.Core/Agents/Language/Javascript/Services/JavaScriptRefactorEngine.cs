using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace A3sist.Orchastrator.Agents.JavaScript.Services
{
    /// <summary>
    /// Provides comprehensive code refactoring capabilities for JavaScript and TypeScript code
    /// </summary>
    public class JavaScriptRefactorEngine : IDisposable
    {
        private bool _disposed = false;

        /// <summary>
        /// Initializes the JavaScript refactoring engine asynchronously
        /// </summary>
        public async Task InitializeAsync()
        {
            // Initialize refactoring engine components
            await Task.CompletedTask;
        }

        /// <summary>
        /// Refactors JavaScript/TypeScript code with multiple improvement techniques
        /// </summary>
        /// <param name="code">The JavaScript/TypeScript code to refactor</param>
        /// <returns>The refactored code</returns>
        public async Task<string> RefactorCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentNullException(nameof(code));

            try
            {
                var refactoredCode = code;

                // Apply various refactoring techniques
                refactoredCode = await ConvertVarToLetConstAsync(refactoredCode);
                refactoredCode = await ConvertToArrowFunctionsAsync(refactoredCode);
                refactoredCode = await UseStrictEqualityAsync(refactoredCode);
                refactoredCode = await OptimizeStringConcatenationAsync(refactoredCode);
                refactoredCode = await SimplifyConditionalExpressionsAsync(refactoredCode);
                refactoredCode = await RemoveUnnecessaryCodeAsync(refactoredCode);
                refactoredCode = await FormatCodeAsync(refactoredCode);

                return refactoredCode;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to refactor JavaScript code.", ex);
            }
        }

        /// <summary>
        /// Formats JavaScript/TypeScript code for better readability
        /// </summary>
        /// <param name="code">The code to format</param>
        /// <returns>The formatted code</returns>
        public async Task<string> FormatCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return code;

            try
            {
                var lines = code.Split('\n');
                var formattedLines = new List<string>();
                var indentLevel = 0;
                var indentString = "  "; // 2 spaces

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    
                    if (string.IsNullOrEmpty(trimmedLine))
                    {
                        formattedLines.Add("");
                        continue;
                    }

                    // Decrease indent for closing braces
                    if (trimmedLine.StartsWith("}"))
                    {
                        indentLevel = Math.Max(0, indentLevel - 1);
                    }

                    // Add proper indentation
                    var indentedLine = new string(' ', indentLevel * indentString.Length) + trimmedLine;
                    formattedLines.Add(indentedLine);

                    // Increase indent for opening braces
                    if (trimmedLine.EndsWith("{"))
                    {
                        indentLevel++;
                    }
                }

                return await Task.FromResult(string.Join(Environment.NewLine, formattedLines));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to format JavaScript code: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Converts var declarations to let/const where appropriate
        /// </summary>
        private async Task<string> ConvertVarToLetConstAsync(string code)
        {
            // Simple pattern matching for var declarations
            // In a real implementation, you'd want more sophisticated AST parsing
            
            // Convert var to const for variables that are never reassigned
            var constPattern = @"\bvar\s+(\w+)\s*=\s*([^;]+);";
            var constMatches = Regex.Matches(code, constPattern);
            
            foreach (Match match in constMatches)
            {
                var varName = match.Groups[1].Value;
                var assignment = match.Groups[2].Value;
                
                // Simple heuristic: if it's a literal or function call, make it const
                if (IsLikelyConstant(assignment) && !IsReassigned(code, varName, match.Index))
                {
                    code = code.Replace(match.Value, $"const {varName} = {assignment};");
                }
                else
                {
                    code = code.Replace(match.Value, $"let {varName} = {assignment};");
                }
            }

            // Convert remaining var declarations to let
            code = Regex.Replace(code, @"\bvar\b", "let");

            return await Task.FromResult(code);
        }

        /// <summary>
        /// Converts function expressions to arrow functions where appropriate
        /// </summary>
        private async Task<string> ConvertToArrowFunctionsAsync(string code)
        {
            // Convert simple function expressions to arrow functions
            var functionPattern = @"function\s*\(([^)]*)\)\s*{([^{}]*)}";
            
            code = Regex.Replace(code, functionPattern, match =>
            {
                var parameters = match.Groups[1].Value;
                var body = match.Groups[2].Value.Trim();
                
                // Simple single-statement functions
                if (!body.Contains('\n') && !body.Contains("return"))
                {
                    return $"({parameters}) => {body}";
                }
                else if (body.StartsWith("return ") && !body.Contains('\n'))
                {
                    var returnValue = body.Substring(7).TrimEnd(';');
                    return $"({parameters}) => {returnValue}";
                }
                
                return match.Value; // Keep original if complex
            });

            return await Task.FromResult(code);
        }

        /// <summary>
        /// Converts == to === for strict equality
        /// </summary>
        private async Task<string> UseStrictEqualityAsync(string code)
        {
            // Replace == with === (but not ===)
            code = Regex.Replace(code, @"(?<!!)==(?!=)", "===");
            
            // Replace != with !== (but not !==)
            code = Regex.Replace(code, @"!=(?!=)", "!==");

            return await Task.FromResult(code);
        }

        /// <summary>
        /// Optimizes string concatenation to use template literals
        /// </summary>
        private async Task<string> OptimizeStringConcatenationAsync(string code)
        {
            // Convert simple string concatenation to template literals
            var concatPattern = @"""([^""]*)""\s*\+\s*(\w+)\s*\+\s*""([^""]*)""";
            
            code = Regex.Replace(code, concatPattern, match =>
            {
                var prefix = match.Groups[1].Value;
                var variable = match.Groups[2].Value;
                var suffix = match.Groups[3].Value;
                
                return $"`{prefix}${{{variable}}}{suffix}`";
            });

            return await Task.FromResult(code);
        }

        /// <summary>
        /// Simplifies conditional expressions
        /// </summary>
        private async Task<string> SimplifyConditionalExpressionsAsync(string code)
        {
            // Simplify boolean comparisons
            code = Regex.Replace(code, @"===\s*true\b", "");
            code = Regex.Replace(code, @"===\s*false\b", " === false");
            code = Regex.Replace(code, @"!==\s*true\b", " === false");
            code = Regex.Replace(code, @"!==\s*false\b", "");

            // Simplify if statements with boolean returns
            var booleanReturnPattern = @"if\s*\(([^)]+)\)\s*{\s*return\s+true;\s*}\s*return\s+false;";
            code = Regex.Replace(code, booleanReturnPattern, "return $1;");

            return await Task.FromResult(code);
        }

        /// <summary>
        /// Removes unnecessary code elements
        /// </summary>
        private async Task<string> RemoveUnnecessaryCodeAsync(string code)
        {
            // Remove unnecessary semicolons after closing braces
            code = Regex.Replace(code, @"}\s*;", "}");

            // Remove trailing whitespace
            var lines = code.Split('\n');
            lines = lines.Select(line => line.TrimEnd()).ToArray();
            code = string.Join("\n", lines);

            // Remove multiple consecutive empty lines
            code = Regex.Replace(code, @"\n\s*\n\s*\n", "\n\n");

            return await Task.FromResult(code);
        }

        /// <summary>
        /// Checks if a value is likely to be a constant
        /// </summary>
        private bool IsLikelyConstant(string value)
        {
            value = value.Trim();
            
            // Literals
            if (value.StartsWith("\"") || value.StartsWith("'") || value.StartsWith("`"))
                return true;
            
            // Numbers
            if (double.TryParse(value, out _))
                return true;
            
            // Booleans
            if (value == "true" || value == "false")
                return true;
            
            // Arrays and objects (simple heuristic)
            if (value.StartsWith("[") || value.StartsWith("{"))
                return true;
            
            // Function calls (might be constant)
            if (value.Contains("(") && value.Contains(")"))
                return true;
            
            return false;
        }

        /// <summary>
        /// Checks if a variable is reassigned after its declaration
        /// </summary>
        private bool IsReassigned(string code, string varName, int declarationIndex)
        {
            // Simple check for reassignment after declaration
            var afterDeclaration = code.Substring(declarationIndex);
            var reassignmentPattern = $@"\b{Regex.Escape(varName)}\s*=(?!=)";
            
            var matches = Regex.Matches(afterDeclaration, reassignmentPattern);
            
            // Skip the first match (which is the declaration itself)
            return matches.Count > 1;
        }

        /// <summary>
        /// Shuts down the refactoring engine asynchronously
        /// </summary>
        public async Task ShutdownAsync()
        {
            // Clean up resources
            await Task.CompletedTask;
        }

        /// <summary>
        /// Disposes the refactoring engine and releases resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                }
                _disposed = true;
            }
        }
    }
}