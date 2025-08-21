using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace A3sist.Orchastrator.Agents.JavaScript.Services
{
    /// <summary>
    /// Provides comprehensive code analysis capabilities for JavaScript and TypeScript code
    /// </summary>
    public class JavaScriptAnalyzer : IDisposable
    {
        private bool _disposed = false;
        private readonly Dictionary<string, int> _keywordCounts;
        private readonly List<string> _issues;

        public JavaScriptAnalyzer()
        {
            _keywordCounts = new Dictionary<string, int>();
            _issues = new List<string>();
        }

        /// <summary>
        /// Initializes the JavaScript analyzer asynchronously
        /// </summary>
        public async Task InitializeAsync()
        {
            // Initialize analyzer components
            await Task.CompletedTask;
        }

        /// <summary>
        /// Analyzes JavaScript/TypeScript code and provides detailed feedback
        /// </summary>
        /// <param name="code">The JavaScript/TypeScript code to analyze</param>
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
                results.Add("=== JavaScript/TypeScript Analysis ===");
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

                // 4. Security analysis
                results.Add("=== Security Analysis ===");
                var securityAnalysis = await AnalyzeSecurityAsync(code);
                results.AddRange(securityAnalysis);

                // 5. Performance analysis
                results.Add("=== Performance Analysis ===");
                var performanceAnalysis = await AnalyzePerformanceAsync(code);
                results.AddRange(performanceAnalysis);

                return results.Any() ? string.Join(Environment.NewLine, results) : "No issues found. Code analysis completed successfully.";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to analyze JavaScript code.", ex);
            }
        }

        /// <summary>
        /// Performs linting-style analysis on JavaScript/TypeScript code
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

                // Check for trailing whitespace
                if (line.EndsWith(" ") || line.EndsWith("\t"))
                {
                    lintIssues.Add($"Line {lineNumber}: Trailing whitespace");
                }

                // Check for missing semicolons (simple check)
                if (line.Trim().EndsWith("}") == false && 
                    line.Trim().EndsWith(";") == false && 
                    line.Trim().Length > 0 &&
                    !line.Trim().StartsWith("//") &&
                    !line.Trim().StartsWith("/*") &&
                    !line.Contains("if ") &&
                    !line.Contains("for ") &&
                    !line.Contains("while ") &&
                    !line.Contains("function ") &&
                    !line.Contains("class "))
                {
                    if (line.Contains("=") || line.Contains("return") || line.Contains("console."))
                    {
                        lintIssues.Add($"Line {lineNumber}: Missing semicolon");
                    }
                }

                // Check for var usage (prefer let/const)
                if (line.Contains("var "))
                {
                    lintIssues.Add($"Line {lineNumber}: Use 'let' or 'const' instead of 'var'");
                }

                // Check for == usage (prefer ===)
                if (line.Contains("==") && !line.Contains("==="))
                {
                    lintIssues.Add($"Line {lineNumber}: Use '===' instead of '=='");
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

            // Check for basic syntax patterns
            var functionCount = Regex.Matches(code, @"\bfunction\s+\w+").Count;
            var arrowFunctionCount = Regex.Matches(code, @"=>\s*{").Count;
            var classCount = Regex.Matches(code, @"\bclass\s+\w+").Count;
            var constCount = Regex.Matches(code, @"\bconst\s+\w+").Count;
            var letCount = Regex.Matches(code, @"\blet\s+\w+").Count;
            var varCount = Regex.Matches(code, @"\bvar\s+\w+").Count;

            results.Add($"  Functions: {functionCount}");
            results.Add($"  Arrow functions: {arrowFunctionCount}");
            results.Add($"  Classes: {classCount}");
            results.Add($"  const declarations: {constCount}");
            results.Add($"  let declarations: {letCount}");
            results.Add($"  var declarations: {varCount}");

            // Check for TypeScript-specific syntax
            var interfaceCount = Regex.Matches(code, @"\binterface\s+\w+").Count;
            var typeCount = Regex.Matches(code, @"\btype\s+\w+").Count;
            var enumCount = Regex.Matches(code, @"\benum\s+\w+").Count;

            if (interfaceCount > 0 || typeCount > 0 || enumCount > 0)
            {
                results.Add("  TypeScript features detected:");
                if (interfaceCount > 0) results.Add($"    Interfaces: {interfaceCount}");
                if (typeCount > 0) results.Add($"    Type aliases: {typeCount}");
                if (enumCount > 0) results.Add($"    Enums: {enumCount}");
            }

            return await Task.FromResult(results);
        }

        private async Task<List<string>> AnalyzeCodeStructureAsync(string code)
        {
            var results = new List<string>();

            // Analyze imports/exports
            var importCount = Regex.Matches(code, @"\bimport\s+").Count;
            var exportCount = Regex.Matches(code, @"\bexport\s+").Count;
            var requireCount = Regex.Matches(code, @"\brequire\s*\(").Count;

            results.Add($"  Imports: {importCount}");
            results.Add($"  Exports: {exportCount}");
            results.Add($"  Requires: {requireCount}");

            // Analyze module patterns
            if (importCount > 0 || exportCount > 0)
            {
                results.Add("  Module system: ES6 modules");
            }
            else if (requireCount > 0)
            {
                results.Add("  Module system: CommonJS");
            }
            else
            {
                results.Add("  Module system: None detected");
            }

            // Analyze async patterns
            var asyncFunctionCount = Regex.Matches(code, @"\basync\s+function").Count;
            var awaitCount = Regex.Matches(code, @"\bawait\s+").Count;
            var promiseCount = Regex.Matches(code, @"\.then\s*\(|\.catch\s*\(|new\s+Promise").Count;

            if (asyncFunctionCount > 0 || awaitCount > 0 || promiseCount > 0)
            {
                results.Add("  Async patterns:");
                if (asyncFunctionCount > 0) results.Add($"    Async functions: {asyncFunctionCount}");
                if (awaitCount > 0) results.Add($"    Await expressions: {awaitCount}");
                if (promiseCount > 0) results.Add($"    Promise usage: {promiseCount}");
            }

            return await Task.FromResult(results);
        }

        private async Task<List<string>> AnalyzeCodeQualityAsync(string code)
        {
            var results = new List<string>();
            var issues = new List<string>();

            // Calculate basic metrics
            var lines = code.Split('\n');
            var totalLines = lines.Length;
            var codeLines = lines.Count(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("//"));
            var commentLines = lines.Count(line => line.Trim().StartsWith("//") || line.Trim().StartsWith("/*"));

            results.Add($"  Total lines: {totalLines}");
            results.Add($"  Code lines: {codeLines}");
            results.Add($"  Comment lines: {commentLines}");

            // Check for code quality issues
            if (Regex.IsMatch(code, @"\bvar\s+"))
            {
                issues.Add("    Use 'let' or 'const' instead of 'var'");
            }

            if (Regex.IsMatch(code, @"==(?!=)"))
            {
                issues.Add("    Use '===' instead of '=='");
            }

            if (Regex.IsMatch(code, @"console\.log"))
            {
                issues.Add("    Remove console.log statements in production code");
            }

            // Check for long functions (simple heuristic)
            var functionMatches = Regex.Matches(code, @"function\s+\w+[^{]*{([^{}]*{[^{}]*}[^{}]*)*[^{}]*}", RegexOptions.Singleline);
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

        private async Task<List<string>> AnalyzeSecurityAsync(string code)
        {
            var results = new List<string>();
            var securityIssues = new List<string>();

            // Check for potential security issues
            if (Regex.IsMatch(code, @"\beval\s*\("))
            {
                securityIssues.Add("    Avoid using eval() - security risk");
            }

            if (Regex.IsMatch(code, @"innerHTML\s*="))
            {
                securityIssues.Add("    innerHTML assignment may be vulnerable to XSS");
            }

            if (Regex.IsMatch(code, @"document\.write\s*\("))
            {
                securityIssues.Add("    document.write() can be a security risk");
            }

            if (Regex.IsMatch(code, @"setTimeout\s*\(\s*[""']"))
            {
                securityIssues.Add("    setTimeout with string argument can be a security risk");
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

        private async Task<List<string>> AnalyzePerformanceAsync(string code)
        {
            var results = new List<string>();
            var performanceIssues = new List<string>();

            // Check for potential performance issues
            if (Regex.IsMatch(code, @"for\s*\([^)]*\.length[^)]*\)"))
            {
                performanceIssues.Add("    Cache array length in for loops");
            }

            if (Regex.IsMatch(code, @"document\.getElementById\s*\([^)]*\)\s*\."))
            {
                var domQueryCount = Regex.Matches(code, @"document\.getElementById").Count;
                if (domQueryCount > 3)
                {
                    performanceIssues.Add($"    {domQueryCount} DOM queries - consider caching elements");
                }
            }

            if (Regex.IsMatch(code, @"setInterval\s*\(.*,\s*[1-9]\d*\s*\)"))
            {
                performanceIssues.Add("    Short interval timers may impact performance");
            }

            if (performanceIssues.Any())
            {
                results.Add("  Performance suggestions:");
                results.AddRange(performanceIssues);
            }
            else
            {
                results.Add("  No obvious performance issues found");
            }

            return await Task.FromResult(results);
        }

        /// <summary>
        /// Shuts down the analyzer asynchronously
        /// </summary>
        public async Task ShutdownAsync()
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