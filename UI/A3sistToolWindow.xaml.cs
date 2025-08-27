using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using A3sist.Models;
using A3sist.Services;
using A3sist.UI;
using Microsoft.VisualStudio.Shell;

namespace A3sist.UI
{
    /// <summary>
    /// Interaction logic for A3sistToolWindow.xaml
    /// </summary>
    public partial class A3sistToolWindow : UserControl
    {
        private IModelManagementService _modelService;
        private IAutoCompleteService _autoCompleteService;
        private IA3sistConfigurationService _configService;
        private IChatService _chatService;
        private IRefactoringService _refactoringService;
        private A3sist.Agent.IAgentModeService _agentService;
        private bool _isAgentRunning = false;

        public A3sistToolWindow()
        {
            InitializeComponent();
            Loaded += A3sistToolWindow_Loaded;
        }

        private async void A3sistToolWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get services from the package
                await InitializeServicesAsync();
                await UpdateUIAsync();
                await LoadSettingsAsync();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading: {ex.Message}", Colors.Red);
            }
        }

        private async Task InitializeServicesAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            
            try
            {
                var package = A3sistPackage.Instance;
                if (package != null)
                {
                    _modelService = package.GetService<IModelManagementService>();
                    _autoCompleteService = package.GetService<IAutoCompleteService>();
                    _configService = package.GetService<IA3sistConfigurationService>();
                    _chatService = package.GetService<IChatService>();
                    _refactoringService = package.GetService<IRefactoringService>();
                    _agentService = package.GetService<A3sist.Agent.IAgentModeService>();

                    // Subscribe to agent events if available
                    if (_agentService != null)
                    {
                        _agentService.ProgressChanged += AgentService_ProgressChanged;
                        _agentService.AnalysisCompleted += AgentService_AnalysisCompleted;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist ToolWindow: Error initializing services: {ex.Message}");
            }
        }

        private async Task UpdateUIAsync()
        {
            try
            {
                // Update active model
                await UpdateActiveModelAsync();
                
                // Update autocomplete status
                await UpdateAutoCompleteStatusAsync();
                
                // Update agent status
                await UpdateAgentStatusAsync();

                UpdateStatus("Ready", Colors.Green);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Update error: {ex.Message}", Colors.Red);
            }
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                if (_configService != null)
                {
                    var ragEnabled = await _configService.GetSettingAsync("rag.enabled", true);
                    RAGEnabledCheckBox.IsChecked = ragEnabled;

                    var realTimeAnalysis = await _configService.GetSettingAsync("analysis.realTimeAnalysis", true);
                    RealTimeAnalysisCheckBox.IsChecked = realTimeAnalysis;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist ToolWindow: Error loading settings: {ex.Message}");
            }
        }

        private async Task UpdateActiveModelAsync()
        {
            try
            {
                if (_modelService != null)
                {
                    var activeModel = await _modelService.GetActiveModelAsync();
                    var availableModels = await _modelService.GetAvailableModelsAsync();

                    // Update model combo box
                    ModelComboBox.ItemsSource = availableModels.Where(m => m.IsAvailable);
                    ModelComboBox.DisplayMemberPath = "Name";
                    ModelComboBox.SelectedValuePath = "Id";

                    if (activeModel != null)
                    {
                        ActiveModelText.Text = $"{activeModel.Name} ({activeModel.Provider})";
                        ModelStatusIndicator.Fill = activeModel.IsAvailable ? Brushes.Green : Brushes.Red;
                        ModelComboBox.SelectedValue = activeModel.Id;
                    }
                    else
                    {
                        ActiveModelText.Text = "No model selected";
                        ModelStatusIndicator.Fill = Brushes.Gray;
                    }
                }
            }
            catch (Exception ex)
            {
                ActiveModelText.Text = "Error loading model";
                ModelStatusIndicator.Fill = Brushes.Red;
                System.Diagnostics.Debug.WriteLine($"A3sist ToolWindow: Error updating active model: {ex.Message}");
            }
        }

        private async Task UpdateAutoCompleteStatusAsync()
        {
            try
            {
                if (_autoCompleteService != null)
                {
                    var isEnabled = await _autoCompleteService.IsAutoCompleteEnabledAsync();
                    AutoCompleteCheckBox.IsChecked = isEnabled;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"A3sist ToolWindow: Error updating autocomplete status: {ex.Message}");
            }
        }

        private async Task UpdateAgentStatusAsync()
        {
            try
            {
                if (_agentService != null)
                {
                    _isAgentRunning = await _agentService.IsAnalysisRunningAsync();
                    
                    if (_isAgentRunning)
                    {
                        AgentStatusText.Text = "Running analysis...";
                        ToggleAgentButton.Content = "Stop";
                        AgentProgressBar.Visibility = Visibility.Visible;
                        AgentProgressText.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        AgentStatusText.Text = "Not running";
                        ToggleAgentButton.Content = "Start";
                        AgentProgressBar.Visibility = Visibility.Collapsed;
                        AgentProgressText.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                AgentStatusText.Text = "Agent unavailable";
                System.Diagnostics.Debug.WriteLine($"A3sist ToolWindow: Error updating agent status: {ex.Message}");
            }
        }

        private void UpdateStatus(string message, Color color)
        {
            StatusText.Text = message;
            StatusIndicator.Fill = new SolidColorBrush(color);
        }

        // Event Handlers
        private async void OpenChatButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_chatService != null && _modelService != null && _configService != null)
                {
                    var chatWindow = new ChatWindow(_chatService, _modelService, _configService);
                    chatWindow.Show();
                }
                else
                {
                    UpdateStatus("Chat service unavailable", Colors.Red);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error opening chat: {ex.Message}", Colors.Red);
            }
        }

        private async void RefactorCodeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_refactoringService != null)
                {
                    UpdateStatus("Getting refactoring suggestions...", Colors.Orange);
                    
                    // This would typically get the selected code from the active editor
                    // For now, we'll show a message
                    UpdateStatus("Select code in editor first", Colors.Orange);
                }
                else
                {
                    UpdateStatus("Refactoring service unavailable", Colors.Red);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error with refactoring: {ex.Message}", Colors.Red);
            }
        }

        private void ConfigureButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_configService != null && _modelService != null)
                {
                    var configWindow = new ConfigurationWindow(_configService, _modelService, null, null);
                    var result = configWindow.ShowDialog();
                    
                    if (result == true)
                    {
                        // Reload settings and UI after configuration changes
                        _ = Task.Run(async () =>
                        {
                            await Dispatcher.InvokeAsync(async () =>
                            {
                                await UpdateUIAsync();
                                await LoadSettingsAsync();
                            });
                        });
                    }
                }
                else
                {
                    UpdateStatus("Configuration service unavailable", Colors.Red);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error opening configuration: {ex.Message}", Colors.Red);
            }
        }

        private async void ToggleAgentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_agentService != null)
                {
                    if (_isAgentRunning)
                    {
                        await _agentService.StopAnalysisAsync();
                        UpdateStatus("Agent stopped", Colors.Orange);
                    }
                    else
                    {
                        // Start agent analysis on current workspace
                        // This would typically get the current solution path
                        var workspacePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        var started = await _agentService.StartAnalysisAsync(workspacePath);
                        
                        if (started)
                        {
                            UpdateStatus("Agent started", Colors.Green);
                        }
                        else
                        {
                            UpdateStatus("Failed to start agent", Colors.Red);
                        }
                    }
                    
                    await UpdateAgentStatusAsync();
                }
                else
                {
                    UpdateStatus("Agent service unavailable", Colors.Red);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error with agent: {ex.Message}", Colors.Red);
            }
        }

        private async void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ModelComboBox.SelectedValue is string selectedModelId && _modelService != null)
                {
                    var success = await _modelService.SetActiveModelAsync(selectedModelId);
                    if (success)
                    {
                        await UpdateActiveModelAsync();
                        UpdateStatus("Model changed", Colors.Green);
                    }
                    else
                    {
                        UpdateStatus("Failed to change model", Colors.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error changing model: {ex.Message}", Colors.Red);
            }
        }

        private async void AutoCompleteCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_autoCompleteService != null)
                {
                    await _autoCompleteService.SetAutoCompleteEnabledAsync(true);
                    UpdateStatus("AutoComplete enabled", Colors.Green);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error enabling AutoComplete: {ex.Message}", Colors.Red);
            }
        }

        private async void AutoCompleteCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_autoCompleteService != null)
                {
                    await _autoCompleteService.SetAutoCompleteEnabledAsync(false);
                    UpdateStatus("AutoComplete disabled", Colors.Orange);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error disabling AutoComplete: {ex.Message}", Colors.Red);
            }
        }

        // Agent Service Event Handlers
        private void AgentService_ProgressChanged(object sender, A3sist.Agent.AgentProgressEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                AgentProgressBar.Value = e.Progress;
                AgentProgressText.Text = $"{e.Message} ({e.Current}/{e.Total})";
                UpdateStatus($"Agent: {e.Message}", Colors.Blue);
            });
        }

        private void AgentService_AnalysisCompleted(object sender, A3sist.Agent.AgentAnalysisCompletedEventArgs e)
        {
            Dispatcher.InvokeAsync(async () =>
            {
                if (string.IsNullOrEmpty(e.Error))
                {
                    UpdateStatus($"Analysis complete: {e.Report.TotalIssues} issues found", Colors.Green);
                }
                else
                {
                    UpdateStatus($"Analysis failed: {e.Error}", Colors.Red);
                }
                
                await UpdateAgentStatusAsync();
            });
        }
    }
}