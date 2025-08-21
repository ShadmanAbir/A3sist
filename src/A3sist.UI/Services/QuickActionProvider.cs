using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;

namespace A3sist.UI.Services
{
    /// <summary>
    /// Provides quick actions (light bulb suggestions) for the editor
    /// </summary>
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name("A3sist Quick Actions")]
    [ContentType("text")]
    internal class QuickActionProvider : ISuggestedActionsSourceProvider
    {
        [Import]
        internal ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            if (textBuffer == null || textView == null)
                return null;

            return new A3sistSuggestedActionsSource(textView, textBuffer, TextDocumentFactoryService);
        }
    }

    /// <summary>
    /// Source for A3sist suggested actions
    /// </summary>
    internal class A3sistSuggestedActionsSource : ISuggestedActionsSource
    {
        private readonly ITextView _textView;
        private readonly ITextBuffer _textBuffer;
        private readonly ITextDocumentFactoryService _textDocumentFactoryService;
        private ISuggestionService _suggestionService;
        private ILogger<A3sistSuggestedActionsSource> _logger;

        public A3sistSuggestedActionsSource(
            ITextView textView, 
            ITextBuffer textBuffer,
            ITextDocumentFactoryService textDocumentFactoryService)
        {
            _textView = textView;
            _textBuffer = textBuffer;
            _textDocumentFactoryService = textDocumentFactoryService;

            // Get services from service locator (in a real implementation, use proper DI)
            _suggestionService = ServiceLocator.GetService<ISuggestionService>();
            _logger = ServiceLocator.GetService<ILogger<A3sistSuggestedActionsSource>>();
        }

        public event EventHandler<EventArgs> SuggestedActionsChanged;

        public void Dispose()
        {
            // Cleanup if needed
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(
            ISuggestedActionCategorySet requestedActionCategories,
            SnapshotSpan range,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!_textDocumentFactoryService.TryGetTextDocument(_textBuffer, out var textDocument))
                    return Enumerable.Empty<SuggestedActionSet>();

                var filePath = textDocument.FilePath;
                var lineNumber = range.Start.GetContainingLine().LineNumber + 1;

                // Get suggestions asynchronously
                var suggestions = GetSuggestionsAsync(filePath, lineNumber, cancellationToken).Result;

                if (!suggestions.Any())
                    return Enumerable.Empty<SuggestedActionSet>();

                var actions = suggestions.Select(suggestion => new A3sistSuggestedAction(suggestion, _suggestionService, _logger)).ToArray();

                var actionSet = new SuggestedActionSet(
                    categoryName: "A3sist",
                    actions: actions,
                    title: "A3sist Suggestions",
                    priority: SuggestedActionSetPriority.Medium,
                    applicableToSpan: range);

                return new[] { actionSet };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting suggested actions for range: {Range}", range);
                return Enumerable.Empty<SuggestedActionSet>();
            }
        }

        public Task<bool> HasSuggestedActionsAsync(
            ISuggestedActionCategorySet requestedActionCategories,
            SnapshotSpan range,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!_textDocumentFactoryService.TryGetTextDocument(_textBuffer, out var textDocument))
                    return Task.FromResult(false);

                var filePath = textDocument.FilePath;
                var lineNumber = range.Start.GetContainingLine().LineNumber + 1;

                return HasSuggestionsAsync(filePath, lineNumber, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking for suggested actions for range: {Range}", range);
                return Task.FromResult(false);
            }
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Parse("A3SIST-QUICK-ACTIONS-PROVIDER");
            return true;
        }

        private async Task<List<CodeSuggestion>> GetSuggestionsAsync(string filePath, int lineNumber, CancellationToken cancellationToken)
        {
            try
            {
                if (_suggestionService == null)
                    return new List<CodeSuggestion>();

                var suggestions = await _suggestionService.GetSuggestionsAsync(filePath, lineNumber);
                return suggestions ?? new List<CodeSuggestion>();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting suggestions for file: {FilePath}, line: {LineNumber}", filePath, lineNumber);
                return new List<CodeSuggestion>();
            }
        }

        private async Task<bool> HasSuggestionsAsync(string filePath, int lineNumber, CancellationToken cancellationToken)
        {
            try
            {
                var suggestions = await GetSuggestionsAsync(filePath, lineNumber, cancellationToken);
                return suggestions.Any();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking for suggestions for file: {FilePath}, line: {LineNumber}", filePath, lineNumber);
                return false;
            }
        }

        protected virtual void OnSuggestedActionsChanged()
        {
            SuggestedActionsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Individual suggested action for A3sist
    /// </summary>
    internal class A3sistSuggestedAction : ISuggestedAction
    {
        private readonly CodeSuggestion _suggestion;
        private readonly ISuggestionService _suggestionService;
        private readonly ILogger _logger;

        public A3sistSuggestedAction(
            CodeSuggestion suggestion, 
            ISuggestionService suggestionService,
            ILogger logger)
        {
            _suggestion = suggestion ?? throw new ArgumentNullException(nameof(suggestion));
            _suggestionService = suggestionService ?? throw new ArgumentNullException(nameof(suggestionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string DisplayText => _suggestion.Title;

        public string IconAutomationText => "A3sist Suggestion";

        public Microsoft.VisualStudio.Imaging.Interop.ImageMoniker IconMoniker => GetIconForSuggestionType(_suggestion.Type);

        public string InputGestureText => null;

        public bool HasActionSets => false;

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Enumerable.Empty<SuggestedActionSet>());
        }

        public bool HasPreview => !string.IsNullOrEmpty(_suggestion.PreviewText);

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            if (!HasPreview)
                return Task.FromResult<object>(null);

            // In a real implementation, this would return a proper preview control
            return Task.FromResult<object>(_suggestion.PreviewText);
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Invoking suggested action: {SuggestionId}", _suggestion.Id);

                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    var success = await _suggestionService.ApplySuggestionAsync(_suggestion);
                    if (success)
                    {
                        _logger.LogInformation("Successfully applied suggestion: {SuggestionId}", _suggestion.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to apply suggestion: {SuggestionId}", _suggestion.Id);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking suggested action: {SuggestionId}", _suggestion.Id);
            }
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Parse("A3SIST-SUGGESTED-ACTION");
            return true;
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        private Microsoft.VisualStudio.Imaging.Interop.ImageMoniker GetIconForSuggestionType(SuggestionType suggestionType)
        {
            return suggestionType switch
            {
                SuggestionType.CodeFix => KnownMonikers.QuickFix,
                SuggestionType.Refactoring => KnownMonikers.Refactoring,
                SuggestionType.StyleImprovement => KnownMonikers.FormatDocument,
                SuggestionType.PerformanceOptimization => KnownMonikers.Performance,
                SuggestionType.SecurityFix => KnownMonikers.Security,
                SuggestionType.BestPractice => KnownMonikers.BestPractices,
                SuggestionType.Documentation => KnownMonikers.Documentation,
                SuggestionType.Testing => KnownMonikers.Test,
                SuggestionType.Naming => KnownMonikers.Rename,
                SuggestionType.Structure => KnownMonikers.Structure,
                SuggestionType.Design => KnownMonikers.Design,
                SuggestionType.Maintenance => KnownMonikers.Maintenance,
                _ => KnownMonikers.Lightbulb
            };
        }
    }

    /// <summary>
    /// Service locator for getting services (temporary solution)
    /// </summary>
    internal static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void RegisterService<T>(T service)
        {
            _services[typeof(T)] = service;
        }

        public static T GetService<T>()
        {
            if (_services.TryGetValue(typeof(T), out var service))
                return (T)service;
            return default(T);
        }
    }
}