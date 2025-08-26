using A3sist.UI.Services.Chat;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.UI.Tests.Services.Chat
{
    /// <summary>
    /// Unit tests for ChatSettingsService
    /// </summary>
    public class ChatSettingsServiceTests : IDisposable
    {
        private readonly Mock<ILogger<ChatSettingsService>> _mockLogger;
        private readonly string _testSettingsPath;
        private readonly ChatSettingsService _settingsService;

        public ChatSettingsServiceTests()
        {
            _mockLogger = new Mock<ILogger<ChatSettingsService>>();
            
            // Use a temporary directory for test settings
            _testSettingsPath = Path.Combine(Path.GetTempPath(), "A3sistTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testSettingsPath);
            
            _settingsService = new ChatSettingsService(_mockLogger.Object);
        }

        [Fact]
        public void GetSettings_ShouldReturnDefaultSettings()
        {
            // Act
            var settings = _settingsService.GetSettings();

            // Assert
            settings.Should().NotBeNull();
            settings.DefaultModel.Should().Be("gpt-4");
            settings.MaxTokens.Should().Be(4000);
            settings.Temperature.Should().Be(0.7);
            settings.EnableStreaming.Should().BeTrue();
            settings.ShowSuggestions.Should().BeTrue();
            settings.AutoSave.Should().BeTrue();
            settings.HistoryLimit.Should().Be(100);
            settings.ChatTheme.Should().Be("Auto");
            settings.EnableNotifications.Should().BeTrue();
            settings.EnableSounds.Should().BeFalse();
            settings.TypingDelay.Should().Be(1500);
        }

        [Fact]
        public async Task SaveSettingsAsync_ShouldPersistSettings()
        {
            // Arrange
            var newSettings = new ChatSettings
            {
                DefaultModel = "gpt-3.5-turbo",
                MaxTokens = 2000,
                Temperature = 0.5,
                EnableStreaming = false,
                ShowSuggestions = false,
                ChatTheme = "Dark"
            };

            // Act
            await _settingsService.SaveSettingsAsync(newSettings);

            // Assert
            var retrievedSettings = _settingsService.GetSettings();
            retrievedSettings.DefaultModel.Should().Be("gpt-3.5-turbo");
            retrievedSettings.MaxTokens.Should().Be(2000);
            retrievedSettings.Temperature.Should().Be(0.5);
            retrievedSettings.EnableStreaming.Should().BeFalse();
            retrievedSettings.ShowSuggestions.Should().BeFalse();
            retrievedSettings.ChatTheme.Should().Be("Dark");
        }

        [Fact]
        public async Task SaveSettingsAsync_ShouldRaiseSettingsChangedEvent()
        {
            // Arrange
            var eventRaised = false;
            ChatSettings? receivedSettings = null;

            _settingsService.SettingsChanged += (sender, settings) =>
            {
                eventRaised = true;
                receivedSettings = settings;
            };

            var newSettings = new ChatSettings
            {
                DefaultModel = "claude-3-opus",
                MaxTokens = 3000
            };

            // Act
            await _settingsService.SaveSettingsAsync(newSettings);

            // Assert
            eventRaised.Should().BeTrue();
            receivedSettings.Should().NotBeNull();
            receivedSettings!.DefaultModel.Should().Be("claude-3-opus");
            receivedSettings.MaxTokens.Should().Be(3000);
        }

        [Fact]
        public async Task ResetToDefaultsAsync_ShouldRestoreDefaultSettings()
        {
            // Arrange - First change settings
            var modifiedSettings = new ChatSettings
            {
                DefaultModel = "custom-model",
                MaxTokens = 1000,
                Temperature = 0.1,
                EnableStreaming = false,
                ShowSuggestions = false
            };

            await _settingsService.SaveSettingsAsync(modifiedSettings);

            // Act - Reset to defaults
            await _settingsService.ResetToDefaultsAsync();

            // Assert
            var settings = _settingsService.GetSettings();
            settings.DefaultModel.Should().Be("gpt-4");
            settings.MaxTokens.Should().Be(4000);
            settings.Temperature.Should().Be(0.7);
            settings.EnableStreaming.Should().BeTrue();
            settings.ShowSuggestions.Should().BeTrue();
        }

        [Fact]
        public async Task ResetToDefaultsAsync_ShouldRaiseSettingsChangedEvent()
        {
            // Arrange
            var eventRaised = false;
            ChatSettings? receivedSettings = null;

            _settingsService.SettingsChanged += (sender, settings) =>
            {
                eventRaised = true;
                receivedSettings = settings;
            };

            // Act
            await _settingsService.ResetToDefaultsAsync();

            // Assert
            eventRaised.Should().BeTrue();
            receivedSettings.Should().NotBeNull();
            receivedSettings!.DefaultModel.Should().Be("gpt-4");
        }

        [Fact]
        public void ChatSettings_Clone_ShouldCreateIndependentCopy()
        {
            // Arrange
            var originalSettings = new ChatSettings
            {
                DefaultModel = "gpt-4",
                MaxTokens = 4000,
                Temperature = 0.7,
                EnableStreaming = true,
                ShowSuggestions = true
            };

            // Act
            var clonedSettings = originalSettings.Clone();

            // Assert
            clonedSettings.Should().NotBeSameAs(originalSettings);
            clonedSettings.DefaultModel.Should().Be(originalSettings.DefaultModel);
            clonedSettings.MaxTokens.Should().Be(originalSettings.MaxTokens);
            clonedSettings.Temperature.Should().Be(originalSettings.Temperature);
            clonedSettings.EnableStreaming.Should().Be(originalSettings.EnableStreaming);
            clonedSettings.ShowSuggestions.Should().Be(originalSettings.ShowSuggestions);

            // Modify cloned settings and verify original is unchanged
            clonedSettings.DefaultModel = "different-model";
            originalSettings.DefaultModel.Should().Be("gpt-4");
        }

        [Theory]
        [InlineData("gpt-4")]
        [InlineData("gpt-3.5-turbo")]
        [InlineData("claude-3-opus")]
        [InlineData("claude-3-sonnet")]
        [InlineData("codestral")]
        public async Task SaveSettingsAsync_WithDifferentModels_ShouldPersistCorrectModel(string model)
        {
            // Arrange
            var settings = new ChatSettings { DefaultModel = model };

            // Act
            await _settingsService.SaveSettingsAsync(settings);

            // Assert
            var retrievedSettings = _settingsService.GetSettings();
            retrievedSettings.DefaultModel.Should().Be(model);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(1.0)]
        [InlineData(1.5)]
        [InlineData(2.0)]
        public async Task SaveSettingsAsync_WithDifferentTemperatures_ShouldPersistCorrectTemperature(double temperature)
        {
            // Arrange
            var settings = new ChatSettings { Temperature = temperature };

            // Act
            await _settingsService.SaveSettingsAsync(settings);

            // Assert
            var retrievedSettings = _settingsService.GetSettings();
            retrievedSettings.Temperature.Should().Be(temperature);
        }

        [Theory]
        [InlineData("Auto")]
        [InlineData("Light")]
        [InlineData("Dark")]
        [InlineData("High Contrast")]
        public async Task SaveSettingsAsync_WithDifferentThemes_ShouldPersistCorrectTheme(string theme)
        {
            // Arrange
            var settings = new ChatSettings { ChatTheme = theme };

            // Act
            await _settingsService.SaveSettingsAsync(settings);

            // Assert
            var retrievedSettings = _settingsService.GetSettings();
            retrievedSettings.ChatTheme.Should().Be(theme);
        }

        [Fact]
        public void ChatSettings_ShouldImplementICloneable()
        {
            // Arrange
            var settings = new ChatSettings();

            // Act & Assert
            settings.Should().BeAssignableTo<ICloneable>();
            
            var cloned = ((ICloneable)settings).Clone();
            cloned.Should().BeOfType<ChatSettings>();
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testSettingsPath))
                {
                    Directory.Delete(_testSettingsPath, true);
                }
            }
            catch (Exception ex)
            {
                // Ignore cleanup errors in tests
                System.Diagnostics.Debug.WriteLine($"Test cleanup error: {ex.Message}");
            }
        }
    }
}