using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using A3sist.Models;

namespace A3sist.Services
{
    public class AutoCompleteService : IAutoCompleteService
    {
        private readonly IModelManagementService _modelService;
        private readonly ICodeAnalysisService _codeAnalysisService;
        private readonly IA3sistConfigurationService _configService;

        public AutoCompleteService(
            IModelManagementService modelService,
            ICodeAnalysisService codeAnalysisService,
            IA3sistConfigurationService configService)
        {
            _modelService = modelService;
            _codeAnalysisService = codeAnalysisService;
            _configService = configService;
        }

        public async Task<IEnumerable<CompletionItem>> GetCompletionsAsync(string code, int position, string language)
        {
            var isEnabled = await IsAutoCompleteEnabledAsync();
            if (!isEnabled)
                return new List<CompletionItem>();

            try
            {
                // Extract context around cursor position
                var context = await _codeAnalysisService.ExtractContextAsync(code, position);
                
                // Build prompt for completion
                var prompt = BuildCompletionPrompt(code, position, context, language);
                
                // Get suggestions from model
                var modelRequest = new ModelRequest
                {
                    Prompt = prompt,
                    SystemMessage = "You are a code completion assistant. Provide concise, accurate code completions.",
                    MaxTokens = 150,
                    Temperature = 0.3
                };

                var response = await _modelService.SendRequestAsync(modelRequest);
                if (!response.Success)
                    return new List<CompletionItem>();

                return ParseCompletionResponse(response.Content, language);
            }
            catch
            {
                return new List<CompletionItem>();
            }
        }

        public async Task<bool> IsAutoCompleteEnabledAsync()
        {
            return await _configService.GetSettingAsync("autocomplete.enabled", true);
        }

        public async Task SetAutoCompleteEnabledAsync(bool enabled)
        {
            await _configService.SetSettingAsync("autocomplete.enabled", enabled);
        }

        public async Task<CompletionSettings> GetCompletionSettingsAsync()
        {
            return new CompletionSettings
            {
                IsEnabled = await _configService.GetSettingAsync("autocomplete.enabled", true),
                TriggerDelay = await _configService.GetSettingAsync("autocomplete.triggerDelay", 500),
                MaxSuggestions = await _configService.GetSettingAsync("autocomplete.maxSuggestions", 10),
                TriggerCharacters = await _configService.GetSettingAsync("autocomplete.triggerCharacters", new List<string> { ".", "_", "-" }),
                ShowDocumentation = await _configService.GetSettingAsync("autocomplete.showDocumentation", true),
                EnableAICompletion = await _configService.GetSettingAsync("autocomplete.enableAI", true)
            };
        }

        public async Task SetCompletionSettingsAsync(CompletionSettings settings)
        {
            await _configService.SetSettingAsync("autocomplete.enabled", settings.IsEnabled);
            await _configService.SetSettingAsync("autocomplete.triggerDelay", settings.TriggerDelay);
            await _configService.SetSettingAsync("autocomplete.maxSuggestions", settings.MaxSuggestions);
            await _configService.SetSettingAsync("autocomplete.triggerCharacters", settings.TriggerCharacters);
            await _configService.SetSettingAsync("autocomplete.showDocumentation", settings.ShowDocumentation);
            await _configService.SetSettingAsync("autocomplete.enableAI", settings.EnableAICompletion);
        }

        private string BuildCompletionPrompt(string code, int position, CodeContext context, string language)
        {
            var beforeCursor = code.Substring(0, position);
            var afterCursor = code.Substring(position);

            return $@"Complete the following {language} code at the cursor position (marked with <CURSOR>):

```{language}
{beforeCursor}<CURSOR>{afterCursor}
```

Context:
- Current method: {context.CurrentMethod ?? "unknown"}
- Current class: {context.CurrentClass ?? "unknown"}
- Language: {language}

Provide only the completion text without any explanation or additional formatting.";
        }

        private IEnumerable<CompletionItem> ParseCompletionResponse(string response, string language)
        {
            var completions = new List<CompletionItem>();

            // Simple parsing - split by lines and create completion items
            var lines = response.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines.Take(5)) // Limit to 5 completions
            {
                var trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine) && !trimmedLine.StartsWith("```"))
                {
                    completions.Add(new CompletionItem
                    {
                        Label = trimmedLine,
                        InsertText = trimmedLine,
                        Kind = DetermineCompletionKind(trimmedLine, language),
                        Confidence = 0.8,
                        Priority = completions.Count
                    });
                }
            }

            return completions;
        }

        private CompletionItemKind DetermineCompletionKind(string text, string language)
        {
            if (text.Contains("(") && text.Contains(")"))
                return CompletionItemKind.Method;
            if (text.Contains("class "))
                return CompletionItemKind.Class;
            if (text.Contains("interface "))
                return CompletionItemKind.Interface;
            if (text.Contains("var ") || text.Contains("let ") || text.Contains("const "))
                return CompletionItemKind.Variable;
            
            return CompletionItemKind.Text;
        }
    }
}