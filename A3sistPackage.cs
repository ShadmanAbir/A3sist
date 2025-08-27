using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using A3sist.Services;
using A3sist.Commands;
using Task = System.Threading.Tasks.Task;

namespace A3sist
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
    [ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideService(typeof(IA3sistConfigurationService), IsAsyncQueryable = true)]
    [ProvideService(typeof(IModelManagementService), IsAsyncQueryable = true)]
    [ProvideService(typeof(IMCPClientService), IsAsyncQueryable = true)]
    [ProvideService(typeof(IRAGEngineService), IsAsyncQueryable = true)]
    [ProvideService(typeof(ICodeAnalysisService), IsAsyncQueryable = true)]
    [ProvideService(typeof(IChatService), IsAsyncQueryable = true)]
    public sealed class A3sistPackage : AsyncPackage
    {
        /// <summary>
        /// A3sistPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "12345678-1234-1234-1234-123456789012";

        private Microsoft.Extensions.DependencyInjection.ServiceProvider _serviceProvider;
        
        /// <summary>
        /// Gets the current package instance
        /// </summary>
        public static A3sistPackage Instance { get; private set; }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on the package object.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // Set the static instance
            Instance = this;
            
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Initialize dependency injection
            await InitializeServicesAsync();

            // Initialize commands
            await InitializeCommandsAsync();

            await base.InitializeAsync(cancellationToken, progress);
        }

        private async Task InitializeServicesAsync()
        {
            var services = new ServiceCollection();

            // Register core services
            services.AddSingleton<IA3sistConfigurationService, A3sistConfigurationService>();
            services.AddSingleton<IModelManagementService, ModelManagementService>();
            services.AddSingleton<IMCPClientService, MCPClientService>();
            services.AddSingleton<IRAGEngineService, RAGEngineService>();
            services.AddSingleton<ICodeAnalysisService, CodeAnalysisService>();
            services.AddSingleton<IChatService, ChatService>();
            services.AddSingleton<IRefactoringService, RefactoringService>();
            services.AddSingleton<IAutoCompleteService, AutoCompleteService>();
            services.AddSingleton<A3sist.Agent.IAgentModeService, A3sist.Agent.AgentModeService>();

            _serviceProvider = services.BuildServiceProvider();

            // Register services with VS Shell
            AddService(typeof(IA3sistConfigurationService), async (container, ct, type) =>
            {
                return _serviceProvider.GetService<IA3sistConfigurationService>();
            }, true);

            AddService(typeof(IModelManagementService), async (container, ct, type) =>
            {
                return _serviceProvider.GetService<IModelManagementService>();
            }, true);

            AddService(typeof(IMCPClientService), async (container, ct, type) =>
            {
                return _serviceProvider.GetService<IMCPClientService>();
            }, true);

            AddService(typeof(IRAGEngineService), async (container, ct, type) =>
            {
                return _serviceProvider.GetService<IRAGEngineService>();
            }, true);

            AddService(typeof(ICodeAnalysisService), async (container, ct, type) =>
            {
                return _serviceProvider.GetService<ICodeAnalysisService>();
            }, true);

            AddService(typeof(IChatService), async (container, ct, type) =>
            {
                return _serviceProvider.GetService<IChatService>();
            }, true);
        }

        private async Task InitializeCommandsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            OleMenuCommandService commandService = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                // Initialize commands
                OpenChatCommand.Initialize(this, commandService);
                ConfigureA3sistCommand.Initialize(this, commandService);
                ToggleAutoCompleteCommand.Initialize(this, commandService);
                RefactorCodeCommand.Initialize(this, commandService);
            }
        }

        public T GetService<T>() where T : class
        {
            return _serviceProvider?.GetService<T>();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _serviceProvider?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}