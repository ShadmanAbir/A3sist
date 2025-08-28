using A3sist.API.Models;
using A3sist.API.Services;
using System.Text.Json;

namespace A3sist.API.Services;

public class ChatService : IChatService, IDisposable
{
    private readonly IModelManagementService _modelService;
    private readonly IRAGEngineService _ragService;
    private readonly ILogger<ChatService> _logger;
    private readonly List<ChatMessage> _chatHistory = new();
    private readonly SemaphoreSlim _historyLock = new(1, 1);
    private string? _activeChatModelId;
    private bool _disposed = false;

    public event EventHandler<ChatMessageReceivedEventArgs>? MessageReceived;

    public ChatService(
        IModelManagementService modelService,
        IRAGEngineService ragService,
        ILogger<ChatService> logger)
    {
        _modelService = modelService;
        _ragService = ragService;
        _logger = logger;
        
        // Don't load chat history in constructor - load on demand
        _logger.LogInformation("ChatService initialized with lazy loading");
    }

    public async Task<ChatResponse> SendMessageAsync(ChatMessage message)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Add user message to history
            message.Id = Guid.NewGuid().ToString();
            message.Timestamp = DateTime.UtcNow;
            message.Role = ChatRole.User;

            await _historyLock.WaitAsync();
            try
            {
                _chatHistory.Add(message);
            }
            finally
            {
                _historyLock.Release();
            }

            // Get enhanced prompt with RAG if available
            var enhancedPrompt = await EnhancePromptWithRAGAsync(message.Content);

            // Build conversation context
            var systemMessage = BuildConversationContext(enhancedPrompt);

            // Send to model
            var modelRequest = new ModelRequest
            {
                Prompt = enhancedPrompt,
                SystemMessage = systemMessage,
                MaxTokens = 2000,
                Temperature = 0.7
            };

            var response = await _modelService.SendRequestAsync(modelRequest);

            if (!response.Success)
            {
                _logger.LogWarning("Model request failed: {Error}", response.Error);
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
                    ["modelUsed"] = await GetActiveChatModelAsync() ?? "unknown",
                    ["tokensUsed"] = response.TokensUsed,
                    ["processingTime"] = response.ProcessingTime.TotalSeconds,
                    ["isCode"] = DetectIfCodeResponse(response.Content)
                }
            };

            await _historyLock.WaitAsync();
            try
            {
                _chatHistory.Add(assistantMessage);
            }
            finally
            {
                _historyLock.Release();
            }

            // Trim history if needed (keep last 100 messages)
            await TrimChatHistoryAsync();

            // Save updated history asynchronously
            _ = Task.Run(SaveChatHistoryAsync);

            // Notify listeners
            MessageReceived?.Invoke(this, new ChatMessageReceivedEventArgs
            {
                Message = assistantMessage
            });

            var totalTime = DateTime.UtcNow - startTime;
            _logger.LogInformation("Chat response generated in {TotalTime}ms", totalTime.TotalMilliseconds);

            return new ChatResponse
            {
                Id = assistantMessage.Id,
                Content = assistantMessage.Content,
                Success = true,
                ResponseTime = totalTime,
                TokensUsed = response.TokensUsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return new ChatResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<IEnumerable<ChatMessage>> GetChatHistoryAsync()
    {
        await EnsureChatHistoryLoadedAsync();
        
        await _historyLock.WaitAsync();
        try
        {
            return _chatHistory.ToList();
        }
        finally
        {
            _historyLock.Release();
        }
    }

    public async Task ClearChatHistoryAsync()
    {
        await _historyLock.WaitAsync();
        try
        {
            _chatHistory.Clear();
        }
        finally
        {
            _historyLock.Release();
        }

        await SaveChatHistoryAsync();
        _logger.LogInformation("Chat history cleared");
    }

    public async Task<bool> SetChatModelAsync(string modelId)
    {
        try
        {
            var models = await _modelService.GetAvailableModelsAsync();
            var model = models.FirstOrDefault(m => m.Id == modelId);

            if (model == null || !model.IsAvailable)
            {
                _logger.LogWarning("Model {ModelId} not found or unavailable", modelId);
                return false;
            }

            _activeChatModelId = modelId;
            _logger.LogInformation("Active chat model changed to {ModelId}", modelId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting chat model");
            return false;
        }
    }

    public async Task<string?> GetActiveChatModelAsync()
    {
        if (string.IsNullOrEmpty(_activeChatModelId))
        {
            // Try to get default from model service
            var activeModel = await _modelService.GetActiveModelAsync();
            _activeChatModelId = activeModel?.Id;
        }

        return _activeChatModelId;
    }

    private async Task<string> EnhancePromptWithRAGAsync(string originalPrompt)
    {
        try
        {
            // Search for relevant context
            var searchResults = await _ragService.SearchAsync(originalPrompt, 3);

            if (!searchResults.Any())
                return originalPrompt;

            // Build enhanced prompt with context
            var contextBuilder = new System.Text.StringBuilder();
            contextBuilder.AppendLine("Based on the following relevant code and documentation:");
            contextBuilder.AppendLine();

            foreach (var result in searchResults.Take(3))
            {
                var path = result.Metadata.TryGetValue("path", out var pathValue) ? pathValue?.ToString() : "unknown";
                contextBuilder.AppendLine($"**From {path}:**");
                contextBuilder.AppendLine("```");
                contextBuilder.AppendLine(result.Content);
                contextBuilder.AppendLine("```");
                contextBuilder.AppendLine();
            }

            contextBuilder.AppendLine("User question:");
            contextBuilder.AppendLine(originalPrompt);

            return contextBuilder.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enhance prompt with RAG");
            return originalPrompt;
        }
    }

    private string BuildConversationContext(string enhancedPrompt)
    {
        return "You are A3sist, an AI-powered development assistant integrated into Visual Studio. " +
               "Provide helpful, accurate, and concise responses about programming, code analysis, and development practices. " +
               "When showing code examples, use proper syntax highlighting and explain your reasoning.";
    }

    private bool DetectIfCodeResponse(string content)
    {
        return content.Contains("```") || 
               content.Contains("class ") || 
               content.Contains("function ") ||
               content.Contains("public ") ||
               content.Contains("private ") ||
               content.Contains("void ");
    }

    private async Task EnsureChatHistoryLoadedAsync()
    {
        if (_chatHistory.Any()) return;

        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var a3sistPath = Path.Combine(appDataPath, "A3sist");
            var historyPath = Path.Combine(a3sistPath, "chat_history.json");

            if (File.Exists(historyPath))
            {
                await using var stream = new FileStream(historyPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
                var history = await JsonSerializer.DeserializeAsync<List<ChatMessage>>(stream);
                
                if (history != null)
                {
                    await _historyLock.WaitAsync();
                    try
                    {
                        _chatHistory.AddRange(history);
                        _logger.LogInformation("Loaded {Count} messages from chat history", history.Count);
                    }
                    finally
                    {
                        _historyLock.Release();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load chat history");
        }
    }

    private async Task SaveChatHistoryAsync()
    {
        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var a3sistPath = Path.Combine(appDataPath, "A3sist");
            Directory.CreateDirectory(a3sistPath);
            var historyPath = Path.Combine(a3sistPath, "chat_history.json");

            List<ChatMessage> historyToSave;
            await _historyLock.WaitAsync();
            try
            {
                historyToSave = _chatHistory.ToList();
            }
            finally
            {
                _historyLock.Release();
            }

            await using var stream = new FileStream(historyPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
            await JsonSerializer.SerializeAsync(stream, historyToSave, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save chat history");
        }
    }

    private async Task TrimChatHistoryAsync()
    {
        const int maxMessages = 100;
        
        await _historyLock.WaitAsync();
        try
        {
            if (_chatHistory.Count > maxMessages)
            {
                var messagesToRemove = _chatHistory.Count - maxMessages;
                _chatHistory.RemoveRange(0, messagesToRemove);
                _logger.LogInformation("Trimmed {Count} old messages from chat history", messagesToRemove);
            }
        }
        finally
        {
            _historyLock.Release();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _historyLock?.Dispose();
            _disposed = true;
        }
    }
}