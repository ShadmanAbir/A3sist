using Xunit;
using FluentAssertions;
using A3sist.Shared.Messaging;
using A3sist.TestUtilities;

namespace A3sist.Core.Tests.Models;

/// <summary>
/// Unit tests for AgentResult model
/// </summary>
public class AgentResultTests : TestBase
{
    [Fact]
    public void AgentResult_Success_ShouldCreateSuccessfulResult()
    {
        // Arrange
        var content = "Test content";
        var agentName = "TestAgent";

        // Act
        var result = AgentResult.Success(content, agentName);

        // Assert
        result.ShouldBeSuccessful();
        result.Content.Should().Be(content);
        result.AgentName.Should().Be(agentName);
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void AgentResult_Error_ShouldCreateFailedResult()
    {
        // Arrange
        var message = "Test error";
        var agentName = "TestAgent";
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = AgentResult.Error(message, agentName, exception);

        // Assert
        result.ShouldBeFailed();
        result.Message.Should().Be(message);
        result.AgentName.Should().Be(agentName);
        result.Exception.Should().Be(exception);
    }

    [Fact]
    public void AgentResult_WithMetadata_ShouldStoreMetadata()
    {
        // Arrange
        var result = TestDataBuilder.AgentResult()
            .WithMetadata("key1", "value1")
            .WithMetadata("key2", 42)
            .Build();

        // Act & Assert
        result.ShouldHaveMetadata("key1", "value1");
        result.ShouldHaveMetadata("key2", 42);
    }

    [Fact]
    public void AgentResult_WithProcessingTime_ShouldTrackPerformance()
    {
        // Arrange
        var processingTime = TimeSpan.FromMilliseconds(250);

        // Act
        var result = TestDataBuilder.AgentResult()
            .WithProcessingTime(processingTime)
            .Build();

        // Assert
        result.ShouldHaveReasonableProcessingTime(TimeSpan.FromSeconds(1));
        result.ProcessingTime.Should().Be(processingTime);
    }

    [Fact]
    public void AgentResult_WithException_ShouldContainExceptionDetails()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument");

        // Act
        var result = TestDataBuilder.AgentResult()
            .WithException(exception)
            .Build();

        // Assert
        result.ShouldContainException<ArgumentException>();
        result.Exception.Message.Should().Be("Invalid argument");
    }

    [Fact]
    public void AgentResult_Serialization_ShouldPreserveAllProperties()
    {
        // Arrange
        var originalResult = TestDataBuilder.AgentResult()
            .WithSuccess(true)
            .WithMessage("Test message")
            .WithContent("Test content")
            .WithAgentName("TestAgent")
            .WithProcessingTime(TimeSpan.FromMilliseconds(100))
            .WithMetadata("testKey", "testValue")
            .Build();

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(originalResult);
        var deserializedResult = System.Text.Json.JsonSerializer.Deserialize<AgentResult>(json);

        // Assert
        deserializedResult.Should().NotBeNull();
        deserializedResult!.Success.Should().Be(originalResult.Success);
        deserializedResult.Message.Should().Be(originalResult.Message);
        deserializedResult.Content.Should().Be(originalResult.Content);
        deserializedResult.AgentName.Should().Be(originalResult.AgentName);
        deserializedResult.ProcessingTime.Should().Be(originalResult.ProcessingTime);
        deserializedResult.Metadata.Should().ContainKey("testKey");
    }

    [Theory]
    [InlineData(true, "Success message")]
    [InlineData(false, "Error message")]
    public void AgentResult_WithDifferentSuccessStates_ShouldBehaveCorrectly(bool success, string message)
    {
        // Act
        var result = TestDataBuilder.AgentResult()
            .WithSuccess(success)
            .WithMessage(message)
            .Build();

        // Assert
        result.Success.Should().Be(success);
        result.Message.Should().Be(message);
        
        if (success)
        {
            result.ShouldBeSuccessful();
        }
        else
        {
            result.ShouldBeFailed();
        }
    }
}