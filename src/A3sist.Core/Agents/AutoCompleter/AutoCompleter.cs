using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Enums;
using A3sist.Orchastrator.Agents.AutoCompleter.Models;
using A3sist.Orchastrator.Agents.AutoCompleter.Services;
using Task = System.Threading.Tasks.Task;

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

        public async Task<AgentResult> HandleAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                Status = WorkStatus.InProgress;

                var completionContext = JsonSerializer.Deserialize<CompletionContext>(request.Context?.GetValueOrDefault("context")?.ToString() ?? "{}");

                switch (request.Prompt?.ToLower())
                {
                    case var p when p.Contains("code completion"):
                        var codeCompletions = await _codeCompletionService.GetCompletionsAsync(completionContext);
                        return AgentResult.CreateSuccess("Code completions generated", JsonSerializer.Serialize(codeCompletions), Name);

                    case var p when p.Contains("snippet completion"):
                        var snippetCompletions = await _snippetCompletionService.GetCompletionsAsync(completionContext);
                        return AgentResult.CreateSuccess("Snippet completions generated", JsonSerializer.Serialize(snippetCompletions), Name);

                    case var p when p.Contains("import completion"):
                        var importCompletions = await _importCompletionService.GetCompletionsAsync(completionContext);
                        return AgentResult.CreateSuccess("Import completions generated", JsonSerializer.Serialize(importCompletions), Name);

                    default:
                        return AgentResult.CreateFailure($"Task '{request.Prompt}' is not supported by this agent", agentName: Name);
                }
            }
            catch (Exception ex)
            {
                Status = WorkStatus.Failed;
                return AgentResult.CreateFailure($"AutoCompleter error: {ex.Message}", ex, Name);
            }
            finally
            {
                Status = WorkStatus.Completed;
            }
        }

        public async Task<bool> CanHandleAsync(AgentRequest request)
        {
            if (request?.Prompt == null) return false;

            var prompt = request.Prompt.ToLowerInvariant();
            var completionKeywords = new[] { "completion", "complete", "autocomplete", "intellisense", "suggest", "snippet", "import" };
            
            return completionKeywords.Any(keyword => prompt.Contains(keyword));
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