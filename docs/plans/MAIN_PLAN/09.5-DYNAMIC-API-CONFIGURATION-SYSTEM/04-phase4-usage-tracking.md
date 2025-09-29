# 📋 PHASE 4: USAGE TRACKING (TDD)

**Parent Plan**: [09.5-DYNAMIC-API-CONFIGURATION-SYSTEM.md](../09.5-DYNAMIC-API-CONFIGURATION-SYSTEM.md)

**Phase Status**: PENDING
**Priority**: HIGH
**Estimated Duration**: 2 days
**Dependencies**: Phase 3 Complete

---

## Phase Objectives

Implement comprehensive usage tracking for API calls with cost estimation, quota management, and analytics. Enable monitoring of API usage patterns and costs per user.

---

## Task 4.1: Implement ApiUsageTracker with Metric Tests

**Status**: PENDING
**Priority**: HIGH
**Estimated**: 90 minutes
**Dependencies**: Phase 3 complete

### TDD Cycle

#### 1. RED: Create usage tracking tests
File: `tests/DigitalMe.Tests.Unit/Services/Usage/ApiUsageTrackerTests.cs`

```csharp
public class ApiUsageTrackerTests
{
    private readonly Mock<IApiUsageRepository> _mockRepo;
    private readonly Mock<ILogger<ApiUsageTracker>> _mockLogger;
    private readonly ApiUsageTracker _tracker;

    public ApiUsageTrackerTests()
    {
        _mockRepo = new Mock<IApiUsageRepository>();
        _mockLogger = new Mock<ILogger<ApiUsageTracker>>();
        _tracker = new ApiUsageTracker(_mockRepo.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task RecordUsage_Should_Track_Request_Details()
    {
        // Arrange & Act
        await _tracker.RecordUsageAsync("user123", "Anthropic",
            new UsageDetails
            {
                RequestType = "chat.completion",
                TokensUsed = 1500,
                ResponseTime = 1234,
                Success = true
            });

        // Assert
        _mockRepo.Verify(r => r.SaveUsageRecordAsync(
            It.Is<ApiUsageRecord>(u =>
                u.TokensUsed == 1500 &&
                u.Success == true)),
            Times.Once);
    }

    [Fact]
    public async Task GetUsageStats_Should_Aggregate_Correctly()
    {
        // Arrange
        var records = new List<ApiUsageRecord>
        {
            new() { TokensUsed = 1000, CostEstimate = 0.015m },
            new() { TokensUsed = 2000, CostEstimate = 0.030m },
            new() { TokensUsed = 1500, CostEstimate = 0.0225m }
        };

        _mockRepo.Setup(r => r.GetUsageRecordsAsync("user", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(records);

        // Act
        var stats = await _tracker.GetUsageStatsAsync("user", DateTime.Today, DateTime.Now);

        // Assert
        stats.TotalTokens.Should().Be(4500);
        stats.TotalCost.Should().Be(0.0675m);
        stats.RequestCount.Should().Be(3);
    }

    [Fact]
    public async Task Should_Calculate_Cost_Estimates_Accurately()
    {
        // Arrange
        var details = new UsageDetails
        {
            RequestType = "chat.completion",
            TokensUsed = 1000,
            Provider = "Anthropic"
        };

        // Act
        var cost = _tracker.CalculateCost("Anthropic", 1000);

        // Assert
        cost.Should().Be(0.015m); // $0.015 per 1K tokens
    }
}
```

#### 2. GREEN: Implement usage tracker

```csharp
public class ApiUsageTracker : IApiUsageTracker
{
    private readonly IApiUsageRepository _repository;
    private readonly ILogger<ApiUsageTracker> _logger;

    private readonly Dictionary<string, decimal> _costPerToken = new()
    {
        ["Anthropic"] = 0.000015m, // $0.015 per 1K tokens
        ["OpenAI"] = 0.000020m,     // $0.020 per 1K tokens
        ["Slack"] = 0.0m,           // Free API
        ["GitHub"] = 0.0m           // Included in plan
    };

    public async Task RecordUsageAsync(string userId, string provider, UsageDetails details)
    {
        try
        {
            var record = new ApiUsageRecord
            {
                UserId = userId,
                Provider = provider,
                RequestType = details.RequestType,
                TokensUsed = details.TokensUsed,
                CostEstimate = CalculateCost(provider, details.TokensUsed),
                ResponseTime = details.ResponseTime,
                Success = details.Success,
                ErrorType = details.ErrorType,
                RequestTimestamp = DateTime.UtcNow
            };

            await _repository.SaveUsageRecordAsync(record);

            // Check if quota needs updating
            await UpdateQuotaUsageAsync(userId, provider, details.TokensUsed);

            _logger.LogInformation(
                "Recorded usage for {UserId}/{Provider}: {Tokens} tokens, ${Cost}",
                userId.Substring(0, 8) + "***",
                provider,
                details.TokensUsed,
                record.CostEstimate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record usage for {UserId}/{Provider}", userId, provider);
            // Don't throw - usage tracking shouldn't break the main flow
        }
    }

    public decimal CalculateCost(string provider, int tokens)
    {
        if (_costPerToken.TryGetValue(provider, out var costPerToken))
        {
            return tokens * costPerToken;
        }
        return 0;
    }

    public async Task<UsageStats> GetUsageStatsAsync(
        string userId,
        DateTime startDate,
        DateTime endDate)
    {
        var records = await _repository.GetUsageRecordsAsync(userId, startDate, endDate);

        return new UsageStats
        {
            TotalTokens = records.Sum(r => r.TokensUsed),
            TotalCost = records.Sum(r => r.CostEstimate),
            RequestCount = records.Count,
            SuccessRate = records.Count > 0
                ? (decimal)records.Count(r => r.Success) / records.Count * 100
                : 0,
            AverageResponseTime = records.Any()
                ? records.Average(r => r.ResponseTime)
                : 0,
            ByProvider = records.GroupBy(r => r.Provider)
                .ToDictionary(
                    g => g.Key,
                    g => new ProviderStats
                    {
                        Tokens = g.Sum(r => r.TokensUsed),
                        Cost = g.Sum(r => r.CostEstimate),
                        Requests = g.Count()
                    })
        };
    }

    private async Task UpdateQuotaUsageAsync(string userId, string provider, int tokensUsed)
    {
        // This will be implemented with quota management
        var currentUsage = await _repository.GetDailyUsageAsync(userId, provider, DateTime.Today);

        if (currentUsage != null)
        {
            currentUsage.TokensUsed += tokensUsed;
            await _repository.UpdateDailyUsageAsync(currentUsage);
        }
        else
        {
            await _repository.CreateDailyUsageAsync(new DailyUsage
            {
                UserId = userId,
                Provider = provider,
                Date = DateTime.Today,
                TokensUsed = tokensUsed
            });
        }
    }
}
```

### Acceptance Criteria
- ✅ Usage recording accurate
- ✅ Cost calculation correct
- ✅ Aggregation working
- ✅ Performance < 5ms overhead

---

## Task 4.2: Implement Quota Management with Limit Tests

**Status**: PENDING
**Priority**: HIGH
**Estimated**: 60 minutes
**Dependencies**: Task 4.1

### Quota Management Tests

```csharp
public class QuotaManagerTests
{
    [Fact]
    public async Task CheckQuota_Should_Enforce_Daily_Limits()
    {
        // Arrange
        var manager = new QuotaManager(_mockRepo.Object);
        var quota = new UserQuota
        {
            DailyTokenLimit = 10000,
            CurrentDailyUsage = 9500
        };

        // Act
        var canUse = await manager.CanUseTokensAsync("user", "Anthropic", 600);

        // Assert
        canUse.Should().BeFalse(); // Would exceed daily limit
    }

    [Fact]
    public async Task CheckQuota_Should_Reset_At_Midnight()
    {
        // Arrange
        var yesterday = DateTime.Today.AddDays(-1);
        var oldUsage = new DailyUsage
        {
            Date = yesterday,
            TokensUsed = 10000
        };

        // Act
        var todayUsage = await manager.GetOrCreateDailyUsageAsync("user", "Anthropic");

        // Assert
        todayUsage.Date.Should().Be(DateTime.Today);
        todayUsage.TokensUsed.Should().Be(0);
    }

    [Fact]
    public async Task Should_Send_Warning_At_80_Percent_Usage()
    {
        // Arrange
        var mockNotifier = new Mock<INotificationService>();
        var manager = new QuotaManager(_mockRepo.Object, mockNotifier.Object);

        // Act
        await manager.UpdateUsageAsync("user", "Anthropic", 8500); // 85% of 10000

        // Assert
        mockNotifier.Verify(n => n.SendQuotaWarningAsync(
            "user",
            It.Is<decimal>(p => p >= 80 && p < 90)),
            Times.Once);
    }
}
```

### Quota Manager Implementation

```csharp
public class QuotaManager : IQuotaManager
{
    private readonly IApiUsageRepository _repository;
    private readonly INotificationService _notifier;
    private readonly Dictionary<string, int> _defaultQuotas = new()
    {
        ["Free"] = 10000,      // 10K tokens/day
        ["Basic"] = 100000,    // 100K tokens/day
        ["Premium"] = 1000000  // 1M tokens/day
    };

    public async Task<bool> CanUseTokensAsync(string userId, string provider, int tokens)
    {
        var quota = await GetUserQuotaAsync(userId, provider);
        var usage = await GetCurrentUsageAsync(userId, provider);

        return usage.TokensUsed + tokens <= quota.DailyTokenLimit;
    }

    public async Task<QuotaStatus> GetQuotaStatusAsync(string userId, string provider)
    {
        var quota = await GetUserQuotaAsync(userId, provider);
        var usage = await GetCurrentUsageAsync(userId, provider);

        var percentUsed = quota.DailyTokenLimit > 0
            ? (decimal)usage.TokensUsed / quota.DailyTokenLimit * 100
            : 0;

        return new QuotaStatus
        {
            DailyLimit = quota.DailyTokenLimit,
            Used = usage.TokensUsed,
            Remaining = Math.Max(0, quota.DailyTokenLimit - usage.TokensUsed),
            PercentUsed = percentUsed,
            ResetsAt = DateTime.Today.AddDays(1)
        };
    }

    private async Task CheckAndNotifyQuotaThresholds(string userId, decimal percentUsed)
    {
        if (percentUsed >= 100)
        {
            await _notifier.SendQuotaExceededAsync(userId);
        }
        else if (percentUsed >= 90)
        {
            await _notifier.SendQuotaWarningAsync(userId, 90);
        }
        else if (percentUsed >= 80)
        {
            await _notifier.SendQuotaWarningAsync(userId, 80);
        }
    }
}
```

### Acceptance Criteria
- ✅ Quota enforcement working
- ✅ Reset logic tested
- ✅ Warnings sent correctly
- ✅ Performance optimized

---

## Task 4.3: Implement Analytics Dashboard Data

**Status**: PENDING
**Priority**: MEDIUM
**Estimated**: 45 minutes
**Dependencies**: Task 4.2

### Analytics Data Tests

```csharp
[Fact]
public async Task Should_Generate_Usage_Trends()
{
    // Arrange & Act
    var trends = await _tracker.GetUsageTrendsAsync("user", 30); // Last 30 days

    // Assert
    trends.Should().HaveCount(30);
    trends.Should().BeInAscendingOrder(t => t.Date);
}

[Fact]
public async Task Should_Calculate_Provider_Distribution()
{
    // Arrange & Act
    var distribution = await _tracker.GetProviderDistributionAsync("user", DateTime.Today.AddDays(-7));

    // Assert
    distribution.Should().ContainKey("Anthropic");
    distribution.Values.Sum().Should().Be(100); // Percentages
}
```

### Acceptance Criteria
- ✅ Trend data accurate
- ✅ Distribution calculated
- ✅ Efficient queries
- ✅ Caching implemented

---

## Phase Completion Checklist

- [ ] Usage tracker implemented
- [ ] Cost calculation accurate
- [ ] Quota management working
- [ ] Analytics data available
- [ ] Notifications integrated
- [ ] Performance optimized
- [ ] 85%+ test coverage
- [ ] Documentation complete

---

## Output Artifacts

1. **Services**: `IApiUsageTracker.cs`, `ApiUsageTracker.cs`
2. **Managers**: `IQuotaManager.cs`, `QuotaManager.cs`
3. **Models**: `UsageDetails.cs`, `UsageStats.cs`, `QuotaStatus.cs`
4. **Repositories**: `IApiUsageRepository.cs`, `ApiUsageRepository.cs`
5. **Tests**: Complete usage tracking test suite

---

## Next Phase Dependencies

Phase 5 (Service Integration) depends on:
- Usage tracking operational
- Quota management working
- Analytics available