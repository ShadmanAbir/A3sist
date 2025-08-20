using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using A3sist.Agents.Designer.Models;

namespace A3sist.Agents.Designer.Services
{
    public class PatternRecommender
    {
        private List<ArchitecturalPattern> _patterns;

        public async Task InitializeAsync()
        {
            // Load architectural patterns
            _patterns = await LoadPatternsAsync();
        }

        private async Task<List<ArchitecturalPattern>> LoadPatternsAsync()
        {
            // In a real implementation, you would load patterns from a database or file system
            // For this example, we'll use hardcoded patterns
            return new List<ArchitecturalPattern>
            {
                new ArchitecturalPattern
                {
                    Name = "Layered Architecture",
                    Description = "Organizes code into horizontal layers (UI, Business, Data)",
                    Benefits = new List<string>
                    {
                        "Clear separation of concerns",
                        "Easier maintenance and testing",
                        "Scalability"
                    },
                    WhenToUse = "Medium to large applications",
                    CommonComponents = new List<string> { "Presentation", "Application", "Domain", "Infrastructure" },
                    ExampleFrameworks = new List<string> { "ASP.NET MVC", "Spring MVC" }
                },
                new ArchitecturalPattern
                {
                    Name = "Microservices",
                    Description = "Decomposes application into small, independent services",
                    Benefits = new List<string>
                    {
                        "Scalability",
                        "Independent deployment",
                        "Technology flexibility"
                    },
                    WhenToUse = "Large, complex applications with multiple teams",
                    CommonComponents = new List<string> { "API Gateway", "Service Discovery", "Configuration Server" },
                    ExampleFrameworks = new List<string> { "Spring Boot", "ASP.NET Core" }
                },
                new ArchitecturalPattern
                {
                    Name = "Event-Driven Architecture",
                    Description = "Components communicate through events",
                    Benefits = new List<string>
                    {
                        "Loose coupling",
                        "Scalability",
                        "Resilience"
                    },
                    WhenToUse = "Applications requiring real-time processing",
                    CommonComponents = new List<string> { "Event Bus", "Event Store", "Event Handlers" },
                    ExampleFrameworks = new List<string> { "RabbitMQ", "Azure Event Hubs" }
                },
                new ArchitecturalPattern
                {
                    Name = "Clean Architecture",
                    Description = "Independent of frameworks, testable, and UI-independent",
                    Benefits = new List<string>
                    {
                        "Testability",
                        "Maintainability",
                        "Flexibility"
                    },
                    WhenToUse = "Complex applications requiring long-term maintainability",
                    CommonComponents = new List<string> { "Entities", "Use Cases", "Interfaces", "Frameworks" },
                    ExampleFrameworks = new List<string> { "ASP.NET Core", "Java Spring" }
                },
                new ArchitecturalPattern
                {
                    Name = "Hexagonal Architecture",
                    Description = "Separates application core from external concerns",
                    Benefits = new List<string>
                    {
                        "Testability",
                        "Flexibility",
                        "Maintainability"
                    },
                    WhenToUse = "Applications requiring flexibility and testability",
                    CommonComponents = new List<string> { "Core", "Adapters", "Ports" },
                    ExampleFrameworks = new List<string> { "ASP.NET Core", "Java Spring" }
                }
            };
        }

        public async Task<List<PatternRecommendation>> RecommendPatternsAsync(string context)
        {
            var designRequest = JsonSerializer.Deserialize<DesignRequest>(context);
            var recommendations = new List<PatternRecommendation>();

            // Recommend patterns based on project size
            if (designRequest.RequiredFeatures.Contains("Large Application"))
            {
                recommendations.AddRange(RecommendPatternsForLargeApplications(designRequest));
            }
            else
            {
                recommendations.AddRange(RecommendPatternsForSmallMediumApplications(designRequest));
            }

            // Recommend patterns based on specific requirements
            recommendations.AddRange(RecommendPatternsBasedOnRequirements(designRequest));

            // Recommend patterns based on preferences
            recommendations.AddRange(RecommendPatternsBasedOnPreferences(designRequest));

            // Remove duplicates and sort by relevance
            return recommendations
                .GroupBy(r => r.PatternName)
                .Select(g => g.First())
                .OrderByDescending(r => r.RelevanceScore)
                .ToList();
        }

        private List<PatternRecommendation> RecommendPatternsForLargeApplications(DesignRequest designRequest)
        {
            var recommendations = new List<PatternRecommendation>();

            // Recommend microservices for large applications
            var microservicesPattern = _patterns.FirstOrDefault(p => p.Name == "Microservices");
            if (microservicesPattern != null)
            {
                recommendations.Add(new PatternRecommendation
                {
                    PatternName = microservicesPattern.Name,
                    Description = microservicesPattern.Description,
                    Benefits = microservicesPattern.Benefits,
                    WhenToUse = microservicesPattern.WhenToUse,
                    CommonComponents = microservicesPattern.CommonComponents,
                    ExampleFrameworks = microservicesPattern.ExampleFrameworks,
                    RelevanceScore = 0.9f,
                    Justification = "Recommended for large applications due to scalability and independent deployment"
                });
            }

            // Recommend event-driven architecture for applications requiring real-time processing
            if (designRequest.RequiredFeatures.Contains("Real-time Processing"))
            {
                var eventDrivenPattern = _patterns.FirstOrDefault(p => p.Name == "Event-Driven Architecture");
                if (eventDrivenPattern != null)
                {
                    recommendations.Add(new PatternRecommendation
                    {
                        PatternName = eventDrivenPattern.Name,
                        Description = eventDrivenPattern.Description,
                        Benefits = eventDrivenPattern.Benefits,
                        WhenToUse = eventDrivenPattern.WhenToUse,
                        CommonComponents = eventDrivenPattern.CommonComponents,
                        ExampleFrameworks = eventDrivenPattern.ExampleFrameworks,
                        RelevanceScore = 0.85f,
                        Justification = "Recommended for applications requiring real-time processing"
                    });
                }
            }

            return recommendations;
        }

        private List<PatternRecommendation> RecommendPatternsForSmallMediumApplications(DesignRequest designRequest)
        {
            var recommendations = new List<PatternRecommendation>();

            // Recommend layered architecture for small to medium applications
            var layeredPattern = _patterns.FirstOrDefault(p => p.Name == "Layered Architecture");
            if (layeredPattern != null)
            {
                recommendations.Add(new PatternRecommendation
                {
                    PatternName = layeredPattern.Name,
                    Description = layeredPattern.Description,
                    Benefits = layeredPattern.Benefits,
                    WhenToUse = layeredPattern.WhenToUse,
                    CommonComponents = layeredPattern.CommonComponents,
                    ExampleFrameworks = layeredPattern.ExampleFrameworks,
                    RelevanceScore = 0.8f,
                    Justification = "Recommended for small to medium applications due to simplicity and maintainability"
                });
            }

            // Recommend clean architecture for applications requiring long-term maintainability
            if (designRequest.Constraints.Contains("Long-term Maintainability"))
            {
                var cleanPattern = _patterns.FirstOrDefault(p => p.Name == "Clean Architecture");
                if (cleanPattern != null)
                {
                    recommendations.Add(new PatternRecommendation
                    {
                        PatternName = cleanPattern.Name,
                        Description = cleanPattern.Description,
                        Benefits = cleanPattern.Benefits,
                        WhenToUse = cleanPattern.WhenToUse,
                        CommonComponents = cleanPattern.CommonComponents,
                        ExampleFrameworks = cleanPattern.ExampleFrameworks,
                        RelevanceScore = 0.75f,
                        Justification = "Recommended for applications requiring long-term maintainability"
                    });
                }
            }

            // Recommend hexagonal architecture for applications requiring flexibility and testability
            if (designRequest.Preferences.PreferTestability || designRequest.Preferences.PreferModularity)
            {
                var hexagonalPattern = _patterns.FirstOrDefault(p => p.Name == "Hexagonal Architecture");
                if (hexagonalPattern != null)
                {
                    recommendations.Add(new PatternRecommendation
                    {
                        PatternName = hexagonalPattern.Name,
                        Description = hexagonalPattern.Description,
                        Benefits = hexagonalPattern.Benefits,
                        WhenToUse = hexagonalPattern.WhenToUse,
                        CommonComponents = hexagonalPattern.CommonComponents,
                        ExampleFrameworks = hexagonalPattern.ExampleFrameworks,
                        RelevanceScore = 0.7f,
                        Justification = "Recommended for applications requiring flexibility and testability"
                    });
                }
            }

            return recommendations;
        }

        private List<PatternRecommendation> RecommendPatternsBasedOnRequirements(DesignRequest designRequest)
        {
            var recommendations = new List<PatternRecommendation>();

            // Recommend patterns based on specific requirements
            if (designRequest.RequiredFeatures.Contains("High Availability"))
            {
                var microservicesPattern = _patterns.FirstOrDefault(p => p.Name == "Microservices");
                if (microservicesPattern != null)
                {
                    recommendations.Add(new PatternRecommendation
                    {
                        PatternName = microservicesPattern.Name,
                        Description = microservicesPattern.Description,
                        Benefits = microservicesPattern.Benefits,
                        WhenToUse = microservicesPattern.WhenToUse,
                        CommonComponents = microservicesPattern.CommonComponents,
                        ExampleFrameworks = microservicesPattern.ExampleFrameworks,
                        RelevanceScore = 0.85f,
                        Justification = "Recommended for applications requiring high availability"
                    });
                }
            }

            if (designRequest.RequiredFeatures.Contains("Complex Domain Logic"))
            {
                var cleanPattern = _patterns.FirstOrDefault(p => p.Name == "Clean Architecture");
                if (cleanPattern != null)
                {
                    recommendations.Add(new PatternRecommendation
                    {
                        PatternName = cleanPattern.Name,
                        Description = cleanPattern.Description,
                        Benefits = cleanPattern.Benefits,
                        WhenToUse = cleanPattern.WhenToUse,
                        CommonComponents = cleanPattern.CommonComponents,
                        ExampleFrameworks = cleanPattern.ExampleFrameworks,
                        RelevanceScore = 0.8f,
                        Justification = "Recommended for applications with complex domain logic"
                    });
                }
            }

            if (designRequest.RequiredFeatures.Contains("Real-time Processing"))
            {
                var eventDrivenPattern = _patterns.FirstOrDefault(p => p.Name == "Event-Driven Architecture");
                if (eventDrivenPattern != null)
                {
                    recommendations.Add(new PatternRecommendation
                    {
                        PatternName = eventDrivenPattern.Name,
                        Description = eventDrivenPattern.Description,
                        Benefits = eventDrivenPattern.Benefits,
                        WhenToUse = eventDrivenPattern.WhenToUse,
                        CommonComponents = eventDrivenPattern.CommonComponents,
                        ExampleFrameworks = eventDrivenPattern.ExampleFrameworks,
                        RelevanceScore = 0.85f,
                        Justification = "Recommended for applications requiring real-time processing"
                    });
                }
            }

            return recommendations;
        }

        private List<PatternRecommendation> RecommendPatternsBasedOnPreferences(DesignRequest designRequest)
        {
            var recommendations = new List<PatternRecommendation>();

            // Recommend patterns based on preferences
            if (designRequest.Preferences.PreferSeparationOfConcerns)
            {
                var layeredPattern = _patterns.FirstOrDefault(p => p.Name == "Layered Architecture");
                if (layeredPattern != null)
                {
                    recommendations.Add(new PatternRecommendation
                    {
                        PatternName = layeredPattern.Name,
                        Description = layeredPattern.Description,
                        Benefits = layeredPattern.Benefits,
                        WhenToUse = layeredPattern.WhenToUse,
                        CommonComponents = layeredPattern.CommonComponents,
                        ExampleFrameworks = layeredPattern.ExampleFrameworks,
                        RelevanceScore = 0.75f,
                        Justification = "Recommended due to preference for separation of concerns"
                    });
                }
            }

            if (designRequest.Preferences.PreferTestability)
            {
                var cleanPattern = _patterns.FirstOrDefault(p => p.Name == "Clean Architecture");
                if (cleanPattern != null)
                {
                    recommendations.Add(new PatternRecommendation
                    {
                        PatternName = cleanPattern.Name,
                        Description = cleanPattern.Description,
                        Benefits = cleanPattern.Benefits,
                        WhenToUse = cleanPattern.WhenToUse,
                        CommonComponents = cleanPattern.CommonComponents,
                        ExampleFrameworks = cleanPattern.ExampleFrameworks,
                        RelevanceScore = 0.8f,
                        Justification = "Recommended due to preference for testability"
                    });
                }
            }

            if (designRequest.Preferences.PreferModularity)
            {
                var microservicesPattern = _patterns.FirstOrDefault(p => p.Name == "Microservices");
                if (microservicesPattern != null)
                {
                    recommendations.Add(new PatternRecommendation
                    {
                        PatternName = microservicesPattern.Name,
                        Description = microservicesPattern.Description,
                        Benefits = microservicesPattern.Benefits,
                        WhenToUse = microservicesPattern.WhenToUse,
                        CommonComponents = microservicesPattern.CommonComponents,
                        ExampleFrameworks = microservicesPattern.ExampleFrameworks,
                        RelevanceScore = 0.7f,
                        Justification = "Recommended due to preference for modularity"
                    });
                }
            }

            return recommendations;
        }

        public async Task ShutdownAsync()
        {
            // Clean up resources
            _patterns.Clear();
        }
    }

    public class ArchitecturalPattern
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Benefits { get; set; } = new List<string>();
        public string WhenToUse { get; set; }
        public List<string> CommonComponents { get; set; } = new List<string>();
        public List<string> ExampleFrameworks { get; set; } = new List<string>();
    }

    public class PatternRecommendation
    {
        public string PatternName { get; set; }
        public string Description { get; set; }
        public List<string> Benefits { get; set; } = new List<string>();
        public string WhenToUse { get; set; }
        public List<string> CommonComponents { get; set; } = new List<string>();
        public List<string> ExampleFrameworks { get; set; } = new List<string>();
        public float RelevanceScore { get; set; }
        public string Justification { get; set; }
    }
}