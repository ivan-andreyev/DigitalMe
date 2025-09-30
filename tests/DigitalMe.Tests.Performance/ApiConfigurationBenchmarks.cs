using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using DigitalMe.Data;
using DigitalMe.Extensions;
using DigitalMe.Services;
using DigitalMe.Services.Security;
using DigitalMe.Services.Usage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Tests.Performance;

/// <summary>
/// Performance benchmarks for Dynamic API Configuration System (Phase 7, Task 7.1).
/// Target: P95 latency < 50ms for GetApiKey.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, warmupCount: 3, iterationCount: 5)]
public class ApiConfigurationBenchmarks
{
    private IApiConfigurationService _configService = null!;
    private IKeyEncryptionService _encryptionService = null!;
    private IApiUsageTracker _usageTracker = null!;
    private IServiceProvider _serviceProvider = null!;
    private const string TestUserId = "benchmark-user";
    private const string TestProvider = "Anthropic";
    private const string TestApiKey = "sk-ant-benchmark-key-1234567890";

    [GlobalSetup]
    public async Task Setup()
    {
        var services = new ServiceCollection();

        // Configuration
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ApiKeys:Anthropic", "sk-ant-system-fallback-key" },
            { "ApiKeys:OpenAI", "sk-openai-system-fallback-key" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // In-Memory Database for benchmarks
        services.AddDbContext<DigitalMeDbContext>(options =>
            options.UseInMemoryDatabase("BenchmarkDb"));

        // Logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Memory Cache
        services.AddMemoryCache();

        // Clean Architecture Services
        services.AddCleanArchitectureServices();

        _serviceProvider = services.BuildServiceProvider();
        _configService = _serviceProvider.GetRequiredService<IApiConfigurationService>();
        _encryptionService = _serviceProvider.GetRequiredService<IKeyEncryptionService>();
        _usageTracker = _serviceProvider.GetRequiredService<IApiUsageTracker>();

        // Seed test data
        await _configService.SetUserApiKeyAsync(TestProvider, TestUserId, TestApiKey);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Benchmark: GetApiKey with cache hit (TARGET: < 10ms).
    /// </summary>
    [Benchmark]
    public async Task GetApiKey_Cached()
    {
        // First call warms cache, subsequent calls hit cache
        await _configService.GetApiKeyAsync(TestProvider, TestUserId);
    }

    /// <summary>
    /// Benchmark: Encrypt + Decrypt roundtrip (TARGET: < 20ms).
    /// </summary>
    [Benchmark]
    public async Task EncryptDecrypt_Roundtrip()
    {
        var encrypted = await _encryptionService.EncryptApiKeyAsync(TestApiKey, TestUserId);
        await _encryptionService.DecryptApiKeyAsync(encrypted, TestUserId);
    }

    /// <summary>
    /// Benchmark: Record single usage (TARGET: < 15ms).
    /// </summary>
    [Benchmark]
    public async Task RecordUsage_SingleRecord()
    {
        await _usageTracker.RecordUsageAsync(
            TestUserId,
            TestProvider,
            new DigitalMe.Models.Usage.UsageDetails
            {
                RequestType = "benchmark",
                TokensUsed = 100,
                ResponseTime = 500,
                Success = true
            });
    }

    /// <summary>
    /// Benchmark: Get usage stats for 30 days (TARGET: < 100ms).
    /// </summary>
    [Benchmark]
    public async Task GetUsageStats_30Days()
    {
        await _usageTracker.GetUsageStatsAsync(
            TestUserId,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow);
    }
}