using System;
using System.Threading.Tasks;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Enums;


namespace A3sist.Orchastrator.Agents.Python
{
    public class AgentPythonHandler : IAgent
    {
        private readonly Analyzer _analyzer;
        private readonly RefactorEngine _refactorEngine;

        public string Name => "Agent.Python";
        public AgentType Type => AgentType.Analyzer;
        public WorkStatus Status { get; private set; }

        public AgentPythonHandler()
        {
            _analyzer = new Analyzer();
            _refactorEngine = new RefactorEngine();
            Status = WorkStatus.Pending;
        }

        public async Task InitializeAsync()
        {
            Status = WorkStatus.InProgress;
            await Task.WhenAll(
                _analyzer.InitializeAsync(),
                _refactorEngine.InitializeAsync()
            );
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
                    case "analyze":
                        response.Result = await _analyzer.AnalyzeCodeAsync(request.Context);
                        break;
                    case "refactor":
                        response.Result = await _refactorEngine.RefactorCodeAsync(request.Context);
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
            await Task.WhenAll(
                _analyzer.ShutdownAsync(),
                _refactorEngine.ShutdownAsync()
            );
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