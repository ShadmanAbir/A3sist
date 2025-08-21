using A3sist.Shared.Enums;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    public interface IAgent
    {
        string Name { get; }
        AgentType Type { get; }
        WorkStatus Status { get; }

        Task InitializeAsync();
        Task<AgentResponse> ExecuteAsync(AgentRequest request);
        Task ShutdownAsync();
        Task<AgentResponse> HandleMessageAsync(TaskMessage message);
    }
}