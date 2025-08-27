using A3sist.Core.LLM;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public string CurrentModel => "codestral";

        public bool IsAvailable { get; private set; } = true;

        public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
        {
            return await GetCompletionAsync(prompt, new LLMOptions());
        }

        public async Task<string> CompleteAsync(string prompt, Dictionary<string, object>? options = null, CancellationToken cancellationToken = default)
        {
            var llmOptions = new LLMOptions();
            if (options != null)
            {
                if (options.TryGetValue("max_tokens", out var maxTokens)) llmOptions.MaxTokens = Convert.ToInt32(maxTokens);
                if (options.TryGetValue("temperature", out var temperature)) llmOptions.Temperature = Convert.ToDouble(temperature);
                if (options.TryGetValue("stop", out var stop)) llmOptions.Stop = stop as string[];
            }
            return await GetCompletionAsync(prompt, llmOptions);
        }

        public async Task CompleteStreamAsync(string prompt, Action<string> onChunk, CancellationToken cancellationToken = default)
        {
            var result = await GetCompletionAsync(prompt, new LLMOptions());
            onChunk(result);
        }

        public async Task<IEnumerable<string>> GetAvailableModelsAsync()
        {
            return new[] { "codestral" };
        }

        public async Task InitializeAsync()
        {
            IsAvailable = true;
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _httpClient?.Dispose();
            await Task.CompletedTask;
        }

        public async Task<string> GetCompletionAsync(string prompt, LLMOptions? options = null)
        {
            options ??= new LLMOptions();

            var requestBody = new
            {
                prompt,
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

        public async Task<string> GetCompletionAsync(string ragPrompt, LLMOptions options)
        {
            return await GetCompletionAsync(ragPrompt, options);
        }

        public async Task<string> GetResponseAsync(string prompt)
        {
            try
            {
                // Use the existing GetCompletionAsync method
                return await GetCompletionAsync(prompt, new LLMOptions());
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}