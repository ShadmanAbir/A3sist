using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using A3sist.Shared.Messaging;
using Microsoft.Extensions.Logging;

namespace A3sist.UI.Components
{
    public class TaskWorkflowManager
    {
        private readonly ITaskQueueService _taskQueueService;
        private readonly ILogger<TaskWorkflowManager> _logger;

        public TaskWorkflowManager(
            ITaskQueueService taskQueueService,
            ILogger<TaskWorkflowManager> logger)
        {
            _taskQueueService = taskQueueService;
            _logger = logger;
        }

        public async Task<List<TaskItem>> GetQueuedTasksAsync()
        {
            try
            {
                var tasks = await _taskQueueService.GetQueuedTasksAsync();
                _logger.LogInformation($"Retrieved {tasks.Count} queued tasks");
                return tasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving queued tasks");
                return new List<TaskItem>();
            }
        }

        public async Task<bool> PauseTaskAsync(string taskId)
        {
            try
            {
                var result = await _taskQueueService.PauseTaskAsync(taskId);
                _logger.LogInformation($"Paused task {taskId}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error pausing task {taskId}");
                return false;
            }
        }

        public async Task<bool> CancelTaskAsync(string taskId)
        {
            try
            {
                var result = await _taskQueueService.CancelTaskAsync(taskId);
                _logger.LogInformation($"Cancelled task {taskId}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling task {taskId}");
                return false;
            }
        }

        public async Task<bool> PrioritizeTaskAsync(string taskId)
        {
            try
            {
                var result = await _taskQueueService.PrioritizeTaskAsync(taskId);
                _logger.LogInformation($"Prioritized task {taskId}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error prioritizing task {taskId}");
                return false;
            }
        }

        public async Task<TaskDetails> GetTaskDetailsAsync(string taskId)
        {
            try
            {
                var details = await _taskQueueService.GetTaskDetailsAsync(taskId);
                _logger.LogInformation($"Retrieved details for task {taskId}");
                return details;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving details for task {taskId}");
                return null;
            }
        }
    }
}