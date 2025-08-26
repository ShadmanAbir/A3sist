using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace A3sist.Chat.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow>? _logger;
        private readonly IChatService? _chatService;

        public MainWindow()
        {
            InitializeComponent();
            
            // Get services from app
            var app = (App)Application.Current;
            if (app.Services != null)
            {
                _logger = app.Services.GetService<ILogger<MainWindow>>();
                _chatService = app.Services.GetService<IChatService>();
            }
            
            // Set up the main content
            InitializeChatView();
            
            _logger?.LogInformation("MainWindow initialized successfully");
        }

        private void InitializeChatView()
        {
            try
            {
                // Create the chat view
                var chatView = new Views.ChatView(_chatService);
                ContentBorder.Child = chatView;
                
                UpdateKnowledgeStatus("Ready");
                _logger?.LogInformation("Chat view initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize chat view");
                var errorMessage = $"Failed to initialize chat view: {ex.Message}";
                MessageBox.Show(errorMessage, "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void UpdateKnowledgeStatus(string status)
        {
            // Update UI with knowledge status
            Title = $"A3sist Chat Desktop - {status}";
            StatusText.Text = status;
        }
    }
}