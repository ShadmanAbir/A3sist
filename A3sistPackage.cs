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
using System.Collections.Generic;
using System.Threading.Tasks;

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
    [ProvideAutoLoad(UIContextGuids.CodeWindow, PackageAutoLoadFlags.BackgroundLoad)]
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

        /// <summary>
        /// Indicates whether all services are fully initialized
        /// </summary>
        public bool AreServicesReady { get; private set; } = false;

        /// <summary>
        /// Tracks which services are initialized
        /// </summary>
        private readonly Dictionary<Type, bool> _serviceStatus = new Dictionary<Type, bool>();
        private readonly object _serviceLock = new object();

        /// <summary>
        /// Event fired when a specific service becomes ready
        /// </summary>
        public event EventHandler<ServiceReadyEventArgs> ServiceReady;

        /// <summary>
        /// Event fired when all services become ready
        /// </summary>
        public event EventHandler ServicesReady;

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
                
                System.Diagnostics.Debug.WriteLine("A3sist: Starting ultra-minimal package initialization...");
                
                await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                await base.InitializeAsync(cancellationToken, progress);

                // Initialize only tool window commands - NO services, NO background loading
                await InitializeEssentialCommandsAsync();

                // Tool window will be created but services load only on user interaction
                System.Diagnostics.Debug.WriteLine("A3sist: Ultra-minimal initialization completed - services load on-demand only");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist package initialization error: {ex.Message}");
                
                try
                {
                    await base.InitializeAsync(cancellationToken, progress);
                    System.Diagnostics.Debug.WriteLine("A3sist: Base package initialization completed despite errors.");
                }
                catch (Exception baseEx)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Base package initialization error: {baseEx.Message}");
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


        private async Task InitializeEssentialCommandsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("A3sist: Initializing essential commands only...");
                
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                OleMenuCommandService commandService = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (commandService != null)
                {
                    // Initialize only tool window commands for immediate access
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
                    
                    System.Diagnostics.Debug.WriteLine("A3sist: Essential commands ready - tool window accessible");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("A3sist: CommandService is null, commands will not be available");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Error initializing essential commands: {ex.Message}");
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
                // Check if this specific service is already available
                if (_serviceProvider != null)
                {
                    var service = _serviceProvider.GetService<T>();
                    if (service != null)
                    {
                        return service;
                    }
                }

                // Service not available - trigger on-demand loading
                System.Diagnostics.Debug.WriteLine($"A3sist: Service {typeof(T).Name} requested - triggering on-demand loading");
                
                // Start loading this specific service in background
                _ = Task.Run(async () => await LoadServiceOnDemandAsync<T>());
                
                return null; // Service will be available on next call
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Error getting service {typeof(T).Name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load a specific service on-demand with multi-threading
        /// </summary>
        public async Task<T> LoadServiceOnDemandAsync<T>() where T : class
        {
            try
            {
                var serviceType = typeof(T);
                
                // Check if already loading or loaded
                lock (_serviceLock)
                {
                    if (_serviceStatus.ContainsKey(serviceType))
                    {
                        return _serviceProvider?.GetService<T>();
                    }
                    _serviceStatus[serviceType] = false; // Mark as loading
                }

                System.Diagnostics.Debug.WriteLine($"A3sist: Loading service {serviceType.Name} on-demand...");

                // Initialize minimal services if not done yet
                if (_serviceProvider == null)
                {
                    await InitializeMinimalServicesAsync();
                }

                // Load specific service based on type
                await LoadSpecificServiceAsync<T>();
                
                // Mark as ready and notify
                lock (_serviceLock)
                {
                    _serviceStatus[serviceType] = true;
                }

                ServiceReady?.Invoke(this, new ServiceReadyEventArgs { ServiceType = serviceType });
                System.Diagnostics.Debug.WriteLine($"A3sist: Service {serviceType.Name} loaded successfully");
                
                return _serviceProvider?.GetService<T>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Error loading service {typeof(T).Name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Initialize only configuration service for basic functionality
        /// </summary>
        private async Task InitializeMinimalServicesAsync()
        {
            try
            {
                if (_serviceProvider != null) return; // Already initialized

                System.Diagnostics.Debug.WriteLine("A3sist: Initializing minimal services (configuration only)...");
                
                var services = new ServiceCollection();
                services.AddSingleton<IA3sistConfigurationService, A3sistConfigurationService>();
                
                _serviceProvider = services.BuildServiceProvider();
                
                // Register with VS Shell
                AddService(typeof(IA3sistConfigurationService), async (container, ct, type) =>
                {
                    return _serviceProvider?.GetService<IA3sistConfigurationService>();
                }, true);
                
                System.Diagnostics.Debug.WriteLine("A3sist: Minimal services initialized");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Error initializing minimal services: {ex.Message}");
            }
        }

        /// <summary>
        /// Load a specific service type on-demand
        /// </summary>
        private async Task LoadSpecificServiceAsync<T>()
        {
            try
            {
                var serviceType = typeof(T);
                var services = new ServiceCollection();
                
                // Copy existing services
                if (_serviceProvider != null)
                {
                    foreach (var existingService in _serviceProvider.GetServices<object>())
                    {
                        if (existingService != null)
                        {
                            services.AddSingleton(existingService.GetType(), existingService);
                        }
                    }
                }

                // Add the specific requested service and its dependencies
                if (serviceType == typeof(IChatService))
                {
                    services.AddSingleton<IChatService, ChatService>();
                    
                    // Chat might need model management
                    if (!_serviceStatus.ContainsKey(typeof(IModelManagementService)))
                    {
                        _ = Task.Run(async () => await LoadServiceOnDemandAsync<IModelManagementService>());
                    }
                }
                else if (serviceType == typeof(IModelManagementService))
                {
                    services.AddSingleton<IModelManagementService, ModelManagementService>();
                }
                else if (serviceType == typeof(IRefactoringService))
                {
                    services.AddSingleton<IRefactoringService, RefactoringService>();
                    
                    // Refactoring needs code analysis
                    if (!_serviceStatus.ContainsKey(typeof(ICodeAnalysisService)))
                    {
                        _ = Task.Run(async () => await LoadServiceOnDemandAsync<ICodeAnalysisService>());
                    }
                }
                else if (serviceType == typeof(ICodeAnalysisService))
                {
                    services.AddSingleton<ICodeAnalysisService, CodeAnalysisService>();
                }
                else if (serviceType == typeof(IAutoCompleteService))
                {
                    services.AddSingleton<IAutoCompleteService, AutoCompleteService>();
                }
                else if (serviceType == typeof(IRAGEngineService))
                {
                    services.AddSingleton<IRAGEngineService, RAGEngineService>();
                }
                else if (serviceType == typeof(IMCPClientService))
                {
                    services.AddSingleton<IMCPClientService, MCPClientService>();
                }
                else if (serviceType == typeof(A3sist.Agent.IAgentModeService))
                {
                    services.AddSingleton<A3sist.Agent.IAgentModeService, A3sist.Agent.AgentModeService>();
                }

                // Replace service provider
                var oldProvider = _serviceProvider;
                _serviceProvider = services.BuildServiceProvider();
                oldProvider?.Dispose();
                
                // Register with VS Shell if needed
                AddService(serviceType, async (container, ct, type) =>
                {
                    return _serviceProvider?.GetService(serviceType);
                }, true);
                
                System.Diagnostics.Debug.WriteLine($"A3sist: Service {serviceType.Name} loaded and registered");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Error loading specific service {typeof(T).Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Load chat service when user starts using chat functionality
        /// </summary>
        public async Task<IChatService> EnsureChatServiceAsync()
        {
            System.Diagnostics.Debug.WriteLine("A3sist: User requested chat - loading chat service...");
            return await LoadServiceOnDemandAsync<IChatService>();
        }

        /// <summary>
        /// Load refactoring service when user tries to refactor code
        /// </summary>
        public async Task<IRefactoringService> EnsureRefactoringServiceAsync()
        {
            System.Diagnostics.Debug.WriteLine("A3sist: User requested refactoring - loading refactoring service...");
            return await LoadServiceOnDemandAsync<IRefactoringService>();
        }

        /// <summary>
        /// Load model management when user opens configuration
        /// </summary>
        public async Task<IModelManagementService> EnsureModelServiceAsync()
        {
            System.Diagnostics.Debug.WriteLine("A3sist: User opened model config - loading model service...");
            return await LoadServiceOnDemandAsync<IModelManagementService>();
        }

        /// <summary>
        /// Load code analysis when user requests analysis
        /// </summary>
        public async Task<ICodeAnalysisService> EnsureCodeAnalysisServiceAsync()
        {
            System.Diagnostics.Debug.WriteLine("A3sist: User requested code analysis - loading analysis service...");
            return await LoadServiceOnDemandAsync<ICodeAnalysisService>();
        }

        /// <summary>
        /// Load autocomplete when user enables it
        /// </summary>
        public async Task<IAutoCompleteService> EnsureAutoCompleteServiceAsync()
        {
            System.Diagnostics.Debug.WriteLine("A3sist: User enabled autocomplete - loading autocomplete service...");
            return await LoadServiceOnDemandAsync<IAutoCompleteService>();
        }

        /// <summary>
        /// Check if a specific service is ready
        /// </summary>
        public bool IsServiceReady<T>()
        {
            lock (_serviceLock)
            {
                return _serviceStatus.ContainsKey(typeof(T)) && _serviceStatus[typeof(T)];
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

    /// <summary>
    /// Event arguments for when a service becomes ready
    /// </summary>
    public class ServiceReadyEventArgs : EventArgs
    {
        public Type ServiceType { get; set; }
    }
}