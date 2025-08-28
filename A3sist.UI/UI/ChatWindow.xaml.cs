using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using A3sist.UI.Models;
using A3sist.UI.Services;

namespace A3sist.UI.UI
{
    public partial class ChatWindow : Window
    {
        private readonly IA3sistApiClient _apiClient;
        private readonly IA3sistConfigurationService _configService;
        private readonly ObservableCollection<ChatMessage> _chatMessages;
        private readonly ObservableCollection<ModelInfo> _availableModels;
        private bool _isInitialized = false;

        public ChatWindow(IA3sistApiClient apiClient, IA3sistConfigurationService configService)
        {
            InitializeComponent();
            
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            
            _chatMessages = new ObservableCollection<ChatMessage>();
            _availableModels = new ObservableCollection<ModelInfo>();
            
            ChatMessages.ItemsSource = _chatMessages;
            ModelComboBox.ItemsSource = _availableModels;
            
            Loaded += ChatWindow_Loaded;
            Closed += ChatWindow_Closed;
        }

        private async void ChatWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isInitialized) return;
            
            try
            {
                await InitializeAsync();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                ShowError($"Failed to initialize chat window: {ex.Message}");
            }
        }

        private async Task InitializeAsync()
        {
            // Setup event handlers
            _apiClient.ConnectionStateChanged += OnConnectionStateChanged;
            _apiClient.ChatMessageReceived += OnChatMessageReceived;
            _apiClient.ActiveModelChanged += OnActiveModelChanged;

            // Connect to API if not connected
            if (!_apiClient.IsConnected)
            {
                var connected = await _apiClient.ConnectAsync();
                if (!connected)
                {
                    ShowError("Failed to connect to A3sist API. Please check your connection settings.");
                    return;
                }
            }

            // Load initial data
            await LoadChatHistoryAsync();
            await LoadAvailableModelsAsync();
            await LoadActiveModelAsync();
            
            UpdateConnectionStatus();
            UpdateSendButtonState();
        }

        private void ChatWindow_Closed(object sender, EventArgs e)
        {
            if (_apiClient != null)
            {
                _apiClient.ConnectionStateChanged -= OnConnectionStateChanged;
                _apiClient.ChatMessageReceived -= OnChatMessageReceived;
                _apiClient.ActiveModelChanged -= OnActiveModelChanged;
            }
        }

        private async Task LoadChatHistoryAsync()
        {
            try
            {
                var history = await _apiClient.GetChatHistoryAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _chatMessages.Clear();
                    foreach (var message in history)
                    {
                        _chatMessages.Add(message);
                    }
                    ScrollToBottom();
                });
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load chat history: {ex.Message}");
            }
        }

        private async Task LoadAvailableModelsAsync()
        {
            try
            {
                var models = await _apiClient.GetAvailableModelsAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _availableModels.Clear();
                    foreach (var model in models)
                    {
                        _availableModels.Add(model);
                    }
                });
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load models: {ex.Message}");
            }
        }

        private async Task LoadActiveModelAsync()
        {
            try
            {
                var activeModel = await _apiClient.GetActiveModelAsync();
                if (activeModel != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ModelComboBox.SelectedValue = activeModel.Id;
                        ModelStatusText.Text = $"Active: {activeModel.Name}";
                    });
                }
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load active model: {ex.Message}");
            }
        }

        private void OnConnectionStateChanged(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateConnectionStatus();
                UpdateSendButtonState();
            });
        }

        private void OnChatMessageReceived(object sender, ChatMessageReceivedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _chatMessages.Add(e.Message);
                ScrollToBottom();
            });
        }

        private void OnActiveModelChanged(object sender, ModelChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.NewModel != null)
                {
                    ModelComboBox.SelectedValue = e.NewModel.Id;
                    ModelStatusText.Text = $"Active: {e.NewModel.Name}";
                }
            });
        }

        private void UpdateConnectionStatus()
        {
            if (_apiClient.IsConnected)
            {
                ConnectionIndicator.Fill = System.Windows.Media.Brushes.Green;
                ConnectionStatus.Text = "Connected";
            }
            else
            {
                ConnectionIndicator.Fill = System.Windows.Media.Brushes.Red;
                ConnectionStatus.Text = "Disconnected";
            }
        }

        private void UpdateSendButtonState()
        {
            SendButton.IsEnabled = _apiClient.IsConnected && !string.IsNullOrWhiteSpace(ChatInput.Text);
        }

        private void ChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    // Ctrl+Enter to send
                    e.Handled = true;
                    if (SendButton.IsEnabled)
                    {
                        _ = SendMessageAsync();
                    }
                }
                // Allow normal Enter for new lines
            }

            // Update send button state as user types
            UpdateSendButtonState();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        private async Task SendMessageAsync()
        {
            var messageText = ChatInput.Text?.Trim();
            if (string.IsNullOrEmpty(messageText) || !_apiClient.IsConnected)
                return;

            try
            {
                // Disable input while sending
                SendButton.IsEnabled = false;
                ChatInput.IsEnabled = false;

                // Create user message
                var userMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = messageText,
                    Role = ChatRole.User,
                    Timestamp = DateTime.Now,
                    Sender = MessageSender.User
                };

                // Add to UI immediately
                _chatMessages.Add(userMessage);
                ChatInput.Clear();
                ScrollToBottom();

                // Send to API
                var response = await _apiClient.SendChatMessageAsync(userMessage);
                
                if (!response.Success)
                {
                    ShowError($"Failed to send message: {response.Error}");
                    return;
                }

                // Assistant response will be added via event handler
            }
            catch (Exception ex)
            {
                ShowError($"Error sending message: {ex.Message}");
            }
            finally
            {
                // Re-enable input
                ChatInput.IsEnabled = true;
                UpdateSendButtonState();
            }
        }

        private async void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModelComboBox.SelectedValue is string modelId && !string.IsNullOrEmpty(modelId))
            {
                try
                {
                    var success = await _apiClient.SetActiveModelAsync(modelId);
                    if (!success)
                    {
                        ShowError("Failed to set active model");
                    }
                    else
                    {
                        var selectedModel = _availableModels.FirstOrDefault(m => m.Id == modelId);
                        if (selectedModel != null)
                        {
                            ModelStatusText.Text = $"Active: {selectedModel.Name}";
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Error setting model: {ex.Message}");
                }
            }
        }

        private async void RefreshModelsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshModelsButton.IsEnabled = false;
                await LoadAvailableModelsAsync();
                await LoadActiveModelAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error refreshing models: {ex.Message}");
            }
            finally
            {
                RefreshModelsButton.IsEnabled = true;
            }
        }

        private async void ClearChatButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear the chat history?", 
                                       "Clear Chat", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _apiClient.ClearChatHistoryAsync();
                    _chatMessages.Clear();
                }
                catch (Exception ex)
                {
                    ShowError($"Error clearing chat: {ex.Message}");
                }
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var configWindow = new ConfigurationWindow(_apiClient, _configService);
                configWindow.Owner = this;
                configWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError($"Error opening settings: {ex.Message}");
            }
        }

        private void ScrollToBottom()
        {
            ChatScrollViewer.ScrollToBottom();
        }

        private void ShowError(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }
}