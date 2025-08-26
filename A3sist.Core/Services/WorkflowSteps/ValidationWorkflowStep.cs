using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Core.Services.WorkflowSteps
{
    /// <summary>
    /// Workflow step that validates incoming requests
    /// </summary>
    public class ValidationWorkflowStep : BaseWorkflowStep
    {
        public override string Name => "Validation";
        public override int Order => 1;

        public ValidationWorkflowStep(ILogger<ValidationWorkflowStep> logger) : base(logger)
        {
        }

        protected override Task<bool> CanHandleRequestAsync(AgentRequest request)
        {
            // This step can handle all requests as validation is always needed
            return Task.FromResult(true);
        }

        protected override async Task<AgentResult> ExecuteStepAsync(AgentRequest request, WorkflowContext context, CancellationToken cancellationToken)
        {
            Logger.LogDebug("Validating request {RequestId}", request.Id);

            // Validate request ID
            if (request.Id == Guid.Empty)
            {
                return AgentResult.CreateFailure("Request ID is required");
            }

            // Validate prompt
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return AgentResult.CreateFailure("Request prompt is required");
            }

            // Validate user ID
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return AgentResult.CreateFailure("User ID is required");
            }

            // Add validation metadata to context
            context.Data["ValidationTimestamp"] = DateTime.UtcNow;
            context.Data["ValidatedBy"] = Name;

            Logger.LogDebug("Request {RequestId} validation completed successfully", request.Id);

            await Task.CompletedTask;
            return AgentResult.CreateSuccess("Request validation completed successfully");
        }
    }
}