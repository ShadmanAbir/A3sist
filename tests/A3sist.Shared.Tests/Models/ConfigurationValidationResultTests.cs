using Xunit;
using FluentAssertions;
using A3sist.Shared.Models;
using A3sist.TestUtilities;

namespace A3sist.Shared.Tests.Models;

/// <summary>
/// Unit tests for ConfigurationValidationResult model
/// </summary>
public class ConfigurationValidationResultTests : TestBase
{
    [Fact]
    public void ConfigurationValidationResult_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new ConfigurationValidationResult();

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotBeNull();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().NotBeNull();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void AddError_WithValidData_ShouldAddErrorAndSetInvalid()
    {
        // Arrange
        var result = new ConfigurationValidationResult();
        var property = "TestProperty";
        var message = "Test error message";

        // Act
        result.AddError(property, message);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Property.Should().Be(property);
        result.Errors[0].Message.Should().Be(message);
    }

    [Fact]
    public void AddWarning_WithValidData_ShouldAddWarningButKeepValid()
    {
        // Arrange
        var result = new ConfigurationValidationResult();
        var property = "TestProperty";
        var message = "Test warning message";

        // Act
        result.AddWarning(property, message);

        // Assert
        result.IsValid.Should().BeTrue(); // Warnings don't make it invalid
        result.Warnings.Should().HaveCount(1);
        result.Warnings[0].Property.Should().Be(property);
        result.Warnings[0].Message.Should().Be(message);
    }

    [Fact]
    public void AddMultipleErrors_ShouldAccumulateErrors()
    {
        // Arrange
        var result = new ConfigurationValidationResult();

        // Act
        result.AddError("Property1", "Error 1");
        result.AddError("Property2", "Error 2");
        result.AddError("Property3", "Error 3");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain(e => e.Property == "Property1" && e.Message == "Error 1");
        result.Errors.Should().Contain(e => e.Property == "Property2" && e.Message == "Error 2");
        result.Errors.Should().Contain(e => e.Property == "Property3" && e.Message == "Error 3");
    }

    [Fact]
    public void AddMultipleWarnings_ShouldAccumulateWarnings()
    {
        // Arrange
        var result = new ConfigurationValidationResult();

        // Act
        result.AddWarning("Property1", "Warning 1");
        result.AddWarning("Property2", "Warning 2");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().HaveCount(2);
        result.Warnings.Should().Contain(w => w.Property == "Property1" && w.Message == "Warning 1");
        result.Warnings.Should().Contain(w => w.Property == "Property2" && w.Message == "Warning 2");
    }

    [Fact]
    public void GetErrorMessages_ShouldReturnAllErrorMessages()
    {
        // Arrange
        var result = new ConfigurationValidationResult();
        result.AddError("Property1", "Error 1");
        result.AddError("Property2", "Error 2");

        // Act
        var errorMessages = result.GetErrorMessages();

        // Assert
        errorMessages.Should().HaveCount(2);
        errorMessages.Should().Contain("Error 1");
        errorMessages.Should().Contain("Error 2");
    }

    [Fact]
    public void GetWarningMessages_ShouldReturnAllWarningMessages()
    {
        // Arrange
        var result = new ConfigurationValidationResult();
        result.AddWarning("Property1", "Warning 1");
        result.AddWarning("Property2", "Warning 2");

        // Act
        var warningMessages = result.GetWarningMessages();

        // Assert
        warningMessages.Should().HaveCount(2);
        warningMessages.Should().Contain("Warning 1");
        warningMessages.Should().Contain("Warning 2");
    }

    [Fact]
    public void GetErrorMessages_WithNoErrors_ShouldReturnEmptyArray()
    {
        // Arrange
        var result = new ConfigurationValidationResult();

        // Act
        var errorMessages = result.GetErrorMessages();

        // Assert
        errorMessages.Should().NotBeNull();
        errorMessages.Should().BeEmpty();
    }

    [Fact]
    public void GetWarningMessages_WithNoWarnings_ShouldReturnEmptyArray()
    {
        // Arrange
        var result = new ConfigurationValidationResult();

        // Act
        var warningMessages = result.GetWarningMessages();

        // Assert
        warningMessages.Should().NotBeNull();
        warningMessages.Should().BeEmpty();
    }

    [Fact]
    public void ConfigurationValidationResult_WithErrorsAndWarnings_ShouldBehaveCorrectly()
    {
        // Arrange
        var result = new ConfigurationValidationResult();

        // Act
        result.AddError("ErrorProperty", "This is an error");
        result.AddWarning("WarningProperty", "This is a warning");

        // Assert
        result.IsValid.Should().BeFalse(); // Errors make it invalid
        result.Errors.Should().HaveCount(1);
        result.Warnings.Should().HaveCount(1);
        result.GetErrorMessages().Should().Contain("This is an error");
        result.GetWarningMessages().Should().Contain("This is a warning");
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(null, null)]
    [InlineData("Property", "")]
    [InlineData("", "Message")]
    public void AddError_WithEdgeCaseInputs_ShouldStillWork(string property, string message)
    {
        // Arrange
        var result = new ConfigurationValidationResult();

        // Act
        result.AddError(property, message);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Property.Should().Be(property);
        result.Errors[0].Message.Should().Be(message);
    }

    [Fact]
    public void Serialization_ShouldPreserveAllProperties()
    {
        // Arrange
        var originalResult = new ConfigurationValidationResult();
        originalResult.AddError("ErrorProp", "Error message");
        originalResult.AddWarning("WarningProp", "Warning message");

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(originalResult);
        var deserializedResult = System.Text.Json.JsonSerializer.Deserialize<ConfigurationValidationResult>(json);

        // Assert
        deserializedResult.Should().NotBeNull();
        deserializedResult!.IsValid.Should().Be(originalResult.IsValid);
        deserializedResult.Errors.Should().HaveCount(originalResult.Errors.Count);
        deserializedResult.Warnings.Should().HaveCount(originalResult.Warnings.Count);
        deserializedResult.Errors[0].Property.Should().Be(originalResult.Errors[0].Property);
        deserializedResult.Errors[0].Message.Should().Be(originalResult.Errors[0].Message);
    }
}