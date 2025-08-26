using A3sist.UI.Models.Chat;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace A3sist.UI.Services.Chat
{
    /// <summary>
    /// Service for managing chat history persistence
    /// </summary>
    public interface IChatHistoryService
    {
        Task SaveConversationAsync(ChatConversation conversation);
        Task<ChatConversation?> LoadConversationAsync(string conversationId);
        Task<IEnumerable<ChatConversation>> LoadAllConversationsAsync();
        Task DeleteConversationAsync(string conversationId);
        Task ClearAllHistoryAsync();
        Task ExportConversationAsync(string conversationId, string filePath);
        Task<ChatConversation?> ImportConversationAsync(string filePath);
    }

    /// <summary>
    /// File-based implementation of chat history service
    /// </summary>
    public class FileBasedChatHistoryService : IChatHistoryService
    {
        private readonly ILogger<FileBasedChatHistoryService> _logger;
        private readonly string _historyDirectory;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly SemaphoreSlim _fileLock;

        public FileBasedChatHistoryService(ILogger<FileBasedChatHistoryService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Create history directory in user's AppData
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _historyDirectory = Path.Combine(appDataPath, "A3sist", "ChatHistory");
            
            Directory.CreateDirectory(_historyDirectory);
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            _fileLock = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Saves a conversation to disk
        /// </summary>
        public async Task SaveConversationAsync(ChatConversation conversation)
        {
            await _fileLock.WaitAsync();
            
            try
            {
                var filePath = GetConversationFilePath(conversation.Id);
                var json = JsonSerializer.Serialize(conversation, _jsonOptions);
                
                await File.WriteAllTextAsync(filePath, json);
                
                _logger.LogDebug("Saved conversation {ConversationId} to {FilePath}", 
                    conversation.Id, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving conversation {ConversationId}", conversation.Id);
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// Loads a specific conversation from disk
        /// </summary>
        public async Task<ChatConversation?> LoadConversationAsync(string conversationId)
        {
            await _fileLock.WaitAsync();
            
            try
            {
                var filePath = GetConversationFilePath(conversationId);
                
                if (!File.Exists(filePath))
                {
                    _logger.LogDebug("Conversation file not found: {FilePath}", filePath);
                    return null;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var conversation = JsonSerializer.Deserialize<ChatConversation>(json, _jsonOptions);
                
                _logger.LogDebug("Loaded conversation {ConversationId} from {FilePath}", 
                    conversationId, filePath);
                
                return conversation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conversation {ConversationId}", conversationId);
                return null;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// Loads all conversations from disk
        /// </summary>
        public async Task<IEnumerable<ChatConversation>> LoadAllConversationsAsync()
        {
            await _fileLock.WaitAsync();
            
            try
            {
                var conversations = new List<ChatConversation>();
                var files = Directory.GetFiles(_historyDirectory, "*.json");
                
                foreach (var file in files)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var conversation = JsonSerializer.Deserialize<ChatConversation>(json, _jsonOptions);
                        
                        if (conversation != null)
                        {
                            conversations.Add(conversation);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error loading conversation from file {FilePath}", file);
                        // Continue loading other conversations
                    }
                }
                
                _logger.LogInformation("Loaded {ConversationCount} conversations from history", 
                    conversations.Count);
                
                return conversations.OrderByDescending(c => c.LastActivity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading all conversations");
                return Enumerable.Empty<ChatConversation>();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// Deletes a conversation from disk
        /// </summary>
        public async Task DeleteConversationAsync(string conversationId)
        {
            await _fileLock.WaitAsync();
            
            try
            {
                var filePath = GetConversationFilePath(conversationId);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Deleted conversation {ConversationId}", conversationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation {ConversationId}", conversationId);
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// Clears all chat history
        /// </summary>
        public async Task ClearAllHistoryAsync()
        {
            await _fileLock.WaitAsync();
            
            try
            {
                var files = Directory.GetFiles(_historyDirectory, "*.json");
                
                foreach (var file in files)
                {
                    File.Delete(file);
                }
                
                _logger.LogInformation("Cleared all chat history ({FileCount} files)", files.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all chat history");
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// Exports a conversation to a specified file
        /// </summary>
        public async Task ExportConversationAsync(string conversationId, string filePath)
        {
            try
            {
                var conversation = await LoadConversationAsync(conversationId);
                
                if (conversation == null)
                {
                    throw new ArgumentException($"Conversation {conversationId} not found");
                }

                // Create export format
                var export = new
                {
                    ExportedAt = DateTime.Now,
                    A3sistVersion = GetType().Assembly.GetName().Version?.ToString(),
                    Conversation = conversation
                };

                var json = JsonSerializer.Serialize(export, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                
                _logger.LogInformation("Exported conversation {ConversationId} to {FilePath}", 
                    conversationId, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting conversation {ConversationId}", conversationId);
                throw;
            }
        }

        /// <summary>
        /// Imports a conversation from a file
        /// </summary>
        public async Task<ChatConversation?> ImportConversationAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Import file not found: {filePath}");
                }

                var json = await File.ReadAllTextAsync(filePath);
                
                // Try to deserialize as export format first
                try
                {
                    var export = JsonSerializer.Deserialize<JsonElement>(json, _jsonOptions);
                    
                    if (export.TryGetProperty("conversation", out var conversationElement))
                    {
                        var conversation = JsonSerializer.Deserialize<ChatConversation>(
                            conversationElement.GetRawText(), _jsonOptions);
                        
                        if (conversation != null)
                        {
                            // Generate new ID to avoid conflicts
                            conversation.Id = Guid.NewGuid().ToString();
                            conversation.Title += " (Imported)";
                            
                            await SaveConversationAsync(conversation);
                            
                            _logger.LogInformation("Imported conversation from {FilePath}", filePath);
                            return conversation;
                        }
                    }
                }
                catch
                {
                    // Try direct conversation format
                    var conversation = JsonSerializer.Deserialize<ChatConversation>(json, _jsonOptions);
                    
                    if (conversation != null)
                    {
                        conversation.Id = Guid.NewGuid().ToString();
                        conversation.Title += " (Imported)";
                        
                        await SaveConversationAsync(conversation);
                        
                        _logger.LogInformation("Imported conversation from {FilePath}", filePath);
                        return conversation;
                    }
                }
                
                throw new InvalidDataException("Invalid conversation file format");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing conversation from {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Gets the file path for a conversation
        /// </summary>
        private string GetConversationFilePath(string conversationId)
        {
            var fileName = $"conversation_{conversationId}.json";
            return Path.Combine(_historyDirectory, fileName);
        }

        /// <summary>
        /// Performs cleanup tasks like removing old conversations
        /// </summary>
        public async Task PerformMaintenanceAsync(int maxConversations = 1000, TimeSpan? maxAge = null)
        {
            await _fileLock.WaitAsync();
            
            try
            {
                var conversations = (await LoadAllConversationsAsync()).ToList();
                
                // Remove conversations exceeding max count
                if (conversations.Count > maxConversations)
                {
                    var toRemove = conversations
                        .OrderBy(c => c.LastActivity)
                        .Take(conversations.Count - maxConversations);
                    
                    foreach (var conversation in toRemove)
                    {
                        await DeleteConversationAsync(conversation.Id);
                    }
                    
                    _logger.LogInformation("Removed {Count} old conversations to maintain max limit", 
                        conversations.Count - maxConversations);
                }
                
                // Remove conversations older than max age
                if (maxAge.HasValue)
                {
                    var cutoffDate = DateTime.Now - maxAge.Value;
                    var oldConversations = conversations.Where(c => c.LastActivity < cutoffDate);
                    
                    foreach (var conversation in oldConversations)
                    {
                        await DeleteConversationAsync(conversation.Id);
                    }
                    
                    _logger.LogInformation("Removed {Count} conversations older than {MaxAge}", 
                        oldConversations.Count(), maxAge.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during chat history maintenance");
            }
            finally
            {
                _fileLock.Release();
            }
        }
    }
}