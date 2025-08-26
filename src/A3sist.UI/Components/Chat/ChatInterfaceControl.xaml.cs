using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using A3sist.UI.Models.Chat;
using A3sist.UI.Services.Chat;
using A3sist.UI.Services;
using A3sist.UI.ViewModels.Chat;

namespace A3sist.UI.Components.Chat
{
    /// <summary>
    /// Interaction logic for ChatInterfaceControl.xaml
    /// </summary>
    public partial class ChatInterfaceControl : UserControl, IDisposable
    {
        public ChatInterfaceViewModel ViewModel { get; }
        private bool _disposed;

        public ChatInterfaceControl()
        {
            InitializeComponent();
            
            // Get ViewModel from service locator or create with mock services for design time
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                // Design time - create with mock data
                ViewModel = CreateDesignTimeViewModel();
            }
            else
            {
                // Runtime - get from service locator
                try
                {
                    ViewModel = EditorServiceRegistration.ServiceLocator?.GetService<ChatInterfaceViewModel>
                        ?? throw new InvalidOperationException("ChatInterfaceViewModel not registered");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error getting ChatInterfaceViewModel: {ex.Message}");
                    ViewModel = CreateDesignTimeViewModel(); // Fallback
                }
            }
            
            DataContext = ViewModel;
            
            // Subscribe to events
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;
                ViewModel.ScrollToBottomRequested += ViewModel_ScrollToBottomRequested;
            }
            
            Loaded += ChatInterfaceControl_Loaded;
        }

        /// <summary>
        /// Creates a design-time ViewModel for XAML designer
        /// </summary>
        private ChatInterfaceViewModel CreateDesignTimeViewModel()
        {
            // Mock services for design time
            var mockChatService = new MockChatService();
            var mockContextService = new MockContextService();
            var mockSettingsService = new MockChatSettingsService();
            var mockLogger = new MockLogger();
            
            return new ChatInterfaceViewModel(mockChatService, mockContextService, mockSettingsService, mockLogger);
        }

        private void ChatInterfaceControl_Loaded(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            try
            {
                // Initialize the view model
                ViewModel?.Initialize();
                
                // Focus on the message input
                MessageTextBox?.Focus();
                
                // Subscribe to suggestions panel events
                if (SmartSuggestionsPanel != null)
                {
                    SmartSuggestionsPanel.SuggestionApplied += OnSuggestionApplied;
                    SmartSuggestionsPanel.QuickActionExecuted += OnQuickActionExecuted;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ChatInterfaceControl_Loaded: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles when a suggestion is applied from the suggestions panel
        /// </summary>
        private void OnSuggestionApplied(object sender, string suggestion)
        {
            try
            {
                if (ViewModel != null)
                {
                    ViewModel.CurrentMessage = suggestion;
                    MessageTextBox?.Focus();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying suggestion: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles when a quick action is executed from the suggestions panel
        /// </summary>
        private void OnQuickActionExecuted(object sender, QuickAction action)
        {
            try
            {
                if (ViewModel != null)
                {
                    ViewModel.CurrentMessage = action.Command;
                    // Auto-send for quick actions
                    if (ViewModel.SendMessageCommand.CanExecute(null))
                    {
                        ViewModel.SendMessageCommand.Execute(null);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error executing quick action: {ex.Message}");
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChatInterfaceViewModel.Messages))
            {
                // Scroll to bottom when new messages are added
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    ChatScrollViewer.ScrollToBottom();
                }));
            }
        }

        private void ViewModel_ScrollToBottomRequested(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                ChatScrollViewer.ScrollToBottom();
            }));
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            
            // Handle Ctrl+Enter to send message
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (ViewModel.SendMessageCommand.CanExecute(null))
                {
                    ViewModel.SendMessageCommand.Execute(null);
                    e.Handled = true;
                }
            }
            // Handle Enter without Ctrl for new line (default behavior)
            else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                // Allow default behavior (new line)
                return;
            }
            // Handle Escape to clear input
            else if (e.Key == Key.Escape)
            {
                textBox.Clear();
                e.Handled = true;
            }
            // Handle Ctrl+K to focus on input and clear
            else if (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.Control)
            {
                textBox.Clear();
                textBox.Focus();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Scrolls the chat messages to the bottom
        /// </summary>
        public void ScrollToBottom()
        {
            try
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    ChatScrollViewer?.ScrollToBottom();
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error scrolling to bottom: {ex.Message}");
            }
        }

        /// <summary>
        /// Focuses the message input textbox
        /// </summary>
        public void FocusInput()
        {
            try
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    MessageTextBox?.Focus();
                    MessageTextBox?.CaretIndex = MessageTextBox?.Text?.Length ?? 0;
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error focusing input: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles cleanup when the control is unloaded
        /// </summary>
        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (ViewModel != null)
                {
                    ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                    ViewModel.ScrollToBottomRequested -= ViewModel_ScrollToBottomRequested;
                    ViewModel.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    // Mock services for design-time support
    internal class MockChatService : IChatService
    {
        public event EventHandler<StreamingResponseChunk>? StreamingResponse;
        public event EventHandler<string>? ConversationUpdated;

        public Task<ChatConversation> CreateConversationAsync(string? title = null)
        {
            return Task.FromResult(new ChatConversation { Title = title ?? "Mock Conversation" });
        }

        public Task<bool> DeleteConversationAsync(string conversationId) => Task.FromResult(true);

        public Task<ChatConversation?> GetConversationAsync(string conversationId)
        {
            return Task.FromResult<ChatConversation?>(new ChatConversation());
        }

        public Task<IEnumerable<ChatConversation>> GetConversationsAsync()
        {
            return Task.FromResult<IEnumerable<ChatConversation>>(new List<ChatConversation>());
        }

        public Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SendMessageResponse { Success = true });
        }

        public Task<bool> UpdateConversationAsync(ChatConversation conversation) => Task.FromResult(true);
    }

    internal class MockContextService : IContextService
    {
        public Task<ChatContext> GetCurrentContextAsync() => Task.FromResult(new ChatContext());
        public Task<string> GetCurrentFilePathAsync() => Task.FromResult(string.Empty);
        public Task<string> GetCurrentProjectPathAsync() => Task.FromResult(string.Empty);
        public Task<List<CompilerError>> GetCurrentErrorsAsync() => Task.FromResult(new List<CompilerError>());
        public Task<string> GetSelectedTextAsync() => Task.FromResult(string.Empty);
    }

    internal class MockLogger : ILogger<ChatInterfaceViewModel>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    internal class MockChatSettingsService : IChatSettingsService
    {
        public event EventHandler<ChatSettings>? SettingsChanged;

        public ChatSettings GetSettings()
        {
            return new ChatSettings
            {
                DefaultModel = "gpt-4",
                ShowSuggestions = true,
                EnableStreaming = true
            };
        }

        public Task SaveSettingsAsync(ChatSettings settings) => Task.CompletedTask;
        public Task ResetToDefaultsAsync() => Task.CompletedTask;
    }
}