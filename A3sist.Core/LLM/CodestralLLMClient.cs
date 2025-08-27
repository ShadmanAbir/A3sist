<<<<<<< HEAD
using A3sist.Core.LLM;
=======
>>>>>>> d9292da76b3bf2140ff68335ee93fce5bcd201a3
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using System;
using System.Collections.Generic;
<<<<<<< HEAD
using System.Linq;
=======
>>>>>>> d9292da76b3bf2140ff68335ee93fce5bcd201a3
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Core.LLM
{
    public class CodestralLLMClient : ILLMClient
    {
        private readonly HttpClient _httpClient;

        public CodestralLLMClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string CurrentModel => "codestral";
        public bool IsAvailable => true;

        public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
        {
            return await GetCompletionAsync(prompt, new LLMOptions());
        }

        public async Task<string> CompleteAsync(string prompt, Dictionary<string, object>? options = null, CancellationToken cancellationToken = default)
        {
            var llmOptions = new LLMOptions();
            if (options != null)
            {
                if (options.TryGetValue("MaxTokens", out var maxTokens) && maxTokens is int mt)
                    llmOptions.MaxTokens = mt;
                if (options.TryGetValue("Temperature", out var temperature) && temperature is double temp)
                    llmOptions.Temperature = temp;
            }
            return await GetCompletionAsync(prompt, llmOptions);
        }

        public async Task CompleteStreamAsync(string prompt, Action<string> onChunk, CancellationToken cancellationToken = default)
        {
            var response = await GetCompletionAsync(prompt, new LLMOptions());
            onChunk(response);
        }

        public async Task<IEnumerable<string>> GetAvailableModelsAsync()
        {
            return new[] { "codestral" };
        }

        public async Task InitializeAsync()
        {
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _httpClient?.Dispose();
            await Task.CompletedTask;
        }

        public async Task<string> GetCompletionAsync(string ragPrompt, LLMOptions options)
        {
            options ??= new LLMOptions();

            var requestBody = new
            {
                prompt = ragPrompt,
                max_tokens = options.MaxTokens,
                temperature = options.Temperature,
                stop = options.Stop
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("http://localhost:11434/api/generate", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

            return responseObject.GetProperty("completion").GetString() ?? string.Empty;
        }
    }
}