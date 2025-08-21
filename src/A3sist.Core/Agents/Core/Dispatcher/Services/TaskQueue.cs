using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using A3sist.Orchastrator.Agents.Dispatcher.Models;
using A3sist.Shared.Enums;

namespace A3sist.Orchastrator.Agents.Dispatcher.Services
{
    public class TaskQueue
    {
        private readonly ConcurrentDictionary<Guid, WorkflowStatus> _workflows = new ConcurrentDictionary<Guid, WorkflowStatus>();
        private readonly BufferBlock<WorkflowRequest> _workflowQueue;
        private readonly BufferBlock<TaskAssignment> _taskQueue;
        private readonly List<Task> _processingTasks = new List<Task>();

        public TaskQueue()
        {
            // Workflow queue — just a simple BufferBlock, ordering only
            var workflowOptions = new DataflowBlockOptions
            {
                EnsureOrdered = true
            };
            var workflowQueue = new BufferBlock<WorkflowRequest>(workflowOptions);

            // Task queue — use ActionBlock if you want parallel execution
            var taskOptions = new ExecutionDataflowBlockOptions
            {
                EnsureOrdered = true,
                MaxDegreeOfParallelism = Environment.ProcessorCount * 2
            };

            //var taskQueue = new ActionBlock<WorkflowRequest>(async request =>
            //{
            //    // Your processing logic here
            //    await ProcessRequestAsync(request);
            //}, taskOptions);

            _taskQueue = new BufferBlock<TaskAssignment>(taskOptions);
        }

        public async Task InitializeAsync()
        {
            // Start processing workflows
            _processingTasks.Add(ProcessWorkflowsAsync());

            // Start processing tasks
            _processingTasks.Add(ProcessTasksAsync());
        }

        private async Task ProcessWorkflowsAsync()
        {
            while (await _workflowQueue.OutputAvailableAsync())
            {
                var workflowRequest = await _workflowQueue.ReceiveAsync();

                // Create workflow status
                var workflowStatus = new WorkflowStatus
                {
                    WorkflowId = workflowRequest.RequestId,
                    WorkflowName = workflowRequest.WorkflowName,
                    Status = WorkflowExecutionStatus.Pending,
                    Tasks = workflowRequest.Tasks.Select(t => new TaskStatus
                    {
                        TaskName = t.TaskName,
                        Status = TaskExecutionStatus.Pending,
                        RetriesRemaining = t.MaxRetries
                    }).ToList()
                };

                // Add to workflow dictionary
                _workflows.TryAdd(workflowRequest.RequestId, workflowStatus);

                // Enqueue tasks
                foreach (var taskDef in workflowRequest.Tasks)
                {
                    var taskAssignment = new TaskAssignment
                    {
                        WorkflowId = workflowRequest.RequestId,
                        TaskName = taskDef.TaskName,
                        AgentName = taskDef.AgentName,
                        TaskType = taskDef.TaskType,
                        Parameters = taskDef.Parameters,
                        MaxRetries = taskDef.MaxRetries,
                        Timeout = taskDef.Timeout,
                        Dependencies = taskDef.Dependencies
                    };

                    await _taskQueue.SendAsync(taskAssignment);
                }

                // Update workflow status
                workflowStatus.Status = WorkflowExecutionStatus.Processing;
            }
        }

        private async Task ProcessTasksAsync()
        {
            while (await _taskQueue.OutputAvailableAsync())
            {
                var taskAssignment = await _taskQueue.ReceiveAsync();

                // Get workflow status
                if (_workflows.TryGetValue(taskAssignment.WorkflowId, out var workflowStatus))
                {
                    // Update task status
                    var taskStatus = workflowStatus.Tasks.FirstOrDefault(t => t.TaskName == taskAssignment.TaskName);
                    if (taskStatus != null)
                    {
                        taskStatus.Status = TaskExecutionStatus.Processing;
                        taskStatus.StartTime = DateTime.UtcNow;
                    }

                    // Process the task (in a real implementation, this would delegate to the appropriate agent)
                    try
                    {
                        // Simulate task processing
                        await Task.Delay(1000);

                        // Update task status to completed
                        if (taskStatus != null)
                        {
                            taskStatus.Status = TaskExecutionStatus.Completed;
                            taskStatus.EndTime = DateTime.UtcNow;
                        }

                        // Check if all tasks are completed
                        if (workflowStatus.Tasks.All(t => t.Status == TaskExecutionStatus.Completed))
                        {
                            workflowStatus.Status = WorkflowExecutionStatus.Completed;
                            workflowStatus.EndTime = DateTime.UtcNow;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle task failure
                        if (taskStatus != null)
                        {
                            taskStatus.Status = TaskExecutionStatus.Failed;
                            taskStatus.ErrorMessage = ex.Message;
                            taskStatus.RetriesRemaining--;

                            // Retry if attempts remain
                            if (taskStatus.RetriesRemaining > 0)
                            {
                                taskStatus.Status = TaskExecutionStatus.Pending;
                                await _taskQueue.SendAsync(taskAssignment);
                            }
                            else
                            {
                                // Check if all tasks are completed or failed
                                if (workflowStatus.Tasks.All(t => t.Status == TaskExecutionStatus.Completed || t.Status == TaskExecutionStatus.Failed))
                                {
                                    workflowStatus.Status = workflowStatus.Tasks.Any(t => t.Status == TaskExecutionStatus.Failed)
                                        ? WorkflowExecutionStatus.Failed
                                        : WorkflowExecutionStatus.Completed;
                                    workflowStatus.EndTime = DateTime.UtcNow;
                                }
                            }
                        }
                    }
                }
            }
        }

        public async Task<Guid> EnqueueWorkflowAsync(WorkflowRequest workflowRequest)
        {
            if (workflowRequest == null)
                throw new ArgumentNullException(nameof(workflowRequest));

            await _workflowQueue.SendAsync(workflowRequest);
            return workflowRequest.RequestId;
        }

        public async Task<WorkflowStatus> GetWorkflowStatusAsync(Guid workflowId)
        {
            if (_workflows.TryGetValue(workflowId, out var workflowStatus))
            {
                return workflowStatus;
            }

            throw new KeyNotFoundException($"Workflow with ID {workflowId} not found");
        }

        public async Task CancelWorkflowAsync(Guid workflowId)
        {
            if (_workflows.TryGetValue(workflowId, out var workflowStatus))
            {
                workflowStatus.Status = WorkflowExecutionStatus.Cancelled;
                workflowStatus.EndTime = DateTime.UtcNow;

                // Cancel all pending tasks in the workflow
                foreach (var task in workflowStatus.Tasks.Where(t => t.Status == TaskExecutionStatus.Pending))
                {
                    task.Status = TaskExecutionStatus.Cancelled;
                }
            }
            else
            {
                throw new KeyNotFoundException($"Workflow with ID {workflowId} not found");
            }
        }

        public async Task RetryFailedTasksAsync(Guid workflowId)
        {
            if (_workflows.TryGetValue(workflowId, out var workflowStatus))
            {
                // Retry all failed tasks
                foreach (var task in workflowStatus.Tasks.Where(t => t.Status == TaskExecutionStatus.Failed && t.RetriesRemaining > 0))
                {
                    task.Status = TaskExecutionStatus.Pending;
                    task.RetriesRemaining--;

                    // Re-enqueue the task
                    var taskAssignment = new TaskAssignment
                    {
                        WorkflowId = workflowId,
                        TaskName = task.TaskName,
                        // Set other properties from the original task definition
                    };

                    await _taskQueue.SendAsync(taskAssignment);
                }

                // Update workflow status
                if (workflowStatus.Tasks.All(t => t.Status != TaskExecutionStatus.Failed))
                {
                    workflowStatus.Status = WorkflowExecutionStatus.Processing;
                }
            }
            else
            {
                throw new KeyNotFoundException($"Workflow with ID {workflowId} not found");
            }
        }

        public async Task ShutdownAsync()
        {
            // Complete the queues
            _workflowQueue.Complete();
            _taskQueue.Complete();

            // Wait for all processing to complete
            await Task.WhenAll(_processingTasks);

            // Clear the workflow dictionary
            _workflows.Clear();
        }
    }

    public class WorkflowStatus
    {
        public Guid WorkflowId { get; set; }
        public string WorkflowName { get; set; }
        public WorkflowExecutionStatus Status { get; set; }
        public List<TaskStatus> Tasks { get; set; } = new List<TaskStatus>();
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
    }

    public class TaskStatus
    {
        public string TaskName { get; set; }
        public TaskExecutionStatus Status { get; set; }
        public int RetriesRemaining { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class TaskAssignment
    {
        public Guid WorkflowId { get; set; }
        public string TaskName { get; set; }
        public string AgentName { get; set; }
        public string TaskType { get; set; }
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        public int MaxRetries { get; set; }
        public TimeSpan Timeout { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
    }

    public enum WorkflowExecutionStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled
    }

    public enum TaskExecutionStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled
    }
}