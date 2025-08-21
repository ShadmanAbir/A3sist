using Xunit;
using FluentAssertions;
using A3sist.Shared.Messaging;
using A3sist.Shared.Enums;
using A3sist.TestUtilities;

namespace A3sist.Core.Tests.Models;

/// <summary>
/// Unit tests for AgentRequest model
/// </summary>
public class AgentRequestTests : TestBase
{
    [Fact]
    public void AgentRequest_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new AgentRequest();

        // Assert
        request.Id.Should().NotBeEmpty();
        request.Context.Should().NotBeNull();
        request.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AgentRequest_WithValidData_ShouldBeValid()
    {
        // Arrange
        var request = TestDataBuilder.AgentRequest()
            .WithPrompt("Test prompt")
            .WithFilePath("test.cs")
            .WithContent("// Test content")
            .WithUserId("test-user")
            .Build();

        // Act & Assert
        request.ShouldBeValid();
    }

    [Fact]
    public void AgentRequest_WithContext_ShouldStoreContextData()
    {
        // Arrange
        var request = TestDataBuilder.AgentRequest()
            .WithContext("key1", "value1")
            .WithContext("key2", 42)
            .Build();

        // Act & Assert
        request.Context.Should().ContainKey("key1");
        request.Context.Should().ContainKey("key2");
        request.Context["key1"].Should().Be("value1");
        request.Context["key2"].Should().Be(42);
    }

    [Fact]
    public void AgentRequest_WithAgentType_ShouldSetPreferredAgentType()
    {
        // Arrange
        var expectedType = AgentType.Fixer;

        // Act
        var request = TestDataBuilder.AgentRequest()
            .WithAgentType(expectedType)
            .Build();

        // Assert
        request.PreferredAgentType.Should().Be(expectedType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AgentRequest_WithInvalidPrompt_ShouldStillBeCreated(string invalidPrompt)
    {
        // Act
        var request = TestDataBuilder.AgentRequest()
            .WithPrompt(invalidPrompt)
            .Build();

        // Assert
        request.Should().NotBeNull();
        request.Prompt.Should().Be(invalidPrompt);
    }

    [Fact]
    public void AgentRequest_Serialization_ShouldPreserveAllProperties()
    {
        // Arrange
        var originalRequest = TestDataBuilder.AgentRequest()
            .WithPrompt("Test prompt")
            .WithFilePath("test.cs")
            .WithContent("// Test content")
            .WithContext("testKey", "testValue")
            .WithUserId("test-user")
            .Build();

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(originalRequest);
        var deserializedRequest = System.Text.Json.JsonSerializer.Deserialize<AgentRequest>(json);

        // Assert
        deserializedRequest.Should().NotBeNull();
        deserializedRequest!.Id.Should().Be(originalRequest.Id);
        deserializedRequest.Prompt.Should().Be(originalRequest.Prompt);
        deserializedRequest.FilePath.Should().Be(originalRequest.FilePath);
        deserializedRequest.Content.Should().Be(originalRequest.Content);
        deserializedRequest.UserId.Should().Be(originalRequest.UserId);
        deserializedRequest.Context.Should().ContainKey("testKey");
    }
}