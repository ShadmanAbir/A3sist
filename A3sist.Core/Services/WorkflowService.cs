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
    /// Workflow service that manages multi-step agent processing workflows
    /// </summary>
    public class WorkflowService : IWorkflowService, IDisposable
    {
        private readonly ILogger<WorkflowService> _logger;
        private readonly ConcurrentDictionary<string, IWorkflowStep> _workflowSteps;
        private bool _disposed;

        /// <summary>
        /// Event raised when a workflow starts
        /// </summary>
        public event EventHandler<WorkflowStartedEventArgs>? WorkflowStarted;

        /// <summary>
        /// Event raised when a workflow completes
        /// </summary>
        public event EventHandler<WorkflowCompletedEventArgs>? WorkflowCompleted;

        public WorkflowService(ILogger<WorkflowService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _workflowSteps = new ConcurrentDictionary<string, IWorkflowStep>();

            _logger.LogInformation("WorkflowService initialized");
        }

        /// <summary>
        /// Executes a workflow for the given request
        /// </summary>
        public async Task<WorkflowResult> ExecuteWorkflowAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (_disposed)
                throw new ObjectDisposedException(nameof(WorkflowService));

            var stopwatch = Stopwatch.StartNew();
            var context = new WorkflowContext { Request = request };
            var workflowResult = new WorkflowResult();

            _logger.LogInformation("Starting workflow execution for request {RequestId}", request.Id);

            try
            {
                // Raise workflow started event
                WorkflowStarted?.Invoke(this, new WorkflowStartedEventArgs(request));

                // Get applicable workflow steps
                var applicableSteps = await GetApplicableStepsAsync(request);
                
                if (!applicableSteps.Any())
                {
                    _logger.LogWarning("No applicable workflow steps found for request {RequestId}", request.Id);
                    return WorkflowResult.CreateFailure("No applicable workflow steps found");
                }

                // Sort steps by execution order
                var orderedSteps = applicableSteps.OrderBy(s => s.Order).ToList();
                
                _logger.LogDebug("Executing {StepCount} workflow steps for request {RequestId}: {StepNames}", 
                    orderedSteps.Count, request.Id, string.Join(", ", orderedSteps.Select(s => s.Name)));

                // Execute each step in order
                foreach (var step in orderedSteps)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Workflow execution cancelled for request {RequestId}", request.Id);
                        return WorkflowResult.CreateFailure("Workflow execution was cancelled");
                    }

                    if (!context.ShouldContinue)
                    {
                        _logger.LogInformation("Workflow execution stopped for request {RequestId}: {StopReason}", 
                            request.Id, context.StopReason);
                        break;
                    }

                    var stepResult = await ExecuteStepAsync(step, request, context, cancellationToken);
                    workflowResult.StepResults.Add(stepResult);
                    context.PreviousResults.Add(stepResult);

                    if (!stepResult.Success)
                    {
                        _logger.LogWarning("Workflow step {StepName} failed for request {RequestId}: {ErrorMessage}", 
                            step.Name, request.Id, stepResult.Result.Message);
                        
                        // Decide whether to continue or stop based on step configuration
                        // For now, we'll stop on any failure
                        workflowResult.Success = false;
                        workflowResult.Result = stepResult.Result;
                        break;
                    }
                }

                // If we completed all steps successfully
                if (workflowResult.StepResults.All(r => r.Success) && context.ShouldContinue)
                {
                    workflowResult.Success = true;
                    
                    // Use the result from the last step, or create a success result
                    var lastStepResult = workflowResult.StepResults.LastOrDefault();
                    workflowResult.Result = lastStepResult?.Result ?? AgentResult.CreateSuccess("Workflow completed successfully");
                }

                stopwatch.Stop();
                workflowResult.TotalExecutionTime = stopwatch.Elapsed;

                _logger.LogInformation("Workflow execution completed for request {RequestId} in {ElapsedMs}ms with success: {Success}", 
                    request.Id, stopwatch.ElapsedMilliseconds, workflowResult.Success);

                // Raise workflow completed event
                WorkflowCompleted?.Invoke(this, new WorkflowCompletedEventArgs(request, workflowResult, stopwatch.Elapsed));

                return workflowResult;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                _logger.LogInformation("Workflow execution cancelled for request {RequestId} after {ElapsedMs}ms", 
                    request.Id, stopwatch.ElapsedMilliseconds);
                
                var result = WorkflowResult.CreateFailure("Workflow execution was cancelled");
                result.TotalExecutionTime = stopwatch.Elapsed;
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Unexpected error during workflow execution for request {RequestId} after {ElapsedMs}ms", 
                    request.Id, stopwatch.ElapsedMilliseconds);
                
                var result = WorkflowResult.CreateFailure($"Workflow execution failed: {ex.Message}", ex);
                result.TotalExecutionTime = stopwatch.Elapsed;
                return result;
            }
        }

        /// <summary>
        /// Registers a workflow step
        /// </summary>
        public async Task RegisterWorkflowStepAsync(IWorkflowStep step)
        {
            if (step == null)
                throw new ArgumentNullException(nameof(step));

            if (_disposed)
                throw new ObjectDisposedException(nameof(WorkflowService));

            if (string.IsNullOrWhiteSpace(step.Name))
                throw new ArgumentException("Workflow step name cannot be null or empty", nameof(step));

            _workflowSteps.AddOrUpdate(step.Name, step, (key, existing) =>
            {
                _logger.LogWarning("Replacing existing workflow step {StepName}", step.Name);
                return step;
            });

            _logger.LogInformation("Registered workflow step {StepName} with order {Order}", step.Name, step.Order);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Gets all registered workflow steps
        /// </summary>
        public async Task<IEnumerable<IWorkflowStep>> GetWorkflowStepsAsync()
        {
            await Task.CompletedTask;
            return _workflowSteps.Values.ToList();
        }

        /// <summary>
        /// Gets workflow steps that can handle the given request
        /// </summary>
        private async Task<List<IWorkflowStep>> GetApplicableStepsAsync(AgentRequest request)
        {
            var applicableSteps = new List<IWorkflowStep>();

            foreach (var step in _workflowSteps.Values)
            {
                try
                {
                    if (await step.CanHandleAsync(request))
                    {
                        applicableSteps.Add(step);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking if workflow step {StepName} can handle request {RequestId}", 
                        step.Name, request.Id);
                }
            }

            return applicableSteps;
        }

        /// <summary>
        /// Executes a single workflow step
        /// </summary>
        private async Task<WorkflowStepResult> ExecuteStepAsync(IWorkflowStep step, AgentRequest request, 
            WorkflowContext context, CancellationToken cancellationToken)
        {
            var stepStopwatch = Stopwatch.StartNew();
            
            try
            {
                _logger.LogDebug("Executing workflow step {StepName} for request {RequestId}", step.Name, request.Id);

                var result = await step.ExecuteAsync(request, context, cancellationToken);
                
                stepStopwatch.Stop();
                result.ExecutionTime = stepStopwatch.Elapsed;

                _logger.LogDebug("Workflow step {StepName} completed for request {RequestId} in {ElapsedMs}ms with success: {Success}", 
                    step.Name, request.Id, stepStopwatch.ElapsedMilliseconds, result.Success);

                return result;
            }
            catch (Exception ex)
            {
                stepStopwatch.Stop();
                _logger.LogError(ex, "Workflow step {StepName} failed for request {RequestId} after {ElapsedMs}ms", 
                    step.Name, request.Id, stepStopwatch.ElapsedMilliseconds);

                return new WorkflowStepResult
                {
                    StepName = step.Name,
                    Success = false,
                    Result = AgentResult.CreateFailure($"Step {step.Name} failed: {ex.Message}", ex),
                    ExecutionTime = stepStopwatch.Elapsed,
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Disposes the workflow service
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                _workflowSteps.Clear();
                _logger.LogInformation("WorkflowService disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing WorkflowService");
            }
        }
    }
}