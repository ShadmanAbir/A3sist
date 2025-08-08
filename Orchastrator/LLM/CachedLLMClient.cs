using System;
using System.Threading.Tasks;

public class CachedLLMClient : ILLMClient
{
    private readonly ILLMClient _innerClient;
    private readonly LLMCacheService _cacheService;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public CachedLLMClient(ILLMClient innerClient, LLMCacheService cacheService)
    {
        _innerClient = innerClient;
        _cacheService = cacheService;
    }

    public async Task&lt;LLMResponse&gt; GetCompletionAsync(string prompt, LLMOptions options = null)
    {
        var cacheKey = $"completion:{prompt}:{options?.MaxTokens}:{options?.Temperature}";

        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            () => _innerClient.GetCompletionAsync(prompt, options),
            _cacheDuration);
    }

    public async Task&lt;LLMResponse&gt; GetChatCompletionAsync(string[] messages, LLMOptions options = null)
    {
        var cacheKey = $"chat:{string.Join("|", messages)}:{options?.MaxTokens}:{options?.Temperature}";

        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            () => _innerClient.GetChatCompletionAsync(messages, options),
            _cacheDuration);
    }
}