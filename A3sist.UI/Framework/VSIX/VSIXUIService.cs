#if NET472

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using A3sist.UI.Shared.Interfaces;
using A3sist.UI.ToolWindows;
using A3sist.Core.Services;

namespace A3sist.UI.Framework.VSIX
{
    /// <summary>
    /// VSIX-specific UI service implementation for .NET 4.7.2
    /// </summary>
    public class VSIXUIService : IUIService, IRAGUIService
    {
        public async Task ShowChatViewAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var toolWindow = await ChatToolWindow.ShowAsync();
        }

        public async Task ShowAgentStatusAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var toolWindow = await AgentStatusWindow.ShowAsync();
        }

        public async Task<string?> GetSelectedCodeAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            
            try
            {
                var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                var textSelection = dte?.ActiveDocument?.Selection as EnvDTE.TextSelection;
                return textSelection?.Text;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task ShowNotificationAsync(string title, string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            
            VsShellUtilities.ShowMessageBox(
                ServiceProvider.GlobalProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            
            var result = VsShellUtilities.ShowMessageBox(
                ServiceProvider.GlobalProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_QUESTION,
                OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            
            return result == (int)VSConstants.MessageBoxResult.IDYES;
        }

        // RAG-specific implementations
        public async Task ShowKnowledgeModeAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var toolWindow = await KnowledgeToolWindow.ShowAsync();
        }

        public async Task<RAGContext?> GetCurrentRAGContextAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            
            // This would integrate with the current context
            // For now, return null - would be implemented with actual context gathering
            return null;
        }

        public async Task ShowCitationsAsync(Citation[] citations)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            
            // Show citations in VS output window
            var outputWindow = GetOutputWindow("A3sist Citations");
            outputWindow.Clear();
            
            foreach (var citation in citations)
            {
                outputWindow.OutputString($"{citation.Title} - {citation.Source} (Relevance: {citation.Relevance:F2})\n");
                if (!string.IsNullOrEmpty(citation.Url))
                {
                    outputWindow.OutputString($"  Source: {citation.Url}\n");
                }
                if (!string.IsNullOrEmpty(citation.Description))
                {
                    outputWindow.OutputString($"  {citation.Description}\n");
                }
                outputWindow.OutputString("\n");
            }
            
            outputWindow.Activate();
        }

        public async Task UpdateKnowledgeStatusAsync(string status)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            
            // Update status bar
            var statusBar = Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar;
            statusBar?.SetText($"A3sist Knowledge: {status}");
        }

        private IVsOutputWindowPane GetOutputWindow(string title)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            var outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            var customGuid = new Guid("12345678-1234-1234-1234-123456789014");
            
            outWindow.CreatePane(ref customGuid, title, 1, 1);
            outWindow.GetPane(ref customGuid, out IVsOutputWindowPane customPane);
            
            return customPane;
        }
    }

    /// <summary>
    /// VSIX-specific chat service implementation
    /// </summary>
    public class VSIXChatService : IChatService
    {
        private readonly EnhancedRequestRouter _router;
        private readonly List<ChatMessage> _history = new List<ChatMessage>();
        
        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

        public VSIXChatService(EnhancedRequestRouter router)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
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
                    FilePath = await GetCurrentFilePathAsync(),
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
                RAGContext? ragContext = null;
                Citation[]? citations = null;
                
                if (result.Metadata?.ContainsKey("RAGContext") == true)
                {
                    // Extract RAG context from metadata
                }
                
                if (result.Metadata?.ContainsKey("Citations") == true)
                {
                    // Extract citations from metadata
                }

                MessageReceived?.Invoke(this, new MessageReceivedEventArgs
                {
                    Message = responseMessage,
                    RAGContext = ragContext,
                    Citations = citations
                });

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

        private async Task<string?> GetCurrentFilePathAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            
            try
            {
                var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                return dte?.ActiveDocument?.FullName;
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> GetSelectedCodeAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            
            try
            {
                var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                var textSelection = dte?.ActiveDocument?.Selection as EnvDTE.TextSelection;
                return textSelection?.Text;
            }
            catch
            {
                return null;
            }
        }
    }
}

#endif