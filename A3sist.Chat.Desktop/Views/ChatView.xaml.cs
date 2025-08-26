using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace A3sist.Chat.Desktop.Views
{
    /// <summary>
    /// Simplified ChatView for standalone desktop application
    /// </summary>
    public partial class ChatView : UserControl, INotifyPropertyChanged
    {
        private readonly IChatService? _chatService;
        private readonly IUIService? _uiService;
        private bool _isTyping;
        private string _currentMessage = string.Empty;
        
        public ObservableCollection<ChatMessage> Messages { get; }
        public event PropertyChangedEventHandler? PropertyChanged;

        public ChatView(IChatService? chatService = null)
        {
            InitializeComponent();
            
            _chatService = chatService;
            
            // Get UI service from app
            var app = (App)Application.Current;
            if (app.Services != null)
            {
                _uiService = app.Services.GetService(typeof(IUIService)) as IUIService;
            }
            
            Messages = new ObservableCollection<ChatMessage>();
            DataContext = this;
            
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
                OnPropertyChanged(nameof(IsInputEnabled));
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

        #endregion

        #region Event Handlers

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        private async void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                await SendMessageAsync();
                e.Handled = true;
            }
        }

        private async void ClearChat_Click(object sender, RoutedEventArgs e)
        {
            if (_uiService != null)
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
            else
            {
                Messages.Clear();
                AddWelcomeMessage();
            }
        }

        private async void Settings_Click(object sender, RoutedEventArgs e)
        {
            if (_uiService != null)
            {
                await _uiService.ShowNotificationAsync("Settings", "Settings dialog would open here in the full version.");
            }
        }

        private async void AttachFile_Click(object sender, RoutedEventArgs e)
        {
            if (_uiService != null)
            {
                await _uiService.ShowNotificationAsync("File Context", "File context attachment not yet implemented in standalone mode.");
            }
        }

        #endregion

        #region Private Methods

        private async Task SendMessageAsync()
        {
            if (!CanSendMessage || _chatService == null) return;

            var message = CurrentMessage.Trim();
            CurrentMessage = string.Empty;

            // Add user message
            var userMessage = new ChatMessage
            {
                Content = message,
                IsFromUser = true,
                Timestamp = DateTime.Now,
                MessageStyle = Application.Current.FindResource("UserMessageStyle") as Style ?? CreateUserMessageStyle(),
                TextColor = Brushes.White
            };
            
            Messages.Add(userMessage);
            ScrollToBottom();

            // Show typing indicator
            IsTyping = true;

            try
            {
                // Get AI response
                var response = await _chatService.SendMessageAsync(message);

                // Add AI message
                var aiMessage = new ChatMessage
                {
                    Content = response,
                    IsFromUser = false,
                    Timestamp = DateTime.Now,
                    MessageStyle = Application.Current.FindResource("AssistantMessageStyle") as Style ?? CreateAssistantMessageStyle(),
                    TextColor = Brushes.Black
                };
                
                Messages.Add(aiMessage);
                ScrollToBottom();
            }
            catch (Exception ex)
            {
                // Add error message
                var errorMessage = new ChatMessage
                {
                    Content = $"Error: {ex.Message}",
                    IsFromUser = false,
                    Timestamp = DateTime.Now,
                    MessageStyle = Application.Current.FindResource("ErrorMessageStyle") as Style ?? CreateErrorMessageStyle(),
                    TextColor = Brushes.Red
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
            var welcomeMessage = new ChatMessage
            {
                Content = "Welcome to A3sist Chat Desktop! 🚀\n\nI'm your AI-powered coding assistant. Currently running in standalone mode.\n\nTry asking me about:\n• Code examples\n• Debugging help\n• Programming questions\n• General assistance\n\nHow can I help you today?",
                IsFromUser = false,
                Timestamp = DateTime.Now,
                MessageStyle = Application.Current.FindResource("AssistantMessageStyle") as Style ?? CreateAssistantMessageStyle(),
                TextColor = Brushes.Black
            };
            
            Messages.Add(welcomeMessage);
        }

        private void ScrollToBottom()
        {
            if (ChatScrollViewer.ScrollableHeight > 0)
            {
                ChatScrollViewer.ScrollToEnd();
            }
        }

        private Style CreateUserMessageStyle()
        {
            var style = new Style(typeof(Border));
            style.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0, 122, 204))));
            style.Setters.Add(new Setter(Border.MarginProperty, new Thickness(5)));
            style.Setters.Add(new Setter(Border.PaddingProperty, new Thickness(10)));
            style.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(8)));
            style.Setters.Add(new Setter(FrameworkElement.MaxWidthProperty, 400.0));
            style.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right));
            return style;
        }

        private Style CreateAssistantMessageStyle()
        {
            var style = new Style(typeof(Border));
            style.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(240, 240, 240))));
            style.Setters.Add(new Setter(Border.MarginProperty, new Thickness(5)));
            style.Setters.Add(new Setter(Border.PaddingProperty, new Thickness(10)));
            style.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(8)));
            style.Setters.Add(new Setter(FrameworkElement.MaxWidthProperty, 400.0));
            style.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left));
            return style;
        }

        private Style CreateErrorMessageStyle()
        {
            var style = new Style(typeof(Border));
            style.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(255, 230, 230))));
            style.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(255, 68, 68))));
            style.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(1)));
            style.Setters.Add(new Setter(Border.MarginProperty, new Thickness(5)));
            style.Setters.Add(new Setter(Border.PaddingProperty, new Thickness(10)));
            style.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(8)));
            style.Setters.Add(new Setter(FrameworkElement.MaxWidthProperty, 400.0));
            style.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left));
            return style;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// Simple chat message model
    /// </summary>
    public class ChatMessage
    {
        public string Content { get; set; } = string.Empty;
        public bool IsFromUser { get; set; }
        public DateTime Timestamp { get; set; }
        public Style? MessageStyle { get; set; }
        public Brush? TextColor { get; set; }
    }
}