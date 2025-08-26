using System;
using System.Threading.Tasks;

namespace A3sist.UI.VSIX
{
    /// <summary>
    /// Service interface for managing WebView2 controls
    /// </summary>
    public interface IWebViewService
    {
        /// <summary>
        /// Initialize WebView2 environment
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Navigate to a URL
        /// </summary>
        Task NavigateAsync(string url);

        /// <summary>
        /// Execute JavaScript in the WebView
        /// </summary>
        Task<string> ExecuteScriptAsync(string script);

        /// <summary>
        /// Add a web resource request handler
        /// </summary>
        void AddWebResourceRequestedHandler(string filter, Func<object, Task<object>> handler);

        /// <summary>
        /// Remove a web resource request handler
        /// </summary>
        void RemoveWebResourceRequestedHandler(string filter);

        /// <summary>
        /// Get or set whether dev tools are enabled
        /// </summary>
        bool AreDevToolsEnabled { get; set; }

        /// <summary>
        /// Event raised when navigation is completed
        /// </summary>
        event EventHandler<NavigationCompletedEventArgs> NavigationCompleted;

        /// <summary>
        /// Event raised when DOM content is loaded
        /// </summary>
        event EventHandler DomContentLoaded;
    }

    /// <summary>
    /// Event arguments for navigation completed events
    /// </summary>
    public class NavigationCompletedEventArgs : EventArgs
    {
        public bool IsSuccess { get; set; }
        public string? Url { get; set; }
        public Exception? Error { get; set; }
    }
}