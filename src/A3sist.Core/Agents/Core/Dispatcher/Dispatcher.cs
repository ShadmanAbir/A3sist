using A3sist.Core.Agents.Base;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Core.Agents.Core.Dispatcher
{
    /// <summary>
    /// Dispatcher agent responsible for task execution coordination, status tracking, and load balancing
    /// </summary>
    public class DispatcherAgent : BaseAgent
    {
        private readonly ITaskQueueService _taskQueueService;
        private readonly IWorkflowService _workflowService;
        private readonly IAgentManager _agentManager;
        private readonly ConcurrentDictionary<Guid, TaskExecution> _activeExecutions;
        private readonly ConcurrentDictionary<TaskPriority, int> _priorityWeights;
        private readonly Timer _loadBalancingTimer;
        private readonly SemaphoreSlim _executionSemaphore;
        private int _maxConcurrentTasks;

        public override string Name => "Dispatcher";
        public override AgentType Type => AgentType.Dispatcher;

        public DispatcherAgent(
            ILogger<DispatcherAgent> logger,
            IAgentConfiguration configuration,
            ITaskQueueService taskQueueService,
            IWorkflowService workflowService,
            IAgentManager agentManager) : base(logger, configuration)
        {
            _taskQueueService = taskQueueService ?? throw new ArgumentNullException(nameof(taskQueueService));
            _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
            _agentManager = agentManager ?? throw new ArgumentNullException(nameof(agentManager));
            
            _activeExecutions = new ConcurrentDictionary<Guid, TaskExecution>();
            _priorityWeights = new ConcurrentDictionary<TaskPriority, int>();
            _maxConcurrentTasks = Environment.ProcessorCount * 2; // Default value
            _executionSemaphore = new SemaphoreSlim(_maxConcurrentTasks, _maxConcurrentTasks);
            
            // Initialize priority weights
            InitializePriorityWeights();
            
            // Start load balancing timer (every 30 seconds)
            _loadBalancingTimer = new Timer(PerformLoadBalancing, null, 
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        protected override async Task<bool> CanHandleRequestAsync(AgentRequest request)
        {
            if (request == null)
                return false;

            // Dispatcher can handle task coordination, status queries, and workflow management requests
            var supportedActions = new[]
            {
                "dispatch", "coordinate", "status", "workflow", "balance", "prioritize",
                "execute", "schedule", "monitor", "cancel", "retry"
            };

            var prompt = request.Prompt?.ToLowerInvariant() ?? "";
            return supportedActions.Any(action => prompt.Contains(action));
        }

        protected override async Task<AgentResult> HandleRequestAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var action = ExtractActionFromRequest(request);
                
                return action.ToLowerInvariant() switch
                {
                    "dispatch" or "execute" => await DispatchTaskAsync(request, cancellationToken),
                    "coordinate" or "workflow" => await CoordinateWorkflowAsync(request, cancellationToken),
                    "status" or "monitor" => await GetExecutionStatusAsync(request, cancellationToken),
                    "balance" => await PerformLoadBalancingAsync(request, cancellationToken),
                    "prioritize" => await UpdateTaskPriorityAsync(request, cancellationToken),
                    "cancel" => await CancelTaskAsync(request, cancellationToken),
                    "retry" => await RetryTaskAsync(request, cancellationToken),
                    _ => await HandleGenericDispatchAsync(request, cancellationToken)
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error handling dispatcher request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Dispatcher error: {ex.Message}", ex, Name);
            }
        }

        protected override async Task InitializeAgentAsync()
        {
            Logger.LogInformation("Initializing Dispatcher agent");
            
            // Load configuration
            var config = await Configuration.GetAgentConfigurationAsync(Name);
            if (config?.Settings?.ContainsKey("MaxConcurrentTasks") == true)
            {
                if (int.TryParse(config.Settings["MaxConcurrentTasks"].ToString(), out var maxTasks))
                {
                    _maxConcurrentTasks = maxTasks;
                    _executionSemaphore.Release(_maxConcurrentTasks - _executionSemaphore.CurrentCount);
                }
            }

            Logger.LogInformation("Dispatcher agent initialized with max concurrent tasks: {MaxTasks}", _maxConcurrentTasks);
        }

        protected override async Task ShutdownAgentAsync()
        {
            Logger.LogInformation("Shutting down Dispatcher agent");
            
            // Cancel all active executions
            var activeTasks = _activeExecutions.Values.ToList();
            foreach (var execution in activeTasks)
            {
                execution.CancellationTokenSource.Cancel();
            }

            // Wait for active executions to complete (with timeout)
            var timeout = TimeSpan.FromSeconds(30);
            var completionTasks = activeTasks.Select(e => e.CompletionTask).ToArray();
            
            try
            {
                await Task.WhenAll(completionTasks).WaitAsync(timeout);
            }
            catch (TimeoutException)
            {
                Logger.LogWarning("Some tasks did not complete within shutdown timeout");
            }

            _loadBalancingTimer?.Dispose();
            _executionSemaphore?.Dispose();
            
            Logger.LogInformation("Dispatcher agent shutdown completed");
        }

        private async Task<AgentResult> DispatchTaskAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Dispatching task for request {RequestId}", request.Id);

            try
            {
                // Determine task priority
                var priority = DeterminePriority(request);
                
                // Enqueue the task
                await _taskQueueService.EnqueueAsync(request, priority);
                
                // Start task execution
                var execution = await StartTaskExecutionAsync(request, priority, cancellationToken);
                
                var result = new
                {
                    TaskId = execution.Id,
                    Priority = priority.ToString(),
                    Status = "Dispatched",
                    EstimatedStartTime = DateTime.UtcNow.AddSeconds(GetEstimatedWaitTime(priority))
                };

                return AgentResult.CreateSuccess(
                    $"Task dispatched successfully with priority {priority}",
                    JsonSerializer.Serialize(result),
                    Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to dispatch task for request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Failed to dispatch task: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> CoordinateWorkflowAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Coordinating workflow for request {RequestId}", request.Id);

            try
            {
                // Execute the workflow using the workflow service
                var workflowResult = await _workflowService.ExecuteWorkflowAsync(request, cancellationToken);
                
                var result = new
                {
                    WorkflowId = request.Id,
                    Success = workflowResult.Success,
                    StepsExecuted = workflowResult.StepResults.Count,
                    TotalExecutionTime = workflowResult.TotalExecutionTime,
                    Results = workflowResult.StepResults.Select(sr => new
                    {
                        StepName = sr.StepName,
                        Success = sr.Success,
                        ExecutionTime = sr.ExecutionTime,
                        Message = sr.Result?.Message
                    })
                };

                return workflowResult.Success
                    ? AgentResult.CreateSuccess("Workflow coordinated successfully", JsonSerializer.Serialize(result), Name)
                    : AgentResult.CreateFailure($"Workflow coordination failed: {workflowResult.Result?.Message}", Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to coordinate workflow for request {RequestId}", request.Id);
                return AgentResult.CreateFailure($"Failed to coordinate workflow: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> GetExecutionStatusAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var queueStats = await _taskQueueService.GetStatisticsAsync();
                var activeExecutions = _activeExecutions.Values.Select(e => new
                {
                    TaskId = e.Id,
                    RequestId = e.Request.Id,
                    Priority = e.Priority.ToString(),
                    Status = e.Status.ToString(),
                    StartTime = e.StartTime,
                    ElapsedTime = DateTime.UtcNow - e.StartTime
                }).ToList();

                var status = new
                {
                    QueueStatistics = new
                    {
                        TotalItems = queueStats.TotalItems,
                        TotalProcessed = queueStats.TotalProcessed,
                        AverageWaitTime = queueStats.AverageWaitTime,
                        ThroughputPerMinute = queueStats.ThroughputPerMinute,
                        ItemsByPriority = queueStats.ItemsByPriority
                    },
                    ActiveExecutions = activeExecutions,
                    LoadBalancing = new
                    {
                        MaxConcurrentTasks = _maxConcurrentTasks,
                        CurrentConcurrentTasks = _maxConcurrentTasks - _executionSemaphore.CurrentCount,
                        PriorityWeights = _priorityWeights
                    }
                };

                return AgentResult.CreateSuccess("Status retrieved successfully", JsonSerializer.Serialize(status), Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get execution status");
                return AgentResult.CreateFailure($"Failed to get status: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> PerformLoadBalancingAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await PerformLoadBalancing(null);
                return AgentResult.CreateSuccess("Load balancing performed successfully", Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to perform load balancing");
                return AgentResult.CreateFailure($"Load balancing failed: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> UpdateTaskPriorityAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            // This would require extending the task queue service to support priority updates
            // For now, return a placeholder implementation
            return AgentResult.CreateSuccess("Priority update functionality not yet implemented", Name);
        }

        private async Task<AgentResult> CancelTaskAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                // Extract task ID from request
                var taskIdStr = ExtractParameterFromRequest(request, "taskId");
                if (Guid.TryParse(taskIdStr, out var taskId))
                {
                    if (_activeExecutions.TryGetValue(taskId, out var execution))
                    {
                        execution.CancellationTokenSource.Cancel();
                        execution.Status = TaskExecutionStatus.Cancelled;
                        
                        return AgentResult.CreateSuccess($"Task {taskId} cancelled successfully", Name);
                    }
                    else
                    {
                        return AgentResult.CreateFailure($"Task {taskId} not found or already completed", Name);
                    }
                }
                else
                {
                    return AgentResult.CreateFailure("Invalid task ID provided", Name);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to cancel task");
                return AgentResult.CreateFailure($"Failed to cancel task: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> RetryTaskAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                // Re-enqueue the request with higher priority
                var priority = TaskPriority.High;
                await _taskQueueService.EnqueueAsync(request, priority);
                
                return AgentResult.CreateSuccess("Task queued for retry with high priority", Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to retry task");
                return AgentResult.CreateFailure($"Failed to retry task: {ex.Message}", ex, Name);
            }
        }

        private async Task<AgentResult> HandleGenericDispatchAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            // Default behavior: dispatch the request to the most appropriate agent
            return await DispatchTaskAsync(request, cancellationToken);
        }

        private async Task<TaskExecution> StartTaskExecutionAsync(AgentRequest request, TaskPriority priority, CancellationToken cancellationToken)
        {
            var execution = new TaskExecution
            {
                Id = Guid.NewGuid(),
                Request = request,
                Priority = priority,
                Status = TaskExecutionStatus.Queued,
                StartTime = DateTime.UtcNow,
                CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            };

            _activeExecutions.TryAdd(execution.Id, execution);

            // Start the execution task
            execution.CompletionTask = ExecuteTaskAsync(execution);

            return execution;
        }

        private async Task ExecuteTaskAsync(TaskExecution execution)
        {
            try
            {
                await _executionSemaphore.WaitAsync(execution.CancellationTokenSource.Token);
                
                execution.Status = TaskExecutionStatus.Running;
                Logger.LogDebug("Starting execution of task {TaskId}", execution.Id);

                // Find the best agent to handle this request
                var availableAgents = await _agentManager.GetAgentsAsync();
                var suitableAgents = new List<IAgent>();

                foreach (var agent in availableAgents)
                {
                    if (agent.Name != Name && await agent.CanHandleAsync(execution.Request))
                    {
                        suitableAgents.Add(agent);
                    }
                }

                if (suitableAgents.Any())
                {
                    // Select the best agent (for now, just pick the first suitable one)
                    var selectedAgent = suitableAgents.First();
                    
                    Logger.LogDebug("Executing task {TaskId} with agent {AgentName}", execution.Id, selectedAgent.Name);
                    
                    var result = await selectedAgent.HandleAsync(execution.Request, execution.CancellationTokenSource.Token);
                    execution.Result = result;
                    execution.Status = result.Success ? TaskExecutionStatus.Completed : TaskExecutionStatus.Failed;
                }
                else
                {
                    Logger.LogWarning("No suitable agent found for task {TaskId}", execution.Id);
                    execution.Status = TaskExecutionStatus.Failed;
                    execution.Result = AgentResult.CreateFailure("No suitable agent found to handle the request", Name);
                }
            }
            catch (OperationCanceledException)
            {
                execution.Status = TaskExecutionStatus.Cancelled;
                Logger.LogDebug("Task {TaskId} was cancelled", execution.Id);
            }
            catch (Exception ex)
            {
                execution.Status = TaskExecutionStatus.Failed;
                execution.Result = AgentResult.CreateFailure($"Task execution failed: {ex.Message}", ex, Name);
                Logger.LogError(ex, "Task {TaskId} execution failed", execution.Id);
            }
            finally
            {
                execution.EndTime = DateTime.UtcNow;
                _executionSemaphore.Release();
                
                // Clean up completed executions after a delay
                _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(_ =>
                {
                    _activeExecutions.TryRemove(execution.Id, out _);
                });
            }
        }

        private void InitializePriorityWeights()
        {
            _priorityWeights[TaskPriority.Critical] = 100;
            _priorityWeights[TaskPriority.High] = 75;
            _priorityWeights[TaskPriority.Normal] = 50;
            _priorityWeights[TaskPriority.Low] = 25;
        }

        private TaskPriority DeterminePriority(AgentRequest request)
        {
            // Simple priority determination logic
            var prompt = request.Prompt?.ToLowerInvariant() ?? "";
            
            if (prompt.Contains("urgent") || prompt.Contains("critical") || prompt.Contains("emergency"))
                return TaskPriority.Critical;
            
            if (prompt.Contains("important") || prompt.Contains("high"))
                return TaskPriority.High;
            
            if (prompt.Contains("low") || prompt.Contains("background"))
                return TaskPriority.Low;
            
            return TaskPriority.Normal;
        }

        private double GetEstimatedWaitTime(TaskPriority priority)
        {
            // Simple estimation based on priority and current queue size
            var weight = _priorityWeights.GetValueOrDefault(priority, 50);
            var queueSize = _activeExecutions.Count;
            
            return Math.Max(1, queueSize * (100.0 / weight));
        }

        private string ExtractActionFromRequest(AgentRequest request)
        {
            // Extract action from prompt or context
            var prompt = request.Prompt?.ToLowerInvariant() ?? "";
            
            if (prompt.Contains("dispatch") || prompt.Contains("execute"))
                return "dispatch";
            if (prompt.Contains("coordinate") || prompt.Contains("workflow"))
                return "coordinate";
            if (prompt.Contains("status") || prompt.Contains("monitor"))
                return "status";
            if (prompt.Contains("balance"))
                return "balance";
            if (prompt.Contains("prioritize"))
                return "prioritize";
            if (prompt.Contains("cancel"))
                return "cancel";
            if (prompt.Contains("retry"))
                return "retry";
            
            return "dispatch"; // Default action
        }

        private string ExtractParameterFromRequest(AgentRequest request, string parameterName)
        {
            // Try to extract parameter from context
            if (request.Context?.ContainsKey(parameterName) == true)
            {
                return request.Context[parameterName]?.ToString() ?? "";
            }
            
            // Try to extract from prompt using simple pattern matching
            var prompt = request.Prompt ?? "";
            var pattern = $"{parameterName}:";
            var index = prompt.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var start = index + pattern.Length;
                var end = prompt.IndexOf(' ', start);
                if (end == -1) end = prompt.Length;
                
                return prompt.Substring(start, end - start).Trim();
            }
            
            return "";
        }

        private async void PerformLoadBalancing(object? state)
        {
            try
            {
                Logger.LogTrace("Performing load balancing check");
                
                var queueStats = await _taskQueueService.GetStatisticsAsync();
                var currentLoad = _maxConcurrentTasks - _executionSemaphore.CurrentCount;
                
                // Adjust concurrent task limit based on system performance
                if (queueStats.ThroughputPerMinute > 0)
                {
                    var targetThroughput = 60.0; // Target: 1 task per second
                    var currentThroughput = queueStats.ThroughputPerMinute;
                    
                    if (currentThroughput < targetThroughput * 0.8 && _maxConcurrentTasks < Environment.ProcessorCount * 4)
                    {
                        // Increase concurrency if throughput is low
                        _maxConcurrentTasks++;
                        _executionSemaphore.Release();
                        Logger.LogDebug("Increased max concurrent tasks to {MaxTasks}", _maxConcurrentTasks);
                    }
                    else if (currentThroughput > targetThroughput * 1.2 && _maxConcurrentTasks > Environment.ProcessorCount)
                    {
                        // Decrease concurrency if throughput is too high (might indicate resource contention)
                        _maxConcurrentTasks--;
                        await _executionSemaphore.WaitAsync(TimeSpan.FromMilliseconds(100));
                        Logger.LogDebug("Decreased max concurrent tasks to {MaxTasks}", _maxConcurrentTasks);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during load balancing");
            }
        }

        public override void Dispose()
        {
            _loadBalancingTimer?.Dispose();
            _executionSemaphore?.Dispose();
            
            foreach (var execution in _activeExecutions.Values)
            {
                execution.CancellationTokenSource?.Dispose();
            }
            
            base.Dispose();
        }
    }

    /// <summary>
    /// Represents an active task execution
    /// </summary>
    internal class TaskExecution
    {
        public Guid Id { get; set; }
        public AgentRequest Request { get; set; } = null!;
        public TaskPriority Priority { get; set; }
        public TaskExecutionStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public AgentResult? Result { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; } = null!;
        public Task CompletionTask { get; set; } = null!;
    }

    /// <summary>
    /// Task execution status enumeration
    /// </summary>
    internal enum TaskExecutionStatus
    {
        Queued,
        Running,
        Completed,
        Failed,
        Cancelled
    }
}