using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using A3sist.UI.Services;
using System.Collections.Immutable;

namespace A3sist.UI.Editors
{
    /// <summary>
    /// Main editor integration service that coordinates all editor-related functionality
    /// </summary>
    public class EditorIntegrationService : IEditorIntegrationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EditorIntegrationService> _logger;
        private readonly CodeAnalysisProvider _codeAnalysisProvider;
        private readonly SuggestionProvider _suggestionProvider;
        private readonly Dictionary<string, ITextView> _activeTextViews;
        private readonly Dictionary<string, DateTime> _lastAnalysisTime;

        public EditorIntegrationService(
            IServiceProvider serviceProvider,
            ILogger<EditorIntegrationService> logger,
            CodeAnalysisProvider codeAnalysisProvider,
            SuggestionProvider suggestionProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _codeAnalysisProvider = codeAnalysisProvider ?? throw new ArgumentNullException(nameof(codeAnalysisProvider));
            _suggestionProvider = suggestionProvider ?? throw new ArgumentNullException(nameof(suggestionProvider));
            _activeTextViews = new Dictionary<string, ITextView>();
            _lastAnalysisTime = new Dictionary<string, DateTime>();
        }

        public async Task RefreshEditorView(object filePath)
        {
            var path = filePath?.ToString();
            if (string.IsNullOrEmpty(path))
                return;

            await RefreshEditorView(path);
        }

        public async Task RefreshEditorView(string path)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(EnvDTE.DTE));
                var document = dte.Documents.Item(path);

                if (document != null)
                {
                    document.Activate();
                    document.Save();
                    _logger.LogInformation("Refreshed editor view for file: {FilePath}", path);

                    // Trigger re-analysis
                    await TriggerCodeAnalysisAsync(path);
                }
                else
                {
                    _logger.LogWarning("File not found in editor: {FilePath}", path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing editor view for file: {FilePath}", path);
                throw;
            }
        }

        public async Task OpenFileInEditor(string path)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(EnvDTE.DTE));
                var window = dte.ItemOperations.OpenFile(path);

                if (window != null)
                {
                    _logger.LogInformation("Successfully opened file in editor: {FilePath}", path);

                    // Trigger initial analysis
                    await TriggerCodeAnalysisAsync(path);
                }
                else
                {
                    _logger.LogWarning("Failed to open file in editor: {FilePath}", path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening file in editor: {FilePath}", path);
                throw;
            }
        }

        public async Task ShowDiffView(string originalPath, string modifiedPath)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(EnvDTE.DTE));
                var diffTool = dte.Commands.Item("Tools.DiffFiles");

                if (diffTool != null)
                {
                    dte.ExecuteCommand("Tools.DiffFiles", $"{originalPath} {modifiedPath}");
                    _logger.LogInformation("Showing diff between {OriginalPath} and {ModifiedPath}", originalPath, modifiedPath);
                }
                else
                {
                    _logger.LogWarning("Diff tool not available in current environment");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing diff view for files: {OriginalPath} and {ModifiedPath}", originalPath, modifiedPath);
                throw;
            }
        }

        public async Task<List<CodeSuggestion>> GetSuggestionsForLocationAsync(string filePath, int lineNumber, int columnNumber)
        {
            try
            {
                _logger.LogDebug("Getting suggestions for location: {FilePath}:{LineNumber}:{ColumnNumber}", filePath, lineNumber, columnNumber);

                var suggestions = await _suggestionProvider.GetSuggestionsAsync(filePath, lineNumber);
                
                // Filter suggestions relevant to the specific column
                var relevantSuggestions = suggestions
                    .Where(s => IsRelevantToLocation(s, lineNumber, columnNumber))
                    .ToList();

                _logger.LogDebug("Found {Count} relevant suggestions for location", relevantSuggestions.Count);
                return relevantSuggestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting suggestions for location: {FilePath}:{LineNumber}:{ColumnNumber}", filePath, lineNumber, columnNumber);
                return new List<CodeSuggestion>();
            }
        }

        public async Task<CodeAnalysisResult> AnalyzeCodeAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("Analyzing code for file: {FilePath}", filePath);

                var content = await GetFileContentAsync(filePath);
                if (string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning("Could not retrieve content for file: {FilePath}", filePath);
                    return null;
                }

                var analysisResult = await _codeAnalysisProvider.AnalyzeCodeAsync(content, filePath);
                
                _logger.LogDebug("Code analysis completed for file: {FilePath}", filePath);
                return analysisResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing code for file: {FilePath}", filePath);
                return null;
            }
        }

        public async Task<bool> ApplySuggestionAsync(CodeSuggestion suggestion)
        {
            try
            {
                _logger.LogDebug("Applying suggestion: {SuggestionId}", suggestion.Id);

                var success = await _suggestionProvider.ApplySuggestionAsync(suggestion);
                
                if (success)
                {
                    // Refresh the editor view after applying the suggestion
                    await RefreshEditorView(suggestion.FilePath);
                    _logger.LogInformation("Successfully applied suggestion: {SuggestionId}", suggestion.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to apply suggestion: {SuggestionId}", suggestion.Id);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying suggestion: {SuggestionId}", suggestion.Id);
                return false;
            }
        }

        public async Task RegisterTextViewAsync(string filePath, ITextView textView)
        {
            try
            {
                _activeTextViews[filePath] = textView;
                
                // Subscribe to text view events
                textView.TextBuffer.Changed += (sender, args) => OnTextBufferChanged(filePath, args);
                textView.Caret.PositionChanged += (sender, args) => OnCaretPositionChanged(filePath, args);

                _logger.LogDebug("Registered text view for file: {FilePath}", filePath);

                // Trigger initial analysis
                await TriggerCodeAnalysisAsync(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering text view for file: {FilePath}", filePath);
            }
        }

        public void UnregisterTextView(string filePath)
        {
            try
            {
                if (_activeTextViews.ContainsKey(filePath))
                {
                    _activeTextViews.Remove(filePath);
                    _lastAnalysisTime.Remove(filePath);
                    
                    // Clear caches
                    _codeAnalysisProvider.ClearCache(filePath);
                    
                    _logger.LogDebug("Unregistered text view for file: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering text view for file: {FilePath}", filePath);
            }
        }

        public async Task ShowCodeSuggestionsAsync(string filePath, int lineNumber)
        {
            try
            {
                _logger.LogDebug("Showing code suggestions for: {FilePath}:{LineNumber}", filePath, lineNumber);

                var suggestions = await _suggestionProvider.GetSuggestionsAsync(filePath, lineNumber);
                
                if (suggestions.Any())
                {
                    // In a real implementation, this would show suggestions in the UI
                    // For now, we'll log the suggestions
                    _logger.LogInformation("Found {Count} suggestions for {FilePath}:{LineNumber}", suggestions.Count, filePath, lineNumber);
                    
                    foreach (var suggestion in suggestions.Take(5)) // Show top 5
                    {
                        _logger.LogInformation("Suggestion: {Title} - {Description}", suggestion.Title, suggestion.Description);
                    }
                }
                else
                {
                    _logger.LogDebug("No suggestions found for {FilePath}:{LineNumber}", filePath, lineNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing code suggestions for: {FilePath}:{LineNumber}", filePath, lineNumber);
            }
        }

        private async Task TriggerCodeAnalysisAsync(string filePath)
        {
            try
            {
                // Throttle analysis to avoid excessive calls
                if (_lastAnalysisTime.ContainsKey(filePath) && 
                    DateTime.UtcNow - _lastAnalysisTime[filePath] < TimeSpan.FromSeconds(5))
                {
                    return;
                }

                _lastAnalysisTime[filePath] = DateTime.UtcNow;

                // Trigger analysis in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await AnalyzeCodeAsync(filePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Background code analysis failed for file: {FilePath}", filePath);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering code analysis for file: {FilePath}", filePath);
            }
        }

        private async Task<string> GetFileContentAsync(string filePath)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Try to get content from active text view first
                if (_activeTextViews.ContainsKey(filePath))
                {
                    var textView = _activeTextViews[filePath];
                    return textView.TextBuffer.CurrentSnapshot.GetText();
                }

                // Try to get content from open document
                var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(EnvDTE.DTE));
                foreach (EnvDTE.Document doc in dte.Documents)
                {
                    if (string.Equals(doc.FullName, filePath, StringComparison.OrdinalIgnoreCase))
                    {
                        var textDocument = doc.Object() as EnvDTE.TextDocument;
                        if (textDocument != null)
                        {
                            var startPoint = textDocument.StartPoint;
                            var endPoint = textDocument.EndPoint;
                            return startPoint.CreateEditPoint().GetText(endPoint);
                        }
                    }
                }

                // Fallback to reading from file system
                if (System.IO.File.Exists(filePath))
                {
                    return await System.IO.File.ReadAllTextAsync(filePath);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file content for: {FilePath}", filePath);
                return null;
            }
        }

        private bool IsRelevantToLocation(CodeSuggestion suggestion, int lineNumber, int columnNumber)
        {
            // Check if the suggestion is relevant to the current location
            var lineDistance = Math.Abs(suggestion.StartLine - lineNumber);
            
            // Consider suggestions within 5 lines as relevant
            if (lineDistance <= 5)
                return true;

            // If on the same line, check column proximity
            if (suggestion.StartLine == lineNumber)
            {
                var columnDistance = Math.Abs(suggestion.StartColumn - columnNumber);
                return columnDistance <= 10;
            }

            return false;
        }

        private void OnTextBufferChanged(string filePath, TextContentChangedEventArgs args)
        {
            try
            {
                // Trigger analysis after text changes (with throttling)
                _ = Task.Delay(1000).ContinueWith(async _ =>
                {
                    await TriggerCodeAnalysisAsync(filePath);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling text buffer change for file: {FilePath}", filePath);
            }
        }

        private void OnCaretPositionChanged(string filePath, CaretPositionChangedEventArgs args)
        {
            try
            {
                var newPosition = args.NewPosition;
                var line = newPosition.BufferPosition.GetContainingLine();
                var lineNumber = line.LineNumber + 1;
                var columnNumber = newPosition.BufferPosition.Position - line.Start.Position + 1;

                // Trigger suggestions for the new location (with throttling)
                _ = Task.Delay(500).ContinueWith(async _ =>
                {
                    await ShowCodeSuggestionsAsync(filePath, lineNumber);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling caret position change for file: {FilePath}", filePath);
            }
        }

        public void Dispose()
        {
            try
            {
                // Cleanup active text views
                foreach (var kvp in _activeTextViews.ToList())
                {
                    UnregisterTextView(kvp.Key);
                }

                _activeTextViews.Clear();
                _lastAnalysisTime.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing EditorIntegrationService");
            }
        }
    }
}