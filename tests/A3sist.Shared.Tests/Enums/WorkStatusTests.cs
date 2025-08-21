using System.Linq;
using Xunit;
using FluentAssertions;
using A3sist.Shared.Enums;
using A3sist.TestUtilities;

namespace A3sist.Shared.Tests.Enums;

/// <summary>
/// Unit tests for WorkStatus enum
/// </summary>
public class WorkStatusTests : TestBase
{
    [Fact]
    public void WorkStatus_ShouldHaveExpectedValues()
    {
        // Act & Assert
        Enum.GetValues(typeof(WorkStatus)).Cast<WorkStatus>().Should().Contain(new[]
        {
            WorkStatus.Pending,
            WorkStatus.InProgress,
            WorkStatus.Completed,
            WorkStatus.Failed,
            WorkStatus.Cancelled,
            WorkStatus.Paused
        });
    }

    [Theory]
    [InlineData(WorkStatus.Pending, "Pending")]
    [InlineData(WorkStatus.InProgress, "InProgress")]
    [InlineData(WorkStatus.Completed, "Completed")]
    [InlineData(WorkStatus.Failed, "Failed")]
    [InlineData(WorkStatus.Cancelled, "Cancelled")]
    [InlineData(WorkStatus.Paused, "Paused")]
    public void WorkStatus_ToString_ShouldReturnCorrectString(WorkStatus workStatus, string expectedString)
    {
        // Act
        var result = workStatus.ToString();

        // Assert
        result.Should().Be(expectedString);
    }

    [Theory]
    [InlineData("Pending", WorkStatus.Pending)]
    [InlineData("InProgress", WorkStatus.InProgress)]
    [InlineData("Completed", WorkStatus.Completed)]
    [InlineData("Failed", WorkStatus.Failed)]
    [InlineData("Cancelled", WorkStatus.Cancelled)]
    [InlineData("Paused", WorkStatus.Paused)]
    public void WorkStatus_Parse_ShouldReturnCorrectEnum(string input, WorkStatus expected)
    {
        // Act
        var result = (WorkStatus)Enum.Parse(typeof(WorkStatus), input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("PENDING")]
    [InlineData("Pending")]
    public void WorkStatus_Parse_IgnoreCase_ShouldWork(string input)
    {
        // Act
        var result = (WorkStatus)Enum.Parse(typeof(WorkStatus), input, ignoreCase: true);

        // Assert
        result.Should().Be(WorkStatus.Pending);
    }

    [Fact]
    public void WorkStatus_Parse_WithInvalidValue_ShouldThrowArgumentException()
    {
        // Act
        var act = () => (WorkStatus)Enum.Parse(typeof(WorkStatus), "InvalidStatus");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("Completed", true)]
    [InlineData("InvalidStatus", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void WorkStatus_TryParse_ShouldReturnExpectedResult(string input, bool expectedSuccess)
    {
        // Act
        var success = Enum.TryParse(input, out WorkStatus result);

        // Assert
        success.Should().Be(expectedSuccess);
        if (expectedSuccess)
        {
            result.Should().Be(WorkStatus.Completed);
        }
    }

    [Fact]
    public void WorkStatus_GetValues_ShouldReturnAllValues()
    {
        // Act
        var values = Enum.GetValues(typeof(WorkStatus)).Cast<WorkStatus>();

        // Assert
        values.Should().NotBeEmpty();
        values.Should().HaveCount(6); // We know there are exactly 6 values
        values.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void WorkStatus_GetNames_ShouldReturnAllNames()
    {
        // Act
        var names = Enum.GetNames(typeof(WorkStatus));

        // Assert
        names.Should().NotBeEmpty();
        names.Should().Contain("Pending");
        names.Should().Contain("InProgress");
        names.Should().Contain("Completed");
        names.Should().Contain("Failed");
        names.Should().Contain("Cancelled");
        names.Should().Contain("Paused");
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void WorkStatus_IsDefined_ShouldWorkCorrectly()
    {
        // Act & Assert
        Enum.IsDefined(typeof(WorkStatus), WorkStatus.Pending).Should().BeTrue();
        Enum.IsDefined(typeof(WorkStatus), WorkStatus.Failed).Should().BeTrue();
        Enum.IsDefined(typeof(WorkStatus), (WorkStatus)999).Should().BeFalse();
    }

    [Fact]
    public void WorkStatus_Serialization_ShouldPreserveValue()
    {
        // Arrange
        var originalStatus = WorkStatus.InProgress;

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(originalStatus);
        var deserializedStatus = System.Text.Json.JsonSerializer.Deserialize<WorkStatus>(json);

        // Assert
        deserializedStatus.Should().Be(originalStatus);
    }

    [Fact]
    public void WorkStatus_InCollections_ShouldWorkCorrectly()
    {
        // Arrange
        var workStatuses = new List<WorkStatus>
        {
            WorkStatus.Pending,
            WorkStatus.InProgress,
            WorkStatus.Completed
        };

        // Act & Assert
        workStatuses.Should().Contain(WorkStatus.Pending);
        workStatuses.Should().NotContain(WorkStatus.Failed);
        workStatuses.Should().HaveCount(3);
    }

    [Theory]
    [InlineData(WorkStatus.Pending, false)]
    [InlineData(WorkStatus.InProgress, false)]
    [InlineData(WorkStatus.Completed, true)]
    [InlineData(WorkStatus.Failed, true)]
    [InlineData(WorkStatus.Cancelled, true)]
    [InlineData(WorkStatus.Paused, false)]
    public void WorkStatus_IsTerminalState_ShouldReturnCorrectValue(WorkStatus status, bool expectedIsTerminal)
    {
        // Act
        var isTerminal = status == WorkStatus.Completed || 
                        status == WorkStatus.Failed || 
                        status == WorkStatus.Cancelled;

        // Assert
        isTerminal.Should().Be(expectedIsTerminal);
    }

    [Theory]
    [InlineData(WorkStatus.Pending, true)]
    [InlineData(WorkStatus.InProgress, true)]
    [InlineData(WorkStatus.Paused, true)]
    [InlineData(WorkStatus.Completed, false)]
    [InlineData(WorkStatus.Failed, false)]
    [InlineData(WorkStatus.Cancelled, false)]
    public void WorkStatus_CanTransition_ShouldReturnCorrectValue(WorkStatus status, bool expectedCanTransition)
    {
        // Act
        var canTransition = status != WorkStatus.Completed && 
                           status != WorkStatus.Failed && 
                           status != WorkStatus.Cancelled;

        // Assert
        canTransition.Should().Be(expectedCanTransition);
    }
}