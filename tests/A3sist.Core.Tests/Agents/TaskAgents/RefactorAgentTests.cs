using A3sist.Core.Agents.TaskAgents.Refactor;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Core.Tests.Agents.Task
{
    public class RefactorAgentTests
    {
        private readonly Mock<ILogger<RefactorAgent>> _mockLogger;
        private readonly Mock<IAgentConfiguration> _mockConfiguration;
        private readonly Mock<IRefactoringService> _mockRefactoringService;
        private readonly Mock<ICodeAnalysisService> _mockCodeAnalysisService;
        private readonly Mock<ILanguageRefactoringProvider> _mockLanguageProvider;
        private readonly RefactorAgent _refactorAgent;

        public RefactorAgentTests()
        {
            _mockLogger = new Mock<ILogger<RefactorAgent>>();
            _mockConfiguration = new Mock<IAgentConfiguration>();
            _mockRefactoringService = new Mock<IRefactoringService>();
            _mockCodeAnalysisService = new Mock<ICodeAnalysisService>();
            _mockLanguageProvider = new Mock<ILanguageRefactoringProvider>();

            _mockLanguageProvider.Setup(x => x.Language).Returns("csharp");
            _mockLanguageProvider.Setup(x => x.InitializeAsync()).Returns(Task.CompletedTask);
            _mockLanguageProvider.Setup(x => x.ShutdownAsync()).Returns(Task.CompletedTask);

            var languageProviders = new[] { _mockLanguageProvider.Object };

            _refactorAgent = new RefactorAgent(
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockRefactoringService.Object,
                _mockCodeAnalysisService.Object,
                languageProviders);
        }

        [Fact]
        public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
        {
            // Assert
            Assert.Equal("RefactorAgent", _refactorAgent.Name);
            Assert.Equal(AgentType.Refactor, _refactorAgent.Type);
        }

        [Fact]
        public void Constructor_WithNullRefactoringService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RefactorAgent(
                _mockLogger.Object,
                _mockConfiguration.Object,
                null,
                _mockCodeAnalysisService.Object,
                new[] { _mockLanguageProvider.Object }));
        }

        [Fact]
        public void Constructor_WithNullCodeAnalysisService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RefactorAgent(
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockRefactoringService.Object,
                null,
                new[] { _mockLanguageProvider.Object }));
        }

        [Theory]
        [InlineData("refactor this code", "public class Test {}", true)]
        [InlineData("improve the code quality", "function test() {}", true)]
        [InlineData("optimize this method", "def test(): pass", true)]
        [InlineData("restructure the class", "class Test {}", true)]
        [InlineData("extract method from this", "public void Method() {}", true)]
        [InlineData("inline this variable", "var x = 5;", true)]
        [InlineData("rename this symbol", "public int Count;", true)]
        [InlineData("move this method", "public void Test() {}", true)]
        [InlineData("simplify expression", "if (x == true)", true)]
        [InlineData("clean up the code", "// messy code", true)]
        [InlineData("hello world", "", false)]
        [InlineData("compile the project", "", false)]
        [InlineData("", "", false)]
        public async Task CanHandleAsync_WithVariousRequests_ReturnsExpectedResult(string prompt, string content, bool expectedResult)
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = prompt,
                Content = content
            };

            // Act
            var result = await _refactorAgent.CanHandleAsync(request);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task CanHandleAsync_WithNullRequest_ReturnsFalse()
        {
            // Act
            var result = await _refactorAgent.CanHandleAsync(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanHandleAsync_WithRefactoringContext_ReturnsTrue()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "some request",
                Content = "public class Test {}",
                Context = new Dictionary<string, object>
                {
                    ["refactoring"] = "extract_method"
                }
            };

            // Act
            var result = await _refactorAgent.CanHandleAsync(request);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HandleAsync_WithAnalyzeAction_ReturnsAnalysisResult()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "analyze this code for refactoring",
                Content = "public class Test { public void Method() { } }",
                FilePath = "Test.cs"
            };

            var analysisResult = new CodeAnalysisResult
            {
                Language = "csharp",
                Complexity = new ComplexityMetrics { CyclomaticComplexity = 5 },
                CodeSmells = new List<CodeSmell>
                {
                    new CodeSmell
                    {
                        Name = "Test Smell",
                        Type = CodeSmellType.LongMethod,
                        Severity = CodeSmellSeverity.Minor,
                        StartLine = 1,
                        EndLine = 5
                    }
                },
                Elements = new List<CodeElement>(),
                Dependencies = new DependencyAnalysisResult()
            };

            var suggestions = new List<RefactoringSuggestion>
            {
                new RefactoringSuggestion
                {
                    Type = RefactoringType.ExtractMethod,
                    Title = "Extract Method",
                    Description = "Consider extracting a method",
                    Severity = RefactoringSeverity.Suggestion,
                    StartLine = 1,
                    EndLine = 5
                }
            };

            var availableRefactorings = new List<RefactoringType> { RefactoringType.ExtractMethod };

            _mockCodeAnalysisService.Setup(x => x.AnalyzeCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(analysisResult);

            _mockRefactoringService.Setup(x => x.AnalyzeCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(suggestions);

            _mockRefactoringService.Setup(x => x.GetAvailableRefactoringsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(availableRefactorings);

            // Act
            var result = await _refactorAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("analysis completed successfully", result.Message);
            Assert.NotNull(result.Content);
            
            var resultData = JsonSerializer.Deserialize<JsonElement>(result.Content);
            Assert.True(resultData.TryGetProperty("Analysis", out _));
            Assert.True(resultData.TryGetProperty("Suggestions", out _));
            Assert.True(resultData.TryGetProperty("AvailableRefactorings", out _));
        }

        [Fact]
        public async Task HandleAsync_WithApplyRefactoringAction_AppliesRefactoring()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "refactor this code",
                Content = "public class Test { public void Method() { } }",
                FilePath = "Test.cs",
                Context = new Dictionary<string, object>
                {
                    ["refactoringType"] = "ExtractMethod"
                }
            };

            var refactoringResult = new RefactoringResult
            {
                Success = true,
                RefactoredCode = "public class Test { public void Method() { ExtractedMethod(); } private void ExtractedMethod() { } }",
                AppliedRefactoring = RefactoringType.ExtractMethod,
                Message = "Refactoring applied successfully"
            };

            var validationResult = new RefactoringValidationResult
            {
                IsValid = true,
                IsSafe = true,
                ConfidenceScore = 0.9
            };

            _mockRefactoringService.Setup(x => x.ApplyRefactoringAsync(
                It.IsAny<string>(), 
                It.IsAny<RefactoringType>(), 
                It.IsAny<Dictionary<string, object>>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(refactoringResult);

            _mockRefactoringService.Setup(x => x.ValidateRefactoringAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _refactorAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("applied successfully", result.Message);
            Assert.NotNull(result.Content);
            
            var resultData = JsonSerializer.Deserialize<JsonElement>(result.Content);
            Assert.True(resultData.TryGetProperty("RefactoredCode", out _));
            Assert.True(resultData.TryGetProperty("Validation", out _));
        }

        [Fact]
        public async Task HandleAsync_WithValidateAction_ValidatesRefactoring()
        {
            // Arrange
            var originalCode = "public class Test { public void Method() { } }";
            var refactoredCode = "public class Test { public void Method() { ExtractedMethod(); } private void ExtractedMethod() { } }";

            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "validate this refactoring",
                Content = originalCode,
                Context = new Dictionary<string, object>
                {
                    ["refactoredCode"] = refactoredCode
                }
            };

            var validationResult = new RefactoringValidationResult
            {
                IsValid = true,
                IsSafe = true,
                ConfidenceScore = 0.95,
                Errors = new List<string>(),
                Warnings = new List<string> { "Minor warning" },
                Suggestions = new List<string> { "Consider adding documentation" }
            };

            _mockRefactoringService.Setup(x => x.ValidateRefactoringAsync(
                originalCode, 
                refactoredCode, 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _refactorAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("validation completed", result.Message);
            Assert.NotNull(result.Content);
            
            var resultData = JsonSerializer.Deserialize<JsonElement>(result.Content);
            Assert.True(resultData.TryGetProperty("IsValid", out var isValidProp));
            Assert.True(isValidProp.GetBoolean());
        }

        [Fact]
        public async Task HandleAsync_WithPreviewAction_GeneratesPreview()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "preview this refactoring",
                Content = "public class Test { public void Method() { } }",
                FilePath = "Test.cs",
                Context = new Dictionary<string, object>
                {
                    ["refactoringType"] = "ExtractMethod"
                }
            };

            var refactoringResult = new RefactoringResult
            {
                Success = true,
                RefactoredCode = "public class Test { public void Method() { ExtractedMethod(); } private void ExtractedMethod() { } }",
                AppliedRefactoring = RefactoringType.ExtractMethod,
                Message = "Preview generated"
            };

            _mockRefactoringService.Setup(x => x.ApplyRefactoringAsync(
                It.IsAny<string>(), 
                It.IsAny<RefactoringType>(), 
                It.IsAny<Dictionary<string, object>>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(refactoringResult);

            // Act
            var result = await _refactorAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("preview generated", result.Message);
            Assert.NotNull(result.Content);
            
            var resultData = JsonSerializer.Deserialize<JsonElement>(result.Content);
            Assert.True(resultData.TryGetProperty("Preview", out _));
            Assert.True(resultData.TryGetProperty("Changes", out _));
        }

        [Fact]
        public async Task HandleAsync_WithOptimizeAction_OptimizesCode()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "optimize this code",
                Content = "public class Test { public void Method() { if (x == true) { } } }",
                FilePath = "Test.cs"
            };

            var suggestions = new List<RefactoringSuggestion>
            {
                new RefactoringSuggestion
                {
                    Type = RefactoringType.SimplifyExpression,
                    Title = "Simplify Boolean Expression",
                    Severity = RefactoringSeverity.Suggestion
                }
            };

            var refactoringResult = new RefactoringResult
            {
                Success = true,
                RefactoredCode = "public class Test { public void Method() { if (x) { } } }",
                AppliedRefactoring = RefactoringType.SimplifyExpression,
                Message = "Optimization applied"
            };

            var validationResult = new RefactoringValidationResult
            {
                IsValid = true,
                IsSafe = true,
                ConfidenceScore = 0.9
            };

            _mockRefactoringService.Setup(x => x.AnalyzeCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(suggestions);

            _mockRefactoringService.Setup(x => x.ApplyRefactoringAsync(
                It.IsAny<string>(), 
                RefactoringType.SimplifyExpression, 
                It.IsAny<Dictionary<string, object>>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(refactoringResult);

            _mockRefactoringService.Setup(x => x.ValidateRefactoringAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _refactorAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("optimization completed", result.Message);
            Assert.NotNull(result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithExtractAction_ExtractsCodeElement()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "extract method from this code",
                Content = "public class Test { public void Method() { Console.WriteLine(\"Hello\"); Console.WriteLine(\"World\"); } }",
                Context = new Dictionary<string, object>
                {
                    ["startLine"] = 1,
                    ["endLine"] = 2,
                    ["newName"] = "PrintMessages"
                }
            };

            var refactoringResult = new RefactoringResult
            {
                Success = true,
                RefactoredCode = "public class Test { public void Method() { PrintMessages(); } private void PrintMessages() { Console.WriteLine(\"Hello\"); Console.WriteLine(\"World\"); } }",
                AppliedRefactoring = RefactoringType.ExtractMethod,
                Message = "Method extracted successfully"
            };

            _mockRefactoringService.Setup(x => x.ApplyRefactoringAsync(
                It.IsAny<string>(), 
                RefactoringType.ExtractMethod, 
                It.IsAny<Dictionary<string, object>>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(refactoringResult);

            // Act
            var result = await _refactorAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("extracted method", result.Message);
            Assert.NotNull(result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithRenameAction_RenamesSymbol()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "rename this symbol",
                Content = "public class Test { public int Count; }",
                Context = new Dictionary<string, object>
                {
                    ["oldName"] = "Count",
                    ["newName"] = "ItemCount"
                }
            };

            var refactoringResult = new RefactoringResult
            {
                Success = true,
                RefactoredCode = "public class Test { public int ItemCount; }",
                AppliedRefactoring = RefactoringType.RenameSymbol,
                Message = "Symbol renamed successfully"
            };

            _mockRefactoringService.Setup(x => x.ApplyRefactoringAsync(
                It.IsAny<string>(), 
                RefactoringType.RenameSymbol, 
                It.IsAny<Dictionary<string, object>>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(refactoringResult);

            // Act
            var result = await _refactorAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("renamed 'Count' to 'ItemCount'", result.Message);
            Assert.NotNull(result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithRenameActionMissingParameters_ReturnsFailure()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "rename this symbol",
                Content = "public class Test { public int Count; }",
                Context = new Dictionary<string, object>
                {
                    ["oldName"] = "Count"
                    // Missing newName
                }
            };

            // Act
            var result = await _refactorAgent.HandleAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Both oldName and newName must be provided", result.Message);
        }

        [Fact]
        public async Task HandleAsync_WithException_ReturnsFailureResult()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "analyze this code",
                Content = "public class Test {}",
                FilePath = "Test.cs"
            };

            _mockCodeAnalysisService.Setup(x => x.AnalyzeCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act
            var result = await _refactorAgent.HandleAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("RefactorAgent error", result.Message);
            Assert.Contains("Test exception", result.Message);
        }

        [Fact]
        public async Task InitializeAsync_InitializesLanguageProviders()
        {
            // Act
            await _refactorAgent.InitializeAsync();

            // Assert
            _mockLanguageProvider.Verify(x => x.InitializeAsync(), Times.Once);
        }

        [Fact]
        public async Task ShutdownAsync_ShutsDownLanguageProviders()
        {
            // Arrange
            await _refactorAgent.InitializeAsync();

            // Act
            await _refactorAgent.ShutdownAsync();

            // Assert
            _mockLanguageProvider.Verify(x => x.ShutdownAsync(), Times.Once);
        }

        [Theory]
        [InlineData("analyze this code", "analyze")]
        [InlineData("suggest improvements", "suggest")]
        [InlineData("refactor the method", "refactor")]
        [InlineData("apply the changes", "apply")]
        [InlineData("validate this refactoring", "validate")]
        [InlineData("check the code", "validate")]
        [InlineData("preview the changes", "preview")]
        [InlineData("show me the diff", "preview")]
        [InlineData("optimize performance", "optimize")]
        [InlineData("improve the code", "optimize")]
        [InlineData("extract this method", "extract")]
        [InlineData("inline this variable", "inline")]
        [InlineData("rename the symbol", "rename")]
        [InlineData("move this method", "move")]
        [InlineData("unknown action", "analyze")] // Default
        public void ExtractActionFromRequest_WithVariousPrompts_ReturnsExpectedAction(string prompt, string expectedAction)
        {
            // Arrange
            var request = new AgentRequest
            {
                Prompt = prompt
            };

            // Use reflection to access the private method for testing
            var method = typeof(RefactorAgent).GetMethod("ExtractActionFromRequest", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var result = method.Invoke(_refactorAgent, new object[] { request });

            // Assert
            Assert.Equal(expectedAction, result);
        }

        [Fact]
        public async Task HandleAsync_WithGenericRequest_ReturnsAnalysisResult()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "help me with this code",
                Content = "public class Test { }",
                FilePath = "Test.cs"
            };

            var analysisResult = new CodeAnalysisResult
            {
                Language = "csharp",
                Complexity = new ComplexityMetrics(),
                CodeSmells = new List<CodeSmell>(),
                Elements = new List<CodeElement>(),
                Dependencies = new DependencyAnalysisResult()
            };

            var suggestions = new List<RefactoringSuggestion>();
            var availableRefactorings = new List<RefactoringType>();

            _mockCodeAnalysisService.Setup(x => x.AnalyzeCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(analysisResult);

            _mockRefactoringService.Setup(x => x.AnalyzeCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(suggestions);

            _mockRefactoringService.Setup(x => x.GetAvailableRefactoringsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(availableRefactorings);

            // Act
            var result = await _refactorAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Review suggestions", result.Message);
        }

        [Fact]
        public async Task HandleAsync_WithMoveAction_MovesCodeElement()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "move this method to another class",
                Content = "public class Test { public void Method() { } }",
                Context = new Dictionary<string, object>
                {
                    ["targetLocation"] = "TargetClass",
                    ["elementName"] = "Method"
                }
            };

            var refactoringResult = new RefactoringResult
            {
                Success = true,
                RefactoredCode = "public class Test { } public class TargetClass { public void Method() { } }",
                AppliedRefactoring = RefactoringType.MoveMethod,
                Message = "Method moved successfully"
            };

            _mockRefactoringService.Setup(x => x.ApplyRefactoringAsync(
                It.IsAny<string>(), 
                RefactoringType.MoveMethod, 
                It.IsAny<Dictionary<string, object>>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(refactoringResult);

            // Act
            var result = await _refactorAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("moved method to TargetClass", result.Message);
        }

        [Fact]
        public async Task HandleAsync_WithMoveActionMissingTargetLocation_ReturnsFailure()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "move this method",
                Content = "public class Test { public void Method() { } }",
                Context = new Dictionary<string, object>
                {
                    ["elementName"] = "Method"
                    // Missing targetLocation
                }
            };

            // Act
            var result = await _refactorAgent.HandleAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Target location must be provided", result.Message);
        }
    }
}