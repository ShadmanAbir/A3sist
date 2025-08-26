using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using A3sist.Core.Configuration;

namespace A3sist.Core.Services
{
    /// <summary>
    /// Interface for caching service operations
    /// </summary>
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task ClearAsync(CancellationToken cancellationToken = default);
        string GenerateKey(params object[] keyParts);
    }

    /// <summary>
    /// High-performance caching service with configurable expiration and memory management
    /// </summary>
    public class CacheService : ICacheService, IDisposable
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheService> _logger;
        private readonly A3sistOptions _options;
        private readonly Timer _cleanupTimer;
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        public CacheService(
            IMemoryCache memoryCache,
            ILogger<CacheService> logger,
            IOptions<A3sistOptions> options)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _semaphore = new SemaphoreSlim(1, 1);

            // Start cleanup timer to manage memory usage
            _cleanupTimer = new Timer(PerformCleanup, null, 
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            _logger.LogInformation("CacheService initialized with caching {Status}", 
                _options.LLM.EnableCaching ? "enabled" : "disabled");
        }

        /// <summary>
        /// Retrieves a cached value by key
        /// </summary>
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            if (!_options.LLM.EnableCaching)
                return null;

            await Task.CompletedTask; // For async consistency
            
            try
            {
                if (_memoryCache.TryGetValue(key, out var cachedValue))
                {
                    _logger.LogTrace("Cache hit for key: {Key}", key);
                    return cachedValue as T;
                }

                _logger.LogTrace("Cache miss for key: {Key}", key);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving from cache for key: {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// Stores a value in the cache with optional expiration
        /// </summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(key) || value == null)
                return;

            if (!_options.LLM.EnableCaching)
                return;

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var cacheExpiration = expiration ?? _options.LLM.CacheExpiration;
                var options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = cacheExpiration,
                    SlidingExpiration = TimeSpan.FromMinutes(15),
                    Priority = CacheItemPriority.Normal
                };

                // Add callback for cache eviction logging
                options.RegisterPostEvictionCallback((key, value, reason, state) =>
                {
                    _logger.LogTrace("Cache entry evicted: {Key}, Reason: {Reason}", key, reason);
                });

                _memoryCache.Set(key, value, options);
                _logger.LogTrace("Cached value for key: {Key}, Expiration: {Expiration}", key, cacheExpiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing to cache for key: {Key}", key);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Removes a specific cache entry
        /// </summary>
        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            await Task.CompletedTask; // For async consistency

            try
            {
                _memoryCache.Remove(key);
                _logger.LogTrace("Removed cache entry for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from cache for key: {Key}", key);
            }
        }

        /// <summary>
        /// Clears all cache entries
        /// </summary>
        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_memoryCache is MemoryCache mc)
                {
                    mc.Compact(1.0); // Compact 100% of cache
                }

                _logger.LogInformation("Cache cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Generates a consistent cache key from multiple parts
        /// </summary>
        public string GenerateKey(params object[] keyParts)
        {
            if (keyParts == null || keyParts.Length == 0)
                throw new ArgumentException("Key parts cannot be null or empty", nameof(keyParts));

            try
            {
                var combined = string.Join("|", keyParts);
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                return Convert.ToBase64String(hash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating cache key");
                return string.Join("|", keyParts); // Fallback to simple concatenation
            }
        }

        /// <summary>
        /// Performs periodic cache cleanup to manage memory usage
        /// </summary>
        private async void PerformCleanup(object? state)
        {
            if (_disposed)
                return;

            try
            {
                await _semaphore.WaitAsync(TimeSpan.FromSeconds(30));
                try
                {
                    var currentMemory = GC.GetTotalMemory(false) / 1024 / 1024; // MB
                    if (currentMemory > _options.Performance.MaxMemoryUsageMB)
                    {
                        _logger.LogWarning("Memory usage ({MemoryMB}MB) exceeds threshold ({ThresholdMB}MB), performing cache cleanup", 
                            currentMemory, _options.Performance.MaxMemoryUsageMB);

                        if (_memoryCache is MemoryCache mc)
                        {
                            mc.Compact(0.25); // Compact 25% of cache
                        }

                        if (_options.Performance.EnableAutoGC)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            GC.Collect();
                        }
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache cleanup");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _cleanupTimer?.Dispose();
            _semaphore?.Dispose();
            _logger.LogInformation("CacheService disposed");
        }
    }
}