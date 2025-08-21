using A3sist.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace A3sist.Core.Tests.Services;

public class SettingsPersistenceServiceTests : IDisposable
{
    private readonly Mock<ILogger<SettingsPersistenceService>> _mockLogger;
    private readonly SettingsPersistenceService _service;
    private readonly string _testDirectory;

    public SettingsPersistenceServiceTests()
    {
        _mockLogger = new Mock<ILogger<SettingsPersistenceService>>();
        
        // Create a temporary directory for testing
        _testDirectory = Path.Combine(Path.GetTempPath(), $"A3sistTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        
        // Override the settings directory for testing
        Environment.SetEnvironmentVariable("APPDATA", _testDirectory);
        
        _service = new SettingsPersistenceService(_mockLogger.Object);
    }

    [Fact]
    public async Task SaveSettingsAsync_WithValidSettings_SavesSuccessfully()
    {
        // Arrange
        var settings = new Dictionary<string, object>
        {
            { "EnableA3sist", true },
            { "MaxConcurrentTasks", 5 },
            { "LogLevel", "Information" }
        };

        // Act
        await _service.SaveSettingsAsync(settings);

        // Assert
        var settingsPath = Path.Combine(_testDirectory, "A3sist", "settings.json");
        Assert.True(File.Exists(settingsPath));
        
        var json = await File.ReadAllTextAsync(settingsPath);
        Assert.False(string.IsNullOrEmpty(json));
        
        var settingsData = JsonSerializer.Deserialize<SettingsData>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        });
        
        Assert.NotNull(settingsData);
        Assert.Equal("1.2", settingsData.Version);
        Assert.NotNull(settingsData.Settings);
        Assert.Equal(3, settingsData.Settings.Count);
        Assert.False(string.IsNullOrEmpty(settingsData.Checksum));
    }

    [Fact]
    public async Task SaveSettingsAsync_WithNullSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.SaveSettingsAsync(null));
    }

    [Fact]
    public async Task LoadSettingsAsync_WithExistingSettings_LoadsSuccessfully()
    {
        // Arrange
        var originalSettings = new Dictionary<string, object>
        {
            { "EnableA3sist", true },
            { "MaxConcurrentTasks", 5 }
        };
        
        await _service.SaveSettingsAsync(originalSettings);

        // Act
        var loadedSettings = await _service.LoadSettingsAsync();

        // Assert
        Assert.NotNull(loadedSettings);
        Assert.Equal(2, loadedSettings.Count);
        Assert.True(loadedSettings.ContainsKey("EnableA3sist"));
        Assert.True(loadedSettings.ContainsKey("MaxConcurrentTasks"));
    }

    [Fact]
    public async Task LoadSettingsAsync_WithNonExistentFile_ReturnsEmptyDictionary()
    {
        // Act
        var loadedSettings = await _service.LoadSettingsAsync();

        // Assert
        Assert.NotNull(loadedSettings);
        Assert.Empty(loadedSettings);
    }

    [Fact]
    public async Task CreateBackupAsync_WithExistingSettings_CreatesBackup()
    {
        // Arrange
        var settings = new Dictionary<string, object>
        {
            { "TestSetting", "TestValue" }
        };
        await _service.SaveSettingsAsync(settings);

        // Act
        var backupPath = await _service.CreateBackupAsync();

        // Assert
        Assert.NotNull(backupPath);
        Assert.True(File.Exists(backupPath));
        
        var backupContent = await File.ReadAllTextAsync(backupPath);
        Assert.False(string.IsNullOrEmpty(backupContent));
    }

    [Fact]
    public async Task CreateBackupAsync_WithNoExistingSettings_ReturnsNull()
    {
        // Act
        var backupPath = await _service.CreateBackupAsync();

        // Assert
        Assert.Null(backupPath);
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithValidBackup_RestoresSuccessfully()
    {
        // Arrange
        var originalSettings = new Dictionary<string, object>
        {
            { "OriginalSetting", "OriginalValue" }
        };
        await _service.SaveSettingsAsync(originalSettings);
        var backupPath = await _service.CreateBackupAsync();

        // Modify settings
        var modifiedSettings = new Dictionary<string, object>
        {
            { "ModifiedSetting", "ModifiedValue" }
        };
        await _service.SaveSettingsAsync(modifiedSettings);

        // Act
        var result = await _service.RestoreFromBackupAsync(backupPath);

        // Assert
        Assert.True(result);
        
        var restoredSettings = await _service.LoadSettingsAsync();
        Assert.True(restoredSettings.ContainsKey("OriginalSetting"));
        Assert.False(restoredSettings.ContainsKey("ModifiedSetting"));
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.RestoreFromBackupAsync(null));
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithNonExistentFile_ReturnsFalse()
    {
        // Act
        var result = await _service.RestoreFromBackupAsync("nonexistent.json");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAvailableBackupsAsync_WithMultipleBackups_ReturnsOrderedList()
    {
        // Arrange
        var settings1 = new Dictionary<string, object> { { "Setting1", "Value1" } };
        var settings2 = new Dictionary<string, object> { { "Setting2", "Value2" } };
        
        await _service.SaveSettingsAsync(settings1);
        var backup1 = await _service.CreateBackupAsync();
        
        await Task.Delay(1000); // Ensure different timestamps
        
        await _service.SaveSettingsAsync(settings2);
        var backup2 = await _service.CreateBackupAsync();

        // Act
        var backups = await _service.GetAvailableBackupsAsync();

        // Assert
        Assert.NotNull(backups);
        Assert.Equal(2, backups.Count);
        
        // Should be ordered by creation time (newest first)
        Assert.True(backups[0].CreatedAt >= backups[1].CreatedAt);
        
        foreach (var backup in backups)
        {
            Assert.False(string.IsNullOrEmpty(backup.FilePath));
            Assert.False(string.IsNullOrEmpty(backup.FileName));
            Assert.False(string.IsNullOrEmpty(backup.Version));
            Assert.True(backup.Size > 0);
        }
    }

    [Fact]
    public async Task GetAvailableBackupsAsync_WithNoBackups_ReturnsEmptyList()
    {
        // Act
        var backups = await _service.GetAvailableBackupsAsync();

        // Assert
        Assert.NotNull(backups);
        Assert.Empty(backups);
    }

    [Fact]
    public async Task ValidateSettingsAsync_WithValidSettings_ReturnsValid()
    {
        // Arrange
        var settings = new Dictionary<string, object>
        {
            { "ValidSetting", "ValidValue" }
        };
        await _service.SaveSettingsAsync(settings);

        // Act
        var result = await _service.ValidateSettingsAsync();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateSettingsAsync_WithNonExistentFile_ReturnsWarning()
    {
        // Act
        var result = await _service.ValidateSettingsAsync();

        // Assert
        Assert.True(result.IsValid);
        Assert.Single(result.Warnings);
        Assert.Contains("does not exist", result.Warnings[0].Message);
    }

    [Fact]
    public async Task ValidateSettingsAsync_WithCorruptedFile_ReturnsError()
    {
        // Arrange
        var settingsPath = Path.Combine(_testDirectory, "A3sist", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
        await File.WriteAllTextAsync(settingsPath, "invalid json content");

        // Act
        var result = await _service.ValidateSettingsAsync();

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ValidateSettingsAsync_WithEmptyFile_ReturnsError()
    {
        // Arrange
        var settingsPath = Path.Combine(_testDirectory, "A3sist", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
        await File.WriteAllTextAsync(settingsPath, "");

        // Act
        var result = await _service.ValidateSettingsAsync();

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("empty", result.Errors[0].Message);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip_PreservesData()
    {
        // Arrange
        var originalSettings = new Dictionary<string, object>
        {
            { "StringValue", "test" },
            { "IntValue", 42 },
            { "BoolValue", true },
            { "DoubleValue", 3.14 },
            { "ArrayValue", new[] { "item1", "item2" } }
        };

        // Act
        await _service.SaveSettingsAsync(originalSettings);
        var loadedSettings = await _service.LoadSettingsAsync();

        // Assert
        Assert.Equal(originalSettings.Count, loadedSettings.Count);
        
        foreach (var kvp in originalSettings)
        {
            Assert.True(loadedSettings.ContainsKey(kvp.Key));
            // Note: JSON serialization may change types, so we compare string representations
            Assert.Equal(kvp.Value.ToString(), loadedSettings[kvp.Key].ToString());
        }
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}