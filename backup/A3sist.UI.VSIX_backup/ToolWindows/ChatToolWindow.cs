using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace A3sist.UI.VSIX.ToolWindows
{
    /// <summary>
    /// This class implements the chat tool window.
    /// </summary>
    [Guid("12345678-1234-1234-1234-123456789015")]
    public class ChatToolWindow : ToolWindowPane
    {
        private readonly ILogger<ChatToolWindow>? _logger;
        private ChatToolWindowControl? _control;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatToolWindow"/> class.
        /// </summary>
        public ChatToolWindow() : base(null)
        {
            this.Caption = "A3sist Chat";

            try
            {
                // Get logger from package service provider
                IServiceProvider? serviceProvider = null;
                if (Package is A3sistVSIXPackage package)
                {
                    serviceProvider = package.GetServiceProvider();
                    _logger = serviceProvider.GetService<ILogger<ChatToolWindow>>();
                }

                _logger?.LogInformation("Initializing A3sist chat tool window");

                // This is the user control hosted by the tool window
                var controlLogger = serviceProvider?.GetService<ILogger<ChatToolWindowControl>>();
                _control = new ChatToolWindowControl(controlLogger);
                this.Content = _control;

                _logger?.LogInformation("A3sist chat tool window initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize A3sist chat tool window");
                
                // Create a simple error control if initialization fails
                this.Content = new System.Windows.Controls.TextBlock
                {
                    Text = $"Error initializing A3sist Chat: {ex.Message}",
                    Margin = new System.Windows.Thickness(10),
                    TextWrapping = System.Windows.TextWrapping.Wrap
                };
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _logger?.LogInformation("Disposing A3sist chat tool window");
                    _control?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error disposing A3sist chat tool window");
                }
            }

            base.Dispose(disposing);
        }
    }
}