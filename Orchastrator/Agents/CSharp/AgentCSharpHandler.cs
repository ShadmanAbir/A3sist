using System;
using System.Threading.Tasks;
using CodeAssist.Shared.Interfaces;
using CodeAssist.Shared.Messaging;
using CodeAssist.Shared.Enums;
using CodeAssist.Agents.CSharp.Services;

namespace CodeAssist.Agents.CSharp
{
    public class AgentCSharpHandler : IAgent
    {
        private readonly Analyzer _analyzer;
        private readonly RefactorEngine _refactorEngine;
        private readonly XamlValidator _xamlValidator;

        public string Name => "Agent.CSharp";
        public AgentType Type => AgentType.Analyzer;
        public TaskStatus Status { get; private set; }

        public AgentCSharpHandler()
        {
            _analyzer = new Analyzer();
            _refactorEngine = new RefactorEngine();
            _xamlValidator = new XamlValidator();
            Status = TaskStatus.Pending;
        }

        public async Task InitializeAsync()
        {
            Status = TaskStatus.InProgress;
            await Task.WhenAll(
                _analyzer.InitializeAsync(),
                _refactorEngine.InitializeAsync(),
                _xamlValidator.InitializeAsync()
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
                    case "analyze":
                        response.Result = await _analyzer.AnalyzeCodeAsync(request.Context);
                        break;
                    case "refactor":
                        response.Result = await _refactorEngine.RefactorCodeAsync(request.Context);
                        break;
                    case "validatexaml":
                        response.Result = await _xamlValidator.ValidateXamlAsync(request.Context);
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
                _analyzer.ShutdownAsync(),
                _refactorEngine.ShutdownAsync(),
                _xamlValidator.ShutdownAsync()
            );
            Status = TaskStatus.Completed;
        }

        public async Task<AgentResponse> HandleMessageAsync(TaskMessage message)
        {
            // Handle incoming messages (e.g., from other agents)
            // This is a simplified example - implement as needed
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