using System;
using System.Threading;

namespace A3sist.Core.Agents.Base
{
    /// <summary>
    /// Metrics collection for agent performance monitoring
    /// </summary>
    public class AgentMetrics
    {
        private long _tasksProcessed;
        private long _tasksSucceeded;
        private long _tasksFailed;
        private long _totalProcessingTimeMs;
        private DateTime _lastActivity;
        private readonly object _lock = new object();

        /// <summary>
        /// Gets the total number of tasks processed
        /// </summary>
        public int TasksProcessed => (int)Interlocked.Read(ref _tasksProcessed);

        /// <summary>
        /// Gets the number of tasks that succeeded
        /// </summary>
        public int TasksSucceeded => (int)Interlocked.Read(ref _tasksSucceeded);

        /// <summary>
        /// Gets the number of tasks that failed
        /// </summary>
        public int TasksFailed => (int)Interlocked.Read(ref _tasksFailed);

        /// <summary>
        /// Gets the last activity timestamp
        /// </summary>
        public DateTime LastActivity
        {
            get
            {
                lock (_lock)
                {
                    return _lastActivity;
                }
            }
        }

        /// <summary>
        /// Gets the average processing time
        /// </summary>
        public TimeSpan AverageProcessingTime
        {
            get
            {
                var processed = TasksProcessed;
                if (processed == 0)
                    return TimeSpan.Zero;

                var totalMs = Interlocked.Read(ref _totalProcessingTimeMs);
                return TimeSpan.FromMilliseconds(totalMs / (double)processed);
            }
        }

        /// <summary>
        /// Gets the success rate (0.0 to 1.0)
        /// </summary>
        public double SuccessRate
        {
            get
            {
                var processed = TasksProcessed;
                return processed > 0 ? (double)TasksSucceeded / processed : 0.0;
            }
        }

        /// <summary>
        /// Gets the failure rate (0.0 to 1.0)
        /// </summary>
        public double FailureRate
        {
            get
            {
                var processed = TasksProcessed;
                return processed > 0 ? (double)TasksFailed / processed : 0.0;
            }
        }

        public AgentMetrics()
        {
            _lastActivity = DateTime.UtcNow;
        }

        /// <summary>
        /// Increments the tasks processed counter
        /// </summary>
        public void IncrementTasksProcessed()
        {
            Interlocked.Increment(ref _tasksProcessed);
            UpdateLastActivity();
        }

        /// <summary>
        /// Increments the tasks succeeded counter
        /// </summary>
        public void IncrementTasksSucceeded()
        {
            Interlocked.Increment(ref _tasksSucceeded);
            UpdateLastActivity();
        }

        /// <summary>
        /// Increments the tasks failed counter
        /// </summary>
        public void IncrementTasksFailed()
        {
            Interlocked.Increment(ref _tasksFailed);
            UpdateLastActivity();
        }

        /// <summary>
        /// Updates the average processing time with a new measurement
        /// </summary>
        public void UpdateAverageProcessingTime(TimeSpan processingTime)
        {
            Interlocked.Add(ref _totalProcessingTimeMs, (long)processingTime.TotalMilliseconds);
            UpdateLastActivity();
        }

        /// <summary>
        /// Resets all metrics
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref _tasksProcessed, 0);
            Interlocked.Exchange(ref _tasksSucceeded, 0);
            Interlocked.Exchange(ref _tasksFailed, 0);
            Interlocked.Exchange(ref _totalProcessingTimeMs, 0);
            UpdateLastActivity();
        }

        private void UpdateLastActivity()
        {
            lock (_lock)
            {
                _lastActivity = DateTime.UtcNow;
            }
        }
    }
}