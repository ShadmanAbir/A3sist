using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using A3sist.UI.Models;
using A3sist.UI.Services;
using Microsoft.VisualStudio.Shell;

namespace A3sist.UI.UI
{
    public partial class A3sistToolWindow : UserControl
    {
        private readonly IA3sistApiClient _apiClient;
        private readonly IA3sistConfigurationService _configService;
        private readonly ObservableCollection<ChatMessage> _chatMessages;
        private readonly ObservableCollection<ModelInfo> _availableModels;
        private readonly ObservableCollection<MCPServerInfo> _mcpServers;
        private readonly ObservableCollection<AgentFinding> _agentFindings;
        private bool _isInitialized = false;

        public A3sistToolWindow(IA3sistApiClient apiClient, IA3sistConfigurationService configService)
        {
            InitializeComponent();
            
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            
            _chatMessages = new ObservableCollection<ChatMessage>();
            _availableModels = new ObservableCollection<ModelInfo>();
            _mcpServers = new ObservableCollection<MCPServerInfo>();
            _agentFindings = new ObservableCollection<AgentFinding>();
            
            // Bind collections to UI
            ChatMessages.ItemsSource = _chatMessages;
            ModelComboBox.ItemsSource = _availableModels;
            MCPServersList.ItemsSource = _mcpServers;
            AgentFindings.ItemsSource = _agentFindings;
            
            Loaded += A3sistToolWindow_Loaded;
        }

        private async void A3sistToolWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isInitialized) return;
            
            try
            {
                await InitializeAsync();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                ShowError($"Failed to initialize A3sist tool window: {ex.Message}");
            }
        }

        private async Task InitializeAsync()
        {
            // Setup event handlers
            _apiClient.ConnectionStateChanged += OnConnectionStateChanged;
            _apiClient.ChatMessageReceived += OnChatMessageReceived;
            _apiClient.AgentProgressChanged += OnAgentProgressChanged;
            _apiClient.ActiveModelChanged += OnActiveModelChanged;
            _apiClient.RAGIndexingProgress += OnRAGIndexingProgress;
            _apiClient.MCPServerStatusChanged += OnMCPServerStatusChanged;

            // Try to connect to API
            if (!_apiClient.IsConnected)
            {
                var autoStart = await _configService.GetSettingAsync("AutoStartApi", false);
                if (autoStart)
                {
                    var connected = await _apiClient.ConnectAsync();
                    if (!connected)
                    {
                        StatusText.Text = "Failed to auto-connect to A3sist API";
                    }
                }
            }

            // Load initial data
            await LoadChatHistoryAsync();
            await LoadAvailableModelsAsync();
            await LoadActiveModelAsync();
            await LoadMCPServersAsync();
            await UpdateRAGStatusAsync();
            await UpdateAgentStatusAsync();
            
            UpdateConnectionStatus(_apiClient.IsConnected);
            UpdateSendButtonState();
        }

        // Event Handlers
        private void OnConnectionStateChanged(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateConnectionStatus(_apiClient.IsConnected);
                UpdateSendButtonState();
            });
        }

        private void OnChatMessageReceived(object sender, ChatMessageReceivedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _chatMessages.Add(e.Message);
                ScrollChatToBottom();
            });
        }

        private void OnAgentProgressChanged(object sender, AgentProgressEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                AgentStatusText.Text = e.StatusMessage;
                AgentProgressBar.Value = e.ProgressPercentage;
                AgentProgressText.Text = $"{e.FilesProcessed}/{e.TotalFiles} files processed";
                
                if (e.Status == AgentAnalysisStatus.Running)
                {
                    StartAgentButton.IsEnabled = false;
                    StopAgentButton.IsEnabled = true;
                }
                else
                {
                    StartAgentButton.IsEnabled = true;
                    StopAgentButton.IsEnabled = false;
                }
            });
        }

        private void OnActiveModelChanged(object sender, ModelChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.NewModel != null)
                {
                    ModelComboBox.SelectedValue = e.NewModel.Id;
                }
            });
        }

        private void OnRAGIndexingProgress(object sender, IndexingProgressEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.Status.IsIndexing)
                {
                    RAGStatusText.Text = $"Indexing: {e.Status.ProgressPercentage:F1}% ({e.Status.CurrentFile})";
                    RAGProgressBar.Value = e.Status.ProgressPercentage;
                }
                else
                {
                    RAGStatusText.Text = $"Index ready ({e.Status.TotalDocuments} documents)";
                    RAGProgressBar.Value = 100;
                }
            });
        }

        private void OnMCPServerStatusChanged(object sender, MCPServerStatusChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var server = _mcpServers.FirstOrDefault(s => s.Id == e.ServerId);
                if (server != null)
                {
                    server.IsConnected = e.IsConnected;
                }
            });
        }

        // Data Loading Methods
        private async Task LoadChatHistoryAsync()
        {
            try
            {
                if (!_apiClient.IsConnected) return;
                
                var history = await _apiClient.GetChatHistoryAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _chatMessages.Clear();
                    foreach (var message in history)
                    {
                        _chatMessages.Add(message);
                    }
                    ScrollChatToBottom();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load chat history: {ex.Message}");
            }
        }

        private async Task LoadAvailableModelsAsync()
        {
            try
            {
                if (!_apiClient.IsConnected) return;
                
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
                System.Diagnostics.Debug.WriteLine($"Failed to load models: {ex.Message}");
            }
        }

        private async Task LoadActiveModelAsync()
        {
            try
            {
                if (!_apiClient.IsConnected) return;
                
                var activeModel = await _apiClient.GetActiveModelAsync();
                if (activeModel != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ModelComboBox.SelectedValue = activeModel.Id;
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load active model: {ex.Message}");
            }
        }

        private async Task LoadMCPServersAsync()
        {
            try
            {
                if (!_apiClient.IsConnected) return;
                
                var servers = await _apiClient.GetMCPServersAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _mcpServers.Clear();
                    foreach (var server in servers)
                    {
                        _mcpServers.Add(server);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load MCP servers: {ex.Message}");
            }
        }

        // UI Event Handlers
        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConnectButton.IsEnabled = false;
                var connected = await _apiClient.ConnectAsync();
                
                if (connected)
                {
                    await RefreshContentAsync();
                }
                else
                {
                    ShowError("Failed to connect to A3sist API. Please check your settings.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Connection failed: {ex.Message}");
            }
            finally
            {
                ConnectButton.IsEnabled = true;
            }
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _apiClient.DisconnectAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Disconnection failed: {ex.Message}");
            }
        }

        private void ChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                _ = SendChatMessageAsync();
            }
            UpdateSendButtonState();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendChatMessageAsync();
        }

        private async Task SendChatMessageAsync()
        {
            var messageText = ChatInput.Text?.Trim();
            if (string.IsNullOrEmpty(messageText) || !_apiClient.IsConnected)
                return;

            try
            {
                SendButton.IsEnabled = false;
                ChatInput.IsEnabled = false;

                var userMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = messageText,
                    Role = ChatRole.User,
                    Timestamp = DateTime.Now,
                    Sender = MessageSender.User
                };

                _chatMessages.Add(userMessage);
                ChatInput.Clear();
                ScrollChatToBottom();

                var response = await _apiClient.SendChatMessageAsync(userMessage);
                
                if (!response.Success)
                {
                    ShowError($"Failed to send message: {response.Error}");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error sending message: {ex.Message}");
            }
            finally
            {
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
                    await _apiClient.SetActiveModelAsync(modelId);
                }
                catch (Exception ex)
                {
                    ShowError($"Failed to set active model: {ex.Message}");
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
            finally
            {
                RefreshModelsButton.IsEnabled = true;
            }
        }

        private async void StartAgentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get current workspace path
                var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                var workspacePath = dte?.Solution?.FileName;
                
                if (string.IsNullOrEmpty(workspacePath))
                {
                    ShowError("No solution is currently open. Please open a solution first.");
                    return;
                }

                var directoryPath = System.IO.Path.GetDirectoryName(workspacePath);
                var success = await _apiClient.StartAgentAnalysisAsync(directoryPath);
                
                if (!success)
                {
                    ShowError("Failed to start agent analysis.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Failed to start agent analysis: {ex.Message}");
            }
        }

        private async void StopAgentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var success = await _apiClient.StopAgentAnalysisAsync();
                if (!success)
                {
                    ShowError("Failed to stop agent analysis.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Failed to stop agent analysis: {ex.Message}");
            }
        }

        private async void IndexWorkspaceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                var workspacePath = dte?.Solution?.FileName;
                
                if (string.IsNullOrEmpty(workspacePath))
                {
                    ShowError("No solution is currently open. Please open a solution first.");
                    return;
                }

                IndexWorkspaceButton.IsEnabled = false;
                var directoryPath = System.IO.Path.GetDirectoryName(workspacePath);
                var success = await _apiClient.IndexWorkspaceAsync(directoryPath);
                
                if (!success)
                {
                    ShowError("Failed to start workspace indexing.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Failed to index workspace: {ex.Message}");
            }
            finally
            {
                IndexWorkspaceButton.IsEnabled = true;
            }
        }

        private async void AutoCompleteEnabledCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                await _configService.SetSettingAsync("AutoCompleteEnabled", true);
            }
            catch (Exception ex)
            {
                ShowError($"Failed to update auto-complete setting: {ex.Message}");
            }
        }

        private async void AutoCompleteEnabledCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                await _configService.SetSettingAsync("AutoCompleteEnabled", false);
            }
            catch (Exception ex)
            {
                ShowError($"Failed to update auto-complete setting: {ex.Message}");
            }
        }

        private async void ConnectMCPServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string serverId)
            {
                try
                {
                    button.IsEnabled = false;
                    var server = _mcpServers.FirstOrDefault(s => s.Id == serverId);
                    
                    if (server?.IsConnected == true)
                    {
                        await _apiClient.DisconnectFromMCPServerAsync(serverId);
                    }
                    else
                    {
                        await _apiClient.ConnectToMCPServerAsync(serverId);
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"MCP server operation failed: {ex.Message}");
                }
                finally
                {
                    button.IsEnabled = true;
                }
            }
        }

        private async void RefreshMCPServersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshMCPServersButton.IsEnabled = false;
                await LoadMCPServersAsync();
            }
            finally
            {
                RefreshMCPServersButton.IsEnabled = true;
            }
        }

        // Helper Methods
        private void UpdateSendButtonState()
        {
            SendButton.IsEnabled = _apiClient.IsConnected && !string.IsNullOrWhiteSpace(ChatInput.Text);
        }

        private void ScrollChatToBottom()
        {
            // The chat messages are in a ScrollViewer, scroll to bottom
            // This would need to be implemented based on the actual XAML structure
        }

        private void ShowError(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusText.Text = $"Error: {message}";
                MessageBox.Show(message, "A3sist Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        // Public methods for tool window pane integration
        public void RefreshContent()
        {
            try
            {
                _ = Task.Run(async () => await RefreshContentAsync());
            }
            catch (Exception ex)
            {
                ShowError($"Failed to refresh content: {ex.Message}");
            }
        }

        private async Task RefreshContentAsync()
        {
            if (_apiClient?.IsConnected == true)
            {
                await LoadChatHistoryAsync();
                await LoadAvailableModelsAsync();
                await LoadActiveModelAsync();
                await LoadMCPServersAsync();
                await UpdateRAGStatusAsync();
                await UpdateAgentStatusAsync();
            }
        }

        public void UpdateConnectionStatus(bool isConnected)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (isConnected)
                {
                    ConnectionIndicator.Fill = System.Windows.Media.Brushes.Green;
                    ConnectionStatus.Text = "Connected";
                    StatusText.Text = "Connected to A3sist API";
                    ConnectButton.IsEnabled = false;
                    DisconnectButton.IsEnabled = true;
                }
                else
                {
                    ConnectionIndicator.Fill = System.Windows.Media.Brushes.Red;
                    ConnectionStatus.Text = "Disconnected";
                    StatusText.Text = "Disconnected from A3sist API";
                    ConnectButton.IsEnabled = true;
                    DisconnectButton.IsEnabled = false;
                }
            });
        }

        private async Task UpdateRAGStatusAsync()
        {
            try
            {
                if (!_apiClient.IsConnected) return;
                
                var status = await _apiClient.GetIndexingStatusAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (status.IsIndexing)
                    {
                        RAGStatusText.Text = $"Indexing: {status.ProgressPercentage:F1}% ({status.CurrentFile})";
                        RAGProgressBar.Value = status.ProgressPercentage;
                    }
                    else
                    {
                        RAGStatusText.Text = $"Index ready ({status.TotalDocuments} documents)";
                        RAGProgressBar.Value = 100;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update RAG status: {ex.Message}");
            }
        }

        private async Task UpdateAgentStatusAsync()
        {
            try
            {
                if (!_apiClient.IsConnected) return;
                
                var isRunning = await _apiClient.IsAgentAnalysisRunningAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (isRunning)
                    {
                        AgentStatusText.Text = "Agent analysis running...";
                        StartAgentButton.IsEnabled = false;
                        StopAgentButton.IsEnabled = true;
                    }
                    else
                    {
                        AgentStatusText.Text = "Agent not running";
                        StartAgentButton.IsEnabled = true;
                        StopAgentButton.IsEnabled = false;
                        AgentProgressBar.Value = 0;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update agent status: {ex.Message}");
            }
        }

        public void Dispose()
        {
            try
            {
                if (_apiClient != null)
                {
                    _apiClient.ConnectionStateChanged -= OnConnectionStateChanged;
                    _apiClient.ChatMessageReceived -= OnChatMessageReceived;
                    _apiClient.AgentProgressChanged -= OnAgentProgressChanged;
                    _apiClient.ActiveModelChanged -= OnActiveModelChanged;
                    _apiClient.RAGIndexingProgress -= OnRAGIndexingProgress;
                    _apiClient.MCPServerStatusChanged -= OnMCPServerStatusChanged;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist tool window dispose error: {ex.Message}");
            }
        }
    }
}