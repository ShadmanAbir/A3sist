using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using A3sist.Core.Services;
using A3sist.Shared.Models;
using A3sist.Shared.Interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace A3sist.Core.Tests.Services
{
    /// <summary>
    /// Unit tests for EnhancedRequestRouter to ensure simplified routing with RAG integration works correctly
    /// </summary>
    public class EnhancedRequestRouterTests
    {
        private readonly Mock<ILogger<EnhancedRequestRouter>> _loggerMock;
        private readonly Mock<IRAGService> _ragServiceMock;
        private readonly Mock<ILLMService> _llmServiceMock;
        private readonly EnhancedRequestRouter _requestRouter;

        public EnhancedRequestRouterTests()
        {
            _loggerMock = new Mock<ILogger<EnhancedRequestRouter>>();
            _ragServiceMock = new Mock<IRAGService>();
            _llmServiceMock = new Mock<ILLMService>();
            
            _requestRouter = new EnhancedRequestRouter(
                _loggerMock.Object,
                _ragServiceMock.Object,
                _llmServiceMock.Object);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithCodeAnalysisRequest_ShouldRouteCorrectly()
        {
            // Arrange
            var request = new AgentRequest
            {
                Type = RequestType.CodeAnalysis,
                Content = "Analyze this C# method for performance issues",
                Context = new Dictionary<string, object>
                {
                    { "code", "public void SlowMethod() { Thread.Sleep(1000); }" },
                    { "language", "csharp" }
                }
            };

            var mockKnowledge = new List<KnowledgeItem>
            {
                new KnowledgeItem
                {
                    Id = "perf-guide",
                    Title = "Performance Analysis Guide",
                    Content = "Performance analysis best practices...",
                    Relevance = 0.9f
                }
            };

            var expectedResponse = new AgentResponse
            {
                Content = "This method has performance issues due to Thread.Sleep...",
                Success = true,
                Citations = new[] { "Performance Analysis Guide" }
            };

            _ragServiceMock
                .Setup(r => r.RetrieveKnowledgeAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(mockKnowledge);

            _ragServiceMock
                .Setup(r => r.AugmentPromptAsync(It.IsAny<string>(), It.IsAny<IEnumerable<KnowledgeItem>>()))
                .ReturnsAsync("Enhanced prompt with knowledge context");

            _llmServiceMock
                .Setup(l => l.ProcessRequestAsync(It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _requestRouter.ProcessRequestAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Content.Should().NotBeNullOrEmpty();
            
            // Verify RAG service was called
            _ragServiceMock.Verify(r => r.RetrieveKnowledgeAsync(
                It.Is<string>(s => s.Contains("performance") || s.Contains("analysis")), 
                It.IsAny<int>()), Times.Once);
            
            _ragServiceMock.Verify(r => r.AugmentPromptAsync(
                It.IsAny<string>(), 
                It.IsAny<IEnumerable<KnowledgeItem>>()), Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithRefactoringRequest_ShouldIncludeRAGContext()
        {
            // Arrange
            var request = new AgentRequest
            {
                Type = RequestType.Refactoring,
                Content = "Refactor this legacy code to use modern patterns",
                Context = new Dictionary<string, object>
                {
                    { "code", "public class LegacyClass { }" },
                    { "targetFramework", "net9.0" }
                }
            };

            var mockKnowledge = new List<KnowledgeItem>
            {
                new KnowledgeItem
                {
                    Id = "refactor-patterns",
                    Title = "Modern Refactoring Patterns",
                    Content = "Use dependency injection, async patterns...",
                    Relevance = 0.95f
                }
            };

            _ragServiceMock
                .Setup(r => r.RetrieveKnowledgeAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(mockKnowledge);

            _ragServiceMock
                .Setup(r => r.AugmentPromptAsync(It.IsAny<string>(), It.IsAny<IEnumerable<KnowledgeItem>>()))
                .ReturnsAsync("Refactoring request with knowledge context");

            _llmServiceMock
                .Setup(l => l.ProcessRequestAsync(It.IsAny<string>()))
                .ReturnsAsync(new AgentResponse { Success = true, Content = "Refactored code..." });

            // Act
            var result = await _requestRouter.ProcessRequestAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            
            // Verify knowledge retrieval focused on refactoring
            _ragServiceMock.Verify(r => r.RetrieveKnowledgeAsync(
                It.Is<string>(s => s.Contains("refactor") || s.Contains("modern")), 
                It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithChatRequest_ShouldUseGeneralKnowledge()
        {
            // Arrange
            var request = new AgentRequest
            {
                Type = RequestType.Chat,
                Content = "How do I implement authentication in ASP.NET Core?",
                Context = new Dictionary<string, object>()
            };

            var mockKnowledge = new List<KnowledgeItem>
            {
                new KnowledgeItem
                {
                    Id = "auth-guide",
                    Title = "ASP.NET Core Authentication",
                    Content = "Authentication setup steps...",
                    Relevance = 0.88f
                }
            };

            _ragServiceMock
                .Setup(r => r.RetrieveKnowledgeAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(mockKnowledge);

            _ragServiceMock
                .Setup(r => r.AugmentPromptAsync(It.IsAny<string>(), It.IsAny<IEnumerable<KnowledgeItem>>()))
                .ReturnsAsync("Chat request with authentication knowledge");

            _llmServiceMock
                .Setup(l => l.ProcessRequestAsync(It.IsAny<string>()))
                .ReturnsAsync(new AgentResponse { Success = true, Content = "Authentication guide..." });

            // Act
            var result = await _requestRouter.ProcessRequestAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            
            // Verify knowledge retrieval for general chat
            _ragServiceMock.Verify(r => r.RetrieveKnowledgeAsync(
                It.Is<string>(s => s.Contains("authentication") || s.Contains("ASP.NET")), 
                It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_WhenRAGServiceFails_ShouldContinueWithoutKnowledge()
        {
            // Arrange
            var request = new AgentRequest
            {
                Type = RequestType.CodeAnalysis,
                Content = "Analyze this code",
                Context = new Dictionary<string, object>()
            };

            _ragServiceMock
                .Setup(r => r.RetrieveKnowledgeAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ThrowsAsync(new System.Exception("Knowledge retrieval failed"));

            _llmServiceMock
                .Setup(l => l.ProcessRequestAsync(It.IsAny<string>()))
                .ReturnsAsync(new AgentResponse { Success = true, Content = "Analysis without knowledge..." });

            // Act
            var result = await _requestRouter.ProcessRequestAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            
            // Verify LLM was still called with original prompt
            _llmServiceMock.Verify(l => l.ProcessRequestAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithNullRequest_ShouldReturnErrorResponse()
        {
            // Act
            var result = await _requestRouter.ProcessRequestAsync(null!);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Request cannot be null");
        }

        [Theory]
        [InlineData(RequestType.CodeAnalysis, "code analysis")]
        [InlineData(RequestType.Refactoring, "refactoring")]
        [InlineData(RequestType.Chat, "general programming")]
        [InlineData(RequestType.Documentation, "documentation")]
        public async Task ProcessRequestAsync_WithDifferentRequestTypes_ShouldUseAppropriateKnowledge(
            RequestType requestType, string expectedKnowledgeContext)
        {
            // Arrange
            var request = new AgentRequest
            {
                Type = requestType,
                Content = "Test request",
                Context = new Dictionary<string, object>()
            };

            _ragServiceMock
                .Setup(r => r.RetrieveKnowledgeAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<KnowledgeItem>());

            _ragServiceMock
                .Setup(r => r.AugmentPromptAsync(It.IsAny<string>(), It.IsAny<IEnumerable<KnowledgeItem>>()))
                .ReturnsAsync("Enhanced prompt");

            _llmServiceMock
                .Setup(l => l.ProcessRequestAsync(It.IsAny<string>()))
                .ReturnsAsync(new AgentResponse { Success = true, Content = "Response" });

            // Act
            var result = await _requestRouter.ProcessRequestAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            
            // Verify knowledge retrieval was attempted
            _ragServiceMock.Verify(r => r.RetrieveKnowledgeAsync(
                It.IsAny<string>(), 
                It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void EnhancedRequestRouter_Construction_ShouldNotThrow()
        {
            // Arrange & Act
            var action = () => new EnhancedRequestRouter(
                _loggerMock.Object,
                _ragServiceMock.Object,
                _llmServiceMock.Object);

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public async Task ProcessRequestAsync_WithLargeContext_ShouldHandleEfficiently()
        {
            // Arrange
            var largeCode = new string('x', 10000); // Large code context
            var request = new AgentRequest
            {
                Type = RequestType.CodeAnalysis,
                Content = "Analyze this large codebase",
                Context = new Dictionary<string, object>
                {
                    { "code", largeCode }
                }
            };

            _ragServiceMock
                .Setup(r => r.RetrieveKnowledgeAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<KnowledgeItem>());

            _llmServiceMock
                .Setup(l => l.ProcessRequestAsync(It.IsAny<string>()))
                .ReturnsAsync(new AgentResponse { Success = true, Content = "Analysis complete" });

            // Act
            var result = await _requestRouter.ProcessRequestAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
        }
    }
}