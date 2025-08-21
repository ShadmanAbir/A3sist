using A3sist.Core.Agents.Utility.Knowledge;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using A3sist.Shared.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Core.Tests.Agents.Utility
{
    public class KnowledgeAgentTests
    {
        private readonly Mock<ILogger<KnowledgeAgent>> _mockLogger;
        private readonly Mock<IAgentConfiguration> _mockConfiguration;
        private readonly Mock<IKnowledgeRepository> _mockRepository;
        private readonly Mock<IDocumentationSearchService> _mockSearchService;
        private readonly Mock<IContextAnalyzer> _mockContextAnalyzer;
        private readonly KnowledgeAgent _agent;

        public KnowledgeAgentTests()
        {
            _mockLogger = new Mock<ILogger<KnowledgeAgent>>();
            _mockConfiguration = new Mock<IAgentConfiguration>();
            _mockRepository = new Mock<IKnowledgeRepository>();
            _mockSearchService = new Mock<IDocumentationSearchService>();
            _mockContextAnalyzer = new Mock<IContextAnalyzer>();

            _agent = new KnowledgeAgent(
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockRepository.Object,
                _mockSearchService.Object,
                _mockContextAnalyzer.Object);
        }

        [Fact]
        public void Constructor_WithValidParameters_SetsProperties()
        {
            // Assert
            Assert.Equal("KnowledgeAgent", _agent.Name);
            Assert.Equal(AgentType.Utility, _agent.Type);
        }

        [Fact]
        public void Constructor_WithNullRepository_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new KnowledgeAgent(
                _mockLogger.Object,
                _mockConfiguration.Object,
                null,
                _mockSearchService.Object,
                _mockContextAnalyzer.Object));
        }

        [Theory]
        [InlineData("help me with this", true)]
        [InlineData("show documentation", true)]
        [InlineData("what is dependency injection", true)]
        [InlineData("how to implement interface", true)]
        [InlineData("example of async method", true)]
        [InlineData("refactor this code", false)]
        [InlineData("compile the project", false)]
        [InlineData("", false)]
        public async Task CanHandleAsync_WithVariousPrompts_ReturnsExpectedResult(string prompt, bool expected)
        {
            // Arrange
            var request = new AgentRequest { Prompt = prompt };

            // Act
            var result = await _agent.CanHandleAsync(request);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task CanHandleAsync_WithNullRequest_ReturnsFalse()
        {
            // Act
            var result = await _agent.CanHandleAsync(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HandleAsync_WithValidRequest_ReturnsSuccessfulResult()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "help me with dependency injection",
                UserId = "test-user"
            };

            var context = new KnowledgeContext
            {
                Language = "C#",
                ProjectType = "Console"
            };

            var knowledgeEntries = new List<KnowledgeEntry>
            {
                new KnowledgeEntry
                {
                    Title = "Dependency Injection Basics",
                    Content = "Dependency injection is a design pattern...",
                    Type = KnowledgeEntryType.Documentation,
                    Summary = "Introduction to dependency injection"
                }
            };

            _mockContextAnalyzer
                .Setup(x => x.AnalyzeContextAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            _mockRepository
                .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<KnowledgeContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(knowledgeEntries);

            _mockSearchService
                .Setup(x => x.SearchDocumentationAsync(It.IsAny<string>(), It.IsAny<KnowledgeContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<KnowledgeEntry>());

            _mockRepository
                .Setup(x => x.RecordInteractionAsync(It.IsAny<KnowledgeInteraction>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Knowledge retrieved successfully", result.Message);
            Assert.Equal("KnowledgeAgent", result.AgentName);
            Assert.NotNull(result.Content);
            Assert.Contains("Documentation", result.Content);
            Assert.Contains("Dependency Injection Basics", result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithNoSearchResults_ReturnsHelpfulMessage()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "unknown topic xyz123"
            };

            var context = new KnowledgeContext();

            _mockContextAnalyzer
                .Setup(x => x.AnalyzeContextAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            _mockRepository
                .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<KnowledgeContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<KnowledgeEntry>());

            _mockSearchService
                .Setup(x => x.SearchDocumentationAsync(It.IsAny<string>(), It.IsAny<KnowledgeContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<KnowledgeEntry>());

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("couldn't find specific information", result.Content);
            Assert.Contains("try again", result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithDirectAnswer_IncludesAnswerInResponse()
        {
            // Arrange
            var request = new AgentRequest
            {
                Prompt = "what is SOLID principles"
            };

            var context = new KnowledgeContext();
            var knowledgeEntries = new List<KnowledgeEntry>
            {
                new KnowledgeEntry
                {
                    Title = "SOLID Principles",
                    Content = "SOLID is an acronym for five design principles...",
                    Type = KnowledgeEntryType.DirectAnswer
                }
            };

            _mockContextAnalyzer
                .Setup(x => x.AnalyzeContextAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            _mockRepository
                .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<KnowledgeContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(knowledgeEntries);

            _mockSearchService
                .Setup(x => x.SearchDocumentationAsync(It.IsAny<string>(), It.IsAny<KnowledgeContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<KnowledgeEntry>());

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("**Answer:**", result.Content);
            Assert.Contains("SOLID is an acronym", result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithCodeExamples_IncludesCodeBlocks()
        {
            // Arrange
            var request = new AgentRequest
            {
                Prompt = "show me dependency injection example"
            };

            var context = new KnowledgeContext();
            var knowledgeEntries = new List<KnowledgeEntry>
            {
                new KnowledgeEntry
                {
                    Title = "DI Example",
                    Content = "public class Service { }",
                    Type = KnowledgeEntryType.CodeExample,
                    Language = "csharp"
                }
            };

            _mockContextAnalyzer
                .Setup(x => x.AnalyzeContextAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            _mockRepository
                .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<KnowledgeContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(knowledgeEntries);

            _mockSearchService
                .Setup(x => x.SearchDocumentationAsync(It.IsAny<string>(), It.IsAny<KnowledgeContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<KnowledgeEntry>());

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("**Examples:**", result.Content);
            Assert.Contains("```csharp", result.Content);
            Assert.Contains("public class Service", result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithException_ReturnsFailureResult()
        {
            // Arrange
            var request = new AgentRequest
            {
                Prompt = "test query"
            };

            _mockContextAnalyzer
                .Setup(x => x.AnalyzeContextAsync(request, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Failed to process knowledge request", result.Message);
            Assert.NotNull(result.Exception);
            Assert.Equal("KnowledgeAgent", result.AgentName);
        }

        [Fact]
        public async Task InitializeAsync_CallsRepositoryInitialize()
        {
            // Act
            await _agent.InitializeAsync();

            // Assert
            _mockRepository.Verify(x => x.InitializeAsync(), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_RecordsInteraction()
        {
            // Arrange
            var request = new AgentRequest
            {
                Prompt = "test query",
                UserId = "test-user"
            };

            var context = new KnowledgeContext();

            _mockContextAnalyzer
                .Setup(x => x.AnalyzeContextAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            _mockRepository
                .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<KnowledgeContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<KnowledgeEntry>());

            _mockSearchService
                .Setup(x => x.SearchDocumentationAsync(It.IsAny<string>(), It.IsAny<KnowledgeContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<KnowledgeEntry>());

            // Act
            await _agent.HandleAsync(request);

            // Assert
            _mockRepository.Verify(
                x => x.RecordInteractionAsync(
                    It.Is<KnowledgeInteraction>(i => 
                        i.Query == request.Prompt && 
                        i.UserId == request.UserId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithBroaderSearch_PerformsAdditionalSearch()
        {
            // Arrange
            var request = new AgentRequest
            {
                Prompt = "complex technical query"
            };

            var context = new KnowledgeContext();

            _mockContextAnalyzer
                .Setup(x => x.AnalyzeContextAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            // First search returns no results
            _mockRepository
                .SetupSequence(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<KnowledgeContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<KnowledgeEntry>())
                .ReturnsAsync(new List<KnowledgeEntry>
                {
                    new KnowledgeEntry { Title = "Broader result", Content = "Content" }
                });

            _mockSearchService
                .Setup(x => x.SearchDocumentationAsync(It.IsAny<string>(), It.IsAny<KnowledgeContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<KnowledgeEntry>());

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            _mockRepository.Verify(
                x => x.SearchAsync(It.IsAny<string>(), It.IsAny<KnowledgeContext>(), It.IsAny<CancellationToken>()),
                Times.AtLeast(2));
        }
    }
}