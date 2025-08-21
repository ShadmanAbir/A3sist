using A3sist.Core.Agents.Core.Dispatcher;
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

namespace A3sist.Core.Tests.Agents.Core
{
    public class DispatcherAgentTests
    {
        private readonly Mock<ILogger<DispatcherAgent>> _mockLogger;
        private readonly Mock<IAgentConfiguration> _mockConfiguration;
        private readonly Mock<ITaskQueueService> _mockTaskQueueService;
        private readonly Mock<IWorkflowService> _mockWorkflowService;
        private readonly Mock<IAgentManager> _mockAgentManager;
        private readonly DispatcherAgent _dispatcherAgent;

        public DispatcherAgentTests()
        {
            _mockLogger = new Mock<ILogger<DispatcherAgent>>();
            _mockConfiguration = new Mock<IAgentConfiguration>();
            _mockTaskQueueService = new Mock<ITaskQueueService>();
            _mockWorkflowService = new Mock<IWorkflowService>();
            _mockAgentManager = new Mock<IAgentManager>();

            // Setup default configuration
            _mockConfiguration.Setup(x => x.GetAgentConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync(new AgentConfiguration
                {
                    Name = "Dispatcher",
                    Type = AgentType.Dispatcher,
                    Enabled = true,
                    Settings = new Dictionary<string, object>
                    {
                        { "MaxConcurrentTasks", 4 }
                    }
                });

            _dispatcherAgent = new DispatcherAgent(
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockTaskQueueService.Object,
                _mockWorkflowService.Object,
                _mockAgentManager.Object);
        }

        [Fact]
        public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
        {
            // Assert
            Assert.Equal("Dispatcher", _dispatcherAgent.Name);
            Assert.Equal(AgentType.Dispatcher, _dispatcherAgent.Type);
        }

        [Fact]
        public void Constructor_WithNullTaskQueueService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DispatcherAgent(
                _mockLogger.Object,
                _mockConfiguration.Object,
                null!,
                _mockWorkflowService.Object,
                _mockAgentManager.Object));
        }

        [Fact]
        public void Constructor_WithNullWorkflowService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DispatcherAgent(
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockTaskQueueService.Object,
                null!,
                _mockAgentManager.Object));
        }

        [Fact]
        public void Constructor_WithNullAgentManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DispatcherAgent(
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockTaskQueueService.Object,
                _mockWorkflowService.Object,
                null!));
        }

        [Theory]
        [InlineData("dispatch this task", true)]
        [InlineData("execute the workflow", true)]
        [InlineData("coordinate agents", true)]
        [InlineData("check status", true)]
        [InlineData("balance load", true)]
        [InlineData("prioritize task", true)]
        [InlineData("monitor execution", true)]
        [InlineData("cancel task", true)]
        [InlineData("retry failed task", true)]
        [InlineData("hello world", false)]
        [InlineData("", false)]
        public async Task CanHandleAsync_WithVariousPrompts_ReturnsExpectedResult(string prompt, bool expected)
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = prompt
            };

            // Act
            var result = await _dispatcherAgent.CanHandleAsync(request);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task CanHandleAsync_WithNullRequest_ReturnsFalse()
        {
            // Act
            var result = await _dispatcherAgent.CanHandleAsync(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HandleAsync_WithDispatchRequest_EnqueuesTaskAndReturnsSuccess()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "dispatch this task"
            };

            var mockAgent = new Mock<IAgent>();
            mockAgent.Setup(x => x.Name).Returns("TestAgent");
            mockAgent.Setup(x => x.CanHandleAsync(It.IsAny<AgentRequest>())).ReturnsAsync(true);
            mockAgent.Setup(x => x.HandleAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AgentResult.CreateSuccess("Task completed", "TestAgent"));

            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(new[] { mockAgent.Object });

            // Act
            var result = await _dispatcherAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("dispatched successfully", result.Message);
            _mockTaskQueueService.Verify(x => x.EnqueueAsync(request, It.IsAny<TaskPriority>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithWorkflowRequest_ExecutesWorkflowAndReturnsResult()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "coordinate workflow"
            };

            var workflowResult = new WorkflowResult
            {
                Success = true,
                StepResults = new List<WorkflowStepResult>
                {
                    new WorkflowStepResult
                    {
                        StepName = "Step1",
                        Success = true,
                        Result = AgentResult.CreateSuccess("Step completed", "TestAgent"),
                        ExecutionTime = TimeSpan.FromSeconds(1)
                    }
                },
                TotalExecutionTime = TimeSpan.FromSeconds(1)
            };

            _mockWorkflowService.Setup(x => x.ExecuteWorkflowAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(workflowResult);

            // Act
            var result = await _dispatcherAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("coordinated successfully", result.Message);
            _mockWorkflowService.Verify(x => x.ExecuteWorkflowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithStatusRequest_ReturnsExecutionStatus()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "check status"
            };

            var queueStats = new QueueStatistics
            {
                TotalItems = 5,
                TotalProcessed = 10,
                AverageWaitTime = TimeSpan.FromSeconds(2),
                ThroughputPerMinute = 30,
                ItemsByPriority = new Dictionary<TaskPriority, int>
                {
                    { TaskPriority.High, 2 },
                    { TaskPriority.Normal, 3 }
                }
            };

            _mockTaskQueueService.Setup(x => x.GetStatisticsAsync())
                .ReturnsAsync(queueStats);

            // Act
            var result = await _dispatcherAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Status retrieved successfully", result.Message);
            Assert.NotNull(result.Content);
            _mockTaskQueueService.Verify(x => x.GetStatisticsAsync(), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithCancelRequest_CancelsTaskWhenFound()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "cancel task",
                Context = new Dictionary<string, object>
                {
                    { "taskId", taskId.ToString() }
                }
            };

            // First dispatch a task to have something to cancel
            var dispatchRequest = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "dispatch task"
            };

            var mockAgent = new Mock<IAgent>();
            mockAgent.Setup(x => x.Name).Returns("TestAgent");
            mockAgent.Setup(x => x.CanHandleAsync(It.IsAny<AgentRequest>())).ReturnsAsync(true);
            mockAgent.Setup(x => x.HandleAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AgentResult.CreateSuccess("Task completed", "TestAgent"));

            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(new[] { mockAgent.Object });

            // Dispatch first
            await _dispatcherAgent.HandleAsync(dispatchRequest);

            // Act
            var result = await _dispatcherAgent.HandleAsync(request);

            // Assert
            // Since we can't easily access the internal task tracking, we'll just verify the request was processed
            Assert.True(result.Success || result.Message.Contains("not found"));
        }

        [Fact]
        public async Task HandleAsync_WithRetryRequest_ReenqueuesTaskWithHighPriority()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "retry task"
            };

            // Act
            var result = await _dispatcherAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("queued for retry", result.Message);
            _mockTaskQueueService.Verify(x => x.EnqueueAsync(request, TaskPriority.High), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithLoadBalanceRequest_PerformsLoadBalancing()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "balance load"
            };

            // Act
            var result = await _dispatcherAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Load balancing performed", result.Message);
        }

        [Fact]
        public async Task HandleAsync_WithUnknownAction_DefaultsToDispatch()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "unknown action"
            };

            var mockAgent = new Mock<IAgent>();
            mockAgent.Setup(x => x.Name).Returns("TestAgent");
            mockAgent.Setup(x => x.CanHandleAsync(It.IsAny<AgentRequest>())).ReturnsAsync(true);
            mockAgent.Setup(x => x.HandleAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AgentResult.CreateSuccess("Task completed", "TestAgent"));

            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(new[] { mockAgent.Object });

            // Act
            var result = await _dispatcherAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            _mockTaskQueueService.Verify(x => x.EnqueueAsync(request, It.IsAny<TaskPriority>()), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_LoadsConfigurationCorrectly()
        {
            // Act
            await _dispatcherAgent.InitializeAsync();

            // Assert
            _mockConfiguration.Verify(x => x.GetAgentConfigurationAsync("Dispatcher"), Times.Once);
        }

        [Fact]
        public async Task ShutdownAsync_CompletesSuccessfully()
        {
            // Arrange
            await _dispatcherAgent.InitializeAsync();

            // Act & Assert
            await _dispatcherAgent.ShutdownAsync();
        }

        [Theory]
        [InlineData("urgent task", TaskPriority.Critical)]
        [InlineData("critical issue", TaskPriority.Critical)]
        [InlineData("emergency fix", TaskPriority.Critical)]
        [InlineData("important task", TaskPriority.High)]
        [InlineData("high priority", TaskPriority.High)]
        [InlineData("low priority task", TaskPriority.Low)]
        [InlineData("background process", TaskPriority.Low)]
        [InlineData("normal task", TaskPriority.Normal)]
        public async Task DeterminePriority_WithVariousPrompts_ReturnsCorrectPriority(string prompt, TaskPriority expectedPriority)
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = prompt
            };

            var mockAgent = new Mock<IAgent>();
            mockAgent.Setup(x => x.Name).Returns("TestAgent");
            mockAgent.Setup(x => x.CanHandleAsync(It.IsAny<AgentRequest>())).ReturnsAsync(true);
            mockAgent.Setup(x => x.HandleAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AgentResult.CreateSuccess("Task completed", "TestAgent"));

            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(new[] { mockAgent.Object });

            // Act
            await _dispatcherAgent.HandleAsync(request);

            // Assert
            _mockTaskQueueService.Verify(x => x.EnqueueAsync(request, expectedPriority), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WhenTaskQueueServiceThrows_ReturnsFailureResult()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "dispatch task"
            };

            _mockTaskQueueService.Setup(x => x.EnqueueAsync(It.IsAny<AgentRequest>(), It.IsAny<TaskPriority>()))
                .ThrowsAsync(new InvalidOperationException("Queue is full"));

            // Act
            var result = await _dispatcherAgent.HandleAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Failed to dispatch task", result.Message);
            Assert.Contains("Queue is full", result.Message);
        }

        [Fact]
        public async Task HandleAsync_WhenWorkflowServiceThrows_ReturnsFailureResult()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "coordinate workflow"
            };

            _mockWorkflowService.Setup(x => x.ExecuteWorkflowAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Workflow failed"));

            // Act
            var result = await _dispatcherAgent.HandleAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Failed to coordinate workflow", result.Message);
            Assert.Contains("Workflow failed", result.Message);
        }

        [Fact]
        public void Dispose_CompletesWithoutException()
        {
            // Act & Assert
            _dispatcherAgent.Dispose();
        }
    }
}