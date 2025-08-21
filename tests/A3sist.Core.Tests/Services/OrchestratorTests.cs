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
    public class OrchestratorTests : IDisposable
    {
        private readonly Mock<IAgentManager> _mockAgentManager;
        private readonly Mock<ITaskQueueService> _mockTaskQueueService;
        private readonly Mock<IWorkflowService> _mockWorkflowService;
        private readonly Mock<ILogger<Orchestrator>> _mockLogger;
        private readonly Mock<IAgentConfiguration> _mockConfiguration;
        private readonly Orchestrator _orchestrator;
        private readonly Mock<IAgent> _mockAgent;

        public OrchestratorTests()
        {
            _mockAgentManager = new Mock<IAgentManager>();
            _mockTaskQueueService = new Mock<ITaskQueueService>();
            _mockWorkflowService = new Mock<IWorkflowService>();
            _mockLogger = new Mock<ILogger<Orchestrator>>();
            _mockConfiguration = new Mock<IAgentConfiguration>();
            _mockAgent = new Mock<IAgent>();
            
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
        public async Task InitializeAsync_ShouldStartAllAgents()
        {
            // Arrange
            _mockAgentManager.Setup(x => x.StartAllAgentsAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _orchestrator.InitializeAsync();

            // Assert
            _mockAgentManager.Verify(x => x.StartAllAgentsAsync(), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenCalledMultipleTimes_ShouldOnlyInitializeOnce()
        {
            // Arrange
            _mockAgentManager.Setup(x => x.StartAllAgentsAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _orchestrator.InitializeAsync();
            await _orchestrator.InitializeAsync();
            await _orchestrator.InitializeAsync();

            // Assert
            _mockAgentManager.Verify(x => x.StartAllAgentsAsync(), Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _orchestrator.ProcessRequestAsync(null!));
        }

        [Fact]
        public async Task ProcessRequestAsync_WithInvalidRequest_ShouldReturnFailure()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.Empty, // Invalid ID
                Prompt = "test prompt"
            };

            // Act
            var result = await _orchestrator.ProcessRequestAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Request ID is required", result.Message);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithEmptyPrompt_ShouldReturnFailure()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "" // Empty prompt
            };

            // Act
            var result = await _orchestrator.ProcessRequestAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Request prompt is required", result.Message);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithNoAvailableAgents_ShouldReturnFailure()
        {
            // Arrange
            var request = CreateValidRequest();
            
            _mockAgentManager.Setup(x => x.StartAllAgentsAsync())
                .Returns(Task.CompletedTask);
            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(Enumerable.Empty<IAgent>());

            // Act
            var result = await _orchestrator.ProcessRequestAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("No suitable agent found", result.Message);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithCapableAgent_ShouldProcessSuccessfully()
        {
            // Arrange
            var request = CreateValidRequest();
            var expectedResult = AgentResult.CreateSuccess("Agent processed successfully");
            
            _mockAgent.Setup(x => x.Name).Returns("TestAgent");
            _mockAgent.Setup(x => x.Type).Returns(AgentType.CSharp);
            _mockAgent.Setup(x => x.CanHandleAsync(request)).ReturnsAsync(true);
            _mockAgent.Setup(x => x.HandleAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            _mockAgentManager.Setup(x => x.StartAllAgentsAsync())
                .Returns(Task.CompletedTask);
            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(new[] { _mockAgent.Object });

            // Act
            var result = await _orchestrator.ProcessRequestAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(expectedResult.Message, result.Message);
            Assert.NotNull(result.Metadata);
            Assert.Equal("TestAgent", result.Metadata["AgentName"]);
            Assert.Equal("CSharp", result.Metadata["AgentType"]);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithPreferredAgentType_ShouldSelectPreferredAgent()
        {
            // Arrange
            var request = CreateValidRequest();
            request.PreferredAgentType = AgentType.Python;

            var csharpAgent = new Mock<IAgent>();
            csharpAgent.Setup(x => x.Name).Returns("CSharpAgent");
            csharpAgent.Setup(x => x.Type).Returns(AgentType.CSharp);
            csharpAgent.Setup(x => x.CanHandleAsync(request)).ReturnsAsync(true);

            var pythonAgent = new Mock<IAgent>();
            pythonAgent.Setup(x => x.Name).Returns("PythonAgent");
            pythonAgent.Setup(x => x.Type).Returns(AgentType.Python);
            pythonAgent.Setup(x => x.CanHandleAsync(request)).ReturnsAsync(true);
            pythonAgent.Setup(x => x.HandleAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(AgentResult.CreateSuccess("Python agent processed"));

            _mockAgentManager.Setup(x => x.StartAllAgentsAsync())
                .Returns(Task.CompletedTask);
            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(new[] { csharpAgent.Object, pythonAgent.Object });

            // Act
            var result = await _orchestrator.ProcessRequestAsync(request);

            // Assert
            Assert.True(result.Success);
            pythonAgent.Verify(x => x.HandleAsync(request, It.IsAny<CancellationToken>()), Times.Once);
            csharpAgent.Verify(x => x.HandleAsync(request, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ProcessRequestAsync_WhenAgentThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var request = CreateValidRequest();
            var exception = new InvalidOperationException("Agent error");
            
            _mockAgent.Setup(x => x.Name).Returns("TestAgent");
            _mockAgent.Setup(x => x.Type).Returns(AgentType.CSharp);
            _mockAgent.Setup(x => x.CanHandleAsync(request)).ReturnsAsync(true);
            _mockAgent.Setup(x => x.HandleAsync(request, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            _mockAgentManager.Setup(x => x.StartAllAgentsAsync())
                .Returns(Task.CompletedTask);
            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(new[] { _mockAgent.Object });

            // Act
            var result = await _orchestrator.ProcessRequestAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Agent TestAgent failed", result.Message);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithCancellationToken_ShouldRespectCancellation()
        {
            // Arrange
            var request = CreateValidRequest();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            _mockAgent.Setup(x => x.Name).Returns("TestAgent");
            _mockAgent.Setup(x => x.Type).Returns(AgentType.CSharp);
            _mockAgent.Setup(x => x.CanHandleAsync(request)).ReturnsAsync(true);
            _mockAgent.Setup(x => x.HandleAsync(request, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            _mockAgentManager.Setup(x => x.StartAllAgentsAsync())
                .Returns(Task.CompletedTask);
            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(new[] { _mockAgent.Object });

            // Act
            var result = await _orchestrator.ProcessRequestAsync(request, cts.Token);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Request was cancelled", result.Message);
        }

        [Fact]
        public async Task GetAvailableAgentsAsync_ShouldReturnAgentsFromManager()
        {
            // Arrange
            var agents = new[] { _mockAgent.Object };
            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(agents);

            // Act
            var result = await _orchestrator.GetAvailableAgentsAsync();

            // Assert
            Assert.Equal(agents, result);
        }

        [Fact]
        public async Task RegisterAgentAsync_ShouldCallAgentManager()
        {
            // Arrange
            var agent = _mockAgent.Object;

            // Act
            await _orchestrator.RegisterAgentAsync(agent);

            // Assert
            _mockAgentManager.Verify(x => x.RegisterAgentAsync(agent), Times.Once);
        }

        [Fact]
        public async Task RegisterAgentAsync_WithNullAgent_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _orchestrator.RegisterAgentAsync(null!));
        }

        [Fact]
        public async Task UnregisterAgentAsync_ShouldCallAgentManager()
        {
            // Arrange
            const string agentName = "TestAgent";

            // Act
            await _orchestrator.UnregisterAgentAsync(agentName);

            // Assert
            _mockAgentManager.Verify(x => x.UnregisterAgentAsync(agentName), Times.Once);
        }

        [Fact]
        public async Task ShutdownAsync_ShouldStopAllAgents()
        {
            // Arrange
            _mockAgentManager.Setup(x => x.StartAllAgentsAsync())
                .Returns(Task.CompletedTask);
            _mockAgentManager.Setup(x => x.StopAllAgentsAsync())
                .Returns(Task.CompletedTask);

            await _orchestrator.InitializeAsync();

            // Act
            await _orchestrator.ShutdownAsync();

            // Assert
            _mockAgentManager.Verify(x => x.StopAllAgentsAsync(), Times.Once);
        }

        [Fact]
        public async Task LoadBalancing_ShouldDistributeRequestsEvenly()
        {
            // Arrange
            var request1 = CreateValidRequest();
            var request2 = CreateValidRequest();

            var agent1 = new Mock<IAgent>();
            agent1.Setup(x => x.Name).Returns("Agent1");
            agent1.Setup(x => x.Type).Returns(AgentType.CSharp);
            agent1.Setup(x => x.CanHandleAsync(It.IsAny<AgentRequest>())).ReturnsAsync(true);
            agent1.Setup(x => x.HandleAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AgentResult.CreateSuccess("Agent1 processed"));

            var agent2 = new Mock<IAgent>();
            agent2.Setup(x => x.Name).Returns("Agent2");
            agent2.Setup(x => x.Type).Returns(AgentType.CSharp);
            agent2.Setup(x => x.CanHandleAsync(It.IsAny<AgentRequest>())).ReturnsAsync(true);
            agent2.Setup(x => x.HandleAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AgentResult.CreateSuccess("Agent2 processed"));

            _mockAgentManager.Setup(x => x.StartAllAgentsAsync())
                .Returns(Task.CompletedTask);
            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(new[] { agent1.Object, agent2.Object });

            // Act
            var result1 = await _orchestrator.ProcessRequestAsync(request1);
            var result2 = await _orchestrator.ProcessRequestAsync(request2);

            // Assert
            Assert.True(result1.Success);
            Assert.True(result2.Success);
            
            // Both agents should have been called at least once
            agent1.Verify(x => x.HandleAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            agent2.Verify(x => x.HandleAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
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

        [Fact]
        public async Task ProcessRequestAsync_WithWorkflowRequest_ShouldUseWorkflowService()
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
            _mockWorkflowService.Verify(x => x.ExecuteWorkflowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithIntentRouter_ShouldUseEnhancedRouting()
        {
            // Arrange
            var request = CreateValidRequest();
            var intentRouter = new Mock<IAgent>();
            var targetAgent = new Mock<IAgent>();
            
            intentRouter.Setup(x => x.Name).Returns("IntentRouter");
            intentRouter.Setup(x => x.Type).Returns(AgentType.IntentRouter);
            
            var routingDecision = new RoutingDecision
            {
                TargetAgent = "TestAgent",
                TargetAgentType = AgentType.CSharp,
                Confidence = 0.9
            };
            
            var routingResult = AgentResult.CreateSuccess("Routing completed");
            routingResult.Metadata = new Dictionary<string, object>
            {
                ["RoutingDecision"] = routingDecision
            };

            targetAgent.Setup(x => x.Name).Returns("TestAgent");
            targetAgent.Setup(x => x.Type).Returns(AgentType.CSharp);
            targetAgent.Setup(x => x.HandleAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(AgentResult.CreateSuccess("Agent processed"));

            _mockAgentManager.Setup(x => x.StartAllAgentsAsync()).Returns(Task.CompletedTask);
            _mockAgentManager.Setup(x => x.GetAgentAsync(AgentType.IntentRouter))
                .ReturnsAsync(intentRouter.Object);
            _mockAgentManager.Setup(x => x.GetAgentAsync("TestAgent"))
                .ReturnsAsync(targetAgent.Object);
            
            intentRouter.Setup(x => x.HandleAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(routingResult);

            // Act
            var result = await _orchestrator.ProcessRequestAsync(request);

            // Assert
            Assert.True(result.Success);
            intentRouter.Verify(x => x.HandleAsync(request, It.IsAny<CancellationToken>()), Times.Once);
            targetAgent.Verify(x => x.HandleAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithRetryableFailure_ShouldRetryAndSucceed()
        {
            // Arrange
            var request = CreateValidRequest();
            var agent = new Mock<IAgent>();
            
            agent.Setup(x => x.Name).Returns("TestAgent");
            agent.Setup(x => x.Type).Returns(AgentType.CSharp);
            agent.Setup(x => x.CanHandleAsync(request)).ReturnsAsync(true);
            
            // First call fails, second succeeds
            agent.SetupSequence(x => x.HandleAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(AgentResult.CreateFailure("Temporary failure"))
                .ReturnsAsync(AgentResult.CreateSuccess("Success on retry"));

            _mockAgentManager.Setup(x => x.StartAllAgentsAsync()).Returns(Task.CompletedTask);
            _mockAgentManager.Setup(x => x.GetAgentAsync(AgentType.IntentRouter))
                .ReturnsAsync((IAgent?)null);
            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(new[] { agent.Object });

            // Act
            var result = await _orchestrator.ProcessRequestAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Success on retry", result.Message);
            agent.Verify(x => x.HandleAsync(request, It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ProcessRequestAsync_WithNonRetryableFailure_ShouldNotRetry()
        {
            // Arrange
            var request = CreateValidRequest();
            var agent = new Mock<IAgent>();
            
            agent.Setup(x => x.Name).Returns("TestAgent");
            agent.Setup(x => x.Type).Returns(AgentType.CSharp);
            agent.Setup(x => x.CanHandleAsync(request)).ReturnsAsync(true);
            
            var nonRetryableResult = AgentResult.CreateFailure("Invalid argument", new ArgumentException("Invalid"));
            agent.Setup(x => x.HandleAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(nonRetryableResult);

            _mockAgentManager.Setup(x => x.StartAllAgentsAsync()).Returns(Task.CompletedTask);
            _mockAgentManager.Setup(x => x.GetAgentAsync(AgentType.IntentRouter))
                .ReturnsAsync((IAgent?)null);
            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(new[] { agent.Object });

            // Act
            var result = await _orchestrator.ProcessRequestAsync(request);

            // Assert
            Assert.False(result.Success);
            agent.Verify(x => x.HandleAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithCircuitBreakerOpen_ShouldRejectRequest()
        {
            // Arrange
            var request = CreateValidRequest();
            var agent = new Mock<IAgent>();
            
            agent.Setup(x => x.Name).Returns("TestAgent");
            agent.Setup(x => x.Type).Returns(AgentType.CSharp);
            agent.Setup(x => x.CanHandleAsync(request)).ReturnsAsync(true);

            _mockAgentManager.Setup(x => x.StartAllAgentsAsync()).Returns(Task.CompletedTask);
            _mockAgentManager.Setup(x => x.GetAgentAsync(AgentType.IntentRouter))
                .ReturnsAsync((IAgent?)null);
            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(new[] { agent.Object });

            // Simulate multiple failures to trigger circuit breaker
            for (int i = 0; i < 5; i++)
            {
                agent.Setup(x => x.HandleAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(AgentResult.CreateFailure("Failure"));
                await _orchestrator.ProcessRequestAsync(CreateValidRequest());
            }

            // Act - This request should be rejected by circuit breaker
            var result = await _orchestrator.ProcessRequestAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("temporarily unavailable", result.Message);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithRecoveryScenario_ShouldAttemptRecovery()
        {
            // Arrange
            var request = CreateValidRequest();
            var failingAgent = new Mock<IAgent>();
            var recoveryAgent = new Mock<IAgent>();
            
            failingAgent.Setup(x => x.Name).Returns("FailingAgent");
            failingAgent.Setup(x => x.Type).Returns(AgentType.CSharp);
            failingAgent.Setup(x => x.CanHandleAsync(request)).ReturnsAsync(true);
            failingAgent.Setup(x => x.HandleAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Critical failure"));

            recoveryAgent.Setup(x => x.Name).Returns("RecoveryAgent");
            recoveryAgent.Setup(x => x.Type).Returns(AgentType.CSharp);
            recoveryAgent.Setup(x => x.HandleAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AgentResult.CreateSuccess("Recovery successful"));

            _mockAgentManager.Setup(x => x.StartAllAgentsAsync()).Returns(Task.CompletedTask);
            _mockAgentManager.Setup(x => x.GetAgentAsync(AgentType.IntentRouter))
                .ReturnsAsync((IAgent?)null);
            _mockAgentManager.SetupSequence(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(new[] { failingAgent.Object })
                .ReturnsAsync(new[] { recoveryAgent.Object });

            // Act
            var result = await _orchestrator.ProcessRequestAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Recovery successful", result.Message);
            Assert.True(result.Metadata?.ContainsKey("IsRecoveryResult") == true);
        }

        [Fact]
        public async Task Constructor_WithNullDependencies_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Orchestrator(null!, _mockTaskQueueService.Object, _mockWorkflowService.Object, _mockLogger.Object, _mockConfiguration.Object));
            Assert.Throws<ArgumentNullException>(() => new Orchestrator(_mockAgentManager.Object, null!, _mockWorkflowService.Object, _mockLogger.Object, _mockConfiguration.Object));
            Assert.Throws<ArgumentNullException>(() => new Orchestrator(_mockAgentManager.Object, _mockTaskQueueService.Object, null!, _mockLogger.Object, _mockConfiguration.Object));
            Assert.Throws<ArgumentNullException>(() => new Orchestrator(_mockAgentManager.Object, _mockTaskQueueService.Object, _mockWorkflowService.Object, null!, _mockConfiguration.Object));
            Assert.Throws<ArgumentNullException>(() => new Orchestrator(_mockAgentManager.Object, _mockTaskQueueService.Object, _mockWorkflowService.Object, _mockLogger.Object, null!));
        }

        [Fact]
        public async Task ProcessRequestAsync_WithWorkflowPrompt_ShouldDetectWorkflowNeed()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Prompt = "Please perform a multi-step analysis of this code";
            
            var workflowResult = new WorkflowResult
            {
                Success = true,
                Result = AgentResult.CreateSuccess("Multi-step workflow completed")
            };

            _mockAgentManager.Setup(x => x.StartAllAgentsAsync()).Returns(Task.CompletedTask);
            _mockWorkflowService.Setup(x => x.ExecuteWorkflowAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(workflowResult);

            // Act
            var result = await _orchestrator.ProcessRequestAsync(request);

            // Assert
            Assert.True(result.Success);
            _mockWorkflowService.Verify(x => x.ExecuteWorkflowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        }

        public void Dispose()
        {
            _orchestrator?.Dispose();
        }
    }
}