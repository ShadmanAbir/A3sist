using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Core.Services.WorkflowSteps
{
    /// <summary>
    /// Base implementation for workflow steps
    /// </summary>
    public abstract class BaseWorkflowStep : IWorkflowStep
    {
        protected readonly ILogger Logger;

        /// <summary>
        /// Gets the name of the workflow step
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the order of execution for this step
        /// </summary>
        public abstract int Order { get; }

        protected BaseWorkflowStep(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Determines if this step can handle the request
        /// </summary>
        public virtual async Task<bool> CanHandleAsync(AgentRequest request)
        {
            if (request == null)
                return false;

            return await CanHandleRequestAsync(request);
        }

        /// <summary>
        /// Executes the workflow step
        /// </summary>
        public async Task<WorkflowStepResult> ExecuteAsync(AgentRequest request, WorkflowContext context, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var stepResult = new WorkflowStepResult
            {
                StepName = Name
            };

            try
            {
                Logger.LogDebug("Executing workflow step {StepName} for request {RequestId}", Name, request.Id);

                var result = await ExecuteStepAsync(request, context, cancellationToken);
                
                stepResult.Success = result.Success;
                stepResult.Result = result;

                Logger.LogDebug("Workflow step {StepName} completed for request {RequestId} with success: {Success}", 
                    Name, request.Id, result.Success);

                return stepResult;
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("Workflow step {StepName} was cancelled for request {RequestId}", Name, request.Id);
                stepResult.Success = false;
                stepResult.Result = AgentResult.CreateFailure("Step execution was cancelled");
                return stepResult;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Workflow step {StepName} failed for request {RequestId}", Name, request.Id);
                stepResult.Success = false;
                stepResult.Result = AgentResult.CreateFailure($"Step {Name} failed: {ex.Message}", ex);
                stepResult.Exception = ex;
                return stepResult;
            }
        }

        /// <summary>
        /// Override this method to implement step-specific logic for determining if the step can handle the request
        /// </summary>
        protected virtual Task<bool> CanHandleRequestAsync(AgentRequest request)
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Override this method to implement the actual step execution logic
        /// </summary>
        protected abstract Task<AgentResult> ExecuteStepAsync(AgentRequest request, WorkflowContext context, CancellationToken cancellationToken);
    }
}