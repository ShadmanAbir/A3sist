using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Chat.Desktop
{
    /// <summary>
    /// Simple chat service interface for the standalone desktop application
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// Sends a message and gets a response
        /// </summary>
        Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a message with streaming response
        /// </summary>
        Task SendMessageStreamAsync(string message, Action<string> onChunk, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets whether the service is available
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Event fired when the service status changes
        /// </summary>
        event EventHandler<string>? StatusChanged;
    }

    /// <summary>
    /// Simple UI service interface for the standalone desktop application
    /// </summary>
    public interface IUIService
    {
        /// <summary>
        /// Shows a notification message
        /// </summary>
        Task ShowNotificationAsync(string title, string message);

        /// <summary>
        /// Shows a confirmation dialog
        /// </summary>
        Task<bool> ShowConfirmationAsync(string title, string message);

        /// <summary>
        /// Gets selected code (placeholder for now)
        /// </summary>
        Task<string> GetSelectedCodeAsync();
    }
}