using DigitalMe.Common;
using DigitalMe.Data.Entities;
using DigitalMe.Models.Usage;
using DigitalMe.Repositories;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Usage;

/// <summary>
/// Сервис для отслеживания использования API с расчетом стоимости и агрегацией статистики.
/// Записывает метрики каждого запроса и обновляет дневное использование для квотирования.
/// </summary>
public class ApiUsageTracker : IApiUsageTracker
{
    private readonly IApiUsageRepository _repository;
    private readonly ILogger<ApiUsageTracker> _logger;

    /// <summary>
    /// Стоимость за токен для каждого провайдера API (в долларах США).
    /// </summary>
    private readonly Dictionary<string, decimal> _costPerToken = new()
    {
        ["Anthropic"] = 0.000015m,  // $0.015 per 1K tokens
        ["OpenAI"] = 0.000020m,     // $0.020 per 1K tokens
        ["Slack"] = 0.0m,           // Free API
        ["GitHub"] = 0.0m           // Free API (included in plan)
    };

    /// <summary>
    /// Инициализирует новый экземпляр ApiUsageTracker.
    /// </summary>
    /// <param name="repository">Репозиторий для хранения записей об использовании.</param>
    /// <param name="logger">Логгер для диагностики.</param>
    public ApiUsageTracker(
        IApiUsageRepository repository,
        ILogger<ApiUsageTracker> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task RecordUsageAsync(string userId, string provider, UsageDetails details)
    {
        ValidationHelper.ValidateUserId(userId, nameof(userId));
        ValidationHelper.ValidateProvider(provider, nameof(provider));

        try
        {
            _logger.LogDebug("Recording usage for user {UserId}, provider {Provider}, tokens {Tokens}",
                userId, provider, details.TokensUsed);

            // Calculate cost estimate
            var costEstimate = CalculateCost(provider, details.TokensUsed);

            // Create usage record
            var record = new ApiUsageRecord
            {
                UserId = userId,
                Provider = provider,
                RequestType = details.RequestType,
                TokensUsed = details.TokensUsed,
                CostEstimate = costEstimate,
                ResponseTimeMs = (int)details.ResponseTime,
                Success = details.Success,
                ErrorType = details.ErrorType,
                RequestTimestamp = DateTime.UtcNow
            };

            // Save usage record
            await _repository.SaveUsageRecordAsync(record).ConfigureAwait(false);

            // Update daily usage for quota tracking
            await UpdateQuotaUsageAsync(userId, provider, details.TokensUsed, costEstimate)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Recorded usage for user {UserId}, provider {Provider}: {Tokens} tokens, ${Cost}",
                userId.Substring(0, Math.Min(8, userId.Length)) + "***",
                provider,
                details.TokensUsed,
                costEstimate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record usage for user {UserId}, provider {Provider}",
                userId, provider);
            // Don't throw - usage tracking shouldn't break the main flow
        }
    }

    /// <inheritdoc />
    public decimal CalculateCost(string provider, int tokens)
    {
        if (_costPerToken.TryGetValue(provider, out var costPerToken))
        {
            return tokens * costPerToken;
        }

        _logger.LogDebug("Unknown provider {Provider}, cost calculation returns 0", provider);
        return 0;
    }

    /// <inheritdoc />
    public async Task<UsageStats> GetUsageStatsAsync(
        string userId,
        DateTime startDate,
        DateTime endDate)
    {
        ValidationHelper.ValidateUserId(userId, nameof(userId));

        _logger.LogDebug("Getting usage stats for user {UserId} from {StartDate} to {EndDate}",
            userId, startDate, endDate);

        var records = await _repository.GetUsageRecordsAsync(userId, startDate, endDate)
            .ConfigureAwait(false);

        // Handle empty records
        if (!records.Any())
        {
            return new UsageStats
            {
                TotalTokens = 0,
                TotalCost = 0,
                RequestCount = 0,
                SuccessRate = 0,
                AverageResponseTime = 0,
                ByProvider = new Dictionary<string, ProviderStats>()
            };
        }

        // Aggregate statistics
        var totalTokens = records.Sum(r => (long)r.TokensUsed);
        var totalCost = records.Sum(r => r.CostEstimate);
        var requestCount = records.Count;
        var successCount = records.Count(r => r.Success);
        var successRate = requestCount > 0
            ? (decimal)successCount / requestCount * 100
            : 0;
        var averageResponseTime = records.Any()
            ? records.Average(r => r.ResponseTimeMs)
            : 0;

        // Group by provider
        var byProvider = records
            .GroupBy(r => r.Provider)
            .ToDictionary(
                g => g.Key,
                g => new ProviderStats
                {
                    Tokens = g.Sum(r => (long)r.TokensUsed),
                    Cost = g.Sum(r => r.CostEstimate),
                    Requests = g.Count()
                });

        return new UsageStats
        {
            TotalTokens = totalTokens,
            TotalCost = totalCost,
            RequestCount = requestCount,
            SuccessRate = successRate,
            AverageResponseTime = averageResponseTime,
            ByProvider = byProvider
        };
    }

    /// <summary>
    /// Обновляет дневное использование для отслеживания квот.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="provider">Название провайдера.</param>
    /// <param name="tokensUsed">Количество использованных токенов.</param>
    /// <param name="cost">Расчетная стоимость.</param>
    private async Task UpdateQuotaUsageAsync(
        string userId,
        string provider,
        int tokensUsed,
        decimal cost)
    {
        var currentUsage = await _repository.GetDailyUsageAsync(userId, provider, DateTime.Today)
            .ConfigureAwait(false);

        if (currentUsage != null)
        {
            // Update existing daily usage
            currentUsage.TokensUsed += tokensUsed;
            currentUsage.RequestCount++;
            currentUsage.TotalCost += cost;

            await _repository.UpdateDailyUsageAsync(currentUsage).ConfigureAwait(false);

            _logger.LogDebug("Updated daily usage for user {UserId}, provider {Provider}: total {Tokens} tokens",
                userId, provider, currentUsage.TokensUsed);
        }
        else
        {
            // Create new daily usage record
            var newUsage = new DailyUsage
            {
                UserId = userId,
                Provider = provider,
                Date = DateTime.Today,
                TokensUsed = tokensUsed,
                RequestCount = 1,
                TotalCost = cost
            };

            await _repository.CreateDailyUsageAsync(newUsage).ConfigureAwait(false);

            _logger.LogDebug("Created daily usage for user {UserId}, provider {Provider}: {Tokens} tokens",
                userId, provider, tokensUsed);
        }
    }
}