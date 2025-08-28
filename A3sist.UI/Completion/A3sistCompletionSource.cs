using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using A3sist.UI.Models;
using A3sist.UI.Services;

namespace A3sist.UI.Completion
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType("text")]
    [Name("A3sist Completion")]
    internal class A3sistCompletionSourceProvider : ICompletionSourceProvider
    {
        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new A3sistCompletionSource(textBuffer);
        }
    }

    internal class A3sistCompletionSource : ICompletionSource
    {
        private readonly ITextBuffer _textBuffer;
        private IA3sistApiClient _apiClient;
        private IA3sistConfigurationService _configService;
        private bool _isDisposed = false;

        public A3sistCompletionSource(ITextBuffer textBuffer)
        {
            _textBuffer = textBuffer;
            
            // Get services from package
            InitializeServices();
        }

        private void InitializeServices()
        {
            try
            {
                // Get the package instance to access services
                var package = A3sistPackage.Instance;
                if (package != null)
                {
                    _apiClient = package.GetService<IA3sistApiClient>();
                    _configService = package.GetService<IA3sistConfigurationService>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Failed to initialize completion services: {ex.Message}");
            }
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            if (_isDisposed || _apiClient == null || _configService == null)
                return;

            try
            {
                // Check if auto-completion is enabled
                var isEnabled = Task.Run(async () => 
                {
                    try
                    {
                        return await _configService.GetSettingAsync("AutoCompleteEnabled", true);
                    }
                    catch
                    {
                        return true; // Default to enabled if config fails
                    }
                }).Result;

                if (!isEnabled)
                    return;

                // Check if API is connected
                if (!_apiClient.IsConnected)
                {
                    // Try to connect
                    var connected = Task.Run(async () => await _apiClient.ConnectAsync()).Result;
                    if (!connected)
                        return;
                }

                var snapshot = _textBuffer.CurrentSnapshot;
                var triggerPoint = session.GetTriggerPoint(snapshot);
                
                if (!triggerPoint.HasValue)
                    return;

                var position = triggerPoint.Value.Position;
                var line = triggerPoint.Value.GetContainingLine();
                var lineText = line.GetText();
                var columnPosition = position - line.Start.Position;

                // Get the current code content
                var fullText = snapshot.GetText();
                
                // Detect language
                var language = DetectLanguage(_textBuffer);
                
                // Get completion suggestions from API
                var completionItems = GetCompletionSuggestions(fullText, position, language);
                
                if (completionItems?.Any() == true)
                {
                    var trackingSpan = FindTokenSpanAtPosition(triggerPoint.Value);
                    var completions = CreateCompletions(completionItems);
                    
                    var completionSet = new CompletionSet(
                        "A3sist",
                        "A3sist AI Completions",
                        trackingSpan,
                        completions,
                        null);
                    
                    completionSets.Add(completionSet);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Completion error: {ex.Message}");
            }
        }

        private IEnumerable<CompletionItem> GetCompletionSuggestions(string code, int position, string language)
        {
            try
            {
                var task = Task.Run(async () =>
                {
                    try
                    {
                        return await _apiClient.GetCompletionSuggestionsAsync(code, position, language);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"A3sist: API completion error: {ex.Message}");
                        return Enumerable.Empty<CompletionItem>();
                    }
                });

                // Wait for result with timeout
                if (task.Wait(TimeSpan.FromSeconds(3)))
                {
                    return task.Result;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("A3sist: Completion request timed out");
                    return Enumerable.Empty<CompletionItem>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Completion request failed: {ex.Message}");
                return Enumerable.Empty<CompletionItem>();
            }
        }

        private string DetectLanguage(ITextBuffer textBuffer)
        {
            try
            {
                var contentType = textBuffer.ContentType;
                
                // Map VS content types to language names
                if (contentType.IsOfType("CSharp"))
                    return "csharp";
                else if (contentType.IsOfType("TypeScript"))
                    return "typescript";
                else if (contentType.IsOfType("JavaScript"))
                    return "javascript";
                else if (contentType.IsOfType("Python"))
                    return "python";
                else if (contentType.IsOfType("Java"))
                    return "java";
                else if (contentType.IsOfType("C/C++"))
                    return "cpp";
                else if (contentType.IsOfType("HTML"))
                    return "html";
                else if (contentType.IsOfType("CSS"))
                    return "css";
                else if (contentType.IsOfType("XML"))
                    return "xml";
                else if (contentType.IsOfType("JSON"))
                    return "json";
                else if (contentType.IsOfType("SQL"))
                    return "sql";
                else
                    return "text";
            }
            catch
            {
                return "text";
            }
        }

        private ITrackingSpan FindTokenSpanAtPosition(SnapshotPoint triggerPoint)
        {
            var line = triggerPoint.GetContainingLine();
            var lineText = line.GetText();
            var position = triggerPoint.Position - line.Start.Position;

            // Find the start of the current word/token
            var tokenStart = position;
            while (tokenStart > 0 && char.IsLetterOrDigit(lineText[tokenStart - 1]))
            {
                tokenStart--;
            }

            // Find the end of the current word/token
            var tokenEnd = position;
            while (tokenEnd < lineText.Length && char.IsLetterOrDigit(lineText[tokenEnd]))
            {
                tokenEnd++;
            }

            var startPoint = line.Start + tokenStart;
            var endPoint = line.Start + tokenEnd;
            
            return triggerPoint.Snapshot.CreateTrackingSpan(
                Span.FromBounds(startPoint, endPoint),
                SpanTrackingMode.EdgeInclusive);
        }

        private IList<Microsoft.VisualStudio.Language.Intellisense.Completion> CreateCompletions(
            IEnumerable<CompletionItem> completionItems)
        {
            var completions = new List<Microsoft.VisualStudio.Language.Intellisense.Completion>();

            foreach (var item in completionItems.OrderByDescending(x => x.Priority).ThenByDescending(x => x.Confidence))
            {
                var insertionText = item.InsertText ?? item.Label;
                var displayText = item.Label;
                var description = item.Documentation ?? item.Detail ?? "";

                // Create icon based on completion item kind
                var iconSource = GetIconForCompletionKind(item.Kind);

                var completion = new Microsoft.VisualStudio.Language.Intellisense.Completion(
                    displayText,
                    insertionText,
                    description,
                    iconSource,
                    null);

                completions.Add(completion);
            }

            return completions;
        }

        private System.Windows.Media.ImageSource GetIconForCompletionKind(CompletionItemKind kind)
        {
            // Return appropriate icons based on completion kind
            // This would typically use VS standard icons or custom icons
            try
            {
                switch (kind)
                {
                    case CompletionItemKind.Method:
                    case CompletionItemKind.Function:
                        return null; // Use default method icon
                    case CompletionItemKind.Class:
                        return null; // Use default class icon
                    case CompletionItemKind.Interface:
                        return null; // Use default interface icon
                    case CompletionItemKind.Property:
                        return null; // Use default property icon
                    case CompletionItemKind.Field:
                    case CompletionItemKind.Variable:
                        return null; // Use default field icon
                    case CompletionItemKind.Keyword:
                        return null; // Use default keyword icon
                    case CompletionItemKind.Snippet:
                        return null; // Use default snippet icon
                    default:
                        return null; // Use default icon
                }
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                
                // Clean up resources
                _apiClient = null;
                _configService = null;
            }
        }
    }

    // Completion trigger provider to control when completion is triggered
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType("text")]
    [Name("A3sist Completion Trigger")]
    internal class A3sistCompletionTriggerProvider : ICompletionSourceProvider
    {
        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            // Return null - we're using the main completion source
            return null;
        }
    }
}