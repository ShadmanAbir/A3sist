using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for language-specific refactoring providers
    /// </summary>
    public interface ILanguageRefactoringProvider
    {
        /// <summary>
        /// Gets the language this provider supports
        /// </summary>
        string Language { get; }

        /// <summary>
        /// Gets the supported refactoring types for this language
        /// </summary>
        IEnumerable<RefactoringType> SupportedRefactorings { get; }

        /// <summary>
        /// Initializes the language provider
        /// </summary>
        /// <returns>Task representing the initialization</returns>
        Task InitializeAsync();

        /// <summary>
        /// Shuts down the language provider
        /// </summary>
        /// <returns>Task representing the shutdown</returns>
        Task ShutdownAsync();

        /// <summary>
        /// Analyzes code for language-specific refactoring opportunities
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <param name="filePath">The file path for context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Language-specific refactoring suggestions</returns>
        Task<IEnumerable<RefactoringSuggestion>> AnalyzeCodeAsync(string code, string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies a language-specific refactoring
        /// </summary>
        /// <param name="code">The code to refactor</param>
        /// <param name="refactoringType">The type of refactoring to apply</param>
        /// <param name="parameters">Additional parameters for the refactoring</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The refactoring result</returns>
        Task<RefactoringResult> ApplyRefactoringAsync(string code, RefactoringType refactoringType, Dictionary<string, object> parameters = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates that a refactoring is safe for this language
        /// </summary>
        /// <param name="originalCode">The original code</param>
        /// <param name="refactoredCode">The refactored code</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<RefactoringValidationResult> ValidateRefactoringAsync(string originalCode, string refactoredCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines if this provider can handle the specified refactoring type
        /// </summary>
        /// <param name="refactoringType">The refactoring type to check</param>
        /// <returns>True if the provider can handle the refactoring</returns>
        bool CanHandleRefactoring(RefactoringType refactoringType);

        /// <summary>
        /// Gets language-specific refactoring patterns and templates
        /// </summary>
        /// <returns>Available refactoring patterns</returns>
        Task<IEnumerable<RefactoringPattern>> GetRefactoringPatternsAsync();
    }

    /// <summary>
    /// Represents a refactoring pattern or template
    /// </summary>
    public class RefactoringPattern
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public RefactoringType Type { get; set; }
        public string Template { get; set; }
        public IEnumerable<string> RequiredParameters { get; set; } = new List<string>();
        public IEnumerable<string> OptionalParameters { get; set; } = new List<string>();
        public string Example { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}