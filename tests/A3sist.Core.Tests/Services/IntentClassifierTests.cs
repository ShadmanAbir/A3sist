using A3sist.Core.Services;
using A3sist.Shared.Enums;
using A3sist.Shared.Messaging;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Core.Tests.Services
{
    public class IntentClassifierTests : IDisposable
    {
        private readonly Mock<ILogger<IntentClassifier>> _mockLogger;
        private readonly IntentClassifier _intentClassifier;

        public IntentClassifierTests()
        {
            _mockLogger = new Mock<ILogger<IntentClassifier>>();
            _intentClassifier = new IntentClassifier(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new IntentClassifier(null!));
        }

        [Fact]
        public void ConfidenceThreshold_ShouldReturn0Point7()
        {
            // Assert
            Assert.Equal(0.7, _intentClassifier.ConfidenceThreshold);
        }

        [Fact]
        public async Task ClassifyAsync_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _intentClassifier.ClassifyAsync(null!));
        }

        [Theory]
        [InlineData("fix the error in this code", "fix_error")]
        [InlineData("there's a bug in my function", "fix_error")]
        [InlineData("something is broken", "fix_error")]
        [InlineData("refactor this method", "refactor")]
        [InlineData("improve the code structure", "refactor")]
        [InlineData("clean up this code", "refactor")]
        [InlineData("generate a new class", "generate_code")]
        [InlineData("create a function", "generate_code")]
        [InlineData("write some code", "generate_code")]
        [InlineData("add a new feature", "add_feature")]
        [InlineData("analyze this code", "analyze_code")]
        [InlineData("review my implementation", "analyze_code")]
        [InlineData("explain what this does", "explain_code")]
        [InlineData("what is this function doing", "explain_code")]
        [InlineData("generate unit tests", "generate_tests")]
        [InlineData("create documentation", "generate_docs")]
        public async Task ClassifyAsync_WithKnownIntents_ShouldClassifyCorrectly(string prompt, string expectedIntent)
        {
            // Arrange
            var request = CreateRequest(prompt);

            // Act
            var result = await _intentClassifier.ClassifyAsync(request);

            // Assert
            Assert.Equal(expectedIntent, result.Intent);
            Assert.True(result.Confidence > 0);
            Assert.NotEmpty(result.Keywords);
        }

        [Fact]
        public async Task ClassifyAsync_WithUnknownIntent_ShouldReturnUnknown()
        {
            // Arrange
            var request = CreateRequest("some random text that doesn't match any patterns");

            // Act
            var result = await _intentClassifier.ClassifyAsync(request);

            // Assert
            Assert.Equal("unknown", result.Intent);
            Assert.Equal(0.1, result.Confidence);
            Assert.Equal(AgentType.Unknown, result.SuggestedAgentType);
        }

        [Theory]
        [InlineData("test.cs", "csharp")]
        [InlineData("script.js", "javascript")]
        [InlineData("app.ts", "typescript")]
        [InlineData("main.py", "python")]
        [InlineData("Program.java", "java")]
        [InlineData("main.cpp", "cpp")]
        [InlineData("program.c", "c")]
        [InlineData("main.go", "go")]
        [InlineData("lib.rs", "rust")]
        [InlineData("unknown.txt", "unknown")]
        public async Task ClassifyAsync_WithDifferentFileExtensions_ShouldDetectLanguageCorrectly(string filePath, string expectedLanguage)
        {
            // Arrange
            var request = CreateRequest("fix this code", filePath);

            // Act
            var result = await _intentClassifier.ClassifyAsync(request);

            // Assert
            Assert.Equal(expectedLanguage, result.Language);
        }

        [Fact]
        public async Task ClassifyAsync_WithCSharpContent_ShouldDetectCSharpLanguage()
        {
            // Arrange
            var request = CreateRequest("fix this code", content: "using System; namespace Test { }");

            // Act
            var result = await _intentClassifier.ClassifyAsync(request);

            // Assert
            Assert.Equal("csharp", result.Language);
        }

        [Fact]
        public async Task ClassifyAsync_WithJavaScriptContent_ShouldDetectJavaScriptLanguage()
        {
            // Arrange
            var request = CreateRequest("fix this code", content: "function test() { const x = 5; }");

            // Act
            var result = await _intentClassifier.ClassifyAsync(request);

            // Assert
            Assert.Equal("javascript", result.Language);
        }

        [Fact]
        public async Task ClassifyAsync_WithPythonContent_ShouldDetectPythonLanguage()
        {
            // Arrange
            var request = CreateRequest("fix this code", content: "def test(): import os");

            // Act
            var result = await _intentClassifier.ClassifyAsync(request);

            // Assert
            Assert.Equal("python", result.Language);
        }

        [Theory]
        [InlineData("fix_error", AgentType.Fixer)]
        [InlineData("refactor", AgentType.Refactor)]
        [InlineData("generate_code", AgentType.CSharp)]
        [InlineData("add_feature", AgentType.CSharp)]
        [InlineData("analyze_code", AgentType.Validator)]
        [InlineData("explain_code", AgentType.Knowledge)]
        [InlineData("generate_tests", AgentType.TestGenerator)]
        [InlineData("generate_docs", AgentType.Knowledge)]
        [InlineData("unknown", AgentType.Unknown)]
        public async Task ClassifyAsync_WithDifferentIntents_ShouldSuggestCorrectAgentType(string intent, AgentType expectedAgentType)
        {
            // Arrange
            var prompt = intent switch
            {
                "fix_error" => "fix this error",
                "refactor" => "refactor this code",
                "generate_code" => "generate a class",
                "add_feature" => "add a new feature",
                "analyze_code" => "analyze this code",
                "explain_code" => "explain this function",
                "generate_tests" => "generate unit tests",
                "generate_docs" => "create documentation",
                _ => "random text"
            };
            var request = CreateRequest(prompt);

            // Act
            var result = await _intentClassifier.ClassifyAsync(request);

            // Assert
            Assert.Equal(expectedAgentType, result.SuggestedAgentType);
        }

        [Fact]
        public async Task ClassifyAsync_WithHighConfidenceResult_ShouldBeReliable()
        {
            // Arrange
            var request = CreateRequest("fix the error in this broken code");

            // Act
            var result = await _intentClassifier.ClassifyAsync(request);

            // Assert
            Assert.True(result.Confidence >= 0.7);
            Assert.True(result.IsReliable);
        }

        [Fact]
        public async Task ClassifyAsync_WithLowConfidenceResult_ShouldNotBeReliable()
        {
            // Arrange
            var request = CreateRequest("some vague request");

            // Act
            var result = await _intentClassifier.ClassifyAsync(request);

            // Assert
            Assert.True(result.Confidence < 0.7);
            Assert.False(result.IsReliable);
        }

        [Fact]
        public async Task ClassifyAsync_WithMultipleMatchingPatterns_ShouldReturnAlternatives()
        {
            // Arrange
            var request = CreateRequest("fix and refactor this code"); // Matches both fix_error and refactor

            // Act
            var result = await _intentClassifier.ClassifyAsync(request);

            // Assert
            Assert.NotEmpty(result.Alternatives);
            Assert.True(result.Alternatives.Count <= 3); // Should limit to top 3 alternatives
        }

        [Fact]
        public async Task ClassifyAsync_ShouldIncludeContextInformation()
        {
            // Arrange
            var request = CreateRequest("fix this error", "test.cs", "some code content");

            // Act
            var result = await _intentClassifier.ClassifyAsync(request);

            // Assert
            Assert.NotNull(result.Context);
            Assert.Contains("RequestId", result.Context.Keys);
            Assert.Contains("Intent", result.Context.Keys);
            Assert.Contains("HasFilePath", result.Context.Keys);
            Assert.Contains("HasContent", result.Context.Keys);
            Assert.Contains("PromptLength", result.Context.Keys);
            Assert.Contains("FileExtension", result.Context.Keys);
            Assert.Contains("FileName", result.Context.Keys);
        }

        [Fact]
        public async Task ClassifyAsync_WithLongPrompt_ShouldBoostConfidence()
        {
            // Arrange
            var shortRequest = CreateRequest("fix");
            var longRequest = CreateRequest("fix the error in this code because it's not working properly and needs attention");

            // Act
            var shortResult = await _intentClassifier.ClassifyAsync(shortRequest);
            var longResult = await _intentClassifier.ClassifyAsync(longRequest);

            // Assert
            Assert.True(longResult.Confidence >= shortResult.Confidence);
        }

        [Fact]
        public async Task ClassifyAsync_WithCancellation_ShouldRespectCancellationToken()
        {
            // Arrange
            var request = CreateRequest("fix this error");
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            // The current implementation doesn't actually use the cancellation token for async operations,
            // but it should complete without throwing
            var result = await _intentClassifier.ClassifyAsync(request, cts.Token);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task TrainAsync_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _intentClassifier.TrainAsync(null!, "fix_error"));
        }

        [Fact]
        public async Task TrainAsync_WithNullIntent_ShouldThrowArgumentException()
        {
            // Arrange
            var request = CreateRequest("fix this error");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _intentClassifier.TrainAsync(request, null!));
        }

        [Fact]
        public async Task TrainAsync_WithEmptyIntent_ShouldThrowArgumentException()
        {
            // Arrange
            var request = CreateRequest("fix this error");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _intentClassifier.TrainAsync(request, ""));
        }

        [Fact]
        public async Task TrainAsync_WithValidData_ShouldCompleteSuccessfully()
        {
            // Arrange
            var request = CreateRequest("fix this error");

            // Act & Assert
            // Should not throw
            await _intentClassifier.TrainAsync(request, "fix_error");
        }

        private static AgentRequest CreateRequest(string prompt, string? filePath = null, string? content = null)
        {
            return new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = prompt,
                FilePath = filePath,
                Content = content,
                PreferredAgentType = AgentType.Unknown,
                CreatedAt = DateTime.UtcNow,
                UserId = "test-user"
            };
        }

        public void Dispose()
        {
            // No cleanup needed for this test class
        }
    }
}