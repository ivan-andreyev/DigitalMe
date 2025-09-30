using DigitalMe.Data.Entities;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Repositories;

/// <summary>
/// Репозиторий для работы с записями использования API.
/// TODO Phase 6: Реализовать persistence в базу данных (DbContext integration).
/// </summary>
public class ApiUsageRepository : IApiUsageRepository
{
    private readonly ILogger<ApiUsageRepository> _logger;

    // TODO Phase 6: Replace with DbContext
    private readonly List<ApiUsageRecord> _usageRecords = new();
    private readonly List<DailyUsage> _dailyUsages = new();
    private readonly List<UserQuota> _userQuotas = new();

    public ApiUsageRepository(ILogger<ApiUsageRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<ApiUsageRecord> SaveUsageRecordAsync(ApiUsageRecord record)
    {
        if (record == null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        record.Id = Guid.NewGuid();
        _usageRecords.Add(record);

        _logger.LogDebug("Saved usage record {RecordId} for user {UserId}", record.Id, record.UserId);

        return Task.FromResult(record);
    }

    /// <inheritdoc />
    public Task<List<ApiUsageRecord>> GetUsageRecordsAsync(
        string userId,
        DateTime startDate,
        DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
        }

        var records = _usageRecords
            .Where(r => r.UserId == userId &&
                       r.RequestTimestamp.Date >= startDate.Date &&
                       r.RequestTimestamp.Date <= endDate.Date)
            .OrderBy(r => r.RequestTimestamp)
            .ToList();

        _logger.LogDebug("Retrieved {Count} usage records for user {UserId}", records.Count, userId);

        return Task.FromResult(records);
    }

    /// <inheritdoc />
    public Task<DailyUsage?> GetDailyUsageAsync(string userId, string provider, DateTime date)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("Provider cannot be null or empty.", nameof(provider));
        }

        var usage = _dailyUsages
            .FirstOrDefault(u => u.UserId == userId &&
                                u.Provider == provider &&
                                u.Date.Date == date.Date);

        return Task.FromResult(usage);
    }

    /// <inheritdoc />
    public Task<DailyUsage> UpdateDailyUsageAsync(DailyUsage dailyUsage)
    {
        if (dailyUsage == null)
        {
            throw new ArgumentNullException(nameof(dailyUsage));
        }

        var existing = _dailyUsages
            .FirstOrDefault(u => u.Id == dailyUsage.Id);

        if (existing != null)
        {
            _dailyUsages.Remove(existing);
        }

        _dailyUsages.Add(dailyUsage);

        _logger.LogDebug("Updated daily usage {UsageId} for user {UserId}", dailyUsage.Id, dailyUsage.UserId);

        return Task.FromResult(dailyUsage);
    }

    /// <inheritdoc />
    public Task<DailyUsage> CreateDailyUsageAsync(DailyUsage dailyUsage)
    {
        if (dailyUsage == null)
        {
            throw new ArgumentNullException(nameof(dailyUsage));
        }

        dailyUsage.Id = Guid.NewGuid();
        _dailyUsages.Add(dailyUsage);

        _logger.LogDebug("Created daily usage {UsageId} for user {UserId}, provider {Provider}",
            dailyUsage.Id, dailyUsage.UserId, dailyUsage.Provider);

        return Task.FromResult(dailyUsage);
    }

    /// <inheritdoc />
    public Task<UserQuota?> GetUserQuotaAsync(string userId, string? provider = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
        }

        var quota = _userQuotas
            .FirstOrDefault(q => q.UserId == userId && q.Provider == provider);

        return Task.FromResult(quota);
    }

    /// <inheritdoc />
    public Task<UserQuota> SaveUserQuotaAsync(UserQuota quota)
    {
        if (quota == null)
        {
            throw new ArgumentNullException(nameof(quota));
        }

        var existing = _userQuotas
            .FirstOrDefault(q => q.UserId == quota.UserId && q.Provider == quota.Provider);

        if (existing != null)
        {
            _userQuotas.Remove(existing);
            quota.Id = existing.Id;
        }
        else
        {
            quota.Id = Guid.NewGuid();
        }

        _userQuotas.Add(quota);

        _logger.LogDebug("Saved user quota {QuotaId} for user {UserId}, provider {Provider}",
            quota.Id, quota.UserId, quota.Provider ?? "ALL");

        return Task.FromResult(quota);
    }
}