using A3sist.Core.Agents.Base;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Core.Tests.Agents.Base
{
    public class BaseAgentTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IAgentConfiguration> _mockConfiguration;
        private readonly TestAgent _testAgent;

        public BaseAgentTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockConfiguration = new Mock<IAgentConfiguration>();
            _testAgent = new TestAgent(_mockLogger.Object, _mockConfiguration.Object);
        }

        [Fact]
        public void Constructor_WithValidParameters_SetsProperties()
        {
            // Assert
            Assert.Equal("TestAgent", _testAgent.Name);
            Assert.Equal(AgentType.Unknown, _testAgent.Type);
            Assert.Equal(WorkStatus.Pending, _testAgent.Status);
            Assert.Equal(HealthStatus.Unknown, _testAgent.Health);
            Assert.NotNull(_testAgent.Metrics);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new TestAgent(null, _mockConfiguration.Object));
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new TestAgent(_mockLogger.Object, null));
        }

        [Fact]
        public async Task HandleAsync_WithNullRequest_ReturnsFailureResult()
        {
            // Act
            var result = await _testAgent.HandleAsync(null);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Request cannot be null", result.Message);
            Assert.Equal("TestAgent", result.AgentName);
        }

        [Fact]
        public async Task HandleAsync_WithValidRequest_ReturnsSuccessResult()
        {
            // Arrange
            var request = new AgentRequest("Test prompt");
            _mockConfiguration.Setup(x => x.GetAgentConfigurationAsync("TestAgent"))
                .ReturnsAsync(new AgentConfiguration { Name = "TestAgent" });

            // Act
            var result = await _testAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Test response", result.Content);
            Assert.Equal("TestAgent", result.AgentName);
            Assert.True(result.ProcessingTime > TimeSpan.Zero);
        }

        [Fact]
        public async Task HandleAsync_WithException_ReturnsFailureResult()
        {
            // Arrange
            var request = new AgentRequest("Test prompt");
            _testAgent.ShouldThrowException = true;
            _mockConfiguration.Setup(x => x.GetAgentConfigurationAsync("TestAgent"))
                .ReturnsAsync(new AgentConfiguration { Name = "TestAgent" });

            // Act
            var result = await _testAgent.HandleAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Test exception", result.Message);
            Assert.Equal("TestAgent", result.AgentName);
            Assert.NotNull(result.Exception);
        }

        [Fact]
        public async Task HandleAsync_WithCancellation_ReturnsCancelledResult()
        {
            // Arrange
            var request = new AgentRequest("Test prompt");
            var cts = new CancellationTokenSource();
            _testAgent.DelayMs = 1000; // Long delay to allow cancellation
            _mockConfiguration.Setup(x => x.GetAgentConfigurationAsync("TestAgent"))
                .ReturnsAsync(new AgentConfiguration { Name = "TestAgent" });

            // Act
            cts.CancelAfter(100); // Cancel after 100ms
            var result = await _testAgent.HandleAsync(request, cts.Token);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Operation was cancelled", result.Message);
            Assert.Equal("TestAgent", result.AgentName);
        }

        [Fact]
        public async Task HandleAsync_UpdatesMetrics()
        {
            // Arrange
            var request = new AgentRequest("Test prompt");
            _mockConfiguration.Setup(x => x.GetAgentConfigurationAsync("TestAgent"))
                .ReturnsAsync(new AgentConfiguration { Name = "TestAgent" });

            // Act
            await _testAgent.HandleAsync(request);

            // Assert
            Assert.Equal(1, _testAgent.Metrics.TasksProcessed);
            Assert.Equal(1, _testAgent.Metrics.TasksSucceeded);
            Assert.Equal(0, _testAgent.Metrics.TasksFailed);
            Assert.True(_testAgent.Metrics.AverageProcessingTime > TimeSpan.Zero);
        }

        [Fact]
        public async Task HandleAsync_WithFailure_UpdatesFailureMetrics()
        {
            // Arrange
            var request = new AgentRequest("Test prompt");
            _testAgent.ShouldReturnFailure = true;
            _mockConfiguration.Setup(x => x.GetAgentConfigurationAsync("TestAgent"))
                .ReturnsAsync(new AgentConfiguration { Name = "TestAgent" });

            // Act
            await _testAgent.HandleAsync(request);

            // Assert
            Assert.Equal(1, _testAgent.Metrics.TasksProcessed);
            Assert.Equal(0, _testAgent.Metrics.TasksSucceeded);
            Assert.Equal(1, _testAgent.Metrics.TasksFailed);
        }

        [Fact]
        public async Task CanHandleAsync_WithNullRequest_ReturnsFalse()
        {
            // Act
            var result = await _testAgent.CanHandleAsync(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanHandleAsync_WithValidRequest_ReturnsTrue()
        {
            // Arrange
            var request = new AgentRequest("Test prompt");

            // Act
            var result = await _testAgent.CanHandleAsync(request);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanHandleAsync_WithPreferredAgentTypeMismatch_ReturnsFalse()
        {
            // Arrange
            var request = new AgentRequest("Test prompt")
            {
                PreferredAgentType = AgentType.Fixer // Different from TestAgent's type
            };

            // Act
            var result = await _testAgent.CanHandleAsync(request);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task InitializeAsync_CallsInitializeAgentAsync()
        {
            // Act
            await _testAgent.InitializeAsync();

            // Assert
            Assert.True(_testAgent.InitializeCalled);
            Assert.Equal(HealthStatus.Healthy, _testAgent.Health);
        }

        [Fact]
        public async Task InitializeAsync_CalledMultipleTimes_InitializesOnlyOnce()
        {
            // Act
            await _testAgent.InitializeAsync();
            await _testAgent.InitializeAsync();
            await _testAgent.InitializeAsync();

            // Assert
            Assert.Equal(1, _testAgent.InitializeCallCount);
        }

        [Fact]
        public async Task ShutdownAsync_CallsShutdownAgentAsync()
        {
            // Act
            await _testAgent.ShutdownAsync();

            // Assert
            Assert.True(_testAgent.ShutdownCalled);
        }

        [Fact]
        public void GetStatus_ReturnsCorrectStatus()
        {
            // Act
            var status = _testAgent.GetStatus();

            // Assert
            Assert.Equal("TestAgent", status.Name);
            Assert.Equal(AgentType.Unknown, status.Type);
            Assert.Equal(WorkStatus.Pending, status.Status);
            Assert.Equal(HealthStatus.Unknown, status.Health);
            Assert.True(status.MemoryUsage > 0);
        }

        [Fact]
        public async Task HandleAsync_WithRetryPolicy_RetriesOnFailure()
        {
            // Arrange
            var request = new AgentRequest("Test prompt");
            var retryPolicy = new RetryPolicy
            {
                MaxRetries = 2,
                InitialDelay = TimeSpan.FromMilliseconds(10),
                RetryableExceptions = new[] { "System.InvalidOperationException" }
            };
            var agentConfig = new AgentConfiguration
            {
                Name = "TestAgent",
                RetryPolicy = retryPolicy
            };
            _mockConfiguration.Setup(x => x.GetAgentConfigurationAsync("TestAgent"))
                .ReturnsAsync(agentConfig);
            
            _testAgent.FailFirstAttempts = 1; // Fail first attempt, succeed on retry

            // Act
            var result = await _testAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, _testAgent.HandleRequestCallCount); // Called twice due to retry
        }
    }

    /// <summary>
    /// Test implementation of BaseAgent for testing purposes
    /// </summary>
    public class TestAgent : BaseAgent
    {
        public override string Name => "TestAgent";
        public override AgentType Type => AgentType.Unknown;

        public bool ShouldThrowException { get; set; }
        public bool ShouldReturnFailure { get; set; }
        public int DelayMs { get; set; }
        public bool InitializeCalled { get; private set; }
        public bool ShutdownCalled { get; private set; }
        public int InitializeCallCount { get; private set; }
        public int HandleRequestCallCount { get; private set; }
        public int FailFirstAttempts { get; set; }
        private int _attemptCount;

        public TestAgent(ILogger logger, IAgentConfiguration configuration) 
            : base(logger, configuration)
        {
        }

        protected override async Task<AgentResult> HandleRequestAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            HandleRequestCallCount++;
            _attemptCount++;

            if (DelayMs > 0)
            {
                await Task.Delay(DelayMs, cancellationToken);
            }

            if (ShouldThrowException)
            {
                throw new InvalidOperationException("Test exception");
            }

            if (FailFirstAttempts > 0 && _attemptCount <= FailFirstAttempts)
            {
                throw new InvalidOperationException("Retry test exception");
            }

            if (ShouldReturnFailure)
            {
                return AgentResult.CreateFailure("Test failure");
            }

            return AgentResult.CreateSuccess("Test success", "Test response");
        }

        protected override Task InitializeAgentAsync()
        {
            InitializeCalled = true;
            InitializeCallCount++;
            return Task.CompletedTask;
        }

        protected override Task ShutdownAgentAsync()
        {
            ShutdownCalled = true;
            return Task.CompletedTask;
        }
    }
}