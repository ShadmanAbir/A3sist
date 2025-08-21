using Microsoft.Extensions.Configuration;

namespace A3sist.Shared.Interfaces;

/// <summary>
/// Interface for configuration service
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets a configuration value by key
    /// </summary>
    /// <typeparam name="T">The type of the configuration value</typeparam>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">The default value if key is not found</param>
    /// <returns>The configuration value</returns>
    T GetValue<T>(string key, T defaultValue = default!);

    /// <summary>
    /// Gets a configuration section
    /// </summary>
    /// <param name="key">The section key</param>
    /// <returns>The configuration section</returns>
    IConfigurationSection GetSection(string key);

    /// <summary>
    /// Checks if a configuration key exists
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <returns>True if the key exists, false otherwise</returns>
    bool KeyExists(string key);
}