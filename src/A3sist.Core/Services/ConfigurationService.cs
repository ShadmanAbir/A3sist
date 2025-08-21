using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using A3sist.Core.Configuration;
using A3sist.Shared.Interfaces;

namespace A3sist.Core.Services;

/// <summary>
/// Service for managing application configuration
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly A3sistConfiguration _a3sistConfig;

    public ConfigurationService(
        IConfiguration configuration,
        ILogger<ConfigurationService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Bind the A3sist configuration section
        _a3sistConfig = new A3sistConfiguration();
        _configuration.GetSection(A3sistConfiguration.SectionName).Bind(_a3sistConfig);
        
        _logger.LogInformation("Configuration service initialized");
    }

    /// <summary>
    /// Gets the A3sist configuration
    /// </summary>
    public A3sistConfiguration GetA3sistConfiguration() => _a3sistConfig;

    /// <summary>
    /// Gets a configuration value by key
    /// </summary>
    /// <typeparam name="T">The type of the configuration value</typeparam>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">The default value if key is not found</param>
    /// <returns>The configuration value</returns>
    public T GetValue<T>(string key, T defaultValue = default!)
    {
        try
        {
            return _configuration.GetValue<T>(key, defaultValue);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get configuration value for key: {Key}", key);
            return defaultValue;
        }
    }

    /// <summary>
    /// Gets a configuration section
    /// </summary>
    /// <param name="key">The section key</param>
    /// <returns>The configuration section</returns>
    public IConfigurationSection GetSection(string key)
    {
        return _configuration.GetSection(key);
    }

    /// <summary>
    /// Checks if a configuration key exists
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <returns>True if the key exists, false otherwise</returns>
    public bool KeyExists(string key)
    {
        return _configuration[key] != null;
    }
}