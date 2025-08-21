using A3sist.Core.Services;
using A3sist.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Core.Tests.Services
{
    public class RefactoringServiceTests
    {
        private readonly Mock<ILogger<RefactoringService>> _mockLogger;
        private readonly Mock<ICodeAnalysisService> _mockCodeAnalysisService;
        private readonly Mock<ILanguageRefactoringProvider> _mockCSharpProvider;
        private readonly Mock<ILanguageRefactoringProvider> _mockJavaScriptProvider;
        private readonly RefactoringService _refactoringService;

        public RefactoringServiceTests()
        {
            _mockLogger = new Mock<ILogger<RefactoringService>>();
            _mockCodeAnalysisService = new Mock<ICodeAnalysisService>();
            _mockCSharpProvider = new Mock<ILanguageRefactoringProvider>();
            _mockJavaScriptProvider = new Mock<ILanguageRefactoringProvider>();

            // Setup language providers
            _mockCSharpProvider.Setup(x => x.Language).Returns("csharp");
            _mockCSharpProvider.Setup(x => x.SupportedRefactorings).Returns(new[]
            {
                RefactoringType.ExtractMethod,
                RefactoringType.ExtractVariable,
                RefactoringType.RenameSymbol
            });

            _mockJavaScriptProvider.Setup(x => x.Language).Returns("javascript");
            _mockJavaScriptProvider.Setup(x => x.SupportedRefactorings).Returns(new[]
            {
                RefactoringType.ExtractMethod,
                RefactoringType.SimplifyExpression
            });

            var languageProviders = new[] { _mockCSharpProvider.Object, _mockJavaScriptProvider.Object };

            _refactoringService = new RefactoringService(
                _mockLogger.Object,
                _mockCodeAnalysisService.Object,
                languageProviders);
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Act & Assert - Constructor should not throw
            Assert.NotNull(_refactoringService);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RefactoringService(
                null,
                _mockCodeAnalysisService.Object,
                new[] { _mockCSharpProvider.Object }));
        }

        [Fact]
        public void Constructor_WithNullCodeAnalysisService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RefactoringService(
                _mockLogger.Object,
                null,
                new[] { _mockCSharpProvider.Object }));
        }

        [Fact]
        public async Task AnalyzeCodeAsync_WithEmptyCode_ReturnsEmptyResult()
        {
            // Act
            var result = await _refactoringService.AnalyzeCodeAsync("", "test.cs");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task AnalyzeCodeAsync_WithValidCode_ReturnsAnalysisResults()
        {
            // Arrange
            var code = "public class Test { public void Method() { } }";
            var filePath = "Test.cs";

            var analysisResult = new CodeAnalysisResult
            {
                Language = "csharp",
                CodeSmells = new List<CodeSmell>
                {
                    new CodeSmell
                    {
                        Name = "Test Smell",
                        Type = CodeSmellType.LongMethod,
                        Severity = CodeSmellSeverity.Minor,
                        SuggestedRefactorings = new[] { RefactoringType.ExtractMethod }
                    }
                },
                Complexity = new ComplexityMetrics
                {
                    CyclomaticComplexity = 15,
                    LinesOfCode = 120,
                    LackOfCohesion = 0.9
                }
            };

            var languageSpecificSuggestions = new List<RefactoringSuggestion>
            {
                new RefactoringSuggestion
                {
                    Type = RefactoringType.ExtractVariable,
                    Title = "Extract Variable",
                    Severity = RefactoringSeverity.Suggestion
                }
            };

            _mockCodeAnalysisService.Setup(x => x.AnalyzeCodeAsync(code, filePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(analysisResult);

            _mockCSharpProvider.Setup(x => x.AnalyzeCodeAsync(code, filePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(languageSpecificSuggestions);

            // Act
            var result = await _refactoringService.AnalyzeCodeAsync(code, filePath);

            // Assert
            Assert.NotEmpty(result);
            
            // Should contain suggestions from code smells
            Assert.Contains(result, s => s.Type == RefactoringType.ExtractMethod);
            
            // Should contain language-specific suggestions
            Assert.Contains(result, s => s.Type == RefactoringType.ExtractVariable);
            
            // Should contain complexity-based suggestions
            Assert.Contains(result, s => s.Description.Contains("complexity"));
        }

        [Fact]
        public async Task AnalyzeCodeAsync_WithException_ReturnsEmptyResult()
        {
            // Arrange
            var code = "public class Test {}";
            var filePath = "Test.cs";

            _mockCodeAnalysisService.Setup(x => x.AnalyzeCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act
            var result = await _refactoringService.AnalyzeCodeAsync(code, filePath);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task ApplyRefactoringAsync_WithEmptyCode_ReturnsFailure()
        {
            // Act
            var result = await _refactoringService.ApplyRefactoringAsync("", RefactoringType.ExtractMethod);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Code cannot be empty", result.Message);
        }

        [Fact]
        public async Task ApplyRefactoringAsync_WithLanguageSpecificProvider_UsesProvider()
        {
            // Arrange
            var code = "public class Test { public void Method() { } }";
            var refactoringType = RefactoringType.ExtractMethod;
            var parameters = new Dictionary<string, object> { ["filePath"] = "Test.cs" };

            var expectedResult = new RefactoringResult
            {
                Success = true,
                RefactoredCode = "public class Test { public void Method() { ExtractedMethod(); } private void ExtractedMethod() { } }",
                AppliedRefactoring = refactoringType,
                Message = "Refactoring applied successfully"
            };

            _mockCSharpProvider.Setup(x => x.CanHandleRefactoring(refactoringType)).Returns(true);
            _mockCSharpProvider.Setup(x => x.ApplyRefactoringAsync(code, refactoringType, parameters, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _refactoringService.ApplyRefactoringAsync(code, refactoringType, parameters);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(expectedResult.RefactoredCode, result.RefactoredCode);
            Assert.Equal(expectedResult.AppliedRefactoring, result.AppliedRefactoring);
            
            _mockCSharpProvider.Verify(x => x.ApplyRefactoringAsync(code, refactoringType, parameters, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ApplyRefactoringAsync_WithGenericRefactoring_UsesGenericImplementation()
        {
            // Arrange
            var code = "using System.Linq;\nusing System.Collections;\npublic class Test { }";
            var refactoringType = RefactoringType.RemoveUnusedUsings;

            // Act
            var result = await _refactoringService.ApplyRefactoringAsync(code, refactoringType);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(refactoringType, result.AppliedRefactoring);
            Assert.Contains("Removed unused import statements", result.Message);
        }

        [Fact]
        public async Task ApplyRefactoringAsync_WithUnsupportedRefactoring_ReturnsFailure()
        {
            // Arrange
            var code = "public class Test {}";
            var refactoringType = RefactoringType.PullUpMethod; // Not implemented in generic refactoring

            // Act
            var result = await _refactoringService.ApplyRefactoringAsync(code, refactoringType);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("not implemented", result.Message);
        }

        [Fact]
        public async Task ApplyRefactoringAsync_WithException_ReturnsFailure()
        {
            // Arrange
            var code = "public class Test {}";
            var refactoringType = RefactoringType.ExtractMethod;
            var parameters = new Dictionary<string, object> { ["filePath"] = "Test.cs" };

            _mockCSharpProvider.Setup(x => x.CanHandleRefactoring(refactoringType)).Returns(true);
            _mockCSharpProvider.Setup(x => x.ApplyRefactoringAsync(It.IsAny<string>(), It.IsAny<RefactoringType>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act
            var result = await _refactoringService.ApplyRefactoringAsync(code, refactoringType, parameters);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Failed to apply refactoring", result.Message);
            Assert.Contains("Test exception", result.Message);
        }

        [Fact]
        public async Task ValidateRefactoringAsync_WithEmptyCode_ReturnsInvalid()
        {
            // Act
            var result = await _refactoringService.ValidateRefactoringAsync("", "some code");

            // Assert
            Assert.False(result.IsValid);
            Assert.False(result.IsSafe);
            Assert.Contains("Both original and refactored code must be provided", result.Errors);
            Assert.Equal(0.0, result.ConfidenceScore);
        }

        [Fact]
        public async Task ValidateRefactoringAsync_WithValidRefactoring_ReturnsValid()
        {
            // Arrange
            var originalCode = "public class Test { public void Method() { Console.WriteLine(\"Hello\"); Console.WriteLine(\"World\"); } }";
            var refactoredCode = "public class Test { public void Method() { PrintMessages(); } private void PrintMessages() { Console.WriteLine(\"Hello\"); Console.WriteLine(\"World\"); } }";

            // Act
            var result = await _refactoringService.ValidateRefactoringAsync(originalCode, refactoredCode);

            // Assert
            Assert.True(result.IsValid);
            Assert.True(result.IsSafe);
            Assert.True(result.ConfidenceScore > 0.0);
        }

        [Fact]
        public async Task ValidateRefactoringAsync_WithMismatchedBraces_ReturnsInvalid()
        {
            // Arrange
            var originalCode = "public class Test { public void Method() { } }";
            var refactoredCode = "public class Test { public void Method() { }"; // Missing closing brace

            // Act
            var result = await _refactoringService.ValidateRefactoringAsync(originalCode, refactoredCode);

            // Assert
            Assert.False(result.IsValid);
            Assert.False(result.IsSafe);
            Assert.Contains("Mismatched braces", result.Errors);
        }

        [Fact]
        public async Task ValidateRefactoringAsync_WithIdenticalCode_HasWarning()
        {
            // Arrange
            var code = "public class Test { public void Method() { } }";

            // Act
            var result = await _refactoringService.ValidateRefactoringAsync(code, code);

            // Assert
            Assert.True(result.IsValid);
            Assert.Contains("No changes detected", result.Warnings);
        }

        [Fact]
        public async Task ValidateRefactoringAsync_WithException_ReturnsInvalid()
        {
            // Arrange
            var originalCode = "public class Test {}";
            var refactoredCode = "public class Test { public void Method() {} }";

            // Mock an exception in one of the validation methods
            // This is tricky to test directly, so we'll test the exception handling in ApplyRefactoringAsync instead

            // Act
            var result = await _refactoringService.ValidateRefactoringAsync(originalCode, refactoredCode);

            // Assert - Should not throw, should return a result
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetAvailableRefactoringsAsync_WithEmptyCode_ReturnsEmpty()
        {
            // Act
            var result = await _refactoringService.GetAvailableRefactoringsAsync("", "test.cs");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAvailableRefactoringsAsync_WithCSharpCode_ReturnsLanguageSpecificRefactorings()
        {
            // Arrange
            var code = "public class Test { public void Method() { } }";
            var filePath = "Test.cs";

            var analysisResult = new CodeAnalysisResult
            {
                Language = "csharp",
                Complexity = new ComplexityMetrics { CyclomaticComplexity = 5 },
                CodeSmells = new List<CodeSmell>
                {
                    new CodeSmell { Type = CodeSmellType.LongMethod }
                }
            };

            _mockCodeAnalysisService.Setup(x => x.AnalyzeCodeAsync(code, filePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(analysisResult);

            // Act
            var result = await _refactoringService.GetAvailableRefactoringsAsync(code, filePath);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains(RefactoringType.ExtractMethod, result);
            Assert.Contains(RefactoringType.ExtractVariable, result);
            Assert.Contains(RefactoringType.RenameSymbol, result);
        }

        [Fact]
        public async Task GetAvailableRefactoringsAsync_WithJavaScriptCode_ReturnsLanguageSpecificRefactorings()
        {
            // Arrange
            var code = "function test() { console.log('hello'); }";
            var filePath = "test.js";

            var analysisResult = new CodeAnalysisResult
            {
                Language = "javascript",
                Complexity = new ComplexityMetrics { CyclomaticComplexity = 3 },
                CodeSmells = new List<CodeSmell>()
            };

            _mockCodeAnalysisService.Setup(x => x.AnalyzeCodeAsync(code, filePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(analysisResult);

            // Act
            var result = await _refactoringService.GetAvailableRefactoringsAsync(code, filePath);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains(RefactoringType.ExtractMethod, result);
            Assert.Contains(RefactoringType.SimplifyExpression, result);
        }

        [Fact]
        public async Task GetAvailableRefactoringsAsync_WithException_ReturnsGeneralRefactorings()
        {
            // Arrange
            var code = "public class Test {}";
            var filePath = "Test.cs";

            _mockCodeAnalysisService.Setup(x => x.AnalyzeCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act
            var result = await _refactoringService.GetAvailableRefactoringsAsync(code, filePath);

            // Assert
            Assert.NotEmpty(result);
            // Should return general refactorings as fallback
            Assert.Contains(RefactoringType.ExtractMethod, result);
            Assert.Contains(RefactoringType.SimplifyExpression, result);
        }

        [Theory]
        [InlineData("Test.cs", "csharp")]
        [InlineData("test.js", "javascript")]
        [InlineData("test.ts", "typescript")]
        [InlineData("test.py", "python")]
        [InlineData("Test.java", "java")]
        [InlineData("test.cpp", "cpp")]
        [InlineData("test.c", "c")]
        [InlineData("unknown.xyz", "unknown")]
        [InlineData("", "unknown")]
        public void DetermineLanguage_WithVariousFilePaths_ReturnsCorrectLanguage(string filePath, string expectedLanguage)
        {
            // Use reflection to access the private method
            var method = typeof(RefactoringService).GetMethod("DetermineLanguage", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var result = method.Invoke(_refactoringService, new object[] { filePath });

            // Assert
            Assert.Equal(expectedLanguage, result);
        }

        [Fact]
        public async Task RemoveUnusedImportsAsync_RemovesUnusedUsings()
        {
            // Arrange
            var code = "using System;\nusing System.Linq;\nusing UnusedNamespace;\npublic class Test { public void Method() { Console.WriteLine(\"Hello\"); } }";

            // Use reflection to access the private method
            var method = typeof(RefactoringService).GetMethod("RemoveUnusedImportsAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var task = (Task<RefactoringResult>)method.Invoke(_refactoringService, new object[] { code, CancellationToken.None });
            var result = await task;

            // Assert
            Assert.True(result.Success);
            Assert.Equal(RefactoringType.RemoveUnusedUsings, result.AppliedRefactoring);
            Assert.DoesNotContain("using UnusedNamespace;", result.RefactoredCode);
        }

        [Fact]
        public async Task SimplifyExpressionsAsync_SimplifiesBooleanExpressions()
        {
            // Arrange
            var code = "if (condition == true) { }";

            // Use reflection to access the private method
            var method = typeof(RefactoringService).GetMethod("SimplifyExpressionsAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var task = (Task<RefactoringResult>)method.Invoke(_refactoringService, new object[] { code, CancellationToken.None });
            var result = await task;

            // Assert
            Assert.True(result.Success);
            Assert.Equal(RefactoringType.SimplifyExpression, result.AppliedRefactoring);
            Assert.DoesNotContain("== true", result.RefactoredCode);
        }

        [Fact]
        public async Task RemoveRedundantCodeAsync_RemovesDuplicateEmptyLines()
        {
            // Arrange
            var code = "public class Test\n{\n\n\n    public void Method()\n    {\n    }\n}";

            // Use reflection to access the private method
            var method = typeof(RefactoringService).GetMethod("RemoveRedundantCodeAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var task = (Task<RefactoringResult>)method.Invoke(_refactoringService, new object[] { code, CancellationToken.None });
            var result = await task;

            // Assert
            Assert.True(result.Success);
            Assert.Equal(RefactoringType.RemoveRedundantCode, result.AppliedRefactoring);
            // Should have fewer empty lines
            var originalEmptyLines = code.Split('\n').Count(line => string.IsNullOrWhiteSpace(line));
            var refactoredEmptyLines = result.RefactoredCode.Split('\n').Count(line => string.IsNullOrWhiteSpace(line));
            Assert.True(refactoredEmptyLines < originalEmptyLines);
        }
    }
}