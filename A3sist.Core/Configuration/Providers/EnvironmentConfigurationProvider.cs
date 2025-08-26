using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace A3sist.Core.Configuration.Providers;

/// <summary>
/// Configuration provider that reads from environment variables
/// </summary>
public class EnvironmentConfigurationProvider : IConfigurationProvider
{
    private readonly string _prefix;
    private readonly ILogger<EnvironmentConfigurationProvider> _logger;
    private readonly ConcurrentDictionary<string, object> _data;

    public string Name => "EnvironmentConfigurationProvider";
    public int Priority => 50; // Lower priority than file and registry
    public bool SupportsWrite => false; // Environment variables are typically read-only

    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    public EnvironmentConfigurationProvider(string prefix, ILogger<EnvironmentConfigurationProvider> logger)
    {
        _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _data = new ConcurrentDictionary<string, object>();

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

                if (typeof(T) == typeof(TimeSpan) && TimeSpan.TryParse(value.ToString(), out var timeSpanValue))
                {
                    return (T)(object)timeSpanValue;
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
            _logger.LogWarning(ex, "Failed to get environment variable for key: {Key}", key);
            return default(T);
        }
    }

    public async Task SetValueAsync<T>(string key, T value)
    {
        // Environment variables are typically read-only
        await Task.FromException(new NotSupportedException("Environment configuration provider does not support writing"));
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
            var environmentVariables = Environment.GetEnvironmentVariables();

            foreach (var key in environmentVariables.Keys)
            {
                var envKey = key.ToString();
                if (envKey.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase))
                {
                    // Remove prefix and convert to configuration key format
                    var configKey = envKey.Substring(_prefix.Length);
                    if (configKey.StartsWith("_"))
                    {
                        configKey = configKey.Substring(1);
                    }

                    // Convert environment variable naming (UPPER_CASE) to configuration naming (dot.notation)
                    configKey = configKey.Replace("__", ":").Replace("_", ".");

                    var value = environmentVariables[envKey];
                    if (value != null)
                    {
                        data[configKey] = value;
                    }
                }
            }

            // Update internal cache
            _data.Clear();
            foreach (var kvp in data)
            {
                _data.TryAdd(kvp.Key, kvp.Value);
            }

            _logger.LogInformation("Loaded {Count} configuration values from environment variables with prefix: {Prefix}", 
                data.Count, _prefix);
            
            return await Task.FromResult(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from environment variables");
            return new Dictionary<string, object>();
        }
    }

    public async Task SaveAsync(Dictionary<string, object> data)
    {
        // Environment variables are typically read-only
        await Task.FromException(new NotSupportedException("Environment configuration provider does not support writing"));
    }

    public async Task<ConfigurationValidationResult> ValidateAsync()
    {
        var result = new ConfigurationValidationResult();

        try
        {
            // Check if we can access environment variables
            var testVar = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(testVar))
            {
                result.AddWarning("Access", "Cannot access environment variables or PATH is not set");
            }

            // Check for required environment variables
            var requiredVars = new[] { $"{_prefix}_LLM_APIKEY", $"{_prefix}_LLM_PROVIDER" };
            foreach (var requiredVar in requiredVars)
            {
                var value = Environment.GetEnvironmentVariable(requiredVar);
                if (string.IsNullOrEmpty(value))
                {
                    result.AddWarning("Missing", $"Optional environment variable not set: {requiredVar}");
                }
            }
        }
        catch (Exception ex)
        {
            result.AddError("Access", $"Cannot access environment variables: {ex.Message}");
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
            
            _logger.LogInformation("Configuration reloaded from environment variables");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload configuration from environment variables");
            throw;
        }
    }
}