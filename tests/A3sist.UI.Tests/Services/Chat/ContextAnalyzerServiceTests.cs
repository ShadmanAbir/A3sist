using A3sist.UI.Models.Chat;
using A3sist.UI.Services.Chat;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.UI.Tests.Services.Chat
{
    /// <summary>
    /// Unit tests for ContextAnalyzerService
    /// </summary>
    public class ContextAnalyzerServiceTests
    {
        private readonly Mock<IContextService> _mockContextService;
        private readonly Mock<ILogger<ContextAnalyzerService>> _mockLogger;
        private readonly ContextAnalyzerService _analyzerService;

        public ContextAnalyzerServiceTests()
        {
            _mockContextService = new Mock<IContextService>();
            _mockLogger = new Mock<ILogger<ContextAnalyzerService>>();
            
            _analyzerService = new ContextAnalyzerService(_mockContextService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task AnalyzeContextAsync_WithCSharpFile_ShouldReturnFileAnalysis()
        {
            // Arrange
            var context = new ChatContext
            {
                CurrentFile = @"C:\Projects\TestApp\Controllers\HomeController.cs",
                SelectedText = "public class HomeController : Controller { }",
                ProjectPath = @"C:\Projects\TestApp\TestApp.csproj"
            };

            // Act
            var result = await _analyzerService.AnalyzeContextAsync(context);

            // Assert
            result.Should().NotBeNull();
            result.HasErrors.Should().BeFalse();
            
            result.CurrentFileAnalysis.Should().NotBeNull();
            result.CurrentFileAnalysis!.FileName.Should().Be("HomeController.cs");
            result.CurrentFileAnalysis.Language.Should().Be("C#");
            result.CurrentFileAnalysis.FileType.Should().Be("C# Source File");

            result.SelectionAnalysis.Should().NotBeNull();
            result.SelectionAnalysis!.ContainsCode.Should().BeTrue();
            result.SelectionAnalysis.SelectionType.Should().Be("Code Block");

            result.ProjectAnalysis.Should().NotBeNull();
            result.ProjectAnalysis!.ProjectName.Should().Be("TestApp");
        }

        [Fact]
        public async Task AnalyzeContextAsync_WithErrors_ShouldReturnErrorAnalysis()
        {
            // Arrange
            var context = new ChatContext
            {
                CurrentFile = @"C:\Projects\TestApp\Program.cs",
                Errors = new List<CompilerError>
                {
                    new() { Severity = "Error", Message = "CS0103: The name 'undefined' does not exist in the current context", Code = "CS0103" },
                    new() { Severity = "Warning", Message = "CS0219: Variable is assigned but never used", Code = "CS0219" }
                }
            };

            // Act
            var result = await _analyzerService.AnalyzeContextAsync(context);

            // Assert
            result.Should().NotBeNull();
            result.ErrorAnalysis.Should().NotBeNull();
            result.ErrorAnalysis!.TotalErrors.Should().Be(2);
            result.ErrorAnalysis.ErrorsBySeverity.Should().ContainKey("Error");
            result.ErrorAnalysis.ErrorsBySeverity.Should().ContainKey("Warning");
            result.ErrorAnalysis.MostCommonError.Should().Be("CS0103");
        }

        [Fact]
        public async Task GetQuickActionsAsync_WithCSharpFile_ShouldReturnCSharpSpecificActions()
        {
            // Arrange
            var context = new ChatContext
            {
                CurrentFile = @"C:\Projects\TestApp\Services\UserService.cs"
            };

            // Act
            var actions = await _analyzerService.GetQuickActionsAsync(context);

            // Assert
            actions.Should().NotBeEmpty();
            actions.Should().Contain(a => a.Category == "Analysis");
            actions.Should().Contain(a => a.Category == "C# Analysis");
            actions.Should().Contain(a => a.Category == "Testing");
            
            var analyzeAction = actions.FirstOrDefault(a => a.Title.Contains("Analyze"));
            analyzeAction.Should().NotBeNull();
            analyzeAction!.Priority.Should().Be(1);
        }

        [Fact]
        public async Task GetQuickActionsAsync_WithSelectedText_ShouldReturnSelectionActions()
        {
            // Arrange
            var context = new ChatContext
            {
                CurrentFile = @"C:\Projects\TestApp\Program.cs",
                SelectedText = @"
                    public void ProcessData(string input)
                    {
                        // TODO: Implement processing logic
                        Console.WriteLine(input);
                    }"
            };

            // Act
            var actions = await _analyzerService.GetQuickActionsAsync(context);

            // Assert
            actions.Should().NotBeEmpty();
            actions.Should().Contain(a => a.Category == "Understanding");
            actions.Should().Contain(a => a.Category == "Refactoring");
            actions.Should().Contain(a => a.Category == "Documentation");
            
            var explainAction = actions.FirstOrDefault(a => a.Title.Contains("Explain"));
            explainAction.Should().NotBeNull();
            explainAction!.Command.Should().Contain("ProcessData");
        }

        [Fact]
        public async Task GetQuickActionsAsync_WithCompilationErrors_ShouldReturnErrorFixActions()
        {
            // Arrange
            var context = new ChatContext
            {
                CurrentFile = @"C:\Projects\TestApp\Program.cs",
                Errors = new List<CompilerError>
                {
                    new() { Severity = "Error", Message = "CS0103: Undefined variable", Code = "CS0103" }
                }
            };

            // Act
            var actions = await _analyzerService.GetQuickActionsAsync(context);

            // Assert
            actions.Should().NotBeEmpty();
            actions.Should().Contain(a => a.Category == "Error Fixing");
            actions.Should().Contain(a => a.Title.Contains("Fix Errors"));
            
            var fixAction = actions.FirstOrDefault(a => a.Category == "Error Fixing");
            fixAction.Should().NotBeNull();
            fixAction!.Priority.Should().Be(1); // Error fixing should be highest priority
        }

        [Fact]
        public async Task GetContextualSuggestionsAsync_WithNoInput_ShouldReturnGeneralSuggestions()
        {
            // Arrange
            var context = new ChatContext
            {
                CurrentFile = @"C:\Projects\TestApp\Controllers\ApiController.cs"
            };

            // Act
            var suggestions = await _analyzerService.GetContextualSuggestionsAsync(context);

            // Assert
            suggestions.Should().NotBeEmpty();
            suggestions.Should().HaveCountLessThanOrEqualTo(10); // Should be limited
            suggestions.Should().Contain(s => s.Contains("ApiController.cs"));
            suggestions.Should().Contain(s => s.Contains("best practices"));
        }

        [Fact]
        public async Task GetContextualSuggestionsAsync_WithTestKeyword_ShouldReturnTestSuggestions()
        {
            // Arrange
            var context = new ChatContext
            {
                CurrentFile = @"C:\Projects\TestApp\Services\DataService.cs"
            };
            var userInput = "generate test";

            // Act
            var suggestions = await _analyzerService.GetContextualSuggestionsAsync(context, userInput);

            // Assert
            suggestions.Should().NotBeEmpty();
            suggestions.Should().Contain(s => s.Contains("unit tests"));
            suggestions.Should().Contain(s => s.Contains("test cases"));
            suggestions.Should().Contain(s => s.Contains("integration tests"));
        }

        [Fact]
        public async Task GetContextualSuggestionsAsync_WithErrorKeyword_ShouldReturnErrorSuggestions()
        {
            // Arrange
            var context = new ChatContext
            {
                CurrentFile = @"C:\Projects\TestApp\Program.cs",
                Errors = new List<CompilerError>
                {
                    new() { Severity = "Error", Message = "Compilation error" }
                }
            };
            var userInput = "fix error";

            // Act
            var suggestions = await _analyzerService.GetContextualSuggestionsAsync(context, userInput);

            // Assert
            suggestions.Should().NotBeEmpty();
            suggestions.Should().Contain(s => s.Contains("error"));
            suggestions.Should().Contain(s => s.Contains("compilation errors"));
            suggestions.Should().Contain(s => s.Contains("debug"));
        }

        [Theory]
        [InlineData(".js", "JavaScript", "JavaScript File")]
        [InlineData(".ts", "TypeScript", "TypeScript File")]
        [InlineData(".py", "Python", "Python File")]
        [InlineData(".java", "Java", "Java File")]
        [InlineData(".cpp", "C++", "C++ File")]
        public async Task AnalyzeContextAsync_WithDifferentFileTypes_ShouldDetectCorrectLanguage(
            string extension, string expectedLanguage, string expectedFileType)
        {
            // Arrange
            var context = new ChatContext
            {
                CurrentFile = $@"C:\Projects\TestApp\src\main{extension}"
            };

            // Act
            var result = await _analyzerService.AnalyzeContextAsync(context);

            // Assert
            result.Should().NotBeNull();
            result.CurrentFileAnalysis.Should().NotBeNull();
            result.CurrentFileAnalysis!.Language.Should().Be(expectedLanguage);
            result.CurrentFileAnalysis.FileType.Should().Be(expectedFileType);
        }
    }
}