using A3sist.Shared.Enums;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for task queue service that manages agent request processing
    /// </summary>
    public interface ITaskQueueService
    {
        /// <summary>
        /// Enqueues a request for processing
        /// </summary>
        /// <param name="request">The request to enqueue</param>
        /// <param name="priority">The priority of the request</param>
        /// <returns>Task representing the enqueue operation</returns>
        Task EnqueueAsync(AgentRequest request, TaskPriority priority = TaskPriority.Normal);

        /// <summary>
        /// Dequeues the next request for processing
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The next request to process, or null if queue is empty</returns>
        Task<AgentRequest?> DequeueAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current queue size
        /// </summary>
        /// <returns>Number of items in the queue</returns>
        Task<int> GetQueueSizeAsync();

        /// <summary>
        /// Gets queue statistics
        /// </summary>
        /// <returns>Queue statistics</returns>
        Task<QueueStatistics> GetStatisticsAsync();

        /// <summary>
        /// Clears all items from the queue
        /// </summary>
        /// <returns>Task representing the clear operation</returns>
        Task ClearAsync();

        /// <summary>
        /// Event raised when a new item is enqueued
        /// </summary>
        event EventHandler<TaskEnqueuedEventArgs> TaskEnqueued;

        /// <summary>
        /// Event raised when an item is dequeued
        /// </summary>
        event EventHandler<TaskDequeuedEventArgs> TaskDequeued;
    }

    /// <summary>
    /// Interface for workflow management service
    /// </summary>
    public interface IWorkflowService
    {
        /// <summary>
        /// Executes a workflow for the given request
        /// </summary>
        /// <param name="request">The request to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The workflow result</returns>
        Task<WorkflowResult> ExecuteWorkflowAsync(AgentRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Registers a workflow step
        /// </summary>
        /// <param name="step">The workflow step to register</param>
        /// <returns>Task representing the registration operation</returns>
        Task RegisterWorkflowStepAsync(IWorkflowStep step);

        /// <summary>
        /// Gets all registered workflow steps
        /// </summary>
        /// <returns>Collection of workflow steps</returns>
        Task<IEnumerable<IWorkflowStep>> GetWorkflowStepsAsync();

        /// <summary>
        /// Event raised when a workflow starts
        /// </summary>
        event EventHandler<WorkflowStartedEventArgs> WorkflowStarted;

        /// <summary>
        /// Event raised when a workflow completes
        /// </summary>
        event EventHandler<WorkflowCompletedEventArgs> WorkflowCompleted;
    }

    /// <summary>
    /// Interface for a workflow step
    /// </summary>
    public interface IWorkflowStep
    {
        /// <summary>
        /// Gets the name of the workflow step
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the order of execution for this step
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Determines if this step can handle the request
        /// </summary>
        /// <param name="request">The request to evaluate</param>
        /// <returns>True if the step can handle the request</returns>
        Task<bool> CanHandleAsync(AgentRequest request);

        /// <summary>
        /// Executes the workflow step
        /// </summary>
        /// <param name="request">The request to process</param>
        /// <param name="context">The workflow context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The step result</returns>
        Task<WorkflowStepResult> ExecuteAsync(AgentRequest request, WorkflowContext context, CancellationToken cancellationToken = default);
    }
}