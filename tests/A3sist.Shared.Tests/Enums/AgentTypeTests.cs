using System.Linq;
using Xunit;
using FluentAssertions;
using A3sist.Shared.Enums;
using A3sist.TestUtilities;

namespace A3sist.Shared.Tests.Enums;

/// <summary>
/// Unit tests for AgentType enum
/// </summary>
public class AgentTypeTests : TestBase
{
    [Fact]
    public void AgentType_ShouldHaveExpectedValues()
    {
        // Act & Assert
        Enum.GetValues(typeof(AgentType)).Cast<AgentType>().Should().Contain(new[]
        {
            AgentType.Analyzer,
            AgentType.AutoCompleter,
            AgentType.Designer,
            AgentType.Fixer,
            AgentType.Refactor,
            AgentType.Chat,
            AgentType.Reasoning,
            AgentType.IntentRouter,
            AgentType.Unknown,
            AgentType.Dispatcher,
            AgentType.Language
        });
    }

    [Theory]
    [InlineData(AgentType.Analyzer, "Analyzer")]
    [InlineData(AgentType.AutoCompleter, "AutoCompleter")]
    [InlineData(AgentType.Designer, "Designer")]
    [InlineData(AgentType.Fixer, "Fixer")]
    [InlineData(AgentType.Refactor, "Refactor")]
    [InlineData(AgentType.Chat, "Chat")]
    [InlineData(AgentType.Reasoning, "Reasoning")]
    [InlineData(AgentType.IntentRouter, "IntentRouter")]
    [InlineData(AgentType.Unknown, "Unknown")]
    [InlineData(AgentType.Dispatcher, "Dispatcher")]
    [InlineData(AgentType.Language, "Language")]
    public void AgentType_ToString_ShouldReturnCorrectString(AgentType agentType, string expectedString)
    {
        // Act
        var result = agentType.ToString();

        // Assert
        result.Should().Be(expectedString);
    }

    [Theory]
    [InlineData("Analyzer", AgentType.Analyzer)]
    [InlineData("AutoCompleter", AgentType.AutoCompleter)]
    [InlineData("Designer", AgentType.Designer)]
    [InlineData("Fixer", AgentType.Fixer)]
    [InlineData("Refactor", AgentType.Refactor)]
    [InlineData("Chat", AgentType.Chat)]
    [InlineData("Reasoning", AgentType.Reasoning)]
    [InlineData("IntentRouter", AgentType.IntentRouter)]
    [InlineData("Unknown", AgentType.Unknown)]
    [InlineData("Dispatcher", AgentType.Dispatcher)]
    [InlineData("Language", AgentType.Language)]
    public void AgentType_Parse_ShouldReturnCorrectEnum(string input, AgentType expected)
    {
        // Act
        var result = (AgentType)Enum.Parse(typeof(AgentType), input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("analyzer")]
    [InlineData("ANALYZER")]
    [InlineData("Analyzer")]
    public void AgentType_Parse_IgnoreCase_ShouldWork(string input)
    {
        // Act
        var result = (AgentType)Enum.Parse(typeof(AgentType), input, ignoreCase: true);

        // Assert
        result.Should().Be(AgentType.Analyzer);
    }

    [Fact]
    public void AgentType_Parse_WithInvalidValue_ShouldThrowArgumentException()
    {
        // Act
        var act = () => (AgentType)Enum.Parse(typeof(AgentType), "InvalidType");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("Analyzer", true)]
    [InlineData("InvalidType", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void AgentType_TryParse_ShouldReturnExpectedResult(string input, bool expectedSuccess)
    {
        // Act
        var success = Enum.TryParse(input, out AgentType result);

        // Assert
        success.Should().Be(expectedSuccess);
        if (expectedSuccess)
        {
            result.Should().Be(AgentType.Analyzer);
        }
    }

    [Fact]
    public void AgentType_GetValues_ShouldReturnAllValues()
    {
        // Act
        var values = Enum.GetValues(typeof(AgentType)).Cast<AgentType>();

        // Assert
        values.Should().NotBeEmpty();
        values.Should().HaveCountGreaterThan(5); // We know there are at least 11 values
        values.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AgentType_GetNames_ShouldReturnAllNames()
    {
        // Act
        var names = Enum.GetNames(typeof(AgentType));

        // Assert
        names.Should().NotBeEmpty();
        names.Should().Contain("Analyzer");
        names.Should().Contain("Fixer");
        names.Should().Contain("Unknown");
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AgentType_IsDefined_ShouldWorkCorrectly()
    {
        // Act & Assert
        Enum.IsDefined(typeof(AgentType), AgentType.Analyzer).Should().BeTrue();
        Enum.IsDefined(typeof(AgentType), AgentType.Unknown).Should().BeTrue();
        Enum.IsDefined(typeof(AgentType), (AgentType)999).Should().BeFalse();
    }

    [Fact]
    public void AgentType_Serialization_ShouldPreserveValue()
    {
        // Arrange
        var originalType = AgentType.Designer;

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(originalType);
        var deserializedType = System.Text.Json.JsonSerializer.Deserialize<AgentType>(json);

        // Assert
        deserializedType.Should().Be(originalType);
    }

    [Fact]
    public void AgentType_InCollections_ShouldWorkCorrectly()
    {
        // Arrange
        var agentTypes = new List<AgentType>
        {
            AgentType.Analyzer,
            AgentType.Fixer,
            AgentType.Designer
        };

        // Act & Assert
        agentTypes.Should().Contain(AgentType.Analyzer);
        agentTypes.Should().NotContain(AgentType.Unknown);
        agentTypes.Should().HaveCount(3);
    }
}