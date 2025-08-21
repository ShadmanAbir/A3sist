using A3sist.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Core.Services
{
    /// <summary>
    /// Basic code analysis service for detecting patterns and code smells
    /// </summary>
    public class CodeAnalysisService : ICodeAnalysisService
    {
        private readonly ILogger<CodeAnalysisService> _logger;

        public CodeAnalysisService(ILogger<CodeAnalysisService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CodeAnalysisResult> AnalyzeCodeAsync(string code, string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(code))
            {
                return new CodeAnalysisResult
                {
                    Language = DetermineLanguage(filePath),
                    Elements = Enumerable.Empty<CodeElement>(),
                    Patterns = Enumerable.Empty<CodePattern>(),
                    Complexity = new ComplexityMetrics(),
                    CodeSmells = Enumerable.Empty<CodeSmell>(),
                    Dependencies = new DependencyAnalysisResult()
                };
            }

            try
            {
                var language = DetermineLanguage(filePath);
                
                var elements = await ExtractCodeElementsAsync(code, language, cancellationToken);
                var patterns = await DetectPatternsAsync(code, language, cancellationToken);
                var complexity = await CalculateComplexityAsync(code, filePath, cancellationToken);
                var codeSmells = await DetectCodeSmellsAsync(code, filePath, cancellationToken);
                var dependencies = await AnalyzeDependenciesAsync(code, filePath, cancellationToken);

                return new CodeAnalysisResult
                {
                    Language = language,
                    Elements = elements,
                    Patterns = patterns,
                    Complexity = complexity,
                    CodeSmells = codeSmells,
                    Dependencies = dependencies
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing code");
                return new CodeAnalysisResult
                {
                    Language = DetermineLanguage(filePath),
                    Elements = Enumerable.Empty<CodeElement>(),
                    Patterns = Enumerable.Empty<CodePattern>(),
                    Complexity = new ComplexityMetrics(),
                    CodeSmells = Enumerable.Empty<CodeSmell>(),
                    Dependencies = new DependencyAnalysisResult()
                };
            }
        }

        public async Task<IEnumerable<CodeSmell>> DetectCodeSmellsAsync(string code, string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(code))
                return Enumerable.Empty<CodeSmell>();

            try
            {
                var codeSmells = new List<CodeSmell>();
                var lines = code.Split('\n');

                // Detect long methods
                codeSmells.AddRange(DetectLongMethods(code, lines));

                // Detect large classes
                codeSmells.AddRange(DetectLargeClasses(code, lines));

                // Detect long parameter lists
                codeSmells.AddRange(DetectLongParameterLists(code, lines));

                // Detect duplicated code
                codeSmells.AddRange(DetectDuplicatedCode(code, lines));

                // Detect magic numbers
                codeSmells.AddRange(DetectMagicNumbers(code, lines));

                // Detect complex conditionals
                codeSmells.AddRange(DetectComplexConditionals(code, lines));

                // Detect dead code
                codeSmells.AddRange(DetectDeadCode(code, lines));

                // Detect naming issues
                codeSmells.AddRange(DetectNamingIssues(code, lines));

                return codeSmells.OrderByDescending(cs => cs.Severity).ThenBy(cs => cs.StartLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting code smells");
                return Enumerable.Empty<CodeSmell>();
            }
        }

        public async Task<ComplexityMetrics> CalculateComplexityAsync(string code, string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(code))
                return new ComplexityMetrics();

            try
            {
                var lines = code.Split('\n');
                var effectiveLines = lines.Where(l => !string.IsNullOrWhiteSpace(l.Trim()) && !l.Trim().StartsWith("//")).ToArray();

                var cyclomaticComplexity = CalculateCyclomaticComplexity(code);
                var numberOfMethods = CountMethods(code);
                var numberOfClasses = CountClasses(code);
                var numberOfInterfaces = CountInterfaces(code);

                return new ComplexityMetrics
                {
                    CyclomaticComplexity = cyclomaticComplexity,
                    LinesOfCode = lines.Length,
                    EffectiveLinesOfCode = effectiveLines.Length,
                    NumberOfMethods = numberOfMethods,
                    NumberOfClasses = numberOfClasses,
                    NumberOfInterfaces = numberOfInterfaces,
                    DepthOfInheritance = CalculateDepthOfInheritance(code),
                    NumberOfChildren = 0, // Would need more sophisticated analysis
                    CouplingBetweenObjects = CalculateCoupling(code),
                    LackOfCohesion = CalculateLackOfCohesion(code),
                    WeightedMethodsPerClass = numberOfMethods > 0 ? cyclomaticComplexity / numberOfMethods : 0,
                    ResponseForClass = numberOfMethods,
                    MaintainabilityIndex = CalculateMaintainabilityIndex(cyclomaticComplexity, effectiveLines.Length, numberOfMethods)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating complexity metrics");
                return new ComplexityMetrics();
            }
        }

        public async Task<DependencyAnalysisResult> AnalyzeDependenciesAsync(string code, string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(code))
                return new DependencyAnalysisResult();

            try
            {
                var dependencies = ExtractDependencies(code);
                var externalDependencies = ExtractExternalDependencies(code);
                var unusedDependencies = DetectUnusedDependencies(code, externalDependencies);

                return new DependencyAnalysisResult
                {
                    Dependencies = dependencies,
                    ExternalDependencies = externalDependencies,
                    UnusedDependencies = unusedDependencies,
                    CircularDependencies = Enumerable.Empty<CircularDependency>(), // Would need more sophisticated analysis
                    CouplingMetric = CalculateCoupling(code),
                    CohesionMetric = 1.0 - CalculateLackOfCohesion(code)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing dependencies");
                return new DependencyAnalysisResult();
            }
        }

        #region Private Methods

        private string DetermineLanguage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "unknown";

            var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            
            return extension switch
            {
                ".cs" => "csharp",
                ".js" => "javascript",
                ".ts" => "typescript",
                ".py" => "python",
                ".java" => "java",
                ".cpp" or ".cc" or ".cxx" => "cpp",
                ".c" => "c",
                _ => "unknown"
            };
        }

        private async Task<IEnumerable<CodeElement>> ExtractCodeElementsAsync(string code, string language, CancellationToken cancellationToken)
        {
            var elements = new List<CodeElement>();
            var lines = code.Split('\n');

            // Basic extraction - would be much more sophisticated in real implementation
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // Detect classes
                if (Regex.IsMatch(line, @"\b(class|interface|struct|enum)\s+\w+"))
                {
                    var match = Regex.Match(line, @"\b(class|interface|struct|enum)\s+(\w+)");
                    if (match.Success)
                    {
                        elements.Add(new CodeElement
                        {
                            Name = match.Groups[2].Value,
                            Type = MapToCodeElementType(match.Groups[1].Value),
                            StartLine = i + 1,
                            EndLine = FindEndOfBlock(lines, i),
                            Signature = line
                        });
                    }
                }
                
                // Detect methods
                if (Regex.IsMatch(line, @"\b(public|private|protected|internal).*\w+\s*\([^)]*\)"))
                {
                    var match = Regex.Match(line, @"\b\w+\s*\([^)]*\)");
                    if (match.Success)
                    {
                        var methodName = match.Value.Split('(')[0].Trim();
                        elements.Add(new CodeElement
                        {
                            Name = methodName,
                            Type = CodeElementType.Method,
                            StartLine = i + 1,
                            EndLine = FindEndOfBlock(lines, i),
                            Signature = line
                        });
                    }
                }
            }

            return elements;
        }

        private async Task<IEnumerable<CodePattern>> DetectPatternsAsync(string code, string language, CancellationToken cancellationToken)
        {
            var patterns = new List<CodePattern>();

            // Detect common patterns
            if (code.Contains("Singleton") || Regex.IsMatch(code, @"private\s+static.*instance", RegexOptions.IgnoreCase))
            {
                patterns.Add(new CodePattern
                {
                    Name = "Singleton Pattern",
                    Description = "Singleton design pattern detected",
                    Type = PatternType.DesignPattern,
                    Confidence = 0.8
                });
            }

            if (code.Contains("Factory") || Regex.IsMatch(code, @"Create\w*\(", RegexOptions.IgnoreCase))
            {
                patterns.Add(new CodePattern
                {
                    Name = "Factory Pattern",
                    Description = "Factory design pattern detected",
                    Type = PatternType.DesignPattern,
                    Confidence = 0.7
                });
            }

            if (Regex.IsMatch(code, @"if\s*\([^)]*\)\s*{\s*return", RegexOptions.IgnoreCase))
            {
                patterns.Add(new CodePattern
                {
                    Name = "Guard Clause",
                    Description = "Guard clause pattern detected",
                    Type = PatternType.BestPractice,
                    Confidence = 0.9
                });
            }

            return patterns;
        }

        private IEnumerable<CodeSmell> DetectLongMethods(string code, string[] lines)
        {
            var codeSmells = new List<CodeSmell>();
            var currentMethodStart = -1;
            var braceCount = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // Detect method start
                if (Regex.IsMatch(line, @"\b(public|private|protected|internal).*\w+\s*\([^)]*\)"))
                {
                    currentMethodStart = i;
                    braceCount = 0;
                }
                
                // Count braces
                braceCount += line.Count(c => c == '{') - line.Count(c => c == '}');
                
                // Method end
                if (currentMethodStart >= 0 && braceCount == 0 && line.Contains('}'))
                {
                    var methodLength = i - currentMethodStart + 1;
                    if (methodLength > 50) // Threshold for long method
                    {
                        codeSmells.Add(new CodeSmell
                        {
                            Name = "Long Method",
                            Description = $"Method is {methodLength} lines long, consider breaking it down",
                            Type = CodeSmellType.LongMethod,
                            Severity = methodLength > 100 ? CodeSmellSeverity.Major : CodeSmellSeverity.Minor,
                            StartLine = currentMethodStart + 1,
                            EndLine = i + 1,
                            Suggestion = "Consider extracting smaller methods",
                            SuggestedRefactorings = new[] { RefactoringType.ExtractMethod }
                        });
                    }
                    currentMethodStart = -1;
                }
            }

            return codeSmells;
        }

        private IEnumerable<CodeSmell> DetectLargeClasses(string code, string[] lines)
        {
            var codeSmells = new List<CodeSmell>();
            
            if (lines.Length > 500) // Threshold for large class
            {
                codeSmells.Add(new CodeSmell
                {
                    Name = "Large Class",
                    Description = $"Class has {lines.Length} lines, consider splitting responsibilities",
                    Type = CodeSmellType.LargeClass,
                    Severity = lines.Length > 1000 ? CodeSmellSeverity.Major : CodeSmellSeverity.Minor,
                    StartLine = 1,
                    EndLine = lines.Length,
                    Suggestion = "Consider splitting into multiple classes with single responsibilities",
                    SuggestedRefactorings = new[] { RefactoringType.ExtractInterface, RefactoringType.MoveMethod }
                });
            }

            return codeSmells;
        }

        private IEnumerable<CodeSmell> DetectLongParameterLists(string code, string[] lines)
        {
            var codeSmells = new List<CodeSmell>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var matches = Regex.Matches(line, @"\w+\s*\([^)]*\)");
                
                foreach (Match match in matches)
                {
                    var parameters = match.Value.Split('(')[1].TrimEnd(')').Split(',');
                    if (parameters.Length > 5 && !string.IsNullOrWhiteSpace(parameters[0])) // Threshold for long parameter list
                    {
                        codeSmells.Add(new CodeSmell
                        {
                            Name = "Long Parameter List",
                            Description = $"Method has {parameters.Length} parameters, consider using parameter objects",
                            Type = CodeSmellType.LongParameterList,
                            Severity = parameters.Length > 8 ? CodeSmellSeverity.Major : CodeSmellSeverity.Minor,
                            StartLine = i + 1,
                            EndLine = i + 1,
                            Suggestion = "Consider introducing parameter objects or builder pattern",
                            SuggestedRefactorings = new[] { RefactoringType.IntroduceParameter, RefactoringType.ExtractMethod }
                        });
                    }
                }
            }

            return codeSmells;
        }

        private IEnumerable<CodeSmell> DetectDuplicatedCode(string code, string[] lines)
        {
            var codeSmells = new List<CodeSmell>();
            var duplicateThreshold = 5; // Minimum lines for duplication
            
            // Simple duplicate detection - compare line sequences
            for (int i = 0; i < lines.Length - duplicateThreshold; i++)
            {
                for (int j = i + duplicateThreshold; j < lines.Length - duplicateThreshold; j++)
                {
                    var duplicateLength = 0;
                    while (i + duplicateLength < lines.Length && 
                           j + duplicateLength < lines.Length &&
                           lines[i + duplicateLength].Trim() == lines[j + duplicateLength].Trim() &&
                           !string.IsNullOrWhiteSpace(lines[i + duplicateLength].Trim()))
                    {
                        duplicateLength++;
                    }
                    
                    if (duplicateLength >= duplicateThreshold)
                    {
                        codeSmells.Add(new CodeSmell
                        {
                            Name = "Duplicated Code",
                            Description = $"Found {duplicateLength} duplicate lines",
                            Type = CodeSmellType.DuplicatedCode,
                            Severity = duplicateLength > 10 ? CodeSmellSeverity.Major : CodeSmellSeverity.Minor,
                            StartLine = i + 1,
                            EndLine = i + duplicateLength,
                            Suggestion = "Consider extracting common code into a method",
                            SuggestedRefactorings = new[] { RefactoringType.ExtractMethod, RefactoringType.ExtractVariable }
                        });
                        
                        j += duplicateLength; // Skip ahead to avoid overlapping duplicates
                    }
                }
            }

            return codeSmells;
        }

        private IEnumerable<CodeSmell> DetectMagicNumbers(string code, string[] lines)
        {
            var codeSmells = new List<CodeSmell>();
            var magicNumberPattern = @"\b\d{2,}\b"; // Numbers with 2+ digits
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var matches = Regex.Matches(line, magicNumberPattern);
                
                foreach (Match match in matches)
                {
                    var number = match.Value;
                    // Skip common non-magic numbers
                    if (number == "10" || number == "100" || number == "1000")
                        continue;
                        
                    codeSmells.Add(new CodeSmell
                    {
                        Name = "Magic Number",
                        Description = $"Magic number '{number}' should be replaced with a named constant",
                        Type = CodeSmellType.MagicNumbers,
                        Severity = CodeSmellSeverity.Minor,
                        StartLine = i + 1,
                        EndLine = i + 1,
                        StartColumn = match.Index,
                        EndColumn = match.Index + match.Length,
                        Suggestion = "Replace with a named constant",
                        SuggestedRefactorings = new[] { RefactoringType.ExtractConstant }
                    });
                }
            }

            return codeSmells;
        }

        private IEnumerable<CodeSmell> DetectComplexConditionals(string code, string[] lines)
        {
            var codeSmells = new List<CodeSmell>();
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                // Count logical operators in conditionals
                if (line.Contains("if") || line.Contains("while") || line.Contains("for"))
                {
                    var andCount = Regex.Matches(line, @"&&").Count;
                    var orCount = Regex.Matches(line, @"\|\|").Count;
                    var totalOperators = andCount + orCount;
                    
                    if (totalOperators > 3) // Threshold for complex conditional
                    {
                        codeSmells.Add(new CodeSmell
                        {
                            Name = "Complex Conditional",
                            Description = $"Conditional has {totalOperators} logical operators, consider simplifying",
                            Type = CodeSmellType.ComplexConditional,
                            Severity = totalOperators > 5 ? CodeSmellSeverity.Major : CodeSmellSeverity.Minor,
                            StartLine = i + 1,
                            EndLine = i + 1,
                            Suggestion = "Consider extracting boolean methods or using guard clauses",
                            SuggestedRefactorings = new[] { RefactoringType.ExtractMethod, RefactoringType.SimplifyExpression }
                        });
                    }
                }
            }

            return codeSmells;
        }

        private IEnumerable<CodeSmell> DetectDeadCode(string code, string[] lines)
        {
            var codeSmells = new List<CodeSmell>();
            
            // Simple dead code detection - look for unreachable code after return statements
            for (int i = 0; i < lines.Length - 1; i++)
            {
                var line = lines[i].Trim();
                
                if (line.EndsWith("return;") || Regex.IsMatch(line, @"return\s+.+;"))
                {
                    // Check if there's non-empty code before the next closing brace
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        var nextLine = lines[j].Trim();
                        
                        if (nextLine == "}" || nextLine.StartsWith("}"))
                            break;
                            
                        if (!string.IsNullOrWhiteSpace(nextLine) && !nextLine.StartsWith("//"))
                        {
                            codeSmells.Add(new CodeSmell
                            {
                                Name = "Dead Code",
                                Description = "Unreachable code after return statement",
                                Type = CodeSmellType.DeadCode,
                                Severity = CodeSmellSeverity.Minor,
                                StartLine = j + 1,
                                EndLine = j + 1,
                                Suggestion = "Remove unreachable code",
                                SuggestedRefactorings = new[] { RefactoringType.RemoveRedundantCode }
                            });
                            break;
                        }
                    }
                }
            }

            return codeSmells;
        }

        private IEnumerable<CodeSmell> DetectNamingIssues(string code, string[] lines)
        {
            var codeSmells = new List<CodeSmell>();
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                // Detect single-letter variable names (except common loop counters)
                var singleLetterVars = Regex.Matches(line, @"\b[a-hj-z]\b"); // Exclude 'i' which is common for loops
                foreach (Match match in singleLetterVars)
                {
                    codeSmells.Add(new CodeSmell
                    {
                        Name = "Poor Naming",
                        Description = $"Single-letter variable '{match.Value}' should have a descriptive name",
                        Type = CodeSmellType.UncommunicativeNames,
                        Severity = CodeSmellSeverity.Minor,
                        StartLine = i + 1,
                        EndLine = i + 1,
                        StartColumn = match.Index,
                        EndColumn = match.Index + match.Length,
                        Suggestion = "Use descriptive variable names",
                        SuggestedRefactorings = new[] { RefactoringType.RenameSymbol }
                    });
                }
            }

            return codeSmells;
        }

        private int CalculateCyclomaticComplexity(string code)
        {
            var complexity = 1; // Base complexity
            
            // Count decision points
            complexity += Regex.Matches(code, @"\bif\b").Count;
            complexity += Regex.Matches(code, @"\belse\b").Count;
            complexity += Regex.Matches(code, @"\bwhile\b").Count;
            complexity += Regex.Matches(code, @"\bfor\b").Count;
            complexity += Regex.Matches(code, @"\bforeach\b").Count;
            complexity += Regex.Matches(code, @"\bswitch\b").Count;
            complexity += Regex.Matches(code, @"\bcase\b").Count;
            complexity += Regex.Matches(code, @"\bcatch\b").Count;
            complexity += Regex.Matches(code, @"&&").Count;
            complexity += Regex.Matches(code, @"\|\|").Count;
            complexity += Regex.Matches(code, @"\?").Count; // Ternary operator
            
            return complexity;
        }

        private int CountMethods(string code)
        {
            return Regex.Matches(code, @"\b(public|private|protected|internal).*\w+\s*\([^)]*\)").Count;
        }

        private int CountClasses(string code)
        {
            return Regex.Matches(code, @"\bclass\s+\w+").Count;
        }

        private int CountInterfaces(string code)
        {
            return Regex.Matches(code, @"\binterface\s+\w+").Count;
        }

        private int CalculateDepthOfInheritance(string code)
        {
            // Simple heuristic - count inheritance levels
            var inheritanceMatches = Regex.Matches(code, @":\s*\w+");
            return inheritanceMatches.Count > 0 ? inheritanceMatches.Count : 1;
        }

        private double CalculateCoupling(string code)
        {
            // Simple coupling metric based on external references
            var externalReferences = Regex.Matches(code, @"\bnew\s+\w+").Count;
            var totalLines = code.Split('\n').Length;
            
            return totalLines > 0 ? (double)externalReferences / totalLines : 0.0;
        }

        private double CalculateLackOfCohesion(string code)
        {
            // Simple cohesion metric - ratio of methods to fields
            var methodCount = CountMethods(code);
            var fieldCount = Regex.Matches(code, @"\b(private|protected|public)\s+\w+\s+\w+;").Count;
            
            if (methodCount == 0) return 1.0;
            if (fieldCount == 0) return 0.0;
            
            return Math.Min(1.0, (double)fieldCount / methodCount);
        }

        private double CalculateMaintainabilityIndex(int cyclomaticComplexity, int linesOfCode, int numberOfMethods)
        {
            // Simplified maintainability index calculation
            if (linesOfCode == 0) return 100.0;
            
            var volume = linesOfCode * Math.Log2(numberOfMethods + 1);
            var maintainabilityIndex = 171 - 5.2 * Math.Log(volume) - 0.23 * cyclomaticComplexity - 16.2 * Math.Log(linesOfCode);
            
            return Math.Max(0.0, Math.Min(100.0, maintainabilityIndex));
        }

        private IEnumerable<Dependency> ExtractDependencies(string code)
        {
            var dependencies = new List<Dependency>();
            
            // Extract using statements/imports
            var usingMatches = Regex.Matches(code, @"using\s+([^;]+);");
            foreach (Match match in usingMatches)
            {
                dependencies.Add(new Dependency
                {
                    From = "current",
                    To = match.Groups[1].Value.Trim(),
                    Type = DependencyType.Import,
                    Strength = 1,
                    Description = "Import dependency"
                });
            }
            
            return dependencies;
        }

        private IEnumerable<string> ExtractExternalDependencies(string code)
        {
            var dependencies = new HashSet<string>();
            
            // Extract using statements
            var usingMatches = Regex.Matches(code, @"using\s+([^;]+);");
            foreach (Match match in usingMatches)
            {
                var dependency = match.Groups[1].Value.Trim();
                if (!dependency.StartsWith("System.") && !dependency.StartsWith("Microsoft."))
                {
                    dependencies.Add(dependency);
                }
            }
            
            return dependencies;
        }

        private IEnumerable<string> DetectUnusedDependencies(string code, IEnumerable<string> externalDependencies)
        {
            var unusedDependencies = new List<string>();
            
            foreach (var dependency in externalDependencies)
            {
                var namespaceParts = dependency.Split('.');
                var isUsed = false;
                
                foreach (var part in namespaceParts)
                {
                    if (code.Contains(part) && code.IndexOf(part) != code.IndexOf($"using {dependency}"))
                    {
                        isUsed = true;
                        break;
                    }
                }
                
                if (!isUsed)
                {
                    unusedDependencies.Add(dependency);
                }
            }
            
            return unusedDependencies;
        }

        private CodeElementType MapToCodeElementType(string keyword)
        {
            return keyword.ToLowerInvariant() switch
            {
                "class" => CodeElementType.Class,
                "interface" => CodeElementType.Interface,
                "struct" => CodeElementType.Struct,
                "enum" => CodeElementType.Enum,
                _ => CodeElementType.Class
            };
        }

        private int FindEndOfBlock(string[] lines, int startIndex)
        {
            var braceCount = 0;
            var foundOpenBrace = false;
            
            for (int i = startIndex; i < lines.Length; i++)
            {
                var line = lines[i];
                
                foreach (var c in line)
                {
                    if (c == '{')
                    {
                        braceCount++;
                        foundOpenBrace = true;
                    }
                    else if (c == '}')
                    {
                        braceCount--;
                        if (foundOpenBrace && braceCount == 0)
                        {
                            return i + 1;
                        }
                    }
                }
            }
            
            return startIndex + 1; // Fallback
        }

        #endregion
    }
}