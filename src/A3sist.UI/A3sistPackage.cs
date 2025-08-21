using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using A3sist.Core;
using A3sist.Core.Services;
using A3sist.UI.Commands;
using A3sist.UI.ToolWindows;
using Task = System.Threading.Tasks.Task;

namespace A3sist.UI
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(A3sistPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(A3ToolWindow))]
    [ProvideToolWindow(typeof(AgentStatusWindow))]
    [ProvideOptionPage(typeof(Options.GeneralOptionsPage), "A3sist", "General", 0, 0, true)]
    [ProvideOptionPage(typeof(Options.AgentOptionsPage), "A3sist", "Agents", 0, 0, true)]
    [ProvideOptionPage(typeof(Options.LLMOptionsPage), "A3sist", "LLM Settings", 0, 0, true)]
    public sealed class A3sistPackage : AsyncPackage
    {
        /// <summary>
        /// A3sistPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "12345678-1234-1234-1234-123456789012";

        /// <summary>
        /// Package GUID
        /// </summary>
        public static readonly Guid PackageGuid = new Guid(PackageGuidString);

        private IServiceProvider? _serviceProvider;
        private ILogger<A3sistPackage>? _logger;

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
                // Initialize core services
                _serviceProvider = Startup.CreateServiceProvider();
                _logger = _serviceProvider.GetService<ILogger<A3sistPackage>>();
                
                _logger?.LogInformation("Initializing A3sist Visual Studio Package");

                // Initialize commands
                await InitializeCommandsAsync(cancellationToken);

                // Initialize tool windows
                await InitializeToolWindowsAsync(cancellationToken);

                _logger?.LogInformation("A3sist Visual Studio Package initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize A3sist Visual Studio Package");
                
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
            await A3sistMainCommand.InitializeAsync(this);
            await ShowA3ToolWindowCommand.InitializeAsync(this);
            await ShowAgentStatusCommand.InitializeAsync(this);
            await AnalyzeCodeCommand.InitializeAsync(this);
            await RefactorCodeCommand.InitializeAsync(this);
            await FixCodeCommand.InitializeAsync(this);

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
                    _logger?.LogInformation("Disposing A3sist Visual Studio Package");
                    
                    // Dispose of core services if needed
                    if (_serviceProvider is IDisposable disposableProvider)
                    {
                        disposableProvider.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error disposing A3sist Visual Studio Package");
                }
            }

            base.Dispose(disposing);
        }
    }
}