using System;
using System.Collections.Generic;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Event arguments for configuration change notifications
    /// </summary>
    public class ConfigurationChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Name of the configuration that changed
        /// </summary>
        public string ConfigurationName { get; set; }

        /// <summary>
        /// Type of change that occurred
        /// </summary>
        public ConfigurationChangeType ChangeType { get; set; }

        /// <summary>
        /// Keys that were changed
        /// </summary>
        public List<string> ChangedKeys { get; set; }

        /// <summary>
        /// Old values (for updates)
        /// </summary>
        public Dictionary<string, object> OldValues { get; set; }

        /// <summary>
        /// New values
        /// </summary>
        public Dictionary<string, object> NewValues { get; set; }

        /// <summary>
        /// When the change occurred
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Source of the change
        /// </summary>
        public string Source { get; set; }

        public ConfigurationChangedEventArgs()
        {
            ChangedKeys = new List<string>();
            OldValues = new Dictionary<string, object>();
            NewValues = new Dictionary<string, object>();
            Timestamp = DateTime.UtcNow;
        }

        public ConfigurationChangedEventArgs(string configurationName, ConfigurationChangeType changeType) : this()
        {
            ConfigurationName = configurationName;
            ChangeType = changeType;
        }
    }

    /// <summary>
    /// Type of configuration change
    /// </summary>
    public enum ConfigurationChangeType
    {
        /// <summary>
        /// Configuration was created
        /// </summary>
        Created,

        /// <summary>
        /// Configuration was updated
        /// </summary>
        Updated,

        /// <summary>
        /// Configuration was deleted
        /// </summary>
        Deleted,

        /// <summary>
        /// Configuration was reloaded
        /// </summary>
        Reloaded
    }
}