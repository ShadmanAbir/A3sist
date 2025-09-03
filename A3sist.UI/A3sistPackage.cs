using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
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
    [ProvideToolWindow(typeof(UI.A3sistToolWindowPane), Style = VsDockStyle.Tabbed, Window = "3ae79031-e1bc-11d0-8f78-00a0c9110057", MultiInstances = false, Transient = false)]
    [ProvideService(typeof(Services.IA3sistApiClient), ServiceName = "A3sist API Client")]
    [ProvideService(typeof(Services.IA3sistConfigurationService), ServiceName = "A3sist Configuration Service")]
    public sealed class A3sistPackage : AsyncPackage
    {
        /// <summary>
        /// A3sistPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "11b6ba66-9b93-4de6-acc0-7baecb76a619";

        /// <summary>
        /// Gets the singleton instance of the package.
        /// </summary>
        public static A3sistPackage Instance { get; private set; }

        private Services.IA3sistApiClient _apiClient;
        private Services.IA3sistConfigurationService _configService;

        /// <summary>
        /// Gets the API client service.
        /// </summary>
        public Services.IA3sistApiClient GetApiClient() => _apiClient;

        /// <summary>
        /// Gets the configuration service.
        /// </summary>
        public Services.IA3sistConfigurationService GetConfigurationService() => _configService;

        /// <summary>
        /// Gets a service by type (compatibility method).
        /// </summary>
        public T GetService<T>() where T : class
        {
            if (typeof(T) == typeof(Services.IA3sistApiClient))
                return _apiClient as T;
            if (typeof(T) == typeof(Services.IA3sistConfigurationService))
                return _configService as T;
            return null;
        }

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
            Console.Write("here");
            // Set the singleton instance
            Instance = this;

            // Initialize services
            _configService = new Services.A3sistConfigurationService();
            _apiClient = new Services.A3sistApiClient();

            // Register services with the correct async signature
            AddService(typeof(Services.IA3sistApiClient), async (container, cancellationToken, type) =>
            {
                return await Task.FromResult<object>(_apiClient);
            }, true);

            AddService(typeof(Services.IA3sistConfigurationService), async (container, cancellationToken, type) =>
            {
                return await Task.FromResult<object>(_configService);
            }, true);

            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Initialize commands
            await Commands.Commands.InitializeAsync(this);

            // Show the tool window after installation
            await ShowToolWindowOnStartupAsync();

            System.Diagnostics.Debug.WriteLine("A3sistPackage INITIALIZED!");

            await Task.CompletedTask;
        }

        /// <summary>
        /// Shows the A3sist tool window automatically after package initialization.
        /// </summary>
        private async Task ShowToolWindowOnStartupAsync()
        {
            try
            {
                // Use JoinableTaskFactory to ensure we're on the UI thread
                await this.JoinableTaskFactory.RunAsync(async delegate
                {
                    await this.JoinableTaskFactory.SwitchToMainThreadAsync();
                    
                    // Show the tool window
                    var window = await this.ShowToolWindowAsync(typeof(UI.A3sistToolWindowPane), 0, true, this.DisposalToken);
                    if (window?.Frame != null)
                    {
                        // Make sure the window is visible and focused
                        var windowFrame = (IVsWindowFrame)window.Frame;
                        Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
                        
                        System.Diagnostics.Debug.WriteLine("A3sist tool window displayed successfully");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Failed to create A3sist tool window - window or frame is null");
                    }
                });
            }
            catch (Exception ex)
            {
                // Log the error but don't prevent the package from loading
                System.Diagnostics.Debug.WriteLine($"Failed to show A3sist tool window on startup: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        #endregion
    }
}