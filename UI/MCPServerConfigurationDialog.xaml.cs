using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using A3sist.Models;

namespace A3sist.UI
{
    /// <summary>
    /// Interaction logic for MCPServerConfigurationDialog.xaml
    /// </summary>
    public partial class MCPServerConfigurationDialog : Window
    {
        public MCPServerInfo ServerInfo { get; set; }
        private List<string> _supportedTools;

        public MCPServerConfigurationDialog(MCPServerType type)
        {
            InitializeComponent();
            
            // Initialize ServerInfo
            ServerInfo = new MCPServerInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = "New MCP Server",
                Type = type,
                Endpoint = type == MCPServerType.Local ? "http://localhost:8080/mcp" : "https://api.example.com/mcp",
                Port = type == MCPServerType.Local ? 8080 : 443,
                IsConnected = false,
                SupportedTools = new List<string>()
            };

            InitializeControls();
            LoadServerInfo();
        }

        public MCPServerConfigurationDialog(MCPServerInfo existingServer)
        {
            InitializeComponent();
            ServerInfo = existingServer ?? throw new ArgumentNullException(nameof(existingServer));
            
            InitializeControls();
            LoadServerInfo();
        }

        private void InitializeControls()
        {
            // Initialize Server Type ComboBox
            ServerTypeComboBox.ItemsSource = Enum.GetValues(typeof(MCPServerType)).Cast<MCPServerType>();
            
            // Initialize supported tools list
            _supportedTools = new List<string>();
            
            // Add common MCP tools as examples
            var commonTools = new[]
            {
                "filesystem",
                "git",
                "web_search",
                "database",
                "code_execution",
                "image_generation",
                "text_processing",
                "api_client",
                "file_watcher",
                "terminal"
            };
            
            foreach (var tool in commonTools)
            {
                _supportedTools.Add(tool);
            }
            
            UpdateToolsListBox();
        }

        private void LoadServerInfo()
        {
            if (ServerInfo == null) return;

            ServerNameTextBox.Text = ServerInfo.Name;
            ServerTypeComboBox.SelectedItem = ServerInfo.Type;
            DescriptionTextBox.Text = ServerInfo.Description;
            EndpointTextBox.Text = ServerInfo.Endpoint;
            PortTextBox.Text = ServerInfo.Port.ToString();
            
            // Set protocol based on endpoint
            if (!string.IsNullOrEmpty(ServerInfo.Endpoint))
            {
                if (ServerInfo.Endpoint.StartsWith("https://"))
                    ProtocolComboBox.Text = "HTTPS";
                else if (ServerInfo.Endpoint.StartsWith("http://"))
                    ProtocolComboBox.Text = "HTTP";
                else if (ServerInfo.Endpoint.StartsWith("ws://") || ServerInfo.Endpoint.StartsWith("wss://"))
                    ProtocolComboBox.Text = "WebSocket";
                else
                    ProtocolComboBox.Text = "HTTP";
            }

            // Authentication
            RequiresAuthCheckBox.IsChecked = ServerInfo.RequiresAuth;
            UsernameTextBox.Text = ServerInfo.Username;
            if (!string.IsNullOrEmpty(ServerInfo.Password))
                PasswordBox.Password = ServerInfo.Password;
            if (!string.IsNullOrEmpty(ServerInfo.ApiKey))
                ApiKeyPasswordBox.Password = ServerInfo.ApiKey;

            // Advanced settings
            TimeoutTextBox.Text = ServerInfo.TimeoutSeconds.ToString();
            RetryCountTextBox.Text = ServerInfo.RetryCount.ToString();
            MaxConcurrentTextBox.Text = ServerInfo.MaxConcurrentRequests.ToString();
            KeepAliveTextBox.Text = ServerInfo.KeepAliveInterval.ToString();
            EnableLoggingCheckBox.IsChecked = ServerInfo.EnableLogging;
            AutoReconnectCheckBox.IsChecked = ServerInfo.AutoReconnect;
            CustomHeadersTextBox.Text = ServerInfo.CustomHeaders;
            EnvironmentVarsTextBox.Text = ServerInfo.EnvironmentVariables;

            // Load supported tools
            if (ServerInfo.SupportedTools != null)
            {
                _supportedTools.Clear();
                _supportedTools.AddRange(ServerInfo.SupportedTools);
                UpdateToolsListBox();
            }

            UpdateUIForServerType();
        }

        private void SaveServerInfo()
        {
            if (ServerInfo == null) return;

            ServerInfo.Name = ServerNameTextBox.Text?.Trim() ?? "";
            ServerInfo.Type = (MCPServerType)(ServerTypeComboBox.SelectedItem ?? MCPServerType.Remote);
            ServerInfo.Description = DescriptionTextBox.Text?.Trim() ?? "";
            ServerInfo.Endpoint = EndpointTextBox.Text?.Trim() ?? "";

            if (int.TryParse(PortTextBox.Text, out int port))
                ServerInfo.Port = port;

            ServerInfo.Protocol = ProtocolComboBox.Text ?? "HTTP";
            ServerInfo.RequiresAuth = RequiresAuthCheckBox.IsChecked == true;
            ServerInfo.Username = UsernameTextBox.Text?.Trim() ?? "";
            ServerInfo.Password = PasswordBox.Password;
            ServerInfo.ApiKey = ApiKeyPasswordBox.Password;

            // Advanced settings
            if (int.TryParse(TimeoutTextBox.Text, out int timeout))
                ServerInfo.TimeoutSeconds = timeout;

            if (int.TryParse(RetryCountTextBox.Text, out int retryCount))
                ServerInfo.RetryCount = retryCount;

            if (int.TryParse(MaxConcurrentTextBox.Text, out int maxConcurrent))
                ServerInfo.MaxConcurrentRequests = maxConcurrent;

            if (int.TryParse(KeepAliveTextBox.Text, out int keepAlive))
                ServerInfo.KeepAliveInterval = keepAlive;

            ServerInfo.EnableLogging = EnableLoggingCheckBox.IsChecked == true;
            ServerInfo.AutoReconnect = AutoReconnectCheckBox.IsChecked == true;
            ServerInfo.CustomHeaders = CustomHeadersTextBox.Text?.Trim() ?? "";
            ServerInfo.EnvironmentVariables = EnvironmentVarsTextBox.Text?.Trim() ?? "";

            // Save supported tools
            ServerInfo.SupportedTools = new List<string>(_supportedTools);
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(ServerNameTextBox.Text))
            {
                MessageBox.Show("Please enter a server name.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ServerNameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(EndpointTextBox.Text))
            {
                MessageBox.Show("Please enter an endpoint URL.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EndpointTextBox.Focus();
                return false;
            }

            if (!Uri.TryCreate(EndpointTextBox.Text, UriKind.Absolute, out Uri result))
            {
                MessageBox.Show("Please enter a valid endpoint URL.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EndpointTextBox.Focus();
                return false;
            }

            if (!int.TryParse(PortTextBox.Text, out int port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("Please enter a valid port number (1-65535).", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PortTextBox.Focus();
                return false;
            }

            return true;
        }

        private void UpdateUIForServerType()
        {
            var selectedType = (MCPServerType)(ServerTypeComboBox.SelectedItem ?? MCPServerType.Remote);
            
            switch (selectedType)
            {
                case MCPServerType.Local:
                    if (string.IsNullOrEmpty(EndpointTextBox.Text) || EndpointTextBox.Text.Contains("api.example.com"))
                        EndpointTextBox.Text = "http://localhost:8080/mcp";
                    if (string.IsNullOrEmpty(PortTextBox.Text) || PortTextBox.Text == "443")
                        PortTextBox.Text = "8080";
                    ProtocolComboBox.Text = "HTTP";
                    RequiresAuthCheckBox.IsChecked = false;
                    AuthenticationPanel.IsEnabled = false;
                    break;
                    
                case MCPServerType.Remote:
                    if (string.IsNullOrEmpty(EndpointTextBox.Text) || EndpointTextBox.Text.Contains("localhost"))
                        EndpointTextBox.Text = "https://api.example.com/mcp";
                    if (string.IsNullOrEmpty(PortTextBox.Text) || PortTextBox.Text == "8080")
                        PortTextBox.Text = "443";
                    ProtocolComboBox.Text = "HTTPS";
                    break;
            }
        }

        private void UpdateToolsListBox()
        {
            SupportedToolsListBox.ItemsSource = null;
            SupportedToolsListBox.ItemsSource = _supportedTools;
        }

        // Event Handlers
        private void ServerTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateUIForServerType();
        }

        private void RequiresAuthCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            AuthenticationPanel.IsEnabled = true;
        }

        private void RequiresAuthCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            AuthenticationPanel.IsEnabled = false;
            UsernameTextBox.Text = "";
            PasswordBox.Password = "";
        }

        private void AddToolButton_Click(object sender, RoutedEventArgs e)
        {
            AddTool();
        }

        private void NewToolNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTool();
                e.Handled = true;
            }
        }

        private void AddTool()
        {
            var toolName = NewToolNameTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(toolName) && !_supportedTools.Contains(toolName, StringComparer.OrdinalIgnoreCase))
            {
                _supportedTools.Add(toolName);
                UpdateToolsListBox();
                NewToolNameTextBox.Clear();
                NewToolNameTextBox.Focus();
            }
        }

        private void RemoveToolButton_Click(object sender, RoutedEventArgs e)
        {
            if (SupportedToolsListBox.SelectedItem is string selectedTool)
            {
                _supportedTools.Remove(selectedTool);
                UpdateToolsListBox();
            }
        }

        private async void AutoDiscoverButton_Click(object sender, RoutedEventArgs e)
        {
            AutoDiscoverButton.IsEnabled = false;
            AutoDiscoverButton.Content = "Discovering...";

            try
            {
                // Simulate auto-discovery
                await System.Threading.Tasks.Task.Delay(1500);
                
                var discoveredTools = new[]
                {
                    "file_operations",
                    "code_analysis",
                    "git_integration",
                    "web_requests"
                };

                foreach (var tool in discoveredTools)
                {
                    if (!_supportedTools.Contains(tool, StringComparer.OrdinalIgnoreCase))
                    {
                        _supportedTools.Add(tool);
                    }
                }

                UpdateToolsListBox();
                MessageBox.Show($"Auto-discovery completed. Found {discoveredTools.Length} tools.", 
                    "Auto-Discovery", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Auto-discovery failed: {ex.Message}", "Auto-Discovery Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                AutoDiscoverButton.IsEnabled = true;
                AutoDiscoverButton.Content = "Auto-Discover Tools";
            }
        }

        private void LoadTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            var templateDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Load MCP Server Template"
            };

            if (templateDialog.ShowDialog() == true)
            {
                try
                {
                    // Simulate loading template
                    var templateTools = new[]
                    {
                        "template_tool_1",
                        "template_tool_2",
                        "template_tool_3"
                    };

                    _supportedTools.Clear();
                    _supportedTools.AddRange(templateTools);
                    UpdateToolsListBox();

                    MessageBox.Show("Template loaded successfully.", "Load Template", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load template: {ex.Message}", "Load Template Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            TestConnectionButton.IsEnabled = false;
            TestConnectionButton.Content = "Testing...";

            try
            {
                SaveServerInfo();
                
                // Simulate connection test
                await System.Threading.Tasks.Task.Delay(1000);
                
                MessageBox.Show("MCP server connection test successful!", "Connection Test", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection test failed: {ex.Message}", "Connection Test", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                TestConnectionButton.IsEnabled = true;
                TestConnectionButton.Content = "Test Connection";
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            SaveServerInfo();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}