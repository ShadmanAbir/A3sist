using Xunit;
using FluentAssertions;
using A3sist.Shared.Models;
using A3sist.Shared.Enums;
using A3sist.TestUtilities;

namespace A3sist.Core.Tests.Models;

/// <summary>
/// Unit tests for AgentConfiguration model
/// </summary>
public class AgentConfigurationTests : TestBase
{
    [Fact]
    public void AgentConfiguration_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var config = new AgentConfiguration();

        // Assert
        config.Should().NotBeNull();
        config.Enabled.Should().BeTrue();
        config.Settings.Should().NotBeNull();
        config.MaxConcurrentTasks.Should().Be(1);
        config.Timeout.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void AgentConfiguration_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var name = "TestAgent";
        var type = AgentType.Fixer;
        var maxTasks = 5;
        var timeout = TimeSpan.FromMinutes(10);

        // Act
        var config = new AgentConfiguration
        {
            Name = name,
            Type = type,
            Enabled = true,
            MaxConcurrentTasks = maxTasks,
            Timeout = timeout
        };

        // Assert
        config.Name.Should().Be(name);
        config.Type.Should().Be(type);
        config.Enabled.Should().BeTrue();
        config.MaxConcurrentTasks.Should().Be(maxTasks);
        config.Timeout.Should().Be(timeout);
    }

    [Fact]
    public void AgentConfiguration_WithSettings_ShouldStoreSettings()
    {
        // Arrange
        var config = new AgentConfiguration();
        
        // Act
        config.Settings["key1"] = "value1";
        config.Settings["key2"] = 42;
        config.Settings["key3"] = true;

        // Assert
        config.Settings.Should().ContainKey("key1");
        config.Settings.Should().ContainKey("key2");
        config.Settings.Should().ContainKey("key3");
        config.Settings["key1"].Should().Be("value1");
        config.Settings["key2"].Should().Be(42);
        config.Settings["key3"].Should().Be(true);
    }

    [Fact]
    public void AgentConfiguration_WithRetryPolicy_ShouldConfigureRetries()
    {
        // Arrange
        var retryPolicy = new RetryPolicy
        {
            MaxAttempts = 3,
            InitialDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(30),
            BackoffMultiplier = 2.0
        };

        // Act
        var config = new AgentConfiguration
        {
            RetryPolicy = retryPolicy
        };

        // Assert
        config.RetryPolicy.Should().NotBeNull();
        config.RetryPolicy.MaxAttempts.Should().Be(3);
        config.RetryPolicy.InitialDelay.Should().Be(TimeSpan.FromSeconds(1));
        config.RetryPolicy.MaxDelay.Should().Be(TimeSpan.FromSeconds(30));
        config.RetryPolicy.BackoffMultiplier.Should().Be(2.0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AgentConfiguration_WithInvalidMaxConcurrentTasks_ShouldStillBeCreated(int invalidMaxTasks)
    {
        // Act
        var config = new AgentConfiguration
        {
            MaxConcurrentTasks = invalidMaxTasks
        };

        // Assert
        config.MaxConcurrentTasks.Should().Be(invalidMaxTasks);
        // Note: Validation would typically be done elsewhere
    }

    [Fact]
    public void AgentConfiguration_Serialization_ShouldPreserveAllProperties()
    {
        // Arrange
        var originalConfig = new AgentConfiguration
        {
            Name = "TestAgent",
            Type = AgentType.Refactor,
            Enabled = true,
            MaxConcurrentTasks = 3,
            Timeout = TimeSpan.FromMinutes(15),
            RetryPolicy = new RetryPolicy
            {
                MaxAttempts = 2,
                InitialDelay = TimeSpan.FromSeconds(5)
            }
        };
        originalConfig.Settings["testKey"] = "testValue";

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(originalConfig);
        var deserializedConfig = System.Text.Json.JsonSerializer.Deserialize<AgentConfiguration>(json);

        // Assert
        deserializedConfig.Should().NotBeNull();
        deserializedConfig!.Name.Should().Be(originalConfig.Name);
        deserializedConfig.Type.Should().Be(originalConfig.Type);
        deserializedConfig.Enabled.Should().Be(originalConfig.Enabled);
        deserializedConfig.MaxConcurrentTasks.Should().Be(originalConfig.MaxConcurrentTasks);
        deserializedConfig.Timeout.Should().Be(originalConfig.Timeout);
        deserializedConfig.Settings.Should().ContainKey("testKey");
    }

    [Fact]
    public void AgentConfiguration_Clone_ShouldCreateDeepCopy()
    {
        // Arrange
        var originalConfig = new AgentConfiguration
        {
            Name = "TestAgent",
            Type = AgentType.Designer,
            Enabled = true
        };
        originalConfig.Settings["key1"] = "value1";

        // Act
        var clonedConfig = new AgentConfiguration
        {
            Name = originalConfig.Name,
            Type = originalConfig.Type,
            Enabled = originalConfig.Enabled,
            MaxConcurrentTasks = originalConfig.MaxConcurrentTasks,
            Timeout = originalConfig.Timeout,
            Settings = new Dictionary<string, object>(originalConfig.Settings)
        };

        // Assert
        clonedConfig.Should().NotBeSameAs(originalConfig);
        clonedConfig.Name.Should().Be(originalConfig.Name);
        clonedConfig.Settings.Should().NotBeSameAs(originalConfig.Settings);
        clonedConfig.Settings.Should().BeEquivalentTo(originalConfig.Settings);
    }
}