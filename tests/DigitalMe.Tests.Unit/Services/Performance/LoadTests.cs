using Xunit;
using FluentAssertions;
using DigitalMe.Services;
using DigitalMe.Services.Performance;
using DigitalMe.Repositories;
using DigitalMe.Services.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using Moq;
using Microsoft.Extensions.Caching.Memory;

namespace DigitalMe.Tests.Unit.Services.Performance;

/// <summary>
/// Load and performance tests for Dynamic API Configuration System (Phase 7, Task 7.1.4).
/// Target: 100 concurrent GetApiKey requests < 5s, cache provides 10x+ speedup.
/// </summary>
public class LoadTests : IDisposable
{
    private readonly IApiConfigurationService _service;
    private readonly IApiConfigurationService _cachedService;
    private readonly Mock<IApiConfigurationRepository> _mockRepo;
    private readonly ICachingService _cachingService;
    private readonly MemoryCache _memoryCache;

    public LoadTests()
    {
        // Mock repository
        _mockRepo = new Mock<IApiConfigurationRepository>();
        _mockRepo.Setup(r => r.GetByUserAndProviderAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((DigitalMe.Data.Entities.ApiConfiguration?)null); // Simulate no user key -> system fallback

        // Mock configuration with system keys
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c[$"ApiKeys:Anthropic"]).Returns("sk-ant-system-key");
        mockConfig.Setup(c => c[$"ApiKeys:OpenAI"]).Returns("sk-openai-system-key");

        // Encryption service
        var encryptionService = new KeyEncryptionService(NullLogger<KeyEncryptionService>.Instance);

        // Baseline service (no caching)
        _service = new ApiConfigurationService(
            _mockRepo.Object,
            encryptionService,
            mockConfig.Object,
            NullLogger<ApiConfigurationService>.Instance);

        // Caching infrastructure
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _cachingService = new CachingService(_memoryCache, NullLogger<CachingService>.Instance);

        // Cached service (with decorator)
        _cachedService = new CachedApiConfigurationService(
            _service,
            _cachingService,
            NullLogger<CachedApiConfigurationService>.Instance);
    }

    /// <summary>
    /// Test: 100 concurrent GetApiKey requests should complete in < 5 seconds.
    /// </summary>
    [Fact]
    public async Task Should_Handle_100_Concurrent_Requests_Under_5_Seconds()
    {
        // Arrange
        const int concurrentRequests = 100;
        var tasks = new List<Task>();

        // Act
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < concurrentRequests; i++)
        {
            var userId = $"user{i % 10}"; // 10 unique users (cache hits after first request per user)
            tasks.Add(Task.Run(async () =>
            {
                await _cachedService.GetApiKeyAsync("Anthropic", userId);
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000,
            $"100 concurrent requests should complete in < 5s (actual: {stopwatch.ElapsedMilliseconds}ms)");

        Console.WriteLine($"✅ 100 concurrent requests completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Test: Cache should improve performance by at least 10x.
    /// </summary>
    [Fact]
    public async Task Cache_Should_Improve_Performance_10x()
    {
        // Arrange
        const string provider = "Anthropic";
        const string userId = "cache-test-user";
        const int iterations = 50;

        // Warm up
        await _service.GetApiKeyAsync(provider, userId);
        await _cachedService.GetApiKeyAsync(provider, userId);

        // Act: Measure WITHOUT cache
        var noCacheTime = await MeasureTime(async () =>
        {
            await _service.GetApiKeyAsync(provider, userId);
        }, iterations);

        // Act: Measure WITH cache
        var withCacheTime = await MeasureTime(async () =>
        {
            await _cachedService.GetApiKeyAsync(provider, userId);
        }, iterations);

        // Assert
        var speedup = (double)noCacheTime / withCacheTime;
        speedup.Should().BeGreaterThan(10,
            $"Cache should provide 10x+ speedup (actual: {speedup:F1}x, no-cache: {noCacheTime}ms, cached: {withCacheTime}ms)");

        Console.WriteLine($"✅ Cache speedup: {speedup:F1}x (no-cache: {noCacheTime}ms, cached: {withCacheTime}ms)");
    }

    /// <summary>
    /// Test: Concurrent access to cached service should be thread-safe.
    /// </summary>
    [Fact]
    public async Task Concurrent_Cache_Access_Should_Be_ThreadSafe()
    {
        // Arrange
        const int parallelTasks = 50;
        const string provider = "Anthropic";
        const string userId = "thread-safety-test";

        // Act: Multiple threads requesting same key simultaneously
        var tasks = Enumerable.Range(0, parallelTasks).Select(_ =>
            Task.Run(async () => await _cachedService.GetApiKeyAsync(provider, userId)));

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllBe("sk-ant-system-key", "all threads should get same result");
        results.Should().HaveCount(parallelTasks, "all tasks should complete");

        // Verify repository was called minimal times (ideally 1, max few due to race)
        _mockRepo.Verify(
            r => r.GetByUserAndProviderAsync(userId, provider),
            Times.AtMost(5),
            "Repository should not be called for every request due to caching");

        Console.WriteLine($"✅ {parallelTasks} parallel requests handled thread-safely");
    }

    /// <summary>
    /// Test: Cache invalidation should work correctly - verifies that cached values
    /// are properly invalidated when RemoveCachedResponseAsync is called.
    /// </summary>
    [Fact]
    public async Task Cache_Should_Invalidate_After_Key_Update()
    {
        // Arrange
        const string cacheKey = "test-invalidation-key";
        const string initialValue = "initial-value";
        const string newValue = "new-value-after-invalidation";

        // Factory function that returns different values
        var callCount = 0;
        var factory = new Func<Task<string>>(() =>
        {
            callCount++;
            return Task.FromResult(callCount == 1 ? initialValue : newValue);
        });

        // Act: First call (cache miss, stores initialValue)
        var value1 = await _cachingService.GetOrSetAsync(cacheKey, factory, TimeSpan.FromMinutes(5));

        // Act: Second call (cache hit, returns initialValue)
        var value2 = await _cachingService.GetOrSetAsync(cacheKey, factory, TimeSpan.FromMinutes(5));

        // Act: Invalidate cache
        await _cachingService.RemoveCachedResponseAsync(cacheKey);

        // Act: Third call (cache miss after invalidation, returns newValue)
        var value3 = await _cachingService.GetOrSetAsync(cacheKey, factory, TimeSpan.FromMinutes(5));

        // Assert
        value1.Should().Be(initialValue, "first call should return initial value");
        value2.Should().Be(initialValue, "second call should hit cache with initial value");
        value3.Should().Be(newValue, "third call should return new value after cache invalidation");
        callCount.Should().Be(2, "factory should be called only twice (cache hit on second call)");

        Console.WriteLine("✅ Cache invalidation working correctly");
    }

    // ==================== HELPER METHODS ====================

    private static async Task<long> MeasureTime(Func<Task> action, int iterations)
    {
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            await action();
        }

        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    private static DigitalMe.Data.Entities.ApiConfiguration CreateMockConfiguration(
        string userId, string provider, string apiKey)
    {
        return new DigitalMe.Data.Entities.ApiConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            EncryptedApiKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(apiKey)),
            EncryptionIV = Convert.ToBase64String(new byte[12]),
            EncryptionSalt = Convert.ToBase64String(new byte[32]),
            KeyFingerprint = "****1234",
            IsActive = true
        };
    }

    public void Dispose()
    {
        _memoryCache?.Dispose();
    }
}