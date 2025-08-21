using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace A3sist.Core.Configuration.Providers;

/// <summary>
/// Configuration provider that reads from and writes to JSON files
/// </summary>
public class FileConfigurationProvider : IConfigurationProvider, IDisposable
{
    private readonly string _filePath;
    private readonly ILogger<FileConfigurationProvider> _logger;
    private readonly ConcurrentDictionary<string, object> _data;
    private readonly FileSystemWatcher _fileWatcher;
    private readonly object _lockObject = new object();
    private bool _disposed;

    public string Name => "FileConfigurationProvider";
    public int Priority => 100;
    public bool SupportsWrite => true;

    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    public FileConfigurationProvider(string filePath, ILogger<FileConfigurationProvider> logger)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _data = new ConcurrentDictionary<string, object>();

        // Set up file watcher
        var directory = Path.GetDirectoryName(_filePath);
        var fileName = Path.GetFileName(_filePath);
        
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
        {
            _fileWatcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };
            _fileWatcher.Changed += OnFileChanged;
        }

        // Load initial data
        _ = Task.Run(async () => await LoadAsync());
    }

    public async Task<T> GetValueAsync<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        try
        {
            if (_data.TryGetValue(key, out var value))
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

            return default(T);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get value for key: {Key}", key);
            return default(T);
        }
    }

    public async Task SetValueAsync<T>(string key, T value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        try
        {
            var oldValue = await GetValueAsync<T>(key);
            _data.AddOrUpdate(key, value, (k, v) => value);

            // Save to file
            await SaveAsync(_data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

            // Notify of change
            var changeArgs = new ConfigurationChangedEventArgs(key, ConfigurationChangeType.Updated)
            {
                Source = Name
            };
            changeArgs.ChangedKeys.Add(key);
            changeArgs.OldValues[key] = oldValue;
            changeArgs.NewValues[key] = value;

            ConfigurationChanged?.Invoke(this, changeArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set value for key: {Key}", key);
            throw;
        }
    }

    public async Task<Dictionary<string, object>> GetAllValuesAsync()
    {
        return await Task.FromResult(_data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
    }

    public async Task<Dictionary<string, object>> LoadAsync()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogInformation("Configuration file does not exist: {FilePath}", _filePath);
                return new Dictionary<string, object>();
            }

            var json = await File.ReadAllTextAsync(_filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new Dictionary<string, object>();
            }

            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (data != null)
            {
                lock (_lockObject)
                {
                    _data.Clear();
                    foreach (var kvp in data)
                    {
                        _data.TryAdd(kvp.Key, kvp.Value);
                    }
                }
            }

            _logger.LogInformation("Loaded {Count} configuration values from {FilePath}", data?.Count ?? 0, _filePath);
            return _data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from file: {FilePath}", _filePath);
            return new Dictionary<string, object>();
        }
    }

    public async Task SaveAsync(Dictionary<string, object> data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(data, options);
            await File.WriteAllTextAsync(_filePath, json);

            _logger.LogInformation("Saved {Count} configuration values to {FilePath}", data.Count, _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration to file: {FilePath}", _filePath);
            throw;
        }
    }

    public async Task<ConfigurationValidationResult> ValidateAsync()
    {
        var result = new ConfigurationValidationResult();

        try
        {
            // Check if file exists and is readable
            if (!File.Exists(_filePath))
            {
                result.AddWarning("File", $"Configuration file does not exist: {_filePath}");
                return result;
            }

            // Try to parse the JSON
            var json = await File.ReadAllTextAsync(_filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                result.AddWarning("Content", "Configuration file is empty");
                return result;
            }

            try
            {
                JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            }
            catch (JsonException ex)
            {
                result.AddError("Format", $"Invalid JSON format: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            result.AddError("Access", $"Cannot access configuration file: {ex.Message}");
        }

        return result;
    }

    public async Task ReloadAsync()
    {
        try
        {
            var data = await LoadAsync();
            
            var changeArgs = new ConfigurationChangedEventArgs("All", ConfigurationChangeType.Reloaded)
            {
                Source = Name
            };
            ConfigurationChanged?.Invoke(this, changeArgs);
            
            _logger.LogInformation("Configuration reloaded from file: {FilePath}", _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload configuration from file: {FilePath}", _filePath);
            throw;
        }
    }

    private async void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            // Debounce file changes
            await Task.Delay(500);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling file change event for: {FilePath}", _filePath);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _fileWatcher?.Dispose();
            _disposed = true;
        }
    }
}