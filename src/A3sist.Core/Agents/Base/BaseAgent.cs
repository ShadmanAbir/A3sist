using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Core.Agents.Base
{
    /// <summary>
    /// Base abstract class providing common agent functionality including logging, configuration, lifecycle management,
    /// error handling, retry logic, and performance monitoring
    /// </summary>
    public abstract class BaseAgent : IAgent
    {
        protected readonly ILogger Logger;
        protected readonly IAgentConfiguration Configuration;
        protected readonly IValidationService? ValidationService;
        protected readonly IPerformanceMonitoringService? PerformanceMonitoringService;
        private readonly AgentMetrics _metrics;
        private readonly SemaphoreSlim _initializationSemaphore;
        private bool _isInitialized;
        private bool _isShuttingDown;
        private DateTime _startTime;

        /// <summary>
        /// Gets the unique name of the agent
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the type of the agent
        /// </summary>
        public abstract AgentType Type { get; }

        /// <summary>
        /// Gets the current status of the agent
        /// </summary>
        public WorkStatus Status { get; private set; } = WorkStatus.Pending;

        /// <summary>
        /// Gets the current health status of the agent
        /// </summary>
        public HealthStatus Health { get; private set; } = HealthStatus.Unknown;

        /// <summary>
        /// Gets the agent metrics
        /// </summary>
        public AgentMetrics Metrics => _metrics;

        protected BaseAgent(
            ILogger logger, 
            IAgentConfiguration configuration,
            IValidationService? validationService = null,
            IPerformanceMonitoringService? performanceMonitoringService = null)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            ValidationService = validationService;
            PerformanceMonitoringService = performanceMonitoringService;
            _metrics = new AgentMetrics();
            _initializationSemaphore = new SemaphoreSlim(1, 1);
            _startTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Handles an agent request asynchronously with error handling, retry logic, and performance monitoring
        /// </summary>
        public async Task<AgentResult> HandleAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                var errorResult = AgentResult.CreateFailure("Request cannot be null", agentName: Name);
                Logger.LogError("Received null request in agent {AgentName}", Name);
                return errorResult;
            }

            if (_isShuttingDown)
            {
                var shutdownResult = AgentResult.CreateFailure("Agent is shutting down", agentName: Name);
                Logger.LogWarning("Request {RequestId} rejected - agent {AgentName} is shutting down", request.Id, Name);
                return shutdownResult;
            }

            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            // Validate request if validation service is available
            if (ValidationService != null)
            {
                var validationResult = await ValidationService.ValidateRequestAsync(request);
                if (!validationResult.IsValid)
                {
                    var validationErrorResult = AgentResult.CreateFailure(
                        $"Request validation failed: {string.Join(", ", validationResult.Errors)}", 
                        agentName: Name);
                    Logger.LogWarning("Request {RequestId} validation failed in agent {AgentName}: {Errors}", 
                        request.Id, Name, string.Join(", ", validationResult.Errors));
                    return validationErrorResult;
                }
                
                // Log warnings if any
                if (validationResult.Warnings.Any())
                {
                    Logger.LogWarning("Request {RequestId} has validation warnings: {Warnings}", 
                        request.Id, string.Join(", ", validationResult.Warnings));
                }
            }

            var stopwatch = Stopwatch.StartNew();
            Logger.LogInformation("Processing request {RequestId} in agent {AgentName}", request.Id, Name);

            // Start performance monitoring if available
            PerformanceMonitoringService?.StartOperation($"{Name}_HandleRequest_{request.Id}");

            try
            {
                _metrics.IncrementTasksProcessed();
                Status = WorkStatus.InProgress;

                // Get retry policy from configuration
                var agentConfig = await Configuration.GetAgentConfigurationAsync(Name);
                var retryPolicy = CreateRetryPolicy(agentConfig?.RetryPolicy);

                // Execute with retry policy
                var result = await retryPolicy.ExecuteAsync(async (ct) =>
                {
                    return await HandleRequestAsync(request, ct);
                }, cancellationToken);

                stopwatch.Stop();
                result.ProcessingTime = stopwatch.Elapsed;
                result.AgentName = Name;

                if (result.Success)
                {
                    _metrics.IncrementTasksSucceeded();
                    Logger.LogInformation("Successfully processed request {RequestId} in {ElapsedMs}ms", 
                        request.Id, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _metrics.IncrementTasksFailed();
                    Logger.LogWarning("Failed to process request {RequestId}: {ErrorMessage}", 
                        request.Id, result.Message);
                }

                _metrics.UpdateAverageProcessingTime(stopwatch.Elapsed);
                Status = WorkStatus.Completed;
                Health = HealthStatus.Healthy;

                // Record performance metrics
                PerformanceMonitoringService?.RecordAgentExecution(Name, stopwatch.Elapsed, result.Success);
                PerformanceMonitoringService?.EndOperation($"{Name}_HandleRequest_{request.Id}", result.Success);

                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _metrics.IncrementTasksFailed();
                Status = WorkStatus.Cancelled;
                Logger.LogWarning("Request {RequestId} was cancelled after {ElapsedMs}ms", 
                    request.Id, stopwatch.ElapsedMilliseconds);
                
                PerformanceMonitoringService?.RecordAgentExecution(Name, stopwatch.Elapsed, false);
                PerformanceMonitoringService?.EndOperation($"{Name}_HandleRequest_{request.Id}", false);
                
                return AgentResult.CreateFailure("Operation was cancelled", agentName: Name);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.IncrementTasksFailed();
                Status = WorkStatus.Failed;
                Health = HealthStatus.Critical;
                
                Logger.LogError(ex, "Unhandled exception in agent {AgentName} processing request {RequestId}", 
                    Name, request.Id);
                
                PerformanceMonitoringService?.RecordAgentExecution(Name, stopwatch.Elapsed, false);
                PerformanceMonitoringService?.EndOperation($"{Name}_HandleRequest_{request.Id}", false);
                
                return AgentResult.CreateFailure($"Unhandled exception: {ex.Message}", ex, Name);
            }
        }

        /// <summary>
        /// Determines if this agent can handle the specified request
        /// </summary>
        public virtual async Task<bool> CanHandleAsync(AgentRequest request)
        {
            if (request == null || _isShuttingDown)
                return false;

            try
            {
                // Check if preferred agent type matches
                if (request.PreferredAgentType.HasValue && request.PreferredAgentType.Value != Type)
                    return false;

                // Allow derived classes to implement custom logic
                return await CanHandleRequestAsync(request);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error determining if agent {AgentName} can handle request {RequestId}", 
                    Name, request.Id);
                return false;
            }
        }

        /// <summary>
        /// Initializes the agent asynchronously
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            await _initializationSemaphore.WaitAsync();
            try
            {
                if (_isInitialized)
                    return;

                Logger.LogInformation("Initializing agent {AgentName}", Name);
                
                await InitializeAgentAsync();
                
                _isInitialized = true;
                Status = WorkStatus.Pending;
                Health = HealthStatus.Healthy;
                _startTime = DateTime.UtcNow;
                
                Logger.LogInformation("Agent {AgentName} initialized successfully", Name);
            }
            catch (Exception ex)
            {
                Health = HealthStatus.Unhealthy;
                Logger.LogError(ex, "Failed to initialize agent {AgentName}", Name);
                throw;
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        /// <summary>
        /// Shuts down the agent asynchronously
        /// </summary>
        public async Task ShutdownAsync()
        {
            if (_isShuttingDown)
                return;

            _isShuttingDown = true;
            Logger.LogInformation("Shutting down agent {AgentName}", Name);

            try
            {
                await ShutdownAgentAsync();
                Status = WorkStatus.Completed;
                Logger.LogInformation("Agent {AgentName} shut down successfully", Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during agent {AgentName} shutdown", Name);
                throw;
            }
        }

        /// <summary>
        /// Gets the current status of the agent
        /// </summary>
        public virtual AgentStatus GetStatus()
        {
            return new AgentStatus
            {
                Name = Name,
                Type = Type,
                Status = Status,
                Health = Health,
                LastActivity = _metrics.LastActivity,
                TasksProcessed = _metrics.TasksProcessed,
                TasksSucceeded = _metrics.TasksSucceeded,
                TasksFailed = _metrics.TasksFailed,
                AverageProcessingTime = _metrics.AverageProcessingTime,
                StartedAt = _startTime,
                Version = GetType().Assembly.GetName().Version?.ToString() ?? "Unknown",
                MemoryUsage = GC.GetTotalMemory(false),
                CpuUsage = 0.0 // TODO: Implement CPU usage monitoring
            };
        }

        /// <summary>
        /// Abstract method for derived classes to implement request handling logic
        /// </summary>
        protected abstract Task<AgentResult> HandleRequestAsync(AgentRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Virtual method for derived classes to implement custom request filtering logic
        /// </summary>
        protected virtual Task<bool> CanHandleRequestAsync(AgentRequest request)
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Virtual method for derived classes to implement custom initialization logic
        /// </summary>
        protected virtual Task InitializeAgentAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Virtual method for derived classes to implement custom shutdown logic
        /// </summary>
        protected virtual Task ShutdownAgentAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates a retry policy based on configuration
        /// </summary>
        private IAsyncPolicy<AgentResult> CreateRetryPolicy(RetryPolicy? retryPolicy)
        {
            if (retryPolicy == null || retryPolicy.MaxRetries <= 0)
            {
                return Policy.NoOpAsync<AgentResult>();
            }

            var policyBuilder = Policy
                .Handle<Exception>(ex => ShouldRetry(ex, retryPolicy))
                .WaitAndRetryAsync(
                    retryPolicy.MaxRetries,
                    retryAttempt => CalculateDelay(retryAttempt, retryPolicy),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        Logger.LogWarning("Retry attempt {RetryCount} for agent {AgentName} after {Delay}ms. Exception: {Exception}",
                            retryCount, Name, timespan.TotalMilliseconds, outcome.Exception?.Message);
                    });

            return policyBuilder;
        }

        /// <summary>
        /// Determines if an exception should trigger a retry
        /// </summary>
        private bool ShouldRetry(Exception exception, RetryPolicy retryPolicy)
        {
            if (retryPolicy.RetryableExceptions == null || retryPolicy.RetryableExceptions.Length == 0)
                return false;

            var exceptionType = exception.GetType().FullName;
            foreach (var retryableType in retryPolicy.RetryableExceptions)
            {
                if (string.Equals(exceptionType, retryableType, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates the delay for retry attempts
        /// </summary>
        private TimeSpan CalculateDelay(int retryAttempt, RetryPolicy retryPolicy)
        {
            var delay = retryPolicy.InitialDelay;

            if (retryPolicy.UseExponentialBackoff)
            {
                delay = TimeSpan.FromMilliseconds(
                    retryPolicy.InitialDelay.TotalMilliseconds * Math.Pow(retryPolicy.BackoffMultiplier, retryAttempt - 1));
            }

            if (delay > retryPolicy.MaxDelay)
                delay = retryPolicy.MaxDelay;

            if (retryPolicy.UseJitter)
            {
                var random = new Random();
                var jitter = random.NextDouble() * 0.1; // 10% jitter
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * (1 + jitter));
            }

            return delay;
        }

        /// <summary>
        /// Disposes the agent resources
        /// </summary>
        public virtual void Dispose()
        {
            _initializationSemaphore?.Dispose();
        }
    }
}