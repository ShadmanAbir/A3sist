using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using A3sist.Models;

namespace A3sist.Services
{
    public class ChatService : IChatService
    {
        private readonly IModelManagementService _modelService;
        private readonly IRAGEngineService _ragService;
        private readonly IA3sistConfigurationService _configService;
        private readonly List<ChatMessage> _chatHistory;
        private readonly object _lockObject = new object();
        private string _activeChatModelId;

        public event EventHandler<ChatMessageReceivedEventArgs> MessageReceived;

        public ChatService(
            IModelManagementService modelService,
            IRAGEngineService ragService,
            IA3sistConfigurationService configService)
        {
            _modelService = modelService;
            _ragService = ragService;
            _configService = configService;
            _chatHistory = new List<ChatMessage>();

            LoadChatHistoryAsync();
        }

        public async Task<ChatResponse> SendMessageAsync(ChatMessage message)
        {
            try
            {
                // Add user message to history
                message.Id = Guid.NewGuid().ToString();
                message.Timestamp = DateTime.UtcNow;
                message.Role = ChatRole.User;

                lock (_lockObject)
                {
                    _chatHistory.Add(message);
                }

                var startTime = DateTime.UtcNow;

                // Enhance prompt with RAG if enabled
                var enhancedPrompt = await EnhancePromptWithRAGAsync(message.Content);

                // Build conversation context
                var systemMessage = BuildConversationContext(enhancedPrompt);

                // Send to model
                var modelRequest = new ModelRequest
                {
                    Prompt = enhancedPrompt,
                    SystemMessage = systemMessage,
                    MaxTokens = await _configService.GetSettingAsync("chat.maxTokens", 2000),
                    Temperature = await _configService.GetSettingAsync("chat.temperature", 0.7)
                };

                var response = await _modelService.SendRequestAsync(modelRequest);

                if (!response.Success)
                {
                    return new ChatResponse
                    {
                        Success = false,
                        Error = response.Error ?? "Unknown error occurred"
                    };
                }

                // Create assistant message
                var assistantMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = response.Content,
                    Role = ChatRole.Assistant,
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["modelUsed"] = await GetActiveChatModelAsync(),
                        ["tokensUsed"] = response.TokensUsed,
                        ["processingTime"] = response.ProcessingTime.TotalSeconds,
                        ["isCode"] = DetectIfCodeResponse(response.Content)
                    }
                };

                lock (_lockObject)
                {
                    _chatHistory.Add(assistantMessage);
                }

                // Trim history if needed
                await TrimChatHistoryAsync();

                // Save updated history
                await SaveChatHistoryAsync();

                // Notify listeners
                MessageReceived?.Invoke(this, new ChatMessageReceivedEventArgs
                {
                    Message = assistantMessage
                });

                return new ChatResponse
                {
                    Id = assistantMessage.Id,
                    Content = assistantMessage.Content,
                    Success = true,
                    ResponseTime = DateTime.UtcNow - startTime,
                    TokensUsed = response.TokensUsed
                };
            }
            catch (Exception ex)
            {
                return new ChatResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<IEnumerable<ChatMessage>> GetChatHistoryAsync()
        {
            lock (_lockObject)
            {
                return _chatHistory.ToList();
            }
        }

        public async Task ClearChatHistoryAsync()
        {
            lock (_lockObject)
            {
                _chatHistory.Clear();
            }

            await SaveChatHistoryAsync();
        }

        public async Task<bool> SetChatModelAsync(string modelId)
        {
            var models = await _modelService.GetAvailableModelsAsync();
            var model = models.FirstOrDefault(m => m.Id == modelId);

            if (model == null || !model.IsAvailable)
                return false;

            _activeChatModelId = modelId;
            await _configService.SetSettingAsync("chat.activeModelId", modelId);
            return true;
        }

        public async Task<string> GetActiveChatModelAsync()
        {
            if (string.IsNullOrEmpty(_activeChatModelId))
            {
                _activeChatModelId = await _configService.GetSettingAsync<string>("chat.activeModelId");
                
                // If still null, use the active model from model management service
                if (string.IsNullOrEmpty(_activeChatModelId))
                {
                    var activeModel = await _modelService.GetActiveModelAsync();
                    _activeChatModelId = activeModel?.Id;
                }
            }

            return _activeChatModelId;
        }

        private async Task<string> EnhancePromptWithRAGAsync(string originalPrompt)
        {
            try
            {
                var ragEnabled = await _configService.GetSettingAsync("rag.enabled", true);
                if (!ragEnabled)
                    return originalPrompt;

                // Search for relevant context
                var maxResults = await _configService.GetSettingAsync("rag.maxResults", 5);
                var searchResults = await _ragService.SearchAsync(originalPrompt, maxResults);

                if (!searchResults.Any())
                    return originalPrompt;

                // Build enhanced prompt with context
                var contextBuilder = new System.Text.StringBuilder();
                contextBuilder.AppendLine("Based on the following relevant code and documentation:");
                contextBuilder.AppendLine();

                foreach (var result in searchResults.Take(3)) // Limit to top 3 results
                {
                    contextBuilder.AppendLine($"**From {(result.Metadata.TryGetValue("path", out var pathValue) ? pathValue.ToString() : "unknown")}:**");
                    contextBuilder.AppendLine("``");
                    contextBuilder.AppendLine(result.Content);
                    contextBuilder.AppendLine("```");
                    contextBuilder.AppendLine();
                }

                contextBuilder.AppendLine("**User Question:**");
                contextBuilder.AppendLine(originalPrompt);

                return contextBuilder.ToString();
            }
            catch
            {
                // If RAG fails, return original prompt
                return originalPrompt;
            }
        }

        private string BuildConversationContext(string currentPrompt)
        {
            var systemMessage = "You are A3sist, an intelligent code assistant specialized in helping developers with coding tasks. " +
                               "You have expertise in C#, .NET, and many other programming languages. " +
                               "Provide helpful, accurate, and concise responses. " +
                               "When showing code, use proper formatting and include relevant comments. " +
                               "If you're unsure about something, say so rather than guessing.";

            // Add recent conversation context (last 5 messages)
            var recentMessages = new List<ChatMessage>();
            lock (_lockObject)
            {
                recentMessages = _chatHistory.Skip(Math.Max(0, _chatHistory.Count - 10)).ToList();
            }

            if (recentMessages.Any())
            {
                systemMessage += "\n\nRecent conversation context:\n";
                foreach (var msg in recentMessages.Take(5))
                {
                    var sender = msg.Role == ChatRole.User ? "User" : "Assistant";
                    systemMessage += $"{sender}: {TruncateMessage(msg.Content, 200)}\n";
                }
            }

            return systemMessage;
        }

        private bool DetectIfCodeResponse(string content)
        {
            // Simple heuristics to detect if response contains code
            var codeIndicators = new[]
            {
                "```", "class ", "public ", "private ", "void ", "string ", "int ", "var ",
                "function", "const ", "let ", "def ", "import ", "using ", "#include",
                "  {", "  }", "  if (", "  for (", "  while (", "  try ", "  catch "
            };

            return codeIndicators.Any(indicator => content.IndexOf(indicator, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private string TruncateMessage(string message, int maxLength)
        {
            if (message.Length <= maxLength)
                return message;

            return message.Substring(0, maxLength - 3) + "...";
        }

        private async Task TrimChatHistoryAsync()
        {
            var maxHistory = await _configService.GetSettingAsync("chat.maxHistory", 100);
            
            lock (_lockObject)
            {
                if (_chatHistory.Count > maxHistory)
                {
                    var messagesToRemove = _chatHistory.Count - maxHistory;
                    _chatHistory.RemoveRange(0, messagesToRemove);
                }
            }
        }

        private async Task SaveChatHistoryAsync()
        {
            var saveHistory = await _configService.GetSettingAsync("chat.saveHistory", true);
            if (!saveHistory)
                return;

            List<ChatMessage> historyToSave;
            lock (_lockObject)
            {
                historyToSave = _chatHistory.ToList();
            }

            await _configService.SetSettingAsync("chat.history", historyToSave);
        }

        private async Task LoadChatHistoryAsync()
        {
            try
            {
                var savedHistory = await _configService.GetSettingAsync<List<ChatMessage>>("chat.history");
                if (savedHistory != null)
                {
                    lock (_lockObject)
                    {
                        _chatHistory.AddRange(savedHistory);
                    }
                }
            }
            catch
            {
                // If loading fails, start with empty history
            }
        }
    }


}