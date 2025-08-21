using Xunit;
using FluentAssertions;
using A3sist.Shared.Models;
using A3sist.Shared.Enums;
using A3sist.Shared.Messaging;

namespace A3sist.TestUtilities;

/// <summary>
/// Tests to verify the test infrastructure is working correctly
/// </summary>
public class TestInfrastructureTests : TestBase
{
    [Fact]
    public void TestBase_ShouldProvideBasicServices()
    {
        // Arrange & Act
        var logger = GetOptionalService<Microsoft.Extensions.Logging.ILogger>();
        var config = GetOptionalService<Microsoft.Extensions.Configuration.IConfiguration>();

        // Assert
        logger.Should().NotBeNull();
        config.Should().NotBeNull();
    }

    [Fact]
    public void MockFactory_ShouldCreateValidAgentMock()
    {
        // Arrange & Act
        var mockAgent = MockFactory.CreateAgent("TestAgent", AgentType.Analyzer);

        // Assert
        mockAgent.Should().NotBeNull();
        mockAgent.Object.Name.Should().Be("TestAgent");
        mockAgent.Object.Type.Should().Be(AgentType.Analyzer);
    }

    [Fact]
    public void TestDataBuilder_ShouldCreateValidAgentRequest()
    {
        // Arrange & Act
        var request = TestDataBuilder.AgentRequest()
            .WithPrompt("Test prompt")
            .WithFilePath("test.cs")
            .WithUserId("test-user")
            .Build();

        // Assert
        request.ShouldBeValid();
        request.Prompt.Should().Be("Test prompt");
        request.FilePath.Should().Be("test.cs");
        request.UserId.Should().Be("test-user");
    }

    [Fact]
    public void TestDataBuilder_ShouldCreateValidAgentResult()
    {
        // Arrange & Act
        var result = TestDataBuilder.AgentResult()
            .WithSuccess(true)
            .WithMessage("Test successful")
            .WithContent("Test content")
            .WithAgentName("TestAgent")
            .Build();

        // Assert
        result.ShouldBeSuccessful();
        result.Message.Should().Be("Test successful");
        result.Content.Should().Be("Test content");
        result.AgentName.Should().Be("TestAgent");
    }

    [Fact]
    public void AssertionExtensions_ShouldValidateSuccessfulResult()
    {
        // Arrange
        var result = new AgentResult
        {
            Success = true,
            Message = "Operation completed",
            Content = "Result content",
            Exception = null
        };

        // Act & Assert
        result.ShouldBeSuccessful();
    }

    [Fact]
    public void AssertionExtensions_ShouldValidateFailedResult()
    {
        // Arrange
        var result = new AgentResult
        {
            Success = false,
            Message = "Operation failed",
            Exception = new InvalidOperationException("Test error")
        };

        // Act & Assert
        result.ShouldBeFailed();
    }

    [Fact]
    public async Task AsyncTestHelpers_ShouldMeasureExecutionTime()
    {
        // Arrange
        var expectedDelay = TimeSpan.FromMilliseconds(100);

        // Act
        var (result, duration) = await AsyncTestHelpers.MeasureAsync(async () =>
        {
            await Task.Delay(expectedDelay);
            return "Test result";
        });

        // Assert
        result.Should().Be("Test result");
        duration.Should().BeGreaterOrEqualTo(expectedDelay);
        duration.Should().BeLessThan(expectedDelay.Add(TimeSpan.FromMilliseconds(50))); // Allow some tolerance
    }

    [Fact]
    public async Task AsyncTestHelpers_ShouldWaitForCondition()
    {
        // Arrange
        var conditionMet = false;
        var timeout = TimeSpan.FromSeconds(1);

        // Start a task that will set the condition after a delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(200);
            conditionMet = true;
        });

        // Act
        var result = await AsyncTestHelpers.WaitForConditionAsync(
            () => conditionMet, 
            timeout, 
            TimeSpan.FromMilliseconds(50));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void TestConfiguration_ShouldProvideDefaultValues()
    {
        // Arrange & Act
        var config = TestConfig.Build();

        // Assert
        config["A3sist:Agents:Orchestrator:Enabled"].Should().Be("true");
        config["A3sist:LLM:Provider"].Should().Be("Test");
        config["A3sist:Logging:Level"].Should().Be("Information");
    }
}