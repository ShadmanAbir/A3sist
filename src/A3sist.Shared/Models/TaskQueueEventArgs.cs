using A3sist.Shared.Enums;
using A3sist.Shared.Messaging;
using System;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Event arguments for task enqueued events
    /// </summary>
    public class TaskEnqueuedEventArgs : EventArgs
    {
        /// <summary>
        /// The request that was enqueued
        /// </summary>
        public AgentRequest Request { get; }

        /// <summary>
        /// The priority of the enqueued task
        /// </summary>
        public TaskPriority Priority { get; }

        /// <summary>
        /// When the task was enqueued
        /// </summary>
        public DateTime EnqueuedAt { get; }

        public TaskEnqueuedEventArgs(AgentRequest request, TaskPriority priority)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Priority = priority;
            EnqueuedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event arguments for task dequeued events
    /// </summary>
    public class TaskDequeuedEventArgs : EventArgs
    {
        /// <summary>
        /// The request that was dequeued
        /// </summary>
        public AgentRequest Request { get; }

        /// <summary>
        /// The priority of the dequeued task
        /// </summary>
        public TaskPriority Priority { get; }

        /// <summary>
        /// When the task was dequeued
        /// </summary>
        public DateTime DequeuedAt { get; }

        /// <summary>
        /// How long the task waited in the queue
        /// </summary>
        public TimeSpan WaitTime { get; }

        public TaskDequeuedEventArgs(AgentRequest request, TaskPriority priority, TimeSpan waitTime)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Priority = priority;
            WaitTime = waitTime;
            DequeuedAt = DateTime.UtcNow;
        }
    }
}