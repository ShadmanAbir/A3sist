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

namespace A3sist.Integration.Tests
{
    /// <summary>
    /// Integration tests for task queue and workflow management coordination
    /// </summary>
    public class TaskQueueWorkflowIntegrationTests : IDisposable
    {
        private readonly Mock<ILogger<TaskQueueService>> _mockTaskQueueLogger;
        private readonly Mock<ILogger<WorkflowService>> _mockWorkflowLogger;
        private readonly Mock<ILogger<Orchestrator>> _mockOrchestratorLogger;
        private readonly Mock<IAgentManager> _mockAgentManager;
        private readonly Mock<IAgentConfiguration> _mockConfiguration;
        private readonly TaskQueueService _taskQueueService;
        private readonly WorkflowService _workflowService;
        private readonly Orchestrator _orchestrator;

        public TaskQueueWorkflowIntegrationTests()
        {
            _mockTaskQueueLogger = new Mock<ILogger<TaskQueueService>>();
            _mockWorkflowLogger = new Mock<ILogger<WorkflowService>>();
            _mockOrchestratorLogger = new Mock<ILogger<Orchestrator>>();
            _mockAgentManager = new Mock<IAgentManager>();
            _mockConfiguration = new Mock<IAgentConfiguration>();

            _taskQueueService = new TaskQueueService(_mockTaskQueueLogger.Object);
            _workflowService = new WorkflowService(_mockWorkflowLogger.Object);
            
            // Setup mock configuration
            _mockConfiguration.Setup(x => x.GetAgentConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync(new AgentConfiguration
                {
                    Name = "TestAgent",
                    Enabled = true,
                    RetryPolicy = new RetryPolicy
                    {
                        MaxRetries = 3,
                        InitialDelay = TimeSpan.FromMilliseconds(100)
                    }
                });

            _orchestrator = new Orchestrator(
                _mockAgentManager.Object,
                _taskQueueService,
                _workflowService,
                _mockOrchestratorLogger.Object,
                _mockConfiguration.Object);
        }

        [Fact]
        public async Task TaskQueueAndWorkflow_ShouldProcessRequestsInPriorityOrder()
        {
            // Arrange
            var processedRequests = new List<AgentRequest>();
            var mockAgent = CreateMockAgent("TestAgent", AgentType.CSharp);
            var mockWorkflowStep = CreateMockWorkflowStep("TestStep", 1);

            // Setup agent manager to return our mock agent
            _mockAgentManager.Setup(x => x.GetAgentsAsync())
                .ReturnsAsync(new[] { mockAgent.Object });
            _mockAgentManager.Setup(x => x.GetAgentAsync(It.IsAny<AgentType>()))
                .ReturnsAsync(mockAgent.Object);

            // Setup agent to track processed requests
            mockAgent.Setup(x => x.HandleAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()))
                .Returns<AgentRequest, CancellationToken>((req, ct) =>
                {
                    processedRequests.Add(req);
                    return Task.FromResult(AgentResult.CreateSuccess($"Processed {req.Id}"));
                });

            await _workflowService.RegisterWorkflowStepAsync(mockWorkflowStep.Object);

            // Create requests with different priorities
            var lowPriorityRequest = CreateRequest("Low priority request");
            var normalPriorityRequest = CreateRequest("Normal priority request");
            var highPriorityRequest = CreateRequest("High priority request");
            var criticalPriorityRequest = CreateRequest("Critical priority request");

            // Act - Enqueue in mixed order
            await _taskQueueService.EnqueueAsync(lowPriorityRequest, TaskPriority.Low);
            await _taskQueueService.EnqueueAsync(criticalPriorityRequest, TaskPriority.Critical);
            await _taskQueueService.EnqueueAsync(normalPriorityRequest, TaskPriority.Normal);
            await _taskQueueService.EnqueueAsync(highPriorityRequest, TaskPriority.High);

            // Process all requests
            var results = new List<AgentRequest?>();
            for (int i = 0; i < 4; i++)
            {
                var request = await _taskQueueService.DequeueAsync();
                if (request != null)
                {
                    results.Add(request);
                    // Simulate processing by orchestrator
                    await _orchestrator.ProcessRequestAsync(request);
                }
            }

            // Assert - Should be processed in priority order (Critical, High, Normal, Low)
            Assert.Equal(4, results.Count);
            Assert.Equal(criticalPriorityRequest.Id, results[0]!.Id);
            Assert.Equal(highPriorityRequest.Id, results[1]!.Id);
            Assert.Equal(normalPriorityRequest.Id, results[2]!.Id);
            Assert.Equal(lowPriorityRequest.Id, results[3]!.Id);
        }

        [Fact]
        public async Task WorkflowService_ShouldCoordinateMultipleAgents()
        {
            // Arrange
            var executionOrder = new List<string>();
            var mockAgent1 = CreateMockAgent("Agent1", AgentType.CSharp);
            var mockAgent2 = CreateMockAgent("Agent2", AgentType.JavaScript);

            var mockStep1 = CreateMockWorkflowStep("Step1", 1);
            var mockStep2 = CreateMockWorkflowStep("Step2", 2);

            // Setup steps to track execution order
            mockStep1.Setup(x => x.ExecuteAsync(It.IsAny<AgentRequest>(), It.IsAny<WorkflowContext>(), It.IsAny<CancellationToken>()))
                .Returns<AgentRequest, WorkflowContext, CancellationToken>((req, ctx, ct) =>
                {
                    executionOrder.Add("Step1");
                    return Task.FromResult(new WorkflowStepResult
                    {
                        StepName = "Step1",
                        Success = true,
                        Result = AgentResult.CreateSuccess("Step1 completed")
                    });
                });

            mockStep2.Setup(x => x.ExecuteAsync(It.IsAny<AgentRequest>(), It.IsAny<WorkflowContext>(), It.IsAny<CancellationToken>()))
                .Returns<AgentRequest, WorkflowContext, CancellationToken>((req, ctx, ct) =>
                {
                    executionOrder.Add("Step2");
                    // Verify that Step1 result is available in context
                    Assert.Single(ctx.PreviousResults);
                    Assert.Equal("Step1", ctx.PreviousResults[0].StepName);
                    return Task.FromResult(new WorkflowStepResult
                    {
                        StepName = "Step2",
                        Success = true,
                        Result = AgentResult.CreateSuccess("Step2 completed")
                    });
                });

            await _workflowService.RegisterWorkflowStepAsync(mockStep1.Object);
            await _workflowService.RegisterWorkflowStepAsync(mockStep2.Object);

            var request = CreateRequest("Multi-step workflow request");

            // Act
            var result = await _workflowService.ExecuteWorkflowAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, result.StepResults.Count);
            Assert.Equal(2, executionOrder.Count);
            Assert.Equal("Step1", executionOrder[0]);
            Assert.Equal("Step2", executionOrder[1]);
            Assert.All(result.StepResults, stepResult => Assert.True(stepResult.Success));
        }

        [Fact]
        public async Task TaskQueueStatistics_ShouldTrackProcessingMetrics()
        {
            // Arrange
            var requests = Enumerable.Range(0, 10)
                .Select(i => CreateRequest($"Request {i}"))
                .ToList();

            // Act - Enqueue requests with different priorities
            for (int i = 0; i < requests.Count; i++)
            {
                var priority = (TaskPriority)(i % 4); // Cycle through priorities
                await _taskQueueService.EnqueueAsync(requests[i], priority);
            }

            // Dequeue some requests
            for (int i = 0; i < 5; i++)
            {
                await _taskQueueService.DequeueAsync();
            }

            // Assert
            var statistics = await _taskQueueService.GetStatisticsAsync();
            Assert.Equal(5, statistics.TotalItems); // 10 enqueued - 5 dequeued
            Assert.True(statistics.ItemsByPriority.Values.Sum() == 5);
            Assert.True(statistics.LastUpdated <= DateTime.UtcNow);
        }

        [Fact]
        public async Task ConcurrentTaskProcessing_ShouldHandleMultipleRequestsSimultaneously()
        {
            // Arrange
            const int requestCount = 20;
            var processedRequests = new List<AgentRequest>();
            var lockObject = new object();

            var mockAgent = CreateMockAgent("ConcurrentAgent", AgentType.CSharp);
            mockAgent.Setup(x => x.HandleAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()))
                .Returns<AgentRequest, CancellationToken>(async (req, ct) =>
                {
                    // Simulate some processing time
                    await Task.Delay(50, ct);
                    
                    lock (lockObject)
                    {
                        processedRequests.Add(req);
                    }
                    
                    return AgentResult.CreateSuccess($"Processed {req.Id}");
                });

            _mockAgentManager.Setup(x => x.GetAgentsAsync())
                .ReturnsAsync(new[] { mockAgent.Object });

            // Create and enqueue requests
            var requests = Enumerable.Range(0, requestCount)
                .Select(i => CreateRequest($"Concurrent request {i}"))
                .ToList();

            var enqueueTasks = requests.Select(req => 
                _taskQueueService.EnqueueAsync(req, TaskPriority.Normal));

            // Act - Enqueue all requests concurrently
            await Task.WhenAll(enqueueTasks);

            // Process all requests concurrently
            var processingTasks = Enumerable.Range(0, requestCount)
                .Select(async i =>
                {
                    var request = await _taskQueueService.DequeueAsync();
                    if (request != null)
                    {
                        return await _orchestrator.ProcessRequestAsync(request);
                    }
                    return null;
                })
                .Where(task => task != null)
                .ToList();

            var results = await Task.WhenAll(processingTasks);

            // Assert
            Assert.Equal(requestCount, results.Length);
            Assert.All(results, result => Assert.True(result?.Success == true));
            Assert.Equal(requestCount, processedRequests.Count);
            Assert.Equal(0, await _taskQueueService.GetQueueSizeAsync());
        }

        [Fact]
        public async Task WorkflowFailureHandling_ShouldStopOnFailedStep()
        {
            // Arrange
            var executionOrder = new List<string>();
            var mockStep1 = CreateMockWorkflowStep("SuccessStep", 1);
            var mockStep2 = CreateMockWorkflowStep("FailureStep", 2);
            var mockStep3 = CreateMockWorkflowStep("ShouldNotExecute", 3);

            mockStep1.Setup(x => x.ExecuteAsync(It.IsAny<AgentRequest>(), It.IsAny<WorkflowContext>(), It.IsAny<CancellationToken>()))
                .Returns<AgentRequest, WorkflowContext, CancellationToken>((req, ctx, ct) =>
                {
                    executionOrder.Add("SuccessStep");
                    return Task.FromResult(new WorkflowStepResult
                    {
                        StepName = "SuccessStep",
                        Success = true,
                        Result = AgentResult.CreateSuccess("Step1 completed")
                    });
                });

            mockStep2.Setup(x => x.ExecuteAsync(It.IsAny<AgentRequest>(), It.IsAny<WorkflowContext>(), It.IsAny<CancellationToken>()))
                .Returns<AgentRequest, WorkflowContext, CancellationToken>((req, ctx, ct) =>
                {
                    executionOrder.Add("FailureStep");
                    return Task.FromResult(new WorkflowStepResult
                    {
                        StepName = "FailureStep",
                        Success = false,
                        Result = AgentResult.CreateFailure("Step2 failed intentionally")
                    });
                });

            mockStep3.Setup(x => x.ExecuteAsync(It.IsAny<AgentRequest>(), It.IsAny<WorkflowContext>(), It.IsAny<CancellationToken>()))
                .Returns<AgentRequest, WorkflowContext, CancellationToken>((req, ctx, ct) =>
                {
                    executionOrder.Add("ShouldNotExecute");
                    return Task.FromResult(new WorkflowStepResult
                    {
                        StepName = "ShouldNotExecute",
                        Success = true,
                        Result = AgentResult.CreateSuccess("Should not reach here")
                    });
                });

            await _workflowService.RegisterWorkflowStepAsync(mockStep1.Object);
            await _workflowService.RegisterWorkflowStepAsync(mockStep2.Object);
            await _workflowService.RegisterWorkflowStepAsync(mockStep3.Object);

            var request = CreateRequest("Workflow with failure");

            // Act
            var result = await _workflowService.ExecuteWorkflowAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(2, result.StepResults.Count); // Only first two steps should execute
            Assert.Equal(2, executionOrder.Count);
            Assert.Equal("SuccessStep", executionOrder[0]);
            Assert.Equal("FailureStep", executionOrder[1]);
            Assert.True(result.StepResults[0].Success);
            Assert.False(result.StepResults[1].Success);
            Assert.Contains("Step2 failed intentionally", result.Result.Message);
        }

        [Fact]
        public async Task TaskQueueEvents_ShouldBeRaisedCorrectly()
        {
            // Arrange
            var enqueuedEvents = new List<TaskEnqueuedEventArgs>();
            var dequeuedEvents = new List<TaskDequeuedEventArgs>();

            _taskQueueService.TaskEnqueued += (sender, args) => enqueuedEvents.Add(args);
            _taskQueueService.TaskDequeued += (sender, args) => dequeuedEvents.Add(args);

            var request1 = CreateRequest("Event test request 1");
            var request2 = CreateRequest("Event test request 2");

            // Act
            await _taskQueueService.EnqueueAsync(request1, TaskPriority.High);
            await _taskQueueService.EnqueueAsync(request2, TaskPriority.Normal);

            var dequeued1 = await _taskQueueService.DequeueAsync();
            var dequeued2 = await _taskQueueService.DequeueAsync();

            // Assert
            Assert.Equal(2, enqueuedEvents.Count);
            Assert.Equal(2, dequeuedEvents.Count);

            // Check enqueued events
            Assert.Equal(request1.Id, enqueuedEvents[0].Request.Id);
            Assert.Equal(TaskPriority.High, enqueuedEvents[0].Priority);
            Assert.Equal(request2.Id, enqueuedEvents[1].Request.Id);
            Assert.Equal(TaskPriority.Normal, enqueuedEvents[1].Priority);

            // Check dequeued events (should be in priority order)
            Assert.Equal(request1.Id, dequeuedEvents[0].Request.Id);
            Assert.Equal(TaskPriority.High, dequeuedEvents[0].Priority);
            Assert.Equal(request2.Id, dequeuedEvents[1].Request.Id);
            Assert.Equal(TaskPriority.Normal, dequeuedEvents[1].Priority);

            // Check that wait times are reasonable
            Assert.True(dequeuedEvents[0].WaitTime >= TimeSpan.Zero);
            Assert.True(dequeuedEvents[1].WaitTime >= TimeSpan.Zero);
        }

        private Mock<IAgent> CreateMockAgent(string name, AgentType type)
        {
            var mockAgent = new Mock<IAgent>();
            mockAgent.Setup(x => x.Name).Returns(name);
            mockAgent.Setup(x => x.Type).Returns(type);
            mockAgent.Setup(x => x.CanHandleAsync(It.IsAny<AgentRequest>())).ReturnsAsync(true);
            mockAgent.Setup(x => x.HandleAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AgentResult.CreateSuccess($"Processed by {name}"));
            mockAgent.Setup(x => x.InitializeAsync()).Returns(Task.CompletedTask);
            mockAgent.Setup(x => x.ShutdownAsync()).Returns(Task.CompletedTask);

            return mockAgent;
        }

        private Mock<IWorkflowStep> CreateMockWorkflowStep(string name, int order)
        {
            var mockStep = new Mock<IWorkflowStep>();
            mockStep.Setup(x => x.Name).Returns(name);
            mockStep.Setup(x => x.Order).Returns(order);
            mockStep.Setup(x => x.CanHandleAsync(It.IsAny<AgentRequest>())).ReturnsAsync(true);
            mockStep.Setup(x => x.ExecuteAsync(It.IsAny<AgentRequest>(), It.IsAny<WorkflowContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WorkflowStepResult
                {
                    StepName = name,
                    Success = true,
                    Result = AgentResult.CreateSuccess($"{name} completed")
                });

            return mockStep;
        }

        private static AgentRequest CreateRequest(string prompt)
        {
            return new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = prompt,
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
            _taskQueueService?.Dispose();
            _workflowService?.Dispose();
            _orchestrator?.Dispose();
        }
    }
}