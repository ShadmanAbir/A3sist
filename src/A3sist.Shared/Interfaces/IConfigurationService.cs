using A3sist.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for configuration management service
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Gets a configuration value by key
        /// </summary>
        /// <typeparam name="T">Type of the configuration value</typeparam>
        /// <param name="key">Configuration key</param>
        /// <param name="defaultValue">Default value if key is not found</param>
        /// <returns>Configuration value</returns>
        Task<T> GetValueAsync<T>(string key, T defaultValue = default);

        /// <summary>
        /// Sets a configuration value
        /// </summary>
        /// <typeparam name="T">Type of the configuration value</typeparam>
        /// <param name="key">Configuration key</param>
        /// <param name="value">Configuration value</param>
        /// <returns>Task representing the set operation</returns>
        Task SetValueAsync<T>(string key, T value);

        /// <summary>
        /// Gets all configuration values
        /// </summary>
        /// <returns>Dictionary of all configuration values</returns>
        Task<Dictionary<string, object>> GetAllValuesAsync();

        /// <summary>
        /// Validates the configuration
        /// </summary>
        /// <returns>Configuration validation result</returns>
        Task<ConfigurationValidationResult> ValidateAsync();

        /// <summary>
        /// Reloads configuration from source
        /// </summary>
        /// <returns>Task representing the reload operation</returns>
        Task ReloadAsync();

        /// <summary>
        /// Event raised when configuration changes
        /// </summary>
        event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
    }
}