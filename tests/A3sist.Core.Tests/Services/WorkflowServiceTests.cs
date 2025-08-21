using A3sist.Core.Services;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Core.Tests.Services
{
    public class WorkflowServiceTests : IDisposable
    {
        private readonly Mock<ILogger<WorkflowService>> _mockLogger;
        private readonly WorkflowService _workflowService;

        public WorkflowServiceTests()
        {
            _mockLogger = new Mock<ILogger<WorkflowService>>();
            _workflowService = new WorkflowService(_mockLogger.Object);
        }

        [Fact]
        public async Task RegisterWorkflowStepAsync_WithValidStep_ShouldRegisterSuccessfully()
        {
            // Arrange
            var mockStep = new Mock<IWorkflowStep>();
            mockStep.Setup(x => x.Name).Returns("TestStep");
            mockStep.Setup(x => x.Order).Returns(1);

            // Act
            await _workflowService.RegisterWorkflowStepAsync(mockStep.Object);

            // Assert
            var steps = await _workflowService.GetWorkflowStepsAsync();
            Assert.Single(steps);
            Assert.Equal("TestStep", steps.First().Name);
        }

        [Fact]
        public async Task RegisterWorkflowStepAsync_WithNullStep_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _workflowService.RegisterWorkflowStepAsync(null!));
        }

        [Fact]
        public async Task RegisterWorkflowStepAsync_WithEmptyName_ShouldThrowArgumentException()
        {
            // Arrange
            var mockStep = new Mock<IWorkflowStep>();
            mockStep.Setup(x => x.Name).Returns("");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _workflowService.RegisterWorkflowStepAsync(mockStep.Object));
        }

        [Fact]
        public async Task ExecuteWorkflowAsync_WithNoApplicableSteps_ShouldReturnFailure()
        {
            // Arrange
            var request = CreateValidRequest();
            var mockStep = new Mock<IWorkflowStep>();
            mockStep.Setup(x => x.Name).Returns("TestStep");
            mockStep.Setup(x => x.Order).Returns(1);
            mockStep.Setup(x => x.CanHandleAsync(request)).ReturnsAsync(false);

            await _workflowService.RegisterWorkflowStepAsync(mockStep.Object);

            // Act
            var result = await _workflowService.ExecuteWorkflowAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("No applicable workflow steps found", result.Result.Message);
        }

        [Fact]
        public async Task ExecuteWorkflowAsync_WithSuccessfulSteps_ShouldReturnSuccess()
        {
            // Arrange
            var request = CreateValidRequest();
            var workflowStarted = false;
            var workflowCompleted = false;

            _workflowService.WorkflowStarted += (sender, args) =>
            {
                workflowStarted = true;
                Assert.Equal(request.Id, args.Request.Id);
            };

            _workflowService.WorkflowCompleted += (sender, args) =>
            {
                workflowCompleted = true;
                Assert.Equal(request.Id, args.Request.Id);
                Assert.True(args.Result.Success);
            };

            var mockStep1 = CreateMockStep("Step1", 1, true, AgentResult.CreateSuccess("Step1 completed"));
            var mockStep2 = CreateMockStep("Step2", 2, true, AgentResult.CreateSuccess("Step2 completed"));

            await _workflowService.RegisterWorkflowStepAsync(mockStep1.Object);
            await _workflowService.RegisterWorkflowStepAsync(mockStep2.Object);

            // Act
            var result = await _workflowService.ExecuteWorkflowAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, result.StepResults.Count);
            Assert.All(result.StepResults, stepResult => Assert.True(stepResult.Success));
            Assert.True(workflowStarted);
            Assert.True(workflowCompleted);
        }

        [Fact]
        public async Task ExecuteWorkflowAsync_WithFailingStep_ShouldReturnFailure()
        {
            // Arrange
            var request = CreateValidRequest();
            var mockStep1 = CreateMockStep("Step1", 1, true, AgentResult.CreateSuccess("Step1 completed"));
            var mockStep2 = CreateMockStep("Step2", 2, true, AgentResult.CreateFailure("Step2 failed"));

            await _workflowService.RegisterWorkflowStepAsync(mockStep1.Object);
            await _workflowService.RegisterWorkflowStepAsync(mockStep2.Object);

            // Act
            var result = await _workflowService.ExecuteWorkflowAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(2, result.StepResults.Count);
            Assert.True(result.StepResults[0].Success);
            Assert.False(result.StepResults[1].Success);
            Assert.Contains("Step2 failed", result.Result.Message);
        }

        [Fact]
        public async Task ExecuteWorkflowAsync_WithStepsInWrongOrder_ShouldExecuteInCorrectOrder()
        {
            // Arrange
            var request = CreateValidRequest();
            var executionOrder = new System.Collections.Generic.List<string>();

            var mockStep3 = CreateMockStep("Step3", 3, true, AgentResult.CreateSuccess("Step3 completed"));
            var mockStep1 = CreateMockStep("Step1", 1, true, AgentResult.CreateSuccess("Step1 completed"));
            var mockStep2 = CreateMockStep("Step2", 2, true, AgentResult.CreateSuccess("Step2 completed"));

            // Modify the steps to track execution order
            mockStep1.Setup(x => x.ExecuteAsync(It.IsAny<AgentRequest>(), It.IsAny<WorkflowContext>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new WorkflowStepResult
                {
                    StepName = "Step1",
                    Success = true,
                    Result = AgentResult.CreateSuccess("Step1 completed")
                }))
                .Callback(() => executionOrder.Add("Step1"));

            mockStep2.Setup(x => x.ExecuteAsync(It.IsAny<AgentRequest>(), It.IsAny<WorkflowContext>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new WorkflowStepResult
                {
                    StepName = "Step2",
                    Success = true,
                    Result = AgentResult.CreateSuccess("Step2 completed")
                }))
                .Callback(() => executionOrder.Add("Step2"));

            mockStep3.Setup(x => x.ExecuteAsync(It.IsAny<AgentRequest>(), It.IsAny<WorkflowContext>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new WorkflowStepResult
                {
                    StepName = "Step3",
                    Success = true,
                    Result = AgentResult.CreateSuccess("Step3 completed")
                }))
                .Callback(() => executionOrder.Add("Step3"));

            // Register in wrong order
            await _workflowService.RegisterWorkflowStepAsync(mockStep3.Object);
            await _workflowService.RegisterWorkflowStepAsync(mockStep1.Object);
            await _workflowService.RegisterWorkflowStepAsync(mockStep2.Object);

            // Act
            var result = await _workflowService.ExecuteWorkflowAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(3, executionOrder.Count);
            Assert.Equal("Step1", executionOrder[0]);
            Assert.Equal("Step2", executionOrder[1]);
            Assert.Equal("Step3", executionOrder[2]);
        }

        [Fact]
        public async Task ExecuteWorkflowAsync_WithCancellation_ShouldReturnCancelledResult()
        {
            // Arrange
            var request = CreateValidRequest();
            var cts = new CancellationTokenSource();
            
            var mockStep = new Mock<IWorkflowStep>();
            mockStep.Setup(x => x.Name).Returns("SlowStep");
            mockStep.Setup(x => x.Order).Returns(1);
            mockStep.Setup(x => x.CanHandleAsync(request)).ReturnsAsync(true);
            mockStep.Setup(x => x.ExecuteAsync(It.IsAny<AgentRequest>(), It.IsAny<WorkflowContext>(), It.IsAny<CancellationToken>()))
                .Returns(async (AgentRequest req, WorkflowContext ctx, CancellationToken ct) =>
                {
                    await Task.Delay(1000, ct); // This will be cancelled
                    return new WorkflowStepResult
                    {
                        StepName = "SlowStep",
                        Success = true,
                        Result = AgentResult.CreateSuccess("Should not reach here")
                    };
                });

            await _workflowService.RegisterWorkflowStepAsync(mockStep.Object);

            // Act
            cts.CancelAfter(100); // Cancel after 100ms
            var result = await _workflowService.ExecuteWorkflowAsync(request, cts.Token);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("cancelled", result.Result.Message.ToLower());
        }

        [Fact]
        public async Task ExecuteWorkflowAsync_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _workflowService.ExecuteWorkflowAsync(null!));
        }

        [Fact]
        public async Task GetWorkflowStepsAsync_ShouldReturnAllRegisteredSteps()
        {
            // Arrange
            var mockStep1 = CreateMockStep("Step1", 1, true, AgentResult.CreateSuccess(""));
            var mockStep2 = CreateMockStep("Step2", 2, true, AgentResult.CreateSuccess(""));

            await _workflowService.RegisterWorkflowStepAsync(mockStep1.Object);
            await _workflowService.RegisterWorkflowStepAsync(mockStep2.Object);

            // Act
            var steps = await _workflowService.GetWorkflowStepsAsync();

            // Assert
            Assert.Equal(2, steps.Count());
            Assert.Contains(steps, s => s.Name == "Step1");
            Assert.Contains(steps, s => s.Name == "Step2");
        }

        private Mock<IWorkflowStep> CreateMockStep(string name, int order, bool canHandle, AgentResult result)
        {
            var mockStep = new Mock<IWorkflowStep>();
            mockStep.Setup(x => x.Name).Returns(name);
            mockStep.Setup(x => x.Order).Returns(order);
            mockStep.Setup(x => x.CanHandleAsync(It.IsAny<AgentRequest>())).ReturnsAsync(canHandle);
            mockStep.Setup(x => x.ExecuteAsync(It.IsAny<AgentRequest>(), It.IsAny<WorkflowContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WorkflowStepResult
                {
                    StepName = name,
                    Success = result.Success,
                    Result = result
                });

            return mockStep;
        }

        private static AgentRequest CreateValidRequest()
        {
            return new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "Test prompt",
                FilePath = "test.cs",
                Content = "test content",
                PreferredAgentType = AgentType.Unknown,
                CreatedAt = DateTime.UtcNow,
                UserId = "test-user"
            };
        }

        public void Dispose()
        {
            _workflowService?.Dispose();
        }
    }
}