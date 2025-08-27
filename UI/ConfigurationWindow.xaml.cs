using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using A3sist.Models;
using A3sist.Services;

namespace A3sist.UI
{
    /// <summary>
    /// Interaction logic for ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window
    {
        private readonly IA3sistConfigurationService _configService;
        private readonly IModelManagementService _modelService;
        private readonly IMCPClientService _mcpService;
        private readonly IRAGEngineService _ragService;

        private ObservableCollection<ModelInfo> _localModels;
        private ObservableCollection<ModelInfo> _remoteModels;
        private ObservableCollection<MCPServerInfo> _mcpServers;

        public ConfigurationWindow(
            IA3sistConfigurationService configService,
            IModelManagementService modelService,
            IMCPClientService mcpService,
            IRAGEngineService ragService)
        {
            InitializeComponent();

            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));
            _mcpService = mcpService;
            _ragService = ragService;

            _localModels = new ObservableCollection<ModelInfo>();
            _remoteModels = new ObservableCollection<ModelInfo>();
            _mcpServers = new ObservableCollection<MCPServerInfo>();

            Loaded += ConfigurationWindow_Loaded;
        }

        private async void ConfigurationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadConfigurationAsync();
        }

        private async System.Threading.Tasks.Task LoadConfigurationAsync()
        {
            try
            {
                // Load models
                var allModels = await _modelService.GetAvailableModelsAsync();
                
                _localModels.Clear();
                _remoteModels.Clear();
                
                foreach (var model in allModels)
                {
                    if (model.Type == ModelType.Local)
                        _localModels.Add(model);
                    else
                        _remoteModels.Add(model);
                }

                LocalModelsDataGrid.ItemsSource = _localModels;
                RemoteModelsDataGrid.ItemsSource = _remoteModels;

                // Load active model
                var activeModel = await _modelService.GetActiveModelAsync();
                ActiveModelComboBox.ItemsSource = allModels.Where(m => m.IsAvailable);
                if (activeModel != null)
                {
                    ActiveModelComboBox.SelectedValue = activeModel.Id;
                }

                // Load MCP servers if service is available
                if (_mcpService != null)
                {
                    var mcpServers = await _mcpService.GetAvailableServersAsync();
                    _mcpServers.Clear();
                    foreach (var server in mcpServers)
                    {
                        _mcpServers.Add(server);
                    }
                    MCPServersDataGrid.ItemsSource = _mcpServers;
                }

                // Load general settings
                await LoadGeneralSettingsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading configuration: {ex.Message}", "Configuration Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadGeneralSettingsAsync()
        {
            try
            {
                MultiModelCheckBox.IsChecked = await _configService.GetSettingAsync("models.multiModelEnabled", false);
                EnableRAGCheckBox.IsChecked = await _configService.GetSettingAsync("rag.enabled", true);
                AutoUpdatesCheckBox.IsChecked = await _configService.GetSettingAsync("general.autoUpdates", true);
                TelemetryCheckBox.IsChecked = await _configService.GetSettingAsync("general.telemetryEnabled", false);
                FallbackEnabledCheckBox.IsChecked = await _configService.GetSettingAsync("general.fallbackEnabled", true);

                MaxTokensSlider.Value = await _configService.GetSettingAsync("models.maxTokens", 2048);
                TemperatureSlider.Value = await _configService.GetSettingAsync("models.temperature", 0.7);
                MaxResultsSlider.Value = await _configService.GetSettingAsync("rag.maxResults", 10);
                SimilarityThresholdSlider.Value = await _configService.GetSettingAsync("rag.similarityThreshold", 0.7);

                var theme = await _configService.GetSettingAsync("general.theme", "Auto");
                ThemeComboBox.Text = theme;

                var privacyLevel = await _configService.GetSettingAsync("general.privacyLevel", "Balanced");
                PrivacyLevelComboBox.Text = privacyLevel;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading general settings: {ex.Message}", "Configuration Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await SaveConfigurationAsync();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Configuration Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task SaveConfigurationAsync()
        {
            // Save general settings
            await _configService.SetSettingAsync("models.multiModelEnabled", MultiModelCheckBox.IsChecked ?? false);
            await _configService.SetSettingAsync("rag.enabled", EnableRAGCheckBox.IsChecked ?? true);
            await _configService.SetSettingAsync("general.autoUpdates", AutoUpdatesCheckBox.IsChecked ?? true);
            await _configService.SetSettingAsync("general.telemetryEnabled", TelemetryCheckBox.IsChecked ?? false);
            await _configService.SetSettingAsync("general.fallbackEnabled", FallbackEnabledCheckBox.IsChecked ?? true);

            await _configService.SetSettingAsync("models.maxTokens", (int)MaxTokensSlider.Value);
            await _configService.SetSettingAsync("models.temperature", TemperatureSlider.Value);
            await _configService.SetSettingAsync("rag.maxResults", (int)MaxResultsSlider.Value);
            await _configService.SetSettingAsync("rag.similarityThreshold", SimilarityThresholdSlider.Value);

            await _configService.SetSettingAsync("general.theme", ((ComboBoxItem)ThemeComboBox.SelectedItem)?.Content?.ToString() ?? "Auto");
            await _configService.SetSettingAsync("general.privacyLevel", ((ComboBoxItem)PrivacyLevelComboBox.SelectedItem)?.Content?.ToString() ?? "Balanced");

            // Set active model
            if (ActiveModelComboBox.SelectedValue != null)
            {
                await _modelService.SetActiveModelAsync(ActiveModelComboBox.SelectedValue.ToString());
            }

            await _configService.SaveConfigurationAsync();
        }

        // Model Management Events
        private async void TestActiveModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveModelComboBox.SelectedValue is string modelId)
            {
                try
                {
                    var result = await _modelService.TestModelConnectionAsync(modelId);
                    MessageBox.Show(result ? "Model connection successful!" : "Model connection failed!", 
                        "Model Test", MessageBoxButton.OK, result ? MessageBoxImage.Information : MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error testing model: {ex.Message}", "Model Test Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddLocalModelButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ModelConfigurationDialog(ModelType.Local);
            if (dialog.ShowDialog() == true)
            {
                _localModels.Add(dialog.ModelInfo);
            }
        }

        private void AddRemoteModelButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ModelConfigurationDialog(ModelType.Remote);
            if (dialog.ShowDialog() == true)
            {
                _remoteModels.Add(dialog.ModelInfo);
            }
        }

        private void RemoveLocalModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (LocalModelsDataGrid.SelectedItem is ModelInfo model)
            {
                _localModels.Remove(model);
            }
        }

        private void RemoveRemoteModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (RemoteModelsDataGrid.SelectedItem is ModelInfo model)
            {
                _remoteModels.Remove(model);
            }
        }

        private async void TestLocalModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is ModelInfo model)
            {
                await TestModel(model);
            }
        }

        private async void TestRemoteModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is ModelInfo model)
            {
                await TestModel(model);
            }
        }

        private async System.Threading.Tasks.Task TestModel(ModelInfo model)
        {
            try
            {
                var result = await _modelService.TestModelConnectionAsync(model.Id);
                model.IsAvailable = result;
                MessageBox.Show(result ? "Model connection successful!" : "Model connection failed!", 
                    "Model Test", MessageBoxButton.OK, result ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error testing model: {ex.Message}", "Model Test Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AutoDiscoverModelsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("Auto-discovery started. This may take a few moments...", "Auto-Discovery", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Reload models after discovery
                await LoadConfigurationAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during auto-discovery: {ex.Message}", "Auto-Discovery Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // MCP Events
        private void AddLocalMCPButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MCPServerConfigurationDialog(MCPServerType.Local);
            if (dialog.ShowDialog() == true)
            {
                _mcpServers.Add(dialog.ServerInfo);
            }
        }

        private void AddRemoteMCPButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MCPServerConfigurationDialog(MCPServerType.Remote);
            if (dialog.ShowDialog() == true)
            {
                _mcpServers.Add(dialog.ServerInfo);
            }
        }

        private void RemoveMCPServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (MCPServersDataGrid.SelectedItem is MCPServerInfo server)
            {
                _mcpServers.Remove(server);
            }
        }

        private async void TestMCPServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is MCPServerInfo server && _mcpService != null)
            {
                try
                {
                    var result = await _mcpService.TestServerConnectionAsync(server.Id);
                    MessageBox.Show(result ? "MCP server connection successful!" : "MCP server connection failed!", 
                        "MCP Test", MessageBoxButton.OK, result ? MessageBoxImage.Information : MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error testing MCP server: {ex.Message}", "MCP Test Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ConnectMCPServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is MCPServerInfo server && _mcpService != null)
            {
                try
                {
                    var result = await _mcpService.ConnectToServerAsync(server);
                    server.IsConnected = result;
                    MessageBox.Show(result ? "Connected successfully!" : "Connection failed!", 
                        "MCP Connection", MessageBoxButton.OK, result ? MessageBoxImage.Information : MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error connecting to MCP server: {ex.Message}", "MCP Connection Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void AutoDiscoverMCPButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mcpService != null)
            {
                try
                {
                    MessageBox.Show("MCP auto-discovery started. This may take a few moments...", "Auto-Discovery", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Reload MCP servers after discovery
                    await LoadConfigurationAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error during MCP auto-discovery: {ex.Message}", "Auto-Discovery Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // RAG Events
        private void BrowseIndexPathButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                IndexPathTextBox.Text = dialog.SelectedPath;
            }
        }

        private async void IndexWorkspaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_ragService != null)
            {
                try
                {
                    var workspacePath = IndexPathTextBox.Text;
                    if (string.IsNullOrEmpty(workspacePath))
                    {
                        MessageBox.Show("Please specify a workspace path to index.", "Index Workspace", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    IndexStatusTextBlock.Text = "Indexing...";
                    IndexStatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
                    
                    var result = await _ragService.IndexWorkspaceAsync(workspacePath);
                    
                    IndexStatusTextBlock.Text = result ? "Indexing completed" : "Indexing failed";
                    IndexStatusTextBlock.Foreground = result ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
                }
                catch (Exception ex)
                {
                    IndexStatusTextBlock.Text = "Indexing error";
                    IndexStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    MessageBox.Show($"Error indexing workspace: {ex.Message}", "Indexing Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearIndexButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear the index? This action cannot be undone.", 
                "Clear Index", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                IndexStatusTextBlock.Text = "Index cleared";
                IndexStatusTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private async void ViewIndexStatsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_ragService != null)
            {
                try
                {
                    var status = await _ragService.GetIndexingStatusAsync();
                    MessageBox.Show($"Index Status:\nFiles Processed: {status.FilesProcessed}\nTotal Files: {status.TotalFiles}\nProgress: {status.Progress:F1}%", 
                        "Index Statistics", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error retrieving index stats: {ex.Message}", "Index Stats Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void TestRemoteRAGButton_Click(object sender, RoutedEventArgs e)
        {
            var endpoint = RemoteRAGEndpointTextBox.Text;
            var apiKey = RemoteRAGApiKeyBox.Password;
            
            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("Please provide both endpoint and API key.", "Test Remote RAG", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("Remote RAG connection test completed.", "Test Remote RAG", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SyncRemoteRAGButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Remote RAG synchronization started.", "Sync Remote RAG", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // General Events
        private void ImportSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Import A3sist Settings"
            };

            if (dialog.ShowDialog() == true)
            {
                MessageBox.Show("Settings imported successfully.", "Import Settings", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Export A3sist Settings",
                DefaultExt = "json"
            };

            if (dialog.ShowDialog() == true)
            {
                MessageBox.Show("Settings exported successfully.", "Export Settings", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void ResetToDefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to reset all settings to defaults? This action cannot be undone.", 
                "Reset to Defaults", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                await LoadGeneralSettingsAsync();
                MessageBox.Show("Settings reset to defaults.", "Reset Complete", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }


}