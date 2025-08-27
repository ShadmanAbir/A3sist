using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using A3sist.Models;

namespace A3sist.Services
{
    public class RefactoringService : IRefactoringService
    {
        private readonly IModelManagementService _modelService;
        private readonly ICodeAnalysisService _codeAnalysisService;
        private readonly IA3sistConfigurationService _configService;
        private readonly Dictionary<string, RefactoringResult> _refactoringHistory;

        public RefactoringService(
            IModelManagementService modelService,
            ICodeAnalysisService codeAnalysisService,
            IA3sistConfigurationService configService)
        {
            _modelService = modelService;
            _codeAnalysisService = codeAnalysisService;
            _configService = configService;
            _refactoringHistory = new Dictionary<string, RefactoringResult>();
        }

        public async Task<IEnumerable<RefactoringSuggestion>> GetRefactoringSuggestionsAsync(string code, string language)
        {
            try
            {
                var context = await _codeAnalysisService.ExtractContextAsync(code, 0);
                var issues = await _codeAnalysisService.AnalyzeCodeAsync(code, language);

                var prompt = BuildRefactoringPrompt(code, language, context, issues);

                var modelRequest = new ModelRequest
                {
                    Prompt = prompt,
                    SystemMessage = "You are a code refactoring expert. Analyze code and suggest improvements for readability, performance, and maintainability.",
                    MaxTokens = 1000,
                    Temperature = 0.5
                };

                var response = await _modelService.SendRequestAsync(modelRequest);
                if (!response.Success)
                    return new List<RefactoringSuggestion>();

                return ParseRefactoringSuggestions(response.Content, code);
            }
            catch
            {
                return new List<RefactoringSuggestion>();
            }
        }

        public async Task<RefactoringResult> ApplyRefactoringAsync(RefactoringSuggestion suggestion)
        {
            try
            {
                var result = new RefactoringResult
                {
                    Id = Guid.NewGuid().ToString(),
                    Success = true,
                    ModifiedCode = suggestion.RefactoredCode,
                    ChangedFiles = new List<string> { "current_file" }
                };

                _refactoringHistory[result.Id] = result;
                return result;
            }
            catch (Exception ex)
            {
                return new RefactoringResult
                {
                    Id = Guid.NewGuid().ToString(),
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<RefactoringPreview> PreviewRefactoringAsync(RefactoringSuggestion suggestion)
        {
            return new RefactoringPreview
            {
                Id = suggestion.Id,
                OriginalCode = suggestion.OriginalCode,
                PreviewCode = suggestion.RefactoredCode,
                Changes = GenerateChanges(suggestion.OriginalCode, suggestion.RefactoredCode)
            };
        }

        public async Task<bool> RollbackRefactoringAsync(string refactoringId)
        {
            return _refactoringHistory.ContainsKey(refactoringId);
        }

        public async Task<IEnumerable<CodeCleanupSuggestion>> GetCleanupSuggestionsAsync(string code, string language)
        {
            var suggestions = new List<CodeCleanupSuggestion>();

            // Add some basic cleanup suggestions
            if (code.Contains("using System;") && code.Contains("using System.Linq;"))
            {
                suggestions.Add(new CodeCleanupSuggestion
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Remove unused using statements",
                    Description = "Remove unnecessary using statements to clean up the code",
                    Type = CleanupType.RemoveUnusedUsings,
                    OriginalCode = code,
                    CleanedCode = code // Would implement actual cleanup logic
                });
            }

            return suggestions;
        }

        private string BuildRefactoringPrompt(string code, string language, CodeContext context, IEnumerable<CodeIssue> issues)
        {
            var prompt = $@"Analyze the following {language} code and suggest refactoring improvements:

```{language}
{code}
```

Context:
- Current method: {context.CurrentMethod ?? "unknown"}
- Current class: {context.CurrentClass ?? "unknown"}

Issues found:
{string.Join("\n", issues.Select(i => $"- {i.Message}"))}

Suggest specific refactoring improvements for:
1. Code readability
2. Performance optimization
3. Best practices compliance
4. Maintainability

Format your response as numbered suggestions with before/after code examples.";

            return prompt;
        }

        private IEnumerable<RefactoringSuggestion> ParseRefactoringSuggestions(string response, string originalCode)
        {
            var suggestions = new List<RefactoringSuggestion>();
            
            // Simple parsing - in a real implementation, you'd have more sophisticated parsing
            suggestions.Add(new RefactoringSuggestion
            {
                Id = Guid.NewGuid().ToString(),
                Title = "AI-suggested refactoring",
                Description = "Refactoring suggestion from AI model",
                OriginalCode = originalCode,
                RefactoredCode = response,
                Type = RefactoringType.OptimizeUsings,
                Priority = 1
            });

            return suggestions;
        }

        private List<CodeChange> GenerateChanges(string original, string refactored)
        {
            // Simple diff - in a real implementation, you'd use a proper diff algorithm
            return new List<CodeChange>
            {
                new CodeChange
                {
                    StartLine = 1,
                    EndLine = 1,
                    OriginalText = original,
                    NewText = refactored,
                    Type = ChangeType.Modification
                }
            };
        }
    }
}