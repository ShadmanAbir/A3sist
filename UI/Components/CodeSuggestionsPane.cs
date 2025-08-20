using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using A3sist.Shared.Messaging;
using Microsoft.Extensions.Logging;

namespace A3sist.UI.Components
{
    public class CodeSuggestionsPane
    {
        private readonly ISuggestionService _suggestionService;
        private readonly IEditorIntegrationService _editorService;
        private readonly ILogger<CodeSuggestionsPane> _logger;

        public CodeSuggestionsPane(
            ISuggestionService suggestionService,
            IEditorIntegrationService editorService,
            ILogger<CodeSuggestionsPane> logger)
        {
            _suggestionService = suggestionService;
            _editorService = editorService;
            _logger = logger;
        }

        public async Task<List<CodeSuggestion>> GetSuggestionsAsync(string filePath, int lineNumber)
        {
            try
            {
                var suggestions = await _suggestionService.GetSuggestionsAsync(filePath, lineNumber);
                _logger.LogInformation($"Retrieved {suggestions.Count} suggestions for {filePath}:{lineNumber}");
                return suggestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving suggestions for {filePath}:{lineNumber}");
                return new List<CodeSuggestion>();
            }
        }

        public async Task<bool> ApplySuggestionAsync(CodeSuggestion suggestion)
        {
            try
            {
                var result = await _suggestionService.ApplySuggestionAsync(suggestion);
                if (result.Success)
                {
                    await _editorService.RefreshEditorView(suggestion.FilePath);
                    _logger.LogInformation($"Applied suggestion to {suggestion.FilePath}");
                }
                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error applying suggestion to {suggestion.FilePath}");
                return false;
            }
        }

        public async Task<List<CodeSuggestion>> GetAlternativeSuggestionsAsync(CodeSuggestion originalSuggestion)
        {
            try
            {
                var alternatives = await _suggestionService.GetAlternativeSuggestionsAsync(originalSuggestion);
                _logger.LogInformation($"Retrieved {alternatives.Count} alternative suggestions");
                return alternatives;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alternative suggestions");
                return new List<CodeSuggestion>();
            }
        }
    }
}