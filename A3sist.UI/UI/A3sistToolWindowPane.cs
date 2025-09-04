using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using A3sist.UI.Services;

namespace A3sist.UI.UI
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided by the Managed Package Framework
    /// and implements the IVsWindowPane interface that is used by the shell to implement the tool window.
    /// </para>
    /// </remarks>
    [Guid("7c3c6c2d-4e5f-4b1a-8d9c-1f2e3a4b5c6d")]
    public class A3sistToolWindowPane : ToolWindowPane
    {
        private A3sistToolWindow _control;
        private IA3sistApiClient _apiClient;
        private IA3sistConfigurationService _configService;

        /// <summary>
        /// Initializes a new instance of the <see cref="A3sistToolWindowPane"/> class.
        /// </summary>
        public A3sistToolWindowPane() : base(null)
        {
            this.Caption = "A3sist Assistant";
            
            InitializeServices();
            
            

            // Set the tool window icon
            this.BitmapResourceID = 301;
            this.BitmapIndex = 1;
        }

        private void InitializeServices()
        {
            try
            {
                // Get services from the global provider since package might not be ready
                _apiClient = ServiceProvider.GlobalProvider.GetService(typeof(IA3sistApiClient)) as IA3sistApiClient;
                _configService = ServiceProvider.GlobalProvider.GetService(typeof(IA3sistConfigurationService)) as IA3sistConfigurationService;

                // Fallback: try to get from package instance
                if (_apiClient == null || _configService == null)
                {
                    var package = A3sistPackage.Instance;
                    if (package != null)
                    {
                        _apiClient = package.GetApiClient();
                        _configService = package.GetConfigurationService();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Failed to initialize tool window services: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the control hosted in this tool window.
        /// </summary>
        public A3sistToolWindow Control => _control;

        /// <summary>
        /// Called when the tool window is created.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            InitializeServices();

            if (_apiClient != null && _configService != null)
            {
                _control = new A3sistToolWindow(_apiClient, _configService);
                this.Content = _control;
            }
            else
            {
                var errorText = new System.Windows.Controls.TextBlock
                {
                    Text = "Failed to initialize A3sist services. Please restart Visual Studio.",
                    Margin = new System.Windows.Thickness(20),
                    TextWrapping = System.Windows.TextWrapping.Wrap,
                    Foreground = System.Windows.Media.Brushes.Red
                };
                this.Content = errorText;
            }
        }

        /// <summary>
        /// Called when the tool window is disposed.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    // Clean up resources
                    _control?.Dispose();
                    _control = null;
                    _apiClient = null;
                    _configService = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Tool window disposal error: {ex.Message}");
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Called when the tool window gains focus.
        /// </summary>
        /// <param name="pfGotFocus">true if the window is gaining focus; false if it is losing focus.</param>
        public override void OnToolWindowCreated()
        {
            base.OnToolWindowCreated();

            try
            {
                // Tool window has been created and is ready
                if (_control != null)
                {
                    // Notify the control that it's been created
                    System.Diagnostics.Debug.WriteLine("A3sist: Tool window created successfully");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Tool window creation notification error: {ex.Message}");
            }
        }

        /// <summary>
        /// Provides access to the API client for external use.
        /// </summary>
        public IA3sistApiClient ApiClient => _apiClient;

        /// <summary>
        /// Provides access to the configuration service for external use.
        /// </summary>
        public IA3sistConfigurationService ConfigurationService => _configService;

        /// <summary>
        /// Forces a refresh of the tool window content.
        /// </summary>
        public void RefreshContent()
        {
            try
            {
                _control?.RefreshContent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Tool window refresh error: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows an error message in the tool window.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        public void ShowError(string message)
        {
            try
            {
                _control?.ShowError(message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Tool window show error failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the connection status display.
        /// </summary>
        /// <param name="isConnected">Whether the API is connected.</param>
        public void UpdateConnectionStatus(bool isConnected)
        {
            try
            {
                _control?.UpdateConnectionStatus(isConnected);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Tool window connection status update error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Extension methods for the A3sistToolWindow control to support additional functionality.
    /// </summary>
    public static class A3sistToolWindowExtensions
    {
        /// <summary>
        /// Safely executes an action on the tool window control.
        /// </summary>
        /// <param name="control">The tool window control.</param>
        /// <param name="action">The action to execute.</param>
        public static void SafeExecute(this A3sistToolWindow control, Action action)
        {
            if (control == null || action == null)
                return;

            try
            {
                if (control.Dispatcher.CheckAccess())
                {
                    action();
                }
                else
                {
                    control.Dispatcher.Invoke(action);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Safe execute error: {ex.Message}");
            }
        }

        /// <summary>
        /// Safely executes an async action on the tool window control.
        /// </summary>
        /// <param name="control">The tool window control.</param>
        /// <param name="action">The async action to execute.</param>
        public static async System.Threading.Tasks.Task SafeExecuteAsync(this A3sistToolWindow control, Func<System.Threading.Tasks.Task> action)
        {
            if (control == null || action == null)
                return;

            try
            {
                if (control.Dispatcher.CheckAccess())
                {
                    await action();
                }
                else
                {
                    await control.Dispatcher.InvokeAsync(action);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Safe execute async error: {ex.Message}");
            }
        }
    }
}