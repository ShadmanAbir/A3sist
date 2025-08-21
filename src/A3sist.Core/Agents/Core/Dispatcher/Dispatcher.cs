using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Enums;
using A3sist.Orchastrator.Agents.Dispatcher.Models;
using A3sist.Orchastrator.Agents.Dispatcher.Services;

namespace A3sist.Orchastrator.Agents.Dispatcher
{
    public class Dispatcher : IAgent
    {
        private readonly TaskQueue _taskQueue;
        private readonly TaskScheduler _taskScheduler;
        private readonly TaskOrchestrator _taskOrchestrator;
        private readonly FailureHandler _failureHandler;

        public string Name => "Dispatcher";
        public AgentType Type => AgentType.Dispatcher;
        public WorkStatus Status { get; private set; }

        public Dispatcher()
        {
            _taskQueue = new TaskQueue();
            // TODO: Implement proper task scheduler in subsequent tasks
            _taskScheduler = null!;
            _taskOrchestrator = new TaskOrchestrator();
            _failureHandler = new FailureHandler();
            Status = WorkStatus.Pending;
        }

        public async Task InitializeAsync()
        {
            Status = WorkStatus.InProgress;
            await _taskQueue.InitializeAsync();
            // TODO: Initialize task scheduler when implemented
            await _taskOrchestrator.InitializeAsync();
            await _failureHandler.InitializeAsync();
            Status = WorkStatus.Completed;
        }

        public async Task<AgentResponse> ExecuteAsync(AgentRequest request)
        {
            var response = new AgentResponse
            {
                RequestId = request.RequestId,
                AgentName = Name,
                TaskName = request.TaskName
            };

            try
            {
                Status = WorkStatus.InProgress;

                switch (request.TaskName.ToLower())
                {
                    case "submitworkflow":
                        var workflowRequest = JsonSerializer.Deserialize<WorkflowRequest>(request.Context);
                        var workflowId = await _taskQueue.EnqueueWorkflowAsync(workflowRequest);
                        response.Result = JsonSerializer.Serialize(new { WorkflowId = workflowId });
                        break;

                    case "getworkflowstatus":
                        var workflowIdParam = JsonSerializer.Deserialize<Dictionary<string, string>>(request.Context)["workflowId"];
                        var workflowStatus = await _taskQueue.GetWorkflowStatusAsync(Guid.Parse(workflowIdParam));
                        response.Result = JsonSerializer.Serialize(workflowStatus);
                        break;

                    case "cancelworkflow":
                        var cancelWorkflowId = JsonSerializer.Deserialize<Dictionary<string, string>>(request.Context)["workflowId"];
                        await _taskQueue.CancelWorkflowAsync(Guid.Parse(cancelWorkflowId));
                        response.Result = "Workflow cancelled successfully";
                        break;

                    case "retryfailedtasks":
                        var retryWorkflowId = JsonSerializer.Deserialize<Dictionary<string, string>>(request.Context)["workflowId"];
                        await _taskQueue.RetryFailedTasksAsync(Guid.Parse(retryWorkflowId));
                        response.Result = "Failed tasks retried successfully";
                        break;

                    default:
                        throw new NotSupportedException($"Task {request.TaskName} is not supported by this agent");
                }

                response.IsSuccess = true;
                Status = WorkStatus.Completed;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessage = ex.Message;
                Status = WorkStatus.Failed;
            }

            return response;
        }

        public async Task ShutdownAsync()
        {
            Status = WorkStatus.InProgress;
            await _taskQueue.ShutdownAsync();
            // TODO: Shutdown task scheduler when implemented
            await _taskOrchestrator.ShutdownAsync();
            await _failureHandler.ShutdownAsync();
            Status = WorkStatus.Completed;
        }

        public async Task<AgentResponse> HandleMessageAsync(TaskMessage message)
        {
            // Handle incoming messages (e.g., from other agents)
            return await Task.FromResult(new AgentResponse
            {
                RequestId = message.MessageId,
                AgentName = Name,
                TaskName = "MessageHandling",
                Result = $"Message from {message.Sender} received",
                IsSuccess = true
            });
        }
    }
}