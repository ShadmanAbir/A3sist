using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using A3sist.Agents.Designer.Models;

namespace A3sist.Agents.Designer.Services
{
    public class ArchitectureAnalyzer
    {
        private MSBuildWorkspace _workspace;

        public async Task InitializeAsync()
        {
            _workspace = MSBuildWorkspace.Create();
        }

        public async Task<ArchitecturePlan> AnalyzeArchitectureAsync(string context)
        {
            var designRequest = JsonSerializer.Deserialize<DesignRequest>(context);
            var architecturePlan = new ArchitecturePlan
            {
                ProjectName = designRequest.ProjectName,
                ArchitectureStyle = "Unknown"
            };

            if (!string.IsNullOrEmpty(designRequest.ExistingCodebasePath))
            {
                // Analyze existing codebase
                var solution = await _workspace.OpenSolutionAsync(designRequest.ExistingCodebasePath);
                architecturePlan = await AnalyzeSolutionAsync(solution, designRequest);
            }
            else
            {
                // Create new architecture plan based on requirements
                architecturePlan = CreateInitialArchitecturePlan(designRequest);
            }

            return architecturePlan;
        }

        private async Task<ArchitecturePlan> AnalyzeSolutionAsync(Solution solution, DesignRequest designRequest)
        {
            var architecturePlan = new ArchitecturePlan
            {
                ProjectName = designRequest.ProjectName,
                ArchitectureStyle = DetermineArchitectureStyle(solution)
            };

            // Analyze projects
            foreach (var project in solution.Projects)
            {
                var projectAnalysis = await AnalyzeProjectAsync(project);
                architecturePlan.Components.AddRange(projectAnalysis.Components);
                architecturePlan.Dependencies.AddRange(projectAnalysis.Dependencies);
            }

            // Identify common patterns
            architecturePlan.RecommendedPatterns = IdentifyPatterns(architecturePlan);

            // Suggest improvements
            architecturePlan.SuggestedImprovements = SuggestImprovements(architecturePlan);

            return architecturePlan;
        }

        private async Task<ProjectAnalysis> AnalyzeProjectAsync(Project project)
        {
            var analysis = new ProjectAnalysis
            {
                ProjectName = project.Name
            };

            // Analyze project structure
            analysis.Components.Add(new Component
            {
                Name = project.Name,
                Description = "Main project component",
                Responsibility = "Contains the main application logic",
                Technologies = new List<string> { project.Language }
            });

            // Analyze project dependencies
            foreach (var dependency in project.ProjectReferences)
            {
                analysis.Dependencies.Add(new Dependency
                {
                    SourceComponent = project.Name,
                    TargetComponent = dependency.ProjectName,
                    Type = "DependsOn",
                    Description = "Project reference"
                });
            }

            // Analyze project files
            foreach (var document in project.Documents)
            {
                var fileAnalysis = await AnalyzeDocumentAsync(document);
                analysis.Components.AddRange(fileAnalysis.Components);
                analysis.Dependencies.AddRange(fileAnalysis.Dependencies);
            }

            return analysis;
        }

        private async Task<DocumentAnalysis> AnalyzeDocumentAsync(Document document)
        {
            var analysis = new DocumentAnalysis
            {
                DocumentName = document.Name
            };

            var syntaxTree = await document.GetSyntaxTreeAsync();
            var root = await syntaxTree.GetRootAsync();

            // Analyze classes
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var classDecl in classes)
            {
                analysis.Components.Add(new Component
                {
                    Name = classDecl.Identifier.Text,
                    Description = "Class component",
                    Responsibility = "Implements specific functionality",
                    Technologies = new List<string> { document.Project.Language }
                });
            }

            // Analyze interfaces
            var interfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();
            foreach (var interfaceDecl in interfaces)
            {
                analysis.Components.Add(new Component
                {
                    Name = interfaceDecl.Identifier.Text,
                    Description = "Interface component",
                    Responsibility = "Defines contract for implementations",
                    Technologies = new List<string> { document.Project.Language }
                });
            }

            return analysis;
        }

        private string DetermineArchitectureStyle(Solution solution)
        {
            // Simple pattern matching - in a real implementation, you would use more sophisticated analysis
            if (solution.Projects.Any(p => p.Name.Contains("Controller") || p.Name.Contains("View")))
            {
                return "MVC";
            }
            else if (solution.Projects.Any(p => p.Name.Contains("ViewModel") || p.Name.Contains("View")))
            {
                return "MVVM";
            }
            else if (solution.Projects.Any(p => p.Name.Contains("Domain") || p.Name.Contains("Application")))
            {
                return "Clean Architecture";
            }

            return "Unknown";
        }

        private List<PatternRecommendation> IdentifyPatterns(ArchitecturePlan plan)
        {
            var patterns = new List<PatternRecommendation>();

            // Check for common patterns
            if (plan.Components.Any(c => c.Name.Contains("Repository")))
            {
                patterns.Add(new PatternRecommendation
                {
                    PatternName = "Repository Pattern",
                    Description = "Separates data access logic from business logic",
                    Benefit = "Improves testability and maintainability",
                    WhenToUse = "When working with databases or external data sources",
                    ComponentsToApplyTo = plan.Components.Where(c => c.Name.Contains("Repository")).Select(c => c.Name).ToList()
                });
            }

            if (plan.Components.Any(c => c.Name.Contains("Service")))
            {
                patterns.Add(new PatternRecommendation
                {
                    PatternName = "Service Layer Pattern",
                    Description = "Separates business logic from presentation layer",
                    Benefit = "Improves separation of concerns and reusability",
                    WhenToUse = "In medium to large applications",
                    ComponentsToApplyTo = plan.Components.Where(c => c.Name.Contains("Service")).Select(c => c.Name).ToList()
                });
            }

            return patterns;
        }

        private List<string> SuggestImprovements(ArchitecturePlan plan)
        {
            var improvements = new List<string>();

            // Check for common issues
            if (plan.Dependencies.Count(d => d.Type == "DependsOn") > 20)
            {
                improvements.Add("Consider reducing project dependencies to improve maintainability");
            }

            if (plan.Components.Any(c => c.Dependencies.Count > 5))
            {
                improvements.Add("Some components have too many dependencies - consider refactoring");
            }

            if (plan.Components.Any(c => c.Name.Contains("Controller") && c.Responsibility.Contains("business logic")))
            {
                improvements.Add("Controllers should not contain business logic - move to service layer");
            }

            return improvements;
        }

        private ArchitecturePlan CreateInitialArchitecturePlan(DesignRequest designRequest)
        {
            var plan = new ArchitecturePlan
            {
                ProjectName = designRequest.ProjectName,
                ArchitectureStyle = designRequest.Preferences.ArchitectureStyle ?? "Unknown"
            };

            // Create basic components based on requirements
            plan.Components.Add(new Component
            {
                Name = "Core",
                Description = "Main application logic",
                Responsibility = "Contains the core business logic of the application"
            });

            if (designRequest.RequiredFeatures.Contains("UI"))
            {
                plan.Components.Add(new Component
                {
                    Name = "UI",
                    Description = "User interface layer",
                    Responsibility = "Handles user interaction and presentation"
                });
            }

            if (designRequest.RequiredFeatures.Contains("Database"))
            {
                plan.Components.Add(new Component
                {
                    Name = "Data",
                    Description = "Data access layer",
                    Responsibility = "Handles data storage and retrieval"
                });
            }

            // Add dependencies
            if (plan.Components.Any(c => c.Name == "UI") && plan.Components.Any(c => c.Name == "Core"))
            {
                plan.Dependencies.Add(new Dependency
                {
                    SourceComponent = "UI",
                    TargetComponent = "Core",
                    Type = "DependsOn",
                    Description = "UI depends on core functionality"
                });
            }

            if (plan.Components.Any(c => c.Name == "Core") && plan.Components.Any(c => c.Name == "Data"))
            {
                plan.Dependencies.Add(new Dependency
                {
                    SourceComponent = "Core",
                    TargetComponent = "Data",
                    Type = "DependsOn",
                    Description = "Core depends on data access"
                });
            }

            return plan;
        }

        public async Task ShutdownAsync()
        {
            if (_workspace != null)
            {
                await _workspace.DisposeAsync();
                _workspace = null;
            }
        }
    }

    internal class ProjectAnalysis
    {
        public string ProjectName { get; set; }
        public List<Component> Components { get; set; } = new List<Component>();
        public List<Dependency> Dependencies { get; set; } = new List<Dependency>();
    }

    internal class DocumentAnalysis
    {
        public string DocumentName { get; set; }
        public List<Component> Components { get; set; } = new List<Component>();
        public List<Dependency> Dependencies { get; set; } = new List<Dependency>();
    }
}