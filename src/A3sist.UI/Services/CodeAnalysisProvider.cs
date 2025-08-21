using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;

namespace A3sist.UI.Services
{
    /// <summary>
    /// Provides code analysis and suggestions for the editor
    /// </summary>
    public class CodeAnalysisProvider : ICodeAnalysisService
    {
        private readonly IOrchestrator _orchestrator;
        private readonly ILogger<CodeAnalysisProvider> _logger;
        private readonly Dictionary<string, DateTime> _lastAnalysisTime = new();
        private readonly Dictionary<string, CodeAnalysisResult> _analysisCache = new();
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(5);

        public CodeAnalysisProvider(IOrchestrator orchestrator, ILogger<CodeAnalysisProvider> logger)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CodeAnalysisResult> AnalyzeCodeAsync(string code, string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Starting code analysis for file: {FilePath}", filePath);

                // Check cache first
                if (IsCacheValid(filePath))
                {
                    _logger.LogDebug("Returning cached analysis for file: {FilePath}", filePath);
                    return _analysisCache[filePath];
                }

                // Create analysis request
                var request = new AgentRequest
                {
                    Prompt = "Analyze code structure and patterns",
                    FilePath = filePath,
                    Content = code,
                    PreferredAgentType = AgentType.Designer,
                    Context = new Dictionary<string, object>
                    {
                        ["AnalysisType"] = "FullAnalysis",
                        ["IncludePatterns"] = true,
                        ["IncludeComplexity"] = true,
                        ["IncludeCodeSmells"] = true,
                        ["IncludeDependencies"] = true
                    }
                };

                // Process through orchestrator
                var result = await _orchestrator.ProcessRequestAsync(request, cancellationToken);

                if (result.Success && result.Metadata.ContainsKey("CodeAnalysis"))
                {
                    var analysisResult = result.Metadata["CodeAnalysis"] as CodeAnalysisResult;
                    if (analysisResult != null)
                    {
                        // Cache the result
                        _analysisCache[filePath] = analysisResult;
                        _lastAnalysisTime[filePath] = DateTime.UtcNow;

                        _logger.LogDebug("Code analysis completed for file: {FilePath}", filePath);
                        return analysisResult;
                    }
                }

                // Fallback: create basic analysis result
                var fallbackResult = CreateBasicAnalysisResult(code, filePath);
                _analysisCache[filePath] = fallbackResult;
                _lastAnalysisTime[filePath] = DateTime.UtcNow;

                _logger.LogWarning("Using fallback analysis for file: {FilePath}", filePath);
                return fallbackResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing code for file: {FilePath}", filePath);
                return CreateBasicAnalysisResult(code, filePath);
            }
        }

        public async Task<IEnumerable<CodeSmell>> DetectCodeSmellsAsync(string code, string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                var analysisResult = await AnalyzeCodeAsync(code, filePath, cancellationToken);
                return analysisResult.CodeSmells ?? Enumerable.Empty<CodeSmell>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting code smells for file: {FilePath}", filePath);
                return Enumerable.Empty<CodeSmell>();
            }
        }

        public async Task<ComplexityMetrics> CalculateComplexityAsync(string code, string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                var analysisResult = await AnalyzeCodeAsync(code, filePath, cancellationToken);
                return analysisResult.Complexity ?? CreateBasicComplexityMetrics(code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating complexity for file: {FilePath}", filePath);
                return CreateBasicComplexityMetrics(code);
            }
        }

        public async Task<DependencyAnalysisResult> AnalyzeDependenciesAsync(string code, string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                var analysisResult = await AnalyzeCodeAsync(code, filePath, cancellationToken);
                return analysisResult.Dependencies ?? new DependencyAnalysisResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing dependencies for file: {FilePath}", filePath);
                return new DependencyAnalysisResult();
            }
        }

        private bool IsCacheValid(string filePath)
        {
            if (!_lastAnalysisTime.ContainsKey(filePath) || !_analysisCache.ContainsKey(filePath))
                return false;

            return DateTime.UtcNow - _lastAnalysisTime[filePath] < _cacheTimeout;
        }

        private CodeAnalysisResult CreateBasicAnalysisResult(string code, string filePath)
        {
            var language = DetermineLanguage(filePath);
            var lines = code.Split('\n');

            return new CodeAnalysisResult
            {
                Language = language,
                Elements = ExtractBasicElements(code, language),
                Patterns = new List<CodePattern>(),
                Complexity = CreateBasicComplexityMetrics(code),
                CodeSmells = DetectBasicCodeSmells(code, lines),
                Dependencies = new DependencyAnalysisResult()
            };
        }

        private ComplexityMetrics CreateBasicComplexityMetrics(string code)
        {
            var lines = code.Split('\n');
            var effectiveLines = lines.Count(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("//"));

            return new ComplexityMetrics
            {
                LinesOfCode = lines.Length,
                EffectiveLinesOfCode = effectiveLines,
                CyclomaticComplexity = EstimateCyclomaticComplexity(code),
                NumberOfMethods = CountMethods(code),
                NumberOfClasses = CountClasses(code),
                MaintainabilityIndex = CalculateMaintainabilityIndex(effectiveLines, EstimateCyclomaticComplexity(code))
            };
        }

        private IEnumerable<CodeElement> ExtractBasicElements(string code, string language)
        {
            var elements = new List<CodeElement>();

            if (language == "C#")
            {
                // Basic C# element extraction
                var lines = code.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (line.Contains("class ") && !line.StartsWith("//"))
                    {
                        var className = ExtractClassName(line);
                        if (!string.IsNullOrEmpty(className))
                        {
                            elements.Add(new CodeElement
                            {
                                Name = className,
                                Type = CodeElementType.Class,
                                StartLine = i + 1,
                                EndLine = FindClassEndLine(lines, i),
                                Signature = line
                            });
                        }
                    }
                    else if (line.Contains("public ") && (line.Contains("(") && line.Contains(")")))
                    {
                        var methodName = ExtractMethodName(line);
                        if (!string.IsNullOrEmpty(methodName))
                        {
                            elements.Add(new CodeElement
                            {
                                Name = methodName,
                                Type = CodeElementType.Method,
                                StartLine = i + 1,
                                EndLine = FindMethodEndLine(lines, i),
                                Signature = line
                            });
                        }
                    }
                }
            }

            return elements;
        }

        private IEnumerable<CodeSmell> DetectBasicCodeSmells(string code, string[] lines)
        {
            var codeSmells = new List<CodeSmell>();

            // Detect long methods
            var methodLines = new List<int>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().Contains("public ") && lines[i].Contains("("))
                {
                    var endLine = FindMethodEndLine(lines, i);
                    if (endLine - i > 50) // Method longer than 50 lines
                    {
                        codeSmells.Add(new CodeSmell
                        {
                            Name = "Long Method",
                            Description = "Method is too long and should be broken down",
                            Type = CodeSmellType.LongMethod,
                            Severity = CodeSmellSeverity.Major,
                            StartLine = i + 1,
                            EndLine = endLine,
                            Suggestion = "Consider breaking this method into smaller, more focused methods"
                        });
                    }
                }
            }

            // Detect magic numbers
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (ContainsMagicNumbers(line))
                {
                    codeSmells.Add(new CodeSmell
                    {
                        Name = "Magic Numbers",
                        Description = "Numeric literals should be replaced with named constants",
                        Type = CodeSmellType.MagicNumbers,
                        Severity = CodeSmellSeverity.Minor,
                        StartLine = i + 1,
                        EndLine = i + 1,
                        Suggestion = "Replace magic numbers with named constants"
                    });
                }
            }

            return codeSmells;
        }

        private string DetermineLanguage(string filePath)
        {
            var extension = System.IO.Path.GetExtension(filePath)?.ToLowerInvariant();
            return extension switch
            {
                ".cs" => "C#",
                ".js" => "JavaScript",
                ".ts" => "TypeScript",
                ".py" => "Python",
                ".java" => "Java",
                ".cpp" or ".cc" or ".cxx" => "C++",
                ".c" => "C",
                _ => "Unknown"
            };
        }

        private int EstimateCyclomaticComplexity(string code)
        {
            // Basic cyclomatic complexity estimation
            var complexity = 1; // Base complexity
            var keywords = new[] { "if", "else", "while", "for", "foreach", "switch", "case", "catch", "&&", "||" };
            
            foreach (var keyword in keywords)
            {
                complexity += CountOccurrences(code, keyword);
            }

            return complexity;
        }

        private int CountMethods(string code)
        {
            return CountOccurrences(code, "public ") + CountOccurrences(code, "private ") + CountOccurrences(code, "protected ");
        }

        private int CountClasses(string code)
        {
            return CountOccurrences(code, "class ");
        }

        private int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += pattern.Length;
            }
            return count;
        }

        private double CalculateMaintainabilityIndex(int effectiveLines, int cyclomaticComplexity)
        {
            // Simplified maintainability index calculation
            if (effectiveLines == 0) return 100;
            
            var maintainabilityIndex = 171 - 5.2 * Math.Log(effectiveLines) - 0.23 * cyclomaticComplexity - 16.2 * Math.Log(effectiveLines);
            return Math.Max(0, Math.Min(100, maintainabilityIndex));
        }

        private string ExtractClassName(string line)
        {
            var parts = line.Split(' ');
            var classIndex = Array.FindIndex(parts, p => p == "class");
            if (classIndex >= 0 && classIndex < parts.Length - 1)
            {
                return parts[classIndex + 1].Split(':')[0].Trim();
            }
            return null;
        }

        private string ExtractMethodName(string line)
        {
            var parenIndex = line.IndexOf('(');
            if (parenIndex > 0)
            {
                var beforeParen = line.Substring(0, parenIndex);
                var parts = beforeParen.Split(' ');
                return parts.LastOrDefault()?.Trim();
            }
            return null;
        }

        private int FindClassEndLine(string[] lines, int startLine)
        {
            int braceCount = 0;
            for (int i = startLine; i < lines.Length; i++)
            {
                braceCount += lines[i].Count(c => c == '{');
                braceCount -= lines[i].Count(c => c == '}');
                if (braceCount == 0 && i > startLine)
                {
                    return i + 1;
                }
            }
            return lines.Length;
        }

        private int FindMethodEndLine(string[] lines, int startLine)
        {
            int braceCount = 0;
            bool foundOpenBrace = false;
            
            for (int i = startLine; i < lines.Length; i++)
            {
                var openBraces = lines[i].Count(c => c == '{');
                var closeBraces = lines[i].Count(c => c == '}');
                
                braceCount += openBraces;
                braceCount -= closeBraces;
                
                if (openBraces > 0) foundOpenBrace = true;
                
                if (foundOpenBrace && braceCount == 0)
                {
                    return i + 1;
                }
            }
            return Math.Min(startLine + 20, lines.Length); // Fallback
        }

        private bool ContainsMagicNumbers(string line)
        {
            // Simple magic number detection - look for numeric literals that aren't 0, 1, or -1
            var words = line.Split(' ', '\t', '(', ')', '[', ']', '{', '}', ',', ';');
            foreach (var word in words)
            {
                if (int.TryParse(word, out int number) && number != 0 && number != 1 && number != -1)
                {
                    return true;
                }
            }
            return false;
        }

        public void ClearCache()
        {
            _analysisCache.Clear();
            _lastAnalysisTime.Clear();
        }

        public void ClearCache(string filePath)
        {
            _analysisCache.Remove(filePath);
            _lastAnalysisTime.Remove(filePath);
        }
    }
}