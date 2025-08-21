using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace A3sist.Orchastrator.LLM
{
    public class CachedLLMClient : ILLMClient
    {
        private readonly ILLMClient _llmClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachedLLMClient> _logger;

        public CachedLLMClient(ILLMClient llmClient, IMemoryCache cache, ILogger<CachedLLMClient> logger)
        {
            _llmClient = llmClient;
            _cache = cache;
            _logger = logger;
        }

        public Task<bool> GetCompletionAsync(object prompt, object lLMOptions)
        {
            throw new NotImplementedException();
        }

        public async Task<string> GetResponseAsync(string prompt)
        {
            if (_cache.TryGetValue(prompt, out string cachedResponse))
            {
                _logger.LogInformation($"Cache hit for prompt: {prompt}");
                return cachedResponse;
            }

            try
            {
                var response = await _llmClient.GetResponseAsync(prompt);
                _cache.Set(prompt, response, TimeSpan.FromMinutes(10));
                _logger.LogInformation($"Cached response for prompt: {prompt}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while getting response from LLM for prompt: {prompt}");
                throw;
            }
        }
    }
}