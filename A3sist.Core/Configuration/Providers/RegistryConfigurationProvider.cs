using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Collections.Concurrent;
using System.Text.Json;

namespace A3sist.Core.Configuration.Providers;

/// <summary>
/// Configuration provider that reads from and writes to Windows Registry
/// </summary>
public class RegistryConfigurationProvider : IConfigurationProvider, IDisposable
{
    private readonly string _registryPath;
    private readonly ILogger<RegistryConfigurationProvider> _logger;
    private readonly ConcurrentDictionary<string, object> _data;
    private readonly Timer _refreshTimer;
    private bool _disposed;

    public string Name => "RegistryConfigurationProvider";
    public int Priority => 200; // Higher priority than file provider
    public bool SupportsWrite => true;

    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    public RegistryConfigurationProvider(string registryPath, ILogger<RegistryConfigurationProvider> logger)
    {
        _registryPath = registryPath ?? throw new ArgumentNullException(nameof(registryPath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _data = new ConcurrentDictionary<string, object>();

        // Set up periodic refresh (every 60 seconds)
        _refreshTimer = new Timer(async _ => await ReloadAsync(), null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));

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
                if (value is T directValue)
                {
                    return directValue;
                }

                // Try to convert
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)value.ToString();
                }

                if (typeof(T) == typeof(int) && int.TryParse(value.ToString(), out var intValue))
                {
                    return (T)(object)intValue;
                }

                if (typeof(T) == typeof(bool) && bool.TryParse(value.ToString(), out var boolValue))
                {
                    return (T)(object)boolValue;
                }

                // Try JSON deserialization for complex types
                if (value is string jsonString)
                {
                    try
                    {
                        return JsonSerializer.Deserialize<T>(jsonString);
                    }
                    catch
                    {
                        // Fall through to conversion
                    }
                }

                return (T)Convert.ChangeType(value, typeof(T));
            }

            return default(T);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get registry value for key: {Key}", key);
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

            using var registryKey = Registry.CurrentUser.CreateSubKey(_registryPath);
            if (registryKey == null)
            {
                throw new InvalidOperationException($"Cannot create registry key: {_registryPath}");
            }

            object registryValue;
            RegistryValueKind valueKind;

            // Determine the appropriate registry value type
            switch (value)
            {
                case string stringValue:
                    registryValue = stringValue;
                    valueKind = RegistryValueKind.String;
                    break;
                case int intValue:
                    registryValue = intValue;
                    valueKind = RegistryValueKind.DWord;
                    break;
                case bool boolValue:
                    registryValue = boolValue ? 1 : 0;
                    valueKind = RegistryValueKind.DWord;
                    break;
                default:
                    // Serialize complex types as JSON
                    registryValue = JsonSerializer.Serialize(value);
                    valueKind = RegistryValueKind.String;
                    break;
            }

            registryKey.SetValue(key, registryValue, valueKind);
            _data.AddOrUpdate(key, value, (k, v) => value);

            // Notify of change
            var changeArgs = new ConfigurationChangedEventArgs(key, ConfigurationChangeType.Updated)
            {
                Source = Name
            };
            changeArgs.ChangedKeys.Add(key);
            changeArgs.OldValues[key] = oldValue;
            changeArgs.NewValues[key] = value;

            ConfigurationChanged?.Invoke(this, changeArgs);

            _logger.LogInformation("Set registry value for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set registry value for key: {Key}", key);
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
            var data = new Dictionary<string, object>();

            using var registryKey = Registry.CurrentUser.OpenSubKey(_registryPath);
            if (registryKey != null)
            {
                foreach (var valueName in registryKey.GetValueNames())
                {
                    if (!string.IsNullOrEmpty(valueName))
                    {
                        var value = registryKey.GetValue(valueName);
                        if (value != null)
                        {
                            data[valueName] = value;
                        }
                    }
                }

                // Update internal cache
                _data.Clear();
                foreach (var kvp in data)
                {
                    _data.TryAdd(kvp.Key, kvp.Value);
                }
            }

            _logger.LogInformation("Loaded {Count} configuration values from registry: {RegistryPath}", 
                data.Count, _registryPath);
            
            return await Task.FromResult(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from registry: {RegistryPath}", _registryPath);
            return new Dictionary<string, object>();
        }
    }

    public async Task SaveAsync(Dictionary<string, object> data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        try
        {
            using var registryKey = Registry.CurrentUser.CreateSubKey(_registryPath);
            if (registryKey == null)
            {
                throw new InvalidOperationException($"Cannot create registry key: {_registryPath}");
            }

            foreach (var kvp in data)
            {
                await SetValueAsync(kvp.Key, kvp.Value);
            }

            _logger.LogInformation("Saved {Count} configuration values to registry: {RegistryPath}", 
                data.Count, _registryPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration to registry: {RegistryPath}", _registryPath);
            throw;
        }
    }

    public async Task<ConfigurationValidationResult> ValidateAsync()
    {
        var result = new ConfigurationValidationResult();

        try
        {
            // Check if we can access the registry key
            using var registryKey = Registry.CurrentUser.OpenSubKey(_registryPath);
            if (registryKey == null)
            {
                result.AddWarning("Access", $"Registry key does not exist: {_registryPath}");
            }
        }
        catch (UnauthorizedAccessException)
        {
            result.AddError("Permission", $"No permission to access registry key: {_registryPath}");
        }
        catch (Exception ex)
        {
            result.AddError("Access", $"Cannot access registry key: {ex.Message}");
        }

        return await Task.FromResult(result);
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
            
            _logger.LogInformation("Configuration reloaded from registry: {RegistryPath}", _registryPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload configuration from registry: {RegistryPath}", _registryPath);
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _refreshTimer?.Dispose();
            _disposed = true;
        }
    }
}