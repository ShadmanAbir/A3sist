using Microsoft.Extensions.Logging;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace A3sist.Orchastrator.Agents.Dispatcher
{
    internal class TaskOrchestrator : IDisposable
    {
        private readonly ILogger<TaskOrchestrator> _logger;
        private readonly Channel<TaskRequest> _taskChannel;
        private readonly ChannelWriter<TaskRequest> _taskWriter;
        private readonly ChannelReader<TaskRequest> _taskReader;
        private readonly ConcurrentDictionary<string, TaskExecution> _activeTasks;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _processingTask;
        private bool _disposed;

        public TaskOrchestrator(ILogger<TaskOrchestrator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _taskChannel = Channel.CreateUnbounded<TaskRequest>();
            _taskWriter = _taskChannel.Writer;
            _taskReader = _taskChannel.Reader;
            _activeTasks = new ConcurrentDictionary<string, TaskExecution>();
            _cancellationTokenSource = new CancellationTokenSource();
            _processingTask = ProcessTasksAsync(_cancellationTokenSource.Token);
        }

        internal async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing TaskOrchestrator");
            await Task.CompletedTask;
        }

        internal async Task ShutdownAsync()
        {
            _logger.LogInformation("Shutting down TaskOrchestrator");
            
            _taskWriter.Complete();
            _cancellationTokenSource.Cancel();
            
            try
            {
                await _processingTask;
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            
            // Cancel any active tasks
            foreach (var task in _activeTasks.Values)
            {
                task.CancellationTokenSource?.Cancel();
            }
            
            _activeTasks.Clear();
        }

        internal async Task<bool> ScheduleTaskAsync(TaskRequest request)
        {
            try
            {
                await _taskWriter.WriteAsync(request);
                _logger.LogDebug("Scheduled task {TaskId} of type {TaskType}", request.Id, request.Type);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling task {TaskId}", request.Id);
                return false;
            }
        }

        private async Task ProcessTasksAsync(CancellationToken cancellationToken)
        {
            await foreach (var taskRequest in _taskReader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    var execution = new TaskExecution
                    {
                        Request = taskRequest,
                        StartTime = DateTime.UtcNow,
                        CancellationTokenSource = new CancellationTokenSource()
                    };

                    _activeTasks[taskRequest.Id] = execution;
                    
                    // Execute task in background
                    _ = Task.Run(async () => await ExecuteTaskAsync(execution), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing task {TaskId}", taskRequest.Id);
                }
            }
        }

        private async Task ExecuteTaskAsync(TaskExecution execution)
        {
            try
            {
                _logger.LogDebug("Executing task {TaskId}", execution.Request.Id);
                
                // Simulate task execution - in real implementation, this would delegate to appropriate handlers
                await Task.Delay(TimeSpan.FromMilliseconds(100), execution.CancellationTokenSource!.Token);
                
                execution.EndTime = DateTime.UtcNow;
                execution.IsCompleted = true;
                
                _logger.LogDebug("Completed task {TaskId} in {Duration}ms", 
                    execution.Request.Id, 
                    (execution.EndTime - execution.StartTime).TotalMilliseconds);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Task {TaskId} was cancelled", execution.Request.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing task {TaskId}", execution.Request.Id);
            }
            finally
            {
                _activeTasks.TryRemove(execution.Request.Id, out _);
                execution.CancellationTokenSource?.Dispose();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                
                foreach (var task in _activeTasks.Values)
                {
                    task.CancellationTokenSource?.Dispose();
                }
                
                _activeTasks.Clear();
                _disposed = true;
            }
        }
    }

    internal class TaskRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = string.Empty;
        public object? Payload { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    internal class TaskExecution
    {
        public TaskRequest Request { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsCompleted { get; set; }
        public CancellationTokenSource? CancellationTokenSource { get; set; }
    }
}