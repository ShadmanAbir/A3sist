using A3sist.Core.Services;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
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
    /// <summary>
    /// Tests for the enhanced Orchestrator functionality
    /// </summary>
    public class OrchestratorEnhancedTests : IDisposable
    {
        private readonly Mock<IAgentManager> _mockAgentManager;
        private readonly Mock<ITaskQueueService> _mockTaskQueueService;
        private readonly Mock<IWorkflowService> _mockWorkflowService;
        private readonly Mock<ILogger<Orchestrator>> _mockLogger;
        private readonly Mock<IAgentConfiguration> _mockConfiguration;
        private readonly Orchestrator _orchestrator;

        public OrchestratorEnhancedTests()
        {
            _mockAgentManager = new Mock<IAgentManager>();
            _mockTaskQueueService = new Mock<ITaskQueueService>();
            _mockWorkflowService = new Mock<IWorkflowService>();
            _mockLogger = new Mock<ILogger<Orchestrator>>();
            _mockConfiguration = new Mock<IAgentConfiguration>();
            
            // Setup default configuration values
            var defaultConfig = new AgentConfiguration
            {
                Name = "Orchestrator",
                RetryPolicy = new RetryPolicy
                {
                    MaxRetries = 3,
                    InitialDelay = TimeSpan.FromMilliseconds(1000)
                }
            };
            _mockConfiguration.Setup(x => x.GetAgentConfigurationAsync("Orchestrator"))
                .ReturnsAsync(defaultConfig);
            
            _orchestrator = new Orchestrator(
                _mockAgentManager.Object, 
                _mockTaskQueueService.Object,
                _mockWorkflowService.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);
        }

        [Fact]
        public void Constructor_WithValidDependencies_ShouldCreateInstance()
        {
            // Act & Assert
            Assert.NotNull(_orchestrator);
        }

        [Fact]
        public void Constructor_WithNullAgentManager_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Orchestrator(
                null!, 
                _mockTaskQueueService.Object, 
                _mockWorkflowService.Object, 
                _mockLogger.Object, 
                _mockConfiguration.Object));
        }

        [Fact]
        public void Constructor_WithNullTaskQueueService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Orchestrator(
                _mockAgentManager.Object, 
                null!, 
                _mockWorkflowService.Object, 
                _mockLogger.Object, 
                _mockConfiguration.Object));
        }

        [Fact]
        public void Constructor_WithNullWorkflowService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Orchestrator(
                _mockAgentManager.Object, 
                _mockTaskQueueService.Object, 
                null!, 
                _mockLogger.Object, 
                _mockConfiguration.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Orchestrator(
                _mockAgentManager.Object, 
                _mockTaskQueueService.Object, 
                _mockWorkflowService.Object, 
                null!, 
                _mockConfiguration.Object));
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Orchestrator(
                _mockAgentManager.Object, 
                _mockTaskQueueService.Object, 
                _mockWorkflowService.Object, 
                _mockLogger.Object, 
                null!));
        }

        [Fact]
        public async Task ProcessRequestAsync_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _orchestrator.ProcessRequestAsync(null!));
        }

        [Fact]
        public async Task ProcessRequestAsync_WithWorkflowContext_ShouldUseWorkflowService()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Context = new Dictionary<string, object> { ["UseWorkflow"] = true };
            
            var workflowResult = new WorkflowResult
            {
                Success = true,
                Result = AgentResult.CreateSuccess("Workflow completed")
            };

            _mockAgentManager.Setup(x => x.StartAllAgentsAsync()).Returns(Task.CompletedTask);
            _mockWorkflowService.Setup(x => x.ExecuteWorkflowAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(workflowResult);

            // Act
            var result = await _orchestrator.ProcessRequestAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Workflow completed", result.Message);
            _mockWorkflowService.Verify(x => x.ExecuteWorkflowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_ShouldCallAgentManagerStartAllAgents()
        {
            // Arrange
            _mockAgentManager.Setup(x => x.StartAllAgentsAsync()).Returns(Task.CompletedTask);

            // Act
            await _orchestrator.InitializeAsync();

            // Assert
            _mockAgentManager.Verify(x => x.StartAllAgentsAsync(), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenCalledMultipleTimes_ShouldOnlyInitializeOnce()
        {
            // Arrange
            _mockAgentManager.Setup(x => x.StartAllAgentsAsync()).Returns(Task.CompletedTask);

            // Act
            await _orchestrator.InitializeAsync();
            await _orchestrator.InitializeAsync();
            await _orchestrator.InitializeAsync();

            // Assert
            _mockAgentManager.Verify(x => x.StartAllAgentsAsync(), Times.Once);
        }

        [Fact]
        public async Task ShutdownAsync_ShouldCallAgentManagerStopAllAgents()
        {
            // Arrange
            _mockAgentManager.Setup(x => x.StartAllAgentsAsync()).Returns(Task.CompletedTask);
            _mockAgentManager.Setup(x => x.StopAllAgentsAsync()).Returns(Task.CompletedTask);

            await _orchestrator.InitializeAsync();

            // Act
            await _orchestrator.ShutdownAsync();

            // Assert
            _mockAgentManager.Verify(x => x.StopAllAgentsAsync(), Times.Once);
        }

        private static AgentRequest CreateValidRequest()
        {
            return new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "Test prompt",
                FilePath = "test.cs",
                Content = "test content",
                Context = new Dictionary<string, object>(),
                PreferredAgentType = AgentType.Unknown,
                CreatedAt = DateTime.UtcNow,
                UserId = "test-user"
            };
        }

        public void Dispose()
        {
            _orchestrator?.Dispose();
        }
    }
}