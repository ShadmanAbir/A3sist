using A3sist.UI.Models.Chat;
using A3sist.UI.Services.Chat;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace A3sist.UI.Services.Chat
{
    /// <summary>
    /// Service for analyzing context and providing intelligent suggestions
    /// </summary>
    public interface IContextAnalyzerService
    {
        Task<ContextAnalysisResult> AnalyzeContextAsync(ChatContext context);
        Task<List<QuickAction>> GetQuickActionsAsync(ChatContext context);
        Task<List<string>> GetContextualSuggestionsAsync(ChatContext context, string userInput = "");
    }

    /// <summary>
    /// Implementation of context analyzer service
    /// </summary>
    public class ContextAnalyzerService : IContextAnalyzerService
    {
        private readonly IContextService _contextService;
        private readonly ILogger<ContextAnalyzerService> _logger;

        public ContextAnalyzerService(
            IContextService contextService,
            ILogger<ContextAnalyzerService> logger)
        {
            _contextService = contextService ?? throw new ArgumentNullException(nameof(contextService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Analyzes the current context to provide insights
        /// </summary>
        public async Task<ContextAnalysisResult> AnalyzeContextAsync(ChatContext context)
        {
            try
            {
                var result = new ContextAnalysisResult();

                // Analyze current file
                if (!string.IsNullOrEmpty(context.CurrentFile))
                {
                    await AnalyzeCurrentFileAsync(context, result);
                }

                // Analyze selected text
                if (!string.IsNullOrEmpty(context.SelectedText))
                {
                    await AnalyzeSelectedTextAsync(context, result);
                }

                // Analyze project structure
                if (!string.IsNullOrEmpty(context.ProjectPath))
                {
                    await AnalyzeProjectAsync(context, result);
                }

                // Analyze errors
                if (context.Errors.Any())
                {
                    await AnalyzeErrorsAsync(context, result);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing context");
                return new ContextAnalysisResult
                {
                    HasErrors = true,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Gets contextual quick actions based on current context
        /// </summary>
        public async Task<List<QuickAction>> GetQuickActionsAsync(ChatContext context)
        {
            var actions = new List<QuickAction>();

            try
            {
                // File-based actions
                if (!string.IsNullOrEmpty(context.CurrentFile))
                {
                    var fileExtension = Path.GetExtension(context.CurrentFile).ToLowerInvariant();
                    
                    actions.Add(new QuickAction
                    {
                        Title = "📋 Analyze This File",
                        Description = "Get a comprehensive analysis of the current file",
                        Command = $"Please analyze the current file ({Path.GetFileName(context.CurrentFile)}) and provide insights on code quality, potential improvements, and best practices.",
                        Category = "Analysis",
                        Priority = 1
                    });

                    if (fileExtension == ".cs")
                    {
                        actions.AddRange(GetCSharpQuickActions());
                    }
                    else if (fileExtension == ".js" || fileExtension == ".ts")
                    {
                        actions.AddRange(GetJavaScriptQuickActions());
                    }
                }

                // Selection-based actions
                if (!string.IsNullOrEmpty(context.SelectedText))
                {
                    actions.AddRange(GetSelectionQuickActions(context.SelectedText));
                }

                // Error-based actions
                if (context.Errors.Any())
                {
                    actions.AddRange(GetErrorQuickActions(context.Errors));
                }

                // Project-based actions
                if (!string.IsNullOrEmpty(context.ProjectPath))
                {
                    actions.AddRange(GetProjectQuickActions());
                }

                return actions.OrderBy(a => a.Priority).ThenBy(a => a.Title).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quick actions");
                return new List<QuickAction>();
            }
        }

        /// <summary>
        /// Gets contextual suggestions for user input
        /// </summary>
        public async Task<List<string>> GetContextualSuggestionsAsync(ChatContext context, string userInput = "")
        {
            var suggestions = new List<string>();

            try
            {
                var input = userInput.ToLowerInvariant();

                // Context-based suggestions
                if (!string.IsNullOrEmpty(context.CurrentFile))
                {
                    var fileName = Path.GetFileName(context.CurrentFile);
                    suggestions.Add($"Help me understand {fileName}");
                    suggestions.Add($"What can I improve in {fileName}?");
                    suggestions.Add($"Add unit tests for {fileName}");
                    suggestions.Add($"Document the code in {fileName}");
                }

                if (context.Errors.Any())
                {
                    suggestions.Add("Fix these compilation errors");
                    suggestions.Add("Explain these error messages");
                    suggestions.Add("How do I resolve these issues?");
                }

                if (!string.IsNullOrEmpty(context.SelectedText))
                {
                    suggestions.Add("Explain this selected code");
                    suggestions.Add("Refactor this code block");
                    suggestions.Add("Add error handling here");
                    suggestions.Add("Optimize this code");
                }

                // Input-based suggestions
                if (!string.IsNullOrEmpty(input))
                {
                    if (input.Contains("test"))
                    {
                        suggestions.Add("Generate unit tests for the current method");
                        suggestions.Add("Create integration tests");
                        suggestions.Add("Add test cases for edge conditions");
                    }

                    if (input.Contains("error") || input.Contains("bug"))
                    {
                        suggestions.Add("Debug this issue step by step");
                        suggestions.Add("What's causing this error?");
                        suggestions.Add("How to handle exceptions here?");
                    }

                    if (input.Contains("optimize") || input.Contains("performance"))
                    {
                        suggestions.Add("Analyze performance bottlenecks");
                        suggestions.Add("Suggest optimization strategies");
                        suggestions.Add("Review algorithm complexity");
                    }
                }

                // General suggestions
                suggestions.AddRange(new[]
                {
                    "Review my code for best practices",
                    "Help me write documentation",
                    "Suggest design patterns for this code",
                    "Generate code comments",
                    "Check for security vulnerabilities"
                });

                return suggestions.Distinct().Take(10).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contextual suggestions");
                return new List<string>();
            }
        }

        #region Private Analysis Methods

        private async Task AnalyzeCurrentFileAsync(ChatContext context, ContextAnalysisResult result)
        {
            var fileName = Path.GetFileName(context.CurrentFile);
            var extension = Path.GetExtension(context.CurrentFile).ToLowerInvariant();

            result.CurrentFileAnalysis = new FileAnalysis
            {
                FileName = fileName,
                FileType = GetFileType(extension),
                Language = GetLanguage(extension),
                EstimatedComplexity = "Medium" // TODO: Implement actual complexity analysis
            };
        }

        private async Task AnalyzeSelectedTextAsync(ChatContext context, ContextAnalysisResult result)
        {
            var selectedText = context.SelectedText;
            var lines = selectedText.Split('\n').Length;
            var hasCode = ContainsCode(selectedText);

            result.SelectionAnalysis = new SelectionAnalysis
            {
                LineCount = lines,
                CharacterCount = selectedText.Length,
                ContainsCode = hasCode,
                SelectionType = hasCode ? "Code Block" : "Text",
                EstimatedPurpose = AnalyzeCodePurpose(selectedText)
            };
        }

        private async Task AnalyzeProjectAsync(ChatContext context, ContextAnalysisResult result)
        {
            var projectName = Path.GetFileNameWithoutExtension(context.ProjectPath);
            
            result.ProjectAnalysis = new ProjectAnalysis
            {
                ProjectName = projectName,
                ProjectType = "C# Project", // TODO: Detect actual project type
                OpenFilesCount = context.OpenFiles.Count,
                HasTests = context.OpenFiles.Any(f => f.Contains("Test") || f.Contains("Spec"))
            };
        }

        private async Task AnalyzeErrorsAsync(ChatContext context, ContextAnalysisResult result)
        {
            var errors = context.Errors;
            var errorsByType = errors.GroupBy(e => e.Severity).ToDictionary(g => g.Key ?? "Unknown", g => g.Count());

            result.ErrorAnalysis = new ErrorAnalysis
            {
                TotalErrors = errors.Count,
                ErrorsBySeverity = errorsByType,
                MostCommonError = errors.GroupBy(e => e.Code).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key
            };
        }

        private List<QuickAction> GetCSharpQuickActions()
        {
            return new List<QuickAction>
            {
                new QuickAction
                {
                    Title = "🔍 C# Code Review",
                    Description = "Review this C# code for best practices and SOLID principles",
                    Command = "Please review this C# code for adherence to SOLID principles, design patterns, and best practices. Suggest improvements.",
                    Category = "C# Analysis",
                    Priority = 2
                },
                new QuickAction
                {
                    Title = "🧪 Generate xUnit Tests",
                    Description = "Create comprehensive unit tests using xUnit",
                    Command = "Generate comprehensive xUnit test cases for this C# code, including edge cases and mock dependencies.",
                    Category = "Testing",
                    Priority = 3
                }
            };
        }

        private List<QuickAction> GetJavaScriptQuickActions()
        {
            return new List<QuickAction>
            {
                new QuickAction
                {
                    Title = "⚡ JS Performance Review",
                    Description = "Analyze JavaScript code for performance issues",
                    Command = "Review this JavaScript code for performance issues, memory leaks, and optimization opportunities.",
                    Category = "Performance",
                    Priority = 2
                }
            };
        }

        private List<QuickAction> GetSelectionQuickActions(string selectedText)
        {
            var actions = new List<QuickAction>
            {
                new QuickAction
                {
                    Title = "💡 Explain Selection",
                    Description = "Get a detailed explanation of the selected code",
                    Command = $"Please explain this code in detail:\n\n{selectedText}",
                    Category = "Understanding",
                    Priority = 1
                },
                new QuickAction
                {
                    Title = "🔧 Refactor Selection",
                    Description = "Suggest improvements for the selected code",
                    Command = $"Please suggest refactoring improvements for this code:\n\n{selectedText}",
                    Category = "Refactoring",
                    Priority = 2
                }
            };

            if (ContainsCode(selectedText))
            {
                actions.Add(new QuickAction
                {
                    Title = "📝 Add Documentation",
                    Description = "Generate documentation for the selected code",
                    Command = $"Please generate comprehensive documentation comments for this code:\n\n{selectedText}",
                    Category = "Documentation",
                    Priority = 3
                });
            }

            return actions;
        }

        private List<QuickAction> GetErrorQuickActions(List<CompilerError> errors)
        {
            return new List<QuickAction>
            {
                new QuickAction
                {
                    Title = "🚨 Fix Errors",
                    Description = "Get help fixing compilation errors",
                    Command = $"Please help me fix these {errors.Count} compilation errors. Provide specific solutions for each error.",
                    Category = "Error Fixing",
                    Priority = 1
                },
                new QuickAction
                {
                    Title = "📚 Explain Errors",
                    Description = "Understand what these errors mean",
                    Command = "Please explain what these compilation errors mean and why they occur.",
                    Category = "Understanding",
                    Priority = 2
                }
            };
        }

        private List<QuickAction> GetProjectQuickActions()
        {
            return new List<QuickAction>
            {
                new QuickAction
                {
                    Title = "🏗️ Project Architecture Review",
                    Description = "Review the overall project structure and architecture",
                    Command = "Please review my project structure and suggest improvements to the architecture, organization, and design patterns.",
                    Category = "Architecture",
                    Priority = 4
                },
                new QuickAction
                {
                    Title = "📋 Generate Project Documentation",
                    Description = "Create comprehensive project documentation",
                    Command = "Please help me create comprehensive documentation for this project, including setup instructions, API docs, and usage examples.",
                    Category = "Documentation",
                    Priority = 5
                }
            };
        }

        private string GetFileType(string extension)
        {
            return extension switch
            {
                ".cs" => "C# Source File",
                ".js" => "JavaScript File",
                ".ts" => "TypeScript File",
                ".py" => "Python File",
                ".java" => "Java File",
                ".cpp" or ".cc" or ".cxx" => "C++ File",
                ".html" => "HTML File",
                ".css" => "CSS File",
                ".json" => "JSON File",
                ".xml" => "XML File",
                ".md" => "Markdown File",
                _ => "Code File"
            };
        }

        private string GetLanguage(string extension)
        {
            return extension switch
            {
                ".cs" => "C#",
                ".js" => "JavaScript",
                ".ts" => "TypeScript",
                ".py" => "Python",
                ".java" => "Java",
                ".cpp" or ".cc" or ".cxx" => "C++",
                ".html" => "HTML",
                ".css" => "CSS",
                _ => "Unknown"
            };
        }

        private bool ContainsCode(string text)
        {
            // Simple heuristic to detect if text contains code
            var codeIndicators = new[] { "{", "}", "(", ")", ";", "class ", "function ", "var ", "let ", "const " };
            return codeIndicators.Any(indicator => text.Contains(indicator));
        }

        private string AnalyzeCodePurpose(string code)
        {
            if (code.Contains("class ")) return "Class Definition";
            if (code.Contains("function ") || code.Contains("def ")) return "Function/Method";
            if (code.Contains("if ") || code.Contains("switch ")) return "Conditional Logic";
            if (code.Contains("for ") || code.Contains("while ")) return "Loop/Iteration";
            if (code.Contains("try ") || code.Contains("catch ")) return "Error Handling";
            return "Code Block";
        }

        #endregion
    }

    #region Data Models

    /// <summary>
    /// Result of context analysis
    /// </summary>
    public class ContextAnalysisResult
    {
        public FileAnalysis? CurrentFileAnalysis { get; set; }
        public SelectionAnalysis? SelectionAnalysis { get; set; }
        public ProjectAnalysis? ProjectAnalysis { get; set; }
        public ErrorAnalysis? ErrorAnalysis { get; set; }
        public bool HasErrors { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Analysis of the current file
    /// </summary>
    public class FileAnalysis
    {
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string EstimatedComplexity { get; set; } = string.Empty;
    }

    /// <summary>
    /// Analysis of selected text
    /// </summary>
    public class SelectionAnalysis
    {
        public int LineCount { get; set; }
        public int CharacterCount { get; set; }
        public bool ContainsCode { get; set; }
        public string SelectionType { get; set; } = string.Empty;
        public string EstimatedPurpose { get; set; } = string.Empty;
    }

    /// <summary>
    /// Analysis of the project
    /// </summary>
    public class ProjectAnalysis
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectType { get; set; } = string.Empty;
        public int OpenFilesCount { get; set; }
        public bool HasTests { get; set; }
    }

    /// <summary>
    /// Analysis of compilation errors
    /// </summary>
    public class ErrorAnalysis
    {
        public int TotalErrors { get; set; }
        public Dictionary<string, int> ErrorsBySeverity { get; set; } = new();
        public string? MostCommonError { get; set; }
    }

    /// <summary>
    /// Quick action suggestion
    /// </summary>
    public class QuickAction
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Priority { get; set; } = 5;
    }

    #endregion
}