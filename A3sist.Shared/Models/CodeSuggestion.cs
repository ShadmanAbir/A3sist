using System;
using System.Collections.Generic;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Represents a code suggestion provided by agents
    /// </summary>
    public class CodeSuggestion
    {
        /// <summary>
        /// Unique identifier for the suggestion
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// File path where the suggestion applies
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Line number where the suggestion starts (1-based)
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// Column number where the suggestion starts (1-based)
        /// </summary>
        public int StartColumn { get; set; }

        /// <summary>
        /// Line number where the suggestion ends (1-based)
        /// </summary>
        public int EndLine { get; set; }

        /// <summary>
        /// Column number where the suggestion ends (1-based)
        /// </summary>
        public int EndColumn { get; set; }

        /// <summary>
        /// Title of the suggestion
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Detailed description of the suggestion
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Type of suggestion
        /// </summary>
        public SuggestionType Type { get; set; }

        /// <summary>
        /// Severity level of the suggestion
        /// </summary>
        public SuggestionSeverity Severity { get; set; }

        /// <summary>
        /// Original code text
        /// </summary>
        public string OriginalText { get; set; }

        /// <summary>
        /// Suggested replacement text
        /// </summary>
        public string SuggestedText { get; set; }

        /// <summary>
        /// Confidence score (0.0 to 1.0)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Name of the agent that provided this suggestion
        /// </summary>
        public string AgentName { get; set; }

        /// <summary>
        /// Category of the suggestion
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Tags associated with the suggestion
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Whether this suggestion can be applied automatically
        /// </summary>
        public bool CanAutoApply { get; set; }

        /// <summary>
        /// Whether this suggestion requires user confirmation
        /// </summary>
        public bool RequiresConfirmation { get; set; } = true;

        /// <summary>
        /// Timestamp when the suggestion was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Preview of the changes this suggestion would make
        /// </summary>
        public string PreviewText { get; set; }
    }

    /// <summary>
    /// Types of code suggestions
    /// </summary>
    public enum SuggestionType
    {
        Refactoring,
        CodeFix,
        StyleImprovement,
        PerformanceOptimization,
        SecurityFix,
        BestPractice,
        Documentation,
        Testing,
        Naming,
        Structure,
        Design,
        Maintenance
    }

    /// <summary>
    /// Severity levels for suggestions
    /// </summary>
    public enum SuggestionSeverity
    {
        Info,
        Suggestion,
        Warning,
        Error,
        Critical
    }
}