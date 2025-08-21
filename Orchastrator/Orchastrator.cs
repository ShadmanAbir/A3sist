using System;
using System.Threading.Tasks;
using A3sist.Orchastrator.Agents;
using A3sist.Orchastrator.LLM;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using Microsoft.Extensions.Logging;

namespace A3sist.Orchastrator
{
    public class Orchestrator
    {
        private readonly ILLMClient _llmClient;
        private readonly FileEditorAgent _fileEditorAgent;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<Orchestrator> _logger;

        public Orchestrator(
            ILLMClient llmClient,
            FileEditorAgent fileEditorAgent,
            IFileSystem fileSystem,
            ILogger<Orchestrator> logger)
        {
            _llmClient = llmClient;
            _fileEditorAgent = fileEditorAgent;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public async Task<AgentResult> OrchestrateTask(AgentRequest request)
        {
            try
            {
                _logger.LogInformation($"Starting orchestration for request: {request.Id}");

                // Step 1: Validate request
                if (string.IsNullOrEmpty(request.FilePath))
                {
                    return AgentResult.CreateFailure("File path is required");
                }

                // Step 2: Get code completion from LLM
                var completion = await _llmClient.GetCompletionAsync(request.Prompt, request.LLMOptions);

                // Step 3: Prepare file editing input
                var editInput = new AgentInput
                {
                    FilePath = request.FilePath,
                    Content = completion
                };

                // Step 4: Execute file editing
                var editResult = await _fileEditorAgent.HandleAsync(editInput);

                if (editResult.Success)
                {
                    _logger.LogInformation($"Successfully processed request: {request.Id}");
                }
                else
                {
                    _logger.LogWarning($"Request {request.Id} completed with warnings: {editResult.Message}");
                }

                return editResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing request: {request.Id}");
                return AgentResult.CreateFailure($"Orchestration failed: {ex.Message}", ex);
            }
        }
    }
}