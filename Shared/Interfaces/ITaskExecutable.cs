using System.Threading.Tasks;
using CodeAssist.Shared.Enums;

namespace CodeAssist.Shared.Interfaces
{
    public interface ITaskExecutable
    {
        string TaskName { get; }
        TaskStatus Status { get; }

        Task ExecuteAsync();
        Task CancelAsync();
        Task PauseAsync();
        Task ResumeAsync();
    }
}