using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using A3sist.Core.Services;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using A3sist.Shared.Enums;
using A3sist.TestUtilities;

namespace A3sist.Core.Tests.Services;

/// <summary>
/// Unit tests for AgentManager
/// </summary>
public class AgentManagerTests : TestBase
{
    private readonly Mock<ILogger<AgentManager>> _mockLogger;
    private readonly AgentManager _agentManager;

    public AgentManagerTests()
    {
        _mockLogger = new Mock<ILogger<AgentManager>>();
        _agentManager = new AgentManager(_mockLogger.Object);
    }

    [Fact]
    public async Task RegisterAgentAsync_WithValidAgent_ShouldRegisterSuccessfully()
    {
        // Arrange
        var mockAgent = MockFactory.CreateAgent("TestAgent", AgentType.Analyzer);

        // Act
        await _agentManager.RegisterAgentAsync(mockAgent.Object);

        // Assert
        var registeredAgent = await _agentManager.GetAgentAsync("TestAgent");
        registeredAgent.Should().NotBeNull();
        registeredAgent.Should().BeSameAs(mockAgent.Object);
    }

    [Fact]
    public async Task RegisterAgentAsync_WithNullAgent_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _agentManager.RegisterAgentAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RegisterAgentAsync_WithDuplicateName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mockAgent1 = MockFactory.CreateAgent("TestAgent", AgentType.Analyzer);
        var mockAgent2 = MockFactory.CreateAgent("TestAgent", AgentType.Fixer);

        // Act
        await _agentManager.RegisterAgentAsync(mockAgent1.Object);
        var act = async () => await _agentManager.RegisterAgentAsync(mockAgent2.Object);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public async Task UnregisterAgentAsync_WithExistingAgent_ShouldUnregisterSuccessfully()
    {
        // Arrange
        var mockAgent = MockFactory.CreateAgent("TestAgent", AgentType.Analyzer);
        await _agentManager.RegisterAgentAsync(mockAgent.Object);

        // Act
        await _agentManager.UnregisterAgentAsync("TestAgent");

        // Assert
        var agent = await _agentManager.GetAgentAsync("TestAgent");
        agent.Should().BeNull();
    }

    [Fact]
    public async Task UnregisterAgentAsync_WithNonExistentAgent_ShouldNotThrow()
    {
        // Act
        var act = async () => await _agentManager.UnregisterAgentAsync("NonExistentAgent");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetAgentAsync_ByName_WithExistingAgent_ShouldReturnAgent()
    {
        // Arrange
        var mockAgent = MockFactory.CreateAgent("TestAgent", AgentType.Analyzer);
        await _agentManager.RegisterAgentAsync(mockAgent.Object);

        // Act
        var result = await _agentManager.GetAgentAsync("TestAgent");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(mockAgent.Object);
    }

    [Fact]
    public async Task GetAgentAsync_ByName_WithNonExistentAgent_ShouldReturnNull()
    {
        // Act
        var result = await _agentManager.GetAgentAsync("NonExistentAgent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAgentAsync_ByType_WithExistingAgent_ShouldReturnAgent()
    {
        // Arrange
        var mockAgent = MockFactory.CreateAgent("TestAgent", AgentType.Fixer);
        await _agentManager.RegisterAgentAsync(mockAgent.Object);

        // Act
        var result = await _agentManager.GetAgentAsync(AgentType.Fixer);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(mockAgent.Object);
    }

    [Fact]
    public async Task GetAgentAsync_ByType_WithNonExistentType_ShouldReturnNull()
    {
        // Act
        var result = await _agentManager.GetAgentAsync(AgentType.Designer);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAgentsAsync_WithoutPredicate_ShouldReturnAllAgents()
    {
        // Arrange
        var mockAgent1 = MockFactory.CreateAgent("Agent1", AgentType.Analyzer);
        var mockAgent2 = MockFactory.CreateAgent("Agent2", AgentType.Fixer);
        await _agentManager.RegisterAgentAsync(mockAgent1.Object);
        await _agentManager.RegisterAgentAsync(mockAgent2.Object);

        // Act
        var result = await _agentManager.GetAgentsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.ShouldContainAgentWithName("Agent1");
        result.ShouldContainAgentWithName("Agent2");
    }

    [Fact]
    public async Task GetAgentsAsync_WithPredicate_ShouldReturnFilteredAgents()
    {
        // Arrange
        var mockAgent1 = MockFactory.CreateAgent("Agent1", AgentType.Analyzer);
        var mockAgent2 = MockFactory.CreateAgent("Agent2", AgentType.Fixer);
        var mockAgent3 = MockFactory.CreateAgent("Agent3", AgentType.Analyzer);
        await _agentManager.RegisterAgentAsync(mockAgent1.Object);
        await _agentManager.RegisterAgentAsync(mockAgent2.Object);
        await _agentManager.RegisterAgentAsync(mockAgent3.Object);

        // Act
        var result = await _agentManager.GetAgentsAsync(a => a.Type == AgentType.Analyzer);

        // Assert
        result.Should().HaveCount(2);
        result.ShouldContainAgentWithName("Agent1");
        result.ShouldContainAgentWithName("Agent3");
        result.Should().NotContain(a => a.Name == "Agent2");
    }

    [Fact]
    public async Task GetAgentStatusAsync_WithExistingAgent_ShouldReturnStatus()
    {
        // Arrange
        var mockAgent = MockFactory.CreateAgent("TestAgent", AgentType.Analyzer);
        await _agentManager.RegisterAgentAsync(mockAgent.Object);

        // Act
        var status = await _agentManager.GetAgentStatusAsync("TestAgent");

        // Assert
        status.Should().NotBeNull();
        status.Name.Should().Be("TestAgent");
        status.Type.Should().Be(AgentType.Analyzer);
    }

    [Fact]
    public async Task GetAgentStatusAsync_WithNonExistentAgent_ShouldReturnNull()
    {
        // Act
        var status = await _agentManager.GetAgentStatusAsync("NonExistentAgent");

        // Assert
        status.Should().BeNull();
    }

    [Fact]
    public async Task AgentManager_ConcurrentOperations_ShouldHandleThreadSafety()
    {
        // Arrange
        var tasks = new List<Task>();
        var agentCount = 10;

        // Act - Register multiple agents concurrently
        for (int i = 0; i < agentCount; i++)
        {
            var agentName = $"Agent{i}";
            var mockAgent = MockFactory.CreateAgent(agentName, AgentType.Analyzer);
            tasks.Add(_agentManager.RegisterAgentAsync(mockAgent.Object));
        }

        await Task.WhenAll(tasks);

        // Assert
        var allAgents = await _agentManager.GetAgentsAsync();
        allAgents.Should().HaveCount(agentCount);
    }

    [Fact]
    public async Task AgentManager_LifecycleTest_ShouldWorkEndToEnd()
    {
        // Arrange
        var mockAgent = MockFactory.CreateAgent("LifecycleAgent", AgentType.Refactor);

        // Act & Assert - Register
        await _agentManager.RegisterAgentAsync(mockAgent.Object);
        var registeredAgent = await _agentManager.GetAgentAsync("LifecycleAgent");
        registeredAgent.Should().NotBeNull();

        // Act & Assert - Get Status
        var status = await _agentManager.GetAgentStatusAsync("LifecycleAgent");
        status.Should().NotBeNull();
        status.ShouldBeHealthy();

        // Act & Assert - Unregister
        await _agentManager.UnregisterAgentAsync("LifecycleAgent");
        var unregisteredAgent = await _agentManager.GetAgentAsync("LifecycleAgent");
        unregisteredAgent.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new AgentManager(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}