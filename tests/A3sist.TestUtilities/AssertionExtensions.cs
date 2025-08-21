using FluentAssertions;
using FluentAssertions.Execution;
using A3sist.Shared.Models;
using A3sist.Shared.Enums;
using A3sist.Shared.Messaging;
using A3sist.Shared.Interfaces;

namespace A3sist.TestUtilities;

/// <summary>
/// Custom assertion extensions for A3sist domain objects
/// </summary>
public static class AssertionExtensions
{
    /// <summary>
    /// Asserts that an AgentResult represents a successful operation
    /// </summary>
    public static void ShouldBeSuccessful(this AgentResult result, string because = "")
    {
        using (new AssertionScope())
        {
            result.Should().NotBeNull(because);
            result.Success.Should().BeTrue(because);
            result.Exception.Should().BeNull(because);
            result.Message.Should().NotBeNullOrEmpty(because);
        }
    }

    /// <summary>
    /// Asserts that an AgentResult represents a failed operation
    /// </summary>
    public static void ShouldBeFailed(this AgentResult result, string because = "")
    {
        using (new AssertionScope())
        {
            result.Should().NotBeNull(because);
            result.Success.Should().BeFalse(because);
            result.Message.Should().NotBeNullOrEmpty(because);
        }
    }

    /// <summary>
    /// Asserts that an AgentResult has specific content
    /// </summary>
    public static void ShouldHaveContent(this AgentResult result, string expectedContent, string because = "")
    {
        result.Should().NotBeNull(because);
        result.Content.Should().Be(expectedContent, because);
    }

    /// <summary>
    /// Asserts that an AgentResult has metadata with a specific key and value
    /// </summary>
    public static void ShouldHaveMetadata(this AgentResult result, string key, object expectedValue, string because = "")
    {
        using (new AssertionScope())
        {
            result.Should().NotBeNull(because);
            result.Metadata.Should().ContainKey(key, because);
            result.Metadata[key].Should().Be(expectedValue, because);
        }
    }

    /// <summary>
    /// Asserts that an AgentRequest is valid
    /// </summary>
    public static void ShouldBeValid(this AgentRequest request, string because = "")
    {
        using (new AssertionScope())
        {
            request.Should().NotBeNull(because);
            request.Id.Should().NotBeEmpty(because);
            request.Prompt.Should().NotBeNullOrEmpty(because);
            request.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1), because);
        }
    }

    /// <summary>
    /// Asserts that an AgentStatus indicates a healthy agent
    /// </summary>
    public static void ShouldBeHealthy(this AgentStatus status, string because = "")
    {
        using (new AssertionScope())
        {
            status.Should().NotBeNull(because);
            status.Name.Should().NotBeNullOrEmpty(because);
            status.Status.Should().NotBe(WorkStatus.Failed, because);
            status.LastActivity.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5), because);
        }
    }

    /// <summary>
    /// Asserts that processing time is within acceptable limits
    /// </summary>
    public static void ShouldHaveReasonableProcessingTime(this AgentResult result, TimeSpan maxExpected, string because = "")
    {
        result.Should().NotBeNull(because);
        result.ProcessingTime.Should().BeLessOrEqualTo(maxExpected, because);
        result.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero, because);
    }

    /// <summary>
    /// Asserts that an exception is properly wrapped in an AgentResult
    /// </summary>
    public static void ShouldContainException<T>(this AgentResult result, string because = "") where T : Exception
    {
        using (new AssertionScope())
        {
            result.Should().NotBeNull(because);
            result.Success.Should().BeFalse(because);
            result.Exception.Should().NotBeNull(because);
            result.Exception.Should().BeOfType<T>(because);
        }
    }

    /// <summary>
    /// Asserts that a collection of agents contains an agent of a specific type
    /// </summary>
    public static void ShouldContainAgentOfType(this IEnumerable<IAgent> agents, AgentType expectedType, string because = "")
    {
        agents.Should().NotBeNull(because);
        agents.Should().Contain(a => a.Type == expectedType, because);
    }

    /// <summary>
    /// Asserts that a collection of agents contains an agent with a specific name
    /// </summary>
    public static void ShouldContainAgentWithName(this IEnumerable<IAgent> agents, string expectedName, string because = "")
    {
        agents.Should().NotBeNull(because);
        agents.Should().Contain(a => a.Name == expectedName, because);
    }
}