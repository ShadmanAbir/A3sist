using System;
using System.Collections.Generic;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for shell agent configuration
    /// </summary>
    public interface IShellConfiguration
    {
        /// <summary>
        /// Maximum time to wait for command execution
        /// </summary>
        TimeSpan CommandTimeout { get; }

        /// <summary>
        /// Maximum number of concurrent command executions
        /// </summary>
        int MaxConcurrentCommands { get; }

        /// <summary>
        /// Working directory for command execution
        /// </summary>
        string WorkingDirectory { get; }

        /// <summary>
        /// Environment variables to set for command execution
        /// </summary>
        Dictionary<string, string> EnvironmentVariables { get; }

        /// <summary>
        /// Whether to enable command logging
        /// </summary>
        bool EnableCommandLogging { get; }

        /// <summary>
        /// Whether to enable sandbox mode
        /// </summary>
        bool EnableSandbox { get; }

        /// <summary>
        /// List of allowed command executables
        /// </summary>
        HashSet<string> AllowedExecutables { get; }

        /// <summary>
        /// List of blocked command patterns
        /// </summary>
        HashSet<string> BlockedPatterns { get; }
    }
}