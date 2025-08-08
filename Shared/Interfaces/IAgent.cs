using System.Threading.Tasks;
using CodeAssist.Shared.Enums;
using CodeAssist.Shared.Messaging;

namespace CodeAssist.Shared.Interfaces
{
    public interface IAgent
    {
        string Name { get; }
        AgentType Type { get; }
        TaskStatus Status { get; }

        Task InitializeAsync();
        Task<AgentResponse> ExecuteAsync(AgentRequest request);
        Task ShutdownAsync();
        Task<AgentResponse> HandleMessageAsync(TaskMessage message);
    }
}