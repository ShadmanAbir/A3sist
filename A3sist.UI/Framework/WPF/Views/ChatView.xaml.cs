#if NET9_0_OR_GREATER

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using A3sist.UI.Shared.Interfaces;
using A3sist.UI.Shared;
using A3sist.Core.Services;

namespace A3sist.UI.Framework.WPF.Views
{
    /// <summary>
    /// Interaction logic for ChatView.xaml - WPF implementation with RAG features
    /// </summary>
    public partial class ChatView : UserControl, INotifyPropertyChanged
    {
        private readonly ChatViewModel _viewModel;
        private readonly IUIService _uiService;
        private bool _isTyping;
        private string _currentMessage = string.Empty;
        
        public ObservableCollection<ChatMessageViewModel> Messages { get; }
        public event PropertyChangedEventHandler? PropertyChanged;

        public ChatView()
        {
            InitializeComponent();
            
            Messages = new ObservableCollection<ChatMessageViewModel>();
            
            // Initialize with dependency injection when available
            // For design-time, create stub services
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                _viewModel = CreateDesignTimeViewModel();
                _uiService = new DesignTimeUIService();
            }
            else
            {
                // In production, these would be injected
                // _viewModel = ServiceLocator.GetService<ChatViewModel>();
                // _uiService = ServiceLocator.GetService<IUIService>();
                _viewModel = CreateDesignTimeViewModel(); // Placeholder
                _uiService = new DesignTimeUIService(); // Placeholder
            }
            
            DataContext = this;
            
            // Subscribe to view model events
            if (_viewModel != null)
            {
                // _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
            
            // Add welcome message
            AddWelcomeMessage();
        }

        #region Properties

        public bool IsTyping
        {
            get => _isTyping;
            set
            {
                _isTyping = value;
                OnPropertyChanged();
            }
        }

        public string CurrentMessage
        {
            get => _currentMessage;
            set
            {
                _currentMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSendMessage));
            }
        }

        public bool IsInputEnabled => !IsTyping;

        public bool CanSendMessage => !string.IsNullOrWhiteSpace(CurrentMessage) && !IsTyping;

        public bool HasContext => HasSelectedCode || HasFileContext;

        public bool HasSelectedCode => false; // Would be implemented with actual context

        public bool HasFileContext => false; // Would be implemented with actual context

        public string CurrentFileName => "example.cs"; // Would be actual file name

        #endregion

        #region Event Handlers

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        private async void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    await SendMessageAsync();
                    e.Handled = true;
                }
                else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    // Allow new line
                }
                else
                {
                    await SendMessageAsync();
                    e.Handled = true;
                }
            }
        }

        private async void KnowledgeMode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.ShowKnowledgeModeAsync();
            }
            catch (Exception ex)
            {
                await _uiService.ShowNotificationAsync("Error", $"Failed to open knowledge mode: {ex.Message}");
            }
        }

        private async void ClearChat_Click(object sender, RoutedEventArgs e)
        {
            var confirmed = await _uiService.ShowConfirmationAsync(
                "Clear Chat", 
                "Are you sure you want to clear the chat history?");
            
            if (confirmed)
            {
                Messages.Clear();
                AddWelcomeMessage();
            }
        }

        private async void Settings_Click(object sender, RoutedEventArgs e)
        {
            // Open settings dialog
            await _uiService.ShowNotificationAsync("Settings", "Settings dialog would open here");
        }

        private async void AttachFile_Click(object sender, RoutedEventArgs e)
        {
            // Attach file context
            await _uiService.ShowNotificationAsync("File Context", "File context attachment not yet implemented");
        }

        private async void AttachSelection_Click(object sender, RoutedEventArgs e)
        {
            // Attach selected code
            var selectedCode = await _uiService.GetSelectedCodeAsync();
            if (!string.IsNullOrEmpty(selectedCode))
            {
                CurrentMessage += $"\n\n```\n{selectedCode}\n```";
            }
        }

        #endregion

        #region Private Methods

        private async Task SendMessageAsync()
        {
            if (!CanSendMessage) return;

            var message = CurrentMessage.Trim();
            CurrentMessage = string.Empty;

            // Add user message
            var userMessage = new ChatMessageViewModel
            {
                Content = message,
                IsFromUser = true,
                Timestamp = DateTime.Now,
                MessageStyle = Application.Current.FindResource("UserMessageStyle") as Style,
                TextColor = System.Windows.Media.Brushes.White
            };
            
            Messages.Add(userMessage);
            ScrollToBottom();

            // Show typing indicator
            IsTyping = true;

            try
            {
                // Send message through view model
                await _viewModel.SendMessageAsync(message);
                
                // For demo purposes, add a mock response
                await Task.Delay(1500); // Simulate processing time
                
                var responseMessage = new ChatMessageViewModel
                {
                    Content = GenerateMockResponse(message),
                    IsFromUser = false,
                    Timestamp = DateTime.Now,
                    MessageStyle = Application.Current.FindResource("RAGMessageStyle") as Style,
                    TextColor = System.Windows.Media.Brushes.Black,
                    HasRAGContext = true,
                    KnowledgeSourceCount = "3 sources",
                    Citations = GenerateMockCitations()
                };
                
                Messages.Add(responseMessage);
                ScrollToBottom();
            }
            catch (Exception ex)
            {
                var errorMessage = new ChatMessageViewModel
                {
                    Content = $"Error: {ex.Message}",
                    IsFromUser = false,
                    Timestamp = DateTime.Now,
                    MessageStyle = Application.Current.FindResource("ErrorMessageStyle") as Style,
                    TextColor = System.Windows.Media.Brushes.DarkRed
                };
                
                Messages.Add(errorMessage);
                ScrollToBottom();
            }
            finally
            {
                IsTyping = false;
            }
        }

        private void AddWelcomeMessage()
        {
            var welcomeMessage = new ChatMessageViewModel
            {
                Content = "Welcome to A3sist! I'm your AI-powered coding assistant with knowledge-enhanced capabilities. How can I help you today?",
                IsFromUser = false,
                Timestamp = DateTime.Now,
                MessageStyle = Application.Current.FindResource("AssistantMessageStyle") as Style,
                TextColor = System.Windows.Media.Brushes.Black
            };
            
            Messages.Add(welcomeMessage);
        }

        private string GenerateMockResponse(string userMessage)
        {
            // Mock response generator for demonstration
            var responses = new[]
            {
                "I can help you analyze your C# code using Roslyn and provide suggestions based on best practices from my knowledge base.",
                "Based on the knowledge sources, here are some recommendations for improving your code structure and performance.",
                "I've found relevant documentation and examples that can help with your request. Let me provide a comprehensive solution.",
                "Using RAG-enhanced analysis, I can offer you context-aware suggestions that incorporate current best practices."
            };
            
            var random = new Random();
            return responses[random.Next(responses.Length)];
        }

        private ObservableCollection<CitationViewModel> GenerateMockCitations()
        {
            return new ObservableCollection<CitationViewModel>
            {
                new CitationViewModel
                {
                    Title = "C# Coding Conventions",
                    Source = "Microsoft Docs",
                    Relevance = 0.95f,
                    Description = "Official Microsoft guidelines for C# coding standards"
                },
                new CitationViewModel
                {
                    Title = "SOLID Principles in C#",
                    Source = "Best Practices Knowledge Base",
                    Relevance = 0.87f,
                    Description = "Comprehensive guide to SOLID principles implementation"
                },
                new CitationViewModel
                {
                    Title = "Async/Await Best Practices",
                    Source = "Code Examples Repository",
                    Relevance = 0.82f,
                    Description = "Common patterns and anti-patterns in asynchronous programming"
                }
            };
        }

        private void ScrollToBottom()
        {
            ChatScrollViewer.ScrollToEnd();
        }

        private ChatViewModel CreateDesignTimeViewModel()
        {
            // Create design-time view model with mock services
            return new ChatViewModel(
                new DesignTimeChatService(),
                new DesignTimeRAGUIService(),
                new DesignTimeLogger<ChatViewModel>());
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// View model for individual chat messages
    /// </summary>
    public class ChatMessageViewModel : INotifyPropertyChanged
    {
        public string Content { get; set; } = string.Empty;
        public bool IsFromUser { get; set; }
        public DateTime Timestamp { get; set; }
        public Style? MessageStyle { get; set; }
        public System.Windows.Media.Brush? TextColor { get; set; }
        public bool HasRAGContext { get; set; }
        public string KnowledgeSourceCount { get; set; } = string.Empty;
        public bool HasCitations => Citations?.Any() == true;
        public ObservableCollection<CitationViewModel>? Citations { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    /// <summary>
    /// View model for citations
    /// </summary>
    public class CitationViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public float Relevance { get; set; }
        public string? Description { get; set; }
    }

    #region Design-Time Services

    internal class DesignTimeUIService : IUIService
    {
        public Task ShowChatViewAsync() => Task.CompletedTask;
        public Task ShowAgentStatusAsync() => Task.CompletedTask;
        public Task<string?> GetSelectedCodeAsync() => Task.FromResult<string?>(null);
        public Task ShowNotificationAsync(string title, string message) => Task.CompletedTask;
        public Task<bool> ShowConfirmationAsync(string title, string message) => Task.FromResult(true);
    }

    internal class DesignTimeChatService : IChatService
    {
        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
        public Task<string> SendMessageAsync(string message) => Task.FromResult("Mock response");
        public Task<ChatMessage[]> GetHistoryAsync() => Task.FromResult(Array.Empty<ChatMessage>());
        public Task ClearHistoryAsync() => Task.CompletedTask;
        public Task StartNewConversationAsync() => Task.CompletedTask;
    }

    internal class DesignTimeRAGUIService : IRAGUIService
    {
        public Task ShowKnowledgeModeAsync() => Task.CompletedTask;
        public Task<RAGContext?> GetCurrentRAGContextAsync() => Task.FromResult<RAGContext?>(null);
        public Task ShowCitationsAsync(Citation[] citations) => Task.CompletedTask;
        public Task UpdateKnowledgeStatusAsync(string status) => Task.CompletedTask;
    }

    internal class DesignTimeLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => new DesignTimeDisposable();
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    internal class DesignTimeDisposable : IDisposable
    {
        public void Dispose() { }
    }

    #endregion
}

#endif