using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Windows;
using System.Windows.Controls;
using A3sist.UI.Components.Chat;
using A3sist.UI.Services;
using A3sist.UI.Services.Chat;
using A3sist.UI.ViewModels.Chat;

namespace A3sist.UI.ToolWindows
{
    /// <summary>
    /// Interaction logic for ChatToolWindowControl.xaml
    /// </summary>
    public partial class ChatToolWindowControl : UserControl, IDisposable
    {
        private readonly ILogger<ChatToolWindowControl> _logger;
        private readonly ChatInterfaceViewModel _viewModel;
        private ChatInterfaceControl _chatInterface;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatToolWindowControl"/> class.
        /// </summary>
        public ChatToolWindowControl()
        {
            InitializeComponent();

            try
            {
                // Get services from service locator
                var serviceProvider = EditorServiceRegistration.ServiceLocator;
                _logger = serviceProvider.GetRequiredService<ILogger<ChatToolWindowControl>>();
                var chatService = serviceProvider.GetRequiredService<IChatService>();
                var contextService = serviceProvider.GetRequiredService<IContextService>();
                var viewModelLogger = serviceProvider.GetRequiredService<ILogger<ChatInterfaceViewModel>>();

                // Create and initialize the view model
                _viewModel = new ChatInterfaceViewModel(chatService, contextService, viewModelLogger);
                
                // Create and configure the chat interface
                _chatInterface = new ChatInterfaceControl();
                _chatInterface.DataContext = _viewModel;
                
                // Set the chat interface as the main content
                MainContent.Content = _chatInterface;
                
                // Subscribe to events for scroll handling
                _viewModel.ScrollToBottomRequested += OnScrollToBottomRequested;

                // Initialize the view model
                Loaded += async (s, e) =>
                {
                    try
                    {
                        // Hide loading indicator
                        LoadingGrid.Visibility = Visibility.Collapsed;
                        
                        await _viewModel.Initialize();
                        _logger.LogInformation("Chat tool window initialized successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error initializing chat tool window");
                        LoadingGrid.Visibility = Visibility.Collapsed;
                        ShowErrorMessage($"Failed to initialize chat: {ex.Message}");
                    }
                };

                _logger.LogDebug("ChatToolWindowControl created successfully");
            }
            catch (Exception ex)
            {
                // Fallback error handling if logger isn't available
                System.Diagnostics.Debug.WriteLine($"Error creating ChatToolWindowControl: {ex}");
                ShowErrorMessage($"Failed to initialize chat interface: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles scroll to bottom requests from the view model
        /// </summary>
        private void OnScrollToBottomRequested(object sender, EventArgs e)
        {
            try
            {
                // The ChatInterfaceControl handles its own scrolling
                _chatInterface?.ScrollToBottom();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error scrolling chat to bottom");
            }
        }

        /// <summary>
        /// Shows an error message to the user
        /// </summary>
        private void ShowErrorMessage(string message)
        {
            try
            {
                Dispatcher.InvokeAsync(() =>
                {
                    var errorText = new TextBlock
                    {
                        Text = message,
                        Foreground = System.Windows.Media.Brushes.Red,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(16),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    MainContent.Content = errorText;
                });
            }
            catch
            {
                // Last resort - can't even show error message
                System.Diagnostics.Debug.WriteLine($"Critical error in ChatToolWindowControl: {message}");
            }
        }

        /// <summary>
        /// Gets the chat interface view model
        /// </summary>
        public ChatInterfaceViewModel ViewModel => _viewModel;

        /// <summary>
        /// Scrolls the chat to the bottom
        /// </summary>
        public void ScrollToBottom()
        {
            _chatInterface?.ScrollToBottom();
        }

        /// <summary>
        /// Focuses the chat input
        /// </summary>
        public void FocusInput()
        {
            _chatInterface?.FocusInput();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    if (_viewModel != null)
                    {
                        _viewModel.ScrollToBottomRequested -= OnScrollToBottomRequested;
                        _viewModel.Dispose();
                    }

                    _chatInterface?.Dispose();
                    
                    _logger?.LogDebug("ChatToolWindowControl disposed");
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error disposing ChatToolWindowControl");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}