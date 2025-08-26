using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Wpf;

namespace A3sist.UI.VSIX.ToolWindows
{
    /// <summary>
    /// Interaction logic for A3sistToolWindowControl.xaml
    /// </summary>
    public partial class A3sistToolWindowControl : UserControl, IDisposable
    {
        private readonly ILogger<A3sistToolWindowControl>? _logger;
        private WebView2? _webView;
        private bool _disposed;

        public A3sistToolWindowControl(ILogger<A3sistToolWindowControl>? logger = null)
        {
            _logger = logger;
            InitializeComponent();
            InitializeWebView();
        }

        private void InitializeComponent()
        {
            // Create the main grid
            var grid = new Grid();
            
            // Add status bar
            var statusBar = new Border
            {
                Background = System.Windows.Media.Brushes.LightGray,
                Height = 25,
                VerticalAlignment = VerticalAlignment.Top
            };
            
            var statusText = new TextBlock
            {
                Text = "A3sist - Loading...",
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center
            };
            
            statusBar.Child = statusText;
            grid.Children.Add(statusBar);

            // Add WebView2 placeholder (will be replaced with actual WebView2)
            var webViewContainer = new Border
            {
                Margin = new Thickness(0, 25, 0, 0),
                Background = System.Windows.Media.Brushes.White
            };

            var loadingText = new TextBlock
            {
                Text = "Initializing A3sist MAUI interface...",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14,
                Foreground = System.Windows.Media.Brushes.Gray
            };

            webViewContainer.Child = loadingText;
            grid.Children.Add(webViewContainer);

            this.Content = grid;
        }

        private async void InitializeWebView()
        {
            try
            {
                _logger?.LogInformation("Initializing WebView2 for A3sist tool window");

                // Create WebView2 control
                _webView = new WebView2();
                
                // Wait for WebView2 to be ready
                await _webView.EnsureCoreWebView2Async();

                // Configure WebView2
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;

                // Navigate to MAUI application URL (placeholder for now)
                var mauiUrl = "about:blank"; // Will be replaced with actual MAUI app URL
                await _webView.CoreWebView2.NavigateToStringAsync(@"
                    <html>
                    <head><title>A3sist</title></head>
                    <body style='font-family: Segoe UI; padding: 20px; background: #f5f5f5;'>
                        <h1>A3sist AI Assistant</h1>
                        <p>Modern MAUI interface loading...</p>
                        <div style='margin-top: 20px; padding: 15px; background: white; border-radius: 5px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                            <h3>Features:</h3>
                            <ul>
                                <li>AI-powered code assistance</li>
                                <li>Multi-agent architecture</li>
                                <li>Real-time chat interface</li>
                                <li>Context-aware suggestions</li>
                            </ul>
                        </div>
                    </body>
                    </html>");

                // Replace the loading control with WebView2
                if (this.Content is Grid grid && grid.Children.Count > 1)
                {
                    grid.Children.RemoveAt(1); // Remove loading container
                    
                    var webViewBorder = new Border
                    {
                        Margin = new Thickness(0, 25, 0, 0),
                        Child = _webView
                    };
                    
                    grid.Children.Add(webViewBorder);

                    // Update status
                    if (grid.Children[0] is Border statusBorder && statusBorder.Child is TextBlock statusText)
                    {
                        statusText.Text = "A3sist - Ready";
                    }
                }

                _logger?.LogInformation("WebView2 initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize WebView2");
                
                // Show error in the control
                if (this.Content is Grid grid && grid.Children.Count > 1 && grid.Children[1] is Border errorContainer)
                {
                    errorContainer.Child = new TextBlock
                    {
                        Text = $"Error loading A3sist interface: {ex.Message}",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(10),
                        Foreground = System.Windows.Media.Brushes.Red
                    };
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                _logger?.LogInformation("Disposing A3sistToolWindowControl");
                _webView?.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing A3sistToolWindowControl");
            }

            _disposed = true;
        }
    }
}