using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for code refactoring services
    /// </summary>
    public interface IRefactoringService
    {
        /// <summary>
        /// Analyzes code for refactoring opportunities
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <param name="filePath">The file path for context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of refactoring suggestions</returns>
        Task<IEnumerable<RefactoringSuggestion>> AnalyzeCodeAsync(string code, string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies a specific refactoring to code
        /// </summary>
        /// <param name="code">The code to refactor</param>
        /// <param name="refactoringType">The type of refactoring to apply</param>
        /// <param name="parameters">Additional parameters for the refactoring</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The refactored code</returns>
        Task<RefactoringResult> ApplyRefactoringAsync(string code, RefactoringType refactoringType, Dictionary<string, object> parameters = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates that a refactoring is safe to apply
        /// </summary>
        /// <param name="code">The original code</param>
        /// <param name="refactoredCode">The refactored code</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<RefactoringValidationResult> ValidateRefactoringAsync(string code, string refactoredCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets available refactoring types for the given code
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <param name="filePath">The file path for context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Available refactoring types</returns>
        Task<IEnumerable<RefactoringType>> GetAvailableRefactoringsAsync(string code, string filePath, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a refactoring suggestion
    /// </summary>
    public class RefactoringSuggestion
    {
        public RefactoringType Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }
        public RefactoringSeverity Severity { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string PreviewCode { get; set; }
    }

    /// <summary>
    /// Result of applying a refactoring
    /// </summary>
    public class RefactoringResult
    {
        public bool Success { get; set; }
        public string RefactoredCode { get; set; }
        public string Message { get; set; }
        public IEnumerable<string> Warnings { get; set; } = new List<string>();
        public RefactoringType AppliedRefactoring { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of validating a refactoring
    /// </summary>
    public class RefactoringValidationResult
    {
        public bool IsValid { get; set; }
        public bool IsSafe { get; set; }
        public IEnumerable<string> Errors { get; set; } = new List<string>();
        public IEnumerable<string> Warnings { get; set; } = new List<string>();
        public IEnumerable<string> Suggestions { get; set; } = new List<string>();
        public double ConfidenceScore { get; set; }
    }

    /// <summary>
    /// Types of refactoring operations
    /// </summary>
    public enum RefactoringType
    {
        ExtractMethod,
        ExtractVariable,
        ExtractConstant,
        InlineMethod,
        InlineVariable,
        RenameSymbol,
        MoveMethod,
        MoveField,
        EncapsulateField,
        IntroduceParameter,
        RemoveParameter,
        ReorderParameters,
        ConvertToProperty,
        ConvertToAutoProperty,
        SimplifyExpression,
        RemoveUnusedUsings,
        AddNullCheck,
        ConvertToLinq,
        SplitVariable,
        MergeConditionalExpressions,
        InvertConditional,
        ReplaceConditionalWithPolymorphism,
        ExtractInterface,
        PullUpMethod,
        PushDownMethod,
        ReplaceInheritanceWithDelegation,
        ReplaceTypeCodeWithClass,
        ReplaceTypeCodeWithSubclasses,
        ReplaceTypeCodeWithState,
        SelfEncapsulateField,
        ReplaceDataValueWithObject,
        ChangeValueToReference,
        ChangeReferenceToValue,
        ReplaceArrayWithObject,
        DuplicateObservedData,
        ChangeUnidirectionalAssociationToBidirectional,
        ChangeBidirectionalAssociationToUnidirectional,
        ReplaceMagicNumberWithSymbolicConstant,
        ConvertToAsyncAwait,
        UseVarKeyword,
        UseExplicitType,
        SimplifyLinqExpression,
        ConvertToStringInterpolation,
        UsePatternMatching,
        ConvertToExpressionBodiedMember,
        ConvertToBlockBody,
        AddBraces,
        RemoveUnnecessaryBraces,
        ConvertToTernaryOperator,
        ConvertFromTernaryOperator,
        UseCollectionInitializer,
        UseObjectInitializer,
        ConvertToNameof,
        UseThrowExpression,
        UseConditionalAccess,
        SimplifyBooleanExpression,
        RemoveRedundantCode,
        OptimizePerformance
    }

    /// <summary>
    /// Severity levels for refactoring suggestions
    /// </summary>
    public enum RefactoringSeverity
    {
        Info,
        Suggestion,
        Warning,
        Error,
        Critical
    }
}