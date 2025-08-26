using A3sist.UI.Options;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace A3sist.UI.Services.Chat
{
    /// <summary>
    /// Interface for chat settings management
    /// </summary>
    public interface IChatSettingsService
    {
        ChatSettings GetSettings();
        Task SaveSettingsAsync(ChatSettings settings);
        Task ResetToDefaultsAsync();
        event EventHandler<ChatSettings> SettingsChanged;
    }

    /// <summary>
    /// Service for managing chat settings and preferences
    /// </summary>
    public class ChatSettingsService : IChatSettingsService, INotifyPropertyChanged
    {
        private readonly ILogger<ChatSettingsService> _logger;
        private readonly string _settingsPath;
        private ChatSettings _currentSettings;

        public event EventHandler<ChatSettings>? SettingsChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public ChatSettingsService(ILogger<ChatSettingsService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Set up settings file path
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var a3sistFolder = Path.Combine(appDataPath, "A3sist");
            Directory.CreateDirectory(a3sistFolder);
            _settingsPath = Path.Combine(a3sistFolder, "chat-settings.json");

            // Load settings
            _currentSettings = LoadSettingsFromFile();
        }

        /// <summary>
        /// Gets the current chat settings
        /// </summary>
        public ChatSettings GetSettings()
        {
            return _currentSettings.Clone();
        }

        /// <summary>
        /// Saves chat settings
        /// </summary>
        public async Task SaveSettingsAsync(ChatSettings settings)
        {
            try
            {
                _currentSettings = settings.Clone();
                await SaveSettingsToFileAsync();
                
                SettingsChanged?.Invoke(this, _currentSettings.Clone());
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChatSettings)));
                
                _logger.LogInformation("Chat settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving chat settings");
                throw;
            }
        }

        /// <summary>
        /// Resets settings to defaults
        /// </summary>
        public async Task ResetToDefaultsAsync()
        {
            try
            {
                _currentSettings = CreateDefaultSettings();
                await SaveSettingsToFileAsync();
                
                SettingsChanged?.Invoke(this, _currentSettings.Clone());
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChatSettings)));
                
                _logger.LogInformation("Chat settings reset to defaults");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting chat settings");
                throw;
            }
        }

        /// <summary>
        /// Loads settings from Visual Studio options page
        /// </summary>
        public ChatSettings LoadFromOptionsPage()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                
                var package = Package.GetGlobalService(typeof(A3sistPackage)) as A3sistPackage;
                if (package?.GetDialogPage(typeof(ChatOptionsPage)) is ChatOptionsPage optionsPage)
                {
                    return new ChatSettings
                    {
                        DefaultModel = optionsPage.DefaultModel,
                        MaxTokens = optionsPage.MaxTokens,
                        Temperature = optionsPage.Temperature,
                        EnableStreaming = optionsPage.EnableStreaming,
                        ShowSuggestions = optionsPage.ShowSuggestions,
                        AutoSave = optionsPage.AutoSave,
                        HistoryLimit = optionsPage.HistoryLimit,
                        ChatTheme = optionsPage.ChatTheme,
                        EnableNotifications = optionsPage.EnableNotifications,
                        EnableSounds = optionsPage.EnableSounds,
                        TypingDelay = optionsPage.TypingDelay
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load settings from options page, using defaults");
            }
            
            return CreateDefaultSettings();
        }

        /// <summary>
        /// Saves settings to Visual Studio options page
        /// </summary>
        public void SaveToOptionsPage(ChatSettings settings)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                
                var package = Package.GetGlobalService(typeof(A3sistPackage)) as A3sistPackage;
                if (package?.GetDialogPage(typeof(ChatOptionsPage)) is ChatOptionsPage optionsPage)
                {
                    optionsPage.DefaultModel = settings.DefaultModel;
                    optionsPage.MaxTokens = settings.MaxTokens;
                    optionsPage.Temperature = settings.Temperature;
                    optionsPage.EnableStreaming = settings.EnableStreaming;
                    optionsPage.ShowSuggestions = settings.ShowSuggestions;
                    optionsPage.AutoSave = settings.AutoSave;
                    optionsPage.HistoryLimit = settings.HistoryLimit;
                    optionsPage.ChatTheme = settings.ChatTheme;
                    optionsPage.EnableNotifications = settings.EnableNotifications;
                    optionsPage.EnableSounds = settings.EnableSounds;
                    optionsPage.TypingDelay = settings.TypingDelay;
                    
                    optionsPage.SaveSettingsToStorage();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not save settings to options page");
            }
        }

        private ChatSettings LoadSettingsFromFile()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var settings = JsonSerializer.Deserialize<ChatSettings>(json);
                    if (settings != null)
                    {
                        _logger.LogDebug("Loaded chat settings from file");
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load chat settings from file, using defaults");
            }

            // Try to load from VS options page as fallback
            var vsSettings = LoadFromOptionsPage();
            if (vsSettings != null)
            {
                return vsSettings;
            }

            return CreateDefaultSettings();
        }

        private async Task SaveSettingsToFileAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_currentSettings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                await File.WriteAllTextAsync(_settingsPath, json);
                _logger.LogDebug("Saved chat settings to file");
                
                // Also save to VS options page if available
                SaveToOptionsPage(_currentSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving chat settings to file");
                throw;
            }
        }

        private static ChatSettings CreateDefaultSettings()
        {
            return new ChatSettings
            {
                DefaultModel = "gpt-4",
                MaxTokens = 4000,
                Temperature = 0.7,
                EnableStreaming = true,
                ShowSuggestions = true,
                AutoSave = true,
                HistoryLimit = 100,
                ChatTheme = "Auto",
                EnableNotifications = true,
                EnableSounds = false,
                TypingDelay = 1500
            };
        }
    }

    /// <summary>
    /// Chat settings model
    /// </summary>
    public class ChatSettings : ICloneable
    {
        public string DefaultModel { get; set; } = "gpt-4";
        public int MaxTokens { get; set; } = 4000;
        public double Temperature { get; set; } = 0.7;
        public bool EnableStreaming { get; set; } = true;
        public bool ShowSuggestions { get; set; } = true;
        public bool AutoSave { get; set; } = true;
        public int HistoryLimit { get; set; } = 100;
        public string ChatTheme { get; set; } = "Auto";
        public bool EnableNotifications { get; set; } = true;
        public bool EnableSounds { get; set; } = false;
        public int TypingDelay { get; set; } = 1500;

        public ChatSettings Clone()
        {
            return new ChatSettings
            {
                DefaultModel = DefaultModel,
                MaxTokens = MaxTokens,
                Temperature = Temperature,
                EnableStreaming = EnableStreaming,
                ShowSuggestions = ShowSuggestions,
                AutoSave = AutoSave,
                HistoryLimit = HistoryLimit,
                ChatTheme = ChatTheme,
                EnableNotifications = EnableNotifications,
                EnableSounds = EnableSounds,
                TypingDelay = TypingDelay
            };
        }

        object ICloneable.Clone() => Clone();
    }
}