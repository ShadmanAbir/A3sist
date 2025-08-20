using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using A3sist.Agents.Designer.Models;

namespace A3sist.Agents.Designer.Services
{
    /// <summary>
    /// Analyzes the architecture of a C# project.
    /// </summary>
    public class ArchitectureAnalyzer : IDisposable
    {
        private MSBuildWorkspace _workspace;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the ArchitectureAnalyzer class.
        /// </summary>
        public ArchitectureAnalyzer()
        {
            _workspace = MSBuildWorkspace.Create();
        }

        /// <summary>
        /// Initializes the analyzer asynchronously.
        /// </summary>
        public async Task InitializeAsync()
        {
            // Initialize analyzers
            await Task.CompletedTask;
        }

        /// <summary>
        /// Analyzes the architecture of the provided project context asynchronously.
        /// </summary>
        /// <param name="context">The project context in JSON format.</param>
        /// <returns>The analyzed architecture plan.</returns>
        /// <exception cref="ArgumentNullException">Thrown when context is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when analysis fails.</exception>
        public async Task<ArchitecturePlan> AnalyzeArchitectureAsync(string context)
        {
            if (string.IsNullOrWhiteSpace(context))
            {
                throw new ArgumentNullException(nameof(context), "Context cannot be null or empty.");
            }

            try
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
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to analyze architecture.", ex);
            }
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
    if (syntaxTree == null) return analysis;

    var root = await syntaxTree.GetRootAsync();
    var semanticModel = await document.GetSemanticModelAsync();
    if (semanticModel == null) return analysis;

    // Analyze classes
    var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
    foreach (var classDecl in classes)
    {
        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
        if (classSymbol == null) continue;

        var component = new Component
        {
            Name = classSymbol.Name,
            Description = "Class component",
            Responsibility = "Implements specific functionality",
            Technologies = new List<string> { document.Project.Language }
        };

        // Track dependencies: base class
        if (classSymbol.BaseType != null && classSymbol.BaseType.Name != "Object")
        {
            analysis.Dependencies.Add(new Dependency
            {
                SourceComponent = component.Name,
                TargetComponent = classSymbol.BaseType.Name,
                Type = "Inherits",
                Description = $"Class {component.Name} inherits {classSymbol.BaseType.Name}"
            });
        }

        // Track dependencies: implemented interfaces
        foreach (var iface in classSymbol.Interfaces)
        {
            analysis.Dependencies.Add(new Dependency
            {
                SourceComponent = component.Name,
                TargetComponent = iface.Name,
                Type = "Implements",
                Description = $"Class {component.Name} implements {iface.Name}"
            });
        }

                // Track dependencies: fields and properties
                var members = classSymbol.GetMembers()
                .Where(m => m is IFieldSymbol or IPropertySymbol);

                foreach (var member in members)
                {
                    ITypeSymbol? memberType = member switch
                    {
                        IFieldSymbol f => f.Type,
                        IPropertySymbol p => p.Type,
                        _ => null
                    };

                    if (memberType == null)
                        continue;

                    // Skip self-dependency
                    if (SymbolEqualityComparer.Default.Equals(memberType, classSymbol))
                        continue;

                    // Handle generics (track type arguments)
                    var targetTypes = new List<ITypeSymbol> { memberType };
                    if (memberType is INamedTypeSymbol namedType && namedType.IsGenericType)
                        targetTypes.AddRange(namedType.TypeArguments);

                    foreach (var t in targetTypes)
                    {
                        analysis.Dependencies.Add(new Dependency
                        {
                            SourceComponent = component.Name,
                            TargetComponent = t.ToDisplayString(), // includes namespace
                            Type = "Uses",
                            Description = $"Class {component.Name} uses {t.ToDisplayString()} in member {member.Name}"
                        });
                    }
                }


                analysis.Components.Add(component);
    }

    // Analyze interfaces
    var interfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();
    foreach (var interfaceDecl in interfaces)
    {
        var ifaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDecl);
        if (ifaceSymbol == null) continue;

        var component = new Component
        {
            Name = ifaceSymbol.Name,
            Description = "Interface component",
            Responsibility = "Defines contract for implementations",
            Technologies = new List<string> { document.Project.Language }
        };

        // Track interface inheritance
        foreach (var baseIface in ifaceSymbol.Interfaces)
        {
            analysis.Dependencies.Add(new Dependency
            {
                SourceComponent = component.Name,
                TargetComponent = baseIface.Name,
                Type = "Inherits",
                Description = $"Interface {component.Name} inherits {baseIface.Name}"
            });
        }

        analysis.Components.Add(component);
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

        /// <summary>
        /// Shuts down the analyzer asynchronously.
        /// </summary>
        public async Task ShutdownAsync()
        {
            if (_workspace != null)
            {
                await _workspace.DisposeAsync();
                _workspace = null;
            }
        }

        /// <summary>
        /// Disposes the analyzer and releases resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the analyzer and releases resources.
        /// </summary>
        /// <param name="disposing">Indicates whether the method is called from Dispose or the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here
                    _workspace?.Dispose();
                }

                // Dispose unmanaged resources here

                _disposed = true;
            }
        }
    }
}