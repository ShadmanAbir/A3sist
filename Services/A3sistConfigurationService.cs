using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using A3sist.Models;

namespace A3sist.Services
{
    public class A3sistConfigurationService : IA3sistConfigurationService
    {
        private readonly string _configPath;
        private Dictionary<string, object> _settings;
        private readonly object _lockObject = new object();

        public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;

        public A3sistConfigurationService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var a3sistPath = Path.Combine(appDataPath, "A3sist");
            Directory.CreateDirectory(a3sistPath);
            _configPath = Path.Combine(a3sistPath, "config.json");
            _settings = new Dictionary<string, object>();
        }

        public async Task<bool> LoadConfigurationAsync()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    await CreateDefaultConfigurationAsync();
                    return true;
                }

                var json = File.ReadAllText(_configPath);
                var settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                lock (_lockObject)
                {
                    _settings.Clear();
                    foreach (var kvp in settings)
                    {
                        _settings[kvp.Key] = ConvertJsonElement(kvp.Value);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                // Log error and create default configuration
                await CreateDefaultConfigurationAsync();
                return false;
            }
        }

        public async Task<bool> SaveConfigurationAsync()
        {
            try
            {
                Dictionary<string, object> settingsToSave;
                lock (_lockObject)
                {
                    settingsToSave = new Dictionary<string, object>(_settings);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(settingsToSave, options);
                File.WriteAllText(_configPath, json);
                return true;
            }
            catch (Exception ex)
            {
                // Log error
                return false;
            }
        }

        public async Task<T> GetSettingAsync<T>(string key, T defaultValue = default)
        {
            await Task.CompletedTask; // Make it async for future database operations

            lock (_lockObject)
            {
                if (_settings.TryGetValue(key, out var value))
                {
                    try
                    {
                        if (value is JsonElement element)
                        {
                            return JsonSerializer.Deserialize<T>(element.GetRawText());
                        }
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }
                return defaultValue;
            }
        }

        public async Task SetSettingAsync<T>(string key, T value)
        {
            await Task.CompletedTask; // Make it async for future database operations

            object oldValue;
            lock (_lockObject)
            {
                _settings.TryGetValue(key, out oldValue);
                _settings[key] = value;
            }

            ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
            {
                Key = key,
                OldValue = oldValue,
                NewValue = value
            });

            // Auto-save on setting change
            await SaveConfigurationAsync();
        }

        private async Task CreateDefaultConfigurationAsync()
        {
            var defaultSettings = new Dictionary<string, object>
            {
                // General Settings
                ["general.preferredMode"] = "Hybrid",
                ["general.fallbackEnabled"] = true,
                ["general.privacyLevel"] = "Balanced",
                ["general.autoUpdates"] = true,
                ["general.telemetryEnabled"] = false,
                ["general.theme"] = "Auto",
                ["general.language"] = "en-US",

                // Model Settings
                ["models.activeModelId"] = "",
                ["models.multiModelEnabled"] = false,
                ["models.fallbackModel"] = "",
                ["models.maxTokens"] = 2048,
                ["models.temperature"] = 0.7,

                // MCP Settings
                ["mcp.autoDiscovery"] = true,
                ["mcp.healthCheckInterval"] = 60,
                ["mcp.timeout"] = 30,

                // RAG Settings
                ["rag.enabled"] = true,
                ["rag.maxResults"] = 10,
                ["rag.similarityThreshold"] = 0.7,
                ["rag.autoIndex"] = true,

                // Chat Settings
                ["chat.maxHistory"] = 100,
                ["chat.saveHistory"] = true,
                ["chat.showTimestamps"] = true,
                ["chat.showModelUsed"] = true,

                // AutoComplete Settings
                ["autocomplete.enabled"] = true,
                ["autocomplete.triggerCharacterCount"] = 3,
                ["autocomplete.maxSuggestions"] = 10,
                ["autocomplete.minConfidence"] = 0.6,
                ["autocomplete.includeSnippets"] = true,
                ["autocomplete.autoInsert"] = false,

                // Refactoring Settings
                ["refactoring.previewEnabled"] = true,
                ["refactoring.autoApprove"] = false,
                ["refactoring.backupEnabled"] = true,

                // Code Analysis Settings
                ["analysis.realTimeAnalysis"] = true,
                ["analysis.showInlineErrors"] = true,
                ["analysis.showSuggestions"] = true,

                // UI Settings
                ["ui.chatWindowDocked"] = true,
                ["ui.showNotifications"] = true,
                ["ui.animationsEnabled"] = true
            };

            lock (_lockObject)
            {
                _settings = defaultSettings;
            }

            await SaveConfigurationAsync();
        }

        private object ConvertJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue))
                        return intValue;
                    if (element.TryGetDouble(out double doubleValue))
                        return doubleValue;
                    return element.GetDecimal();
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.GetBoolean();
                case JsonValueKind.Array:
                case JsonValueKind.Object:
                    return element; // Keep as JsonElement for complex types
                case JsonValueKind.Null:
                    return null;
                default:
                    return element.ToString();
            }
        }
    }
}