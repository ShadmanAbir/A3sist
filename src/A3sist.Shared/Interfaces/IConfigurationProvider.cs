using A3sist.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for configuration providers that can load, save, and validate configuration data
    /// </summary>
    public interface IConfigurationProvider
    {
        /// <summary>
        /// Gets the name of the configuration provider
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the priority of this provider (higher values take precedence)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Checks if the provider supports writing
        /// </summary>
        bool SupportsWrite { get; }

        /// <summary>
        /// Gets a configuration value by key
        /// </summary>
        /// <typeparam name="T">Type of the configuration value</typeparam>
        /// <param name="key">Configuration key</param>
        /// <returns>Configuration value</returns>
        Task<T> GetValueAsync<T>(string key);

        /// <summary>
        /// Sets a configuration value
        /// </summary>
        /// <typeparam name="T">Type of the configuration value</typeparam>
        /// <param name="key">Configuration key</param>
        /// <param name="value">Configuration value</param>
        Task SetValueAsync<T>(string key, T value);

        /// <summary>
        /// Gets all configuration values
        /// </summary>
        /// <returns>Dictionary of all configuration values</returns>
        Task<Dictionary<string, object>> GetAllValuesAsync();

        /// <summary>
        /// Loads configuration data
        /// </summary>
        /// <returns>Configuration data as key-value pairs</returns>
        Task<Dictionary<string, object>> LoadAsync();

        /// <summary>
        /// Saves configuration data
        /// </summary>
        /// <param name="data">Configuration data to save</param>
        Task SaveAsync(Dictionary<string, object> data);

        /// <summary>
        /// Validates the configuration data
        /// </summary>
        /// <returns>Validation result</returns>
        Task<ConfigurationValidationResult> ValidateAsync();

        /// <summary>
        /// Reloads configuration data from source
        /// </summary>
        Task ReloadAsync();

        /// <summary>
        /// Event raised when configuration data changes
        /// </summary>
        event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
    }
}