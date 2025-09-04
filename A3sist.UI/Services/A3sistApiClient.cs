using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using A3sist.UI.Models;

namespace A3sist.UI.Services
{
    public interface IA3sistApiClient : IDisposable
    {
        // Connection Management
        Task<bool> ConnectAsync();
        Task DisconnectAsync();
        bool IsConnected { get; }
        
        // Chat API
        Task<ChatResponse> SendChatMessageAsync(ChatMessage message);
        Task<IEnumerable<ChatMessage>> GetChatHistoryAsync();
        Task ClearChatHistoryAsync();
        Task<string> GetActiveChatModelAsync();
        Task<bool> SetActiveChatModelAsync(string modelId);
        
        // Model API
        Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync();
        Task<ModelInfo> GetActiveModelAsync();
        Task<bool> SetActiveModelAsync(string modelId);
        Task<bool> AddModelAsync(ModelInfo model);
        Task<bool> RemoveModelAsync(string modelId);
        Task<bool> TestModelConnectionAsync(string modelId);
        
        // Code Analysis API
        Task<IEnumerable<CodeIssue>> AnalyzeCodeAsync(string code, string language);
        Task<CodeContext> ExtractContextAsync(string code, int position);
        Task<IEnumerable<string>> GetSupportedLanguagesAsync();
        
        // Refactoring API
        Task<IEnumerable<RefactoringSuggestion>> GetRefactoringSuggestionsAsync(string code, string language);
        Task<RefactoringResult> ApplyRefactoringAsync(string suggestionId, string code);
        Task<RefactoringPreview> PreviewRefactoringAsync(string suggestionId, string code);
        
        // RAG API
        Task<bool> IndexWorkspaceAsync(string workspacePath);
        Task<IEnumerable<SearchResult>> SearchAsync(string query, int maxResults = 10);
        Task<IndexingStatus> GetIndexingStatusAsync();
        
        // MCP API
        Task<IEnumerable<MCPServerInfo>> GetMCPServersAsync();
        Task<bool> ConnectToMCPServerAsync(string serverId);
        Task<bool> DisconnectFromMCPServerAsync(string serverId);
        Task<IEnumerable<string>> GetAvailableToolsAsync(string serverId);
        
        // Agent API
        Task<bool> StartAgentAnalysisAsync(string workspacePath);
        Task<bool> StopAgentAnalysisAsync();
        Task<AgentAnalysisReport> GetAgentReportAsync();
        Task<bool> IsAgentAnalysisRunningAsync();
        
        // Auto Complete API
        Task<IEnumerable<CompletionItem>> GetCompletionSuggestionsAsync(string code, int position, string language);
        Task<bool> IsAutoCompleteEnabledAsync();
        Task<bool> SetAutoCompleteEnabledAsync(bool enabled);
        
        // Events for real-time updates
        event EventHandler<ChatMessageReceivedEventArgs> ChatMessageReceived;
        event EventHandler<AgentProgressEventArgs> AgentProgressChanged;
        event EventHandler<ModelChangedEventArgs> ActiveModelChanged;
        event EventHandler<IndexingProgressEventArgs> RAGIndexingProgress;
        event EventHandler<MCPServerStatusChangedEventArgs> MCPServerStatusChanged;
        event EventHandler ConnectionStateChanged;
    }

    public class A3sistApiClient : IA3sistApiClient
    {
        private readonly HttpClient _httpClient;
        private HubConnection _hubConnection;
        private const string API_BASE_URL = "http://localhost:8342/api";
        private const string HUB_URL = "http://localhost:8342/a3sistHub";
        private bool _disposed = false;

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        // Events
        public event EventHandler<ChatMessageReceivedEventArgs> ChatMessageReceived;
        public event EventHandler<AgentProgressEventArgs> AgentProgressChanged;
        public event EventHandler<ModelChangedEventArgs> ActiveModelChanged;
        public event EventHandler<IndexingProgressEventArgs> RAGIndexingProgress;
        public event EventHandler<MCPServerStatusChangedEventArgs> MCPServerStatusChanged;
        public event EventHandler ConnectionStateChanged;

        public A3sistApiClient()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(API_BASE_URL),
                Timeout = TimeSpan.FromMinutes(5) // Longer timeout for heavy operations
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "A3sist-UI/1.0");
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                // Test HTTP connection first
                var healthResponse = await _httpClient.GetAsync("/health");
                if (!healthResponse.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine("A3sist API health check failed");
                    return false;
                }

                // Setup SignalR connection
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(HUB_URL)
                    .WithAutomaticReconnect()
                    .Build();

                SetupSignalREventHandlers();

                await _hubConnection.StartAsync();
                
                ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
                System.Diagnostics.Debug.WriteLine("A3sist API client connected successfully");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to connect to A3sist API: {ex.Message}");
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
                ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void SetupSignalREventHandlers()
        {
            _hubConnection.On<ChatMessage>("ChatMessageReceived", message =>
            {
                ChatMessageReceived?.Invoke(this, new ChatMessageReceivedEventArgs { Message = message });
            });

            _hubConnection.On<AgentProgressEventArgs>("AgentProgressChanged", progress =>
            {
                AgentProgressChanged?.Invoke(this, progress);
            });

            _hubConnection.On<ModelChangedEventArgs>("ActiveModelChanged", modelChange =>
            {
                ActiveModelChanged?.Invoke(this, modelChange);
            });

            _hubConnection.On<IndexingProgressEventArgs>("RAGIndexingProgress", progress =>
            {
                RAGIndexingProgress?.Invoke(this, progress);
            });

            _hubConnection.On<MCPServerStatusChangedEventArgs>("MCPServerStatusChanged", status =>
            {
                MCPServerStatusChanged?.Invoke(this, status);
            });

            _hubConnection.Reconnected += async (connectionId) =>
            {
                ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
                System.Diagnostics.Debug.WriteLine($"A3sist API reconnected: {connectionId}");
            };

            _hubConnection.Closed += async (error) =>
            {
                ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
                if (error != null)
                {
                    System.Diagnostics.Debug.WriteLine($"A3sist API connection closed with error: {error.Message}");
                }
            };
        }

        // Chat API Implementation
        public async Task<ChatResponse> SendChatMessageAsync(ChatMessage message)
        {
            var json = JsonSerializer.Serialize(message);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/chat/send", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ChatResponse>(responseJson, GetJsonOptions());
        }

        public async Task<IEnumerable<ChatMessage>> GetChatHistoryAsync()
        {
            var response = await _httpClient.GetAsync("/chat/history");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<ChatMessage>>(json, GetJsonOptions());
        }

        public async Task ClearChatHistoryAsync()
        {
            var response = await _httpClient.DeleteAsync("/chat/history");
            response.EnsureSuccessStatusCode();
        }

        public async Task<string> GetActiveChatModelAsync()
        {
            var response = await _httpClient.GetAsync("/chat/active-model");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);
            return result.GetProperty("activeModel").GetString();
        }

        public async Task<bool> SetActiveChatModelAsync(string modelId)
        {
            var json = JsonSerializer.Serialize(modelId);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync("/chat/active-model", content);
            return response.IsSuccessStatusCode;
        }

        // Model API Implementation
        public async Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync()
        {
            var response = await _httpClient.GetAsync("/models");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<ModelInfo>>(json, GetJsonOptions());
        }

        public async Task<ModelInfo> GetActiveModelAsync()
        {
            var response = await _httpClient.GetAsync("/models/active");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ModelInfo>(json, GetJsonOptions());
        }

        public async Task<bool> SetActiveModelAsync(string modelId)
        {
            var response = await _httpClient.PutAsync($"/models/{modelId}/activate", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AddModelAsync(ModelInfo model)
        {
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/models", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RemoveModelAsync(string modelId)
        {
            var response = await _httpClient.DeleteAsync($"/models/{modelId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> TestModelConnectionAsync(string modelId)
        {
            var response = await _httpClient.PostAsync($"/models/{modelId}/test", null);
            return response.IsSuccessStatusCode;
        }

        // Code Analysis API Implementation
        public async Task<IEnumerable<CodeIssue>> AnalyzeCodeAsync(string code, string language)
        {
            var request = new { code, language };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/analysis/analyze", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<CodeIssue>>(responseJson, GetJsonOptions());
        }

        public async Task<CodeContext> ExtractContextAsync(string code, int position)
        {
            var request = new { code, position };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/analysis/context", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CodeContext>(responseJson, GetJsonOptions());
        }

        public async Task<IEnumerable<string>> GetSupportedLanguagesAsync()
        {
            var response = await _httpClient.GetAsync("/analysis/languages");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<string>>(json, GetJsonOptions());
        }

        // Refactoring API Implementation
        public async Task<IEnumerable<RefactoringSuggestion>> GetRefactoringSuggestionsAsync(string code, string language)
        {
            var request = new { code, language };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/refactoring/suggestions", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<RefactoringSuggestion>>(responseJson, GetJsonOptions());
        }

        public async Task<RefactoringResult> ApplyRefactoringAsync(string suggestionId, string code)
        {
            var request = new { suggestionId, code };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/refactoring/apply", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RefactoringResult>(responseJson, GetJsonOptions());
        }

        public async Task<RefactoringPreview> PreviewRefactoringAsync(string suggestionId, string code)
        {
            var request = new { suggestionId, code };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/refactoring/preview", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RefactoringPreview>(responseJson, GetJsonOptions());
        }

        // RAG API Implementation
        public async Task<bool> IndexWorkspaceAsync(string workspacePath)
        {
            var request = new { workspacePath };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/rag/index", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<IEnumerable<SearchResult>> SearchAsync(string query, int maxResults = 10)
        {
            var request = new { query, maxResults };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/rag/search", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<SearchResult>>(responseJson, GetJsonOptions());
        }

        public async Task<IndexingStatus> GetIndexingStatusAsync()
        {
            var response = await _httpClient.GetAsync("/rag/status");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IndexingStatus>(json, GetJsonOptions());
        }

        // MCP API Implementation
        public async Task<IEnumerable<MCPServerInfo>> GetMCPServersAsync()
        {
            var response = await _httpClient.GetAsync("/mcp/servers");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<MCPServerInfo>>(json, GetJsonOptions());
        }

        public async Task<bool> ConnectToMCPServerAsync(string serverId)
        {
            var response = await _httpClient.PostAsync($"/mcp/servers/{serverId}/connect", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DisconnectFromMCPServerAsync(string serverId)
        {
            var response = await _httpClient.PostAsync($"/mcp/servers/{serverId}/disconnect", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<IEnumerable<string>> GetAvailableToolsAsync(string serverId)
        {
            var response = await _httpClient.GetAsync($"/mcp/tools?serverId={serverId}");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<string>>(json, GetJsonOptions());
        }

        // Agent API Implementation
        public async Task<bool> StartAgentAnalysisAsync(string workspacePath)
        {
            var request = new { workspacePath };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/agent/start", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> StopAgentAnalysisAsync()
        {
            var response = await _httpClient.PostAsync("/agent/stop", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<AgentAnalysisReport> GetAgentReportAsync()
        {
            var response = await _httpClient.GetAsync("/agent/report");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AgentAnalysisReport>(json, GetJsonOptions());
        }

        public async Task<bool> IsAgentAnalysisRunningAsync()
        {
            var response = await _httpClient.GetAsync("/agent/status");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);
            return result.GetProperty("isRunning").GetBoolean();
        }

        // Auto Complete API Implementation
        public async Task<IEnumerable<CompletionItem>> GetCompletionSuggestionsAsync(string code, int position, string language)
        {
            var request = new { code, position, language };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/autocomplete/suggestions", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<CompletionItem>>(responseJson, GetJsonOptions());
        }

        public async Task<bool> IsAutoCompleteEnabledAsync()
        {
            var response = await _httpClient.GetAsync("/autocomplete/settings");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);
            return result.GetProperty("isEnabled").GetBoolean();
        }

        public async Task<bool> SetAutoCompleteEnabledAsync(bool enabled)
        {
            var request = new { isEnabled = enabled };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync("/autocomplete/settings", content);
            return response.IsSuccessStatusCode;
        }

        private JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _hubConnection?.DisposeAsync();
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
}