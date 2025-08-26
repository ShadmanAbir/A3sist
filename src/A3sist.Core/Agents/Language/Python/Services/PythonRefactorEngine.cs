using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace A3sist.Core.Agents.Language.Python.Services
{
    /// <summary>
    /// Provides comprehensive code refactoring capabilities for Python code
    /// </summary>
    public class PythonRefactorEngine : IDisposable
    {
        private bool _disposed = false;

        /// <summary>
        /// Initializes the Python refactoring engine asynchronously
        /// </summary>
        public async Task InitializeAsync()
        {
            // Initialize refactoring engine components
            await Task.CompletedTask;
        }

        /// <summary>
        /// Refactors Python code with multiple improvement techniques
        /// </summary>
        /// <param name="code">The Python code to refactor</param>
        /// <returns>The refactored code</returns>
        public async Task<string> RefactorCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentNullException(nameof(code));

            try
            {
                var refactoredCode = code;

                // Apply various refactoring techniques
                refactoredCode = await OptimizeImportsAsync(refactoredCode);
                refactoredCode = await ConvertToFStringsAsync(refactoredCode);
                refactoredCode = await SimplifyConditionalExpressionsAsync(refactoredCode);
                refactoredCode = await OptimizeListComprehensionsAsync(refactoredCode);
                refactoredCode = await RemoveUnnecessaryCodeAsync(refactoredCode);
                refactoredCode = await FormatCodeAsync(refactoredCode);

                return refactoredCode;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to refactor Python code.", ex);
            }
        }

        /// <summary>
        /// Formats Python code for better readability following PEP 8
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
                var indentString = "    "; // 4 spaces per PEP 8

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    
                    if (string.IsNullOrEmpty(trimmedLine))
                    {
                        formattedLines.Add("");
                        continue;
                    }

                    // Handle dedentation for certain keywords
                    if (IsDeindentKeyword(trimmedLine))
                    {
                        indentLevel = Math.Max(0, indentLevel - 1);
                    }

                    // Add proper indentation
                    var indentedLine = new string(' ', indentLevel * indentString.Length) + trimmedLine;
                    formattedLines.Add(indentedLine);

                    // Handle indentation for certain keywords
                    if (IsIndentKeyword(trimmedLine))
                    {
                        indentLevel++;
                    }
                }

                // Add proper spacing around operators
                var formatted = string.Join(Environment.NewLine, formattedLines);
                formatted = AddOperatorSpacing(formatted);

                return await Task.FromResult(formatted);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to format Python code: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Optimizes import statements
        /// </summary>
        private async Task<string> OptimizeImportsAsync(string code)
        {
            // Group imports: standard library, third-party, local
            var lines = code.Split('\n').ToList();
            var importLines = new List<string>();
            var fromImportLines = new List<string>();
            var otherLines = new List<string>();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("import "))
                {
                    importLines.Add(line);
                }
                else if (trimmed.StartsWith("from "))
                {
                    fromImportLines.Add(line);
                }
                else
                {
                    otherLines.Add(line);
                }
            }

            // Sort imports alphabetically
            importLines.Sort();
            fromImportLines.Sort();

            // Reconstruct code with organized imports
            var result = new List<string>();
            result.AddRange(importLines);
            if (importLines.Any() && fromImportLines.Any())
                result.Add("");
            result.AddRange(fromImportLines);
            if ((importLines.Any() || fromImportLines.Any()) && otherLines.Any(l => !string.IsNullOrWhiteSpace(l)))
                result.Add("");
            result.AddRange(otherLines);

            return await Task.FromResult(string.Join('\n', result));
        }

        /// <summary>
        /// Converts string formatting to f-strings where appropriate
        /// </summary>
        private async Task<string> ConvertToFStringsAsync(string code)
        {
            // Convert .format() to f-strings
            var formatPattern = @"""([^""]*)\""\.format\(([^)]+)\)";
            code = Regex.Replace(code, formatPattern, match =>
            {
                var template = match.Groups[1].Value;
                var args = match.Groups[2].Value.Split(',').Select(a => a.Trim()).ToArray();
                
                // Simple conversion for positional arguments
                var fString = template;
                for (int i = 0; i < args.Length; i++)
                {
                    fString = fString.Replace($"{{{i}}}", $"{{{args[i]}}}");
                }
                
                return $"f\"{fString}\"";
            });

            // Convert % formatting to f-strings
            var percentPattern = @"""([^""]*%[sd].*?)""\s*%\s*\(([^)]+)\)";
            code = Regex.Replace(code, percentPattern, match =>
            {
                var template = match.Groups[1].Value;
                var args = match.Groups[2].Value.Split(',').Select(a => a.Trim()).ToArray();
                
                // Simple conversion for %s and %d
                var fString = template;
                int argIndex = 0;
                fString = Regex.Replace(fString, @"%[sd]", m => 
                {
                    if (argIndex < args.Length)
                        return $"{{{args[argIndex++]}}}";
                    return m.Value;
                });
                
                return $"f\"{fString}\"";
            });

            return await Task.FromResult(code);
        }

        /// <summary>
        /// Simplifies conditional expressions
        /// </summary>
        private async Task<string> SimplifyConditionalExpressionsAsync(string code)
        {
            // Simplify boolean comparisons
            code = Regex.Replace(code, @"==\s*True\b", " is True");
            code = Regex.Replace(code, @"==\s*False\b", " is False");
            code = Regex.Replace(code, @"==\s*None\b", " is None");
            code = Regex.Replace(code, @"!=\s*None\b", " is not None");

            // Simplify if-else with boolean returns
            var booleanReturnPattern = @"if\s+([^:]+):\s*return\s+True\s*else:\s*return\s+False";
            code = Regex.Replace(code, booleanReturnPattern, "return $1", RegexOptions.Multiline);

            // Convert if-else to ternary where appropriate
            var simpleIfElsePattern = @"if\s+([^:]+):\s*(\w+)\s*=\s*([^\\n]+)\s*else:\s*\2\s*=\s*([^\\n]+)";
            code = Regex.Replace(code, simpleIfElsePattern, "$2 = $3 if $1 else $4");

            return await Task.FromResult(code);
        }

        /// <summary>
        /// Optimizes list comprehensions and generator expressions
        /// </summary>
        private async Task<string> OptimizeListComprehensionsAsync(string code)
        {
            // Convert simple for loops to list comprehensions
            var forLoopPattern = @"(\w+)\s*=\s*\[\]\s*for\s+(\w+)\s+in\s+([^:]+):\s*\1\.append\(([^)]+)\)";
            code = Regex.Replace(code, forLoopPattern, "$1 = [$4 for $2 in $3]", RegexOptions.Multiline);

            // Convert filter + map to list comprehension
            var filterMapPattern = @"list\(map\(([^,]+),\s*filter\(([^,]+),\s*([^)]+)\)\)\)";
            code = Regex.Replace(code, filterMapPattern, "[$1(x) for x in $3 if $2(x)]");

            return await Task.FromResult(code);
        }

        /// <summary>
        /// Removes unnecessary code elements
        /// </summary>
        private async Task<string> RemoveUnnecessaryCodeAsync(string code)
        {
            // Remove unnecessary pass statements
            code = Regex.Replace(code, @"\n\s*pass\s*\n", "\n");

            // Remove trailing whitespace
            var lines = code.Split('\n');
            lines = lines.Select(line => line.TrimEnd()).ToArray();
            code = string.Join('\n', lines);

            // Remove multiple consecutive empty lines
            code = Regex.Replace(code, @"\n\s*\n\s*\n", "\n\n");

            // Remove unnecessary parentheses in return statements
            code = Regex.Replace(code, @"return\s+\(([^,()]+)\)", "return $1");

            return await Task.FromResult(code);
        }

        /// <summary>
        /// Checks if a line contains a keyword that should increase indentation
        /// </summary>
        private bool IsIndentKeyword(string line)
        {
            var indentKeywords = new[] { "if ", "elif ", "else:", "for ", "while ", "def ", "class ", "try:", "except", "finally:", "with " };
            return indentKeywords.Any(keyword => line.Contains(keyword)) && line.EndsWith(":");
        }

        /// <summary>
        /// Checks if a line contains a keyword that should decrease indentation
        /// </summary>
        private bool IsDeindentKeyword(string line)
        {
            var deindentKeywords = new[] { "elif ", "else:", "except", "finally:" };
            return deindentKeywords.Any(keyword => line.StartsWith(keyword));
        }

        /// <summary>
        /// Adds proper spacing around operators
        /// </summary>
        private string AddOperatorSpacing(string code)
        {
            // Add spaces around binary operators
            var operators = new[] { "=", "+", "-", "*", "/", "//", "%", "**", "==", "!=", "<", ">", "<=", ">=", "and", "or" };
            
            foreach (var op in operators)
            {
                // Skip if already has proper spacing
                var pattern = $@"(\w)\s*{Regex.Escape(op)}\s*(\w)";
                code = Regex.Replace(code, pattern, $"$1 {op} $2");
            }

            // Add space after commas
            code = Regex.Replace(code, @",(\w)", ", $1");

            // Add space after colons in dictionaries
            code = Regex.Replace(code, @":(\w)", ": $1");

            return code;
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