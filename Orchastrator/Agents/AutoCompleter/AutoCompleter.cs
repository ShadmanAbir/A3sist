using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Enums;
using A3sist.Orchastrator.Agents.AutoCompleter.Models;
using A3sist.Orchastrator.Agents.AutoCompleter.Services;

namespace A3sist.Orchastrator.Agents.AutoCompleter
{
    public class AutoCompleter : IAgent
    {
        private readonly CodeCompletionService _codeCompletionService;
        private readonly SnippetCompletionService _snippetCompletionService;
        private readonly ImportCompletionService _importCompletionService;

        public string Name => "AutoCompleter";
        public AgentType Type => AgentType.AutoCompleter;
        public WorkStatus Status { get; private set; }

        public AutoCompleter()
        {
            _codeCompletionService = new CodeCompletionService();
            _snippetCompletionService = new SnippetCompletionService();
            _importCompletionService = new ImportCompletionService();
            Status = WorkStatus.Pending;
        }

        public async Task InitializeAsync()
        {
            Status = WorkStatus.InProgress;
            await Task.WhenAll(
                _codeCompletionService.InitializeAsync(),
                _snippetCompletionService.InitializeAsync(),
                _importCompletionService.InitializeAsync()
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

                var completionContext = JsonSerializer.Deserialize<CompletionContext>(request.Context);

                switch (request.TaskName.ToLower())
                {
                    case "codecompletion":
                        var codeCompletions = await _codeCompletionService.GetCompletionsAsync(completionContext);
                        response.Result = JsonSerializer.Serialize(codeCompletions);
                        break;

                    case "snippetcompletion":
                        var snippetCompletions = await _snippetCompletionService.GetCompletionsAsync(completionContext);
                        response.Result = JsonSerializer.Serialize(snippetCompletions);
                        break;

                    case "importcompletion":
                        var importCompletions = await _importCompletionService.GetCompletionsAsync(completionContext);
                        response.Result = JsonSerializer.Serialize(importCompletions);
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
                _codeCompletionService.ShutdownAsync(),
                _snippetCompletionService.ShutdownAsync(),
                _importCompletionService.ShutdownAsync()
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