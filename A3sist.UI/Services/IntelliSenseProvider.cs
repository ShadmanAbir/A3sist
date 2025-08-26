using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;

namespace A3sist.UI.Services
{
    /// <summary>
    /// Provides IntelliSense completion enhancements powered by A3sist agents
    /// </summary>
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [Name("A3sist IntelliSense Provider")]
    [ContentType("text")]
    internal class IntelliSenseProvider : IAsyncCompletionSourceProvider
    {
        [Import]
        internal ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public IAsyncCompletionSource GetOrCreate(ITextView textView)
        {
            if (textView == null)
                return null;

            return new A3sistCompletionSource(textView, TextDocumentFactoryService);
        }
    }

    /// <summary>
    /// A3sist completion source for enhanced IntelliSense
    /// </summary>
    internal class A3sistCompletionSource : IAsyncCompletionSource
    {
        private readonly ITextView _textView;
        private readonly ITextDocumentFactoryService _textDocumentFactoryService;
        private ISuggestionService _suggestionService;
        private IOrchestrator _orchestrator;
        private ILogger<A3sistCompletionSource> _logger;

        public A3sistCompletionSource(ITextView textView, ITextDocumentFactoryService textDocumentFactoryService)
        {
            _textView = textView;
            _textDocumentFactoryService = textDocumentFactoryService;

            // Get services from service locator (in a real implementation, use proper DI)
            _suggestionService = ServiceLocator.GetService<ISuggestionService>();
            _orchestrator = ServiceLocator.GetService<IOrchestrator>();
            _logger = ServiceLocator.GetService<ILogger<A3sistCompletionSource>>();
        }

        public CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
        {
            try
            {
                // Only provide completions for specific triggers
                if (trigger.Reason == CompletionTriggerReason.Invoke ||
                    trigger.Reason == CompletionTriggerReason.InvokeAndCommitIfUnique ||
                    (trigger.Reason == CompletionTriggerReason.Insertion && IsCompletionTriggerCharacter(trigger.Character)))
                {
                    var span = GetCompletionSpan(triggerLocation);
                    return new CompletionStartData(CompletionParticipation.ProvidesItems, span);
                }

                return CompletionStartData.DoesNotParticipateInCompletion;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing completion at position: {Position}", triggerLocation.Position);
                return CompletionStartData.DoesNotParticipateInCompletion;
            }
        }

        public async Task<Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionContext> GetCompletionContextAsync(
            IAsyncCompletionSession session,
            CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken token)
        {
            try
            {
                if (!_textDocumentFactoryService.TryGetTextDocument(_textView.TextBuffer, out var textDocument))
                    return Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionContext.Empty;

                var filePath = textDocument.FilePath;
                var lineNumber = triggerLocation.GetContainingLine().LineNumber + 1;
                var columnNumber = triggerLocation.Position - triggerLocation.GetContainingLine().Start.Position + 1;

                // Get context around the trigger location
                var context = GetCompletionContext(triggerLocation, applicableToSpan);

                // Get AI-powered completions
                var completions = await GetAICompletionsAsync(filePath, lineNumber, columnNumber, context, token);

                var completionItems = completions.Select(completion => CreateCompletionItem(completion)).ToArray();

                return new Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionContext(completionItems);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting completion context at position: {Position}", triggerLocation.Position);
                return Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionContext.Empty;
            }
        }

        public async Task<object> GetDescriptionAsync(
            IAsyncCompletionSession session,
            CompletionItem item,
            CancellationToken token)
        {
            try
            {
                if (item.Properties.TryGetProperty("A3sistCompletion", out CompletionSuggestion completion))
                {
                    return new CompletionDescription(
                        text: completion.Description,
                        documentation: completion.Documentation);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting description for completion item: {DisplayText}", item.DisplayText);
                return null;
            }
        }

        private bool IsCompletionTriggerCharacter(char character)
        {
            // Define characters that should trigger completion
            return character == '.' || character == '(' || character == '<' || character == ' ';
        }

        private SnapshotSpan GetCompletionSpan(SnapshotPoint triggerLocation)
        {
            var line = triggerLocation.GetContainingLine();
            var lineText = line.GetText();
            var position = triggerLocation.Position - line.Start.Position;

            // Find the start of the current word
            int start = position;
            while (start > 0 && (char.IsLetterOrDigit(lineText[start - 1]) || lineText[start - 1] == '_'))
            {
                start--;
            }

            // Find the end of the current word
            int end = position;
            while (end < lineText.Length && (char.IsLetterOrDigit(lineText[end]) || lineText[end] == '_'))
            {
                end++;
            }

            return new SnapshotSpan(line.Start + start, end - start);
        }

        private CompletionContext GetCompletionContext(SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan)
        {
            var line = triggerLocation.GetContainingLine();
            var lineText = line.GetText();
            var position = triggerLocation.Position - line.Start.Position;

            return new CompletionContext
            {
                LineText = lineText,
                Position = position,
                PrecedingText = position > 0 ? lineText.Substring(0, position) : string.Empty,
                FollowingText = position < lineText.Length ? lineText.Substring(position) : string.Empty
            };
        }

        private async Task<List<CompletionSuggestion>> GetAICompletionsAsync(
            string filePath, 
            int lineNumber, 
            int columnNumber, 
            CompletionContext context, 
            CancellationToken token)
        {
            try
            {
                if (_orchestrator == null)
                    return new List<CompletionSuggestion>();

                var request = new AgentRequest
                {
                    Prompt = $"Provide code completions for the current context",
                    FilePath = filePath,
                    Content = context.LineText,
                    PreferredAgentType = AgentType.CSharp, // Determine based on file extension
                    Context = new Dictionary<string, object>
                    {
                        ["RequestType"] = "CodeCompletion",
                        ["LineNumber"] = lineNumber,
                        ["ColumnNumber"] = columnNumber,
                        ["PrecedingText"] = context.PrecedingText,
                        ["FollowingText"] = context.FollowingText,
                        ["MaxCompletions"] = 10
                    }
                };

                var result = await _orchestrator.ProcessRequestAsync(request, token);

                if (result.Success && result.Metadata.ContainsKey("Completions"))
                {
                    var completions = result.Metadata["Completions"] as List<CompletionSuggestion>;
                    return completions ?? new List<CompletionSuggestion>();
                }

                // Fallback: provide basic completions
                return GetBasicCompletions(context);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting AI completions for file: {FilePath}", filePath);
                return new List<CompletionSuggestion>();
            }
        }

        private List<CompletionSuggestion> GetBasicCompletions(CompletionContext context)
        {
            var completions = new List<CompletionSuggestion>();

            // Basic C# completions based on context
            if (context.PrecedingText.EndsWith("."))
            {
                completions.AddRange(new[]
                {
                    new CompletionSuggestion
                    {
                        Text = "ToString()",
                        DisplayText = "ToString()",
                        Description = "Returns a string representation of the object",
                        Kind = CompletionKind.Method,
                        Priority = 1
                    },
                    new CompletionSuggestion
                    {
                        Text = "GetHashCode()",
                        DisplayText = "GetHashCode()",
                        Description = "Returns the hash code for this instance",
                        Kind = CompletionKind.Method,
                        Priority = 2
                    },
                    new CompletionSuggestion
                    {
                        Text = "Equals(object)",
                        DisplayText = "Equals(object)",
                        Description = "Determines whether the specified object is equal to the current object",
                        Kind = CompletionKind.Method,
                        Priority = 3
                    }
                });
            }

            return completions;
        }

        private CompletionItem CreateCompletionItem(CompletionSuggestion completion)
        {
            var item = new CompletionItem(
                displayText: completion.DisplayText,
                source: this,
                icon: GetIconForCompletionKind(completion.Kind),
                filters: GetFiltersForCompletionKind(completion.Kind),
                suffix: completion.Suffix,
                insertText: completion.Text,
                sortText: completion.Priority.ToString("D3") + completion.DisplayText,
                filterText: completion.FilterText ?? completion.DisplayText,
                automationText: completion.DisplayText);

            // Store the completion suggestion for later use
            item.Properties.AddProperty("A3sistCompletion", completion);

            return item;
        }

        private Microsoft.VisualStudio.Imaging.Interop.ImageMoniker GetIconForCompletionKind(CompletionKind kind)
        {
            return kind switch
            {
                CompletionKind.Method => Microsoft.VisualStudio.Imaging.KnownMonikers.Method,
                CompletionKind.Property => Microsoft.VisualStudio.Imaging.KnownMonikers.Property,
                CompletionKind.Field => Microsoft.VisualStudio.Imaging.KnownMonikers.Field,
                CompletionKind.Class => Microsoft.VisualStudio.Imaging.KnownMonikers.Class,
                CompletionKind.Interface => Microsoft.VisualStudio.Imaging.KnownMonikers.Interface,
                CompletionKind.Enum => Microsoft.VisualStudio.Imaging.KnownMonikers.Enumeration,
                CompletionKind.Namespace => Microsoft.VisualStudio.Imaging.KnownMonikers.Namespace,
                CompletionKind.Variable => Microsoft.VisualStudio.Imaging.KnownMonikers.LocalVariable,
                CompletionKind.Keyword => Microsoft.VisualStudio.Imaging.KnownMonikers.Keyword,
                CompletionKind.Snippet => Microsoft.VisualStudio.Imaging.KnownMonikers.Snippet,
                _ => Microsoft.VisualStudio.Imaging.KnownMonikers.Method
            };
        }

        private ImmutableArray<CompletionFilter> GetFiltersForCompletionKind(CompletionKind kind)
        {
            // Return appropriate filters based on completion kind
            return ImmutableArray<CompletionFilter>.Empty;
        }
    }

    /// <summary>
    /// Represents a completion suggestion from A3sist agents
    /// </summary>
    public class CompletionSuggestion
    {
        public string Text { get; set; }
        public string DisplayText { get; set; }
        public string Description { get; set; }
        public string Documentation { get; set; }
        public CompletionKind Kind { get; set; }
        public int Priority { get; set; }
        public string Suffix { get; set; }
        public string FilterText { get; set; }
    }

    /// <summary>
    /// Types of completions
    /// </summary>
    public enum CompletionKind
    {
        Method,
        Property,
        Field,
        Class,
        Interface,
        Enum,
        Namespace,
        Variable,
        Keyword,
        Snippet,
        Event,
        Delegate,
        Constructor
    }

    /// <summary>
    /// Context information for completion
    /// </summary>
    public class CompletionContext
    {
        public string LineText { get; set; }
        public int Position { get; set; }
        public string PrecedingText { get; set; }
        public string FollowingText { get; set; }
    }

    /// <summary>
    /// Completion description for detailed information
    /// </summary>
    public class CompletionDescription
    {
        public string Text { get; }
        public string Documentation { get; }

        public CompletionDescription(string text, string documentation = null)
        {
            Text = text;
            Documentation = documentation;
        }
    }
}