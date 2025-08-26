#if NET9_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.Extensions.Caching.Memory;
using A3sist.UI.Shared.Interfaces;
using A3sist.UI.Framework.WPF.Views;
using A3sist.Core.Services;
using A3sist.Shared.Messaging;

namespace A3sist.UI.Framework.WPF
{
    /// <summary>
    /// WPF-specific UI service implementation for .NET 9
    /// </summary>
    public class WPFUIService : IUIService, IRAGUIService
    {
        public async Task ShowChatViewAsync()
        {
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var chatView = new ChatView();
                    var window = new Window 
                    { 
                        Content = chatView,
                        Title = "A3sist Chat",
                        Width = 800,
                        Height = 600,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };
                    window.Show();
                });
            });
        }

        public async Task ShowAgentStatusAsync()
        {
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var statusView = new AgentStatusView();
                    var window = new Window 
                    { 
                        Content = statusView,
                        Title = "A3sist Agent Status",
                        Width = 600,
                        Height = 400,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };
                    window.Show();
                });
            });
        }

        public async Task<string?> GetSelectedCodeAsync()
        {
            await Task.CompletedTask;
            // For standalone WPF, this would get from clipboard or current context
            return System.Windows.Clipboard.GetText();
        }

        public async Task ShowNotificationAsync(string title, string message)
        {
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
                });
            });
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message)
        {
            return await Task.Run(() =>
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                    return result == MessageBoxResult.Yes;
                });
            });
        }

        // RAG-specific implementations
        public async Task ShowKnowledgeModeAsync()
        {
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var knowledgeView = new KnowledgeView();
                    var window = new Window 
                    { 
                        Content = knowledgeView,
                        Title = "A3sist Knowledge Explorer",
                        Width = 1000,
                        Height = 700,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };
                    window.Show();
                });
            });
        }

        public async Task<RAGContext?> GetCurrentRAGContextAsync()
        {
            await Task.CompletedTask;
            // For standalone WPF, this would be based on current application context
            return null;
        }

        public async Task ShowCitationsAsync(Citation[] citations)
        {
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var citationsView = new CitationsView { DataContext = citations };
                    var popup = new Popup
                    {
                        Child = citationsView,
                        IsOpen = true,
                        Placement = PlacementMode.Mouse,
                        StaysOpen = false
                    };
                    
                    // Auto-close after 10 seconds
                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(10);
                    timer.Tick += (s, e) =>
                    {
                        popup.IsOpen = false;
                        timer.Stop();
                    };
                    timer.Start();
                });
            });
        }

        public async Task UpdateKnowledgeStatusAsync(string status)
        {
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Update status in main window or status bar
                    if (Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        mainWindow.UpdateKnowledgeStatus(status);
                    }
                });
            });
        }
    }

    /// <summary>
    /// WPF-specific chat service implementation
    /// </summary>
    public class WPFChatService : IChatService
    {
        private readonly EnhancedRequestRouter _router;
        private readonly IMemoryCache _cache;
        private readonly List<ChatMessage> _history = new List<ChatMessage>();
        
        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

        public WPFChatService(EnhancedRequestRouter router, IMemoryCache cache)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<string> SendMessageAsync(string message)
        {
            var userMessage = new ChatMessage
            {
                Content = message,
                IsFromUser = true,
                Type = ChatMessageType.Text
            };
            
            _history.Add(userMessage);

            try
            {
                var request = new AgentRequest
                {
                    Id = Guid.NewGuid().ToString(),
                    Prompt = message,
                    FilePath = GetCurrentContext(),
                    Content = await GetSelectedCodeAsync()
                };

                var result = await _router.ProcessRequestAsync(request);
                
                var responseMessage = new ChatMessage
                {
                    Content = result.Data?.ToString() ?? result.Message,
                    IsFromUser = false,
                    Type = result.Success ? ChatMessageType.Text : ChatMessageType.Error,
                    Metadata = result.Metadata
                };
                
                _history.Add(responseMessage);

                // Extract RAG context and citations from metadata
                RAGContext? ragContext = ExtractRAGContext(result.Metadata);
                Citation[]? citations = ExtractCitations(result.Metadata);

                MessageReceived?.Invoke(this, new MessageReceivedEventArgs
                {
                    Message = responseMessage,
                    RAGContext = ragContext,
                    Citations = citations
                });

                // Cache successful responses
                if (result.Success)
                {
                    _cache.Set($"response_{request.Id}", result, TimeSpan.FromMinutes(30));
                }

                return responseMessage.Content;
            }
            catch (Exception ex)
            {
                var errorMessage = new ChatMessage
                {
                    Content = $"Error: {ex.Message}",
                    IsFromUser = false,
                    Type = ChatMessageType.Error
                };
                
                _history.Add(errorMessage);
                
                MessageReceived?.Invoke(this, new MessageReceivedEventArgs
                {
                    Message = errorMessage
                });

                return errorMessage.Content;
            }
        }

        public async Task<ChatMessage[]> GetHistoryAsync()
        {
            await Task.CompletedTask;
            return _history.ToArray();
        }

        public async Task ClearHistoryAsync()
        {
            await Task.CompletedTask;
            _history.Clear();
        }

        public async Task StartNewConversationAsync()
        {
            await ClearHistoryAsync();
        }

        private async Task<string?> GetSelectedCodeAsync()
        {
            await Task.CompletedTask;
            // For WPF, get from clipboard or current context
            try
            {
                return System.Windows.Clipboard.GetText();
            }
            catch
            {
                return null;
            }
        }

        private string? GetCurrentContext()
        {
            // For standalone WPF, this could be based on current file or project context
            return "wpf_context";
        }

        private RAGContext? ExtractRAGContext(Dictionary<string, object>? metadata)
        {
            if (metadata?.ContainsKey("RAGContext") == true)
            {
                // Extract and deserialize RAG context
                // Implementation would depend on metadata structure
                return null; // Placeholder
            }
            return null;
        }

        private Citation[]? ExtractCitations(Dictionary<string, object>? metadata)
        {
            if (metadata?.ContainsKey("Citations") == true)
            {
                // Extract and deserialize citations
                // Implementation would depend on metadata structure
                return null; // Placeholder
            }
            return null;
        }
    }
}

#endif