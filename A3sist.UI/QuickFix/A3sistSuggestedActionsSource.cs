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

namespace A3sist.UI.QuickFix
{
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [ContentType("text")]
    [Name("A3sist Suggested Actions")]
    internal class A3sistSuggestedActionsSourceProvider : ISuggestedActionsSourceProvider
    {
        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            if (textBuffer == null || textView == null)
                return null;

            return new A3sistSuggestedActionsSource(textView, textBuffer);
        }
    }

    internal class A3sistSuggestedActionsSource : ISuggestedActionsSource
    {
        private readonly ITextView _textView;
        private readonly ITextBuffer _textBuffer;
        private IA3sistApiClient _apiClient;
        private IA3sistConfigurationService _configService;
        private bool _isDisposed = false;

        public event EventHandler<EventArgs> SuggestedActionsChanged;

        public A3sistSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            _textView = textView;
            _textBuffer = textBuffer;
            
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
                System.Diagnostics.Debug.WriteLine($"A3sist: Failed to initialize suggested actions services: {ex.Message}");
            }
        }

        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, 
            SnapshotSpan range, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                if (_isDisposed || _apiClient == null || _configService == null)
                    return false;

                try
                {
                    // Check if suggestions are enabled
                    var showSuggestions = await _configService.GetSettingAsync("ShowSuggestions", true);
                    if (!showSuggestions)
                        return false;

                    // Check if API is connected
                    if (!_apiClient.IsConnected)
                        return false;

                    // Get code and language
                    var snapshot = range.Snapshot;
                    var text = snapshot.GetText();
                    var language = DetectLanguage(_textBuffer);

                    // Check for code issues or refactoring opportunities
                    var issues = await _apiClient.AnalyzeCodeAsync(text, language);
                    var suggestions = await _apiClient.GetRefactoringSuggestionsAsync(text, language);

                    return issues?.Any() == true || suggestions?.Any() == true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Error checking for suggested actions: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, 
            SnapshotSpan range, CancellationToken cancellationToken)
        {
            if (_isDisposed || _apiClient == null || _configService == null)
                return Enumerable.Empty<SuggestedActionSet>();

            try
            {
                var snapshot = range.Snapshot;
                var text = snapshot.GetText();
                var language = DetectLanguage(_textBuffer);
                
                var actionSets = new List<SuggestedActionSet>();

                // Get refactoring suggestions
                var refactoringSuggestions = GetRefactoringSuggestions(text, language, range);
                if (refactoringSuggestions?.Any() == true)
                {
                    var refactoringActions = refactoringSuggestions.Select(suggestion => 
                        new A3sistRefactoringAction(suggestion, _textView, _textBuffer, _apiClient));
                    
                    actionSets.Add(new SuggestedActionSet(
                        "A3sist Refactoring", 
                        refactoringActions, 
                        SuggestedActionSetPriority.Medium));
                }

                // Get code analysis suggestions
                var codeIssues = GetCodeIssues(text, language, range);
                if (codeIssues?.Any() == true)
                {
                    var codeFixActions = codeIssues
                        .Where(issue => issue.SuggestedFixes?.Any() == true)
                        .Select(issue => new A3sistCodeFixAction(issue, _textView, _textBuffer, _apiClient));
                    
                    if (codeFixActions.Any())
                    {
                        actionSets.Add(new SuggestedActionSet(
                            "A3sist Code Fixes", 
                            codeFixActions, 
                            SuggestedActionSetPriority.High));
                    }
                }

                return actionSets;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Error getting suggested actions: {ex.Message}");
                return Enumerable.Empty<SuggestedActionSet>();
            }
        }

        private IEnumerable<RefactoringSuggestion> GetRefactoringSuggestions(string code, string language, SnapshotSpan range)
        {
            try
            {
                var task = Task.Run(async () =>
                {
                    try
                    {
                        return await _apiClient.GetRefactoringSuggestionsAsync(code, language);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"A3sist: API refactoring error: {ex.Message}");
                        return Enumerable.Empty<RefactoringSuggestion>();
                    }
                });

                if (task.Wait(TimeSpan.FromSeconds(2)))
                {
                    return task.Result?.Where(s => IsRelevantForRange(s, range)) ?? Enumerable.Empty<RefactoringSuggestion>();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("A3sist: Refactoring suggestions request timed out");
                    return Enumerable.Empty<RefactoringSuggestion>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Refactoring suggestions failed: {ex.Message}");
                return Enumerable.Empty<RefactoringSuggestion>();
            }
        }

        private IEnumerable<CodeIssue> GetCodeIssues(string code, string language, SnapshotSpan range)
        {
            try
            {
                var task = Task.Run(async () =>
                {
                    try
                    {
                        return await _apiClient.AnalyzeCodeAsync(code, language);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"A3sist: API analysis error: {ex.Message}");
                        return Enumerable.Empty<CodeIssue>();
                    }
                });

                if (task.Wait(TimeSpan.FromSeconds(2)))
                {
                    return task.Result?.Where(issue => IsRelevantForRange(issue, range)) ?? Enumerable.Empty<CodeIssue>();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("A3sist: Code analysis request timed out");
                    return Enumerable.Empty<CodeIssue>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist: Code analysis failed: {ex.Message}");
                return Enumerable.Empty<CodeIssue>();
            }
        }

        private bool IsRelevantForRange(RefactoringSuggestion suggestion, SnapshotSpan range)
        {
            // Check if the suggestion is relevant for the current range
            try
            {
                var suggestionStart = suggestion.StartLine;
                var suggestionEnd = suggestion.EndLine;
                var rangeStartLine = range.Start.GetContainingLine().LineNumber;
                var rangeEndLine = range.End.GetContainingLine().LineNumber;

                return suggestionStart <= rangeEndLine && suggestionEnd >= rangeStartLine;
            }
            catch
            {
                return true; // Default to showing if we can't determine relevance
            }
        }

        private bool IsRelevantForRange(CodeIssue issue, SnapshotSpan range)
        {
            // Check if the issue is relevant for the current range
            try
            {
                var issueLineNumber = issue.Line;
                var rangeStartLine = range.Start.GetContainingLine().LineNumber;
                var rangeEndLine = range.End.GetContainingLine().LineNumber;

                return issueLineNumber >= rangeStartLine && issueLineNumber <= rangeEndLine;
            }
            catch
            {
                return true; // Default to showing if we can't determine relevance
            }
        }

        private string DetectLanguage(ITextBuffer textBuffer)
        {
            try
            {
                var contentType = textBuffer.ContentType;
                
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
                else
                    return "text";
            }
            catch
            {
                return "text";
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _apiClient = null;
                _configService = null;
            }
        }
    }

    // Refactoring action implementation
    internal class A3sistRefactoringAction : ISuggestedAction
    {
        private readonly RefactoringSuggestion _suggestion;
        private readonly ITextView _textView;
        private readonly ITextBuffer _textBuffer;
        private readonly IA3sistApiClient _apiClient;

        public string DisplayText => _suggestion.Title;
        public string IconAutomationText => "A3sist Refactoring";
        public string InputGestureText => null;
        public bool HasActionSets => false;

        public A3sistRefactoringAction(RefactoringSuggestion suggestion, ITextView textView, 
            ITextBuffer textBuffer, IA3sistApiClient apiClient)
        {
            _suggestion = suggestion;
            _textView = textView;
            _textBuffer = textBuffer;
            _apiClient = apiClient;
        }

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Enumerable.Empty<SuggestedActionSet>());
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                try
                {
                    var preview = await _apiClient.PreviewRefactoringAsync(_suggestion.Id, _textBuffer.CurrentSnapshot.GetText());
                    return (object)preview?.PreviewCode ?? _suggestion.Description;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Preview failed: {ex.Message}");
                    return (object)_suggestion.Description;
                }
            }, cancellationToken);
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                try
                {
                    var result = await _apiClient.ApplyRefactoringAsync(_suggestion.Id, _textBuffer.CurrentSnapshot.GetText());
                    
                    if (result.Success)
                    {
                        // Apply the changes to the text buffer
                        var snapshot = _textBuffer.CurrentSnapshot;
                        var edit = _textBuffer.CreateEdit();
                        edit.Replace(0, snapshot.Length, result.ModifiedCode);
                        edit.Apply();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"A3sist: Refactoring failed: {result.Error}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Refactoring invoke failed: {ex.Message}");
                }
            }, cancellationToken);
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        public void Dispose()
        {
            // No cleanup needed
        }
    }

    // Code fix action implementation
    internal class A3sistCodeFixAction : ISuggestedAction
    {
        private readonly CodeIssue _issue;
        private readonly ITextView _textView;
        private readonly ITextBuffer _textBuffer;
        private readonly IA3sistApiClient _apiClient;

        public string DisplayText => $"Fix: {_issue.Message}";
        public string IconAutomationText => "A3sist Code Fix";
        public string InputGestureText => null;
        public bool HasActionSets => false;

        public A3sistCodeFixAction(CodeIssue issue, ITextView textView, ITextBuffer textBuffer, IA3sistApiClient apiClient)
        {
            _issue = issue;
            _textView = textView;
            _textBuffer = textBuffer;
            _apiClient = apiClient;
        }

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Enumerable.Empty<SuggestedActionSet>());
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult((object)(_issue.SuggestedFixes?.FirstOrDefault() ?? _issue.Message));
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            // Apply the first suggested fix
            var fix = _issue.SuggestedFixes?.FirstOrDefault();
            if (!string.IsNullOrEmpty(fix))
            {
                try
                {
                    // This is a simplified implementation
                    // In a real scenario, you'd apply the specific fix to the specific location
                    var line = _textBuffer.CurrentSnapshot.GetLineFromLineNumber(_issue.Line);
                    var edit = _textBuffer.CreateEdit();
                    edit.Replace(line.Span, fix);
                    edit.Apply();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist: Code fix failed: {ex.Message}");
                }
            }
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        public void Dispose()
        {
            // No cleanup needed
        }
    }
}