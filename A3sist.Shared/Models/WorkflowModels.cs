using A3sist.Shared.Messaging;
using System;
using System.Collections.Generic;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Result of a workflow execution
    /// </summary>
    public class WorkflowResult
    {
        /// <summary>
        /// Whether the workflow completed successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The final result of the workflow
        /// </summary>
        public AgentResult Result { get; set; } = new();

        /// <summary>
        /// Results from individual workflow steps
        /// </summary>
        public List<WorkflowStepResult> StepResults { get; set; } = new();

        /// <summary>
        /// Total time taken to execute the workflow
        /// </summary>
        public TimeSpan TotalExecutionTime { get; set; }

        /// <summary>
        /// Any error that occurred during workflow execution
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Creates a successful workflow result
        /// </summary>
        public static WorkflowResult CreateSuccess(AgentResult result)
        {
            return new WorkflowResult
            {
                Success = true,
                Result = result
            };
        }

        /// <summary>
        /// Creates a failed workflow result
        /// </summary>
        public static WorkflowResult CreateFailure(string message, Exception? exception = null)
        {
            return new WorkflowResult
            {
                Success = false,
                Result = AgentResult.CreateFailure(message, exception),
                Exception = exception
            };
        }
    }

    /// <summary>
    /// Result of a workflow step execution
    /// </summary>
    public class WorkflowStepResult
    {
        /// <summary>
        /// Name of the step that executed
        /// </summary>
        public string StepName { get; set; } = string.Empty;

        /// <summary>
        /// Whether the step completed successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The result of the step
        /// </summary>
        public AgentResult Result { get; set; } = new();

        /// <summary>
        /// Time taken to execute the step
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// Any error that occurred during step execution
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Additional metadata from the step
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Context passed between workflow steps
    /// </summary>
    public class WorkflowContext
    {
        /// <summary>
        /// The original request being processed
        /// </summary>
        public AgentRequest Request { get; set; } = new();

        /// <summary>
        /// Shared data between workflow steps
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();

        /// <summary>
        /// Results from previous steps
        /// </summary>
        public List<WorkflowStepResult> PreviousResults { get; set; } = new();

        /// <summary>
        /// Whether the workflow should continue processing
        /// </summary>
        public bool ShouldContinue { get; set; } = true;

        /// <summary>
        /// Reason for stopping the workflow (if ShouldContinue is false)
        /// </summary>
        public string? StopReason { get; set; }
    }

    /// <summary>
    /// Event arguments for workflow started events
    /// </summary>
    public class WorkflowStartedEventArgs : EventArgs
    {
        /// <summary>
        /// The request that started the workflow
        /// </summary>
        public AgentRequest Request { get; }

        /// <summary>
        /// When the workflow started
        /// </summary>
        public DateTime StartedAt { get; }

        public WorkflowStartedEventArgs(AgentRequest request)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            StartedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event arguments for workflow completed events
    /// </summary>
    public class WorkflowCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// The request that was processed
        /// </summary>
        public AgentRequest Request { get; }

        /// <summary>
        /// The result of the workflow
        /// </summary>
        public WorkflowResult Result { get; }

        /// <summary>
        /// When the workflow completed
        /// </summary>
        public DateTime CompletedAt { get; }

        /// <summary>
        /// Total execution time
        /// </summary>
        public TimeSpan ExecutionTime { get; }

        public WorkflowCompletedEventArgs(AgentRequest request, WorkflowResult result, TimeSpan executionTime)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Result = result ?? throw new ArgumentNullException(nameof(result));
            ExecutionTime = executionTime;
            CompletedAt = DateTime.UtcNow;
        }
    }
}