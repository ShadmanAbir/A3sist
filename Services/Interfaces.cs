using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using A3sist.Models;

namespace A3sist.Services
{
    /// <summary>
    /// Main configuration service for A3sist settings
    /// </summary>
    public interface IA3sistConfigurationService
    {
        Task<bool> SaveConfigurationAsync();
        Task<bool> LoadConfigurationAsync();
        Task<T> GetSettingAsync<T>(string key, T defaultValue = default);
        Task SetSettingAsync<T>(string key, T value);
        event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
    }

    /// <summary>
    /// Service for managing local and remote AI models
    /// </summary>
    public interface IModelManagementService
    {
        Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync();
        Task<ModelInfo> GetActiveModelAsync();
        Task<bool> SetActiveModelAsync(string modelId);
        Task<bool> AddModelAsync(ModelInfo model);
        Task<bool> RemoveModelAsync(string modelId);
        Task<bool> TestModelConnectionAsync(string modelId);
        Task<ModelResponse> SendRequestAsync(ModelRequest request);
        event EventHandler<ModelChangedEventArgs> ActiveModelChanged;
    }

    /// <summary>
    /// Service for MCP (Model Control Protocol) client operations
    /// </summary>
    public interface IMCPClientService
    {
        Task<bool> ConnectToServerAsync(MCPServerInfo serverInfo);
        Task<bool> DisconnectFromServerAsync(string serverId);
        Task<MCPResponse> SendRequestAsync(MCPRequest request);
        Task<IEnumerable<MCPServerInfo>> GetAvailableServersAsync();
        Task<IEnumerable<string>> GetAvailableToolsAsync(string serverId);
        Task<bool> TestServerConnectionAsync(string serverId);
        event EventHandler<MCPServerStatusChangedEventArgs> ServerStatusChanged;
    }

    /// <summary>
    /// Service for RAG (Retrieval-Augmented Generation) operations
    /// </summary>
    public interface IRAGEngineService
    {
        Task<bool> IndexWorkspaceAsync(string workspacePath);
        Task<IEnumerable<SearchResult>> SearchAsync(string query, int maxResults = 10);
        Task<bool> AddDocumentAsync(string documentPath, string content);
        Task<bool> RemoveDocumentAsync(string documentPath);
        Task<IndexingStatus> GetIndexingStatusAsync();
        Task<bool> ConfigureLocalRAGAsync(LocalRAGConfig config);
        Task<bool> ConfigureRemoteRAGAsync(RemoteRAGConfig config);
        event EventHandler<IndexingProgressEventArgs> IndexingProgress;
    }

    /// <summary>
    /// Service for code analysis and language detection
    /// </summary>
    public interface ICodeAnalysisService
    {
        Task<string> DetectLanguageAsync(string code, string fileName = null);
        Task<CodeContext> ExtractContextAsync(string code, int position);
        Task<IEnumerable<CodeIssue>> AnalyzeCodeAsync(string code, string language);
        Task<SyntaxTree> GetSyntaxTreeAsync(string code, string language);
        Task<IEnumerable<string>> GetSupportedLanguagesAsync();
    }

    /// <summary>
    /// Service for chat functionality with AI models
    /// </summary>
    public interface IChatService
    {
        Task<ChatResponse> SendMessageAsync(ChatMessage message);
        Task<IEnumerable<ChatMessage>> GetChatHistoryAsync();
        Task ClearChatHistoryAsync();
        Task<bool> SetChatModelAsync(string modelId);
        Task<string> GetActiveChatModelAsync();
        event EventHandler<ChatMessageReceivedEventArgs> MessageReceived;
    }

    /// <summary>
    /// Service for code refactoring operations
    /// </summary>
    public interface IRefactoringService
    {
        Task<IEnumerable<RefactoringSuggestion>> GetRefactoringSuggestionsAsync(string code, string language);
        Task<RefactoringResult> ApplyRefactoringAsync(RefactoringSuggestion suggestion);
        Task<RefactoringPreview> PreviewRefactoringAsync(RefactoringSuggestion suggestion);
        Task<bool> RollbackRefactoringAsync(string refactoringId);
        Task<IEnumerable<CodeCleanupSuggestion>> GetCleanupSuggestionsAsync(string code, string language);
    }

    /// <summary>
    /// Service for AI-powered autocomplete functionality
    /// </summary>
    public interface IAutoCompleteService
    {
        Task<IEnumerable<CompletionItem>> GetCompletionsAsync(string code, int position, string language);
        Task<bool> IsAutoCompleteEnabledAsync();
        Task SetAutoCompleteEnabledAsync(bool enabled);
        Task<CompletionSettings> GetCompletionSettingsAsync();
        Task SetCompletionSettingsAsync(CompletionSettings settings);
    }
}