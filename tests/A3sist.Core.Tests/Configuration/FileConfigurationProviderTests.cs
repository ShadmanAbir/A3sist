using A3sist.Core.Configuration.Providers;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace A3sist.Core.Tests.Configuration;

public class FileConfigurationProviderTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly Mock<ILogger<FileConfigurationProvider>> _mockLogger;
    private readonly FileConfigurationProvider _provider;

    public FileConfigurationProviderTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_config_{Guid.NewGuid()}.json");
        _mockLogger = new Mock<ILogger<FileConfigurationProvider>>();
        _provider = new FileConfigurationProvider(_testFilePath, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        // Assert
        Assert.Equal("FileConfigurationProvider", _provider.Name);
        Assert.Equal(100, _provider.Priority);
        Assert.True(_provider.SupportsWrite);
    }

    [Fact]
    public void Constructor_WithNullFilePath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new FileConfigurationProvider(null, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new FileConfigurationProvider(_testFilePath, null));
    }

    [Fact]
    public async Task LoadAsync_WithNonExistentFile_ReturnsEmptyDictionary()
    {
        // Ensure file doesn't exist
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }

        // Act
        var result = await _provider.LoadAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadAsync_WithValidJsonFile_ReturnsData()
    {
        // Arrange
        var testData = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 42 },
            { "key3", true }
        };

        var json = JsonSerializer.Serialize(testData);
        await File.WriteAllTextAsync(_testFilePath, json);

        // Act
        var result = await _provider.LoadAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains("key1", result.Keys);
        Assert.Contains("key2", result.Keys);
        Assert.Contains("key3", result.Keys);
    }

    [Fact]
    public async Task LoadAsync_WithInvalidJson_ReturnsEmptyDictionary()
    {
        // Arrange
        await File.WriteAllTextAsync(_testFilePath, "invalid json content");

        // Act
        var result = await _provider.LoadAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SaveAsync_WithValidData_WritesToFile()
    {
        // Arrange
        var testData = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 42 }
        };

        // Act
        await _provider.SaveAsync(testData);

        // Assert
        Assert.True(File.Exists(_testFilePath));
        
        var json = await File.ReadAllTextAsync(_testFilePath);
        var savedData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        
        Assert.NotNull(savedData);
        Assert.Equal(2, savedData.Count);
        Assert.Contains("key1", savedData.Keys);
        Assert.Contains("key2", savedData.Keys);
    }

    [Fact]
    public async Task SaveAsync_WithNullData_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _provider.SaveAsync(null));
    }

    [Fact]
    public async Task GetValueAsync_WithExistingKey_ReturnsValue()
    {
        // Arrange
        var testData = new Dictionary<string, object>
        {
            { "testKey", "testValue" }
        };
        await _provider.SaveAsync(testData);

        // Act
        var result = await _provider.GetValueAsync<string>("testKey");

        // Assert
        Assert.Equal("testValue", result);
    }

    [Fact]
    public async Task GetValueAsync_WithNonExistentKey_ReturnsDefault()
    {
        // Act
        var result = await _provider.GetValueAsync<string>("nonExistentKey");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetValueAsync_WithNullKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _provider.GetValueAsync<string>(null));
    }

    [Fact]
    public async Task SetValueAsync_WithValidKey_SetsAndSavesValue()
    {
        // Arrange
        const string key = "newKey";
        const string value = "newValue";

        // Act
        await _provider.SetValueAsync(key, value);

        // Assert
        var retrievedValue = await _provider.GetValueAsync<string>(key);
        Assert.Equal(value, retrievedValue);
        
        // Verify file was updated
        Assert.True(File.Exists(_testFilePath));
    }

    [Fact]
    public async Task SetValueAsync_WithNullKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _provider.SetValueAsync<string>(null, "value"));
    }

    [Fact]
    public async Task GetAllValuesAsync_ReturnsAllValues()
    {
        // Arrange
        var testData = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };
        await _provider.SaveAsync(testData);

        // Act
        var result = await _provider.GetAllValuesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("key1", result.Keys);
        Assert.Contains("key2", result.Keys);
    }

    [Fact]
    public async Task ValidateAsync_WithValidFile_ReturnsValid()
    {
        // Arrange
        var testData = new Dictionary<string, object> { { "key", "value" } };
        await _provider.SaveAsync(testData);

        // Act
        var result = await _provider.ValidateAsync();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_WithNonExistentFile_ReturnsWarning()
    {
        // Ensure file doesn't exist
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }

        // Act
        var result = await _provider.ValidateAsync();

        // Assert
        Assert.True(result.IsValid);
        Assert.Single(result.Warnings);
        Assert.Contains("does not exist", result.Warnings[0].Message);
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidJson_ReturnsError()
    {
        // Arrange
        await File.WriteAllTextAsync(_testFilePath, "invalid json");

        // Act
        var result = await _provider.ValidateAsync();

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("Invalid JSON format", result.Errors[0].Message);
    }

    [Fact]
    public async Task ReloadAsync_ReloadsDataFromFile()
    {
        // Arrange
        var initialData = new Dictionary<string, object> { { "key1", "value1" } };
        await _provider.SaveAsync(initialData);

        // Modify file directly
        var modifiedData = new Dictionary<string, object> { { "key1", "modifiedValue" } };
        var json = JsonSerializer.Serialize(modifiedData);
        await File.WriteAllTextAsync(_testFilePath, json);

        // Act
        await _provider.ReloadAsync();

        // Assert
        var reloadedValue = await _provider.GetValueAsync<string>("key1");
        Assert.Equal("modifiedValue", reloadedValue);
    }

    [Fact]
    public async Task ConfigurationChanged_EventRaised_WhenValueSet()
    {
        // Arrange
        ConfigurationChangedEventArgs receivedArgs = null;
        _provider.ConfigurationChanged += (sender, args) => receivedArgs = args;

        // Act
        await _provider.SetValueAsync("testKey", "testValue");

        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal("testKey", receivedArgs.ConfigurationName);
        Assert.Equal(ConfigurationChangeType.Updated, receivedArgs.ChangeType);
        Assert.Equal("FileConfigurationProvider", receivedArgs.Source);
    }

    public void Dispose()
    {
        _provider?.Dispose();
        
        if (File.Exists(_testFilePath))
        {
            try
            {
                File.Delete(_testFilePath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}