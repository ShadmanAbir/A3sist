using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using A3sist.Core.Configuration;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace A3sist.Core.Services;

/// <summary>
/// Enhanced service for managing application configuration with multiple sources, validation, and change notifications
/// </summary>
public class ConfigurationService : IConfigurationService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly A3sist.Shared.Interfaces.IConfigurationProvider[] _configurationProviders;
    private readonly ConcurrentDictionary<string, object> _configurationCache;
    private readonly Timer _reloadTimer;
    private readonly object _lockObject = new object();
    private A3sistConfiguration _a3sistConfig;
    private bool _disposed;

    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    public ConfigurationService(
        IConfiguration configuration,
        ILogger<ConfigurationService> logger,
        IEnumerable<A3sist.Shared.Interfaces.IConfigurationProvider> configurationProviders = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configurationProviders = configurationProviders?.ToArray() ?? Array.Empty<IConfigurationProvider>();
        _configurationCache = new ConcurrentDictionary<string, object>();
        
        // Initialize configuration
        LoadConfiguration();
        
        // Set up periodic reload timer (every 30 seconds)
        _reloadTimer = new Timer(async _ => await ReloadAsync(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        
        _logger.LogInformation("Enhanced configuration service initialized with {ProviderCount} providers", _configurationProviders.Length);
    }

    /// <summary>
    /// Gets a configuration value by key asynchronously
    /// </summary>
    public async Task<T> GetValueAsync<T>(string key, T defaultValue = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        try
        {
            // Check cache first
            if (_configurationCache.TryGetValue(key, out var cachedValue) && cachedValue is T)
            {
                return (T)cachedValue;
            }

            // Try configuration providers first
            foreach (var provider in _configurationProviders)
            {
                try
                {
                    var value = await provider.GetValueAsync<T>(key);
                    if (value != null && !value.Equals(default(T)))
                    {
                        _configurationCache.TryAdd(key, value);
                        return value;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Provider {ProviderType} failed to get value for key: {Key}", 
                        provider.GetType().Name, key);
                }
            }

            // Fall back to built-in configuration
            var configValue = _configuration.GetValue<T>(key, defaultValue);
            if (configValue != null)
            {
                _configurationCache.TryAdd(key, configValue);
            }
            
            return configValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get configuration value for key: {Key}", key);
            return defaultValue;
        }
    }

    /// <summary>
    /// Sets a configuration value asynchronously
    /// </summary>
    public async Task SetValueAsync<T>(string key, T value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        try
        {
            var oldValue = await GetValueAsync<T>(key);
            
            // Try to set value using providers
            var success = false;
            foreach (var provider in _configurationProviders)
            {
                try
                {
                    await provider.SetValueAsync(key, value);
                    success = true;
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Provider {ProviderType} failed to set value for key: {Key}", 
                        provider.GetType().Name, key);
                }
            }

            if (!success)
            {
                _logger.LogWarning("No configuration provider could set value for key: {Key}", key);
                return;
            }

            // Update cache
            _configurationCache.AddOrUpdate(key, value, (k, v) => value);

            // Notify of change
            var changeArgs = new ConfigurationChangedEventArgs(key, ConfigurationChangeType.Updated)
            {
                Source = "ConfigurationService"
            };
            changeArgs.ChangedKeys.Add(key);
            changeArgs.OldValues[key] = oldValue;
            changeArgs.NewValues[key] = value;

            ConfigurationChanged?.Invoke(this, changeArgs);
            
            _logger.LogInformation("Configuration value updated for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set configuration value for key: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Gets all configuration values asynchronously
    /// </summary>
    public async Task<Dictionary<string, object>> GetAllValuesAsync()
    {
        try
        {
            var allValues = new Dictionary<string, object>();

            // Get values from built-in configuration
            foreach (var kvp in _configuration.AsEnumerable())
            {
                if (!string.IsNullOrEmpty(kvp.Key) && kvp.Value != null)
                {
                    allValues[kvp.Key] = kvp.Value;
                }
            }

            // Get values from providers
            foreach (var provider in _configurationProviders)
            {
                try
                {
                    var providerValues = await provider.GetAllValuesAsync();
                    foreach (var kvp in providerValues)
                    {
                        allValues[kvp.Key] = kvp.Value;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Provider {ProviderType} failed to get all values", 
                        provider.GetType().Name);
                }
            }

            return allValues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all configuration values");
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Validates the configuration asynchronously
    /// </summary>
    public async Task<ConfigurationValidationResult> ValidateAsync()
    {
        var result = new ConfigurationValidationResult();

        try
        {
            // Validate A3sist configuration
            await ValidateA3sistConfiguration(result);

            // Validate using providers
            foreach (var provider in _configurationProviders)
            {
                try
                {
                    var providerResult = await provider.ValidateAsync();
                    result.Errors.AddRange(providerResult.Errors);
                    result.Warnings.AddRange(providerResult.Warnings);
                    if (!providerResult.IsValid)
                    {
                        result.IsValid = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Provider {ProviderType} validation failed", 
                        provider.GetType().Name);
                    result.AddError("Provider", $"Validation failed for {provider.GetType().Name}: {ex.Message}");
                }
            }

            _logger.LogInformation("Configuration validation completed. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                result.IsValid, result.Errors.Count, result.Warnings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration validation failed");
            result.AddError("Validation", $"Validation process failed: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Reloads configuration from all sources asynchronously
    /// </summary>
    public async Task ReloadAsync()
    {
        try
        {
            lock (_lockObject)
            {
                // Clear cache
                _configurationCache.Clear();
                
                // Reload built-in configuration
                if (_configuration is IConfigurationRoot configRoot)
                {
                    configRoot.Reload();
                }
                
                // Reload A3sist configuration
                LoadConfiguration();
            }

            // Reload from providers
            foreach (var provider in _configurationProviders)
            {
                try
                {
                    await provider.ReloadAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Provider {ProviderType} reload failed", 
                        provider.GetType().Name);
                }
            }

            // Notify of reload
            var changeArgs = new ConfigurationChangedEventArgs("All", ConfigurationChangeType.Reloaded)
            {
                Source = "ConfigurationService"
            };
            ConfigurationChanged?.Invoke(this, changeArgs);
            
            _logger.LogInformation("Configuration reloaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload configuration");
            throw;
        }
    }

    /// <summary>
    /// Gets the A3sist configuration
    /// </summary>
    public A3sistConfiguration GetA3sistConfiguration() => _a3sistConfig;

    /// <summary>
    /// Gets a configuration value by key (synchronous version for backward compatibility)
    /// </summary>
    public T GetValue<T>(string key, T defaultValue = default!)
    {
        return GetValueAsync(key, defaultValue).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets a configuration section
    /// </summary>
    public IConfigurationSection GetSection(string key)
    {
        return _configuration.GetSection(key);
    }

    /// <summary>
    /// Checks if a configuration key exists
    /// </summary>
    public bool KeyExists(string key)
    {
        return _configuration[key] != null;
    }

    private void LoadConfiguration()
    {
        _a3sistConfig = new A3sistConfiguration();
        _configuration.GetSection(A3sistConfiguration.SectionName).Bind(_a3sistConfig);
    }

    private async Task ValidateA3sistConfiguration(ConfigurationValidationResult result)
    {
        var context = new ValidationContext(_a3sistConfig);
        var validationResults = new List<ValidationResult>();
        
        if (!Validator.TryValidateObject(_a3sistConfig, context, validationResults, true))
        {
            foreach (var validationResult in validationResults)
            {
                var property = validationResult.MemberNames.FirstOrDefault() ?? "Unknown";
                result.AddError(property, validationResult.ErrorMessage ?? "Validation failed");
            }
        }

        // Additional custom validations
        if (_a3sistConfig.LLM?.MaxTokens <= 0)
        {
            result.AddError("LLM.MaxTokens", "MaxTokens must be greater than 0");
        }

        if (string.IsNullOrWhiteSpace(_a3sistConfig.LLM?.Provider))
        {
            result.AddWarning("LLM.Provider", "LLM Provider is not specified, using default");
        }

        if (_a3sistConfig.Agents?.Orchestrator?.MaxConcurrentTasks <= 0)
        {
            result.AddError("Agents.Orchestrator.MaxConcurrentTasks", "MaxConcurrentTasks must be greater than 0");
        }

        await Task.CompletedTask; // Make method async for consistency
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _reloadTimer?.Dispose();
            _disposed = true;
        }
    }
}