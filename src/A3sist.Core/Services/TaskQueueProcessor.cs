using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Core.Services
{
    /// <summary>
    /// Background service that continuously processes items from the task queue
    /// </summary>
    public class TaskQueueProcessor : BackgroundService
    {
        private readonly ITaskQueueService _taskQueueService;
        private readonly IOrchestrator _orchestrator;
        private readonly ILogger<TaskQueueProcessor> _logger;
        private readonly SemaphoreSlim _processingLock;
        private int _maxConcurrentTasks;

        public TaskQueueProcessor(
            ITaskQueueService taskQueueService,
            IOrchestrator orchestrator,
            ILogger<TaskQueueProcessor> logger)
        {
            _taskQueueService = taskQueueService ?? throw new ArgumentNullException(nameof(taskQueueService));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _maxConcurrentTasks = Environment.ProcessorCount; // Default to CPU count
            _processingLock = new SemaphoreSlim(_maxConcurrentTasks, _maxConcurrentTasks);
        }

        /// <summary>
        /// Sets the maximum number of concurrent tasks that can be processed
        /// </summary>
        public void SetMaxConcurrentTasks(int maxTasks)
        {
            if (maxTasks <= 0)
                throw new ArgumentException("Max concurrent tasks must be greater than 0", nameof(maxTasks));

            _maxConcurrentTasks = maxTasks;
            
            // Update semaphore if needed
            var currentCount = _processingLock.CurrentCount;
            var availableCount = _maxConcurrentTasks - (_processingLock.CurrentCount);
            
            if (availableCount > 0)
            {
                _processingLock.Release(availableCount);
            }
            
            _logger.LogInformation("Updated max concurrent tasks to {MaxTasks}", maxTasks);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TaskQueueProcessor started with max concurrent tasks: {MaxTasks}", _maxConcurrentTasks);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        // Wait for available processing slot
                        await _processingLock.WaitAsync(stoppingToken);

                        // Start processing task without waiting for completion
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await ProcessNextTaskAsync(stoppingToken);
                            }
                            finally
                            {
                                _processingLock.Release();
                            }
                        }, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancellation is requested
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in task queue processor main loop");
                        
                        // Brief delay before retrying to avoid tight error loops
                        await Task.Delay(1000, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            finally
            {
                _logger.LogInformation("TaskQueueProcessor stopped");
            }
        }

        private async Task ProcessNextTaskAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Dequeue the next request
                var request = await _taskQueueService.DequeueAsync(cancellationToken);
                
                if (request == null)
                {
                    // No request available, this is normal
                    return;
                }

                _logger.LogDebug("Processing request {RequestId} from task queue", request.Id);

                // Process the request through the orchestrator
                var result = await _orchestrator.ProcessRequestAsync(request, cancellationToken);

                if (result.Success)
                {
                    _logger.LogDebug("Successfully processed request {RequestId}", request.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to process request {RequestId}: {ErrorMessage}", 
                        request.Id, result.Message);
                }

                // TODO: In a real implementation, you might want to:
                // - Store results in a database
                // - Send notifications to clients
                // - Update request status
                // - Handle retries for failed requests
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
                _logger.LogDebug("Task processing was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing task from queue");
            }
        }

        public override void Dispose()
        {
            _processingLock?.Dispose();
            base.Dispose();
        }
    }
}