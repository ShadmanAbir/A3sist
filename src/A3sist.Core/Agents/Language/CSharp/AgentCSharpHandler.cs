using System;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Enums;
using A3sist.Orchastrator.Agents.CSharp.Services;

namespace A3sist.Orchastrator.Agents.CSharp
{
    public class AgentCSharpHandler : IAgent
    {
        private readonly Analyzer _analyzer;
        private readonly RefactorEngine _refactorEngine;
        private readonly XamlValidator _xamlValidator;

        public string Name => "Agent.CSharp";
        public AgentType Type => AgentType.Analyzer;
        public WorkStatus Status { get; private set; }



        public AgentCSharpHandler()
        {
            _analyzer = new Analyzer();
            _refactorEngine = new RefactorEngine();
            _xamlValidator = new XamlValidator();
            Status = WorkStatus.Pending;
        }

        public async Task InitializeAsync()
        {
            Status = WorkStatus.InProgress;
            await Task.WhenAll(
                _analyzer.InitializeAsync(),
                _refactorEngine.InitializeAsync(),
                _xamlValidator.InitializeAsync()
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
                    case "validatexaml":
                        response.Result = await _xamlValidator.ValidateXamlAsync(request.Context);
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
                _refactorEngine.ShutdownAsync(),
                _xamlValidator.ShutdownAsync()
            );
            Status = WorkStatus.Completed;
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