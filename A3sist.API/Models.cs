using System;
using System.Collections.Generic;

namespace A3sist.API.Models
{
    public class ConfigurationChangedEventArgs : EventArgs
    {
        public string Key { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
    }

    public class ModelInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Provider { get; set; }
        public ModelType Type { get; set; }
        public string Endpoint { get; set; }
        public string ApiKey { get; set; }
        public string ModelId { get; set; }
        public int MaxTokens { get; set; }
        public double Temperature { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime LastTested { get; set; }
        
        // Additional properties for configuration dialog
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryCount { get; set; } = 3;
        public bool StreamResponse { get; set; } = true;
        public bool EnableLogging { get; set; } = true;
        public string SystemMessage { get; set; }
        public string[] StopSequences { get; set; }
        public double TopP { get; set; } = 0.9;
        public double FrequencyPenalty { get; set; } = 0.0;
        public double PresencePenalty { get; set; } = 0.0;
        public string CustomHeaders { get; set; }
    }

    public enum ModelType
    {
        Local,
        Remote
    }

    public class ModelRequest
    {
        public string Prompt { get; set; }
        public string SystemMessage { get; set; }
        public int MaxTokens { get; set; }
        public double Temperature { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }

    public class ModelResponse
    {
        public string Content { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public int TokensUsed { get; set; }
    }

    public class ModelChangedEventArgs : EventArgs
    {
        public ModelInfo PreviousModel { get; set; }
        public ModelInfo NewModel { get; set; }
    }

    public class MCPServerInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Endpoint { get; set; }
        public string ApiKey { get; set; }
        public MCPServerType Type { get; set; }
        public bool IsConnected { get; set; }
        public List<string> SupportedTools { get; set; }
        
        // Additional properties for configuration dialog
        public int Port { get; set; } = 8080;
        public string Protocol { get; set; } = "HTTP";
        public bool RequiresAuth { get; set; } = false;
        public string Username { get; set; }
        public string Password { get; set; }
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryCount { get; set; } = 3;
        public int MaxConcurrentRequests { get; set; } = 10;
        public int KeepAliveInterval { get; set; } = 60;
        public bool EnableLogging { get; set; } = true;
        public bool AutoReconnect { get; set; } = true;
        public string CustomHeaders { get; set; }
        public string EnvironmentVariables { get; set; }
    }

    public enum MCPServerType
    {
        Local,
        Remote
    }

    public class MCPRequest
    {
        public string Method { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public string Id { get; set; }
    }

    public class MCPResponse
    {
        public object Result { get; set; }
        public MCPError Error { get; set; }
        public string Id { get; set; }
        public bool Success { get; set; }
    }

    public class MCPError
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }

    public class MCPServerStatusChangedEventArgs : EventArgs
    {
        public string ServerId { get; set; }
        public MCPServerStatus Status { get; set; }
        public string Message { get; set; }
        public bool IsConnected { get; set; }
        public string StatusMessage { get; set; }
    }

    public enum MCPServerStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    public class SearchResult
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public string FilePath { get; set; }
        public double Score { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public string DocumentId { get; set; }
    }

    public class IndexingStatus
    {
        public bool IsIndexing { get; set; }
        public int TotalDocuments { get; set; }
        public int IndexedDocuments { get; set; }
        public string CurrentDocument { get; set; }
        public double ProgressPercentage { get; set; }
        public int FilesProcessed { get; set; }
        public int TotalFiles { get; set; }
        public double Progress { get; set; }
        public string CurrentFile { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
    }

    public class LocalRAGConfig
    {
        public string IndexPath { get; set; }
        public string EmbeddingModel { get; set; }
        public int ChunkSize { get; set; }
        public int ChunkOverlap { get; set; }
        public List<string> SupportedExtensions { get; set; }
        public string VectorStoreType { get; set; }
        public string DatabasePath { get; set; }
        public int EmbeddingDimensions { get; set; }
        public double SimilarityThreshold { get; set; }
    }

    public class RemoteRAGConfig
    {
        public string ApiEndpoint { get; set; }
        public string ApiKey { get; set; }
        public string IndexName { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public string Endpoint { get; set; }
        public string EmbeddingModel { get; set; }
    }

    public class IndexingProgressEventArgs : EventArgs
    {
        public IndexingStatus Status { get; set; }
    }

    public class CodeContext
    {
        public string SelectedText { get; set; }
        public string SurroundingCode { get; set; }
        public string FileName { get; set; }
        public string Language { get; set; }
        public int LineNumber { get; set; }
        public int ColumnNumber { get; set; }
        public List<string> ImportStatements { get; set; }
        public List<string> AvailableSymbols { get; set; }
        public string CurrentMethod { get; set; }
        public string CurrentClass { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public List<string> UsingStatements { get; set; }
    }

    public class CodeIssue
    {
        public string Id { get; set; }
        public string Message { get; set; }
        public IssueSeverity Severity { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public string Code { get; set; }
        public string Category { get; set; }
        public List<string> SuggestedFixes { get; set; }
        public string SuggestedFix { get; set; }
    }

    public enum IssueSeverity
    {
        Info,
        Warning,
        Error
    }

    public class SyntaxTree
    {
        public string Language { get; set; }
        public List<SyntaxNode> Nodes { get; set; }
        public string SourceCode { get; set; }
        public List<CodeIssue> Issues { get; set; }
        public SyntaxNode Root { get; set; }
        public string Text { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
    }

    public class SyntaxNode
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }
        public List<SyntaxNode> Children { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public string Text { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
    }

    public class ChatMessage
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public ChatRole Role { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public MessageSender Sender { get; set; }
        public string ModelUsed { get; set; }
        public bool IsCode { get; set; }
    }

    public enum ChatRole
    {
        User,
        Assistant,
        System
    }

    public enum MessageSender
    {
        User,
        Assistant
    }

    public class ChatResponse
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public int TokensUsed { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }

    public class ChatMessageReceivedEventArgs : EventArgs
    {
        public ChatMessage Message { get; set; }
    }

    public class RefactoringSuggestion
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public RefactoringType Type { get; set; }
        public string OriginalCode { get; set; }
        public string RefactoredCode { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public double ConfidenceScore { get; set; }
        public int Priority { get; set; }
    }

    public enum RefactoringType
    {
        ExtractMethod,
        RenameVariable,
        SimplifyExpression,
        RemoveDeadCode,
        OptimizeImports,
        OptimizeUsings,
        ExtractVariable,
        RenameSymbol
    }

    public class RefactoringResult
    {
        public string Id { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public string ModifiedCode { get; set; }
        public List<string> ChangedFiles { get; set; }
    }

    public class RefactoringPreview
    {
        public string Id { get; set; }
        public string OriginalCode { get; set; }
        public string PreviewCode { get; set; }
        public List<CodeChange> Changes { get; set; }
    }

    public class CodeChange
    {
        public ChangeType Type { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public string OriginalText { get; set; }
        public string NewText { get; set; }
    }

    public enum ChangeType
    {
        Addition,
        Deletion,
        Modification
    }

    public class CodeCleanupSuggestion
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public CleanupType Type { get; set; }
        public int Line { get; set; }
        public string OriginalCode { get; set; }
        public string CleanedCode { get; set; }
        public double ImpactScore { get; set; }
    }

    public enum CleanupType
    {
        RemoveUnusedUsings,
        FormatCode,
        SimplifyCode,
        OptimizePerformance,
        ImproveReadability
    }

    public class CompletionItem
    {
        public string Label { get; set; }
        public string Detail { get; set; }
        public string Documentation { get; set; }
        public CompletionItemKind Kind { get; set; }
        public string InsertText { get; set; }
        public int SortOrder { get; set; }
        public bool IsSnippet { get; set; }
        public double Confidence { get; set; }
        public int Priority { get; set; }
    }

    public enum CompletionItemKind
    {
        Text,
        Method,
        Function,
        Constructor,
        Field,
        Variable,
        Class,
        Interface,
        Module,
        Property,
        Unit,
        Value,
        Enum,
        Keyword,
        Snippet,
        Color,
        File,
        Reference
    }

    public class CompletionSettings
    {
        public bool IsEnabled { get; set; }
        public int MaxSuggestions { get; set; }
        public int TriggerDelay { get; set; }
        public List<string> TriggerCharacters { get; set; }
        public bool ShowDocumentation { get; set; }
        public bool EnableAICompletion { get; set; }
    }

    // Agent Mode Models
    public class AgentAnalysisReport
    {
        public string Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string WorkspacePath { get; set; }
        public AgentAnalysisStatus Status { get; set; }
        public int FilesAnalyzed { get; set; }
        public int TotalFiles { get; set; }
        public List<AgentFinding> Findings { get; set; }
        public List<AgentRecommendation> Recommendations { get; set; }
        public TimeSpan TotalTime { get; set; }
        public Dictionary<string, object> Statistics { get; set; }
    }

    public enum AgentAnalysisStatus
    {
        NotStarted,
        Running,
        Paused,
        Completed,
        Failed,
        Cancelled
    }

    public class AgentFinding
    {
        public string Id { get; set; }
        public string FilePath { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public AgentFindingType Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public AgentSeverity Severity { get; set; }
        public double Confidence { get; set; }
        public List<string> SuggestedActions { get; set; }
    }

    public enum AgentFindingType
    {
        CodeSmell,
        SecurityIssue,
        PerformanceIssue,
        BestPracticeViolation,
        PotentialBug,
        DocumentationMissing,
        TestCoverageLow,
        DependencyIssue
    }

    public enum AgentSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public class AgentRecommendation
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public AgentRecommendationType Type { get; set; }
        public int Priority { get; set; }
        public double ImpactScore { get; set; }
        public List<string> AffectedFiles { get; set; }
        public string ActionPlan { get; set; }
    }

    public enum AgentRecommendationType
    {
        Refactoring,
        Architecture,
        Testing,
        Documentation,
        Security,
        Performance,
        Dependencies,
        CodeOrganization
    }

    public class AgentProgressEventArgs : EventArgs
    {
        public string CurrentFile { get; set; }
        public int FilesProcessed { get; set; }
        public int TotalFiles { get; set; }
        public double ProgressPercentage { get; set; }
        public AgentAnalysisStatus Status { get; set; }
        public string StatusMessage { get; set; }
    }

    public class AgentIssueFoundEventArgs : EventArgs
    {
        public AgentFinding Finding { get; set; }
        public string FilePath { get; set; }
    }

    public class AgentAnalysisCompletedEventArgs : EventArgs
    {
        public AgentAnalysisReport Report { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public TimeSpan Duration { get; set; }
    }

    // API-specific request/response models
    public class SearchRequest
    {
        public string Query { get; set; }
        public int MaxResults { get; set; } = 10;
    }

    public class IndexWorkspaceRequest
    {
        public string WorkspacePath { get; set; }
    }

    public class StartAgentAnalysisRequest
    {
        public string WorkspacePath { get; set; }
    }
}