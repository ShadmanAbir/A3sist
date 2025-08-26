using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace A3sist.Core.Agents.Language.Python.Services
{
    /// <summary>
    /// Provides comprehensive code analysis capabilities for Python code
    /// </summary>
    public class PythonAnalyzer : IDisposable
    {
        private bool _disposed = false;
        private readonly Dictionary<string, int> _keywordCounts;
        private readonly List<string> _issues;

        public PythonAnalyzer()
        {
            _keywordCounts = new Dictionary<string, int>();
            _issues = new List<string>();
        }

        /// <summary>
        /// Initializes the Python analyzer asynchronously
        /// </summary>
        public async System.Threading.Tasks.Task InitializeAsync()
        {
            // Check if Python is available
            try
            {
                var result = await ExecutePythonCommandAsync("--version");
                if (!result.Success)
                {
                    throw new InvalidOperationException("Python is not available or not installed");
                }
            }
            catch (Exception ex)
            {
                // Python might not be available, but we can still do basic analysis
                Console.WriteLine($"Warning: Python not available for advanced analysis: {ex.Message}");
            }
        }

        /// <summary>
        /// Analyzes Python code and provides detailed feedback
        /// </summary>
        /// <param name="code">The Python code to analyze</param>
        /// <returns>Analysis results with detailed information</returns>
        public async Task<string> AnalyzeCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentNullException(nameof(code), "Code cannot be null or empty.");

            try
            {
                var results = new List<string>();
                _issues.Clear();
                _keywordCounts.Clear();

                // 1. Basic syntax analysis
                results.Add("=== Python Code Analysis ===");
                var syntaxAnalysis = await AnalyzeSyntaxAsync(code);
                results.AddRange(syntaxAnalysis);

                // 2. Code structure analysis
                results.Add("=== Code Structure ===");
                var structureAnalysis = await AnalyzeCodeStructureAsync(code);
                results.AddRange(structureAnalysis);

                // 3. Code quality metrics
                results.Add("=== Code Quality Metrics ===");
                var qualityAnalysis = await AnalyzeCodeQualityAsync(code);
                results.AddRange(qualityAnalysis);

                // 4. PEP 8 compliance check
                results.Add("=== PEP 8 Compliance ===");
                var pep8Analysis = await AnalyzePep8ComplianceAsync(code);
                results.AddRange(pep8Analysis);

                // 5. Security analysis
                results.Add("=== Security Analysis ===");
                var securityAnalysis = await AnalyzeSecurityAsync(code);
                results.AddRange(securityAnalysis);

                return results.Any() ? string.Join(Environment.NewLine, results) : "No issues found. Code analysis completed successfully.";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to analyze Python code.", ex);
            }
        }

        /// <summary>
        /// Performs linting-style analysis on Python code
        /// </summary>
        /// <param name="code">The code to lint</param>
        /// <returns>Linting results</returns>
        public async Task<string> LintCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentNullException(nameof(code), "Code cannot be null or empty.");

            var lintIssues = new List<string>();

            // Check for common linting issues
            var lines = code.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNumber = i + 1;

                // Check line length (PEP 8: max 79 characters)
                if (line.Length > 79)
                {
                    lintIssues.Add($"Line {lineNumber}: Line too long ({line.Length} > 79 characters)");
                }

                // Check for trailing whitespace
                if (line.EndsWith(" ") || line.EndsWith("\t"))
                {
                    lintIssues.Add($"Line {lineNumber}: Trailing whitespace");
                }

                // Check for tabs (PEP 8: use spaces)
                if (line.Contains("\t"))
                {
                    lintIssues.Add($"Line {lineNumber}: Use spaces instead of tabs");
                }

                // Check for multiple statements on one line
                if (line.Contains(";") && !line.Trim().StartsWith("#"))
                {
                    lintIssues.Add($"Line {lineNumber}: Multiple statements on one line");
                }

                // Check for bare except clauses
                if (Regex.IsMatch(line.Trim(), @"^except\s*:"))
                {
                    lintIssues.Add($"Line {lineNumber}: Bare except clause - specify exception type");
                }

                // Check for unused imports (simple check)
                if (line.Trim().StartsWith("import ") || line.Trim().StartsWith("from "))
                {
                    var importMatch = Regex.Match(line, @"import\s+(\w+)");
                    if (importMatch.Success)
                    {
                        var importName = importMatch.Groups[1].Value;
                        if (!code.Contains(importName + ".") && !code.Contains(importName + "("))
                        {
                            lintIssues.Add($"Line {lineNumber}: Unused import '{importName}'");
                        }
                    }
                }
            }

            var result = lintIssues.Any() 
                ? $"Linting found {lintIssues.Count} issues:\n" + string.Join("\n", lintIssues)
                : "No linting issues found.";

            return await Task.FromResult(result);
        }

        private async Task<List<string>> AnalyzeSyntaxAsync(string code)
        {
            var results = new List<string>();

            // Count different Python constructs
            var functionCount = Regex.Matches(code, @"\bdef\s+\w+").Count;
            var classCount = Regex.Matches(code, @"\bclass\s+\w+").Count;
            var importCount = Regex.Matches(code, @"\bimport\s+").Count;
            var fromImportCount = Regex.Matches(code, @"\bfrom\s+\w+\s+import").Count;
            var decoratorCount = Regex.Matches(code, @"@\w+").Count;

            results.Add($"  Functions: {functionCount}");
            results.Add($"  Classes: {classCount}");
            results.Add($"  Import statements: {importCount}");
            results.Add($"  From-import statements: {fromImportCount}");
            results.Add($"  Decorators: {decoratorCount}");

            // Check for Python version specific features
            var fStringCount = Regex.Matches(code, @"f[""']").Count;
            var walrusOperatorCount = Regex.Matches(code, @":=").Count;
            var typeHintCount = Regex.Matches(code, @":\s*\w+\s*=").Count;

            if (fStringCount > 0 || walrusOperatorCount > 0 || typeHintCount > 0)
            {
                results.Add("  Modern Python features detected:");
                if (fStringCount > 0) results.Add($"    f-strings: {fStringCount}");
                if (walrusOperatorCount > 0) results.Add($"    Walrus operator (:=): {walrusOperatorCount}");
                if (typeHintCount > 0) results.Add($"    Type hints: {typeHintCount}");
            }

            return await Task.FromResult(results);
        }

        private async Task<List<string>> AnalyzeCodeStructureAsync(string code)
        {
            var results = new List<string>();

            // Analyze indentation
            var lines = code.Split('\n');
            var indentationLevels = new Dictionary<int, int>();
            var inconsistentIndentation = false;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var leadingSpaces = line.Length - line.TrimStart().Length;
                if (leadingSpaces > 0)
                {
                    if (indentationLevels.ContainsKey(leadingSpaces))
                        indentationLevels[leadingSpaces]++;
                    else
                        indentationLevels[leadingSpaces] = 1;
                }
            }

            if (indentationLevels.Any())
            {
                var commonIndent = indentationLevels.OrderByDescending(kv => kv.Value).First().Key;
                results.Add($"  Common indentation: {commonIndent} spaces");
                
                if (indentationLevels.Keys.Any(k => k % commonIndent != 0))
                {
                    results.Add("  Warning: Inconsistent indentation detected");
                }
            }

            // Analyze complexity
            var cyclomaticComplexity = CalculateCyclomaticComplexity(code);
            results.Add($"  Cyclomatic complexity: {cyclomaticComplexity}");

            if (cyclomaticComplexity > 10)
            {
                results.Add("  Warning: High cyclomatic complexity - consider refactoring");
            }

            // Analyze docstrings
            var docstringCount = Regex.Matches(code, @"[""{3}].*?[""{3}]", RegexOptions.Singleline).Count;
            results.Add($"  Docstrings: {docstringCount}");

            return await Task.FromResult(results);
        }

        private async Task<List<string>> AnalyzeCodeQualityAsync(string code)
        {
            var results = new List<string>();
            var issues = new List<string>();

            // Calculate basic metrics
            var lines = code.Split('\n');
            var totalLines = lines.Length;
            var codeLines = lines.Count(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("#"));
            var commentLines = lines.Count(line => line.Trim().StartsWith("#"));

            results.Add($"  Total lines: {totalLines}");
            results.Add($"  Code lines: {codeLines}");
            results.Add($"  Comment lines: {commentLines}");

            // Check for code quality issues
            if (Regex.IsMatch(code, @"\bprint\s*\("))
            {
                var printCount = Regex.Matches(code, @"\bprint\s*\(").Count;
                issues.Add($"    {printCount} print statement(s) - consider using logging");
            }

            if (Regex.IsMatch(code, @"except\s*:"))
            {
                issues.Add("    Bare except clauses found - specify exception types");
            }

            if (Regex.IsMatch(code, @"global\s+\w+"))
            {
                issues.Add("    Global variables found - consider alternative design");
            }

            // Check for long functions
            var functionMatches = Regex.Matches(code, @"def\s+\w+[^:]*:.*?(?=\ndef|\nclass|\Z)", RegexOptions.Singleline);
            var longFunctions = functionMatches.Cast<Match>().Where(m => m.Value.Split('\n').Length > 20).Count();
            
            if (longFunctions > 0)
            {
                issues.Add($"    {longFunctions} function(s) are longer than 20 lines");
            }

            if (issues.Any())
            {
                results.Add("  Quality issues:");
                results.AddRange(issues);
            }
            else
            {
                results.Add("  No quality issues found");
            }

            return await Task.FromResult(results);
        }

        private async Task<List<string>> AnalyzePep8ComplianceAsync(string code)
        {
            var results = new List<string>();
            var pep8Issues = new List<string>();

            var lines = code.Split('\n');

            // Check various PEP 8 rules
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNumber = i + 1;

                // Line length
                if (line.Length > 79)
                {
                    pep8Issues.Add($"    Line {lineNumber}: Line too long ({line.Length} > 79)");
                }

                // Whitespace around operators
                if (Regex.IsMatch(line, @"\w[+\-*/=<>!]=?\w"))
                {
                    pep8Issues.Add($"    Line {lineNumber}: Missing whitespace around operator");
                }

                // Function/class naming
                var functionMatch = Regex.Match(line, @"def\s+([A-Z]\w*)");
                if (functionMatch.Success)
                {
                    pep8Issues.Add($"    Line {lineNumber}: Function name should be lowercase with underscores");
                }

                var classMatch = Regex.Match(line, @"class\s+([a-z]\w*)");
                if (classMatch.Success)
                {
                    pep8Issues.Add($"    Line {lineNumber}: Class name should use CapWords convention");
                }
            }

            if (pep8Issues.Any())
            {
                results.Add($"  PEP 8 violations found ({pep8Issues.Count}):");
                results.AddRange(pep8Issues.Take(10)); // Limit to first 10 issues
                if (pep8Issues.Count > 10)
                {
                    results.Add($"    ... and {pep8Issues.Count - 10} more issues");
                }
            }
            else
            {
                results.Add("  No PEP 8 violations found");
            }

            return await Task.FromResult(results);
        }

        private async Task<List<string>> AnalyzeSecurityAsync(string code)
        {
            var results = new List<string>();
            var securityIssues = new List<string>();

            // Check for potential security issues
            if (Regex.IsMatch(code, @"\beval\s*\("))
            {
                securityIssues.Add("    Avoid using eval() - security risk");
            }

            if (Regex.IsMatch(code, @"\bexec\s*\("))
            {
                securityIssues.Add("    Avoid using exec() - security risk");
            }

            if (Regex.IsMatch(code, @"subprocess\.call\s*\([^)]*shell\s*=\s*True"))
            {
                securityIssues.Add("    subprocess.call with shell=True can be a security risk");
            }

            if (Regex.IsMatch(code, @"pickle\.loads?\s*\("))
            {
                securityIssues.Add("    pickle.load/loads can be unsafe with untrusted data");
            }

            if (Regex.IsMatch(code, @"input\s*\("))
            {
                securityIssues.Add("    input() can be a security risk in Python 2 (use raw_input)");
            }

            if (securityIssues.Any())
            {
                results.Add("  Security concerns:");
                results.AddRange(securityIssues);
            }
            else
            {
                results.Add("  No obvious security issues found");
            }

            return await Task.FromResult(results);
        }

        private int CalculateCyclomaticComplexity(string code)
        {
            var complexity = 1; // Base complexity

            // Count decision points
            var decisionKeywords = new[] { "if", "elif", "while", "for", "except", "and", "or" };
            
            foreach (var keyword in decisionKeywords)
            {
                var pattern = $@"\b{keyword}\b";
                complexity += Regex.Matches(code, pattern).Count;
            }

            return complexity;
        }

        private async Task<(bool Success, string Output, string Error)> ExecutePythonCommandAsync(string arguments)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                var output = await outputTask;
                var error = await errorTask;

                return (process.ExitCode == 0, output, error);
            }
            catch (Exception ex)
            {
                return (false, "", ex.Message);
            }
        }

        /// <summary>
        /// Shuts down the analyzer asynchronously
        /// </summary>
        public async System.Threading.Tasks.Task ShutdownAsync()
        {
            _keywordCounts.Clear();
            _issues.Clear();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Disposes the analyzer and releases resources
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
                    _keywordCounts.Clear();
                    _issues.Clear();
                }
                _disposed = true;
            }
        }
    }
}