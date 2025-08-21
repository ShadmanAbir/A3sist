using A3sist.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    public interface ISuggestionService
    {
        Task ApplySuggestionAsync(CodeSuggestion suggestion);
        Task<List<CodeSuggestion>> GetAlternativeSuggestionsAsync(CodeSuggestion originalSuggestion);
        Task<List<CodeSuggestion>> GetSuggestionsAsync(string filePath, int lineNumber);
    }
}