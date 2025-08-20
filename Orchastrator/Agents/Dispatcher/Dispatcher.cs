using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Enums;
using A3sist.Agents.Dispatcher.Models;
using A3sist.Agents.Dispatcher.Services;

namespace A3sist.Agents.Dispatcher
{
    public class Dispatcher : IAgent
    {
        private readonly TaskQueue _taskQueue;
        private readonly TaskScheduler _taskScheduler;
        private readonly TaskOrchestrator _taskOrchestrator;
        private readonly FailureHandler _failureHandler;

        public string Name => "Dispatcher";
        public AgentType Type => AgentType.Dispatcher;
        public TaskStatus Status { get; private set; }

        public Dispatcher()
        {
            _taskQueue = new TaskQueue();
            _taskScheduler = new TaskScheduler();
            _taskOrchestrator = new TaskOrchestrator();
            _failureHandler = new FailureHandler();
            Status = TaskStatus.Pending;
        }

        public async Task InitializeAsync()
        {
            Status = TaskStatus.InProgress;
            await Task.WhenAll(
                _taskQueue.InitializeAsync(),
                _taskScheduler.InitializeAsync(),
                _taskOrchestrator.InitializeAsync(),
                _failureHandler.InitializeAsync()
            );
            Status = TaskStatus.Completed;
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
                Status = TaskStatus.InProgress;

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
                Status = TaskStatus.Completed;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessage = ex.Message;
                Status = TaskStatus.Failed;
            }

            return response;
        }

        public async Task ShutdownAsync()
        {
            Status = TaskStatus.InProgress;
            await Task.WhenAll(
                _taskQueue.ShutdownAsync(),
                _taskScheduler.ShutdownAsync(),
                _taskOrchestrator.ShutdownAsync(),
                _failureHandler.ShutdownAsync()
            );
            Status = TaskStatus.Completed;
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