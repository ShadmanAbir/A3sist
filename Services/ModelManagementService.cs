using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using A3sist.Models;

namespace A3sist.Services
{
    public class ModelManagementService : IModelManagementService
    {
        private readonly IA3sistConfigurationService _configService;
        private readonly HttpClient _httpClient;
        private readonly List<ModelInfo> _models;
        private ModelInfo _activeModel;
        private readonly object _lockObject = new object();

        public event EventHandler<ModelChangedEventArgs> ActiveModelChanged;

        public ModelManagementService(IA3sistConfigurationService configService)
        {
            _configService = configService;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _models = new List<ModelInfo>();
            
            // Initialize with default models
            InitializeDefaultModels();
        }

        public async Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync()
        {
            lock (_lockObject)
            {
                return _models.ToList();
            }
        }

        public async Task<ModelInfo> GetActiveModelAsync()
        {
            if (_activeModel == null)
            {
                var activeModelId = await _configService.GetSettingAsync<string>("models.activeModelId");
                if (!string.IsNullOrEmpty(activeModelId))
                {
                    lock (_lockObject)
                    {
                        _activeModel = _models.FirstOrDefault(m => m.Id == activeModelId);
                    }
                }

                // If no active model or not found, set first available model
                if (_activeModel == null)
                {
                    lock (_lockObject)
                    {
                        _activeModel = _models.FirstOrDefault(m => m.IsAvailable);
                    }
                }
            }

            return _activeModel;
        }

        public async Task<bool> SetActiveModelAsync(string modelId)
        {
            ModelInfo model;
            lock (_lockObject)
            {
                model = _models.FirstOrDefault(m => m.Id == modelId);
            }

            if (model == null || !model.IsAvailable)
                return false;

            var previousModel = _activeModel;
            _activeModel = model;

            await _configService.SetSettingAsync("models.activeModelId", modelId);

            ActiveModelChanged?.Invoke(this, new ModelChangedEventArgs
            {
                PreviousModel = previousModel,
                NewModel = model
            });

            return true;
        }

        public async Task<bool> AddModelAsync(ModelInfo model)
        {
            if (string.IsNullOrEmpty(model.Id))
                model.Id = Guid.NewGuid().ToString();

            // Test the model connection
            var connectionTest = await TestModelConnectionAsync(model.Id, model);
            model.IsAvailable = connectionTest;
            model.LastTested = DateTime.UtcNow;

            lock (_lockObject)
            {
                var existingModel = _models.FirstOrDefault(m => m.Id == model.Id);
                if (existingModel != null)
                {
                    // Update existing model
                    var index = _models.IndexOf(existingModel);
                    _models[index] = model;
                }
                else
                {
                    _models.Add(model);
                }
            }

            await SaveModelsConfigurationAsync();
            return true;
        }

        public async Task<bool> RemoveModelAsync(string modelId)
        {
            lock (_lockObject)
            {
                var model = _models.FirstOrDefault(m => m.Id == modelId);
                if (model == null)
                    return false;

                _models.Remove(model);

                // If this was the active model, clear it
                if (_activeModel?.Id == modelId)
                {
                    _activeModel = null;
                }
            }

            await SaveModelsConfigurationAsync();
            return true;
        }

        public async Task<bool> TestModelConnectionAsync(string modelId)
        {
            ModelInfo model;
            lock (_lockObject)
            {
                model = _models.FirstOrDefault(m => m.Id == modelId);
            }

            if (model == null)
                return false;

            return await TestModelConnectionAsync(modelId, model);
        }

        private async Task<bool> TestModelConnectionAsync(string modelId, ModelInfo model)
        {
            try
            {
                if (model.Type == ModelType.Local)
                {
                    return await TestLocalModelAsync(model);
                }
                else
                {
                    return await TestRemoteModelAsync(model);
                }
            }
            catch (Exception ex)
            {
                // Log error
                return false;
            }
        }

        private async Task<bool> TestLocalModelAsync(ModelInfo model)
        {
            try
            {
                var testRequest = new
                {
                    model = model.Name,
                    prompt = "Hello",
                    max_tokens = 5,
                    temperature = 0.1
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(testRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{model.Endpoint}/v1/completions", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                // Try Ollama format
                try
                {
                    var ollamaRequest = new
                    {
                        model = model.Name,
                        prompt = "Hello",
                        stream = false
                    };

                    var content = new StringContent(
                        JsonSerializer.Serialize(ollamaRequest),
                        Encoding.UTF8,
                        "application/json"
                    );

                    var response = await _httpClient.PostAsync($"{model.Endpoint}/api/generate", content);
                    return response.IsSuccessStatusCode;
                }
                catch
                {
                    return false;
                }
            }
        }

        private async Task<bool> TestRemoteModelAsync(ModelInfo model)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{model.Endpoint}/v1/chat/completions");
                
                if (!string.IsNullOrEmpty(model.ApiKey))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", model.ApiKey);
                }

                var testRequest = new
                {
                    model = model.Name,
                    messages = new[]
                    {
                        new { role = "user", content = "Hello" }
                    },
                    max_tokens = 5,
                    temperature = 0.1
                };

                request.Content = new StringContent(
                    JsonSerializer.Serialize(testRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<ModelResponse> SendRequestAsync(ModelRequest request)
        {
            var activeModel = await GetActiveModelAsync();
            if (activeModel == null || !activeModel.IsAvailable)
            {
                return new ModelResponse
                {
                    Success = false,
                    Error = "No active model available"
                };
            }

            var startTime = DateTime.UtcNow;

            try
            {
                if (activeModel.Type == ModelType.Local)
                {
                    return await SendLocalModelRequestAsync(activeModel, request, startTime);
                }
                else
                {
                    return await SendRemoteModelRequestAsync(activeModel, request, startTime);
                }
            }
            catch (Exception ex)
            {
                return new ModelResponse
                {
                    Success = false,
                    Error = ex.Message,
                    ProcessingTime = DateTime.UtcNow - startTime
                };
            }
        }

        private async Task<ModelResponse> SendLocalModelRequestAsync(ModelInfo model, ModelRequest request, DateTime startTime)
        {
            try
            {
                // Try OpenAI-compatible format first
                var openAIRequest = new
                {
                    model = model.Name,
                    messages = new[]
                    {
                        new { role = "system", content = request.SystemMessage ?? "You are a helpful coding assistant." },
                        new { role = "user", content = request.Prompt }
                    },
                    max_tokens = request.MaxTokens > 0 ? request.MaxTokens : model.MaxTokens,
                    temperature = request.Temperature > 0 ? request.Temperature : model.Temperature,
                    stream = false
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(openAIRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{model.Endpoint}/v1/chat/completions", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var choices = result.GetProperty("choices");
                    if (choices.GetArrayLength() > 0)
                    {
                        var message = choices[0].GetProperty("message").GetProperty("content").GetString();
                        var usage = result.TryGetProperty("usage", out var usageElement) ? 
                            usageElement.TryGetProperty("total_tokens", out var tokensElement) ? 
                                tokensElement.GetInt32() : 0 : 0;

                        return new ModelResponse
                        {
                            Success = true,
                            Content = message,
                            ProcessingTime = DateTime.UtcNow - startTime,
                            TokensUsed = usage
                        };
                    }
                }
                
                // Fallback to Ollama format
                return await SendOllamaRequestAsync(model, request, startTime);
            }
            catch
            {
                // Fallback to Ollama format
                return await SendOllamaRequestAsync(model, request, startTime);
            }
        }

        private async Task<ModelResponse> SendOllamaRequestAsync(ModelInfo model, ModelRequest request, DateTime startTime)
        {
            var ollamaRequest = new
            {
                model = model.Name,
                prompt = $"{request.SystemMessage}\n\nUser: {request.Prompt}\nAssistant:",
                stream = false,
                options = new
                {
                    num_predict = request.MaxTokens > 0 ? request.MaxTokens : model.MaxTokens,
                    temperature = request.Temperature > 0 ? request.Temperature : model.Temperature
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(ollamaRequest),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync($"{model.Endpoint}/api/generate", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                var responseText = result.GetProperty("response").GetString();
                
                return new ModelResponse
                {
                    Success = true,
                    Content = responseText,
                    ProcessingTime = DateTime.UtcNow - startTime,
                    TokensUsed = 0 // Ollama doesn't always provide token count
                };
            }

            throw new Exception($"Request failed with status: {response.StatusCode}");
        }

        private async Task<ModelResponse> SendRemoteModelRequestAsync(ModelInfo model, ModelRequest request, DateTime startTime)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{model.Endpoint}/v1/chat/completions");
            
            if (!string.IsNullOrEmpty(model.ApiKey))
            {
                httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", model.ApiKey);
            }

            var apiRequest = new
            {
                model = model.Name,
                messages = new[]
                {
                    new { role = "system", content = request.SystemMessage ?? "You are a helpful coding assistant." },
                    new { role = "user", content = request.Prompt }
                },
                max_tokens = request.MaxTokens > 0 ? request.MaxTokens : model.MaxTokens,
                temperature = request.Temperature > 0 ? request.Temperature : model.Temperature
            };

            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(apiRequest),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(httpRequest);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                var choices = result.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var message = choices[0].GetProperty("message").GetProperty("content").GetString();
                    var usage = result.TryGetProperty("usage", out var usageElement) ? 
                        usageElement.TryGetProperty("total_tokens", out var tokensElement) ? 
                            tokensElement.GetInt32() : 0 : 0;

                    return new ModelResponse
                    {
                        Success = true,
                        Content = message,
                        ProcessingTime = DateTime.UtcNow - startTime,
                        TokensUsed = usage
                    };
                }
            }

            throw new Exception($"Request failed with status: {response.StatusCode}");
        }

        private void InitializeDefaultModels()
        {
            // Add some common local model configurations
            _models.Add(new ModelInfo
            {
                Id = "ollama-llama2",
                Name = "llama2",
                Provider = "Ollama",
                Type = ModelType.Local,
                Endpoint = "http://localhost:11434",
                MaxTokens = 2048,
                Temperature = 0.7,
                IsAvailable = false
            });

            _models.Add(new ModelInfo
            {
                Id = "ollama-codellama",
                Name = "codellama",
                Provider = "Ollama",
                Type = ModelType.Local,
                Endpoint = "http://localhost:11434",
                MaxTokens = 2048,
                Temperature = 0.3,
                IsAvailable = false
            });

            _models.Add(new ModelInfo
            {
                Id = "lmstudio-local",
                Name = "local-model",
                Provider = "LM Studio",
                Type = ModelType.Local,
                Endpoint = "http://localhost:1234",
                MaxTokens = 2048,
                Temperature = 0.7,
                IsAvailable = false
            });

            // Test availability of local models
            Task.Run(async () =>
            {
                foreach (var model in _models.ToList())
                {
                    if (model.Type == ModelType.Local)
                    {
                        model.IsAvailable = await TestModelConnectionAsync(model.Id, model);
                        model.LastTested = DateTime.UtcNow;
                    }
                }
            });
        }

        private async Task SaveModelsConfigurationAsync()
        {
            List<ModelInfo> modelsToSave;
            lock (_lockObject)
            {
                modelsToSave = _models.ToList();
            }

            await _configService.SetSettingAsync("models.configurations", modelsToSave);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}