using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

public class LLMCacheService : IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary&lt;string, SemaphoreSlim&gt; _locks = new();

    public LLMCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task&lt;T&gt; GetOrCreateAsync&lt;T&gt;(string key, Func&lt;Task&lt;T&gt;&gt; createItem, TimeSpan absoluteExpiration)
    {
        if (_cache.TryGetValue(key, out T cacheEntry))
        {
            return cacheEntry;
        }

        var mylock = _locks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));

        await mylock.WaitAsync();
        try
        {
            if (_cache.TryGetValue(key, out cacheEntry))
            {
                return cacheEntry;
            }

            cacheEntry = await createItem();
            _cache.Set(key, cacheEntry, absoluteExpiration);
            return cacheEntry;
        }
        finally
        {
            mylock.Release();
            _locks.TryRemove(key, out _);
        }
    }

    public void Dispose()
    {
        foreach (var semaphore in _locks.Values)
        {
            semaphore.Dispose();
        }
        _locks.Clear();
    }
}