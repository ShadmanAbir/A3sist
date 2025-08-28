using A3sist.API.Models;
using A3sist.API.Services;
using System.Collections.Concurrent;
using System.Text.Json;

namespace A3sist.API.Services;

public class ModelManagementService : IModelManagementService, IDisposable
{
    private readonly ILogger<ModelManagementService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConcurrentDictionary<string, ModelInfo> _models;
    private readonly SemaphoreSlim _semaphore;
    private ModelInfo? _activeModel;
    private bool _disposed;

    public event EventHandler<ModelChangedEventArgs>? ActiveModelChanged;

    public ModelManagementService(ILogger<ModelManagementService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _models = new ConcurrentDictionary<string, ModelInfo>();
        _semaphore = new SemaphoreSlim(1, 1);
        
        // Initialize with default models
        _ = Task.Run(LoadDefaultModelsAsync);
    }

    public async Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _models.Values.ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<ModelInfo?> GetActiveModelAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _activeModel;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> SetActiveModelAsync(string modelId)
    {
        if (string.IsNullOrEmpty(modelId))
            return false;

        await _semaphore.WaitAsync();
        try
        {
            if (!_models.TryGetValue(modelId, out var model))
            {
                _logger.LogWarning("Model {ModelId} not found", modelId);
                return false;
            }

            // Test model connection before setting as active
            var isAvailable = await TestModelConnectionInternalAsync(model);
            if (!isAvailable)
            {
                _logger.LogWarning("Model {ModelId} is not available", modelId);
                return false;
            }

            var previousModel = _activeModel;
            _activeModel = model;

            // Raise event
            ActiveModelChanged?.Invoke(this, new ModelChangedEventArgs
            {
                PreviousModel = previousModel,
                NewModel = model
            });

            _logger.LogInformation("Active model changed to {ModelName}", model.Name);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> AddModelAsync(ModelInfo model)
    {
        if (model == null || string.IsNullOrEmpty(model.Id))
            return false;

        await _semaphore.WaitAsync();
        try
        {
            // Test model connection
            model.IsAvailable = await TestModelConnectionInternalAsync(model);
            model.LastTested = DateTime.UtcNow;

            _models.AddOrUpdate(model.Id, model, (key, existing) => model);
            
            _logger.LogInformation("Model {ModelName} added", model.Name);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> RemoveModelAsync(string modelId)
    {
        if (string.IsNullOrEmpty(modelId))
            return false;

        await _semaphore.WaitAsync();
        try
        {
            var removed = _models.TryRemove(modelId, out var model);
            
            // If this was the active model, clear it
            if (removed && _activeModel?.Id == modelId)
            {
                var previousModel = _activeModel;
                _activeModel = null;
                
                ActiveModelChanged?.Invoke(this, new ModelChangedEventArgs
                {
                    PreviousModel = previousModel,
                    NewModel = null
                });
            }

            if (removed)
                _logger.LogInformation("Model {ModelName} removed", model?.Name);

            return removed;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> TestModelConnectionAsync(string modelId)
    {
        if (string.IsNullOrEmpty(modelId))
            return false;

        await _semaphore.WaitAsync();
        try
        {
            if (!_models.TryGetValue(modelId, out var model))
                return false;

            var isAvailable = await TestModelConnectionInternalAsync(model);
            
            // Update model availability
            model.IsAvailable = isAvailable;
            model.LastTested = DateTime.UtcNow;
            
            return isAvailable;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<ModelResponse> SendRequestAsync(ModelRequest request)
    {
        if (request == null)
            return new ModelResponse { Success = false, Error = "Request is null" };

        var model = await GetActiveModelAsync();
        if (model == null)
            return new ModelResponse { Success = false, Error = "No active model" };

        var startTime = DateTime.UtcNow;
        
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(model.TimeoutSeconds);

            // Add API key if required
            if (!string.IsNullOrEmpty(model.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {model.ApiKey}");
            }

            // Add custom headers if specified
            if (!string.IsNullOrEmpty(model.CustomHeaders))
            {
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(model.CustomHeaders);
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
            }

            // Build request payload
            var payload = new
            {
                model = model.ModelId,
                prompt = request.Prompt,
                system = request.SystemMessage ?? model.SystemMessage,
                max_tokens = request.MaxTokens > 0 ? request.MaxTokens : model.MaxTokens,
                temperature = request.Temperature > 0 ? request.Temperature : model.Temperature,
                top_p = model.TopP,
                frequency_penalty = model.FrequencyPenalty,
                presence_penalty = model.PresencePenalty,
                stop = model.StopSequences,
                stream = model.StreamResponse
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            using var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(model.Endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Parse response based on model type
                var result = await ParseModelResponseAsync(responseContent, model);
                result.ProcessingTime = DateTime.UtcNow - startTime;
                
                _logger.LogInformation("Model request completed in {Duration}ms", result.ProcessingTime.TotalMilliseconds);
                return result;
            }
            else
            {
                _logger.LogError("Model request failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new ModelResponse
                {
                    Success = false,
                    Error = $"HTTP {response.StatusCode}: {responseContent}",
                    ProcessingTime = DateTime.UtcNow - startTime
                };
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError("Model request timed out after {Timeout}s", model.TimeoutSeconds);
            return new ModelResponse
            {
                Success = false,
                Error = "Request timed out",
                ProcessingTime = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending request to model {ModelName}", model.Name);
            return new ModelResponse
            {
                Success = false,
                Error = ex.Message,
                ProcessingTime = DateTime.UtcNow - startTime
            };
        }
    }

    private async Task<bool> TestModelConnectionInternalAsync(ModelInfo model)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10); // Quick test timeout

            // Add API key if required
            if (!string.IsNullOrEmpty(model.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {model.ApiKey}");
            }

            // Simple test request
            var testPayload = new
            {
                model = model.ModelId,
                prompt = "Hello",
                max_tokens = 1,
                temperature = 0.1
            };

            var jsonContent = JsonSerializer.Serialize(testPayload);
            using var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(model.Endpoint, content);
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest; // BadRequest might indicate endpoint is alive but request format is wrong
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Model connection test failed for {ModelName}: {Error}", model.Name, ex.Message);
            return false;
        }
    }

    private async Task<ModelResponse> ParseModelResponseAsync(string responseContent, ModelInfo model)
    {
        try
        {
            using var document = JsonDocument.Parse(responseContent);
            var root = document.RootElement;

            // Handle different response formats based on model provider
            string content = "";
            int tokensUsed = 0;

            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                // OpenAI/compatible format
                var choice = choices[0];
                if (choice.TryGetProperty("message", out var message) && 
                    message.TryGetProperty("content", out var messageContent))
                {
                    content = messageContent.GetString() ?? "";
                }
                else if (choice.TryGetProperty("text", out var text))
                {
                    content = text.GetString() ?? "";
                }
            }
            else if (root.TryGetProperty("response", out var response))
            {
                // Alternative format
                content = response.GetString() ?? "";
            }
            else if (root.TryGetProperty("content", out var directContent))
            {
                // Direct content format
                content = directContent.GetString() ?? "";
            }

            // Extract token usage if available
            if (root.TryGetProperty("usage", out var usage) && 
                usage.TryGetProperty("total_tokens", out var tokens))
            {
                tokensUsed = tokens.GetInt32();
            }

            return new ModelResponse
            {
                Content = content,
                Success = true,
                TokensUsed = tokensUsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing model response");
            return new ModelResponse
            {
                Success = false,
                Error = "Failed to parse response",
                Content = responseContent // Return raw content as fallback
            };
        }
    }

    private async Task LoadDefaultModelsAsync()
    {
        try
        {
            // Add some default models
            var defaultModels = new[]
            {
                new ModelInfo
                {
                    Id = "openai-gpt-4",
                    Name = "GPT-4",
                    Provider = "OpenAI",
                    Type = ModelType.Remote,
                    Endpoint = "https://api.openai.com/v1/chat/completions",
                    ModelId = "gpt-4",
                    MaxTokens = 4096,
                    Temperature = 0.7,
                    IsAvailable = false,
                    TimeoutSeconds = 30,
                    RetryCount = 3,
                    StreamResponse = false,
                    EnableLogging = true
                },
                new ModelInfo
                {
                    Id = "openai-gpt-3.5-turbo",
                    Name = "GPT-3.5 Turbo",
                    Provider = "OpenAI",
                    Type = ModelType.Remote,
                    Endpoint = "https://api.openai.com/v1/chat/completions",
                    ModelId = "gpt-3.5-turbo",
                    MaxTokens = 4096,
                    Temperature = 0.7,
                    IsAvailable = false,
                    TimeoutSeconds = 30,
                    RetryCount = 3,
                    StreamResponse = false,
                    EnableLogging = true
                },
                new ModelInfo
                {
                    Id = "local-ollama",
                    Name = "Local Ollama",
                    Provider = "Ollama",
                    Type = ModelType.Local,
                    Endpoint = "http://localhost:11434/api/generate",
                    ModelId = "llama2",
                    MaxTokens = 2048,
                    Temperature = 0.7,
                    IsAvailable = false,
                    TimeoutSeconds = 60,
                    RetryCount = 3,
                    StreamResponse = false,
                    EnableLogging = true
                }
            };

            foreach (var model in defaultModels)
            {
                await AddModelAsync(model);
            }

            _logger.LogInformation("Loaded {Count} default models", defaultModels.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading default models");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _semaphore.Dispose();
            _disposed = true;
        }
    }
}