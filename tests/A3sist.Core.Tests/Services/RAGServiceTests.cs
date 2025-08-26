using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using A3sist.Core.Services;
using A3sist.Shared.Models;
using A3sist.Shared.Messaging;
using A3sist.Shared.Interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Caching.Memory;
using static A3sist.Core.Services.RAGService;

namespace A3sist.Core.Tests.Services
{
    /// <summary>
    /// Unit tests for RAGService to ensure knowledge retrieval and augmentation works correctly
    /// </summary>
    public class RAGServiceTests
    {
        private readonly Mock<ILogger<RAGService>> _loggerMock;
        private readonly Mock<IKnowledgeRepository> _knowledgeRepositoryMock;
        private readonly HttpClient _httpClient;
        private readonly RAGService _ragService;

        public RAGServiceTests()
        {
            _loggerMock = new Mock<ILogger<RAGService>>();
            _knowledgeRepositoryMock = new Mock<IKnowledgeRepository>();
            _httpClient = new HttpClient();
            _ragService = new RAGService(_httpClient, _knowledgeRepositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task RetrieveContextAsync_WithValidRequest_ShouldReturnRAGContext()
        {
            // Arrange
            var request = new AgentRequest("How to implement dependency injection");
            var expectedKnowledge = new List<KnowledgeEntry>
            {
                new KnowledgeEntry
                {
                    Title = "Dependency Injection Basics",
                    Content = "Dependency injection is a design pattern...",
                    Source = "docs",
                    Relevance = 0.95f
                }
            };

            _knowledgeRepositoryMock
                .Setup(k => k.SearchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(expectedKnowledge);

            // Act
            var result = await _ragService.RetrieveContextAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.KnowledgeEntries.Should().NotBeEmpty();
        }

        [Fact]
        public async Task RetrieveContextAsync_WithEmptyRequest_ShouldReturnEmptyContext()
        {
            // Arrange
            var request = new AgentRequest("");

            // Act
            var result = await _ragService.RetrieveContextAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.KnowledgeEntries.Should().BeEmpty();
        }

        [Fact]
        public void AugmentPrompt_WithKnowledgeContext_ShouldEnhancePrompt()
        {
            // Arrange
            var originalPrompt = "How do I implement authentication?";
            var context = new RAGContext
            {
                KnowledgeEntries = new List<KnowledgeEntry>
                {
                    new KnowledgeEntry
                    {
                        Title = "Authentication Guide",
                        Content = "To implement authentication, follow these steps...",
                        Source = "security-docs",
                        Relevance = 0.9f
                    }
                }
            };

            // Act
            var augmentedPrompt = _ragService.AugmentPrompt(originalPrompt, context);

            // Assert
            augmentedPrompt.Should().NotBeNull();
            augmentedPrompt.Should().NotBe(originalPrompt);
            augmentedPrompt.Should().Contain("Enhanced Request with Retrieved Knowledge");
            augmentedPrompt.Should().Contain("Authentication Guide");
            augmentedPrompt.Should().Contain(originalPrompt);
        }

        [Fact]
        public void AugmentPrompt_WithNoKnowledge_ShouldReturnOriginalPrompt()
        {
            // Arrange
            var originalPrompt = "How do I implement authentication?";
            var emptyContext = new RAGContext { KnowledgeEntries = new List<KnowledgeEntry>() };

            // Act
            var augmentedPrompt = _ragService.AugmentPrompt(originalPrompt, emptyContext);

            // Assert
            augmentedPrompt.Should().Be(originalPrompt);
        }

        [Fact]
        public void RAGService_Construction_ShouldNotThrow()
        {
            // Arrange & Act
            var action = () => new RAGService(_httpClient, _knowledgeRepositoryMock.Object, _loggerMock.Object);

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Act & Assert - Should not throw
            var action = () => _ragService.Dispose();
            action.Should().NotThrow();
        }
    }
}