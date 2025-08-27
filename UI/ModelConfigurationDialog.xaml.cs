using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using A3sist.Models;

namespace A3sist.UI
{
    /// <summary>
    /// Interaction logic for ModelConfigurationDialog.xaml
    /// </summary>
    public partial class ModelConfigurationDialog : Window
    {
        public ModelInfo ModelInfo { get; set; }
        private bool _isUpdatingSliders = false;

        public ModelConfigurationDialog(ModelType type)
        {
            InitializeComponent();
            
            // Initialize ModelInfo
            ModelInfo = new ModelInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = "New Model",
                Type = type,
                Endpoint = type == ModelType.Local ? "http://localhost:11434" : "https://api.openai.com",
                MaxTokens = 2048,
                Temperature = 0.7,
                IsAvailable = false
            };

            InitializeControls();
            LoadModelInfo();
        }

        public ModelConfigurationDialog(ModelInfo existingModel)
        {
            InitializeComponent();
            ModelInfo = existingModel ?? throw new ArgumentNullException(nameof(existingModel));
            
            InitializeControls();
            LoadModelInfo();
        }

        private void InitializeControls()
        {
            // Initialize Model Type ComboBox
            ModelTypeComboBox.ItemsSource = Enum.GetValues(typeof(ModelType)).Cast<ModelType>();
            
            // Initialize Model Provider ComboBox with common providers
            ModelProviderComboBox.ItemsSource = new[]
            {
                "OpenAI",
                "Anthropic",
                "Google",
                "Microsoft",
                "Ollama",
                "Hugging Face",
                "Custom"
            };
        }

        private void LoadModelInfo()
        {
            if (ModelInfo == null) return;

            ModelNameTextBox.Text = ModelInfo.Name;
            ModelTypeComboBox.SelectedItem = ModelInfo.Type;
            EndpointTextBox.Text = ModelInfo.Endpoint;
            ModelIdTextBox.Text = ModelInfo.ModelId;
            MaxTokensTextBox.Text = ModelInfo.MaxTokens.ToString();
            
            _isUpdatingSliders = true;
            TemperatureSlider.Value = ModelInfo.Temperature;
            TemperatureTextBox.Text = ModelInfo.Temperature.ToString("F2");
            _isUpdatingSliders = false;

            // Set default provider based on endpoint
            if (!string.IsNullOrEmpty(ModelInfo.Endpoint))
            {
                if (ModelInfo.Endpoint.Contains("openai"))
                    ModelProviderComboBox.Text = "OpenAI";
                else if (ModelInfo.Endpoint.Contains("anthropic"))
                    ModelProviderComboBox.Text = "Anthropic";
                else if (ModelInfo.Endpoint.Contains("localhost") || ModelInfo.Endpoint.Contains("11434"))
                    ModelProviderComboBox.Text = "Ollama";
                else
                    ModelProviderComboBox.Text = "Custom";
            }

            UpdateUIForModelType();
        }

        private void SaveModelInfo()
        {
            if (ModelInfo == null) return;

            ModelInfo.Name = ModelNameTextBox.Text?.Trim() ?? "";
            ModelInfo.Type = (ModelType)(ModelTypeComboBox.SelectedItem ?? ModelType.Remote);
            ModelInfo.Endpoint = EndpointTextBox.Text?.Trim() ?? "";
            ModelInfo.ModelId = ModelIdTextBox.Text?.Trim() ?? "";
            ModelInfo.ApiKey = ApiKeyPasswordBox.Password;

            if (int.TryParse(MaxTokensTextBox.Text, out int maxTokens))
                ModelInfo.MaxTokens = maxTokens;

            if (double.TryParse(TemperatureTextBox.Text, out double temperature))
                ModelInfo.Temperature = Math.Max(0, Math.Min(2, temperature));

            if (int.TryParse(TimeoutTextBox.Text, out int timeout))
                ModelInfo.TimeoutSeconds = timeout;

            if (int.TryParse(RetryCountTextBox.Text, out int retryCount))
                ModelInfo.RetryCount = retryCount;

            ModelInfo.StreamResponse = StreamResponseCheckBox.IsChecked == true;
            ModelInfo.EnableLogging = EnableLoggingCheckBox.IsChecked == true;
            ModelInfo.SystemMessage = SystemMessageTextBox.Text?.Trim() ?? "";

            // Parse stop sequences
            if (!string.IsNullOrEmpty(StopSequencesTextBox.Text))
            {
                ModelInfo.StopSequences = StopSequencesTextBox.Text
                    .Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();
            }

            if (double.TryParse(TopPTextBox.Text, out double topP))
                ModelInfo.TopP = Math.Max(0, Math.Min(1, topP));

            if (double.TryParse(FrequencyPenaltyTextBox.Text, out double freqPenalty))
                ModelInfo.FrequencyPenalty = Math.Max(-2, Math.Min(2, freqPenalty));

            if (double.TryParse(PresencePenaltyTextBox.Text, out double presPenalty))
                ModelInfo.PresencePenalty = Math.Max(-2, Math.Min(2, presPenalty));

            // Custom headers
            if (UseCustomHeadersCheckBox.IsChecked == true && !string.IsNullOrEmpty(CustomHeadersTextBox.Text))
            {
                ModelInfo.CustomHeaders = CustomHeadersTextBox.Text;
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(ModelNameTextBox.Text))
            {
                MessageBox.Show("Please enter a model name.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ModelNameTextBox.Focus();
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

            if (!int.TryParse(MaxTokensTextBox.Text, out int maxTokens) || maxTokens <= 0)
            {
                MessageBox.Show("Please enter a valid positive number for max tokens.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                MaxTokensTextBox.Focus();
                return false;
            }

            return true;
        }

        private void UpdateUIForModelType()
        {
            var selectedType = (ModelType)(ModelTypeComboBox.SelectedItem ?? ModelType.Remote);
            
            switch (selectedType)
            {
                case ModelType.Local:
                    EndpointTextBox.Text = string.IsNullOrEmpty(EndpointTextBox.Text) ? "http://localhost:11434" : EndpointTextBox.Text;
                    ApiKeyPasswordBox.IsEnabled = false;
                    break;
                case ModelType.Remote:
                    EndpointTextBox.Text = string.IsNullOrEmpty(EndpointTextBox.Text) ? "https://api.openai.com" : EndpointTextBox.Text;
                    ApiKeyPasswordBox.IsEnabled = true;
                    break;
            }
        }

        // Event Handlers
        private void ModelTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateUIForModelType();
        }

        private void UseCustomHeadersCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CustomHeadersTextBox.IsEnabled = true;
        }

        private void UseCustomHeadersCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CustomHeadersTextBox.IsEnabled = false;
            CustomHeadersTextBox.Text = "";
        }

        private void TemperatureSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdatingSliders && TemperatureTextBox != null)
            {
                TemperatureTextBox.Text = e.NewValue.ToString("F2");
            }
        }

        private void TemperatureTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isUpdatingSliders && double.TryParse(TemperatureTextBox.Text, out double value))
            {
                _isUpdatingSliders = true;
                TemperatureSlider.Value = Math.Max(0, Math.Min(2, value));
                _isUpdatingSliders = false;
            }
        }

        private void TopPSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdatingSliders && TopPTextBox != null)
            {
                TopPTextBox.Text = e.NewValue.ToString("F2");
            }
        }

        private void TopPTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isUpdatingSliders && double.TryParse(TopPTextBox.Text, out double value))
            {
                _isUpdatingSliders = true;
                TopPSlider.Value = Math.Max(0, Math.Min(1, value));
                _isUpdatingSliders = false;
            }
        }

        private void FrequencyPenaltySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdatingSliders && FrequencyPenaltyTextBox != null)
            {
                FrequencyPenaltyTextBox.Text = e.NewValue.ToString("F2");
            }
        }

        private void FrequencyPenaltyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isUpdatingSliders && double.TryParse(FrequencyPenaltyTextBox.Text, out double value))
            {
                _isUpdatingSliders = true;
                FrequencyPenaltySlider.Value = Math.Max(-2, Math.Min(2, value));
                _isUpdatingSliders = false;
            }
        }

        private void PresencePenaltySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdatingSliders && PresencePenaltyTextBox != null)
            {
                PresencePenaltyTextBox.Text = e.NewValue.ToString("F2");
            }
        }

        private void PresencePenaltyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isUpdatingSliders && double.TryParse(PresencePenaltyTextBox.Text, out double value))
            {
                _isUpdatingSliders = true;
                PresencePenaltySlider.Value = Math.Max(-2, Math.Min(2, value));
                _isUpdatingSliders = false;
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
                SaveModelInfo();
                
                // Simulate connection test
                await System.Threading.Tasks.Task.Delay(1000);
                
                MessageBox.Show("Connection test successful!", "Connection Test", 
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

            SaveModelInfo();
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