using A3sist.Core.Configuration;
using A3sist.Core.Services;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace A3sist.Core.Tests.Services;

public class ConfigurationServiceTests : IDisposable
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<ConfigurationService>> _mockLogger;
    private readonly Mock<IConfigurationProvider> _mockProvider1;
    private readonly Mock<IConfigurationProvider> _mockProvider2;
    private readonly ConfigurationService _configurationService;

    public ConfigurationServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<ConfigurationService>>();
        _mockProvider1 = new Mock<IConfigurationProvider>();
        _mockProvider2 = new Mock<IConfigurationProvider>();

        // Setup mock configuration
        var mockSection = new Mock<IConfigurationSection>();
        _mockConfiguration.Setup(c => c.GetSection(A3sistConfiguration.SectionName))
            .Returns(mockSection.Object);

        // Setup mock providers
        _mockProvider1.Setup(p => p.Name).Returns("Provider1");
        _mockProvider1.Setup(p => p.Priority).Returns(100);
        _mockProvider1.Setup(p => p.SupportsWrite).Returns(true);

        _mockProvider2.Setup(p => p.Name).Returns("Provider2");
        _mockProvider2.Setup(p => p.Priority).Returns(200);
        _mockProvider2.Setup(p => p.SupportsWrite).Returns(false);

        var providers = new[] { _mockProvider1.Object, _mockProvider2.Object };
        _configurationService = new ConfigurationService(_mockConfiguration.Object, _mockLogger.Object, providers);
    }

    [Fact]
    public async Task GetValueAsync_WithValidKey_ReturnsValue()
    {
        // Arrange
        const string key = "test.key";
        const string expectedValue = "test value";
        
        _mockProvider2.Setup(p => p.GetValueAsync<string>(key))
            .ReturnsAsync(expectedValue);

        // Act
        var result = await _configurationService.GetValueAsync<string>(key);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task GetValueAsync_WithInvalidKey_ReturnsDefault()
    {
        // Arrange
        const string key = "invalid.key";
        
        _mockProvider1.Setup(p => p.GetValueAsync<string>(key))
            .ReturnsAsync((string)null);
        _mockProvider2.Setup(p => p.GetValueAsync<string>(key))
            .ReturnsAsync((string)null);
        _mockConfiguration.Setup(c => c.GetValue<string>(key, It.IsAny<string>()))
            .Returns((string)null);

        // Act
        var result = await _configurationService.GetValueAsync<string>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetValueAsync_WithNullKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _configurationService.GetValueAsync<string>(null));
    }

    [Fact]
    public async Task SetValueAsync_WithValidKey_SetsValue()
    {
        // Arrange
        const string key = "test.key";
        const string value = "test value";
        
        _mockProvider1.Setup(p => p.SetValueAsync(key, value))
            .Returns(Task.CompletedTask);

        // Act
        await _configurationService.SetValueAsync(key, value);

        // Assert
        _mockProvider1.Verify(p => p.SetValueAsync(key, value), Times.Once);
    }

    [Fact]
    public async Task SetValueAsync_WithNullKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _configurationService.SetValueAsync<string>(null, "value"));
    }

    [Fact]
    public async Task GetAllValuesAsync_ReturnsAllValues()
    {
        // Arrange
        var provider1Values = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };
        
        var provider2Values = new Dictionary<string, object>
        {
            { "key3", "value3" },
            { "key4", "value4" }
        };

        _mockProvider1.Setup(p => p.GetAllValuesAsync())
            .ReturnsAsync(provider1Values);
        _mockProvider2.Setup(p => p.GetAllValuesAsync())
            .ReturnsAsync(provider2Values);

        var configValues = new List<KeyValuePair<string, string>>
        {
            new("config.key", "config.value")
        };
        _mockConfiguration.Setup(c => c.AsEnumerable())
            .Returns(configValues);

        // Act
        var result = await _configurationService.GetAllValuesAsync();

        // Assert
        Assert.Contains("key1", result.Keys);
        Assert.Contains("key2", result.Keys);
        Assert.Contains("key3", result.Keys);
        Assert.Contains("key4", result.Keys);
        Assert.Contains("config.key", result.Keys);
    }

    [Fact]
    public async Task ValidateAsync_WithValidConfiguration_ReturnsValid()
    {
        // Arrange
        var validationResult1 = new ConfigurationValidationResult { IsValid = true };
        var validationResult2 = new ConfigurationValidationResult { IsValid = true };

        _mockProvider1.Setup(p => p.ValidateAsync())
            .ReturnsAsync(validationResult1);
        _mockProvider2.Setup(p => p.ValidateAsync())
            .ReturnsAsync(validationResult2);

        // Act
        var result = await _configurationService.ValidateAsync();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidConfiguration_ReturnsInvalid()
    {
        // Arrange
        var validationResult1 = new ConfigurationValidationResult { IsValid = false };
        validationResult1.AddError("Property1", "Error message 1");

        var validationResult2 = new ConfigurationValidationResult { IsValid = true };

        _mockProvider1.Setup(p => p.ValidateAsync())
            .ReturnsAsync(validationResult1);
        _mockProvider2.Setup(p => p.ValidateAsync())
            .ReturnsAsync(validationResult2);

        // Act
        var result = await _configurationService.ValidateAsync();

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Error message 1", result.Errors[0].Message);
    }

    [Fact]
    public async Task ReloadAsync_CallsReloadOnAllProviders()
    {
        // Arrange
        _mockProvider1.Setup(p => p.ReloadAsync())
            .Returns(Task.CompletedTask);
        _mockProvider2.Setup(p => p.ReloadAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _configurationService.ReloadAsync();

        // Assert
        _mockProvider1.Verify(p => p.ReloadAsync(), Times.Once);
        _mockProvider2.Verify(p => p.ReloadAsync(), Times.Once);
    }

    [Fact]
    public async Task ConfigurationChanged_EventRaised_WhenProviderChanges()
    {
        // Arrange
        ConfigurationChangedEventArgs receivedArgs = null;
        _configurationService.ConfigurationChanged += (sender, args) => receivedArgs = args;

        var changeArgs = new ConfigurationChangedEventArgs("test.key", ConfigurationChangeType.Updated);

        // Act
        _mockProvider1.Raise(p => p.ConfigurationChanged += null, _mockProvider1.Object, changeArgs);

        // Wait a bit for async event handling
        await Task.Delay(100);

        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal("test.key", receivedArgs.ConfigurationName);
        Assert.Equal(ConfigurationChangeType.Updated, receivedArgs.ChangeType);
    }

    [Fact]
    public void GetA3sistConfiguration_ReturnsConfiguration()
    {
        // Act
        var result = _configurationService.GetA3sistConfiguration();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<A3sistConfiguration>(result);
    }

    [Fact]
    public void GetValue_SynchronousVersion_ReturnsValue()
    {
        // Arrange
        const string key = "test.key";
        const string expectedValue = "test value";
        
        _mockConfiguration.Setup(c => c.GetValue<string>(key, It.IsAny<string>()))
            .Returns(expectedValue);

        // Act
        var result = _configurationService.GetValue<string>(key);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public void GetSection_ReturnsConfigurationSection()
    {
        // Arrange
        const string key = "test.section";
        var mockSection = new Mock<IConfigurationSection>();
        
        _mockConfiguration.Setup(c => c.GetSection(key))
            .Returns(mockSection.Object);

        // Act
        var result = _configurationService.GetSection(key);

        // Assert
        Assert.Equal(mockSection.Object, result);
    }

    [Fact]
    public void KeyExists_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        const string key = "existing.key";
        
        _mockConfiguration.Setup(c => c[key])
            .Returns("some value");

        // Act
        var result = _configurationService.KeyExists(key);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void KeyExists_WithNonExistingKey_ReturnsFalse()
    {
        // Arrange
        const string key = "non.existing.key";
        
        _mockConfiguration.Setup(c => c[key])
            .Returns((string)null);

        // Act
        var result = _configurationService.KeyExists(key);

        // Assert
        Assert.False(result);
    }

    public void Dispose()
    {
        _configurationService?.Dispose();
    }
}