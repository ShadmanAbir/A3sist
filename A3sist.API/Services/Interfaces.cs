using A3sist.API.Models;

namespace A3sist.API.Services;

/// <summary>
/// Service for managing local and remote AI models
/// </summary>
public interface IModelManagementService
{
    Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync();
    Task<ModelInfo?> GetActiveModelAsync();
    Task<bool> SetActiveModelAsync(string modelId);
    Task<bool> AddModelAsync(ModelInfo model);
    Task<bool> RemoveModelAsync(string modelId);
    Task<bool> TestModelConnectionAsync(string modelId);
    Task<ModelResponse> SendRequestAsync(ModelRequest request);
    event EventHandler<ModelChangedEventArgs>? ActiveModelChanged;
}

/// <summary>
/// Service for MCP (Model Context Protocol) operations
/// </summary>
public interface IMCPClientService
{
    Task<bool> ConnectToServerAsync(MCPServerInfo serverInfo);
    Task<bool> DisconnectFromServerAsync(string serverId);
    Task<MCPResponse> SendRequestAsync(MCPRequest request);
    Task<IEnumerable<MCPServerInfo>> GetAvailableServersAsync();
    Task<IEnumerable<string>> GetAvailableToolsAsync(string serverId);
    Task<bool> TestServerConnectionAsync(string serverId);
    Task<bool> AddServerAsync(MCPServerInfo serverInfo);
    Task<bool> RemoveServerAsync(string serverId);
    event EventHandler<MCPServerStatusChangedEventArgs>? ServerStatusChanged;
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
    event EventHandler<IndexingProgressEventArgs>? IndexingProgress;
}

/// <summary>
/// Service for code analysis capabilities
/// </summary>
public interface ICodeAnalysisService
{
    Task<string> DetectLanguageAsync(string code, string? fileName = null);
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
    Task<string?> GetActiveChatModelAsync();
    event EventHandler<ChatMessageReceivedEventArgs>? MessageReceived;
}

/// <summary>
/// Service for code refactoring suggestions and operations
/// </summary>
public interface IRefactoringService
{
    Task<IEnumerable<RefactoringSuggestion>> GetRefactoringSuggestionsAsync(string code, string language);
    Task<RefactoringResult> ApplyRefactoringAsync(string suggestionId, string code);
    Task<RefactoringPreview> PreviewRefactoringAsync(string suggestionId, string code);
    Task<IEnumerable<CodeCleanupSuggestion>> GetCleanupSuggestionsAsync(string code, string language);
}

/// <summary>
/// Service for AI-powered auto-completion
/// </summary>
public interface IAutoCompleteService
{
    Task<IEnumerable<CompletionItem>> GetCompletionSuggestionsAsync(string code, int position, string language);
    Task<bool> IsAutoCompleteEnabledAsync();
    Task<bool> SetAutoCompleteEnabledAsync(bool enabled);
    Task<CompletionSettings> GetSettingsAsync();
    Task<bool> UpdateSettingsAsync(CompletionSettings settings);
}

/// <summary>
/// Service for autonomous agent mode analysis
/// </summary>
public interface IAgentModeService
{
    Task<bool> StartAnalysisAsync(string workspacePath);
    Task<bool> StopAnalysisAsync();
    Task<AgentAnalysisReport> GetCurrentReportAsync();
    Task<bool> IsAnalysisRunningAsync();
    event EventHandler<AgentProgressEventArgs>? ProgressChanged;
    event EventHandler<AgentIssueFoundEventArgs>? IssueFound;
    event EventHandler<AgentAnalysisCompletedEventArgs>? AnalysisCompleted;
}