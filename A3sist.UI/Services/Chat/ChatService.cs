using A3sist.Core.LLM;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.UI.Models.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using A3sist.Core.Configuration;

namespace A3sist.UI.Services.Chat
{
    /// <summary>
    /// Main chat service that handles conversation management and MCP integration
    /// </summary>
    public interface IChatService
    {
        Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default);
        Task<ChatConversation> CreateConversationAsync(string? title = null);
        Task<ChatConversation?> GetConversationAsync(string conversationId);
        Task<IEnumerable<ChatConversation>> GetConversationsAsync();
        Task<bool> DeleteConversationAsync(string conversationId);
        Task<bool> UpdateConversationAsync(ChatConversation conversation);
        
        event EventHandler<StreamingResponseChunk>? StreamingResponse;
        event EventHandler<string>? ConversationUpdated;
    }

    /// <summary>
    /// Implementation of the chat service with MCP orchestrator integration
    /// </summary>
    public class ChatService : IChatService, IDisposable
    {
        private readonly MCPOrchestrator _mcpOrchestrator;
        private readonly IChatHistoryService _historyService;
        private readonly IContextService _contextService;
        private readonly ILogger<ChatService> _logger;
        private readonly A3sistOptions _options;
        
        private readonly ConcurrentDictionary<string, ChatConversation> _conversations;
        private readonly SemaphoreSlim _processingSemaphore;
        private bool _disposed;

        public event EventHandler<StreamingResponseChunk>? StreamingResponse;
        public event EventHandler<string>? ConversationUpdated;

        public ChatService(
            MCPOrchestrator mcpOrchestrator,
            IChatHistoryService historyService,
            IContextService contextService,
            ILogger<ChatService> logger,
            IOptions<A3sistOptions> options)
        {
            _mcpOrchestrator = mcpOrchestrator ?? throw new ArgumentNullException(nameof(mcpOrchestrator));
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
            _contextService = contextService ?? throw new ArgumentNullException(nameof(contextService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _conversations = new ConcurrentDictionary<string, ChatConversation>();
            _processingSemaphore = new SemaphoreSlim(1, 1);

            LoadConversationsAsync();
        }

        /// <summary>
        /// Sends a message and gets an AI response using MCP orchestration with streaming support
        /// </summary>
        public async Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                await _processingSemaphore.WaitAsync(cancellationToken);

                _logger.LogInformation("Processing chat message for conversation {ConversationId}", request.ConversationId);

                // Get or create conversation
                var conversation = await GetOrCreateConversationAsync(request.ConversationId);
                
                // Add user message to conversation
                var userMessage = new ChatMessage
                {
                    Type = ChatMessageType.User,
                    Content = request.Content,
                    Timestamp = DateTime.Now
                };
                conversation.Messages.Add(userMessage);

                // Update context
                await UpdateConversationContextAsync(conversation, request.Context);

                // Create agent request from chat context
                var agentRequest = await BuildAgentRequestAsync(conversation, request);

                // Create placeholder assistant message for streaming
                var assistantMessage = new ChatMessage
                {
                    Type = ChatMessageType.Assistant,
                    Content = "",
                    Timestamp = DateTime.Now
                };
                conversation.Messages.Add(assistantMessage);

                // Start streaming response
                var streamingCancellation = new CancellationTokenSource();
                var streamingTask = StartStreamingResponseAsync(assistantMessage.Id, streamingCancellation.Token);

                try
                {
                    // Process with MCP orchestrator
                    var orchestratorResult = await _mcpOrchestrator.ProcessRequestAsync(agentRequest, cancellationToken);

                    // Stop streaming and update final content
                    streamingCancellation.Cancel();
                    await streamingTask;

                    // Update assistant message with final content
                    assistantMessage.Content = orchestratorResult.Content ?? "I apologize, but I couldn't process your request.";
                    assistantMessage.HasCodeSuggestion = ContainsCodeSuggestion(orchestratorResult.Content);
                    assistantMessage.Metadata = new ChatMessageMetadata
                    {
                        ToolsUsed = orchestratorResult.ToolResults?.Keys.ToList() ?? new List<string>(),
                        ServersInvolved = orchestratorResult.ServersUsed,
                        ProcessingTimeMs = orchestratorResult.ProcessingTimeMs,
                        CodeSuggestion = ExtractCodeSuggestion(orchestratorResult.Content)
                    };

                    // Set up message commands
                    SetupMessageCommands(assistantMessage, conversation);

                    // Send final streaming chunk
                    StreamingResponse?.Invoke(this, new StreamingResponseChunk
                    {
                        MessageId = assistantMessage.Id,
                        Content = assistantMessage.Content,
                        IsComplete = true,
                        Metadata = assistantMessage.Metadata
                    });

                    conversation.LastActivity = DateTime.Now;

                    // Save conversation
                    await _historyService.SaveConversationAsync(conversation);

                    // Notify listeners
                    ConversationUpdated?.Invoke(this, conversation.Id);

                    _logger.LogInformation("Successfully processed chat message. Used servers: {Servers}", 
                        string.Join(", ", orchestratorResult.ServersUsed));

                    return new SendMessageResponse
                    {
                        Success = orchestratorResult.Success,
                        MessageId = assistantMessage.Id,
                        Metadata = assistantMessage.Metadata
                    };
                }
                catch (Exception ex)
                {
                    // Stop streaming on error
                    streamingCancellation.Cancel();
                    await streamingTask;

                    // Update message with error
                    assistantMessage.Content = $"Error: {ex.Message}";
                    assistantMessage.Type = ChatMessageType.System;

                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message for conversation {ConversationId}", request.ConversationId);
                
                return new SendMessageResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }

        /// <summary>
        /// Starts a streaming response simulation for better UX
        /// </summary>
        private async Task StartStreamingResponseAsync(string messageId, CancellationToken cancellationToken)
        {
            try
            {
                // Simulate typing with periodic updates
                var typingIndicators = new[] { "⌨️", "💭", "🤔", "📝" };
                var index = 0;

                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1500, cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                        break;

                    StreamingResponse?.Invoke(this, new StreamingResponseChunk
                    {
                        MessageId = messageId,
                        Content = typingIndicators[index % typingIndicators.Length],
                        IsComplete = false,
                        IsTypingIndicator = true
                    });

                    index++;
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when streaming is cancelled
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in streaming response simulation");
            }
        }

        /// <summary>
        /// Creates a new conversation
        /// </summary>
        public async Task<ChatConversation> CreateConversationAsync(string? title = null)
        {
            var conversation = new ChatConversation
            {
                Title = title ?? GenerateConversationTitle(),
                CreatedAt = DateTime.Now,
                LastActivity = DateTime.Now
            };

            _conversations[conversation.Id] = conversation;
            await _historyService.SaveConversationAsync(conversation);

            _logger.LogInformation("Created new conversation: {ConversationId}", conversation.Id);
            return conversation;
        }

        /// <summary>
        /// Gets a conversation by ID
        /// </summary>
        public async Task<ChatConversation?> GetConversationAsync(string conversationId)
        {
            if (_conversations.TryGetValue(conversationId, out var conversation))
                return conversation;

            // Try loading from history
            conversation = await _historyService.LoadConversationAsync(conversationId);
            if (conversation != null)
            {
                _conversations[conversationId] = conversation;
            }

            return conversation;
        }

        /// <summary>
        /// Gets all conversations
        /// </summary>
        public async Task<IEnumerable<ChatConversation>> GetConversationsAsync()
        {
            var allConversations = await _historyService.LoadAllConversationsAsync();
            
            // Update cache
            foreach (var conversation in allConversations)
            {
                _conversations[conversation.Id] = conversation;
            }

            return allConversations.OrderByDescending(c => c.LastActivity);
        }

        /// <summary>
        /// Deletes a conversation
        /// </summary>
        public async Task<bool> DeleteConversationAsync(string conversationId)
        {
            try
            {
                _conversations.TryRemove(conversationId, out _);
                await _historyService.DeleteConversationAsync(conversationId);
                
                _logger.LogInformation("Deleted conversation: {ConversationId}", conversationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation {ConversationId}", conversationId);
                return false;
            }
        }

        /// <summary>
        /// Updates a conversation
        /// </summary>
        public async Task<bool> UpdateConversationAsync(ChatConversation conversation)
        {
            try
            {
                conversation.LastActivity = DateTime.Now;
                _conversations[conversation.Id] = conversation;
                await _historyService.SaveConversationAsync(conversation);
                
                ConversationUpdated?.Invoke(this, conversation.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating conversation {ConversationId}", conversation.Id);
                return false;
            }
        }

        #region Private Methods

        private async Task<ChatConversation> GetOrCreateConversationAsync(string conversationId)
        {
            var conversation = await GetConversationAsync(conversationId);
            return conversation ?? await CreateConversationAsync();
        }

        private async Task UpdateConversationContextAsync(ChatConversation conversation, ChatContext? requestContext)
        {
            if (requestContext != null)
            {
                conversation.Context = requestContext;
            }
            else
            {
                // Get current context from Visual Studio
                conversation.Context = await _contextService.GetCurrentContextAsync();
            }
        }

        private async Task<AgentRequest> BuildAgentRequestAsync(ChatConversation conversation, SendMessageRequest request)
        {
            var agentRequest = new AgentRequest
            {
                Prompt = request.Content,
                Context = new Dictionary<string, object>()
            };

            // Add conversation history for context
            if (conversation.Messages.Any())
            {
                var recentMessages = conversation.Messages.TakeLast(10).ToList();
                agentRequest.Context["ConversationHistory"] = recentMessages;
            }

            // Add Visual Studio context
            if (conversation.Context != null)
            {
                agentRequest.FilePath = conversation.Context.CurrentFile;
                agentRequest.Content = conversation.Context.SelectedText;
                agentRequest.Context["ProjectPath"] = conversation.Context.ProjectPath;
                agentRequest.Context["OpenFiles"] = conversation.Context.OpenFiles;
                agentRequest.Context["Errors"] = conversation.Context.Errors;
            }

            // Add request options
            foreach (var option in request.Options)
            {
                agentRequest.Context[option.Key] = option.Value;
            }

            return agentRequest;
        }

        private bool ContainsCodeSuggestion(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return false;

            return content.Contains("```") || 
                   content.Contains("public class") || 
                   content.Contains("function ") ||
                   content.Contains("def ");
        }

        private string? ExtractCodeSuggestion(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return null;

            // Simple code extraction - look for code blocks
            var codeBlockStart = content.IndexOf("```");
            if (codeBlockStart >= 0)
            {
                var codeBlockEnd = content.IndexOf("```", codeBlockStart + 3);
                if (codeBlockEnd > codeBlockStart)
                {
                    return content.Substring(codeBlockStart + 3, codeBlockEnd - codeBlockStart - 3).Trim();
                }
            }

            return null;
        }

        private void SetupMessageCommands(ChatMessage message, ChatConversation conversation)
        {
            // Implement relay commands for copy, apply, and explain code
            // These integrate with Visual Studio's editor services
            
            // Create a copy command for copying message content
            var copyCommand = new RelayCommand<ChatMessage>(async (msg) =>
            {
                if (msg?.Content != null)
                {
                    try
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        System.Windows.Clipboard.SetText(msg.Content);
                        _logger.LogDebug("Copied message content to clipboard");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to copy message content to clipboard");
                    }
                }
            });
            
            // Create an apply command for applying code suggestions
            var applyCommand = new RelayCommand<ChatMessage>(async (msg) =>
            {
                if (msg?.Content != null)
                {
                    try
                    {
                        var codeContent = ExtractCodeSuggestion(msg.Content);
                        if (!string.IsNullOrEmpty(codeContent))
                        {
                            // In a real implementation, this would integrate with VS editor APIs
                            _logger.LogInformation("Would apply code suggestion: {Code}", codeContent);
                            // TODO: Integrate with Visual Studio editor services to apply code
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to apply code suggestion");
                    }
                }
            });
            
            // Create an explain command for getting explanations
            var explainCommand = new RelayCommand<ChatMessage>(async (msg) =>
            {
                if (msg?.Content != null)
                {
                    try
                    {
                        // Create a follow-up request for explanation
                        var explanationRequest = $"Please explain this code: {msg.Content}";
                        await SendMessageAsync(conversation.Id, explanationRequest);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to request code explanation");
                    }
                }
            });
            
            // Assign commands to message (assuming ChatMessage has command properties)
            // In a real implementation, you would set these on the message object
            message.CopyCommand = copyCommand;
            message.ApplyCommand = applyCommand;
            message.ExplainCommand = explainCommand;
        }

        private string GenerateConversationTitle()
        {
            var topics = new[] { "Code Review", "Bug Fix", "Feature Request", "Refactoring", "Help", "Question" };
            var randomTopic = topics[new Random().Next(topics.Length)];
            return $"{randomTopic} - {DateTime.Now:MMM dd, HH:mm}";
        }

        private async void LoadConversationsAsync()
        {
            try
            {
                var conversations = await _historyService.LoadAllConversationsAsync();
                foreach (var conversation in conversations)
                {
                    _conversations[conversation.Id] = conversation;
                }

                _logger.LogInformation("Loaded {ConversationCount} conversations from history", conversations.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conversations from history");
            }
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _processingSemaphore?.Dispose();
                _disposed = true;
            }
        }
    }
}