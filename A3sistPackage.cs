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
    [ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideService(typeof(IA3sistConfigurationService), IsAsyncQueryable = true)]
    [ProvideService(typeof(IChatService), IsAsyncQueryable = true)]
    public class A3sistPackage : AsyncPackage
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
                
                System.Diagnostics.Debug.WriteLine("A3sist: Starting minimal package initialization...");
                
                // Initialize only the absolute minimum required for Visual Studio
                await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                // Initialize only commands - defer everything else
                await InitializeCommandsAsync();

                await base.InitializeAsync(cancellationToken, progress);

                await ShowToolWindowAsync();

                System.Diagnostics.Debug.WriteLine("A3sist: Minimal package initialization completed.");
            }
            catch (Exception ex)
            {
                // Log the error but don't throw to prevent VS from failing to load the package
                System.Diagnostics.Debug.WriteLine($"A3sist package initialization error: {ex.Message}");
                
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
                System.Diagnostics.Debug.WriteLine("A3sist: Initializing minimal services for startup...");
                
                var services = new ServiceCollection();

                // Register only essential services during startup to prevent freezing
                try
                {
                    services.AddSingleton<IA3sistConfigurationService, A3sistConfigurationService>();
                    System.Diagnostics.Debug.WriteLine("A3sist: Registered IA3sistConfigurationService");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Failed to register IA3sistConfigurationService: {ex.Message}");
                }

                // Defer heavy services like ModelManagement, RAG, CodeAnalysis until actually needed
                // This prevents startup freezing caused by heavy initialization
                
                try
                {
                    services.AddSingleton<IChatService, ChatService>();
                    System.Diagnostics.Debug.WriteLine("A3sist: Registered IChatService");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Failed to register IChatService: {ex.Message}");
                }

                // Build service provider with error handling
                try
                {
                    _serviceProvider = services.BuildServiceProvider();
                    System.Diagnostics.Debug.WriteLine("A3sist: Minimal service provider built successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Failed to build service provider: {ex.Message}");
                    // Create a minimal service provider as fallback
                    var fallbackServices = new ServiceCollection();
                    _serviceProvider = fallbackServices.BuildServiceProvider();
                    System.Diagnostics.Debug.WriteLine("A3sist: Created fallback service provider");
                }

                // Register only essential services with VS Shell
                await RegisterEssentialVSShellServicesAsync();
                
                System.Diagnostics.Debug.WriteLine("A3sist: Essential services initialization completed");
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

        private async Task RegisterEssentialVSShellServicesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("A3sist: Registering essential services with VS Shell...");

                // Register only essential services during startup
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
                
                System.Diagnostics.Debug.WriteLine("A3sist: Essential VS Shell service registration completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Critical error in essential VS Shell service registration: {ex.Message}");
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
                    // Initialize only essential commands to speed up installation
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
                    
                    // Defer other commands until services are available
                    System.Diagnostics.Debug.WriteLine("A3sist: Essential command initialization completed");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("A3sist: CommandService is null, commands will not be available");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Error initializing commands: {ex.Message}");
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
                // Initialize services on first access to avoid startup delays
                if (_serviceProvider == null)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: First service request for {typeof(T).Name}, initializing services...");
                    _ = Task.Run(async () => await InitializeServicesAsync());
                    return null; // Return null for first call, services will be available on subsequent calls
                }
                
                var service = _serviceProvider.GetService<T>();
                if (service == null)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Service {typeof(T).Name} not found, attempting lazy initialization...");
                    
                    // Try to initialize remaining services if this is the first time a heavy service is requested
                    _ = Task.Run(async () => await InitializeRemainingServicesAsync());
                    
                    return null;
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

        private async Task InitializeRemainingServicesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("A3sist: Initializing remaining services...");
                
                var services = new ServiceCollection();
                
                // Copy existing services
                if (_serviceProvider != null)
                {
                    var existingConfig = _serviceProvider.GetService<IA3sistConfigurationService>();
                    var existingChat = _serviceProvider.GetService<IChatService>();
                    
                    if (existingConfig != null)
                        services.AddSingleton(existingConfig);
                    if (existingChat != null)
                        services.AddSingleton(existingChat);
                }
                
                // Add the heavy services that were deferred
                services.AddSingleton<IModelManagementService, ModelManagementService>();
                services.AddSingleton<IMCPClientService, MCPClientService>();
                services.AddSingleton<IRAGEngineService, RAGEngineService>();
                services.AddSingleton<ICodeAnalysisService, CodeAnalysisService>();
                services.AddSingleton<IRefactoringService, RefactoringService>();
                services.AddSingleton<IAutoCompleteService, AutoCompleteService>();
                services.AddSingleton<A3sist.Agent.IAgentModeService, A3sist.Agent.AgentModeService>();
                
                // Replace service provider
                var oldProvider = _serviceProvider;
                _serviceProvider = services.BuildServiceProvider();
                oldProvider?.Dispose();
                
                // Initialize remaining commands now that services are available
                await InitializeRemainingCommandsAsync();
                
                System.Diagnostics.Debug.WriteLine("A3sist: Remaining services initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Error initializing remaining services: {ex.Message}");
            }
        }

        private async Task InitializeRemainingCommandsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("A3sist: Initializing remaining commands...");
                
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                OleMenuCommandService commandService = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (commandService != null)
                {
                    // Initialize commands that require services
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
                    
                    System.Diagnostics.Debug.WriteLine("A3sist: Remaining commands initialized");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Error initializing remaining commands: {ex.Message}");
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