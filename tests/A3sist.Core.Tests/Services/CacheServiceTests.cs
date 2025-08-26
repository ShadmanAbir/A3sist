using A3sist.Core.Configuration;
using A3sist.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Core.Tests.Services
{
    /// <summary>
    /// Unit tests for CacheService
    /// </summary>
    public class CacheServiceTests : IDisposable
    {
        private readonly Mock<ILogger<CacheService>> _mockLogger;
        private readonly Mock<IOptions<A3sistOptions>> _mockOptions;
        private readonly IMemoryCache _memoryCache;
        private readonly CacheService _cacheService;
        private readonly A3sistOptions _options;

        public CacheServiceTests()
        {
            _mockLogger = new Mock<ILogger<CacheService>>();
            _mockOptions = new Mock<IOptions<A3sistOptions>>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            
            _options = new A3sistOptions
            {
                LLM = new LLMOptions
                {
                    EnableCaching = true,
                    CacheExpiration = TimeSpan.FromMinutes(30)
                },
                Performance = new PerformanceOptions
                {
                    MaxMemoryUsageMB = 512
                }
            };
            
            _mockOptions.Setup(x => x.Value).Returns(_options);
            _cacheService = new CacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
        }

        [Fact]
        public async Task GetAsync_WithValidKey_ShouldReturnCachedValue()
        {
            // Arrange
            const string key = "test-key";
            const string value = "test-value";
            await _cacheService.SetAsync(key, value);

            // Act
            var result = await _cacheService.GetAsync<string>(key);

            // Assert
            result.Should().Be(value);
        }

        [Fact]
        public async Task GetAsync_WithNonExistentKey_ShouldReturnNull()
        {
            // Arrange
            const string key = "non-existent-key";

            // Act
            var result = await _cacheService.GetAsync<string>(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task SetAsync_WithValidKeyValue_ShouldCacheValue()
        {
            // Arrange
            const string key = "test-key";
            const string value = "test-value";

            // Act
            await _cacheService.SetAsync(key, value);
            var result = await _cacheService.GetAsync<string>(key);

            // Assert
            result.Should().Be(value);
        }

        [Fact]
        public async Task SetAsync_WhenCachingDisabled_ShouldNotCacheValue()
        {
            // Arrange
            _options.LLM.EnableCaching = false;
            const string key = "test-key";
            const string value = "test-value";

            // Act
            await _cacheService.SetAsync(key, value);
            var result = await _cacheService.GetAsync<string>(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task RemoveAsync_WithExistingKey_ShouldRemoveValue()
        {
            // Arrange
            const string key = "test-key";
            const string value = "test-value";
            await _cacheService.SetAsync(key, value);

            // Act
            await _cacheService.RemoveAsync(key);
            var result = await _cacheService.GetAsync<string>(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GenerateKey_WithMultipleParts_ShouldReturnConsistentKey()
        {
            // Arrange
            var parts = new object[] { "part1", 123, "part3" };

            // Act
            var key1 = _cacheService.GenerateKey(parts);
            var key2 = _cacheService.GenerateKey(parts);

            // Assert
            key1.Should().Be(key2);
            key1.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void GenerateKey_WithDifferentParts_ShouldReturnDifferentKeys()
        {
            // Arrange
            var parts1 = new object[] { "part1", 123 };
            var parts2 = new object[] { "part1", 456 };

            // Act
            var key1 = _cacheService.GenerateKey(parts1);
            var key2 = _cacheService.GenerateKey(parts2);

            // Assert
            key1.Should().NotBe(key2);
        }

        [Fact]
        public void GenerateKey_WithNullOrEmptyParts_ShouldThrowException()
        {
            // Act & Assert
            _cacheService.Invoking(x => x.GenerateKey())
                .Should().Throw<ArgumentException>();
            
            _cacheService.Invoking(x => x.GenerateKey(null))
                .Should().Throw<ArgumentException>();
        }

        [Fact]
        public async Task ClearAsync_ShouldRemoveAllCachedItems()
        {
            // Arrange
            await _cacheService.SetAsync("key1", "value1");
            await _cacheService.SetAsync("key2", "value2");

            // Act
            await _cacheService.ClearAsync();

            // Assert
            var result1 = await _cacheService.GetAsync<string>("key1");
            var result2 = await _cacheService.GetAsync<string>("key2");
            
            result1.Should().BeNull();
            result2.Should().BeNull();
        }

        public void Dispose()
        {
            _cacheService?.Dispose();
            _memoryCache?.Dispose();
        }
    }
}