using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using A3sist.Orchastrator.Agents.CSharp;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace A3sist.Core.Tests.Agents.Language
{
    /// <summary>
    /// Unit tests for the CSharpAgent class
    /// </summary>
    public class CSharpAgentTests
    {
        private readonly Mock<ILogger<CSharpAgent>> _mockLogger;
        private readonly Mock<IAgentConfiguration> _mockConfiguration;
        private readonly CSharpAgent _agent;

        public CSharpAgentTests()
        {
            _mockLogger = new Mock<ILogger<CSharpAgent>>();
            _mockConfiguration = new Mock<IAgentConfiguration>();
            _agent = new CSharpAgent(_mockLogger.Object, _mockConfiguration.Object);
        }

        [Fact]
        public void Constructor_ShouldInitializeProperties()
        {
            // Assert
            _agent.Name.Should().Be("CSharpAgent");
            _agent.Type.Should().Be(AgentType.Language);
        }

        [Fact]
        public async Task InitializeAsync_ShouldInitializeSuccessfully()
        {
            // Act
            await _agent.InitializeAsync();

            // Assert
            // Verify that initialization completed without throwing
            _agent.Should().NotBeNull();
        }

        [Theory]
        [InlineData("analyze")]
        [InlineData("refactor")]
        [InlineData("validatexaml")]
        [InlineData("generate")]
        [InlineData("fix")]
        public async Task CanHandleAsync_WithSupportedRequestTypes_ShouldReturnTrue(string requestType)
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = $"Please {requestType} this code",
                Content = "public class TestClass { }",
                Context = new Dictionary<string, object> { ["requestType"] = requestType }
            };

            // Act
            var result = await _agent.CanHandleAsync(request);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CanHandleAsync_WithCSharpFile_ShouldReturnTrue()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                FilePath = "TestClass.cs",
                Content = "public class TestClass { }",
                Context = new Dictionary<string, object> { ["requestType"] = "analyze" }
            };

            // Act
            var result = await _agent.CanHandleAsync(request);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CanHandleAsync_WithXamlFile_ShouldReturnTrue()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                FilePath = "MainWindow.xaml",
                Content = "<Window></Window>",
                Context = new Dictionary<string, object> { ["requestType"] = "validatexaml" }
            };

            // Act
            var result = await _agent.CanHandleAsync(request);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CanHandleAsync_WithCSharpLanguageContext_ShouldReturnTrue()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Content = "public class TestClass { }",
                Context = new Dictionary<string, object> 
                { 
                    ["language"] = "csharp",
                    ["requestType"] = "analyze"
                }
            };

            // Act
            var result = await _agent.CanHandleAsync(request);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CanHandleAsync_WithUnsupportedRequestType_ShouldReturnFalse()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Content = "public class TestClass { }",
                Context = new Dictionary<string, object> { ["requestType"] = "unsupported" }
            };

            // Act
            var result = await _agent.CanHandleAsync(request);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task CanHandleAsync_WithNullRequest_ShouldReturnFalse()
        {
            // Act
            var result = await _agent.CanHandleAsync(null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task HandleAsync_WithAnalyzeRequest_ShouldReturnSuccessResult()
        {
            // Arrange
            await _agent.InitializeAsync();
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Content = "public class TestClass { public void TestMethod() { } }",
                Context = new Dictionary<string, object> { ["requestType"] = "analyze" }
            };

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.AgentName.Should().Be("CSharpAgent");
            result.Content.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task HandleAsync_WithRefactorRequest_ShouldReturnSuccessResult()
        {
            // Arrange
            await _agent.InitializeAsync();
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Content = "public class TestClass { public void TestMethod() { var x = new List<string>(); } }",
                Context = new Dictionary<string, object> { ["requestType"] = "refactor" }
            };

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.AgentName.Should().Be("CSharpAgent");
            result.Content.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task HandleAsync_WithValidateXamlRequest_ShouldReturnSuccessResult()
        {
            // Arrange
            await _agent.InitializeAsync();
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Content = "<Window xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Grid></Grid></Window>",
                Context = new Dictionary<string, object> { ["requestType"] = "validatexaml" }
            };

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.AgentName.Should().Be("CSharpAgent");
            result.Content.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task HandleAsync_WithGenerateRequest_ShouldReturnSuccessResult()
        {
            // Arrange
            await _agent.InitializeAsync();
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "Generate a simple C# class",
                Context = new Dictionary<string, object> { ["requestType"] = "generate" }
            };

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.AgentName.Should().Be("CSharpAgent");
            result.Content.Should().NotBeNullOrEmpty();
            result.Content.Should().Contain("class");
        }

        [Fact]
        public async Task HandleAsync_WithFixRequest_ShouldReturnSuccessResult()
        {
            // Arrange
            await _agent.InitializeAsync();
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Content = "public class TestClass { public void TestMethod() { var x = new List<string>(); } }",
                Context = new Dictionary<string, object> { ["requestType"] = "fix" }
            };

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.AgentName.Should().Be("CSharpAgent");
            result.Content.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task HandleAsync_WithUnsupportedRequestType_ShouldReturnFailureResult()
        {
            // Arrange
            await _agent.InitializeAsync();
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Content = "public class TestClass { }",
                Context = new Dictionary<string, object> { ["requestType"] = "unsupported" }
            };

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Unsupported request type");
        }

        [Fact]
        public async Task HandleAsync_WithNullRequest_ShouldReturnFailureResult()
        {
            // Arrange
            await _agent.InitializeAsync();

            // Act
            var result = await _agent.HandleAsync(null);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Request cannot be null");
        }

        [Fact]
        public async Task HandleAsync_WithEmptyCode_ShouldReturnFailureResult()
        {
            // Arrange
            await _agent.InitializeAsync();
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Content = "",
                Context = new Dictionary<string, object> { ["requestType"] = "analyze" }
            };

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("No code provided");
        }

        [Fact]
        public async Task HandleAsync_WithCancellationToken_ShouldRespectCancellation()
        {
            // Arrange
            await _agent.InitializeAsync();
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Content = "public class TestClass { }",
                Context = new Dictionary<string, object> { ["requestType"] = "analyze" }
            };

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _agent.HandleAsync(request, cts.Token));
        }

        [Fact]
        public async Task HandleAsync_ShouldIncludeMetadata()
        {
            // Arrange
            await _agent.InitializeAsync();
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Content = "public class TestClass { }",
                Context = new Dictionary<string, object> { ["requestType"] = "analyze" }
            };

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Metadata.Should().NotBeNull();
            result.Metadata.Should().ContainKey("analysisType");
            result.Metadata.Should().ContainKey("timestamp");
        }

        [Fact]
        public async Task HandleAsync_ShouldSetProcessingTime()
        {
            // Arrange
            await _agent.InitializeAsync();
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Content = "public class TestClass { }",
                Context = new Dictionary<string, object> { ["requestType"] = "analyze" }
            };

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Fact]
        public async Task ShutdownAsync_ShouldCompleteSuccessfully()
        {
            // Arrange
            await _agent.InitializeAsync();

            // Act
            await _agent.ShutdownAsync();

            // Assert
            // Verify that shutdown completed without throwing
            _agent.Should().NotBeNull();
        }

        [Fact]
        public void Dispose_ShouldNotThrow()
        {
            // Act & Assert
            _agent.Invoking(a => a.Dispose()).Should().NotThrow();
        }

        [Theory]
        [InlineData("using System; public class Test { }")]
        [InlineData("namespace MyNamespace { public class Test { } }")]
        [InlineData("public void Method() { }")]
        public async Task CanHandleAsync_WithCSharpCode_ShouldReturnTrue(string code)
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Content = code,
                Context = new Dictionary<string, object> { ["requestType"] = "analyze" }
            };

            // Act
            var result = await _agent.CanHandleAsync(request);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("SELECT * FROM Users")]
        [InlineData("function test() { return true; }")]
        [InlineData("def test(): return True")]
        public async Task CanHandleAsync_WithNonCSharpCode_ShouldReturnFalse(string code)
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Content = code,
                Context = new Dictionary<string, object> { ["requestType"] = "unsupported" }
            };

            // Act
            var result = await _agent.CanHandleAsync(request);

            // Assert
            result.Should().BeFalse();
        }
    }
}