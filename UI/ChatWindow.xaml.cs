using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using A3sist.Models;
using A3sist.Services;

namespace A3sist.UI
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window, INotifyPropertyChanged
    {
        private readonly IChatService _chatService;
        private readonly IModelManagementService _modelService;
        private readonly IA3sistConfigurationService _configService;
        private ObservableCollection<ChatMessageViewModel> _chatMessages;
        private ObservableCollection<ModelInfo> _availableModels;
        private bool _isSendingMessage;

        public ChatWindow(
            IChatService chatService, 
            IModelManagementService modelService, 
            IA3sistConfigurationService configService)
        {
            InitializeComponent();
            
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));

            _chatMessages = new ObservableCollection<ChatMessageViewModel>();
            _availableModels = new ObservableCollection<ModelInfo>();

            DataContext = this;
            
            Loaded += ChatWindow_Loaded;
            _chatService.MessageReceived += ChatService_MessageReceived;
        }

        public ObservableCollection<ChatMessageViewModel> ChatMessages
        {
            get => _chatMessages;
            set
            {
                _chatMessages = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ModelInfo> AvailableModels
        {
            get => _availableModels;
            set
            {
                _availableModels = value;
                OnPropertyChanged();
            }
        }

        public bool IsSendingMessage
        {
            get => _isSendingMessage;
            set
            {
                _isSendingMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSendMessage));
            }
        }

        public bool CanSendMessage => !IsSendingMessage && !string.IsNullOrWhiteSpace(MessageTextBox.Text);

        private async void ChatWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAvailableModelsAsync();
            await LoadChatHistoryAsync();
            await UpdateModelStatusAsync();
            
            MessageTextBox.Focus();
        }

        private async Task LoadAvailableModelsAsync()
        {
            try
            {
                var models = await _modelService.GetAvailableModelsAsync();
                AvailableModels.Clear();
                
                foreach (var model in models.Where(m => m.IsAvailable))
                {
                    AvailableModels.Add(model);
                }

                if (AvailableModels.Any())
                {
                    var activeModel = await _modelService.GetActiveModelAsync();
                    if (activeModel != null)
                    {
                        ModelComboBox.SelectedValue = activeModel.Id;
                    }
                    else
                    {
                        ModelComboBox.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error loading models: {ex.Message}");
            }
        }

        private async Task LoadChatHistoryAsync()
        {
            try
            {
                var history = await _chatService.GetChatHistoryAsync();
                ChatMessages.Clear();
                
                foreach (var message in history)
                {
                    ChatMessages.Add(new ChatMessageViewModel(message)
                    {
                        ShowTimestamps = ShowTimestampsCheckBox.IsChecked ?? false,
                        ShowModelUsed = ShowModelUsedCheckBox.IsChecked ?? false
                    });
                }

                ScrollToBottom();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error loading chat history: {ex.Message}");
            }
        }

        private async Task UpdateModelStatusAsync()
        {
            try
            {
                var activeModel = await _modelService.GetActiveModelAsync();
                if (activeModel != null)
                {
                    ModelStatusIndicator.Fill = activeModel.IsAvailable ? 
                        System.Windows.Media.Brushes.Green : 
                        System.Windows.Media.Brushes.Red;
                    ModelStatusText.Text = activeModel.IsAvailable ? 
                        $"Connected to {activeModel.Name}" : 
                        $"Disconnected from {activeModel.Name}";
                }
                else
                {
                    ModelStatusIndicator.Fill = System.Windows.Media.Brushes.Gray;
                    ModelStatusText.Text = "No model selected";
                }
            }
            catch
            {
                ModelStatusIndicator.Fill = System.Windows.Media.Brushes.Red;
                ModelStatusText.Text = "Model status unknown";
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        private async void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    // Ctrl+Enter inserts a new line
                    var textBox = sender as TextBox;
                    var caretIndex = textBox.CaretIndex;
                    textBox.Text = textBox.Text.Insert(caretIndex, Environment.NewLine);
                    textBox.CaretIndex = caretIndex + Environment.NewLine.Length;
                    e.Handled = true;
                }
                else
                {
                    // Enter sends the message
                    await SendMessageAsync();
                    e.Handled = true;
                }
            }
        }

        private async Task SendMessageAsync()
        {
            if (IsSendingMessage || string.IsNullOrWhiteSpace(MessageTextBox.Text))
                return;

            var messageText = MessageTextBox.Text.Trim();
            MessageTextBox.Clear();
            IsSendingMessage = true;
            
            try
            {
                StatusTextBlock.Text = "Sending message...";

                // Create user message
                var userMessage = new ChatMessage
                {
                    Content = messageText,
                    Sender = MessageSender.User,
                    Timestamp = DateTime.UtcNow
                };

                // Add to UI immediately
                var userMessageViewModel = new ChatMessageViewModel(userMessage)
                {
                    ShowTimestamps = ShowTimestampsCheckBox.IsChecked ?? false,
                    ShowModelUsed = ShowModelUsedCheckBox.IsChecked ?? false
                };
                ChatMessages.Add(userMessageViewModel);
                ScrollToBottom();

                // Send to chat service
                var response = await _chatService.SendMessageAsync(userMessage);

                if (response.Success)
                {
                    StatusTextBlock.Text = $"Response received in {response.ProcessingTime.TotalMilliseconds:F0}ms";
                    
                    // The response message will be added via the MessageReceived event
                }
                else
                {
                    StatusTextBlock.Text = "Error sending message";
                    ShowErrorMessage($"Error: {response.Error}");
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Error occurred";
                ShowErrorMessage($"Error sending message: {ex.Message}");
            }
            finally
            {
                IsSendingMessage = false;
                MessageTextBox.Focus();
            }
        }

        private void ChatService_MessageReceived(object sender, ChatMessageReceivedEventArgs e)
        {
            // Ensure UI updates happen on the UI thread
            Dispatcher.Invoke(() =>
            {
                var messageViewModel = new ChatMessageViewModel(e.Message)
                {
                    ShowTimestamps = ShowTimestampsCheckBox.IsChecked ?? false,
                    ShowModelUsed = ShowModelUsedCheckBox.IsChecked ?? false
                };
                ChatMessages.Add(messageViewModel);
                ScrollToBottom();
            });
        }

        private async void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModelComboBox.SelectedValue is string selectedModelId)
            {
                try
                {
                    await _chatService.SetChatModelAsync(selectedModelId);
                    await UpdateModelStatusAsync();
                    StatusTextBlock.Text = $"Switched to model: {((ModelInfo)ModelComboBox.SelectedItem)?.Name}";
                }
                catch (Exception ex)
                {
                    ShowErrorMessage($"Error switching model: {ex.Message}");
                }
            }
        }

        private void ConfigureButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var configWindow = new ConfigurationWindow(_configService, _modelService, null, null);
                configWindow.Owner = this;
                if (configWindow.ShowDialog() == true)
                {
                    // Reload models after configuration changes
                    _ = LoadAvailableModelsAsync();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error opening configuration: {ex.Message}");
            }
        }

        private async void ClearChatButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear the chat history?",
                "Clear Chat",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _chatService.ClearChatHistoryAsync();
                    ChatMessages.Clear();
                    StatusTextBlock.Text = "Chat history cleared";
                }
                catch (Exception ex)
                {
                    ShowErrorMessage($"Error clearing chat: {ex.Message}");
                }
            }
        }

        private void ScrollToBottom()
        {
            ChatScrollViewer.ScrollToBottom();
        }

        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "A3sist Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// ViewModel for chat messages to handle UI-specific properties
    /// </summary>
    public class ChatMessageViewModel : INotifyPropertyChanged
    {
        private readonly ChatMessage _message;
        private bool _showTimestamps;
        private bool _showModelUsed;

        public ChatMessageViewModel(ChatMessage message)
        {
            _message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public string Id => _message.Id;
        public string Content => _message.Content;
        public MessageSender Sender => _message.Sender;
        public DateTime Timestamp => _message.Timestamp;
        public string ModelUsed => _message.ModelUsed;
        public bool IsCode => _message.IsCode;

        public bool ShowTimestamps
        {
            get => _showTimestamps;
            set
            {
                _showTimestamps = value;
                OnPropertyChanged();
            }
        }

        public bool ShowModelUsed
        {
            get => _showModelUsed;
            set
            {
                _showModelUsed = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Converter to check if a value is greater than zero
    /// </summary>
    public class GreaterThanZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
                return intValue > 0;
            if (value is double doubleValue)
                return doubleValue > 0;
            if (value is string stringValue)
                return !string.IsNullOrWhiteSpace(stringValue);
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}