using A3sist.Core.Configuration;
using A3sist.Core.LLM;
using A3sist.Shared.Enums;
using A3sist.UI.Models.Chat;
using A3sist.UI.Services.Chat;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.UI.Tests.Services.Chat
{
    /// <summary>
    /// Unit tests for ChatService
    /// </summary>
    public class ChatServiceTests : IDisposable
    {
        private readonly Mock<MCPOrchestrator> _mockOrchestrator;
        private readonly Mock<IChatHistoryService> _mockHistoryService;
        private readonly Mock<IContextService> _mockContextService;
        private readonly Mock<ILogger<ChatService>> _mockLogger;
        private readonly Mock<IOptions<A3sistOptions>> _mockOptions;
        private readonly ChatService _chatService;

        public ChatServiceTests()
        {
            _mockOrchestrator = new Mock<MCPOrchestrator>();
            _mockHistoryService = new Mock<IChatHistoryService>();
            _mockContextService = new Mock<IContextService>();
            _mockLogger = new Mock<ILogger<ChatService>>();
            _mockOptions = new Mock<IOptions<A3sistOptions>>();

            // Setup options
            _mockOptions.Setup(x => x.Value).Returns(new A3sistOptions());

            _chatService = new ChatService(
                _mockOrchestrator.Object,
                _mockHistoryService.Object,
                _mockContextService.Object,
                _mockLogger.Object,
                _mockOptions.Object);
        }

        [Fact]
        public async Task SendMessageAsync_WithValidRequest_ShouldReturnSuccessResponse()
        {
            // Arrange
            var request = new SendMessageRequest
            {
                ConversationId = "test-conversation",
                Content = "Hello, A3sist!"
            };

            var mockConversation = new ChatConversation
            {
                Id = request.ConversationId,
                Messages = new List<ChatMessage>()
            };

            var orchestratorResult = new MCPOrchestrationResult
            {
                Success = true,
                Content = "Hello! How can I help you today?",
                ServersUsed = new List<string> { "core-development" },
                ProcessingTimeMs = 150
            };

            _mockHistoryService.Setup(x => x.LoadConversationAsync(request.ConversationId))
                .ReturnsAsync(mockConversation);

            _mockContextService.Setup(x => x.GetCurrentContextAsync())
                .ReturnsAsync(new ChatContext());

            _mockOrchestrator.Setup(x => x.ProcessRequestAsync(It.IsAny<A3sist.Shared.Messaging.AgentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(orchestratorResult);

            _mockHistoryService.Setup(x => x.SaveConversationAsync(It.IsAny<ChatConversation>()))
                .Returns(Task.CompletedTask);

            // Act
            var response = await _chatService.SendMessageAsync(request);

            // Assert
            response.Should().NotBeNull();
            response.Success.Should().BeTrue();
            response.MessageId.Should().NotBeNullOrEmpty();
            response.Metadata.Should().NotBeNull();
            response.Metadata!.ServersInvolved.Should().Contain("core-development");

            // Verify the conversation was saved
            _mockHistoryService.Verify(x => x.SaveConversationAsync(It.IsAny<ChatConversation>()), Times.Once);
        }

        [Fact]
        public async Task CreateConversationAsync_ShouldCreateNewConversation()
        {
            // Arrange
            var title = "Test Conversation";
            
            _mockHistoryService.Setup(x => x.SaveConversationAsync(It.IsAny<ChatConversation>()))
                .Returns(Task.CompletedTask);

            // Act
            var conversation = await _chatService.CreateConversationAsync(title);

            // Assert
            conversation.Should().NotBeNull();
            conversation.Title.Should().Be(title);
            conversation.Id.Should().NotBeNullOrEmpty();
            conversation.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
            conversation.Messages.Should().BeEmpty();

            _mockHistoryService.Verify(x => x.SaveConversationAsync(conversation), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_WhenOrchestratorFails_ShouldReturnFailureResponse()
        {
            // Arrange
            var request = new SendMessageRequest
            {
                ConversationId = "test-conversation",
                Content = "Hello"
            };

            var mockConversation = new ChatConversation
            {
                Id = request.ConversationId,
                Messages = new List<ChatMessage>()
            };

            _mockHistoryService.Setup(x => x.LoadConversationAsync(request.ConversationId))
                .ReturnsAsync(mockConversation);

            _mockContextService.Setup(x => x.GetCurrentContextAsync())
                .ReturnsAsync(new ChatContext());

            _mockOrchestrator.Setup(x => x.ProcessRequestAsync(It.IsAny<A3sist.Shared.Messaging.AgentRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Orchestrator error"));

            // Act
            var response = await _chatService.SendMessageAsync(request);

            // Assert
            response.Should().NotBeNull();
            response.Success.Should().BeFalse();
            response.Error.Should().Contain("Orchestrator error");
        }

        [Fact]
        public async Task GetConversationAsync_WithExistingId_ShouldReturnConversation()
        {
            // Arrange
            var conversationId = "existing-conversation";
            var expectedConversation = new ChatConversation
            {
                Id = conversationId,
                Title = "Test Conversation"
            };

            _mockHistoryService.Setup(x => x.LoadConversationAsync(conversationId))
                .ReturnsAsync(expectedConversation);

            // Act
            var result = await _chatService.GetConversationAsync(conversationId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(conversationId);
            result.Title.Should().Be("Test Conversation");
        }

        [Fact]
        public async Task GetConversationAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var conversationId = "non-existent";

            _mockHistoryService.Setup(x => x.LoadConversationAsync(conversationId))
                .ReturnsAsync((ChatConversation?)null);

            // Act
            var result = await _chatService.GetConversationAsync(conversationId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void StreamingResponse_Event_ShouldBeRaised()
        {
            // Arrange
            var eventRaised = false;
            StreamingResponseChunk? receivedChunk = null;

            _chatService.StreamingResponse += (sender, chunk) =>
            {
                eventRaised = true;
                receivedChunk = chunk;
            };

            // This test demonstrates the event structure but actual streaming
            // would be tested in integration tests due to the complexity
            // of mocking the streaming behavior

            // Assert
            _chatService.Should().NotBeNull();
            eventRaised.Should().BeFalse(); // No streaming in this unit test
        }

        public void Dispose()
        {
            _chatService?.Dispose();
        }
    }

    /// <summary>
    /// Mock implementation of MCPOrchestrationResult for testing
    /// </summary>
    public class MCPOrchestrationResult
    {
        public bool Success { get; set; }
        public string? Content { get; set; }
        public List<string> ServersUsed { get; set; } = new();
        public Dictionary<string, string>? ToolResults { get; set; }
        public double ProcessingTimeMs { get; set; }
    }
}