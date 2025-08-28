using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using A3sist.UI.Models;

namespace A3sist.UI.Services
{
    /// <summary>
    /// Local JSON configuration service for A3sist UI
    /// Stores configuration in %AppData%\A3sist\config.json
    /// </summary>
    public interface IA3sistConfigurationService
    {
        Task<T> GetSettingAsync<T>(string key, T defaultValue = default);
        Task SetSettingAsync<T>(string key, T value);
        Task<bool> HasSettingAsync(string key);
        Task RemoveSettingAsync(string key);
        Task<Dictionary<string, object>> GetAllSettingsAsync();
        Task SaveSettingsAsync(Dictionary<string, object> settings);
        Task ResetToDefaultsAsync();
        
        event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
    }

    public class A3sistConfigurationService : IA3sistConfigurationService
    {
        private readonly string _configPath;
        private readonly string _configDirectory;
        private Dictionary<string, object> _settings;
        private readonly object _lock = new object();
        private bool _isLoaded = false;
        
        public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;

        public A3sistConfigurationService()
        {
            // Store configuration in %AppData%\A3sist\config.json
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _configDirectory = Path.Combine(appDataPath, "A3sist");
            _configPath = Path.Combine(_configDirectory, "config.json");
            _settings = new Dictionary<string, object>();
            
            // Ensure directory exists
            Directory.CreateDirectory(_configDirectory);
            
            // Load settings immediately
            _ = Task.Run(LoadConfigurationAsync);
        }

        public async Task<T> GetSettingAsync<T>(string key, T defaultValue = default)
        {
            await EnsureLoadedAsync();
            
            lock (_lock)
            {
                if (_settings.TryGetValue(key, out var value))
                {
                    try
                    {
                        if (value is JsonElement jsonElement)
                        {
                            return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
                        }
                        
                        if (value is T directValue)
                        {
                            return directValue;
                        }
                        
                        // Try to convert
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch
                    {
                        // If conversion fails, return default
                        return defaultValue;
                    }
                }
                
                return defaultValue;
            }
        }

        public async Task SetSettingAsync<T>(string key, T value)
        {
            await EnsureLoadedAsync();
            
            object oldValue;
            lock (_lock)
            {
                _settings.TryGetValue(key, out oldValue);
                _settings[key] = value;
            }
            
            // Save to disk
            await SaveConfigurationAsync();
            
            // Raise event
            ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
            {
                Key = key,
                OldValue = oldValue,
                NewValue = value
            });
        }

        public async Task<bool> HasSettingAsync(string key)
        {
            await EnsureLoadedAsync();
            
            lock (_lock)
            {
                return _settings.ContainsKey(key);
            }
        }

        public async Task RemoveSettingAsync(string key)
        {
            await EnsureLoadedAsync();
            
            object oldValue = null;
            bool removed;
            
            lock (_lock)
            {
                _settings.TryGetValue(key, out oldValue);
                removed = _settings.Remove(key);
            }
            
            if (removed)
            {
                await SaveConfigurationAsync();
                
                ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
                {
                    Key = key,
                    OldValue = oldValue,
                    NewValue = null
                });
            }
        }

        public async Task<Dictionary<string, object>> GetAllSettingsAsync()
        {
            await EnsureLoadedAsync();
            
            lock (_lock)
            {
                return new Dictionary<string, object>(_settings);
            }
        }

        public async Task SaveSettingsAsync(Dictionary<string, object> settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            
            lock (_lock)
            {
                _settings = new Dictionary<string, object>(settings);
            }
            
            await SaveConfigurationAsync();
            
            // Raise event for bulk change
            ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
            {
                Key = "*", // Indicates bulk change
                OldValue = null,
                NewValue = settings
            });
        }

        public async Task ResetToDefaultsAsync()
        {
            var defaultSettings = GetDefaultSettings();
            await SaveSettingsAsync(defaultSettings);
        }

        private async Task EnsureLoadedAsync()
        {
            if (!_isLoaded)
            {
                await LoadConfigurationAsync();
            }
        }

        private async Task LoadConfigurationAsync()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    using var stream = new FileStream(_configPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
                    using var document = await JsonDocument.ParseAsync(stream);
                    
                    var newSettings = new Dictionary<string, object>();
                    
                    foreach (var property in document.RootElement.EnumerateObject())
                    {
                        newSettings[property.Name] = property.Value.Clone();
                    }
                    
                    lock (_lock)
                    {
                        _settings = newSettings;
                        _isLoaded = true;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"A3sist configuration loaded from {_configPath}");
                }
                else
                {
                    // Create default configuration
                    lock (_lock)
                    {
                        _settings = GetDefaultSettings();
                        _isLoaded = true;
                    }
                    
                    await SaveConfigurationAsync();
                    System.Diagnostics.Debug.WriteLine($"A3sist default configuration created at {_configPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading A3sist configuration: {ex.Message}");
                
                // Fall back to default settings
                lock (_lock)
                {
                    _settings = GetDefaultSettings();
                    _isLoaded = true;
                }
            }
        }

        private async Task SaveConfigurationAsync()
        {
            try
            {
                Directory.CreateDirectory(_configDirectory);
                
                Dictionary<string, object> settingsToSave;
                lock (_lock)
                {
                    settingsToSave = new Dictionary<string, object>(_settings);
                }
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var json = JsonSerializer.Serialize(settingsToSave, options);
                
                using var stream = new FileStream(_configPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
                await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(json));
                await stream.FlushAsync();
                
                System.Diagnostics.Debug.WriteLine($"A3sist configuration saved to {_configPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving A3sist configuration: {ex.Message}");
            }
        }

        private Dictionary<string, object> GetDefaultSettings()
        {
            return new Dictionary<string, object>
            {
                // UI Settings
                ["ui.theme"] = "auto",
                ["ui.fontSize"] = 14,
                ["ui.showWelcome"] = true,
                ["ui.autoSave"] = true,
                
                // API Connection Settings
                ["api.baseUrl"] = "http://localhost:8341/api",
                ["api.timeout"] = 30000, // 30 seconds
                ["api.retryCount"] = 3,
                ["api.autoReconnect"] = true,
                
                // Feature Toggles
                ["features.autoComplete"] = true,
                ["features.realTimeAnalysis"] = true,
                ["features.ragEnabled"] = true,
                ["features.agentMode"] = true,
                ["features.mcpEnabled"] = false,
                
                // AutoComplete Settings
                ["autoComplete.enabled"] = true,
                ["autoComplete.maxSuggestions"] = 20,
                ["autoComplete.triggerDelay"] = 300,
                ["autoComplete.enableAI"] = true,
                
                // Chat Settings
                ["chat.maxHistory"] = 100,
                ["chat.autoSave"] = true,
                ["chat.showTimestamps"] = true,
                ["chat.enableMarkdown"] = true,
                
                // RAG Settings
                ["rag.enabled"] = true,
                ["rag.autoIndex"] = false,
                ["rag.maxResults"] = 10,
                ["rag.similarityThreshold"] = 0.6,
                
                // Agent Settings
                ["agent.enabled"] = true,
                ["agent.autoStart"] = false,
                ["agent.analysisDepth"] = "normal", // "quick", "normal", "thorough"
                ["agent.includeThirdParty"] = false,
                
                // Model Settings
                ["models.defaultProvider"] = "openai",
                ["models.temperature"] = 0.7,
                ["models.maxTokens"] = 4096,
                ["models.autoSelectBest"] = true,
                
                // Refactoring Settings
                ["refactoring.enabled"] = true,
                ["refactoring.autoApply"] = false,
                ["refactoring.showPreview"] = true,
                ["refactoring.confirmChanges"] = true,
                
                // Privacy Settings
                ["privacy.sendDiagnostics"] = true,
                ["privacy.shareUsageData"] = false,
                ["privacy.enableLogging"] = true,
                
                // Advanced Settings
                ["advanced.debugMode"] = false,
                ["advanced.verboseLogging"] = false,
                ["advanced.cacheSize"] = 100, // MB
                ["advanced.backgroundProcessing"] = true
            };
        }
    }
}