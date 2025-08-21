using A3sist.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for configuration providers
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
        /// Checks if the provider supports writing
        /// </summary>
        bool SupportsWrite { get; }

        /// <summary>
        /// Event raised when configuration data changes
        /// </summary>
        event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
    }
}