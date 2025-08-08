using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CodeAssist.Shared.Interfaces;
using CodeAssist.Shared.Messaging;
using CodeAssist.Shared.Enums;
using CodeAssist.Agents.AutoCompleter.Services;

namespace CodeAssist.Agents.AutoCompleter
{
    public class AutoCompleter : IAgent
    {
        private readonly CodeCompletionService _codeCompletionService;
        private readonly SnippetCompletionService _snippetCompletionService;
        private readonly ImportCompletionService _importCompletionService;

        public string Name => "AutoCompleter";
        public AgentType Type => AgentType.AutoCompleter;
        public TaskStatus Status { get; private set; }

        public AutoCompleter()
        {
            _codeCompletionService = new CodeCompletionService();
            _snippetCompletionService = new SnippetCompletionService();
            _importCompletionService = new ImportCompletionService();
            Status = TaskStatus.Pending;
        }

        public async Task InitializeAsync()
        {
            Status = TaskStatus.InProgress;
            await Task.WhenAll(
                _codeCompletionService.InitializeAsync(),
                _snippetCompletionService.InitializeAsync(),
                _importCompletionService.InitializeAsync()
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
                _codeCompletionService.ShutdownAsync(),
                _snippetCompletionService.ShutdownAsync(),
                _importCompletionService.ShutdownAsync()
            );
            Status = TaskStatus.Completed;
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

    public class CompletionContext
    {
        public string Code { get; set; }
        public int CursorPosition { get; set; }
        public string Language { get; set; }
        public string FilePath { get; set; }
        public List<string> ExistingImports { get; set; } = new List<string>();
    }
}