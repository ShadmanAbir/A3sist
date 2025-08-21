using A3sist.Core.Services;
using A3sist.Core.Services.WorkflowSteps;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Core.Tests.Services
{
    /// <summary>
    /// Basic tests for task queue and workflow integration
    /// </summary>
    public class TaskQueueWorkflowBasicTests : IDisposable
    {
        private readonly Mock<ILogger<TaskQueueService>> _mockTaskQueueLogger;
        private readonly Mock<ILogger<WorkflowService>> _mockWorkflowLogger;
        private readonly Mock<ILogger<ValidationWorkflowStep>> _mockValidationLogger;
        private readonly TaskQueueService _taskQueueService;
        private readonly WorkflowService _workflowService;
        private readonly ValidationWorkflowStep _validationStep;

        public TaskQueueWorkflowBasicTests()
        {
            _mockTaskQueueLogger = new Mock<ILogger<TaskQueueService>>();
            _mockWorkflowLogger = new Mock<ILogger<WorkflowService>>();
            _mockValidationLogger = new Mock<ILogger<ValidationWorkflowStep>>();
            
            _taskQueueService = new TaskQueueService(_mockTaskQueueLogger.Object);
            _workflowService = new WorkflowService(_mockWorkflowLogger.Object);
            _validationStep = new ValidationWorkflowStep(_mockValidationLogger.Object);
        }

        [Fact]
        public async Task TaskQueue_BasicEnqueueDequeue_ShouldWork()
        {
            // Arrange
            var request = CreateValidRequest();

            // Act
            await _taskQueueService.EnqueueAsync(request, TaskPriority.Normal);
            var dequeuedRequest = await _taskQueueService.DequeueAsync();

            // Assert
            Assert.NotNull(dequeuedRequest);
            Assert.Equal(request.Id, dequeuedRequest.Id);
            Assert.Equal(request.Prompt, dequeuedRequest.Prompt);
        }

        [Fact]
        public async Task WorkflowService_BasicStepRegistration_ShouldWork()
        {
            // Act
            await _workflowService.RegisterWorkflowStepAsync(_validationStep);
            var steps = await _workflowService.GetWorkflowStepsAsync();

            // Assert
            Assert.Single(steps);
            Assert.Contains(steps, s => s.Name == "Validation");
        }

        [Fact]
        public async Task ValidationWorkflowStep_ValidRequest_ShouldSucceed()
        {
            // Arrange
            var request = CreateValidRequest();
            var context = new WorkflowContext { Request = request };

            // Act
            var result = await _validationStep.ExecuteAsync(request, context, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Validation", result.StepName);
            Assert.True(context.Data.ContainsKey("ValidationTimestamp"));
        }

        [Fact]
        public async Task ValidationWorkflowStep_InvalidRequest_ShouldFail()
        {
            // Arrange
            var invalidRequest = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "", // Invalid: empty prompt
                UserId = "test-user",
                CreatedAt = DateTime.UtcNow
            };
            var context = new WorkflowContext { Request = invalidRequest };

            // Act
            var result = await _validationStep.ExecuteAsync(invalidRequest, context, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("prompt is required", result.Result.Message.ToLowerInvariant());
        }

        [Fact]
        public async Task WorkflowService_ExecuteWithValidationStep_ShouldWork()
        {
            // Arrange
            await _workflowService.RegisterWorkflowStepAsync(_validationStep);
            var request = CreateValidRequest();

            // Act
            var result = await _workflowService.ExecuteWorkflowAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Single(result.StepResults);
            Assert.Equal("Validation", result.StepResults[0].StepName);
            Assert.True(result.StepResults[0].Success);
        }

        [Fact]
        public async Task TaskQueueStatistics_ShouldTrackBasicMetrics()
        {
            // Arrange & Act
            await _taskQueueService.EnqueueAsync(CreateValidRequest(), TaskPriority.High);
            await _taskQueueService.EnqueueAsync(CreateValidRequest(), TaskPriority.Normal);
            
            var statistics = await _taskQueueService.GetStatisticsAsync();

            // Assert
            Assert.Equal(2, statistics.TotalItems);
            Assert.True(statistics.ItemsByPriority.ContainsKey(TaskPriority.High));
            Assert.True(statistics.ItemsByPriority.ContainsKey(TaskPriority.Normal));
            Assert.Equal(1, statistics.ItemsByPriority[TaskPriority.High]);
            Assert.Equal(1, statistics.ItemsByPriority[TaskPriority.Normal]);
        }

        [Fact]
        public async Task TaskQueue_PriorityOrdering_ShouldWork()
        {
            // Arrange
            var lowRequest = CreateValidRequest();
            var highRequest = CreateValidRequest();
            var criticalRequest = CreateValidRequest();

            // Act - Enqueue in mixed order
            await _taskQueueService.EnqueueAsync(lowRequest, TaskPriority.Low);
            await _taskQueueService.EnqueueAsync(criticalRequest, TaskPriority.Critical);
            await _taskQueueService.EnqueueAsync(highRequest, TaskPriority.High);

            // Dequeue all
            var first = await _taskQueueService.DequeueAsync();
            var second = await _taskQueueService.DequeueAsync();
            var third = await _taskQueueService.DequeueAsync();

            // Assert - Should be in priority order
            Assert.Equal(criticalRequest.Id, first!.Id);
            Assert.Equal(highRequest.Id, second!.Id);
            Assert.Equal(lowRequest.Id, third!.Id);
        }

        [Fact]
        public async Task TaskQueueEvents_ShouldBeRaised()
        {
            // Arrange
            var enqueuedEventRaised = false;
            var dequeuedEventRaised = false;

            _taskQueueService.TaskEnqueued += (sender, args) => enqueuedEventRaised = true;
            _taskQueueService.TaskDequeued += (sender, args) => dequeuedEventRaised = true;

            var request = CreateValidRequest();

            // Act
            await _taskQueueService.EnqueueAsync(request);
            await _taskQueueService.DequeueAsync();

            // Assert
            Assert.True(enqueuedEventRaised);
            Assert.True(dequeuedEventRaised);
        }

        [Fact]
        public async Task WorkflowService_WithMultipleSteps_ShouldExecuteInOrder()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            var mockStep1 = new Mock<IWorkflowStep>();
            mockStep1.Setup(x => x.Name).Returns("Step1");
            mockStep1.Setup(x => x.Order).Returns(1);
            mockStep1.Setup(x => x.CanHandleAsync(It.IsAny<AgentRequest>())).ReturnsAsync(true);
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

            var mockStep2 = new Mock<IWorkflowStep>();
            mockStep2.Setup(x => x.Name).Returns("Step2");
            mockStep2.Setup(x => x.Order).Returns(2);
            mockStep2.Setup(x => x.CanHandleAsync(It.IsAny<AgentRequest>())).ReturnsAsync(true);
            mockStep2.Setup(x => x.ExecuteAsync(It.IsAny<AgentRequest>(), It.IsAny<WorkflowContext>(), It.IsAny<CancellationToken>()))
                .Returns<AgentRequest, WorkflowContext, CancellationToken>((req, ctx, ct) =>
                {
                    executionOrder.Add("Step2");
                    return Task.FromResult(new WorkflowStepResult
                    {
                        StepName = "Step2",
                        Success = true,
                        Result = AgentResult.CreateSuccess("Step2 completed")
                    });
                });

            // Register steps in reverse order to test ordering
            await _workflowService.RegisterWorkflowStepAsync(mockStep2.Object);
            await _workflowService.RegisterWorkflowStepAsync(mockStep1.Object);

            var request = CreateValidRequest();

            // Act
            var result = await _workflowService.ExecuteWorkflowAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, result.StepResults.Count);
            Assert.Equal(2, executionOrder.Count);
            Assert.Equal("Step1", executionOrder[0]); // Should execute in order despite registration order
            Assert.Equal("Step2", executionOrder[1]);
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
            _taskQueueService?.Dispose();
            _workflowService?.Dispose();
        }
    }
}