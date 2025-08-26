using A3sist.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace A3sist.UI.Models.Chat
{
    /// <summary>
    /// Represents a chat message in the conversation
    /// </summary>
    public class ChatMessage : INotifyPropertyChanged
    {
        private string _content = string.Empty;
        private bool _hasCodeSuggestion;
        private ChatMessageMetadata? _metadata;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public ChatMessageType Type { get; set; }
        
        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }
        
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        public string? UserId { get; set; }
        
        public bool HasCodeSuggestion
        {
            get => _hasCodeSuggestion;
            set => SetProperty(ref _hasCodeSuggestion, value);
        }

        public ChatMessageMetadata? Metadata
        {
            get => _metadata;
            set => SetProperty(ref _metadata, value);
        }

        // Commands for message actions
        public ICommand? CopyCodeCommand { get; set; }
        public ICommand? ApplyCodeCommand { get; set; }
        public ICommand? ExplainCodeCommand { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    /// <summary>
    /// Metadata associated with a chat message
    /// </summary>
    public class ChatMessageMetadata
    {
        public string? SourceFile { get; set; }
        public string? Language { get; set; }
        public List<string> ToolsUsed { get; set; } = new();
        public List<string> ServersInvolved { get; set; } = new();
        public double ProcessingTimeMs { get; set; }
        public string? CodeSuggestion { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    /// <summary>
    /// Context information for chat requests
    /// </summary>
    public class ChatContext
    {
        public string? CurrentFile { get; set; }
        public string? SelectedText { get; set; }
        public string? ProjectPath { get; set; }
        public List<string> OpenFiles { get; set; } = new();
        public List<CompilerError> Errors { get; set; } = new();
        public Dictionary<string, object> EnvironmentInfo { get; set; } = new();
    }

    /// <summary>
    /// Compiler error information
    /// </summary>
    public class CompilerError
    {
        public string? File { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public string? Message { get; set; }
        public string? Severity { get; set; }
        public string? Code { get; set; }
    }

    /// <summary>
    /// Chat conversation information
    /// </summary>
    public class ChatConversation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "New Conversation";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastActivity { get; set; } = DateTime.Now;
        public List<ChatMessage> Messages { get; set; } = new();
        public ChatContext? Context { get; set; }
        public bool IsPinned { get; set; }
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    /// <summary>
    /// Request to send a chat message
    /// </summary>
    public class SendMessageRequest
    {
        public string ConversationId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public ChatContext? Context { get; set; }
        public Dictionary<string, object> Options { get; set; } = new();
    }

    /// <summary>
    /// Response from sending a chat message
    /// </summary>
    public class SendMessageResponse
    {
        public bool Success { get; set; }
        public string? MessageId { get; set; }
        public string? Error { get; set; }
        public ChatMessageMetadata? Metadata { get; set; }
    }

    /// <summary>
    /// Streaming response chunk for real-time updates
    /// </summary>
    public class StreamingResponseChunk
    {
        public string ConversationId { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
        public bool IsTypingIndicator { get; set; }
        public ChatMessageMetadata? Metadata { get; set; }
    }
}