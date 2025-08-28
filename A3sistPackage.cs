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
    [ProvideToolWindow(typeof(A3sist.UI.A3sistToolWindowPane), Style = VsDockStyle.Float, Window = ToolWindowGuids80.SolutionExplorer)]
    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
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
        public const string PackageGuidString = "285cd009-b086-4f05-a305-35790de8f3d1";

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
            try
            {
                // Set the static instance
                Instance = this;
                
                System.Diagnostics.Debug.WriteLine("A3sist: Starting package initialization...");
                
                // When initialized asynchronously, the current thread may be a background thread at this point.
                // Do any initialization that requires the UI thread after switching to the UI thread.
                await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                // Initialize dependency injection with error handling
                await InitializeServicesAsync();

                // Initialize commands with error handling
                await InitializeCommandsAsync();

                await base.InitializeAsync(cancellationToken, progress);
                
                System.Diagnostics.Debug.WriteLine("A3sist: Package initialization completed successfully.");
            }
            catch (Exception ex)
            {
                // Log the error but don't throw to prevent VS from failing to load the package
                System.Diagnostics.Debug.WriteLine($"A3sist package initialization error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                try
                {
                    // Try to continue with base initialization even if our services fail
                    await base.InitializeAsync(cancellationToken, progress);
                    System.Diagnostics.Debug.WriteLine("A3sist: Base package initialization completed despite errors.");
                }
                catch (Exception baseEx)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Base package initialization error: {baseEx.Message}");
                    // Still don't throw - let VS continue
                }
            }
        }

        private async Task InitializeServicesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("A3sist: Initializing services...");
                
                var services = new ServiceCollection();

                // Register core services with individual error handling
                try
                {
                    services.AddSingleton<IA3sistConfigurationService, A3sistConfigurationService>();
                    System.Diagnostics.Debug.WriteLine("A3sist: Registered IA3sistConfigurationService");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Failed to register IA3sistConfigurationService: {ex.Message}");
                }

                try
                {
                    services.AddSingleton<IModelManagementService, ModelManagementService>();
                    System.Diagnostics.Debug.WriteLine("A3sist: Registered IModelManagementService");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Failed to register IModelManagementService: {ex.Message}");
                }

                try
                {
                    services.AddSingleton<IMCPClientService, MCPClientService>();
                    System.Diagnostics.Debug.WriteLine("A3sist: Registered IMCPClientService");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Failed to register IMCPClientService: {ex.Message}");
                }

                try
                {
                    services.AddSingleton<IRAGEngineService, RAGEngineService>();
                    System.Diagnostics.Debug.WriteLine("A3sist: Registered IRAGEngineService");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Failed to register IRAGEngineService: {ex.Message}");
                }

                try
                {
                    services.AddSingleton<ICodeAnalysisService, CodeAnalysisService>();
                    System.Diagnostics.Debug.WriteLine("A3sist: Registered ICodeAnalysisService");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Failed to register ICodeAnalysisService: {ex.Message}");
                }

                try
                {
                    services.AddSingleton<IChatService, ChatService>();
                    System.Diagnostics.Debug.WriteLine("A3sist: Registered IChatService");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Failed to register IChatService: {ex.Message}");
                }

                try
                {
                    services.AddSingleton<IRefactoringService, RefactoringService>();
                    System.Diagnostics.Debug.WriteLine("A3sist: Registered IRefactoringService");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Failed to register IRefactoringService: {ex.Message}");
                }

                try
                {
                    services.AddSingleton<IAutoCompleteService, AutoCompleteService>();
                    System.Diagnostics.Debug.WriteLine("A3sist: Registered IAutoCompleteService");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Failed to register IAutoCompleteService: {ex.Message}");
                }

                try
                {
                    services.AddSingleton<A3sist.Agent.IAgentModeService, A3sist.Agent.AgentModeService>();
                    System.Diagnostics.Debug.WriteLine("A3sist: Registered IAgentModeService");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Failed to register IAgentModeService: {ex.Message}");
                }

                // Build service provider with error handling
                try
                {
                    _serviceProvider = services.BuildServiceProvider();
                    System.Diagnostics.Debug.WriteLine("A3sist: Service provider built successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Failed to build service provider: {ex.Message}");
                    // Create a minimal service provider as fallback
                    var fallbackServices = new ServiceCollection();
                    _serviceProvider = fallbackServices.BuildServiceProvider();
                    System.Diagnostics.Debug.WriteLine("A3sist: Created fallback service provider");
                }

                // Register services with VS Shell with individual error handling
                await RegisterVSShellServicesAsync();
                
                System.Diagnostics.Debug.WriteLine("A3sist: Services initialization completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Critical error in service initialization: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Create a minimal service provider to prevent null reference exceptions
                try
                {
                    var emergencyServices = new ServiceCollection();
                    _serviceProvider = emergencyServices.BuildServiceProvider();
                    System.Diagnostics.Debug.WriteLine("A3sist: Emergency service provider created");
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Even fallback service provider failed: {fallbackEx.Message}");
                }
            }
        }

        private async Task RegisterVSShellServicesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("A3sist: Registering services with VS Shell...");

                // Register services with VS Shell with individual error handling
                try
                {
                    AddService(typeof(IA3sistConfigurationService), async (container, ct, type) =>
                    {
                        try
                        {
                            return _serviceProvider?.GetService<IA3sistConfigurationService>();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"A3sist: Error creating IA3sistConfigurationService: {ex.Message}");
                            return null;
                        }
                    }, true);
                    System.Diagnostics.Debug.WriteLine("A3sist: Registered IA3sistConfigurationService with VS Shell");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Failed to register IA3sistConfigurationService with VS Shell: {ex.Message}");
                }

                try
                {
                    AddService(typeof(IModelManagementService), async (container, ct, type) =>
                    {
                        try
                        {
                            return _serviceProvider?.GetService<IModelManagementService>();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"A3sist: Error creating IModelManagementService: {ex.Message}");
                            return null;
                        }
                    }, true);
                    System.Diagnostics.Debug.WriteLine("A3sist: Registered IModelManagementService with VS Shell");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Failed to register IModelManagementService with VS Shell: {ex.Message}");
                }

                try
                {
                    AddService(typeof(IMCPClientService), async (container, ct, type) =>
                    {
                        try
                        {
                            return _serviceProvider?.GetService<IMCPClientService>();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"A3sist: Error creating IMCPClientService: {ex.Message}");
                            return null;
                        }
                    }, true);
                    System.Diagnostics.Debug.WriteLine("A3sist: Registered IMCPClientService with VS Shell");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Failed to register IMCPClientService with VS Shell: {ex.Message}");
                }

                try
                {
                    AddService(typeof(IRAGEngineService), async (container, ct, type) =>
                    {
                        try
                        {
                            return _serviceProvider?.GetService<IRAGEngineService>();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"A3sist: Error creating IRAGEngineService: {ex.Message}");
                            return null;
                        }
                    }, true);
                    System.Diagnostics.Debug.WriteLine("A3sist: Registered IRAGEngineService with VS Shell");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Failed to register IRAGEngineService with VS Shell: {ex.Message}");
                }

                try
                {
                    AddService(typeof(ICodeAnalysisService), async (container, ct, type) =>
                    {
                        try
                        {
                            return _serviceProvider?.GetService<ICodeAnalysisService>();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"A3sist: Error creating ICodeAnalysisService: {ex.Message}");
                            return null;
                        }
                    }, true);
                    System.Diagnostics.Debug.WriteLine("A3sist: Registered ICodeAnalysisService with VS Shell");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Failed to register ICodeAnalysisService with VS Shell: {ex.Message}");
                }

                try
                {
                    AddService(typeof(IChatService), async (container, ct, type) =>
                    {
                        try
                        {
                            return _serviceProvider?.GetService<IChatService>();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"A3sist: Error creating IChatService: {ex.Message}");
                            return null;
                        }
                    }, true);
                    System.Diagnostics.Debug.WriteLine("A3sist: Registered IChatService with VS Shell");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Failed to register IChatService with VS Shell: {ex.Message}");
                }
                
                System.Diagnostics.Debug.WriteLine("A3sist: VS Shell service registration completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Critical error in VS Shell service registration: {ex.Message}");
            }
        }

        private async Task InitializeCommandsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("A3sist: Initializing commands...");
                
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                OleMenuCommandService commandService = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (commandService != null)
                {
                    // Initialize commands with individual error handling
                    try
                    {
                        OpenChatCommand.Initialize(this, commandService);
                        System.Diagnostics.Debug.WriteLine("A3sist: OpenChatCommand initialized");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"A3sist: Error initializing OpenChatCommand: {ex.Message}");
                    }

                    try
                    {
                        ConfigureA3sistCommand.Initialize(this, commandService);
                        System.Diagnostics.Debug.WriteLine("A3sist: ConfigureA3sistCommand initialized");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"A3sist: Error initializing ConfigureA3sistCommand: {ex.Message}");
                    }

                    try
                    {
                        ToggleAutoCompleteCommand.Initialize(this, commandService);
                        System.Diagnostics.Debug.WriteLine("A3sist: ToggleAutoCompleteCommand initialized");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"A3sist: Error initializing ToggleAutoCompleteCommand: {ex.Message}");
                    }

                    try
                    {
                        RefactorCodeCommand.Initialize(this, commandService);
                        System.Diagnostics.Debug.WriteLine("A3sist: RefactorCodeCommand initialized");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"A3sist: Error initializing RefactorCodeCommand: {ex.Message}");
                    }

                    try
                    {
                        ShowA3sistToolWindowCommand.Initialize(this, commandService);
                        System.Diagnostics.Debug.WriteLine("A3sist: ShowA3sistToolWindowCommand initialized");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"A3sist: Error initializing ShowA3sistToolWindowCommand: {ex.Message}");
                    }

                    try
                    {
                        ShowA3sistToolWindowViewCommand.Initialize(this, commandService);
                        System.Diagnostics.Debug.WriteLine("A3sist: ShowA3sistToolWindowViewCommand initialized");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"A3sist: Error initializing ShowA3sistToolWindowViewCommand: {ex.Message}");
                    }
                    
                    System.Diagnostics.Debug.WriteLine("A3sist: Command initialization completed");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("A3sist: CommandService is null, commands will not be available");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Error initializing commands: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private async Task ShowToolWindowAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("A3sist: Attempting to show tool window...");
                
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                // Get the instance of the tool window created when package was initialized
                var window = await this.FindToolWindowAsync(typeof(A3sist.UI.A3sistToolWindowPane), 0, true, this.DisposalToken);
                if ((null != window) && (null != window.Frame))
                {
                    var windowFrame = (IVsWindowFrame)window.Frame;
                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
                    System.Diagnostics.Debug.WriteLine("A3sist: Tool window shown successfully");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("A3sist: Failed to create or show tool window - window or frame is null");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Error showing tool window: {ex.Message}");
                // Don't throw - this is not critical for package loading
            }
        }

        public T GetService<T>() where T : class
        {
            try
            {
                if (_serviceProvider == null)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Service provider is null when requesting {typeof(T).Name}");
                    return null;
                }
                
                var service = _serviceProvider.GetService<T>();
                if (service == null)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Service {typeof(T).Name} not found in service provider");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Successfully retrieved service {typeof(T).Name}");
                }
                
                return service;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Error getting service {typeof(T).Name}: {ex.Message}");
                return null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("A3sist: Disposing package...");
                    
                    if (_serviceProvider != null)
                    {
                        _serviceProvider.Dispose();
                        _serviceProvider = null;
                        System.Diagnostics.Debug.WriteLine("A3sist: Service provider disposed");
                    }
                    
                    // Clear static instance
                    Instance = null;
                    
                    System.Diagnostics.Debug.WriteLine("A3sist: Package disposal completed");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Error during disposal: {ex.Message}");
                }
            }
            
            try
            {
                base.Dispose(disposing);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Error in base disposal: {ex.Message}");
            }
        }

        #endregion
    }
}