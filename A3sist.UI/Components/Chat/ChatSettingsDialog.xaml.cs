using A3sist.UI.Services.Chat;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace A3sist.UI.Components.Chat
{
    /// <summary>
    /// Interaction logic for ChatSettingsDialog.xaml
    /// </summary>
    public partial class ChatSettingsDialog : Window
    {
        public ChatSettingsDialogViewModel ViewModel { get; }

        public ChatSettingsDialog(IChatSettingsService settingsService, ILogger<ChatSettingsDialogViewModel> logger)
        {
            InitializeComponent();
            
            ViewModel = new ChatSettingsDialogViewModel(settingsService, logger);
            DataContext = ViewModel;
            
            // Subscribe to commands
            ViewModel.CloseRequested += OnCloseRequested;
        }

        private void OnCloseRequested(object? sender, bool? dialogResult)
        {
            DialogResult = dialogResult;
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            ViewModel.CloseRequested -= OnCloseRequested;
            base.OnClosing(e);
        }
    }

    /// <summary>
    /// ViewModel for the chat settings dialog
    /// </summary>
    public class ChatSettingsDialogViewModel : INotifyPropertyChanged
    {
        private readonly IChatSettingsService _settingsService;
        private readonly ILogger<ChatSettingsDialogViewModel> _logger;
        private ChatSettings _settings;
        private readonly ChatSettings _originalSettings;

        public ChatSettings Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ResetToDefaultsCommand { get; }

        // Events
        public event EventHandler<bool?>? CloseRequested;
        public event PropertyChangedEventHandler? PropertyChanged;

        public ChatSettingsDialogViewModel(
            IChatSettingsService settingsService, 
            ILogger<ChatSettingsDialogViewModel> logger)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Load current settings
            _originalSettings = _settingsService.GetSettings();
            _settings = _originalSettings.Clone();

            // Initialize commands
            SaveCommand = new RelayCommand(async () => await SaveSettingsAsync());
            CancelCommand = new RelayCommand(() => Cancel());
            ResetToDefaultsCommand = new RelayCommand(async () => await ResetToDefaultsAsync());
        }

        /// <summary>
        /// Saves the settings and closes the dialog
        /// </summary>
        private async System.Threading.Tasks.Task SaveSettingsAsync()
        {
            try
            {
                await _settingsService.SaveSettingsAsync(Settings);
                _logger.LogInformation("Chat settings saved successfully");
                
                CloseRequested?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving chat settings");
                MessageBox.Show(
                    $"Error saving settings: {ex.Message}",
                    "Settings Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Cancels the dialog without saving changes
        /// </summary>
        private void Cancel()
        {
            CloseRequested?.Invoke(this, false);
        }

        /// <summary>
        /// Resets settings to defaults
        /// </summary>
        private async System.Threading.Tasks.Task ResetToDefaultsAsync()
        {
            try
            {
                var result = MessageBox.Show(
                    "Are you sure you want to reset all settings to their default values?",
                    "Reset Settings",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await _settingsService.ResetToDefaultsAsync();
                    Settings = _settingsService.GetSettings();
                    _logger.LogInformation("Chat settings reset to defaults");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting chat settings");
                MessageBox.Show(
                    $"Error resetting settings: {ex.Message}",
                    "Settings Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}