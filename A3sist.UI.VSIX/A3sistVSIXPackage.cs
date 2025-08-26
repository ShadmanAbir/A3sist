using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using A3sist.Shared.Interfaces;
using A3sist.UI.VSIX.Commands;
using A3sist.UI.VSIX.ToolWindows;
using Task = System.Threading.Tasks.Task;

namespace A3sist.UI.VSIX
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// This VSIX package hosts the MAUI application via WebView2 for modern UI.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(A3sistVSIXPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(A3sistToolWindow))]
    [ProvideToolWindow(typeof(ChatToolWindow))]
    public sealed class A3sistVSIXPackage : AsyncPackage
    {
        /// <summary>
        /// A3sistVSIXPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "12345678-1234-1234-1234-123456789013";

        /// <summary>
        /// Package GUID
        /// </summary>
        public static readonly Guid PackageGuid = new Guid(PackageGuidString);

        private IServiceProvider? _serviceProvider;
        private ILogger<A3sistVSIXPackage>? _logger;

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            try
            {
                // Initialize minimal services for .NET 4.7.2 compatibility
                _serviceProvider = CreateMinimalServiceProvider();
                _logger = _serviceProvider.GetService<ILogger<A3sistVSIXPackage>>();
                
                _logger?.LogInformation("Initializing A3sist VSIX Package (Minimal Version)");

                // Initialize commands
                await InitializeCommandsAsync(cancellationToken);

                // Initialize tool windows
                await InitializeToolWindowsAsync(cancellationToken);

                _logger?.LogInformation("A3sist VSIX Package initialized successfully (Minimal Version)");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize A3sist VSIX Package");
                
                // Show error to user
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                VsShellUtilities.ShowMessageBox(
                    this,
                    $"Failed to initialize A3sist extension: {ex.Message}",
                    "A3sist Initialization Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                
                throw;
            }
        }

        /// <summary>
        /// Initialize all commands
        /// </summary>
        private async Task InitializeCommandsAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Initialize main commands
            await ShowA3sistToolWindowCommand.InitializeAsync(this);
            await ShowChatWindowCommand.InitializeAsync(this);

            _logger?.LogDebug("Commands initialized successfully");
        }

        /// <summary>
        /// Initialize tool windows
        /// </summary>
        private async Task InitializeToolWindowsAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Tool windows are initialized on-demand when first shown
            _logger?.LogDebug("Tool windows registration completed");
        }

        /// <summary>
        /// Creates a minimal service provider compatible with .NET 4.7.2
        /// </summary>
        private IServiceProvider CreateMinimalServiceProvider()
        {
            var services = new ServiceCollection();
            
            // Add basic logging
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Add minimal VSIX services
            services.AddSingleton<IVSIXHostService, VSIXHostService>();
            services.AddSingleton<IWebViewService, WebViewService>();

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Gets the service provider for the package
        /// </summary>
        public IServiceProvider GetServiceProvider()
        {
            return _serviceProvider ?? throw new InvalidOperationException("Package not initialized");
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _logger?.LogInformation("Disposing A3sist VSIX Package");
                    
                    // Dispose of core services if needed
                    if (_serviceProvider is IDisposable disposableProvider)
                    {
                        disposableProvider.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error disposing A3sist VSIX Package");
                }
            }

            base.Dispose(disposing);
        }
    }
}