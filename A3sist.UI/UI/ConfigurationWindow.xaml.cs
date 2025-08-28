using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using A3sist.UI.Models;
using A3sist.UI.Services;

namespace A3sist.UI.UI
{
    public partial class ConfigurationWindow : Window
    {
        private readonly IA3sistApiClient _apiClient;
        private readonly IA3sistConfigurationService _configService;
        private readonly ObservableCollection<ModelInfo> _models;
        private readonly ObservableCollection<MCPServerInfo> _mcpServers;
        private bool _isInitialized = false;
        private bool _hasChanges = false;

        public ConfigurationWindow(IA3sistApiClient apiClient, IA3sistConfigurationService configService)
        {
            InitializeComponent();
            
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            
            _models = new ObservableCollection<ModelInfo>();
            _mcpServers = new ObservableCollection<MCPServerInfo>();
            
            ModelsList.ItemsSource = _models;
            MCPServersList.ItemsSource = _mcpServers;
            
            Loaded += ConfigurationWindow_Loaded;
            Closing += ConfigurationWindow_Closing;
        }

        private async void ConfigurationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isInitialized) return;
            
            try
            {
                await InitializeAsync();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                ShowError($"Failed to initialize configuration window: {ex.Message}");
            }
        }

        private async Task InitializeAsync()
        {
            // Setup event handlers
            _apiClient.ConnectionStateChanged += OnConnectionStateChanged;
            _apiClient.ActiveModelChanged += OnActiveModelChanged;
            _apiClient.MCPServerStatusChanged += OnMCPServerStatusChanged;

            // Load configuration settings
            await LoadConfigurationAsync();
            
            // Connect to API if not connected
            if (!_apiClient.IsConnected)
            {
                var connected = await _apiClient.ConnectAsync();
                if (!connected)
                {
                    ShowWarning("Failed to connect to A3sist API. Some features may not be available.");
                }
            }

            // Load API data
            await LoadModelsAsync();
            await LoadMCPServersAsync();
            
            UpdateConnectionStatus();
            _hasChanges = false;
        }

        private async Task LoadConfigurationAsync()
        {
            try
            {
                // API Settings
                var apiUrl = await _configService.GetSettingAsync("ApiUrl", "http://localhost:8341");
                ApiUrlTextBox.Text = apiUrl;

                var autoStartApi = await _configService.GetSettingAsync("AutoStartApi", false);
                AutoStartApiCheckBox.IsChecked = autoStartApi;

                var requestTimeout = await _configService.GetSettingAsync("RequestTimeout", 30);
                RequestTimeoutTextBox.Text = requestTimeout.ToString();

                var enableLogging = await _configService.GetSettingAsync("EnableLogging", true);
                EnableLoggingCheckBox.IsChecked = enableLogging;

                var streamResponses = await _configService.GetSettingAsync("StreamResponses", true);
                StreamResponsesCheckBox.IsChecked = streamResponses;

                // Feature Settings
                var autoCompleteEnabled = await _configService.GetSettingAsync("AutoCompleteEnabled", true);
                AutoCompleteEnabledCheckBox.IsChecked = autoCompleteEnabled;

                var autoCompleteSnippets = await _configService.GetSettingAsync("AutoCompleteSnippets", true);
                AutoCompleteSnippetsCheckBox.IsChecked = autoCompleteSnippets;

                var maxSuggestions = await _configService.GetSettingAsync("MaxSuggestions", 10);
                MaxSuggestionsTextBox.Text = maxSuggestions.ToString();

                var realTimeAnalysis = await _configService.GetSettingAsync("RealTimeAnalysis", true);
                RealTimeAnalysisCheckBox.IsChecked = realTimeAnalysis;

                var showSuggestions = await _configService.GetSettingAsync("ShowSuggestions", true);
                ShowSuggestionsCheckBox.IsChecked = showSuggestions;

                var showCodeIssues = await _configService.GetSettingAsync("ShowCodeIssues", true);
                ShowCodeIssuesCheckBox.IsChecked = showCodeIssues;

                var autoStartAgent = await _configService.GetSettingAsync("AutoStartAgent", false);
                AutoStartAgentCheckBox.IsChecked = autoStartAgent;

                var backgroundAnalysis = await _configService.GetSettingAsync("BackgroundAnalysis", true);
                BackgroundAnalysisCheckBox.IsChecked = backgroundAnalysis;

                var analysisInterval = await _configService.GetSettingAsync("AnalysisInterval", 30);
                AnalysisIntervalTextBox.Text = analysisInterval.ToString();

                var autoIndexWorkspace = await _configService.GetSettingAsync("AutoIndexWorkspace", true);
                AutoIndexWorkspaceCheckBox.IsChecked = autoIndexWorkspace;

                var indexDependencies = await _configService.GetSettingAsync("IndexDependencies", false);
                IndexDependenciesCheckBox.IsChecked = indexDependencies;

                var maxSearchResults = await _configService.GetSettingAsync("MaxSearchResults", 10);
                MaxSearchResultsTextBox.Text = maxSearchResults.ToString();
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load configuration: {ex.Message}");
            }
        }

        private async Task LoadModelsAsync()
        {
            try
            {
                var models = await _apiClient.GetAvailableModelsAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _models.Clear();
                    foreach (var model in models)
                    {
                        _models.Add(model);
                    }
                });
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load models: {ex.Message}");
            }
        }

        private async Task LoadMCPServersAsync()
        {
            try
            {
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
                ShowError($"Failed to load MCP servers: {ex.Message}");
            }
        }

        private void OnConnectionStateChanged(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateConnectionStatus();
            });
        }

        private void OnActiveModelChanged(object sender, ModelChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Refresh models list to show active model
                _ = LoadModelsAsync();
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

        private void UpdateConnectionStatus()
        {
            if (_apiClient.IsConnected)
            {
                ConnectionIndicator.Fill = System.Windows.Media.Brushes.Green;
                ConnectionStatusText.Text = "Connected";
                ConnectButton.IsEnabled = false;
                DisconnectButton.IsEnabled = true;
            }
            else
            {
                ConnectionIndicator.Fill = System.Windows.Media.Brushes.Red;
                ConnectionStatusText.Text = "Disconnected";
                ConnectButton.IsEnabled = true;
                DisconnectButton.IsEnabled = false;
            }
        }

        // Event Handlers
        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TestConnectionButton.IsEnabled = false;
                TestConnectionButton.Content = "Testing...";

                var connected = await _apiClient.ConnectAsync();
                if (connected)
                {
                    ShowInfo("Connection test successful!");
                }
                else
                {
                    ShowError("Connection test failed. Please check your API URL and ensure the A3sist API is running.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Connection test failed: {ex.Message}");
            }
            finally
            {
                TestConnectionButton.IsEnabled = true;
                TestConnectionButton.Content = "Test";
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var connected = await _apiClient.ConnectAsync();
                if (!connected)
                {
                    ShowError("Failed to connect to A3sist API.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Connection failed: {ex.Message}");
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

        private async void TestModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string modelId)
            {
                try
                {
                    button.IsEnabled = false;
                    button.Content = "Testing...";

                    var success = await _apiClient.TestModelConnectionAsync(modelId);
                    if (success)
                    {
                        ShowInfo("Model test successful!");
                    }
                    else
                    {
                        ShowError("Model test failed. Please check the model configuration.");
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Model test failed: {ex.Message}");
                }
                finally
                {
                    button.IsEnabled = true;
                    button.Content = "Test";
                }
            }
        }

        private async void SetActiveModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string modelId)
            {
                try
                {
                    var success = await _apiClient.SetActiveModelAsync(modelId);
                    if (success)
                    {
                        ShowInfo("Active model updated successfully!");
                        await LoadModelsAsync();
                    }
                    else
                    {
                        ShowError("Failed to set active model.");
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Failed to set active model: {ex.Message}");
                }
            }
        }

        private void AddModelButton_Click(object sender, RoutedEventArgs e)
        {
            ShowInfo("Add Model functionality will be implemented in a future update.");
        }

        private async void RefreshModelsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshModelsButton.IsEnabled = false;
                await LoadModelsAsync();
            }
            finally
            {
                RefreshModelsButton.IsEnabled = true;
            }
        }

        private void RemoveModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModelsList.SelectedItem is ModelInfo selectedModel)
            {
                var result = MessageBox.Show($"Are you sure you want to remove the model '{selectedModel.Name}'?",
                                           "Remove Model", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    ShowInfo("Remove Model functionality will be implemented in a future update.");
                }
            }
            else
            {
                ShowWarning("Please select a model to remove.");
            }
        }

        private async void ToggleMCPServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string serverId)
            {
                try
                {
                    var server = _mcpServers.FirstOrDefault(s => s.Id == serverId);
                    if (server == null) return;

                    button.IsEnabled = false;

                    if (server.IsConnected)
                    {
                        var success = await _apiClient.DisconnectFromMCPServerAsync(serverId);
                        if (!success)
                        {
                            ShowError("Failed to disconnect from MCP server.");
                        }
                    }
                    else
                    {
                        var success = await _apiClient.ConnectToMCPServerAsync(serverId);
                        if (!success)
                        {
                            ShowError("Failed to connect to MCP server.");
                        }
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

        private async void ViewToolsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string serverId)
            {
                try
                {
                    var tools = await _apiClient.GetAvailableToolsAsync(serverId);
                    var toolsList = string.Join("\n", tools);
                    
                    MessageBox.Show($"Available tools:\n\n{toolsList}", "MCP Server Tools", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    ShowError($"Failed to get tools: {ex.Message}");
                }
            }
        }

        private void AddMCPServerButton_Click(object sender, RoutedEventArgs e)
        {
            ShowInfo("Add MCP Server functionality will be implemented in a future update.");
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

        private void RemoveMCPServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (MCPServersList.SelectedItem is MCPServerInfo selectedServer)
            {
                var result = MessageBox.Show($"Are you sure you want to remove the server '{selectedServer.Name}'?",
                                           "Remove Server", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    ShowInfo("Remove MCP Server functionality will be implemented in a future update.");
                }
            }
            else
            {
                ShowWarning("Please select a server to remove.");
            }
        }

        // Configuration change handlers
        private void AutoStartApiCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _hasChanges = true;
        }

        private void AutoStartApiCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _hasChanges = true;
        }

        private void EnableLoggingCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _hasChanges = true;
        }

        private void EnableLoggingCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _hasChanges = true;
        }

        private void StreamResponsesCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _hasChanges = true;
        }

        private void StreamResponsesCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _hasChanges = true;
        }

        private void AutoCompleteEnabledCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _hasChanges = true;
        }

        private void AutoCompleteEnabledCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _hasChanges = true;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveButton.IsEnabled = false;
                SaveButton.Content = "Saving...";

                await SaveConfigurationAsync();
                _hasChanges = false;
                
                ShowInfo("Configuration saved successfully!");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Failed to save configuration: {ex.Message}");
            }
            finally
            {
                SaveButton.IsEnabled = true;
                SaveButton.Content = "Save Settings";
            }
        }

        private async Task SaveConfigurationAsync()
        {
            // API Settings
            await _configService.SetSettingAsync("ApiUrl", ApiUrlTextBox.Text);
            await _configService.SetSettingAsync("AutoStartApi", AutoStartApiCheckBox.IsChecked ?? false);
            
            if (int.TryParse(RequestTimeoutTextBox.Text, out int timeout))
            {
                await _configService.SetSettingAsync("RequestTimeout", timeout);
            }

            await _configService.SetSettingAsync("EnableLogging", EnableLoggingCheckBox.IsChecked ?? true);
            await _configService.SetSettingAsync("StreamResponses", StreamResponsesCheckBox.IsChecked ?? true);

            // Feature Settings
            await _configService.SetSettingAsync("AutoCompleteEnabled", AutoCompleteEnabledCheckBox.IsChecked ?? true);
            await _configService.SetSettingAsync("AutoCompleteSnippets", AutoCompleteSnippetsCheckBox.IsChecked ?? true);
            
            if (int.TryParse(MaxSuggestionsTextBox.Text, out int maxSuggestions))
            {
                await _configService.SetSettingAsync("MaxSuggestions", maxSuggestions);
            }

            await _configService.SetSettingAsync("RealTimeAnalysis", RealTimeAnalysisCheckBox.IsChecked ?? true);
            await _configService.SetSettingAsync("ShowSuggestions", ShowSuggestionsCheckBox.IsChecked ?? true);
            await _configService.SetSettingAsync("ShowCodeIssues", ShowCodeIssuesCheckBox.IsChecked ?? true);
            await _configService.SetSettingAsync("AutoStartAgent", AutoStartAgentCheckBox.IsChecked ?? false);
            await _configService.SetSettingAsync("BackgroundAnalysis", BackgroundAnalysisCheckBox.IsChecked ?? true);
            
            if (int.TryParse(AnalysisIntervalTextBox.Text, out int interval))
            {
                await _configService.SetSettingAsync("AnalysisInterval", interval);
            }

            await _configService.SetSettingAsync("AutoIndexWorkspace", AutoIndexWorkspaceCheckBox.IsChecked ?? true);
            await _configService.SetSettingAsync("IndexDependencies", IndexDependenciesCheckBox.IsChecked ?? false);
            
            if (int.TryParse(MaxSearchResultsTextBox.Text, out int maxResults))
            {
                await _configService.SetSettingAsync("MaxSearchResults", maxResults);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ConfigurationWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_hasChanges)
            {
                var result = MessageBox.Show("You have unsaved changes. Do you want to save them before closing?",
                                           "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    SaveButton_Click(SaveButton, new RoutedEventArgs());
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }

            // Clean up event handlers
            if (_apiClient != null)
            {
                _apiClient.ConnectionStateChanged -= OnConnectionStateChanged;
                _apiClient.ActiveModelChanged -= OnActiveModelChanged;
                _apiClient.MCPServerStatusChanged -= OnMCPServerStatusChanged;
            }
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowWarning(string message)
        {
            MessageBox.Show(message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ShowInfo(string message)
        {
            MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}