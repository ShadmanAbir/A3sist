using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Core.Services
{
    /// <summary>
    /// Enhanced orchestrator that coordinates agent interactions with proper error handling, monitoring, and advanced routing
    /// </summary>
    public class Orchestrator : IOrchestrator, IDisposable
    {
        private readonly IAgentManager _agentManager;
        private readonly ITaskQueueService _taskQueueService;
        private readonly IWorkflowService _workflowService;
        private readonly ILogger<Orchestrator> _logger;
        private readonly IAgentConfiguration _configuration;
        private readonly SemaphoreSlim _processingLock;
        private readonly Dictionary<string, int> _agentLoadBalancer;
        private readonly Dictionary<string, DateTime> _agentLastActivity;
        private readonly Dictionary<string, int> _agentFailureCount;
        private readonly Timer _healthCheckTimer;
        private bool _disposed;
        private bool _initialized;

        public Orchestrator(
            IAgentManager agentManager,
            ITaskQueueService taskQueueService,
            IWorkflowService workflowService,
            ILogger<Orchestrator> logger,
            IAgentConfiguration configuration)
        {
            _agentManager = agentManager ?? throw new ArgumentNullException(nameof(agentManager));
            _taskQueueService = taskQueueService ?? throw new ArgumentNullException(nameof(taskQueueService));
            _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            
            _processingLock = new SemaphoreSlim(1, 1);
            _agentLoadBalancer = new Dictionary<string, int>();
            _agentLastActivity = new Dictionary<string, DateTime>();
            _agentFailureCount = new Dictionary<string, int>();
            
            // Set up health check timer (every 30 seconds)
            _healthCheckTimer = new Timer(PerformHealthCheck, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Initializes the orchestrator
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            _logger.LogInformation("Initializing Orchestrator");

            try
            {
                // Start all registered agents
                await _agentManager.StartAllAgentsAsync();
                
                _initialized = true;
                _logger.LogInformation("Orchestrator initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Orchestrator");
                throw;
            }
        }

        /// <summary>
        /// Processes a request by routing it to appropriate agents with enhanced error handling and recovery
        /// </summary>
        public async Task<AgentResult> ProcessRequestAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!_initialized)
            {
                _logger.LogWarning("Orchestrator not initialized, initializing now");
                await InitializeAsync();
            }

            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Processing request {RequestId} with prompt: {Prompt}", 
                request.Id, request.Prompt?.Substring(0, Math.Min(100, request.Prompt.Length ?? 0)));

            try
            {
                // Step 1: Validate request
                var validationResult = ValidateRequest(request);
                if (!validationResult.Success)
                {
                    _logger.LogWarning("Request validation failed for {RequestId}: {Message}", 
                        request.Id, validationResult.Message);
                    return validationResult;
                }

                // Step 2: Check if we should use workflow processing
                if (ShouldUseWorkflow(request))
                {
                    _logger.LogDebug("Using workflow processing for request {RequestId}", request.Id);
                    var workflowResult = await _workflowService.ExecuteWorkflowAsync(request, cancellationToken);
                    
                    stopwatch.Stop();
                    _logger.LogInformation("Workflow processing completed for request {RequestId} in {ElapsedMs}ms with success: {Success}", 
                        request.Id, stopwatch.ElapsedMilliseconds, workflowResult.Success);
                    
                    return workflowResult.Result;
                }

                // Step 3: Use enhanced agent selection with intent routing
                var routingResult = await RouteRequestAsync(request, cancellationToken);
                if (!routingResult.Success)
                {
                    _logger.LogWarning("Request routing failed for {RequestId}: {Message}", 
                        request.Id, routingResult.Message);
                    return routingResult;
                }

                var selectedAgent = routingResult.Metadata?["SelectedAgent"] as IAgent;
                if (selectedAgent == null)
                {
                    var errorMessage = "No suitable agent found after routing";
                    _logger.LogWarning(errorMessage);
                    return AgentResult.CreateFailure(errorMessage);
                }

                // Step 4: Process request with selected agent using retry logic
                var result = await ProcessWithAgentWithRetryAsync(selectedAgent, request, cancellationToken);

                // Step 5: Update metrics and monitoring
                UpdateAgentMetrics(selectedAgent.Name, result.Success, stopwatch.Elapsed);

                stopwatch.Stop();
                _logger.LogInformation("Request {RequestId} processed in {ElapsedMs}ms by agent {AgentName} with result: {Success}", 
                    request.Id, stopwatch.ElapsedMilliseconds, selectedAgent.Name, result.Success);

                return result;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                _logger.LogInformation("Request {RequestId} was cancelled after {ElapsedMs}ms", request.Id, stopwatch.ElapsedMilliseconds);
                return AgentResult.CreateFailure("Request was cancelled");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Unexpected error processing request {RequestId} after {ElapsedMs}ms", 
                    request.Id, stopwatch.ElapsedMilliseconds);
                
                // Attempt recovery if possible
                var recoveryResult = await AttemptRecoveryAsync(request, ex, cancellationToken);
                if (recoveryResult != null)
                {
                    _logger.LogInformation("Recovery successful for request {RequestId}", request.Id);
                    return recoveryResult;
                }
                
                return AgentResult.CreateFailure($"Orchestration failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets all available agents
        /// </summary>
        public async Task<IEnumerable<IAgent>> GetAvailableAgentsAsync()
        {
            try
            {
                return await _agentManager.GetAgentsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available agents");
                return Enumerable.Empty<IAgent>();
            }
        }

        /// <summary>
        /// Registers an agent with the orchestrator
        /// </summary>
        public async Task RegisterAgentAsync(IAgent agent)
        {
            if (agent == null)
                throw new ArgumentNullException(nameof(agent));

            try
            {
                await _agentManager.RegisterAgentAsync(agent);
                _agentLoadBalancer[agent.Name] = 0;
                _logger.LogInformation("Agent {AgentName} registered with orchestrator", agent.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register agent {AgentName}", agent.Name);
                throw;
            }
        }

        /// <summary>
        /// Unregisters an agent from the orchestrator
        /// </summary>
        public async Task UnregisterAgentAsync(string agentName)
        {
            if (string.IsNullOrWhiteSpace(agentName))
                return;

            try
            {
                await _agentManager.UnregisterAgentAsync(agentName);
                _agentLoadBalancer.Remove(agentName);
                _logger.LogInformation("Agent {AgentName} unregistered from orchestrator", agentName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister agent {AgentName}", agentName);
                throw;
            }
        }

        /// <summary>
        /// Shuts down the orchestrator
        /// </summary>
        public async Task ShutdownAsync()
        {
            if (!_initialized)
                return;

            _logger.LogInformation("Shutting down Orchestrator");

            try
            {
                await _agentManager.StopAllAgentsAsync();
                _initialized = false;
                _logger.LogInformation("Orchestrator shut down successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during orchestrator shutdown");
                throw;
            }
        }

        /// <summary>
        /// Validates the incoming request
        /// </summary>
        private AgentResult ValidateRequest(AgentRequest request)
        {
            if (request.Id == Guid.Empty)
                return AgentResult.CreateFailure("Request ID is required");

            if (string.IsNullOrWhiteSpace(request.Prompt))
                return AgentResult.CreateFailure("Request prompt is required");

            // Additional validation can be added here
            return AgentResult.CreateSuccess("Request validation passed");
        }

        /// <summary>
        /// Selects the most appropriate agent for the request
        /// </summary>
        private async Task<IAgent?> SelectAgentAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                // Get all available agents
                var agents = await _agentManager.GetAgentsAsync();
                var availableAgents = agents.ToList();

                if (!availableAgents.Any())
                {
                    _logger.LogWarning("No agents available for request processing");
                    return null;
                }

                // First, try to find agents that can handle the request
                var capableAgents = new List<IAgent>();
                foreach (var agent in availableAgents)
                {
                    try
                    {
                        if (await agent.CanHandleAsync(request))
                        {
                            capableAgents.Add(agent);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error checking if agent {AgentName} can handle request", agent.Name);
                    }
                }

                if (!capableAgents.Any())
                {
                    _logger.LogWarning("No capable agents found for request type: {RequestType}", request.PreferredAgentType);
                    return null;
                }

                // If preferred agent type is specified, try to find it first
                if (request.PreferredAgentType != AgentType.Unknown)
                {
                    var preferredAgent = capableAgents.FirstOrDefault(a => a.Type == request.PreferredAgentType);
                    if (preferredAgent != null)
                    {
                        _logger.LogDebug("Selected preferred agent {AgentName} of type {AgentType}", 
                            preferredAgent.Name, preferredAgent.Type);
                        return preferredAgent;
                    }
                }

                // Use load balancing to select the least loaded capable agent
                var selectedAgent = capableAgents
                    .OrderBy(a => _agentLoadBalancer.GetValueOrDefault(a.Name, 0))
                    .First();

                _logger.LogDebug("Selected agent {AgentName} via load balancing", selectedAgent.Name);
                return selectedAgent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting agent for request");
                return null;
            }
        }

        /// <summary>
        /// Processes the request with the selected agent
        /// </summary>
        private async Task<AgentResult> ProcessWithAgentAsync(IAgent agent, AgentRequest request, CancellationToken cancellationToken)
        {
            var agentStopwatch = Stopwatch.StartNew();
            
            try
            {
                _logger.LogDebug("Processing request {RequestId} with agent {AgentName}", request.Id, agent.Name);

                // Increment load counter
                _agentLoadBalancer[agent.Name] = _agentLoadBalancer.GetValueOrDefault(agent.Name, 0) + 1;

                // Process the request
                var result = await agent.HandleAsync(request, cancellationToken);

                agentStopwatch.Stop();

                // Enhance result with processing metadata
                if (result.Metadata == null)
                    result.Metadata = new Dictionary<string, object>();

                result.Metadata["AgentName"] = agent.Name;
                result.Metadata["AgentType"] = agent.Type.ToString();
                result.Metadata["ProcessingTimeMs"] = agentStopwatch.ElapsedMilliseconds;
                result.ProcessingTime = agentStopwatch.Elapsed;

                _logger.LogDebug("Agent {AgentName} processed request {RequestId} in {ElapsedMs}ms with success: {Success}", 
                    agent.Name, request.Id, agentStopwatch.ElapsedMilliseconds, result.Success);

                return result;
            }
            catch (Exception ex)
            {
                agentStopwatch.Stop();
                _logger.LogError(ex, "Agent {AgentName} failed to process request {RequestId} after {ElapsedMs}ms", 
                    agent.Name, request.Id, agentStopwatch.ElapsedMilliseconds);

                return AgentResult.CreateFailure($"Agent {agent.Name} failed: {ex.Message}", ex);
            }
            finally
            {
                // Decrement load counter
                if (_agentLoadBalancer.ContainsKey(agent.Name))
                {
                    _agentLoadBalancer[agent.Name] = Math.Max(0, _agentLoadBalancer[agent.Name] - 1);
                }
            }
        }

        /// <summary>
        /// Updates load balancer metrics based on agent performance
        /// </summary>
        private void UpdateLoadBalancerMetrics(string agentName, bool success)
        {
            // This is a simple implementation - could be enhanced with more sophisticated metrics
            // For now, we just track current load which is handled in ProcessWithAgentAsync
            _logger.LogTrace("Updated load balancer metrics for agent {AgentName}, success: {Success}", agentName, success);
        }

        /// <summary>
        /// Determines if the request should use workflow processing
        /// </summary>
        private bool ShouldUseWorkflow(AgentRequest request)
        {
            // Use workflow for complex requests that might need multiple agents
            return request.Context?.ContainsKey("UseWorkflow") == true ||
                   request.Prompt?.Contains("workflow", StringComparison.OrdinalIgnoreCase) == true ||
                   request.Prompt?.Contains("multi-step", StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// Routes the request using enhanced intent-based routing
        /// </summary>
        private async Task<AgentResult> RouteRequestAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                // Try to get the IntentRouter agent for advanced routing
                var intentRouter = await _agentManager.GetAgentAsync(AgentType.IntentRouter);
                if (intentRouter != null)
                {
                    _logger.LogDebug("Using IntentRouter for request {RequestId}", request.Id);
                    var routingResult = await intentRouter.HandleAsync(request, cancellationToken);
                    
                    if (routingResult.Success && routingResult.Metadata?.ContainsKey("RoutingDecision") == true)
                    {
                        var routingDecision = routingResult.Metadata["RoutingDecision"] as RoutingDecision;
                        if (routingDecision != null)
                        {
                            var targetAgent = await _agentManager.GetAgentAsync(routingDecision.TargetAgent);
                            if (targetAgent != null)
                            {
                                var result = AgentResult.CreateSuccess("Agent routing completed");
                                result.Metadata = new Dictionary<string, object> { ["SelectedAgent"] = targetAgent };
                                return result;
                            }
                        }
                    }
                }

                // Fallback to traditional agent selection
                var agent = await SelectAgentAsync(request, cancellationToken);
                if (agent != null)
                {
                    var result = AgentResult.CreateSuccess("Agent selection completed");
                    result.Metadata = new Dictionary<string, object> { ["SelectedAgent"] = agent };
                    return result;
                }

                return AgentResult.CreateFailure("No suitable agent found for request");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error routing request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Routing failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Processes the request with an agent using retry logic and circuit breaker pattern
        /// </summary>
        private async Task<AgentResult> ProcessWithAgentWithRetryAsync(IAgent agent, AgentRequest request, CancellationToken cancellationToken)
        {
            // Get configuration values with defaults
            var agentConfig = await _configuration.GetAgentConfigurationAsync("Orchestrator");
            var maxRetries = agentConfig?.RetryPolicy?.MaxRetries ?? 3;
            var retryDelay = (int)(agentConfig?.RetryPolicy?.InitialDelay.TotalMilliseconds ?? 1000);
            var circuitBreakerThreshold = 5; // Default circuit breaker threshold

            // Check circuit breaker
            var failureCount = _agentFailureCount.GetValueOrDefault(agent.Name, 0);
            if (failureCount >= circuitBreakerThreshold)
            {
                _logger.LogWarning("Circuit breaker open for agent {AgentName} (failures: {FailureCount})", 
                    agent.Name, failureCount);
                return AgentResult.CreateFailure($"Agent {agent.Name} is temporarily unavailable due to repeated failures");
            }

            Exception? lastException = null;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogDebug("Processing request {RequestId} with agent {AgentName} (attempt {Attempt}/{MaxRetries})", 
                        request.Id, agent.Name, attempt, maxRetries);

                    var result = await ProcessWithAgentAsync(agent, request, cancellationToken);
                    
                    if (result.Success)
                    {
                        // Reset failure count on success
                        _agentFailureCount[agent.Name] = 0;
                        return result;
                    }

                    // If it's a non-retryable error, don't retry
                    if (IsNonRetryableError(result))
                    {
                        _logger.LogWarning("Non-retryable error from agent {AgentName}: {Message}", 
                            agent.Name, result.Message);
                        break;
                    }

                    lastException = result.Exception;
                    _logger.LogWarning("Agent {AgentName} failed on attempt {Attempt}: {Message}", 
                        agent.Name, attempt, result.Message);
                }
                catch (OperationCanceledException)
                {
                    throw; // Don't retry cancellation
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "Agent {AgentName} threw exception on attempt {Attempt}", 
                        agent.Name, attempt);
                }

                // Wait before retry (except on last attempt)
                if (attempt < maxRetries)
                {
                    await Task.Delay(retryDelay * attempt, cancellationToken); // Exponential backoff
                }
            }

            // All retries failed, increment failure count
            _agentFailureCount[agent.Name] = failureCount + 1;
            
            var errorMessage = $"Agent {agent.Name} failed after {maxRetries} attempts";
            _logger.LogError("Agent {AgentName} failed after {MaxRetries} attempts. Total failures: {TotalFailures}", 
                agent.Name, maxRetries, _agentFailureCount[agent.Name]);
            
            return AgentResult.CreateFailure(errorMessage, lastException);
        }

        /// <summary>
        /// Determines if an error is non-retryable
        /// </summary>
        private bool IsNonRetryableError(AgentResult result)
        {
            if (result.Exception != null)
            {
                // Don't retry argument exceptions, configuration errors, etc.
                return result.Exception is ArgumentException ||
                       result.Exception is ArgumentNullException ||
                       result.Exception is InvalidOperationException ||
                       result.Exception is NotSupportedException;
            }

            // Check error message for non-retryable patterns
            var message = result.Message?.ToLowerInvariant() ?? "";
            return message.Contains("invalid") ||
                   message.Contains("not supported") ||
                   message.Contains("unauthorized") ||
                   message.Contains("forbidden");
        }

        /// <summary>
        /// Attempts to recover from orchestration failures
        /// </summary>
        private async Task<AgentResult?> AttemptRecoveryAsync(AgentRequest request, Exception exception, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Attempting recovery for request {RequestId} after error: {ErrorType}", 
                    request.Id, exception.GetType().Name);

                // Try to find a fallback agent
                var availableAgents = await _agentManager.GetAgentsAsync();
                var workingAgents = availableAgents.Where(a => _agentFailureCount.GetValueOrDefault(a.Name, 0) < 3).ToList();
                
                if (workingAgents.Any())
                {
                    // Try with the least loaded working agent
                    var fallbackAgent = workingAgents
                        .OrderBy(a => _agentLoadBalancer.GetValueOrDefault(a.Name, 0))
                        .First();

                    _logger.LogInformation("Attempting recovery with fallback agent {AgentName} for request {RequestId}", 
                        fallbackAgent.Name, request.Id);

                    // Create a simplified request for recovery
                    var recoveryRequest = new AgentRequest
                    {
                        Id = Guid.NewGuid(),
                        Prompt = $"Recovery attempt for failed request: {request.Prompt}",
                        FilePath = request.FilePath,
                        Content = request.Content,
                        Context = new Dictionary<string, object>(request.Context ?? new Dictionary<string, object>())
                        {
                            ["IsRecovery"] = true,
                            ["OriginalRequestId"] = request.Id.ToString()
                        },
                        PreferredAgentType = fallbackAgent.Type,
                        CreatedAt = DateTime.UtcNow,
                        UserId = request.UserId
                    };

                    var recoveryResult = await ProcessWithAgentAsync(fallbackAgent, recoveryRequest, cancellationToken);
                    if (recoveryResult.Success)
                    {
                        recoveryResult.Metadata = recoveryResult.Metadata ?? new Dictionary<string, object>();
                        recoveryResult.Metadata["IsRecoveryResult"] = true;
                        recoveryResult.Metadata["OriginalError"] = exception.Message;
                        return recoveryResult;
                    }
                }

                _logger.LogWarning("Recovery attempt failed for request {RequestId}", request.Id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during recovery attempt for request {RequestId}", request.Id);
                return null;
            }
        }

        /// <summary>
        /// Updates agent metrics and monitoring data
        /// </summary>
        private void UpdateAgentMetrics(string agentName, bool success, TimeSpan processingTime)
        {
            _agentLastActivity[agentName] = DateTime.UtcNow;
            
            if (success)
            {
                // Reset failure count on success
                _agentFailureCount[agentName] = 0;
            }
            else
            {
                // Increment failure count
                _agentFailureCount[agentName] = _agentFailureCount.GetValueOrDefault(agentName, 0) + 1;
            }

            _logger.LogTrace("Updated metrics for agent {AgentName}: success={Success}, processingTime={ProcessingTimeMs}ms, failures={FailureCount}", 
                agentName, success, processingTime.TotalMilliseconds, _agentFailureCount.GetValueOrDefault(agentName, 0));
        }

        /// <summary>
        /// Performs periodic health checks on agents
        /// </summary>
        private async void PerformHealthCheck(object? state)
        {
            if (_disposed || !_initialized)
                return;

            try
            {
                _logger.LogTrace("Performing agent health check");

                var agents = await _agentManager.GetAgentsAsync();
                var unhealthyAgents = new List<string>();

                foreach (var agent in agents)
                {
                    var lastActivity = _agentLastActivity.GetValueOrDefault(agent.Name, DateTime.MinValue);
                    var failureCount = _agentFailureCount.GetValueOrDefault(agent.Name, 0);
                    var timeSinceLastActivity = DateTime.UtcNow - lastActivity;

                    // Consider agent unhealthy if it hasn't been active for 10 minutes and has failures
                    if (timeSinceLastActivity > TimeSpan.FromMinutes(10) && failureCount > 0)
                    {
                        unhealthyAgents.Add(agent.Name);
                    }

                    // Reset failure count if agent has been inactive for a long time (circuit breaker reset)
                    if (timeSinceLastActivity > TimeSpan.FromMinutes(30) && failureCount > 0)
                    {
                        _logger.LogInformation("Resetting failure count for agent {AgentName} after extended inactivity", agent.Name);
                        _agentFailureCount[agent.Name] = 0;
                    }
                }

                if (unhealthyAgents.Any())
                {
                    _logger.LogWarning("Health check identified {UnhealthyCount} unhealthy agents: {UnhealthyAgents}", 
                        unhealthyAgents.Count, string.Join(", ", unhealthyAgents));
                }
                else
                {
                    _logger.LogTrace("Health check completed - all agents healthy");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check");
            }
        }

        /// <summary>
        /// Disposes the orchestrator
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                _healthCheckTimer?.Dispose();
                
                if (_initialized)
                {
                    var shutdownTask = ShutdownAsync();
                    shutdownTask.Wait(TimeSpan.FromSeconds(30));
                }

                _processingLock?.Dispose();
                _logger.LogInformation("Orchestrator disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing Orchestrator");
            }
        }
    }
}