using System;
using System.Collections.Generic;

namespace A3sist.UI.Models
{
    // Configuration Models
    public class ConfigurationChangedEventArgs : EventArgs
    {
        public string Key { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
    }

    // Model Management Models
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
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryCount { get; set; } = 3;
        public bool StreamResponse { get; set; } = true;
        public bool EnableLogging { get; set; } = true;
        public string SystemMessage { get; set; }
    }

    public enum ModelType
    {
        Local,
        Remote
    }

    public class ModelChangedEventArgs : EventArgs
    {
        public ModelInfo PreviousModel { get; set; }
        public ModelInfo NewModel { get; set; }
    }

    // Chat Models
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

    // Code Analysis Models
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
    }

    public enum IssueSeverity
    {
        Info,
        Warning,
        Error
    }

    // Refactoring Models
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

    // Auto Complete Models
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

    // RAG Models
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

    public class IndexingProgressEventArgs : EventArgs
    {
        public IndexingStatus Status { get; set; }
    }

    // MCP Models
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
    }

    public enum MCPServerType
    {
        Local,
        Remote
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

    // Agent Models
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
}