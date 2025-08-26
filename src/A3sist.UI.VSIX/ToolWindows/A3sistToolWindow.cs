using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace A3sist.UI.VSIX.ToolWindows
{
    /// <summary>
    /// This class implements the tool window exposed to the user.
    /// 
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// 
    /// This class derives from the ToolWindowPane class provided by the Managed Package Framework.
    /// </summary>
    [Guid("12345678-1234-1234-1234-123456789014")]
    public class A3sistToolWindow : ToolWindowPane
    {
        private readonly ILogger<A3sistToolWindow>? _logger;
        private A3sistToolWindowControl? _control;

        /// <summary>
        /// Initializes a new instance of the <see cref="A3sistToolWindow"/> class.
        /// </summary>
        public A3sistToolWindow() : base(null)
        {
            this.Caption = "A3sist";

            try
            {
                // Get logger from package service provider
                if (Package is A3sistVSIXPackage package)
                {
                    var serviceProvider = package.GetServiceProvider();
                    _logger = serviceProvider.GetService<ILogger<A3sistToolWindow>>();
                }

                _logger?.LogInformation("Initializing A3sist tool window");

                // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
                // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
                // the object returned by the Content property.
                _control = new A3sistToolWindowControl(_logger);
                this.Content = _control;

                _logger?.LogInformation("A3sist tool window initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize A3sist tool window");
                
                // Create a simple error control if initialization fails
                this.Content = new System.Windows.Controls.TextBlock
                {
                    Text = $"Error initializing A3sist: {ex.Message}",
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
                    _logger?.LogInformation("Disposing A3sist tool window");
                    _control?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error disposing A3sist tool window");
                }
            }

            base.Dispose(disposing);
        }
    }
}