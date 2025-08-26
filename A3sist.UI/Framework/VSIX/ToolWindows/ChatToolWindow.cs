#if NET472

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using A3sist.UI.Framework.WPF.Views;
using A3sist.UI.Shared;

namespace A3sist.UI.Framework.VSIX.ToolWindows
{
    /// <summary>
    /// Chat tool window that hosts the unified WPF ChatView within VSIX
    /// </summary>
    [Guid("12345678-1234-1234-1234-123456789015")]
    public class ChatToolWindow : ToolWindowPane
    {
        private readonly ILogger<ChatToolWindow>? _logger;
        private ChatView? _chatView;
        private IServiceProvider? _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatToolWindow"/> class.
        /// </summary>
        public ChatToolWindow() : base(null)
        {
            this.Caption = "A3sist Chat";

            try
            {
                // Get service provider from package
                if (Package is A3sistPackage package)
                {
                    _serviceProvider = package.GetServiceProvider();
                    _logger = _serviceProvider.GetService<ILogger<ChatToolWindow>>();
                }

                _logger?.LogInformation("Initializing A3sist unified chat tool window");

                // Create the unified WPF ChatView that works across frameworks
#if NET9_0_OR_GREATER
                _chatView = new ChatView();
#else
                // For .NET 4.7.2, we create a compatible version
                _chatView = CreateCompatibleChatView();
#endif

                // Set up data context with services from unified DI container
                if (_serviceProvider != null)
                {
                    var chatViewModel = _serviceProvider.GetService<ChatViewModel>();
                    if (chatViewModel != null)
                    {
                        _chatView.DataContext = chatViewModel;
                    }
                }

                this.Content = _chatView;

                _logger?.LogInformation("A3sist unified chat tool window initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize A3sist chat tool window");
                
                // Create a simple error control if initialization fails
                this.Content = new System.Windows.Controls.StackPanel
                {
                    Margin = new System.Windows.Thickness(10),
                    Children =
                    {
                        new System.Windows.Controls.TextBlock
                        {
                            Text = "A3sist Chat - Initialization Error",
                            FontWeight = System.Windows.FontWeights.Bold,
                            Margin = new System.Windows.Thickness(0, 0, 0, 10)
                        },
                        new System.Windows.Controls.TextBlock
                        {
                            Text = $"Error: {ex.Message}",
                            TextWrapping = System.Windows.TextWrapping.Wrap,
                            Foreground = System.Windows.Media.Brushes.Red
                        },
                        new System.Windows.Controls.Button
                        {
                            Content = "Retry",
                            Margin = new System.Windows.Thickness(0, 10, 0, 0),
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                            Padding = new System.Windows.Thickness(20, 5)
                        }
                    }
                };
            }
        }

        /// <summary>
        /// Creates a .NET 4.7.2 compatible version of ChatView
        /// </summary>
        private ChatView CreateCompatibleChatView()
        {
            try
            {
                // The ChatView should work on .NET 4.7.2 with WPF enabled
                return new ChatView();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create compatible ChatView");
                
                // Return a basic fallback
                return new ChatView(); // Let it fail gracefully with base implementation
            }
        }

        /// <summary>
        /// Static method to show the chat window
        /// </summary>
        public static async Task<ChatToolWindow?> ShowAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var package = Package.GetGlobalService(typeof(A3sistPackage)) as A3sistPackage;
                if (package != null)
                {
                    var window = await package.ShowToolWindowAsync(typeof(ChatToolWindow), 0, true, package.DisposalToken);
                    return window as ChatToolWindow;
                }
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    ServiceProvider.GlobalProvider,
                    $"Failed to show A3sist Chat window: {ex.Message}",
                    "A3sist Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }

            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _logger?.LogInformation("Disposing A3sist unified chat tool window");
                    
                    if (_chatView is IDisposable disposableChatView)
                    {
                        disposableChatView.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error disposing A3sist chat tool window");
                }
            }

            base.Dispose(disposing);
        }
    }
}

#endif