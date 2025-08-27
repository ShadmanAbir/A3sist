using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using A3sist.Models;
using A3sist.Services;

namespace A3sist.UI
{
    /// <summary>
    /// Interaction logic for RefactoringWindow.xaml
    /// </summary>
    public partial class RefactoringWindow : Window, INotifyPropertyChanged
    {
        private readonly IRefactoringService _refactoringService;
        private ObservableCollection<RefactoringSuggestion> _suggestions;
        private RefactoringSuggestion _selectedSuggestion;
        private RefactoringPreview _currentPreview;
        private Stack<string> _undoStack;
        private string _originalCode;

        public RefactoringWindow(
            IEnumerable<RefactoringSuggestion> suggestions, 
            IRefactoringService refactoringService)
        {
            InitializeComponent();
            
            _refactoringService = refactoringService ?? throw new ArgumentNullException(nameof(refactoringService));
            _suggestions = new ObservableCollection<RefactoringSuggestion>(suggestions ?? new List<RefactoringSuggestion>());
            _undoStack = new Stack<string>();

            if (_suggestions.Any())
            {
                _originalCode = _suggestions.First().OriginalCode;
            }

            DataContext = this;
            SuggestionsListBox.ItemsSource = _suggestions;
            
            Loaded += RefactoringWindow_Loaded;
        }

        public ObservableCollection<RefactoringSuggestion> Suggestions
        {
            get => _suggestions;
            set
            {
                _suggestions = value;
                OnPropertyChanged();
            }
        }

        public RefactoringSuggestion SelectedSuggestion
        {
            get => _selectedSuggestion;
            set
            {
                _selectedSuggestion = value;
                OnPropertyChanged();
                UpdateButtonStates();
                
                if (AutoPreviewCheckBox.IsChecked == true && value != null)
                {
                    _ = PreviewRefactoringAsync(value);
                }
            }
        }

        private void RefactoringWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateStatus($"Found {_suggestions.Count} refactoring suggestions");
            
            if (_suggestions.Any())
            {
                SuggestionsListBox.SelectedIndex = 0;
            }
        }

        private void SuggestionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedSuggestion = SuggestionsListBox.SelectedItem as RefactoringSuggestion;
        }

        private async void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSuggestion != null)
            {
                await PreviewRefactoringAsync(SelectedSuggestion);
            }
        }

        private async Task PreviewRefactoringAsync(RefactoringSuggestion suggestion)
        {
            try
            {
                UpdateStatus("Generating preview...");
                
                _currentPreview = await _refactoringService.PreviewRefactoringAsync(suggestion);
                
                if (_currentPreview != null)
                {
                    // Update diff view
                    OriginalCodeTextBox.Text = _currentPreview.OriginalCode;
                    RefactoredCodeTextBox.Text = _currentPreview.PreviewCode;
                    
                    // Update change summary
                    UpdateChangeSummary(_currentPreview.Changes);
                    
                    // Update AI analysis
                    UpdateAIAnalysis(suggestion);
                    
                    UpdateStatus("Preview generated successfully");
                }
                else
                {
                    UpdateStatus("Failed to generate preview");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error generating preview: {ex.Message}");
                MessageBox.Show($"Error generating preview: {ex.Message}", "Preview Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSuggestion == null)
                return;

            var result = MessageBox.Show(
                $"Are you sure you want to apply this refactoring?\n\n{SelectedSuggestion.Title}\n{SelectedSuggestion.Description}",
                "Apply Refactoring",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await ApplyRefactoringAsync(SelectedSuggestion);
            }
        }

        private async void ApplyAllButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to apply all {_suggestions.Count} refactoring suggestions?",
                "Apply All Refactorings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await ApplyAllRefactoringsAsync();
            }
        }

        private async Task ApplyRefactoringAsync(RefactoringSuggestion suggestion)
        {
            try
            {
                UpdateStatus("Applying refactoring...");
                
                // Save current state for undo
                if (!string.IsNullOrEmpty(_originalCode))
                {
                    _undoStack.Push(_originalCode);
                }
                
                var result = await _refactoringService.ApplyRefactoringAsync(suggestion);
                
                if (result.Success)
                {
                    // Update the original code with the refactored version
                    _originalCode = result.ModifiedCode;
                    
                    // Remove the applied suggestion from the list
                    _suggestions.Remove(suggestion);
                    
                    // Update UI
                    if (_suggestions.Any())
                    {
                        SuggestionsListBox.SelectedIndex = 0;
                    }
                    else
                    {
                        ClearPreview();
                    }
                    
                    UndoButton.IsEnabled = _undoStack.Count > 0;
                    UpdateStatus($"Refactoring applied successfully. {_suggestions.Count} suggestions remaining.");
                }
                else
                {
                    UpdateStatus($"Failed to apply refactoring: {result.Error}");
                    MessageBox.Show($"Failed to apply refactoring: {result.Error}", "Apply Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error applying refactoring: {ex.Message}");
                MessageBox.Show($"Error applying refactoring: {ex.Message}", "Apply Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ApplyAllRefactoringsAsync()
        {
            try
            {
                var totalSuggestions = _suggestions.Count;
                var appliedCount = 0;
                var failedCount = 0;
                
                UpdateStatus($"Applying {totalSuggestions} refactorings...");
                
                // Save current state for undo
                if (!string.IsNullOrEmpty(_originalCode))
                {
                    _undoStack.Push(_originalCode);
                }
                
                var suggestionsToApply = _suggestions.ToList();
                
                foreach (var suggestion in suggestionsToApply)
                {
                    try
                    {
                        var result = await _refactoringService.ApplyRefactoringAsync(suggestion);
                        
                        if (result.Success)
                        {
                            _originalCode = result.ModifiedCode;
                            _suggestions.Remove(suggestion);
                            appliedCount++;
                        }
                        else
                        {
                            failedCount++;
                        }
                    }
                    catch
                    {
                        failedCount++;
                    }
                    
                    UpdateStatus($"Applied {appliedCount} of {totalSuggestions} refactorings...");
                }
                
                UndoButton.IsEnabled = _undoStack.Count > 0;
                ClearPreview();
                
                var message = $"Applied {appliedCount} refactorings successfully.";
                if (failedCount > 0)
                {
                    message += $" {failedCount} failed.";
                }
                
                UpdateStatus(message);
                MessageBox.Show(message, "Apply All Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error applying refactorings: {ex.Message}");
                MessageBox.Show($"Error applying refactorings: {ex.Message}", "Apply All Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("Refreshing suggestions...");
                
                // This would typically re-analyze the current code
                // For now, just update the status
                UpdateStatus($"{_suggestions.Count} suggestions available");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error refreshing: {ex.Message}");
            }
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_undoStack.Count > 0)
            {
                var previousCode = _undoStack.Pop();
                _originalCode = previousCode;
                
                // Update preview if there's a current selection
                if (SelectedSuggestion != null)
                {
                    OriginalCodeTextBox.Text = previousCode;
                }
                
                UndoButton.IsEnabled = _undoStack.Count > 0;
                UpdateStatus("Undid last refactoring");
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|Diff files (*.diff)|*.diff|All files (*.*)|*.*",
                    Title = "Export Refactoring Diff",
                    DefaultExt = "txt"
                };

                if (dialog.ShowDialog() == true)
                {
                    var diffContent = GenerateDiffContent();
                    System.IO.File.WriteAllText(dialog.FileName, diffContent);
                    UpdateStatus($"Diff exported to {dialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting diff: {ex.Message}", "Export Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateDiffContent()
        {
            var content = new System.Text.StringBuilder();
            
            content.AppendLine("A3sist Refactoring Diff Report");
            content.AppendLine("Generated on: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            content.AppendLine(new string('=', 50));
            content.AppendLine();
            
            if (SelectedSuggestion != null)
            {
                content.AppendLine($"Suggestion: {SelectedSuggestion.Title}");
                content.AppendLine($"Description: {SelectedSuggestion.Description}");
                content.AppendLine($"Type: {SelectedSuggestion.Type}");
                content.AppendLine($"Priority: {SelectedSuggestion.Priority}");
                content.AppendLine();
                
                content.AppendLine("ORIGINAL CODE:");
                content.AppendLine(new string('-', 20));
                content.AppendLine(OriginalCodeTextBox.Text);
                content.AppendLine();
                
                content.AppendLine("REFACTORED CODE:");
                content.AppendLine(new string('-', 20));
                content.AppendLine(RefactoredCodeTextBox.Text);
            }
            else
            {
                content.AppendLine("No suggestion selected");
            }
            
            return content.ToString();
        }

        private void UpdateChangeSummary(List<CodeChange> changes)
        {
            ChangeSummaryPanel.Children.Clear();
            NoChangesTextBlock.Visibility = Visibility.Collapsed;
            
            if (changes == null || !changes.Any())
            {
                NoChangesTextBlock.Visibility = Visibility.Visible;
                return;
            }
            
            foreach (var change in changes)
            {
                var changePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                
                var typeIcon = new TextBlock
                {
                    Text = GetChangeTypeIcon(change.Type),
                    FontFamily = new FontFamily("Segoe UI Symbol"),
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 5, 0),
                    Foreground = GetChangeTypeColor(change.Type)
                };
                
                var description = new TextBlock
                {
                    Text = $"Lines {change.StartLine}-{change.EndLine}: {GetChangeTypeDescription(change.Type)}",
                    FontSize = 11
                };
                
                changePanel.Children.Add(typeIcon);
                changePanel.Children.Add(description);
                ChangeSummaryPanel.Children.Add(changePanel);
            }
        }

        private void UpdateAIAnalysis(RefactoringSuggestion suggestion)
        {
            var analysis = new System.Text.StringBuilder();
            
            analysis.AppendLine($"Refactoring Type: {suggestion.Type}");
            analysis.AppendLine($"Priority Level: {suggestion.Priority} (1=High, 3=Low)");
            analysis.AppendLine();
            analysis.AppendLine("Description:");
            analysis.AppendLine(suggestion.Description);
            analysis.AppendLine();
            analysis.AppendLine("Benefits:");
            
            switch (suggestion.Type)
            {
                case RefactoringType.ExtractMethod:
                    analysis.AppendLine("• Improves code readability and reusability");
                    analysis.AppendLine("• Reduces code duplication");
                    analysis.AppendLine("• Makes testing easier");
                    break;
                case RefactoringType.ExtractVariable:
                    analysis.AppendLine("• Improves code readability");
                    analysis.AppendLine("• Makes complex expressions easier to understand");
                    analysis.AppendLine("• Facilitates debugging");
                    break;
                case RefactoringType.RenameSymbol:
                    analysis.AppendLine("• Improves code clarity and self-documentation");
                    analysis.AppendLine("• Makes code more maintainable");
                    break;
                case RefactoringType.OptimizeUsings:
                    analysis.AppendLine("• Reduces compilation time");
                    analysis.AppendLine("• Improves code organization");
                    analysis.AppendLine("• Removes unnecessary dependencies");
                    break;
                default:
                    analysis.AppendLine("• Improves code quality and maintainability");
                    break;
            }
            
            AIAnalysisTextBlock.Text = analysis.ToString();
        }

        private string GetChangeTypeIcon(ChangeType type)
        {
            switch (type)
            {
                case ChangeType.Addition:
                    return "➕";
                case ChangeType.Deletion:
                    return "➖";
                case ChangeType.Modification:
                    return "✏️";
                default:
                    return "•";
            }
        }

        private Brush GetChangeTypeColor(ChangeType type)
        {
            switch (type)
            {
                case ChangeType.Addition:
                    return Brushes.Green;
                case ChangeType.Deletion:
                    return Brushes.Red;
                case ChangeType.Modification:
                    return Brushes.Blue;
                default:
                    return Brushes.Gray;
            }
        }

        private string GetChangeTypeDescription(ChangeType type)
        {
            switch (type)
            {
                case ChangeType.Addition:
                    return "Code added";
                case ChangeType.Deletion:
                    return "Code removed";
                case ChangeType.Modification:
                    return "Code modified";
                default:
                    return "Code changed";
            }
        }

        private void ClearPreview()
        {
            OriginalCodeTextBox.Clear();
            RefactoredCodeTextBox.Clear();
            ChangeSummaryPanel.Children.Clear();
            NoChangesTextBlock.Visibility = Visibility.Visible;
            AIAnalysisTextBlock.Text = "No suggestion selected";
        }

        private void UpdateButtonStates()
        {
            var hasSelection = SelectedSuggestion != null;
            PreviewButton.IsEnabled = hasSelection;
            ApplyButton.IsEnabled = hasSelection;
            ApplyAllButton.IsEnabled = _suggestions.Any();
        }

        private void UpdateStatus(string message)
        {
            StatusTextBlock.Text = message;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}