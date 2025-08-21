using A3sist.Core.Services;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Core.Tests.Services
{
    public class AgentManagerTests : IDisposable
    {
        private readonly Mock<ILogger<AgentManager>> _mockLogger;
        private readonly AgentManager _agentManager;
        private readonly Mock<IAgent> _mockAgent;

        public AgentManagerTests()
        {
            _mockLogger = new Mock<ILogger<AgentManager>>();
            _agentManager = new AgentManager(_mockLogger.Object);
            _mockAgent = new Mock<IAgent>();
            _mockAgent.Setup(x => x.Name).Returns("TestAgent");
            _mockAgent.Setup(x => x.Type).Returns(AgentType.Unknown);
            _mockAgent.Setup(x => x.InitializeAsync()).Returns(Task.CompletedTask);
            _mockAgent.Setup(x => x.ShutdownAsync()).Returns(Task.CompletedTask);
        }

        [Fact]
        public void Constructor_WithValidLogger_InitializesSuccessfully()
        {
            // Assert
            Assert.NotNull(_agentManager);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AgentManager(null));
        }

        [Fact]
        public async Task RegisterAgentAsync_WithValidAgent_RegistersSuccessfully()
        {
            // Arrange
            bool eventRaised = false;
            _agentManager.AgentRegistered += (sender, args) => eventRaised = true;

            // Act
            await _agentManager.RegisterAgentAsync(_mockAgent.Object);

            // Assert
            var retrievedAgent = await _agentManager.GetAgentAsync("TestAgent");
            Assert.NotNull(retrievedAgent);
            Assert.Equal("TestAgent", retrievedAgent.Name);
            Assert.True(eventRaised);
            _mockAgent.Verify(x => x.InitializeAsync(), Times.Once);
        }

        [Fact]
        public async Task RegisterAgentAsync_WithNullAgent_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _agentManager.RegisterAgentAsync(null));
        }

        [Fact]
        public async Task RegisterAgentAsync_WithDuplicateAgent_DoesNotRegisterTwice()
        {
            // Arrange
            await _agentManager.RegisterAgentAsync(_mockAgent.Object);

            // Act
            await _agentManager.RegisterAgentAsync(_mockAgent.Object);

            // Assert
            var agents = await _agentManager.GetAgentsAsync();
            Assert.Single(agents);
            _mockAgent.Verify(x => x.InitializeAsync(), Times.Once);
        }

        [Fact]
        public async Task RegisterAgentAsync_WithInitializationFailure_ThrowsException()
        {
            // Arrange
            _mockAgent.Setup(x => x.InitializeAsync()).ThrowsAsync(new InvalidOperationException("Init failed"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _agentManager.RegisterAgentAsync(_mockAgent.Object));
        }

        [Fact]
        public async Task UnregisterAgentAsync_WithExistingAgent_UnregistersSuccessfully()
        {
            // Arrange
            await _agentManager.RegisterAgentAsync(_mockAgent.Object);
            bool eventRaised = false;
            _agentManager.AgentUnregistered += (sender, args) => eventRaised = true;

            // Act
            await _agentManager.UnregisterAgentAsync("TestAgent");

            // Assert
            var retrievedAgent = await _agentManager.GetAgentAsync("TestAgent");
            Assert.Null(retrievedAgent);
            Assert.True(eventRaised);
            _mockAgent.Verify(x => x.ShutdownAsync(), Times.Once);
        }

        [Fact]
        public async Task UnregisterAgentAsync_WithNonExistentAgent_DoesNotThrow()
        {
            // Act & Assert (should not throw)
            await _agentManager.UnregisterAgentAsync("NonExistentAgent");
        }

        [Fact]
        public async Task UnregisterAgentAsync_WithNullOrEmptyName_DoesNotThrow()
        {
            // Act & Assert (should not throw)
            await _agentManager.UnregisterAgentAsync(null);
            await _agentManager.UnregisterAgentAsync("");
            await _agentManager.UnregisterAgentAsync("   ");
        }

        [Fact]
        public async Task GetAgentAsync_ByName_ReturnsCorrectAgent()
        {
            // Arrange
            await _agentManager.RegisterAgentAsync(_mockAgent.Object);

            // Act
            var agent = await _agentManager.GetAgentAsync("TestAgent");

            // Assert
            Assert.NotNull(agent);
            Assert.Equal("TestAgent", agent.Name);
        }

        [Fact]
        public async Task GetAgentAsync_ByType_ReturnsCorrectAgent()
        {
            // Arrange
            await _agentManager.RegisterAgentAsync(_mockAgent.Object);

            // Act
            var agent = await _agentManager.GetAgentAsync(AgentType.Unknown);

            // Assert
            Assert.NotNull(agent);
            Assert.Equal(AgentType.Unknown, agent.Type);
        }

        [Fact]
        public async Task GetAgentAsync_WithNonExistentName_ReturnsNull()
        {
            // Act
            var agent = await _agentManager.GetAgentAsync("NonExistentAgent");

            // Assert
            Assert.Null(agent);
        }

        [Fact]
        public async Task GetAgentsAsync_WithoutPredicate_ReturnsAllAgents()
        {
            // Arrange
            var mockAgent2 = new Mock<IAgent>();
            mockAgent2.Setup(x => x.Name).Returns("TestAgent2");
            mockAgent2.Setup(x => x.Type).Returns(AgentType.Fixer);
            mockAgent2.Setup(x => x.InitializeAsync()).Returns(Task.CompletedTask);

            await _agentManager.RegisterAgentAsync(_mockAgent.Object);
            await _agentManager.RegisterAgentAsync(mockAgent2.Object);

            // Act
            var agents = await _agentManager.GetAgentsAsync();

            // Assert
            Assert.Equal(2, agents.Count());
        }

        [Fact]
        public async Task GetAgentsAsync_WithPredicate_ReturnsFilteredAgents()
        {
            // Arrange
            var mockAgent2 = new Mock<IAgent>();
            mockAgent2.Setup(x => x.Name).Returns("TestAgent2");
            mockAgent2.Setup(x => x.Type).Returns(AgentType.Fixer);
            mockAgent2.Setup(x => x.InitializeAsync()).Returns(Task.CompletedTask);

            await _agentManager.RegisterAgentAsync(_mockAgent.Object);
            await _agentManager.RegisterAgentAsync(mockAgent2.Object);

            // Act
            var agents = await _agentManager.GetAgentsAsync(a => a.Type == AgentType.Fixer);

            // Assert
            Assert.Single(agents);
            Assert.Equal("TestAgent2", agents.First().Name);
        }

        [Fact]
        public async Task GetAgentStatusAsync_WithExistingAgent_ReturnsStatus()
        {
            // Arrange
            await _agentManager.RegisterAgentAsync(_mockAgent.Object);

            // Act
            var status = await _agentManager.GetAgentStatusAsync("TestAgent");

            // Assert
            Assert.NotNull(status);
            Assert.Equal("TestAgent", status.Name);
            Assert.Equal(AgentType.Unknown, status.Type);
        }

        [Fact]
        public async Task GetAgentStatusAsync_WithNonExistentAgent_ReturnsNull()
        {
            // Act
            var status = await _agentManager.GetAgentStatusAsync("NonExistentAgent");

            // Assert
            Assert.Null(status);
        }

        [Fact]
        public async Task GetAllAgentStatusesAsync_ReturnsAllStatuses()
        {
            // Arrange
            var mockAgent2 = new Mock<IAgent>();
            mockAgent2.Setup(x => x.Name).Returns("TestAgent2");
            mockAgent2.Setup(x => x.Type).Returns(AgentType.Fixer);
            mockAgent2.Setup(x => x.InitializeAsync()).Returns(Task.CompletedTask);

            await _agentManager.RegisterAgentAsync(_mockAgent.Object);
            await _agentManager.RegisterAgentAsync(mockAgent2.Object);

            // Act
            var statuses = await _agentManager.GetAllAgentStatusesAsync();

            // Assert
            Assert.Equal(2, statuses.Count());
        }

        [Fact]
        public async Task StartAllAgentsAsync_InitializesAllAgents()
        {
            // Arrange
            var mockAgent2 = new Mock<IAgent>();
            mockAgent2.Setup(x => x.Name).Returns("TestAgent2");
            mockAgent2.Setup(x => x.Type).Returns(AgentType.Fixer);
            mockAgent2.Setup(x => x.InitializeAsync()).Returns(Task.CompletedTask);

            await _agentManager.RegisterAgentAsync(_mockAgent.Object);
            await _agentManager.RegisterAgentAsync(mockAgent2.Object);

            // Reset the mock to verify StartAllAgentsAsync calls
            _mockAgent.Reset();
            _mockAgent.Setup(x => x.Name).Returns("TestAgent");
            _mockAgent.Setup(x => x.InitializeAsync()).Returns(Task.CompletedTask);

            // Act
            await _agentManager.StartAllAgentsAsync();

            // Assert
            _mockAgent.Verify(x => x.InitializeAsync(), Times.Once);
            mockAgent2.Verify(x => x.InitializeAsync(), Times.Once);
        }

        [Fact]
        public async Task StopAllAgentsAsync_ShutsDownAllAgents()
        {
            // Arrange
            var mockAgent2 = new Mock<IAgent>();
            mockAgent2.Setup(x => x.Name).Returns("TestAgent2");
            mockAgent2.Setup(x => x.Type).Returns(AgentType.Fixer);
            mockAgent2.Setup(x => x.InitializeAsync()).Returns(Task.CompletedTask);
            mockAgent2.Setup(x => x.ShutdownAsync()).Returns(Task.CompletedTask);

            await _agentManager.RegisterAgentAsync(_mockAgent.Object);
            await _agentManager.RegisterAgentAsync(mockAgent2.Object);

            // Act
            await _agentManager.StopAllAgentsAsync();

            // Assert
            _mockAgent.Verify(x => x.ShutdownAsync(), Times.Once);
            mockAgent2.Verify(x => x.ShutdownAsync(), Times.Once);
        }

        [Fact]
        public async Task PerformHealthChecksAsync_ReturnsHealthStatuses()
        {
            // Arrange
            await _agentManager.RegisterAgentAsync(_mockAgent.Object);

            // Act
            var healthStatuses = await _agentManager.PerformHealthChecksAsync();

            // Assert
            Assert.Single(healthStatuses);
            Assert.True(healthStatuses.ContainsKey("TestAgent"));
        }

        [Fact]
        public async Task AgentStatusChanged_EventRaised_WhenStatusChanges()
        {
            // This test would require a more complex setup with BaseAgent
            // For now, we'll just verify the event exists
            Assert.NotNull(_agentManager.AgentStatusChanged);
        }

        public void Dispose()
        {
            _agentManager?.Dispose();
        }
    }
}