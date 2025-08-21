using System.Threading.Tasks;
using A3sist.Shared.Enums;

namespace A3sist.Shared.Interfaces
{
    public interface ITaskExecutable
    {
        string TaskName { get; }
        WorkStatus Status { get; }

        Task ExecuteAsync();
        Task CancelAsync();
        Task PauseAsync();
        Task ResumeAsync();
    }
}