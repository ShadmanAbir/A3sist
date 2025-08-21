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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Integration.Tests
{
    /// <summary>
    /// Complete integration tests for task queue, workflow, and orchestrator working together
    /// </summary>
    public class CompleteWorkflowIntegrationTests : IDisposable
    {
        private readonly Mock<ILogger<TaskQueueService>> _mockTaskQueueLogger;
        private readonly Mock<ILogger<WorkflowService>> _mockWorkflowLogger;
        private readonly Mock<ILogger<ValidationWorkflowStep>> _mockValidationLogger;
        private readonly Mock<ILogger<PreprocessingWorkflowStep>> _mockPreprocessingLogger;
        private readonly Mock<ILogger<Orchestrator>> _mockOrchestratorLogger;
        private readonly Mock<IAgentManager> _mockAgentManager;
        private readonly Mock<IAgentConfiguration> _mockConfiguration;
        
        private readonly TaskQueueService _taskQueueService;
        private readonly WorkflowService _workflowService;
        private readonly ValidationWorkflowStep _validationStep;
        private readonly PreprocessingWorkflowStep _preprocessingStep;
        private readonly Orchestrator _orchestrator;

        public CompleteWorkflowIntegrationTests()
        {
            _mockTaskQueueLogger = new Mock<ILogger<TaskQueueService>>();
            _mockWorkflowLogger = new Mock<ILogger<WorkflowService>>();
            _mockValidationLogger = new Mock<ILogger<ValidationWorkflowStep>>();
            _mockPreprocessingLogger = new Mock<ILogger<PreprocessingWorkflowStep>>();
            _mockOrchestratorLogger = new Mock<ILogger<Orchestrator>>();
            _mockAgentManager = new Mock<IAgentManager>();
            _mockConfiguration = new Mock<IAgentConfiguration>();

            _taskQueueService = new TaskQueueService(_mockTaskQueueLogger.Object);
            _workflowService = new WorkflowService(_mockWorkflowLogger.Object);
            _validationStep = new ValidationWorkflowStep(_mockValidationLogger.Object);
            _preprocessingStep = new PreprocessingWorkflowStep(_mockPreprocessingLogger.Object);
            
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
        public async Task CompleteWorkflow_ShouldProcessRequestThroughAllStages()
        {
            // Arrange
            var processedRequests = new List<AgentRequest>();
            var mockAgent = CreateMockAgent("CSharpAgent", AgentType.CSharp);

            // Setup agent manager
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

            // Register workflow steps
            await _workflowService.RegisterWorkflowStepAsync(_validationStep);
            await _workflowService.RegisterWorkflowStepAsync(_preprocessingStep);

            // Create a request that should trigger workflow processing
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "Please refactor this C# code using workflow processing",
                FilePath = "TestClass.cs",
                Content = "public class TestClass { public void TestMethod() { } }",
                Context = new Dictionary<string, object> { ["UseWorkflow"] = true },
                PreferredAgentType = AgentType.CSharp,
                CreatedAt = DateTime.UtcNow,
                UserId = "test-user"
            };

            // Act
            // 1. Enqueue the request
            await _taskQueueService.EnqueueAsync(request, TaskPriority.High);
            
            // 2. Dequeue and process through orchestrator
            var dequeuedRequest = await _taskQueueService.DequeueAsync();
            Assert.NotNull(dequeuedRequest);
            
            var result = await _orchestrator.ProcessRequestAsync(dequeuedRequest);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(0, await _taskQueueService.GetQueueSizeAsync());
            
            // Verify workflow was executed (should have processed through workflow steps)
            // Since the request has UseWorkflow context, it should go through workflow processing
            Assert.Contains("workflow", result.Message.ToLowerInvariant());
        }

        [Fact]
        public async Task WorkflowSteps_ShouldExecuteInCorrectOrderWithContext()
        {
            // Arrange
            await _workflowService.RegisterWorkflowStepAsync(_validationStep);
            await _workflowService.RegisterWorkflowStepAsync(_preprocessingStep);

            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "Test workflow execution",
                FilePath = "test.cs",
                Content = "class TestClass { function testMethod() { } }",
                PreferredAgentType = AgentType.CSharp,
                CreatedAt = DateTime.UtcNow,
                UserId = "test-user"
            };

            // Act
            var result = await _workflowService.ExecuteWorkflowAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, result.StepResults.Count);
            
            // Verify execution order
            Assert.Equal("Validation", result.StepResults[0].StepName);
            Assert.Equal("Preprocessing", result.StepResults[1].StepName);
            
            // Verify both steps succeeded
            Assert.All(result.StepResults, stepResult => Assert.True(stepResult.Success));
            
            // Verify preprocessing step received validation context
            var preprocessingResult = result.StepResults[1];
            Assert.NotNull(preprocessingResult.Result.Metadata);
            Assert.True(preprocessingResult.Result.Metadata.ContainsKey("DetectedLanguage"));
        }

        [Fact]
        public async Task ValidationWorkflowStep_ShouldValidateRequestCorrectly()
        {
            // Arrange - Valid request
            var validRequest = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "Valid request",
                UserId = "test-user",
                CreatedAt = DateTime.UtcNow
            };

            var context = new WorkflowContext { Request = validRequest };

            // Act
            var result = await _validationStep.ExecuteAsync(validRequest, context, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Validation", result.StepName);
            Assert.Contains("validation completed successfully", result.Result.Message.ToLowerInvariant());
            Assert.True(context.Data.ContainsKey("ValidationTimestamp"));
            Assert.Equal("Validation", context.Data["ValidatedBy"]);
        }

        [Fact]
        public async Task ValidationWorkflowStep_ShouldFailForInvalidRequest()
        {
            // Arrange - Invalid request (missing prompt)
            var invalidRequest = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "", // Empty prompt should fail validation
                UserId = "test-user",
                CreatedAt = DateTime.UtcNow
            };

            var context = new WorkflowContext { Request = invalidRequest };

            // Act
            var result = await _validationStep.ExecuteAsync(invalidRequest, context, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Validation", result.StepName);
            Assert.Contains("prompt is required", result.Result.Message.ToLowerInvariant());
        }

        [Fact]
        public async Task PreprocessingWorkflowStep_ShouldAnalyzeCodeContent()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "Please refactor this code",
                FilePath = "TestClass.cs",
                Content = @"
                    using System;
                    
                    public class TestClass 
                    {
                        public void TestMethod() 
                        {
                            Console.WriteLine(""Hello World"");
                        }
                        
                        public interface ITestInterface 
                        {
                            void TestMethod();
                        }
                    }",
                UserId = "test-user",
                CreatedAt = DateTime.UtcNow
            };

            var context = new WorkflowContext { Request = request };

            // Act
            var result = await _preprocessingStep.ExecuteAsync(request, context, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Preprocessing", result.StepName);
            
            // Verify preprocessing results
            Assert.True(context.Data.ContainsKey("PreprocessingResults"));
            var preprocessingResults = context.Data["PreprocessingResults"] as Dictionary<string, object>;
            Assert.NotNull(preprocessingResults);
            
            // Check file analysis
            Assert.Equal(".cs", preprocessingResults["FileExtension"]);
            Assert.Equal("C#", preprocessingResults["DetectedLanguage"]);
            
            // Check content analysis
            Assert.True(preprocessingResults.ContainsKey("ContentLength"));
            Assert.True(preprocessingResults.ContainsKey("LineCount"));
            
            // Check keyword analysis
            Assert.True(preprocessingResults.ContainsKey("KeywordAnalysis"));
            var keywordAnalysis = preprocessingResults["KeywordAnalysis"] as Dictionary<string, int>;
            Assert.NotNull(keywordAnalysis);
            Assert.True(keywordAnalysis.ContainsKey("class"));
            Assert.True(keywordAnalysis.ContainsKey("interface"));
            
            // Check intent hints
            Assert.True(preprocessingResults.ContainsKey("IntentHints"));
            var intentHints = preprocessingResults["IntentHints"] as List<string>;
            Assert.NotNull(intentHints);
            Assert.Contains("refactoring", intentHints);
        }

        [Fact]
        public async Task TaskQueueWithWorkflow_ShouldHandlePriorityProcessing()
        {
            // Arrange
            var processedRequests = new List<(AgentRequest Request, TaskPriority Priority)>();
            var mockAgent = CreateMockAgent("TestAgent", AgentType.CSharp);

            _mockAgentManager.Setup(x => x.GetAgentsAsync())
                .ReturnsAsync(new[] { mockAgent.Object });

            mockAgent.Setup(x => x.HandleAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()))
                .Returns<AgentRequest, CancellationToken>((req, ct) =>
                {
                    // Extract priority from context if available
                    var priority = TaskPriority.Normal;
                    if (req.Context?.ContainsKey("OriginalPriority") == true)
                    {
                        priority = (TaskPriority)req.Context["OriginalPriority"];
                    }
                    processedRequests.Add((req, priority));
                    return Task.FromResult(AgentResult.CreateSuccess($"Processed {req.Id}"));
                });

            await _workflowService.RegisterWorkflowStepAsync(_validationStep);

            // Create requests with different priorities
            var requests = new[]
            {
                (CreateRequest("Low priority"), TaskPriority.Low),
                (CreateRequest("Critical priority"), TaskPriority.Critical),
                (CreateRequest("Normal priority"), TaskPriority.Normal),
                (CreateRequest("High priority"), TaskPriority.High)
            };

            // Act - Enqueue in mixed order
            foreach (var (request, priority) in requests)
            {
                request.Context = new Dictionary<string, object> { ["OriginalPriority"] = priority };
                await _taskQueueService.EnqueueAsync(request, priority);
            }

            // Process all requests
            var results = new List<AgentResult>();
            for (int i = 0; i < requests.Length; i++)
            {
                var dequeuedRequest = await _taskQueueService.DequeueAsync();
                if (dequeuedRequest != null)
                {
                    var result = await _orchestrator.ProcessRequestAsync(dequeuedRequest);
                    results.Add(result);
                }
            }

            // Assert
            Assert.Equal(4, results.Count);
            Assert.All(results, result => Assert.True(result.Success));
            
            // Verify processing order (Critical, High, Normal, Low)
            Assert.Equal(4, processedRequests.Count);
            Assert.Equal(TaskPriority.Critical, processedRequests[0].Priority);
            Assert.Equal(TaskPriority.High, processedRequests[1].Priority);
            Assert.Equal(TaskPriority.Normal, processedRequests[2].Priority);
            Assert.Equal(TaskPriority.Low, processedRequests[3].Priority);
        }

        [Fact]
        public async Task WorkflowWithFailure_ShouldStopProcessingAndReportError()
        {
            // Arrange
            var mockFailingStep = new Mock<IWorkflowStep>();
            mockFailingStep.Setup(x => x.Name).Returns("FailingStep");
            mockFailingStep.Setup(x => x.Order).Returns(3);
            mockFailingStep.Setup(x => x.CanHandleAsync(It.IsAny<AgentRequest>())).ReturnsAsync(true);
            mockFailingStep.Setup(x => x.ExecuteAsync(It.IsAny<AgentRequest>(), It.IsAny<WorkflowContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WorkflowStepResult
                {
                    StepName = "FailingStep",
                    Success = false,
                    Result = AgentResult.CreateFailure("Intentional failure for testing")
                });

            await _workflowService.RegisterWorkflowStepAsync(_validationStep);
            await _workflowService.RegisterWorkflowStepAsync(_preprocessingStep);
            await _workflowService.RegisterWorkflowStepAsync(mockFailingStep.Object);

            var request = CreateRequest("Request that will fail in workflow");

            // Act
            var result = await _workflowService.ExecuteWorkflowAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(3, result.StepResults.Count); // All three steps should have been attempted
            Assert.True(result.StepResults[0].Success); // Validation should succeed
            Assert.True(result.StepResults[1].Success); // Preprocessing should succeed
            Assert.False(result.StepResults[2].Success); // Failing step should fail
            Assert.Contains("Intentional failure", result.Result.Message);
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