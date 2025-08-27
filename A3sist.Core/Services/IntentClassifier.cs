using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
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
    /// Intent classifier that analyzes requests to determine user intent
    /// </summary>
    public class IntentClassifier : IIntentClassifier
    {
        private readonly ILogger<IntentClassifier> _logger;
        private readonly Dictionary<string, IntentPattern> _intentPatterns;
        private readonly Dictionary<string, double> _intentWeights;

        public double ConfidenceThreshold => 0.7;

        public IntentClassifier(ILogger<IntentClassifier> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _intentPatterns = new Dictionary<string, IntentPattern>();
            _intentWeights = new Dictionary<string, double>();
            
            InitializeIntentPatterns();
        }

        /// <summary>
        /// Classifies the intent of a request
        /// </summary>
        public async Task<IntentClassification> ClassifyAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogDebug("Classifying intent for request {RequestId}", request.Id);

            try
            {
                var prompt = request.Prompt?.ToLowerInvariant() ?? string.Empty;
                var filePath = request.FilePath ?? string.Empty;
                var content = request.Content ?? string.Empty;

                // Detect programming language
                var language = DetectLanguage(filePath, content);

                // Score all intent patterns
                var intentScores = new Dictionary<string, double>();
                var matchedKeywords = new List<string>();

                foreach (var pattern in _intentPatterns.Values)
                {
                    var score = ScoreIntentPattern(pattern, prompt, content, filePath);
                    if (score > 0)
                    {
                        intentScores[pattern.Intent] = score;
                        matchedKeywords.AddRange(pattern.Keywords.Where(k => 
                            prompt.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0));
                    }
                }

                // Find the best matching intent
                var bestIntent = intentScores.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
                
                if (bestIntent.Key == null)
                {
                    // No patterns matched, return unknown intent
                    return CreateUnknownIntentClassification(language, prompt);
                }

                // Calculate confidence based on score and context
                var confidence = CalculateConfidence(bestIntent.Value, intentScores, prompt);

                // Get alternative intents
                var alternatives = intentScores
                    .Where(kvp => kvp.Key != bestIntent.Key && kvp.Value > 0.1)
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(3)
                    .Select(kvp => new AlternativeIntent
                    {
                        Intent = kvp.Key,
                        Confidence = kvp.Value * 0.8, // Slightly lower than primary
                        SuggestedAgentType = GetAgentTypeForIntent(kvp.Key)
                    })
                    .ToList();

                var classification = new IntentClassification
                {
                    Intent = bestIntent.Key,
                    Confidence = confidence,
                    Language = language,
                    SuggestedAgentType = GetAgentTypeForIntent(bestIntent.Key),
                    Keywords = matchedKeywords.Distinct().ToList(),
                    Alternatives = alternatives,
                    Context = ExtractContext(request, bestIntent.Key)
                };

                _logger.LogDebug("Intent classified as '{Intent}' with confidence {Confidence:F2} for request {RequestId}", 
                    classification.Intent, classification.Confidence, request.Id);

                return classification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying intent for request {RequestId}", request.Id);
                return CreateUnknownIntentClassification("unknown", request.Prompt ?? string.Empty);
            }
        }

        /// <summary>
        /// Trains the classifier with new data (placeholder for future ML implementation)
        /// </summary>
        public async Task TrainAsync(AgentRequest request, string actualIntent, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(actualIntent))
                throw new ArgumentException("Actual intent cannot be null or empty", nameof(actualIntent));

            _logger.LogDebug("Training classifier with request {RequestId} and actual intent '{ActualIntent}'", 
                request.Id, actualIntent);

            // For now, just log the training data
            // In a future implementation, this would update ML models or pattern weights
            await Task.CompletedTask;
        }

        /// <summary>
        /// Initializes the intent patterns used for classification
        /// </summary>
        private void InitializeIntentPatterns()
        {
            _intentPatterns["refactor"] = new IntentPattern
            {
                Intent = "refactor",
                Keywords = new[] { "refactor", "improve", "optimize", "clean", "restructure", "reorganize" },
                Patterns = new[] { @"\brefactor\b", @"\bimprove\b.*\bcode\b", @"\boptimize\b", @"\bclean\s+up\b" },
                Weight = 1.0
            };

            // Code generation intents
            _intentPatterns["generate_code"] = new IntentPattern
            {
                Intent = "generate_code",
                Keywords = new[] { "generate", "create", "write", "implement", "build", "make", "add" },
                Patterns = new[] { @"\bgenerate\b", @"\bcreate\b.*\b(class|method|function)\b", @"\bwrite\b.*\bcode\b", @"\bimplement\b" },
                Weight = 1.0
            };

            _intentPatterns["add_feature"] = new IntentPattern
            {
                Intent = "add_feature",
                Keywords = new[] { "add", "feature", "functionality", "capability", "enhancement" },
                Patterns = new[] { @"\badd\b.*\bfeature\b", @"\bnew\b.*\bfunctionality\b", @"\benhancement\b" },
                Weight = 1.0
            };

            // Code analysis intents
            _intentPatterns["analyze_code"] = new IntentPattern
            {
                Intent = "analyze_code",
                Keywords = new[] { "analyze", "review", "check", "examine", "inspect", "audit" },
                Patterns = new[] { @"\banalyze\b", @"\breview\b.*\bcode\b", @"\bcheck\b", @"\bexamine\b" },
                Weight = 1.0
            };

            _intentPatterns["explain_code"] = new IntentPattern
            {
                Intent = "explain_code",
                Keywords = new[] { "explain", "describe", "what", "how", "why", "understand" },
                Patterns = new[] { @"\bexplain\b", @"\bwhat\s+(does|is)\b", @"\bhow\s+(does|to)\b", @"\bwhy\b" },
                Weight = 1.0
            };

            // Testing intents
            _intentPatterns["generate_tests"] = new IntentPattern
            {
                Intent = "generate_tests",
                Keywords = new[] { "test", "unit test", "testing", "spec", "verify" },
                Patterns = new[] { @"\btest\b", @"\bunit\s+test\b", @"\btesting\b", @"\bspec\b" },
                Weight = 1.0
            };

            // Documentation intents
            _intentPatterns["generate_docs"] = new IntentPattern
            {
                Intent = "generate_docs",
                Keywords = new[] { "document", "documentation", "comment", "doc", "readme" },
                Patterns = new[] { @"\bdocument\b", @"\bdocumentation\b", @"\bcomment\b", @"\breadme\b" },
                Weight = 1.0
            };

            _logger.LogInformation("Initialized {PatternCount} intent patterns", _intentPatterns.Count);
        }

        /// <summary>
        /// Scores an intent pattern against the request
        /// </summary>
        private double ScoreIntentPattern(IntentPattern pattern, string prompt, string content, string filePath)
        {
            double score = 0.0;

            // Score based on keyword matches
            var keywordMatches = pattern.Keywords.Count(keyword => 
                prompt.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
            
            if (keywordMatches > 0)
            {
                score += (double)keywordMatches / pattern.Keywords.Length * 0.6;
            }

            // Score based on regex pattern matches
            foreach (var regexPattern in pattern.Patterns)
            {
                try
                {
                    if (Regex.IsMatch(prompt, regexPattern, RegexOptions.IgnoreCase))
                    {
                        score += 0.4;
                        break; // Only count one pattern match
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error matching regex pattern '{Pattern}'", regexPattern);
                }
            }

            // Apply pattern weight
            score *= pattern.Weight;

            return Math.Min(score, 1.0); // Cap at 1.0
        }

        /// <summary>
        /// Calculates confidence based on scores and context
        /// </summary>
        private double CalculateConfidence(double bestScore, Dictionary<string, double> allScores, string prompt)
        {
            // Base confidence is the score itself
            var confidence = bestScore;

            // Boost confidence if there's a clear winner
            var secondBestScore = allScores.Values.OrderByDescending(s => s).Skip(1).FirstOrDefault();
            if (bestScore > secondBestScore * 1.5)
            {
                confidence += 0.1;
            }

            // Reduce confidence for very short prompts
            if (prompt.Length < 10)
            {
                confidence *= 0.8;
            }

            // Boost confidence for longer, more detailed prompts
            if (prompt.Length > 50)
            {
                confidence += 0.05;
            }

            return Math.Min(confidence, 1.0);
        }

        /// <summary>
        /// Detects the programming language from file path and content
        /// </summary>
        private string DetectLanguage(string filePath, string content)
        {
            // First try to detect from file extension
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
                var languageFromExtension = extension switch
                {
                    ".cs" => "csharp",
                    ".js" => "javascript",
                    ".ts" => "typescript",
                    ".py" => "python",
                    ".java" => "java",
                    ".cpp" or ".cc" or ".cxx" => "cpp",
                    ".c" => "c",
                    ".go" => "go",
                    ".rs" => "rust",
                    _ => null
                };

                if (languageFromExtension != null)
                    return languageFromExtension;
            }

            // Try to detect from content patterns
            if (!string.IsNullOrWhiteSpace(content))
            {
                var contentLower = content.ToLowerInvariant();
                
                if (contentLower.Contains("using system") || contentLower.Contains("namespace"))
                    return "csharp";
                if (contentLower.Contains("function") || contentLower.Contains("const ") || contentLower.Contains("let "))
                    return "javascript";
                if (contentLower.Contains("def ") || contentLower.Contains("import "))
                    return "python";
                if (contentLower.Contains("public class") || contentLower.Contains("import java"))
                    return "java";
            }

            return "unknown";
        }

        /// <summary>
        /// Gets the suggested agent type for an intent
        /// </summary>
        private AgentType GetAgentTypeForIntent(string intent)
        {
            return intent switch
            {
                "refactor" => AgentType.Refactor,
                "generate_code" => AgentType.CSharp, // Default to C# for now
                "add_feature" => AgentType.CSharp,
                "analyze_code" => AgentType.Validator,
                "explain_code" => AgentType.Knowledge,
                "generate_tests" => AgentType.TestGenerator,
                "generate_docs" => AgentType.Knowledge,
                _ => AgentType.Unknown
            };
        }

        /// <summary>
        /// Extracts additional context from the request
        /// </summary>
        private Dictionary<string, object> ExtractContext(AgentRequest request, string intent)
        {
            var context = new Dictionary<string, object>
            {
                ["RequestId"] = request.Id,
                ["Intent"] = intent,
                ["HasFilePath"] = !string.IsNullOrWhiteSpace(request.FilePath),
                ["HasContent"] = !string.IsNullOrWhiteSpace(request.Content),
                ["PromptLength"] = request.Prompt?.Length ?? 0
            };

            // Add file-specific context
            if (!string.IsNullOrWhiteSpace(request.FilePath))
            {
                context["FileExtension"] = System.IO.Path.GetExtension(request.FilePath);
                context["FileName"] = System.IO.Path.GetFileName(request.FilePath);
            }

            return context;
        }

        /// <summary>
        /// Creates an unknown intent classification
        /// </summary>
        private IntentClassification CreateUnknownIntentClassification(string language, string prompt)
        {
            return new IntentClassification
            {
                Intent = "unknown",
                Confidence = 0.1,
                Language = language,
                SuggestedAgentType = AgentType.Unknown,
                Context = new Dictionary<string, object>
                {
                    ["Reason"] = "No matching intent patterns found",
                    ["PromptLength"] = prompt.Length
                }
            };
        }

        /// <summary>
        /// Internal class for intent patterns
        /// </summary>
        private class IntentPattern
        {
            public string Intent { get; set; } = string.Empty;
            public string[] Keywords { get; set; } = Array.Empty<string>();
            public string[] Patterns { get; set; } = Array.Empty<string>();
            public double Weight { get; set; } = 1.0;
        }
    }
}