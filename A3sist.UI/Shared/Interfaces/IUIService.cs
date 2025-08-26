using System;
using System.Threading.Tasks;
using A3sist.Core.Services;
using A3sist.Shared.Messaging;

namespace A3sist.UI.Shared.Interfaces
{
    /// <summary>
    /// Framework-agnostic UI service interface
    /// </summary>
    public interface IUIService
    {
        Task ShowChatViewAsync();
        Task ShowAgentStatusAsync();
        Task<string?> GetSelectedCodeAsync();
        Task ShowNotificationAsync(string title, string message);
        Task<bool> ShowConfirmationAsync(string title, string message);
    }

    /// <summary>
    /// Framework-agnostic chat service interface
    /// </summary>
    public interface IChatService
    {
        Task<string> SendMessageAsync(string message);
        event EventHandler<MessageReceivedEventArgs> MessageReceived;
        Task<ChatMessage[]> GetHistoryAsync();
        Task ClearHistoryAsync();
        Task StartNewConversationAsync();
    }

    /// <summary>
    /// RAG-specific UI service interface
    /// </summary>
    public interface IRAGUIService
    {
        Task ShowKnowledgeModeAsync();
        Task<RAGContext?> GetCurrentRAGContextAsync();
        Task ShowCitationsAsync(Citation[] citations);
        Task UpdateKnowledgeStatusAsync(string status);
    }

    /// <summary>
    /// Event arguments for message received events
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        public ChatMessage Message { get; set; } = null!;
        public RAGContext? RAGContext { get; set; }
        public Citation[]? Citations { get; set; }
    }

    /// <summary>
    /// Represents a chat message
    /// </summary>
    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; } = string.Empty;
        public bool IsFromUser { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public ChatMessageType Type { get; set; } = ChatMessageType.Text;
        public object? Metadata { get; set; }
    }

    /// <summary>
    /// Types of chat messages
    /// </summary>
    public enum ChatMessageType
    {
        Text,
        Code,
        Error,
        System,
        Typing,
        Knowledge
    }

    /// <summary>
    /// Represents a citation from knowledge sources
    /// </summary>
    public class Citation
    {
        public string Title { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string? Url { get; set; }
        public float Relevance { get; set; }
        public string? Description { get; set; }
    }
}