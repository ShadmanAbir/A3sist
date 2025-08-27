using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Core.Services
{
    /// <summary>
    /// Task queue service that manages agent request processing with priority-based scheduling
    /// </summary>
    public class TaskQueueService : ITaskQueueService, IDisposable
    {
        private readonly ILogger<TaskQueueService> _logger;
        private readonly ConcurrentDictionary<TaskPriority, ConcurrentQueue<QueueItem>> _queues;
        private readonly SemaphoreSlim _queueSemaphore;
        private readonly Timer _statisticsTimer;
        private readonly object _statisticsLock = new();
        
        private QueueStatistics _statistics;
        private long _totalEnqueued;
        private long _totalDequeued;
        private bool _disposed;

        /// <summary>
        /// Event raised when a new item is enqueued
        /// </summary>
        public event EventHandler<TaskEnqueuedEventArgs>? TaskEnqueued;

        /// <summary>
        /// Event raised when an item is dequeued
        /// </summary>
        public event EventHandler<TaskDequeuedEventArgs>? TaskDequeued;

        public TaskQueueService(ILogger<TaskQueueService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queues = new ConcurrentDictionary<TaskPriority, ConcurrentQueue<QueueItem>>();
            _queueSemaphore = new SemaphoreSlim(0);
            _statistics = new QueueStatistics();

            // Initialize priority queues
            foreach (TaskPriority priority in Enum.GetValues(typeof(TaskPriority)))
            {
                _queues[priority] = new ConcurrentQueue<QueueItem>();
            }

            // Start statistics update timer (every 30 seconds)
            _statisticsTimer = new Timer(UpdateStatistics, null, 
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            _logger.LogInformation("TaskQueueService initialized with {QueueCount} priority queues", _queues.Count);
        }

        /// <summary>
        /// Enqueues a request for processing
        /// </summary>
        public async Task EnqueueAsync(AgentRequest request, TaskPriority priority = TaskPriority.Normal)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (_disposed)
                throw new ObjectDisposedException(nameof(TaskQueueService));

            var queueItem = new QueueItem(request, priority, DateTime.UtcNow);
            
            if (!_queues.TryGetValue(priority, out var queue))
            {
                _logger.LogError("Queue for priority {Priority} not found", priority);
                throw new InvalidOperationException($"Queue for priority {priority} not found");
            }

            queue.Enqueue(queueItem);
            Interlocked.Increment(ref _totalEnqueued);

            // Signal that an item is available
            _queueSemaphore.Release();

            _logger.LogDebug("Enqueued request {RequestId} with priority {Priority}. Queue size: {QueueSize}", 
                request.Id, priority, await GetQueueSizeAsync());

            // Raise event
            TaskEnqueued?.Invoke(this, new TaskEnqueuedEventArgs(request, priority));

            await Task.CompletedTask;
        }

        /// <summary>
        /// Dequeues the next request for processing
        /// </summary>
        public async Task<AgentRequest?> DequeueAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                return null;

            try
            {
                // Wait for an item to be available
                await _queueSemaphore.WaitAsync(cancellationToken);

                // Try to dequeue from highest priority queue first
                var priorities = Enum.GetValues(typeof(TaskPriority))
                    .Cast<TaskPriority>()
                    .OrderByDescending(p => (int)p);

                foreach (var priority in priorities)
                {
                    if (_queues.TryGetValue(priority, out var queue) && 
                        queue.TryDequeue(out var queueItem))
                    {
                        Interlocked.Increment(ref _totalDequeued);
                        
                        var waitTime = DateTime.UtcNow - queueItem.EnqueuedAt;
                        
                        _logger.LogDebug("Dequeued request {RequestId} with priority {Priority} after waiting {WaitTime}ms", 
                            queueItem.Request.Id, priority, waitTime.TotalMilliseconds);

                        // Raise event
                        TaskDequeued?.Invoke(this, new TaskDequeuedEventArgs(queueItem.Request, priority, waitTime));

                        return queueItem.Request;
                    }
                }

                // This shouldn't happen if semaphore is working correctly
                _logger.LogWarning("Semaphore signaled but no items found in any queue");
                return null;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Dequeue operation was cancelled");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dequeuing request");
                return null;
            }
        }

        /// <summary>
        /// Gets the current queue size
        /// </summary>
        public async Task<int> GetQueueSizeAsync()
        {
            await Task.CompletedTask;
            return _queues.Values.Sum(q => q.Count);
        }

        /// <summary>
        /// Gets queue statistics
        /// </summary>
        public async Task<QueueStatistics> GetStatisticsAsync()
        {
            await Task.CompletedTask;
            
            lock (_statisticsLock)
            {
                // Update current queue sizes
                _statistics.TotalItems = _queues.Values.Sum(q => q.Count);
                _statistics.ItemsByPriority.Clear();
                
                foreach (var kvp in _queues)
                {
                    _statistics.ItemsByPriority[kvp.Key] = kvp.Value.Count;
                }

                _statistics.LastUpdated = DateTime.UtcNow;
                
                // Create a copy to avoid concurrent modification
                return new QueueStatistics
                {
                    TotalItems = _statistics.TotalItems,
                    ItemsByPriority = new Dictionary<TaskPriority, int>(_statistics.ItemsByPriority),
                    AverageWaitTime = _statistics.AverageWaitTime,
                    TotalProcessed = _statistics.TotalProcessed,
                    TotalFailed = _statistics.TotalFailed,
                    AverageProcessingTime = _statistics.AverageProcessingTime,
                    ThroughputPerMinute = _statistics.ThroughputPerMinute,
                    LastUpdated = _statistics.LastUpdated
                };
            }
        }

        /// <summary>
        /// Clears all items from the queue
        /// </summary>
        public async Task ClearAsync()
        {
            if (_disposed)
                return;

            var totalCleared = 0;

            foreach (var queue in _queues.Values)
            {
                var count = queue.Count;
                while (queue.TryDequeue(out _))
                {
                    totalCleared++;
                }
            }

            // Reset semaphore count
            while (_queueSemaphore.CurrentCount > 0)
            {
                await _queueSemaphore.WaitAsync(TimeSpan.FromMilliseconds(1));
            }

            _logger.LogInformation("Cleared {ItemCount} items from all queues", totalCleared);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Updates queue statistics
        /// </summary>
        private void UpdateStatistics(object? state)
        {
            if (_disposed)
                return;

            try
            {
                lock (_statisticsLock)
                {
                    var totalEnqueued = Interlocked.Read(ref _totalEnqueued);
                    var totalDequeued = Interlocked.Read(ref _totalDequeued);
                    
                    _statistics.TotalProcessed = totalDequeued;
                    
                    // Calculate throughput (items per minute)
                    var timeSinceLastUpdate = DateTime.UtcNow - _statistics.LastUpdated;
                    if (timeSinceLastUpdate.TotalMinutes > 0)
                    {
                        var itemsProcessedSinceLastUpdate = totalDequeued - (_statistics.TotalProcessed - totalDequeued);
                        _statistics.ThroughputPerMinute = itemsProcessedSinceLastUpdate / timeSinceLastUpdate.TotalMinutes;
                    }

                    _logger.LogTrace("Updated queue statistics: {TotalItems} items, {TotalProcessed} processed, {Throughput:F2} items/min",
                        _statistics.TotalItems, _statistics.TotalProcessed, _statistics.ThroughputPerMinute);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating queue statistics");
            }
        }

        /// <summary>
        /// Disposes the task queue service
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                _statisticsTimer?.Dispose();
                _queueSemaphore?.Dispose();
                
                _logger.LogInformation("TaskQueueService disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing TaskQueueService");
            }
        }

        /// <summary>
        /// Internal queue item wrapper
        /// </summary>
        private class QueueItem
        {
            public AgentRequest Request { get; }
            public TaskPriority Priority { get; }
            public DateTime EnqueuedAt { get; }

            public QueueItem(AgentRequest request, TaskPriority priority, DateTime enqueuedAt)
            {
                Request = request;
                Priority = priority;
                EnqueuedAt = enqueuedAt;
            }
        }
    }
}