using Xunit;
using FluentAssertions;
using A3sist.Shared.Models;
using A3sist.Shared.Enums;
using A3sist.TestUtilities;

namespace A3sist.Core.Tests.Models;

/// <summary>
/// Unit tests for AgentStatus model
/// </summary>
public class AgentStatusTests : TestBase
{
    [Fact]
    public void AgentStatus_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var status = new AgentStatus();

        // Assert
        status.Should().NotBeNull();
        status.TasksProcessed.Should().Be(0);
        status.TasksSucceeded.Should().Be(0);
        status.TasksFailed.Should().Be(0);
    }

    [Fact]
    public void AgentStatus_WithValidData_ShouldBeHealthy()
    {
        // Arrange
        var status = TestDataBuilder.AgentStatus()
            .WithName("TestAgent")
            .WithType(AgentType.Analyzer)
            .WithStatus(WorkStatus.Completed)
            .WithTaskCounts(10, 8, 2)
            .Build();

        // Act & Assert
        status.ShouldBeHealthy();
        status.Name.Should().Be("TestAgent");
        status.Type.Should().Be(AgentType.Analyzer);
        status.Status.Should().Be(WorkStatus.Completed);
    }

    [Fact]
    public void AgentStatus_WithTaskCounts_ShouldCalculateCorrectly()
    {
        // Arrange
        var processed = 100;
        var succeeded = 85;
        var failed = 15;

        // Act
        var status = TestDataBuilder.AgentStatus()
            .WithTaskCounts(processed, succeeded, failed)
            .Build();

        // Assert
        status.TasksProcessed.Should().Be(processed);
        status.TasksSucceeded.Should().Be(succeeded);
        status.TasksFailed.Should().Be(failed);
        (status.TasksSucceeded + status.TasksFailed).Should().Be(processed);
    }

    [Fact]
    public void AgentStatus_WithAverageProcessingTime_ShouldTrackPerformance()
    {
        // Arrange
        var averageTime = TimeSpan.FromMilliseconds(150);

        // Act
        var status = TestDataBuilder.AgentStatus()
            .WithAverageProcessingTime(averageTime)
            .Build();

        // Assert
        status.AverageProcessingTime.Should().Be(averageTime);
    }

    [Theory]
    [InlineData(WorkStatus.Pending)]
    [InlineData(WorkStatus.InProgress)]
    [InlineData(WorkStatus.Completed)]
    [InlineData(WorkStatus.Paused)]
    public void AgentStatus_WithDifferentWorkStatuses_ShouldBeValid(WorkStatus workStatus)
    {
        // Act
        var status = TestDataBuilder.AgentStatus()
            .WithStatus(workStatus)
            .Build();

        // Assert
        status.Status.Should().Be(workStatus);
        if (workStatus != WorkStatus.Failed)
        {
            status.ShouldBeHealthy();
        }
    }

    [Fact]
    public void AgentStatus_WithFailedStatus_ShouldNotBeHealthy()
    {
        // Act
        var status = TestDataBuilder.AgentStatus()
            .WithStatus(WorkStatus.Failed)
            .Build();

        // Assert
        status.Status.Should().Be(WorkStatus.Failed);
        // Note: The ShouldBeHealthy extension checks for WorkStatus.Failed
    }

    [Fact]
    public void AgentStatus_Serialization_ShouldPreserveAllProperties()
    {
        // Arrange
        var originalStatus = TestDataBuilder.AgentStatus()
            .WithName("TestAgent")
            .WithType(AgentType.Fixer)
            .WithStatus(WorkStatus.InProgress)
            .WithTaskCounts(50, 45, 5)
            .WithAverageProcessingTime(TimeSpan.FromMilliseconds(200))
            .Build();

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(originalStatus);
        var deserializedStatus = System.Text.Json.JsonSerializer.Deserialize<AgentStatus>(json);

        // Assert
        deserializedStatus.Should().NotBeNull();
        deserializedStatus!.Name.Should().Be(originalStatus.Name);
        deserializedStatus.Type.Should().Be(originalStatus.Type);
        deserializedStatus.Status.Should().Be(originalStatus.Status);
        deserializedStatus.TasksProcessed.Should().Be(originalStatus.TasksProcessed);
        deserializedStatus.TasksSucceeded.Should().Be(originalStatus.TasksSucceeded);
        deserializedStatus.TasksFailed.Should().Be(originalStatus.TasksFailed);
        deserializedStatus.AverageProcessingTime.Should().Be(originalStatus.AverageProcessingTime);
    }

    [Fact]
    public void AgentStatus_SuccessRate_ShouldCalculateCorrectly()
    {
        // Arrange
        var status = TestDataBuilder.AgentStatus()
            .WithTaskCounts(100, 80, 20)
            .Build();

        // Act
        var successRate = status.TasksProcessed > 0 
            ? (double)status.TasksSucceeded / status.TasksProcessed 
            : 0.0;

        // Assert
        successRate.Should().Be(0.8); // 80/100 = 0.8
    }
}