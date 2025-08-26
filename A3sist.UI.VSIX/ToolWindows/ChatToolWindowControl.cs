using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;

namespace A3sist.UI.VSIX.ToolWindows
{
    /// <summary>
    /// Interaction logic for ChatToolWindowControl
    /// </summary>
    public partial class ChatToolWindowControl : UserControl, IDisposable
    {
        private readonly ILogger<ChatToolWindowControl>? _logger;
        private bool _disposed;

        public ChatToolWindowControl(ILogger<ChatToolWindowControl>? logger = null)
        {
            _logger = logger;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Create the main grid
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header
            var header = new Border
            {
                Background = System.Windows.Media.Brushes.DarkBlue,
                Height = 40
            };

            var headerText = new TextBlock
            {
                Text = "🤖 A3sist Chat Assistant",
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(10),
                VerticalAlignment = VerticalAlignment.Center
            };

            header.Child = headerText;
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // Chat area
            var chatScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(5)
            };

            var chatStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            // Add welcome message
            var welcomeMessage = CreateChatMessage("Welcome to A3sist! I'm your AI coding assistant. How can I help you today?", true);
            chatStackPanel.Children.Add(welcomeMessage);

            // Add sample conversation
            var userMessage = CreateChatMessage("Can you help me refactor this code?", false);
            chatStackPanel.Children.Add(userMessage);

            var assistantMessage = CreateChatMessage("Of course! Please share the code you'd like me to help refactor, and I'll analyze it and provide suggestions for improvement.", true);
            chatStackPanel.Children.Add(assistantMessage);

            chatScrollViewer.Content = chatStackPanel;
            Grid.SetRow(chatScrollViewer, 1);
            grid.Children.Add(chatScrollViewer);

            // Input area
            var inputGrid = new Grid
            {
                Margin = new Thickness(5),
                Height = 60
            };
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var inputTextBox = new TextBox
            {
                Text = "Type your message here...",
                FontSize = 12,
                Padding = new Thickness(8),
                VerticalContentAlignment = VerticalAlignment.Top,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                Foreground = System.Windows.Media.Brushes.Gray
            };

            // Handle placeholder text
            inputTextBox.GotFocus += (s, e) =>
            {
                if (inputTextBox.Text == "Type your message here...")
                {
                    inputTextBox.Text = "";
                    inputTextBox.Foreground = System.Windows.Media.Brushes.Black;
                }
            };

            inputTextBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(inputTextBox.Text))
                {
                    inputTextBox.Text = "Type your message here...";
                    inputTextBox.Foreground = System.Windows.Media.Brushes.Gray;
                }
            };

            Grid.SetColumn(inputTextBox, 0);
            inputGrid.Children.Add(inputTextBox);

            var sendButton = new Button
            {
                Content = "Send",
                Width = 60,
                Margin = new Thickness(5, 0, 0, 0),
                Background = System.Windows.Media.Brushes.DarkBlue,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold
            };

            sendButton.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(inputTextBox.Text) && inputTextBox.Text != "Type your message here...")
                {
                    var userMsg = CreateChatMessage(inputTextBox.Text, false);
                    chatStackPanel.Children.Add(userMsg);
                    
                    inputTextBox.Text = "";
                    
                    // Simulate AI response
                    var aiResponse = CreateChatMessage("I'm processing your request. This is a placeholder response from the A3sist chat interface.", true);
                    chatStackPanel.Children.Add(aiResponse);
                    
                    // Scroll to bottom
                    chatScrollViewer.ScrollToEnd();
                    
                    _logger?.LogInformation("Chat message sent");
                }
            };

            // Handle Enter key
            inputTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter && !System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
                {
                    sendButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    e.Handled = true;
                }
            };

            Grid.SetColumn(sendButton, 1);
            inputGrid.Children.Add(sendButton);

            Grid.SetRow(inputGrid, 2);
            grid.Children.Add(inputGrid);

            this.Content = grid;
        }

        private Border CreateChatMessage(string text, bool isAssistant)
        {
            var border = new Border
            {
                Margin = new Thickness(5, 2, 5, 2),
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(8),
                MaxWidth = 300,
                Background = isAssistant ? 
                    System.Windows.Media.Brushes.LightBlue : 
                    System.Windows.Media.Brushes.LightGreen,
                HorizontalAlignment = isAssistant ? 
                    HorizontalAlignment.Left : 
                    HorizontalAlignment.Right
            };

            var textBlock = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = System.Windows.Media.Brushes.Black
            };

            border.Child = textBlock;
            return border;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                _logger?.LogInformation("Disposing ChatToolWindowControl");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing ChatToolWindowControl");
            }

            _disposed = true;
        }
    }
}