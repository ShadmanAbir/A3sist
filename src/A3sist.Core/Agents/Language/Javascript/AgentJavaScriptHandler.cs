using System;
using System.Linq;
using System.Threading.Tasks;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Enums;
using A3sist.Orchastrator.Agents.JavaScript.Services;

namespace A3sist.Orchastrator.Agents.JavaScript
{
    public class AgentJavaScriptHandler : IAgent
    {
        private readonly JsAgentLoader _loader;

        public string Name => "Agent.JavaScript";
        public AgentType Type => AgentType.Analyzer;
        public WorkStatus Status { get; private set; }

        public AgentJavaScriptHandler()
        {
            _loader = new JsAgentLoader();
            Status = WorkStatus.Pending;
        }

        public async Task InitializeAsync()
        {
            Status = WorkStatus.InProgress;
            await _loader.InitializeAsync();
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

                var analyzeTasks = _loader.Analyzers
                    .Select(a => a.AnalyzeCodeAsync(request.Context));

                var results = await Task.WhenAll(analyzeTasks);

                response.Result = string.Join("\n", results);
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
            await _loader.ShutdownAsync();
            Status = WorkStatus.Completed;
        }

        public async Task<AgentResponse> HandleMessageAsync(TaskMessage message)
        {
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
