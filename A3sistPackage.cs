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
using System.Collections.Concurrent;
using System.Linq;

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
        private readonly IServiceCollection _services = new ServiceCollection();
        private readonly object _serviceProviderLock = new object();
        
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
        private readonly ConcurrentDictionary<Type, ServiceStatus> _serviceStatus = new ConcurrentDictionary<Type, ServiceStatus>();
        
        /// <summary>
        /// Tracks ongoing service loading tasks to prevent race conditions
        /// </summary>
        private readonly ConcurrentDictionary<Type, Task> _loadingTasks = new ConcurrentDictionary<Type, Task>();
        
        /// <summary>
        /// Semaphores to prevent concurrent loading of the same service type
        /// </summary>
        private readonly ConcurrentDictionary<Type, SemaphoreSlim> _serviceSemaphores = new ConcurrentDictionary<Type, SemaphoreSlim>();
        
        /// <summary>
        /// Service dependency mapping
        /// </summary>
        private readonly Dictionary<Type, Type[]> _serviceDependencies = new Dictionary<Type, Type[]>
        {
            [typeof(IChatService)] = new[] { typeof(IA3sistConfigurationService), typeof(IModelManagementService) },
            [typeof(IRefactoringService)] = new[] { typeof(IA3sistConfigurationService), typeof(ICodeAnalysisService) },
            [typeof(IAutoCompleteService)] = new[] { typeof(IA3sistConfigurationService), typeof(IModelManagementService) },
            [typeof(A3sist.Agent.IAgentModeService)] = new[] { typeof(ICodeAnalysisService), typeof(IRefactoringService), typeof(IRAGEngineService), typeof(IModelManagementService) }
        };

        /// <summary>
        /// Event fired when a specific service becomes ready
        /// </summary>
        public event EventHandler<ServiceReadyEventArgs> ServiceReady;

        /// <summary>
        /// Event fired when all services become ready
        /// </summary>
        public event EventHandler ServicesReady;

        #region Service Status Tracking
        
        /// <summary>
        /// Service status enumeration
        /// </summary>
        public enum ServiceStatus
        {
            NotLoaded,
            Loading,
            Ready,
            Failed
        }
        
        /// <summary>
        /// Service health information
        /// </summary>
        public class ServiceHealth
        {
            public ServiceStatus Status { get; set; }
            public DateTime? LoadedAt { get; set; }
            public string Error { get; set; }
            public TimeSpan? LoadTime { get; set; }
        }
        
        #endregion

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

                // Initialize minimal configuration service only
                await InitializeMinimalConfigurationAsync();

                // Initialize only tool window commands - NO heavy services, NO background loading
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

        private async Task InitializeMinimalConfigurationAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("A3sist: Initializing minimal configuration service...");
                
                // Only register configuration service initially
                _services.AddSingleton<IA3sistConfigurationService, A3sistConfigurationService>();
                
                lock (_serviceProviderLock)
                {
                    _serviceProvider = _services.BuildServiceProvider();
                }
                
                // Register with VS Shell
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
                
                // Mark configuration service as ready
                _serviceStatus[typeof(IA3sistConfigurationService)] = ServiceStatus.Ready;
                
                System.Diagnostics.Debug.WriteLine("A3sist: Minimal configuration initialized");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Error initializing minimal configuration: {ex.Message}");
                _serviceStatus[typeof(IA3sistConfigurationService)] = ServiceStatus.Failed;
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

        /// <summary>
        /// Gets a service asynchronously with proper race condition prevention
        /// </summary>
        public async ValueTask<T> GetServiceAsync<T>() where T : class
        {
            var serviceType = typeof(T);
            
            // Check if service is already ready
            if (_serviceStatus.TryGetValue(serviceType, out var status) && status == ServiceStatus.Ready)
            {
                lock (_serviceProviderLock)
                {
                    return _serviceProvider?.GetService<T>();
                }
            }
            
            // Check if service is currently loading
            if (_loadingTasks.TryGetValue(serviceType, out var existingTask))
            {
                await existingTask;
                lock (_serviceProviderLock)
                {
                    return _serviceProvider?.GetService<T>();
                }
            }
            
            // Start loading the service
            return await LoadServiceOnDemandAsync<T>();
        }

        /// <summary>
        /// Legacy synchronous method - returns null if service not ready, triggers async loading
        /// </summary>
        [Obsolete("Use GetServiceAsync<T>() instead for proper async service access")]
        public T GetService<T>() where T : class
        {
            try
            {
                var serviceType = typeof(T);
                
                // Check if service is already available
                if (_serviceStatus.TryGetValue(serviceType, out var status) && status == ServiceStatus.Ready)
                {
                    lock (_serviceProviderLock)
                    {
                        return _serviceProvider?.GetService<T>();
                    }
                }

                // Service not available - trigger on-demand loading
                System.Diagnostics.Debug.WriteLine($"A3sist: Service {serviceType.Name} requested - triggering on-demand loading");
                
                // Start loading this specific service in background (fire-and-forget)
                _ = LoadServiceOnDemandAsync<T>();
                
                return null; // Service will be available on next call
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Error getting service {typeof(T).Name}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Get service health information
        /// </summary>
        public ServiceHealth GetServiceHealth<T>()
        {
            var serviceType = typeof(T);
            var status = _serviceStatus.GetValueOrDefault(serviceType, ServiceStatus.NotLoaded);
            
            return new ServiceHealth
            {
                Status = status,
                LoadedAt = status == ServiceStatus.Ready ? DateTime.UtcNow : null, // Simplified for demo
                Error = status == ServiceStatus.Failed ? "Service failed to load" : null
            };
        }

        /// <summary>
        /// Load a specific service on-demand with proper race condition prevention and dependency handling
        /// </summary>
        public async Task<T> LoadServiceOnDemandAsync<T>() where T : class
        {
            var serviceType = typeof(T);
            var semaphore = _serviceSemaphores.GetOrAdd(serviceType, _ => new SemaphoreSlim(1, 1));
            
            try
            {
                await semaphore.WaitAsync();
                
                // Double-check if service was loaded while waiting
                if (_serviceStatus.TryGetValue(serviceType, out var currentStatus) && currentStatus == ServiceStatus.Ready)
                {
                    lock (_serviceProviderLock)
                    {
                        return _serviceProvider?.GetService<T>();
                    }
                }
                
                // Check if already loading
                if (currentStatus == ServiceStatus.Loading)
                {
                    // Another thread is loading, wait for it
                    if (_loadingTasks.TryGetValue(serviceType, out var loadingTask))
                    {
                        await loadingTask;
                        lock (_serviceProviderLock)
                        {
                            return _serviceProvider?.GetService<T>();
                        }
                    }
                }
                
                // Mark as loading
                _serviceStatus[serviceType] = ServiceStatus.Loading;
                System.Diagnostics.Debug.WriteLine($"A3sist: Loading service {serviceType.Name} on-demand...");
                
                var loadingTask = LoadSpecificServiceWithDependenciesAsync<T>();
                _loadingTasks[serviceType] = loadingTask;
                
                try
                {
                    await loadingTask;
                    
                    // Mark as ready and notify
                    _serviceStatus[serviceType] = ServiceStatus.Ready;
                    ServiceReady?.Invoke(this, new ServiceReadyEventArgs { ServiceType = serviceType });
                    System.Diagnostics.Debug.WriteLine($"A3sist: Service {serviceType.Name} loaded successfully");
                    
                    lock (_serviceProviderLock)
                    {
                        return _serviceProvider?.GetService<T>();
                    }
                }
                catch (Exception ex)
                {
                    _serviceStatus[serviceType] = ServiceStatus.Failed;
                    System.Diagnostics.Debug.WriteLine($"A3sist: Failed to load service {serviceType.Name}: {ex.Message}");
                    throw;
                }
                finally
                {
                    _loadingTasks.TryRemove(serviceType, out _);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Load a specific service type with its dependencies, without rebuilding the entire service provider
        /// </summary>
        private async Task LoadSpecificServiceWithDependenciesAsync<T>()
        {
            var serviceType = typeof(T);
            
            try
            {
                // Check configuration to see if this service should be loaded
                if (!await ShouldLoadServiceAsync<T>())
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Service {serviceType.Name} is disabled in configuration");
                    return;
                }
                
                // Load dependencies first
                if (_serviceDependencies.TryGetValue(serviceType, out var dependencies))
                {
                    foreach (var dependencyType in dependencies)
                    {
                        await EnsureDependencyLoadedAsync(dependencyType);
                    }
                }

                // Register the specific service
                RegisterSpecificService<T>();
                
                // Rebuild service provider only if necessary
                await RebuildServiceProviderIfNeededAsync();
                
                // Register with VS Shell
                AddService(serviceType, async (container, ct, type) =>
                {
                    lock (_serviceProviderLock)
                    {
                        return _serviceProvider?.GetService(serviceType);
                    }
                }, true);
                
                System.Diagnostics.Debug.WriteLine($"A3sist: Service {serviceType.Name} loaded and registered");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Error loading specific service {serviceType.Name}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Check if a service should be loaded based on configuration
        /// </summary>
        private async Task<bool> ShouldLoadServiceAsync<T>()
        {
            try
            {
                // Get configuration service if available
                var configService = _serviceProvider?.GetService<IA3sistConfigurationService>();
                if (configService == null) return true; // Default to enabled if config not available
                
                var serviceType = typeof(T);
                var configKey = $"services.{serviceType.Name}.enabled";
                
                return await configService.GetSettingAsync(configKey, true); // Default enabled
            }
            catch
            {
                return true; // Default to enabled on error
            }
        }
        
        /// <summary>
        /// Ensure a dependency is loaded before loading the dependent service
        /// </summary>
        private async Task EnsureDependencyLoadedAsync(Type dependencyType)
        {
            if (_serviceStatus.TryGetValue(dependencyType, out var status) && status == ServiceStatus.Ready)
            {
                return; // Already loaded
            }
            
            if (status == ServiceStatus.Loading)
            {
                // Wait for ongoing load
                if (_loadingTasks.TryGetValue(dependencyType, out var loadingTask))
                {
                    await loadingTask;
                }
                return;
            }
            
            // Load dependency using reflection to call generic method
            var method = typeof(A3sistPackage).GetMethod(nameof(LoadServiceOnDemandAsync));
            var genericMethod = method.MakeGenericMethod(dependencyType);
            var task = (Task)genericMethod.Invoke(this, null);
            await task;
        }
        
        /// <summary>
        /// Register a specific service type without rebuilding provider
        /// </summary>
        private void RegisterSpecificService<T>()
        {
            var serviceType = typeof(T);
            
            if (serviceType == typeof(IChatService))
            {
                _services.AddSingleton<IChatService, ChatService>();
            }
            else if (serviceType == typeof(IModelManagementService))
            {
                _services.AddSingleton<IModelManagementService, ModelManagementService>();
            }
            else if (serviceType == typeof(IRefactoringService))
            {
                _services.AddSingleton<IRefactoringService, RefactoringService>();
            }
            else if (serviceType == typeof(ICodeAnalysisService))
            {
                _services.AddSingleton<ICodeAnalysisService, CodeAnalysisService>();
            }
            else if (serviceType == typeof(IAutoCompleteService))
            {
                _services.AddSingleton<IAutoCompleteService, AutoCompleteService>();
            }
            else if (serviceType == typeof(IRAGEngineService))
            {
                _services.AddSingleton<IRAGEngineService, RAGEngineService>();
            }
            else if (serviceType == typeof(IMCPClientService))
            {
                _services.AddSingleton<IMCPClientService, MCPClientService>();
            }
            else if (serviceType == typeof(A3sist.Agent.IAgentModeService))
            {
                _services.AddSingleton<A3sist.Agent.IAgentModeService, A3sist.Agent.AgentModeService>();
            }
        }
        
        /// <summary>
        /// Rebuild service provider only when necessary
        /// </summary>
        private async Task RebuildServiceProviderIfNeededAsync()
        {
            lock (_serviceProviderLock)
            {
                var oldProvider = _serviceProvider;
                _serviceProvider = _services.BuildServiceProvider();
                oldProvider?.Dispose();
            }
        }

        /// <summary>
        /// Load chat service when user starts using chat functionality
        /// </summary>
        public async Task<IChatService> EnsureChatServiceAsync()
        {
            System.Diagnostics.Debug.WriteLine("A3sist: User requested chat - loading chat service...");
            return await GetServiceAsync<IChatService>();
        }

        /// <summary>
        /// Load refactoring service when user tries to refactor code
        /// </summary>
        public async Task<IRefactoringService> EnsureRefactoringServiceAsync()
        {
            System.Diagnostics.Debug.WriteLine("A3sist: User requested refactoring - loading refactoring service...");
            return await GetServiceAsync<IRefactoringService>();
        }

        /// <summary>
        /// Load model management when user opens configuration
        /// </summary>
        public async Task<IModelManagementService> EnsureModelServiceAsync()
        {
            System.Diagnostics.Debug.WriteLine("A3sist: User opened model config - loading model service...");
            return await GetServiceAsync<IModelManagementService>();
        }

        /// <summary>
        /// Load code analysis when user requests analysis
        /// </summary>
        public async Task<ICodeAnalysisService> EnsureCodeAnalysisServiceAsync()
        {
            System.Diagnostics.Debug.WriteLine("A3sist: User requested code analysis - loading analysis service...");
            return await GetServiceAsync<ICodeAnalysisService>();
        }

        /// <summary>
        /// Load autocomplete when user enables it
        /// </summary>
        public async Task<IAutoCompleteService> EnsureAutoCompleteServiceAsync()
        {
            System.Diagnostics.Debug.WriteLine("A3sist: User enabled autocomplete - loading autocomplete service...");
            return await GetServiceAsync<IAutoCompleteService>();
        }

        /// <summary>
        /// Load RAG engine when user uses knowledge features
        /// </summary>
        public async Task<IRAGEngineService> EnsureRAGServiceAsync()
        {
            System.Diagnostics.Debug.WriteLine("A3sist: User requested RAG features - loading RAG service...");
            return await GetServiceAsync<IRAGEngineService>();
        }

        /// <summary>
        /// Load agent mode service when user activates agent analysis
        /// </summary>
        public async Task<A3sist.Agent.IAgentModeService> EnsureAgentServiceAsync()
        {
            System.Diagnostics.Debug.WriteLine("A3sist: User activated agent mode - loading agent service...");
            return await GetServiceAsync<A3sist.Agent.IAgentModeService>();
        }

        /// <summary>
        /// Check if a specific service is ready
        /// </summary>
        public bool IsServiceReady<T>()
        {
            var serviceType = typeof(T);
            return _serviceStatus.TryGetValue(serviceType, out var status) && status == ServiceStatus.Ready;
        }
        
        /// <summary>
        /// Check if a specific service is loading
        /// </summary>
        public bool IsServiceLoading<T>()
        {
            var serviceType = typeof(T);
            return _serviceStatus.TryGetValue(serviceType, out var status) && status == ServiceStatus.Loading;
        }
        
        /// <summary>
        /// Get all service statuses for debugging
        /// </summary>
        public Dictionary<string, ServiceStatus> GetAllServiceStatuses()
        {
            return _serviceStatus.ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("A3sist: Disposing package...");
                    
                    // Cancel any ongoing loading tasks
                    foreach (var loadingTask in _loadingTasks.Values)
                    {
                        try
                        {
                            if (!loadingTask.IsCompleted)
                            {
                                loadingTask.Wait(TimeSpan.FromSeconds(5)); // Give tasks time to complete
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"A3sist: Error waiting for loading task: {ex.Message}");
                        }
                    }
                    _loadingTasks.Clear();
                    
                    // Dispose semaphores
                    foreach (var semaphore in _serviceSemaphores.Values)
                    {
                        try
                        {
                            semaphore?.Dispose();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"A3sist: Error disposing semaphore: {ex.Message}");
                        }
                    }
                    _serviceSemaphores.Clear();
                    
                    // Dispose service provider
                    if (_serviceProvider != null)
                    {
                        _serviceProvider.Dispose();
                        _serviceProvider = null;
                        System.Diagnostics.Debug.WriteLine("A3sist: Service provider disposed");
                    }
                    
                    // Clear service status
                    _serviceStatus.Clear();
                    
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