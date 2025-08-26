using A3sist.UI.Models.Chat;
using A3sist.UI.Services.Chat;
using A3sist.UI.ViewModels.Chat;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace A3sist.UI.Components.Chat
{
    /// <summary>
    /// Interaction logic for SmartSuggestionsPanel.xaml
    /// </summary>
    public partial class SmartSuggestionsPanel : UserControl
    {
        public SmartSuggestionsPanelViewModel ViewModel { get; }

        public SmartSuggestionsPanel()
        {
            InitializeComponent();
            
            try
            {
                // Get services from service locator
                var serviceProvider = EditorServiceRegistration.ServiceLocator;
                var contextAnalyzerService = serviceProvider?.GetService<IContextAnalyzerService>();
                var contextService = serviceProvider?.GetService<IContextService>();
                var logger = serviceProvider?.GetService<ILogger<SmartSuggestionsPanelViewModel>>();
                
                ViewModel = new SmartSuggestionsPanelViewModel(
                    contextAnalyzerService,
                    contextService,
                    logger);
                
                DataContext = ViewModel;
                
                Loaded += OnLoaded;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing SmartSuggestionsPanel: {ex.Message}");
                // Create a fallback ViewModel
                ViewModel = new SmartSuggestionsPanelViewModel(null, null, null);
                DataContext = ViewModel;
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.RefreshAsync();
        }

        /// <summary>
        /// Event fired when a suggestion is applied
        /// </summary>
        public event EventHandler<string> SuggestionApplied;

        /// <summary>
        /// Event fired when a quick action is executed
        /// </summary>
        public event EventHandler<QuickAction> QuickActionExecuted;

        /// <summary>
        /// Refreshes the suggestions
        /// </summary>
        public async Task RefreshSuggestionsAsync()
        {
            await ViewModel.RefreshAsync();
        }

        /// <summary>
        /// Updates the context for suggestions
        /// </summary>
        public async Task UpdateContextAsync(ChatContext context)
        {
            await ViewModel.UpdateContextAsync(context);
        }

        private void OnSuggestionApplied(string suggestion)
        {
            SuggestionApplied?.Invoke(this, suggestion);
        }

        private void OnQuickActionExecuted(QuickAction action)
        {
            QuickActionExecuted?.Invoke(this, action);
        }
    }

    /// <summary>
    /// ViewModel for the SmartSuggestionsPanel
    /// </summary>
    public class SmartSuggestionsPanelViewModel : INotifyPropertyChanged
    {
        private readonly IContextAnalyzerService _contextAnalyzerService;
        private readonly IContextService _contextService;
        private readonly ILogger<SmartSuggestionsPanelViewModel> _logger;

        private ChatContext _currentContext = new();
        private bool _isLoading;

        public ObservableCollection<QuickAction> QuickActions { get; } = new();
        public ObservableCollection<string> ContextualSuggestions { get; } = new();

        private string _currentFileInfo = string.Empty;
        private string _selectionInfo = string.Empty;
        private string _errorInfo = string.Empty;

        public string CurrentFileInfo
        {
            get => _currentFileInfo;
            set => SetProperty(ref _currentFileInfo, value);
        }

        public string SelectionInfo
        {
            get => _selectionInfo;
            set => SetProperty(ref _selectionInfo, value);
        }

        public string ErrorInfo
        {
            get => _errorInfo;
            set => SetProperty(ref _errorInfo, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool HasQuickActions => QuickActions.Count > 0;
        public bool HasSuggestions => ContextualSuggestions.Count > 0;
        public bool HasContextInfo => !string.IsNullOrEmpty(CurrentFileInfo) || !string.IsNullOrEmpty(SelectionInfo) || !string.IsNullOrEmpty(ErrorInfo);
        public bool IsEmpty => !HasQuickActions && !HasSuggestions && !HasContextInfo;

        // Commands
        public ICommand RefreshSuggestionsCommand { get; }
        public ICommand ExecuteQuickActionCommand { get; }
        public ICommand ApplySuggestionCommand { get; }

        // Events
        public event EventHandler<string> SuggestionApplied;
        public event EventHandler<QuickAction> QuickActionExecuted;
        public event PropertyChangedEventHandler PropertyChanged;

        public SmartSuggestionsPanelViewModel(
            IContextAnalyzerService contextAnalyzerService,
            IContextService contextService,
            ILogger<SmartSuggestionsPanelViewModel> logger)
        {
            _contextAnalyzerService = contextAnalyzerService;
            _contextService = contextService;
            _logger = logger;

            RefreshSuggestionsCommand = new RelayCommand(async () => await RefreshAsync());
            ExecuteQuickActionCommand = new RelayCommand<QuickAction>(ExecuteQuickAction);
            ApplySuggestionCommand = new RelayCommand<string>(ApplySuggestion);
        }

        /// <summary>
        /// Refreshes all suggestions and context information
        /// </summary>
        public async Task RefreshAsync()
        {
            if (_contextService == null || _contextAnalyzerService == null)
            {
                _logger?.LogWarning("Services not available, cannot refresh suggestions");
                return;
            }

            try
            {
                IsLoading = true;

                // Get current context
                _currentContext = await _contextService.GetCurrentContextAsync();

                // Update context info
                await UpdateContextInfoAsync();

                // Get quick actions
                var quickActions = await _contextAnalyzerService.GetQuickActionsAsync(_currentContext);
                
                QuickActions.Clear();
                foreach (var action in quickActions.Take(5)) // Limit to 5 quick actions
                {
                    QuickActions.Add(action);
                }

                // Get contextual suggestions
                var suggestions = await _contextAnalyzerService.GetContextualSuggestionsAsync(_currentContext);
                
                ContextualSuggestions.Clear();
                foreach (var suggestion in suggestions.Take(8)) // Limit to 8 suggestions
                {
                    ContextualSuggestions.Add(suggestion);
                }

                // Notify property changes
                OnPropertyChanged(nameof(HasQuickActions));
                OnPropertyChanged(nameof(HasSuggestions));
                OnPropertyChanged(nameof(HasContextInfo));
                OnPropertyChanged(nameof(IsEmpty));

                _logger?.LogDebug("Refreshed suggestions: {QuickActions} quick actions, {Suggestions} suggestions", 
                    QuickActions.Count, ContextualSuggestions.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error refreshing suggestions");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Updates the context and refreshes suggestions
        /// </summary>
        public async Task UpdateContextAsync(ChatContext context)
        {
            _currentContext = context;
            await UpdateContextInfoAsync();
            await RefreshAsync();
        }

        /// <summary>
        /// Updates context information display
        /// </summary>
        private async Task UpdateContextInfoAsync()
        {
            try
            {
                // Update file info
                if (!string.IsNullOrEmpty(_currentContext.CurrentFile))
                {
                    var fileName = System.IO.Path.GetFileName(_currentContext.CurrentFile);
                    var extension = System.IO.Path.GetExtension(_currentContext.CurrentFile);
                    CurrentFileInfo = $"📄 {fileName} ({extension})";
                }
                else
                {
                    CurrentFileInfo = string.Empty;
                }

                // Update selection info
                if (!string.IsNullOrEmpty(_currentContext.SelectedText))
                {
                    var lines = _currentContext.SelectedText.Split('\n').Length;
                    var chars = _currentContext.SelectedText.Length;
                    SelectionInfo = $"🔍 Selected: {lines} lines, {chars} characters";
                }
                else
                {
                    SelectionInfo = string.Empty;
                }

                // Update error info
                if (_currentContext.Errors.Any())
                {
                    var errorCount = _currentContext.Errors.Count;
                    var warningCount = _currentContext.Errors.Count(e => e.Severity?.ToLowerInvariant() == "warning");
                    ErrorInfo = $"🚨 {errorCount} errors, {warningCount} warnings";
                }
                else
                {
                    ErrorInfo = string.Empty;
                }

                OnPropertyChanged(nameof(HasContextInfo));
                OnPropertyChanged(nameof(IsEmpty));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating context info");
            }
        }

        /// <summary>
        /// Executes a quick action
        /// </summary>
        private void ExecuteQuickAction(QuickAction action)
        {
            if (action == null) return;

            try
            {
                _logger?.LogInformation("Executing quick action: {ActionTitle}", action.Title);
                QuickActionExecuted?.Invoke(this, action);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing quick action: {ActionTitle}", action.Title);
            }
        }

        /// <summary>
        /// Applies a suggestion
        /// </summary>
        private void ApplySuggestion(string suggestion)
        {
            if (string.IsNullOrEmpty(suggestion)) return;

            try
            {
                _logger?.LogInformation("Applying suggestion: {Suggestion}", suggestion);
                SuggestionApplied?.Invoke(this, suggestion);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error applying suggestion: {Suggestion}", suggestion);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    /// <summary>
    /// Generic RelayCommand implementation
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }
    }
}