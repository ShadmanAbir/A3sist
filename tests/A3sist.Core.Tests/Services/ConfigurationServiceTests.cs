using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using A3sist.Core.Services;
using A3sist.Shared.Models;
using A3sist.TestUtilities;

namespace A3sist.Core.Tests.Services;

/// <summary>
/// Unit tests for ConfigurationService
/// </summary>
public class ConfigurationServiceTests : TestBase
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<ConfigurationService>> _mockLogger;
    private readonly ConfigurationService _configurationService;

    public ConfigurationServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<ConfigurationService>>();
        
        _configurationService = new ConfigurationService(
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void GetValue_WithExistingKey_ShouldReturnValue()
    {
        // Arrange
        var key = "TestKey";
        var expectedValue = "TestValue";
        _mockConfiguration.Setup(x => x[key]).Returns(expectedValue);

        // Act
        var result = _configurationService.GetValue(key);

        // Assert
        result.Should().Be(expectedValue);
        _mockConfiguration.Verify(x => x[key], Times.Once);
    }

    [Fact]
    public void GetValue_WithNonExistentKey_ShouldReturnNull()
    {
        // Arrange
        var key = "NonExistentKey";
        _mockConfiguration.Setup(x => x[key]).Returns((string?)null);

        // Act
        var result = _configurationService.GetValue(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetValue_WithDefaultValue_ShouldReturnDefaultWhenKeyNotFound()
    {
        // Arrange
        var key = "NonExistentKey";
        var defaultValue = "DefaultValue";
        _mockConfiguration.Setup(x => x[key]).Returns((string?)null);

        // Act
        var result = _configurationService.GetValue(key, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void GetValue_WithDefaultValue_ShouldReturnActualValueWhenKeyExists()
    {
        // Arrange
        var key = "ExistingKey";
        var actualValue = "ActualValue";
        var defaultValue = "DefaultValue";
        _mockConfiguration.Setup(x => x[key]).Returns(actualValue);

        // Act
        var result = _configurationService.GetValue(key, defaultValue);

        // Assert
        result.Should().Be(actualValue);
        result.Should().NotBe(defaultValue);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("True", true)]
    [InlineData("False", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public void GetBoolValue_WithValidBooleanStrings_ShouldParseCorrectly(string configValue, bool expected)
    {
        // Arrange
        var key = "BoolKey";
        _mockConfiguration.Setup(x => x[key]).Returns(configValue);

        // Act
        var result = _configurationService.GetBoolValue(key);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetBoolValue_WithInvalidValue_ShouldReturnFalse()
    {
        // Arrange
        var key = "InvalidBoolKey";
        _mockConfiguration.Setup(x => x[key]).Returns("invalid");

        // Act
        var result = _configurationService.GetBoolValue(key);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("123", 123)]
    [InlineData("0", 0)]
    [InlineData("-456", -456)]
    public void GetIntValue_WithValidIntegers_ShouldParseCorrectly(string configValue, int expected)
    {
        // Arrange
        var key = "IntKey";
        _mockConfiguration.Setup(x => x[key]).Returns(configValue);

        // Act
        var result = _configurationService.GetIntValue(key);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetIntValue_WithInvalidValue_ShouldReturnZero()
    {
        // Arrange
        var key = "InvalidIntKey";
        _mockConfiguration.Setup(x => x[key]).Returns("invalid");

        // Act
        var result = _configurationService.GetIntValue(key);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void GetIntValue_WithDefaultValue_ShouldReturnDefaultWhenInvalid()
    {
        // Arrange
        var key = "InvalidIntKey";
        var defaultValue = 42;
        _mockConfiguration.Setup(x => x[key]).Returns("invalid");

        // Act
        var result = _configurationService.GetIntValue(key, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public async Task SetValueAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var key = "TestKey";
        var value = "TestValue";

        // Act
        var act = async () => await _configurationService.SetValueAsync(key, value);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ReloadAsync_ShouldCompleteSuccessfully()
    {
        // Act
        var act = async () => await _configurationService.ReloadAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnValidationResult()
    {
        // Act
        var result = await _configurationService.ValidateAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue(); // Default implementation should be valid
    }

    [Fact]
    public void GetSection_ShouldReturnConfigurationSection()
    {
        // Arrange
        var sectionName = "TestSection";
        var mockSection = new Mock<IConfigurationSection>();
        _mockConfiguration.Setup(x => x.GetSection(sectionName)).Returns(mockSection.Object);

        // Act
        var result = _configurationService.GetSection(sectionName);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(mockSection.Object);
        _mockConfiguration.Verify(x => x.GetSection(sectionName), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void GetValue_WithInvalidKey_ShouldHandleGracefully(string invalidKey)
    {
        // Arrange
        _mockConfiguration.Setup(x => x[invalidKey]).Returns((string?)null);

        // Act
        var result = _configurationService.GetValue(invalidKey);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new ConfigurationService(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new ConfigurationService(_mockConfiguration.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}