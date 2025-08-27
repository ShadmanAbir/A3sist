using System;
using System.Threading.Tasks;

namespace A3sist.UI.Services
{
    /// <summary>
    /// Service interface for hosting MAUI content within VSIX
    /// </summary>
    public interface IVSIXHostService
    {
        /// <summary>
        /// Initialize the MAUI host environment
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Start the MAUI application host
        /// </summary>
        Task StartHostAsync();

        /// <summary>
        /// Stop the MAUI application host
        /// </summary>
        Task StopHostAsync();

        /// <summary>
        /// Get the URL for the hosted MAUI application
        /// </summary>
        string GetHostUrl();

        /// <summary>
        /// Check if the host is running
        /// </summary>
        bool IsHostRunning { get; }

        /// <summary>
        /// Event raised when host status changes
        /// </summary>
        event EventHandler<HostStatusChangedEventArgs> HostStatusChanged;
    }

    /// <summary>
    /// Event arguments for host status changes
    /// </summary>
    public class HostStatusChangedEventArgs : EventArgs
    {
        public bool IsRunning { get; set; }
        public string? Message { get; set; }
        public Exception? Error { get; set; }
    }
}