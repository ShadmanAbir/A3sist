using A3sist.Shared.Enums;
using A3sist.UI.Models.Chat;
using A3sist.UI.Services.Chat;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace A3sist.UI.ViewModels.Chat
{
    /// <summary>
    /// ViewModel for the chat interface
    /// </summary>
    public class ChatInterfaceViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IChatService _chatService;
        private readonly IContextService _contextService;
        private readonly IChatSettingsService _settingsService;
        private readonly ILogger<ChatInterfaceViewModel> _logger;
        private readonly Dispatcher _dispatcher;

        private string _currentMessage = string.Empty;
        private bool _isTyping;
        private bool _isInputEnabled = true;
        private ConnectionStatus _connectionStatus = ConnectionStatus.Disconnected;
        private ChatConversation? _currentConversation;
        private bool _hasFileContext;
        private bool _hasSelectionContext;
        private bool _hasProjectContext;
        private bool _showSuggestions = true;
        private bool _disposed;

        public ObservableCollection<ChatMessage> Messages { get; }

        public string CurrentMessage
        {
            get => _currentMessage;
            set => SetProperty(ref _currentMessage, value);
        }

        public bool IsTyping
        {
            get => _isTyping;
            set => SetProperty(ref _isTyping, value);
        }

        public bool IsInputEnabled
        {
            get => _isInputEnabled;
            set => SetProperty(ref _isInputEnabled, value);
        }

        public ConnectionStatus ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        public bool HasFileContext
        {
            get => _hasFileContext;
            set => SetProperty(ref _hasFileContext, value);
        }

        public bool HasSelectionContext
        {
            get => _hasSelectionContext;
            set => SetProperty(ref _hasSelectionContext, value);
        }

        public bool HasProjectContext
        {
            get => _hasProjectContext;
            set => SetProperty(ref _hasProjectContext, value);
        }

        public bool ShowSuggestions
        {
            get => _showSuggestions;
            set => SetProperty(ref _showSuggestions, value);
        }

        public bool HasActiveContext => HasFileContext || HasSelectionContext || HasProjectContext;

        // Commands
        public ICommand SendMessageCommand { get; }
        public ICommand ClearChatCommand { get; }
        public ICommand NewChatCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand AttachContextCommand { get; }
        public ICommand AttachCurrentFileCommand { get; }
        public ICommand AttachSelectionCommand { get; }
        public ICommand AttachProjectCommand { get; }
        public ICommand AttachErrorsCommand { get; }
        public ICommand ClearContextCommand { get; }
        public ICommand ToggleSuggestionsCommand { get; }

        // Events
        public event EventHandler? ScrollToBottomRequested;
        public event PropertyChangedEventHandler? PropertyChanged;

        public ChatInterfaceViewModel(
            IChatService chatService,
            IContextService contextService,
            IChatSettingsService settingsService,
            ILogger<ChatInterfaceViewModel> logger)
        {
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _contextService = contextService ?? throw new ArgumentNullException(nameof(contextService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dispatcher = Dispatcher.CurrentDispatcher;

            Messages = new ObservableCollection<ChatMessage>();

            // Initialize commands
            SendMessageCommand = new RelayCommand(async () => await SendMessageAsync(), () => CanSendMessage());
            ClearChatCommand = new RelayCommand(async () => await ClearChatAsync());
            NewChatCommand = new RelayCommand(async () => await StartNewChatAsync());
            OpenSettingsCommand = new RelayCommand(() => OpenSettings());
            AttachContextCommand = new RelayCommand(() => { }); // Context menu will handle
            AttachCurrentFileCommand = new RelayCommand(async () => await AttachCurrentFileAsync());
            AttachSelectionCommand = new RelayCommand(async () => await AttachSelectionAsync());
            AttachProjectCommand = new RelayCommand(async () => await AttachProjectAsync());
            AttachErrorsCommand = new RelayCommand(async () => await AttachErrorsAsync());
            ClearContextCommand = new RelayCommand(async () => await ClearContextAsync());
            ToggleSuggestionsCommand = new RelayCommand(() => ShowSuggestions = !ShowSuggestions);

            // Subscribe to chat service events
            _chatService.StreamingResponse += OnStreamingResponse;
            _chatService.ConversationUpdated += OnConversationUpdated;
        }

        /// <summary>
        /// Initializes the view model
        /// </summary>
        public async Task Initialize()
        {
            try
            {
                _logger.LogInformation("Initializing chat interface");

                ConnectionStatus = ConnectionStatus.Connecting;

                // Create or load a conversation
                await StartNewChatAsync();

                // Update context indicators
                await UpdateContextIndicatorsAsync();

                ConnectionStatus = ConnectionStatus.Connected;

                _logger.LogInformation("Chat interface initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing chat interface");
                ConnectionStatus = ConnectionStatus.Error;
            }
        }

        /// <summary>
        /// Sends the current message
        /// </summary>
        private async Task SendMessageAsync()
        {
            if (!CanSendMessage())
                return;

            try
            {
                var messageContent = CurrentMessage.Trim();
                CurrentMessage = string.Empty;
                IsInputEnabled = false;
                IsTyping = true;

                // Add user message to UI immediately
                var userMessage = new ChatMessage
                {
                    Type = ChatMessageType.User,
                    Content = messageContent,
                    Timestamp = DateTime.Now
                };

                await _dispatcher.InvokeAsync(() =>
                {
                    Messages.Add(userMessage);
                    ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
                });

                // Send message to service
                var request = new SendMessageRequest
                {
                    ConversationId = _currentConversation?.Id ?? string.Empty,
                    Content = messageContent,
                    Context = await _contextService.GetCurrentContextAsync()
                };

                var response = await _chatService.SendMessageAsync(request);

                if (!response.Success)
                {
                    // Add error message
                    var errorMessage = new ChatMessage
                    {
                        Type = ChatMessageType.System,
                        Content = $"Error: {response.Error ?? "Failed to process message"}",
                        Timestamp = DateTime.Now
                    };

                    await _dispatcher.InvokeAsync(() =>
                    {
                        Messages.Add(errorMessage);
                        ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");

                var errorMessage = new ChatMessage
                {
                    Type = ChatMessageType.System,
                    Content = $"Error: {ex.Message}",
                    Timestamp = DateTime.Now
                };

                await _dispatcher.InvokeAsync(() =>
                {
                    Messages.Add(errorMessage);
                    ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
                });
            }
            finally
            {
                IsTyping = false;
                IsInputEnabled = true;
            }
        }

        /// <summary>
        /// Determines if a message can be sent
        /// </summary>
        private bool CanSendMessage()
        {
            return IsInputEnabled &&
                   ConnectionStatus == ConnectionStatus.Connected &&
                   !string.IsNullOrWhiteSpace(CurrentMessage);
        }

        /// <summary>
        /// Clears the current chat
        /// </summary>
        private async Task ClearChatAsync()
        {
            try
            {
                await _dispatcher.InvokeAsync(() =>
                {
                    Messages.Clear();
                });

                if (_currentConversation != null)
                {
                    _currentConversation.Messages.Clear();
                    await _chatService.UpdateConversationAsync(_currentConversation);
                }

                _logger.LogInformation("Chat cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing chat");
            }
        }

        /// <summary>
        /// Starts a new chat conversation
        /// </summary>
        private async Task StartNewChatAsync()
        {
            try
            {
                _currentConversation = await _chatService.CreateConversationAsync();

                await _dispatcher.InvokeAsync(() =>
                {
                    Messages.Clear();
                    
                    // Add welcome message
                    Messages.Add(new ChatMessage
                    {
                        Type = ChatMessageType.System,
                        Content = "Welcome to A3sist! I'm here to help you with code analysis, refactoring, documentation, and more. How can I assist you today?",
                        Timestamp = DateTime.Now
                    });
                });

                _logger.LogInformation("Started new chat conversation: {ConversationId}", _currentConversation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting new chat");
            }
        }

        /// <summary>
        /// Opens the settings dialog
        /// </summary>
        private void OpenSettings()
        {
            try
            {
                _dispatcher.Invoke(() =>
                {
                    var dialogLogger = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                        .GetService<ILogger<Components.Chat.ChatSettingsDialogViewModel>>(
                            EditorServiceRegistration.ServiceLocator);
                    
                    var dialog = new Components.Chat.ChatSettingsDialog(_settingsService, dialogLogger);
                    var result = dialog.ShowDialog();
                    
                    if (result == true)
                    {
                        // Settings were saved, apply them
                        ApplySettings();
                        _logger.LogInformation("Settings dialog closed with changes saved");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening settings dialog");
            }
        }

        /// <summary>
        /// Applies current settings to the chat interface
        /// </summary>
        private void ApplySettings()
        {
            try
            {
                var settings = _settingsService.GetSettings();
                ShowSuggestions = settings.ShowSuggestions;
                
                _logger.LogDebug("Applied chat settings to interface");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying settings");
            }
        }

        /// <summary>
        /// Attaches current file context
        /// </summary>
        private async Task AttachCurrentFileAsync()
        {
            try
            {
                var filePath = await _contextService.GetCurrentFilePathAsync();
                if (!string.IsNullOrEmpty(filePath))
                {
                    HasFileContext = true;
                    _logger.LogDebug("Attached current file context: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error attaching current file context");
            }
        }

        /// <summary>
        /// Attaches selected text context
        /// </summary>
        private async Task AttachSelectionAsync()
        {
            try
            {
                var selectedText = await _contextService.GetSelectedTextAsync();
                if (!string.IsNullOrEmpty(selectedText))
                {
                    HasSelectionContext = true;
                    _logger.LogDebug("Attached selection context: {Length} characters", selectedText.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error attaching selection context");
            }
        }

        /// <summary>
        /// Attaches project context
        /// </summary>
        private async Task AttachProjectAsync()
        {
            try
            {
                var projectPath = await _contextService.GetCurrentProjectPathAsync();
                if (!string.IsNullOrEmpty(projectPath))
                {
                    HasProjectContext = true;
                    _logger.LogDebug("Attached project context: {ProjectPath}", projectPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error attaching project context");
            }
        }

        /// <summary>
        /// Attaches error list context
        /// </summary>
        private async Task AttachErrorsAsync()
        {
            try
            {
                var errors = await _contextService.GetCurrentErrorsAsync();
                if (errors.Any())
                {
                    var errorMessage = $"Found {errors.Count} errors/warnings in the current project.";
                    
                    await _dispatcher.InvokeAsync(() =>
                    {
                        Messages.Add(new ChatMessage
                        {
                            Type = ChatMessageType.System,
                            Content = errorMessage,
                            Timestamp = DateTime.Now
                        });
                        
                        ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
                    });

                    _logger.LogDebug("Attached {ErrorCount} errors to context", errors.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error attaching errors context");
            }
        }

        /// <summary>
        /// Clears all context
        /// </summary>
        private async Task ClearContextAsync()
        {
            await Task.Run(() =>
            {
                HasFileContext = false;
                HasSelectionContext = false;
                HasProjectContext = false;
            });

            _logger.LogDebug("Cleared all context");
        }

        /// <summary>
        /// Updates context indicators based on current Visual Studio state
        /// </summary>
        private async Task UpdateContextIndicatorsAsync()
        {
            try
            {
                var context = await _contextService.GetCurrentContextAsync();
                
                HasFileContext = !string.IsNullOrEmpty(context.CurrentFile);
                HasSelectionContext = !string.IsNullOrEmpty(context.SelectedText);
                HasProjectContext = !string.IsNullOrEmpty(context.ProjectPath);
                
                OnPropertyChanged(nameof(HasActiveContext));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating context indicators");
            }
        }

        #region Event Handlers

        private void OnStreamingResponse(object? sender, StreamingResponseChunk chunk)
        {
            // Handle streaming responses for real-time updates
            _dispatcher.InvokeAsync(() =>
            {
                var existingMessage = Messages.FirstOrDefault(m => m.Id == chunk.MessageId);
                if (existingMessage != null)
                {
                    existingMessage.Content += chunk.Content;
                }
                else if (!string.IsNullOrEmpty(chunk.Content))
                {
                    Messages.Add(new ChatMessage
                    {
                        Id = chunk.MessageId,
                        Type = ChatMessageType.Assistant,
                        Content = chunk.Content,
                        Timestamp = DateTime.Now,
                        Metadata = chunk.Metadata
                    });
                }

                if (chunk.IsComplete)
                {
                    ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
                }
            });
        }

        private void OnConversationUpdated(object? sender, string conversationId)
        {
            if (conversationId == _currentConversation?.Id)
            {
                // Refresh conversation data
                Task.Run(async () =>
                {
                    try
                    {
                        var updatedConversation = await _chatService.GetConversationAsync(conversationId);
                        if (updatedConversation != null)
                        {
                            _currentConversation = updatedConversation;
                            
                            await _dispatcher.InvokeAsync(() =>
                            {
                                Messages.Clear();
                                foreach (var message in updatedConversation.Messages)
                                {
                                    Messages.Add(message);
                                }
                                
                                ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error refreshing conversation");
                    }
                });
            }
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _chatService.StreamingResponse -= OnStreamingResponse;
                _chatService.ConversationUpdated -= OnConversationUpdated;
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Simple implementation of ICommand for the ViewModel
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }
}