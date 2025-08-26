using A3sist.Core.Agents.Base;
using A3sist.Orchastrator.Agents.Designer.Services;
using A3sist.Orchastrator.Agents.Designer.Models;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace A3sist.Core.Agents.Core.Designer
{
    /// <summary>
    /// Designer agent responsible for architecture analysis, design pattern recommendations, and code structure analysis
    /// </summary>
    public class DesignerAgent : BaseAgent
    {
        private readonly ArchitectureAnalyzer _architectureAnalyzer;
        private readonly ScaffoldingGenerator _scaffoldingGenerator;
        private readonly DesignPlanner _designPlanner;
        private readonly PatternRecommender _patternRecommender;

        public override string Name => "Designer";
        public override AgentType Type => AgentType.Designer;

        public DesignerAgent(
            ILogger<DesignerAgent> logger,
            IAgentConfiguration configuration) : base(logger, configuration)
        {
            _architectureAnalyzer = new ArchitectureAnalyzer();
            _scaffoldingGenerator = new ScaffoldingGenerator();
            _designPlanner = new DesignPlanner();
            _patternRecommender = new PatternRecommender();
        }

        protected override async Task<bool> CanHandleRequestAsync(AgentRequest request)
        {
            if (request == null)
                return false;

            // Designer can handle architecture analysis, design planning, pattern recommendations, and code structure analysis
            var supportedActions = new[]
            {
                "design", "architecture", "analyze", "pattern", "recommend", "structure",
                "plan", "scaffold", "blueprint", "organize", "refactor", "improve"
            };

            var prompt = request.Prompt?.ToLowerInvariant() ?? "";
            var hasCodeFile = !string.IsNullOrEmpty(request.FilePath) && 
                             (request.FilePath.EndsWith(".cs") || request.FilePath.EndsWith(".js") || 
                              request.FilePath.EndsWith(".py") || request.FilePath.EndsWith(".java"));

            return supportedActions.Any(action => prompt.Contains(action)) || hasCodeFile;
        }

        protected override async Task<AgentResult> HandleRequestAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var action = ExtractActionFromRequest(request);
                
                return action.ToLowerInvariant() switch
                {
                    "analyze" or "architecture" => await AnalyzeArchitectureAsync(request, cancellationToken),
                    "design" or "plan" => await CreateDesignPlanAsync(request, cancellationToken),
                    "pattern" or "recommend" => await RecommendPatternsAsync(request, cancellationToken),
                    "scaffold" or "structure" => await GenerateScaffoldingAsync(request, cancellationToken),
                    "improve" or "refactor" => await SuggestImprovementsAsync(request, cancellationToken),
                    _ => await PerformComprehensiveAnalysisAsync(request, cancellationToken)
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error handling designer request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Designer error: {ex.Message}", ex, Name);
            }
        }

        protected override System.Threading.Tasks.Task InitializeAgentAsync()
        {
            Logger.LogInformation("Initializing Designer agent");
            
            return System.Threading.Tasks.Task.WhenAll(
                _architectureAnalyzer.InitializeAsync(),
                _scaffoldingGenerator.InitializeAsync(),
                _designPlanner.InitializeAsync(),
                _patternRecommender.InitializeAsync()
            ).ContinueWith(_ => {
                Logger.LogInformation("Designer agent initialized successfully");
            });
        }

        protected override System.Threading.Tasks.Task ShutdownAgentAsync()
        {
            Logger.LogInformation("Shutting down Designer agent");
            
            return System.Threading.Tasks.Task.WhenAll(
                _architectureAnalyzer.ShutdownAsync(),
                _scaffoldingGenerator.ShutdownAsync(),
                _designPlanner.ShutdownAsync(),
                _patternRecommender.ShutdownAsync()
            ).ContinueWith(_ => {
                _architectureAnalyzer?.Dispose();
                Logger.LogInformation("Designer agent shutdown completed");
            });
        }

        private async Task<AgentResult> AnalyzeArchitectureAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Analyzing architecture for request {RequestId}", request.Id);

            try
            {
                var designRequest = CreateDesignRequestFromAgentRequest(request);
                var context = JsonSerializer.Serialize(designRequest);
                
                var architecturePlan = await _architectureAnalyzer.AnalyzeArchitectureAsync(context);
                
                var result = new
                {
                    ArchitectureStyle = architecturePlan.ArchitectureStyle,
                    Components = architecturePlan.Components.Select(c => new
                    {
                        c.Name,
                        c.Description,
                        c.Responsibility,
                        c.Technologies,
                        DependencyCount = c.Dependencies?.Count ?? 0
                    }),
                    Dependencies = architecturePlan.Dependencies.Select(d => new
                    {
                        d.SourceComponent,
                        d.TargetComponent,
                        d.Type,
                        d.Description
                    }),
                    RecommendedPatterns = architecturePlan.RecommendedPatterns?.Select(p => new
                    {
                        p.PatternName,
                        p.Description,
                        p.Benefit,
                        p.WhenToUse
                    }),
                    SuggestedImprovements = architecturePlan.SuggestedImprovements
                };

                return AgentResult.CreateSuccess(
                    $"Architecture analysis completed. Found {architecturePlan.Components.Count} components with {architecturePlan.Dependencies.Count} dependencies.",
                    JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
                    Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to analyze architecture for request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Architecture analysis failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> CreateDesignPlanAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Creating design plan for request {RequestId}", request.Id);

            try
            {
                var designRequest = CreateDesignRequestFromAgentRequest(request);
                var context = JsonSerializer.Serialize(designRequest);
                
                var architecturePlan = await _designPlanner.CreateDesignPlanAsync(context);
                
                var result = new
                {
                    ProjectName = architecturePlan.ProjectName,
                    ArchitectureStyle = architecturePlan.ArchitectureStyle,
                    Components = architecturePlan.Components.Select(c => new
                    {
                        c.Name,
                        c.Description,
                        c.Responsibility,
                        c.Technologies
                    }),
                    Dependencies = architecturePlan.Dependencies.Select(d => new
                    {
                        d.SourceComponent,
                        d.TargetComponent,
                        d.Type,
                        d.Description
                    }),
                    RecommendedPatterns = architecturePlan.RecommendedPatterns?.Select(p => new
                    {
                        p.PatternName,
                        p.Description,
                        p.Benefit,
                        p.WhenToUse,
                        p.ComponentsToApplyTo
                    }),
                    SuggestedImprovements = architecturePlan.SuggestedImprovements
                };

                return AgentResult.CreateSuccess(
                    $"Design plan created with {architecturePlan.Components.Count} components and {architecturePlan.RecommendedPatterns?.Count ?? 0} pattern recommendations.",
                    JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
                    Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create design plan for request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Design plan creation failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> RecommendPatternsAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Recommending patterns for request {RequestId}", request.Id);

            try
            {
                var designRequest = CreateDesignRequestFromAgentRequest(request);
                var context = JsonSerializer.Serialize(designRequest);
                
                var patterns = await _patternRecommender.RecommendPatternsAsync(context);
                
                var result = new
                {
                    TotalRecommendations = patterns.Count,
                    Patterns = patterns.Select(p => new
                    {
                        p.PatternName,
                        p.Description,
                        p.Benefits,
                        p.WhenToUse,
                        p.CommonComponents,
                        p.ExampleFrameworks,
                        p.RelevanceScore,
                        p.Justification
                    }).OrderByDescending(p => p.RelevanceScore)
                };

                return AgentResult.CreateSuccess(
                    $"Found {patterns.Count} pattern recommendations based on your requirements.",
                    JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
                    Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to recommend patterns for request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Pattern recommendation failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> GenerateScaffoldingAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Generating scaffolding for request {RequestId}", request.Id);

            try
            {
                var designRequest = CreateDesignRequestFromAgentRequest(request);
                var context = JsonSerializer.Serialize(designRequest);
                
                var scaffolding = await _scaffoldingGenerator.GenerateScaffoldingAsync(context);
                
                var result = new
                {
                    ProjectName = scaffolding.ProjectName,
                    Language = scaffolding.Language,
                    Framework = scaffolding.Framework,
                    Structure = scaffolding.Structure,
                    Dependencies = scaffolding.Dependencies,
                    Configuration = scaffolding.Configuration
                };

                return AgentResult.CreateSuccess(
                    $"Scaffolding generated for {scaffolding.ProjectName} with {scaffolding.Structure.Count} folders and {scaffolding.Dependencies.Count} dependencies.",
                    JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
                    Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to generate scaffolding for request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Scaffolding generation failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> SuggestImprovementsAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Suggesting improvements for request {RequestId}", request.Id);

            try
            {
                // First analyze the current architecture
                var designRequest = CreateDesignRequestFromAgentRequest(request);
                var context = JsonSerializer.Serialize(designRequest);
                
                var architecturePlan = await _architectureAnalyzer.AnalyzeArchitectureAsync(context);
                var patterns = await _patternRecommender.RecommendPatternsAsync(context);
                
                var improvements = new List<string>(architecturePlan.SuggestedImprovements ?? new List<string>());
                
                // Add pattern-based improvements
                foreach (var pattern in patterns.Take(3)) // Top 3 patterns
                {
                    improvements.Add($"Consider implementing {pattern.PatternName}: {pattern.Justification}");
                }
                
                // Add code structure improvements
                if (!string.IsNullOrEmpty(request.FilePath) && File.Exists(request.FilePath))
                {
                    improvements.AddRange(await AnalyzeCodeStructureAsync(request.FilePath));
                }
                
                var result = new
                {
                    TotalImprovements = improvements.Count,
                    Improvements = improvements.Select((imp, index) => new
                    {
                        Priority = index < 3 ? "High" : index < 6 ? "Medium" : "Low",
                        Description = imp
                    }),
                    ArchitectureInsights = new
                    {
                        CurrentStyle = architecturePlan.ArchitectureStyle,
                        ComponentCount = architecturePlan.Components.Count,
                        DependencyCount = architecturePlan.Dependencies.Count,
                        ComplexityScore = CalculateComplexityScore(architecturePlan)
                    }
                };

                return AgentResult.CreateSuccess(
                    $"Found {improvements.Count} improvement suggestions for your codebase.",
                    JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
                    Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to suggest improvements for request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Improvement suggestion failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> PerformComprehensiveAnalysisAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Performing comprehensive analysis for request {RequestId}", request.Id);

            try
            {
                var designRequest = CreateDesignRequestFromAgentRequest(request);
                var context = JsonSerializer.Serialize(designRequest);
                
                // Perform all analyses
                var architectureTask = _architectureAnalyzer.AnalyzeArchitectureAsync(context);
                var patternsTask = _patternRecommender.RecommendPatternsAsync(context);
                var scaffoldingTask = _scaffoldingGenerator.GenerateScaffoldingAsync(context);
                
                await Task.WhenAll(architectureTask, patternsTask, scaffoldingTask);
                
                var architecture = await architectureTask;
                var patterns = await patternsTask;
                var scaffolding = await scaffoldingTask;
                
                var result = new
                {
                    Summary = new
                    {
                        ProjectName = architecture.ProjectName,
                        ArchitectureStyle = architecture.ArchitectureStyle,
                        ComponentCount = architecture.Components.Count,
                        PatternRecommendations = patterns.Count,
                        ComplexityScore = CalculateComplexityScore(architecture)
                    },
                    Architecture = new
                    {
                        Components = architecture.Components.Take(10), // Limit for readability
                        Dependencies = architecture.Dependencies.Take(10),
                        SuggestedImprovements = architecture.SuggestedImprovements
                    },
                    TopPatterns = patterns.Take(5).Select(p => new
                    {
                        p.PatternName,
                        p.Description,
                        p.RelevanceScore,
                        p.Justification
                    }),
                    Scaffolding = new
                    {
                        scaffolding.Language,
                        scaffolding.Framework,
                        FolderCount = scaffolding.Structure.Count,
                        DependencyCount = scaffolding.Dependencies.Count
                    }
                };

                return AgentResult.CreateSuccess(
                    $"Comprehensive analysis completed. Found {architecture.Components.Count} components, {patterns.Count} pattern recommendations, and generated scaffolding structure.",
                    JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
                    Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to perform comprehensive analysis for request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Comprehensive analysis failed: {ex.Message}", ex, Name);
            }
        }

        private DesignRequest CreateDesignRequestFromAgentRequest(AgentRequest request)
        {
            var designRequest = new DesignRequest
            {
                ProjectName = ExtractProjectName(request),
                Language = ExtractLanguage(request),
                Framework = ExtractFramework(request),
                RequiredFeatures = ExtractRequiredFeatures(request),
                Constraints = ExtractConstraints(request),
                Preferences = ExtractPreferences(request)
            };

            // If we have a file path, use it for existing codebase analysis
            if (!string.IsNullOrEmpty(request.FilePath))
            {
                var directory = Path.GetDirectoryName(request.FilePath);
                var solutionFile = Directory.GetFiles(directory ?? ".", "*.sln", SearchOption.AllDirectories).FirstOrDefault();
                if (!string.IsNullOrEmpty(solutionFile))
                {
                    designRequest.ExistingCodebasePath = solutionFile;
                }
            }

            return designRequest;
        }

        private string ExtractProjectName(AgentRequest request)
        {
            // Try to extract project name from context or file path
            if (request.Context?.ContainsKey("projectName") == true)
                return request.Context["projectName"].ToString() ?? "UnknownProject";
            
            if (!string.IsNullOrEmpty(request.FilePath))
            {
                var directory = Path.GetDirectoryName(request.FilePath);
                return Path.GetFileName(directory) ?? "UnknownProject";
            }
            
            return "UnknownProject";
        }

        private string ExtractLanguage(AgentRequest request)
        {
            if (request.Context?.ContainsKey("language") == true)
                return request.Context["language"].ToString() ?? "C#";
            
            if (!string.IsNullOrEmpty(request.FilePath))
            {
                var extension = Path.GetExtension(request.FilePath).ToLowerInvariant();
                return extension switch
                {
                    ".cs" => "C#",
                    ".js" => "JavaScript",
                    ".ts" => "TypeScript",
                    ".py" => "Python",
                    ".java" => "Java",
                    _ => "C#"
                };
            }
            
            return "C#";
        }

        private string ExtractFramework(AgentRequest request)
        {
            if (request.Context?.ContainsKey("framework") == true)
                return request.Context["framework"].ToString() ?? "";
            
            var language = ExtractLanguage(request);
            return language switch
            {
                "C#" => "ASP.NET Core",
                "JavaScript" => "React",
                "TypeScript" => "Angular",
                "Python" => "Django",
                "Java" => "Spring Boot",
                _ => ""
            };
        }

        private List<string> ExtractRequiredFeatures(AgentRequest request)
        {
            var features = new List<string>();
            var prompt = request.Prompt?.ToLowerInvariant() ?? "";
            
            if (prompt.Contains("ui") || prompt.Contains("interface"))
                features.Add("UI");
            if (prompt.Contains("database") || prompt.Contains("data"))
                features.Add("Database");
            if (prompt.Contains("test") || prompt.Contains("testing"))
                features.Add("Testing");
            if (prompt.Contains("api") || prompt.Contains("service"))
                features.Add("API");
            if (prompt.Contains("real-time") || prompt.Contains("realtime"))
                features.Add("Real-time Processing");
            if (prompt.Contains("complex") && prompt.Contains("query"))
                features.Add("Complex Queries");
            if (prompt.Contains("audit") || prompt.Contains("trail"))
                features.Add("Audit Trail");
            
            return features;
        }

        private List<string> ExtractConstraints(AgentRequest request)
        {
            var constraints = new List<string>();
            var prompt = request.Prompt?.ToLowerInvariant() ?? "";
            
            if (prompt.Contains("maintain") || prompt.Contains("legacy"))
                constraints.Add("Long-term Maintainability");
            if (prompt.Contains("performance") || prompt.Contains("fast"))
                constraints.Add("High Performance");
            if (prompt.Contains("scale") || prompt.Contains("scalable"))
                constraints.Add("Scalability");
            if (prompt.Contains("secure") || prompt.Contains("security"))
                constraints.Add("Security");
            
            return constraints;
        }

        private DesignPreferences ExtractPreferences(AgentRequest request)
        {
            var prompt = request.Prompt?.ToLowerInvariant() ?? "";
            
            return new DesignPreferences
            {
                PreferSeparationOfConcerns = prompt.Contains("separation") || prompt.Contains("clean"),
                PreferTestability = prompt.Contains("test") || prompt.Contains("testable"),
                PreferModularity = prompt.Contains("modular") || prompt.Contains("module"),
                ArchitectureStyle = ExtractArchitectureStyle(prompt),
                PreferredPatterns = ExtractPreferredPatterns(prompt)
            };
        }

        private string ExtractArchitectureStyle(string prompt)
        {
            if (prompt.Contains("mvc"))
                return "MVC";
            if (prompt.Contains("mvvm"))
                return "MVVM";
            if (prompt.Contains("clean"))
                return "CleanArchitecture";
            if (prompt.Contains("microservice"))
                return "Microservices";
            if (prompt.Contains("layered"))
                return "Layered Architecture";
            
            return "";
        }

        private List<string> ExtractPreferredPatterns(string prompt)
        {
            var patterns = new List<string>();
            
            if (prompt.Contains("repository"))
                patterns.Add("Repository");
            if (prompt.Contains("service"))
                patterns.Add("Service Layer");
            if (prompt.Contains("cqrs"))
                patterns.Add("CQRS");
            if (prompt.Contains("event"))
                patterns.Add("Event Sourcing");
            
            return patterns;
        }

        private async Task<List<string>> AnalyzeCodeStructureAsync(string filePath)
        {
            var improvements = new List<string>();
            
            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                
                // Simple code analysis
                if (content.Length > 10000)
                    improvements.Add("Consider breaking down large files into smaller, more focused modules");
                
                if (content.Split('\n').Count(line => line.Trim().StartsWith("public class")) > 3)
                    improvements.Add("Multiple classes in one file - consider separating into individual files");
                
                if (content.Contains("// TODO") || content.Contains("// FIXME"))
                    improvements.Add("Address TODO and FIXME comments in the codebase");
                
                if (!content.Contains("namespace"))
                    improvements.Add("Consider organizing code into proper namespaces");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to analyze code structure for file {FilePath}", filePath);
            }
            
            return improvements;
        }

        private double CalculateComplexityScore(ArchitecturePlan plan)
        {
            // Simple complexity calculation based on components and dependencies
            var componentCount = plan.Components.Count;
            var dependencyCount = plan.Dependencies.Count;
            
            if (componentCount == 0) return 0;
            
            var dependencyRatio = (double)dependencyCount / componentCount;
            
            // Score from 0-10, where higher means more complex
            return Math.Min(10, dependencyRatio * 2 + (componentCount / 10.0));
        }

        private string ExtractActionFromRequest(AgentRequest request)
        {
            var prompt = request.Prompt?.ToLowerInvariant() ?? "";
            
            if (prompt.Contains("analyze") || prompt.Contains("architecture"))
                return "analyze";
            if (prompt.Contains("design") || prompt.Contains("plan"))
                return "design";
            if (prompt.Contains("pattern") || prompt.Contains("recommend"))
                return "pattern";
            if (prompt.Contains("scaffold") || prompt.Contains("structure"))
                return "scaffold";
            if (prompt.Contains("improve") || prompt.Contains("refactor"))
                return "improve";
            
            return "comprehensive"; // Default to comprehensive analysis
        }
    }
}