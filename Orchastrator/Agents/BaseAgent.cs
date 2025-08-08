using System;
using System.Threading.Tasks;

namespace CodeAssist.Agents
{
    public abstract class BaseAgent
    {
        protected internal string AgentName { get; set; }
        protected string AgentVersion { get; set; }
        protected internal AgentStatus Status { get; set; }

        public BaseAgent(string name, string version)
        {
            AgentName = name;
            AgentVersion = version;
            Status = AgentStatus.Idle;
        }

        public virtual async Task InitializeAsync()
        {
            Status = AgentStatus.Initializing;
            await OnInitialize();
            Status = AgentStatus.Ready;
        }

        public virtual async Task ExecuteAsync()
        {
            if (Status != AgentStatus.Ready)
            {
                throw new InvalidOperationException("Agent is not ready to execute.");
            }

            Status = AgentStatus.Executing;
            await OnExecute();
            Status = AgentStatus.Completed;
        }

        public virtual async Task ShutdownAsync()
        {
            Status = AgentStatus.ShuttingDown;
            await OnShutdown();
            Status = AgentStatus.Idle;
        }

        protected abstract Task OnInitialize();
        protected abstract Task OnExecute();
        protected abstract Task OnShutdown();

        public enum AgentStatus
        {
            Idle,
            Initializing,
            Ready,
            Executing,
            Completed,
            ShuttingDown,
            Error
        }
    }
}