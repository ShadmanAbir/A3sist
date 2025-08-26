using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using A3sist.UI.Shared;
using A3sist.Shared.Models;
using A3sist.Shared.Interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace A3sist.UI.Tests.Shared
{
    /// <summary>
    /// Tests for ChatViewModel to ensure RAG-enhanced chat functionality works correctly
    /// </summary>
    public class ChatViewModelTests
    {
        private readonly Mock<ILogger<ChatViewModel>> _loggerMock;
        private readonly Mock<IRequestRouter> _requestRouterMock;
        private readonly Mock<IRAGService> _ragServiceMock;
        private readonly ChatViewModel _chatViewModel;

        public ChatViewModelTests()
        {
            _loggerMock = new Mock<ILogger<ChatViewModel>>();
            _requestRouterMock = new Mock<IRequestRouter>();
            _ragServiceMock = new Mock<IRAGService>();
            
            _chatViewModel = new ChatViewModel(
                _loggerMock.Object,
                _requestRouterMock.Object,
                _ragServiceMock.Object);
        }

        [Fact]
        public void ChatViewModel_Initialization_ShouldSetCorrectDefaults()
        {
            // Assert
            _chatViewModel.Messages.Should().NotBeNull();
            _chatViewModel.Messages.Should().BeEmpty();
            _chatViewModel.CurrentMessage.Should().BeEmpty();
            _chatViewModel.IsProcessing.Should().BeFalse();
            _chatViewModel.SendCommand.Should().NotBeNull();
            _chatViewModel.ClearCommand.Should().NotBeNull();
        }

        [Fact]
        public async Task SendMessageAsync_WithValidMessage_ShouldAddMessageAndGetResponse()
        {
            // Arrange
            var userMessage = "How do I implement dependency injection?";
            var expectedResponse = new AgentResponse
            {
                Success = true,
                Content = "To implement dependency injection, you need to...",
                Citations = new[] { "DI Guide", "Best Practices" }
            };

            _requestRouterMock
                .Setup(r => r.ProcessRequestAsync(It.IsAny<AgentRequest>()))
                .ReturnsAsync(expectedResponse);

            _ragServiceMock
                .Setup(r => r.GetCitationsAsync(It.IsAny<IEnumerable<KnowledgeItem>>()))
                .ReturnsAsync(new[] { "[1] DI Guide", "[2] Best Practices" });

            _chatViewModel.CurrentMessage = userMessage;

            // Act
            await _chatViewModel.SendMessageAsync();

            // Assert
            _chatViewModel.Messages.Should().HaveCount(2); // User message + AI response
            _chatViewModel.Messages.First().Content.Should().Be(userMessage);
            _chatViewModel.Messages.First().IsUser.Should().BeTrue();
            
            _chatViewModel.Messages.Last().Content.Should().Be(expectedResponse.Content);
            _chatViewModel.Messages.Last().IsUser.Should().BeFalse();
            _chatViewModel.Messages.Last().Citations.Should().NotBeEmpty();
            
            _chatViewModel.CurrentMessage.Should().BeEmpty();
            _chatViewModel.IsProcessing.Should().BeFalse();
        }

        [Fact]
        public async Task SendMessageAsync_WithEmptyMessage_ShouldNotSendRequest()
        {
            // Arrange
            _chatViewModel.CurrentMessage = "";

            // Act
            await _chatViewModel.SendMessageAsync();

            // Assert
            _chatViewModel.Messages.Should().BeEmpty();
            _requestRouterMock.Verify(r => r.ProcessRequestAsync(It.IsAny<AgentRequest>()), Times.Never);
        }

        [Fact]
        public async Task SendMessageAsync_WithWhitespaceMessage_ShouldNotSendRequest()
        {
            // Arrange
            _chatViewModel.CurrentMessage = "   \t\n  ";

            // Act
            await _chatViewModel.SendMessageAsync();

            // Assert
            _chatViewModel.Messages.Should().BeEmpty();
            _requestRouterMock.Verify(r => r.ProcessRequestAsync(It.IsAny<AgentRequest>()), Times.Never);
        }

        [Fact]
        public async Task SendMessageAsync_WhenRequestFails_ShouldShowErrorMessage()
        {
            // Arrange
            var userMessage = "Test message";
            var errorResponse = new AgentResponse
            {
                Success = false,
                ErrorMessage = "Service temporarily unavailable"
            };

            _requestRouterMock
                .Setup(r => r.ProcessRequestAsync(It.IsAny<AgentRequest>()))
                .ReturnsAsync(errorResponse);

            _chatViewModel.CurrentMessage = userMessage;

            // Act
            await _chatViewModel.SendMessageAsync();

            // Assert
            _chatViewModel.Messages.Should().HaveCount(2);
            _chatViewModel.Messages.Last().Content.Should().Contain("error").Or.Contain("unavailable");
            _chatViewModel.Messages.Last().IsUser.Should().BeFalse();
        }

        [Fact]
        public async Task SendMessageAsync_WithException_ShouldHandleGracefully()
        {
            // Arrange
            var userMessage = "Test message";
            
            _requestRouterMock
                .Setup(r => r.ProcessRequestAsync(It.IsAny<AgentRequest>()))
                .ThrowsAsync(new System.Exception("Network error"));

            _chatViewModel.CurrentMessage = userMessage;

            // Act
            await _chatViewModel.SendMessageAsync();

            // Assert
            _chatViewModel.Messages.Should().HaveCount(2);
            _chatViewModel.Messages.Last().Content.Should().Contain("error");
            _chatViewModel.Messages.Last().IsUser.Should().BeFalse();
            _chatViewModel.IsProcessing.Should().BeFalse();
        }

        [Fact]
        public void ClearMessages_ShouldRemoveAllMessages()
        {
            // Arrange
            _chatViewModel.Messages.Add(new ChatMessage { Content = "Test 1", IsUser = true });
            _chatViewModel.Messages.Add(new ChatMessage { Content = "Test 2", IsUser = false });

            // Act
            _chatViewModel.ClearMessages();

            // Assert
            _chatViewModel.Messages.Should().BeEmpty();
        }

        [Fact]
        public void CurrentMessage_PropertyChange_ShouldNotifySubscribers()
        {
            // Arrange
            var propertyChangedFired = false;
            _chatViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ChatViewModel.CurrentMessage))
                    propertyChangedFired = true;
            };

            // Act
            _chatViewModel.CurrentMessage = "New message";

            // Assert
            propertyChangedFired.Should().BeTrue();
            _chatViewModel.CurrentMessage.Should().Be("New message");
        }

        [Fact]
        public void IsProcessing_PropertyChange_ShouldNotifySubscribers()
        {
            // Arrange
            var propertyChangedFired = false;
            _chatViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ChatViewModel.IsProcessing))
                    propertyChangedFired = true;
            };

            // Act
            _chatViewModel.IsProcessing = true;

            // Assert
            propertyChangedFired.Should().BeTrue();
            _chatViewModel.IsProcessing.Should().BeTrue();
        }

        [Fact]
        public void SendCommand_CanExecute_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            _chatViewModel.CurrentMessage = "";
            var canExecuteEmpty = _chatViewModel.SendCommand.CanExecute(null);

            _chatViewModel.CurrentMessage = "Valid message";
            var canExecuteValid = _chatViewModel.SendCommand.CanExecute(null);

            _chatViewModel.IsProcessing = true;
            var canExecuteProcessing = _chatViewModel.SendCommand.CanExecute(null);

            // Assert
            canExecuteEmpty.Should().BeFalse();
            canExecuteValid.Should().BeTrue();
            canExecuteProcessing.Should().BeFalse();
        }

        [Fact]
        public void ClearCommand_CanExecute_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            var canExecuteEmpty = _chatViewModel.ClearCommand.CanExecute(null);

            _chatViewModel.Messages.Add(new ChatMessage { Content = "Test", IsUser = true });
            var canExecuteWithMessages = _chatViewModel.ClearCommand.CanExecute(null);

            // Assert
            canExecuteEmpty.Should().BeFalse();
            canExecuteWithMessages.Should().BeTrue();
        }

        [Fact]
        public async Task SendMessageAsync_WithLongMessage_ShouldHandleCorrectly()
        {
            // Arrange
            var longMessage = new string('a', 5000); // 5000 character message
            var expectedResponse = new AgentResponse
            {
                Success = true,
                Content = "Response to long message"
            };

            _requestRouterMock
                .Setup(r => r.ProcessRequestAsync(It.IsAny<AgentRequest>()))
                .ReturnsAsync(expectedResponse);

            _chatViewModel.CurrentMessage = longMessage;

            // Act
            await _chatViewModel.SendMessageAsync();

            // Assert
            _chatViewModel.Messages.Should().HaveCount(2);
            _chatViewModel.Messages.First().Content.Should().Be(longMessage);
            _chatViewModel.Messages.Last().Content.Should().Be(expectedResponse.Content);
        }

        [Fact]
        public async Task SendMessageAsync_WithCodeSnippet_ShouldProcessAsCodeAnalysis()
        {
            // Arrange
            var codeMessage = @"Please analyze this code:
            ```csharp
            public void TestMethod()
            {
                Console.WriteLine(""Hello"");
            }
            ```";

            var expectedResponse = new AgentResponse
            {
                Success = true,
                Content = "Code analysis results..."
            };

            _requestRouterMock
                .Setup(r => r.ProcessRequestAsync(It.Is<AgentRequest>(req => 
                    req.Type == RequestType.CodeAnalysis)))
                .ReturnsAsync(expectedResponse);

            _chatViewModel.CurrentMessage = codeMessage;

            // Act
            await _chatViewModel.SendMessageAsync();

            // Assert
            _requestRouterMock.Verify(r => r.ProcessRequestAsync(It.Is<AgentRequest>(req => 
                req.Type == RequestType.CodeAnalysis)), Times.Once);
        }

        [Fact]
        public async Task ParallelSendRequests_ShouldHandleCorrectly()
        {
            // Arrange
            _requestRouterMock
                .Setup(r => r.ProcessRequestAsync(It.IsAny<AgentRequest>()))
                .ReturnsAsync(new AgentResponse { Success = true, Content = "Response" });

            // Act
            _chatViewModel.CurrentMessage = "Message 1";
            var task1 = _chatViewModel.SendMessageAsync();

            _chatViewModel.CurrentMessage = "Message 2";
            var task2 = _chatViewModel.SendMessageAsync();

            await Task.WhenAll(task1, task2);

            // Assert
            _chatViewModel.Messages.Should().HaveCountGreaterOrEqualTo(2);
            _chatViewModel.IsProcessing.Should().BeFalse();
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Act & Assert - Should not throw
            var action = () => _chatViewModel.Dispose();
            action.Should().NotThrow();
        }
    }
}