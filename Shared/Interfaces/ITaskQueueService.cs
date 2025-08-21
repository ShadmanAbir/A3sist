
using A3sist.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace A3sist.UI.Components
{
    public interface ITaskQueueService
    {
        Task<bool> CancelTaskAsync(string taskId);
        Task<List<TaskItem>> GetQueuedTasksAsync();
        Task<TaskDetails> GetTaskDetailsAsync(string taskId);
        Task<bool> PauseTaskAsync(string taskId);
        Task<bool> PrioritizeTaskAsync(string taskId);
    }
}