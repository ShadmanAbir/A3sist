using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Core.Services.WorkflowSteps
{
    /// <summary>
    /// Workflow step that preprocesses requests before agent processing
    /// </summary>
    public class PreprocessingWorkflowStep : BaseWorkflowStep
    {
        public override string Name => "Preprocessing";
        public override int Order => 2;

        public PreprocessingWorkflowStep(ILogger<PreprocessingWorkflowStep> logger) : base(logger)
        {
        }

        protected override Task<bool> CanHandleRequestAsync(AgentRequest request)
        {
            // This step can handle requests that need preprocessing
            return Task.FromResult(!string.IsNullOrWhiteSpace(request.Content) || 
                                   !string.IsNullOrWhiteSpace(request.FilePath));
        }

        protected override async Task<AgentResult> ExecuteStepAsync(AgentRequest request, WorkflowContext context, CancellationToken cancellationToken)
        {
            Logger.LogDebug("Preprocessing request {RequestId}", request.Id);

            var preprocessingResults = new Dictionary<string, object>();

            // Analyze file extension if file path is provided
            if (!string.IsNullOrWhiteSpace(request.FilePath))
            {
                var extension = System.IO.Path.GetExtension(request.FilePath)?.ToLowerInvariant();
                preprocessingResults["FileExtension"] = extension ?? "";
                
                // Determine language based on file extension
                var language = extension switch
                {
                    ".cs" => "C#",
                    ".js" => "JavaScript",
                    ".ts" => "TypeScript",
                    ".py" => "Python",
                    ".java" => "Java",
                    ".cpp" or ".cc" or ".cxx" => "C++",
                    ".c" => "C",
                    ".go" => "Go",
                    ".rs" => "Rust",
                    _ => "Unknown"
                };
                
                preprocessingResults["DetectedLanguage"] = language;
                Logger.LogDebug("Detected language {Language} for file {FilePath}", language, request.FilePath);
            }

            // Analyze content if provided
            if (!string.IsNullOrWhiteSpace(request.Content))
            {
                preprocessingResults["ContentLength"] = request.Content.Length;
                preprocessingResults["LineCount"] = request.Content.Split('\n').Length;
                
                // Simple keyword analysis
                var keywords = new[] { "class", "function", "method", "interface", "import", "export" };
                var keywordCounts = new Dictionary<string, int>();
                
                foreach (var keyword in keywords)
                {
                    var count = CountOccurrences(request.Content, keyword);
                    if (count > 0)
                    {
                        keywordCounts[keyword] = count;
                    }
                }
                
                if (keywordCounts.Count > 0)
                {
                    preprocessingResults["KeywordAnalysis"] = keywordCounts;
                }
            }

            // Analyze prompt for intent hints
            if (!string.IsNullOrWhiteSpace(request.Prompt))
            {
                var promptLower = request.Prompt.ToLowerInvariant();
                var intentHints = new List<string>();
                
                if (promptLower.Contains("refactor"))
                    intentHints.Add("refactoring");
                if (promptLower.Contains("fix") || promptLower.Contains("error"))
                    intentHints.Add("fixing");
                if (promptLower.Contains("test"))
                    intentHints.Add("testing");
                if (promptLower.Contains("document"))
                    intentHints.Add("documentation");
                if (promptLower.Contains("optimize"))
                    intentHints.Add("optimization");
                
                if (intentHints.Count > 0)
                {
                    preprocessingResults["IntentHints"] = intentHints;
                }
            }

            // Add preprocessing results to context
            context.Data["PreprocessingResults"] = preprocessingResults;
            context.Data["PreprocessedAt"] = DateTime.UtcNow;

            Logger.LogDebug("Request {RequestId} preprocessing completed with {ResultCount} analysis results", 
                request.Id, preprocessingResults.Count);

            await Task.CompletedTask;
            
            var result = AgentResult.CreateSuccess("Request preprocessing completed successfully");
            result.Metadata = new Dictionary<string, object>(preprocessingResults);
            
            return result;
        }

        private static int CountOccurrences(string text, string keyword)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword))
                return 0;

            int count = 0;
            int index = 0;
            
            while ((index = text.IndexOf(keyword, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += keyword.Length;
            }
            
            return count;
        }
    }
}