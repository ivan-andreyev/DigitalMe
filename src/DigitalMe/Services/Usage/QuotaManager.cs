using DigitalMe.Common;
using DigitalMe.Data.Entities;
using DigitalMe.Models.Usage;
using DigitalMe.Repositories;
using DigitalMe.Services.Notifications;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Usage;

/// <summary>
/// Сервис для управления квотами использования API.
/// Проверяет лимиты, отслеживает использование и отправляет уведомления о превышении порогов.
/// </summary>
public class QuotaManager : IQuotaManager
{
    private readonly IApiUsageRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<QuotaManager> _logger;

    /// <summary>
    /// Квоты по умолчанию для уровней подписки (в токенах в день).
    /// </summary>
    private readonly Dictionary<string, int> _defaultQuotas = new()
    {
        ["Free"] = 10000,      // 10K tokens/day
        ["Basic"] = 100000,    // 100K tokens/day
        ["Premium"] = 1000000  // 1M tokens/day
    };

    /// <summary>
    /// Инициализирует новый экземпляр QuotaManager.
    /// </summary>
    /// <param name="repository">Репозиторий для работы с квотами и использованием.</param>
    /// <param name="notificationService">Сервис для отправки уведомлений.</param>
    /// <param name="logger">Логгер для диагностики.</param>
    public QuotaManager(
        IApiUsageRepository repository,
        INotificationService notificationService,
        ILogger<QuotaManager> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<bool> CanUseTokensAsync(string userId, string provider, int tokens)
    {
        ValidationHelper.ValidateUserId(userId, nameof(userId));
        ValidationHelper.ValidateProvider(provider, nameof(provider));

        if (tokens < 0)
        {
            throw new ArgumentException("Tokens must be non-negative.", nameof(tokens));
        }

        _logger.LogDebug("Checking quota for user {UserId}, provider {Provider}, tokens {Tokens}",
            userId, provider, tokens);

        // Get user quota
        var quota = await GetUserQuotaAsync(userId, provider).ConfigureAwait(false);
        var dailyLimit = quota?.DailyTokenLimit ?? _defaultQuotas["Free"];

        // Get current usage
        var dailyUsage = await _repository.GetDailyUsageAsync(userId, provider, DateTime.Today)
            .ConfigureAwait(false);
        var currentUsage = dailyUsage?.TokensUsed ?? 0;

        var canUse = currentUsage + tokens <= dailyLimit;

        _logger.LogDebug("Quota check result: {CanUse} (current: {Current}, requested: {Requested}, limit: {Limit})",
            canUse, currentUsage, tokens, dailyLimit);

        return canUse;
    }

    /// <inheritdoc />
    public async Task<QuotaStatus> GetQuotaStatusAsync(string userId, string provider)
    {
        ValidationHelper.ValidateUserId(userId, nameof(userId));
        ValidationHelper.ValidateProvider(provider, nameof(provider));

        _logger.LogDebug("Getting quota status for user {UserId}, provider {Provider}",
            userId, provider);

        // Get user quota
        var quota = await GetUserQuotaAsync(userId, provider).ConfigureAwait(false);
        var dailyLimit = quota?.DailyTokenLimit ?? _defaultQuotas["Free"];

        // Get current usage
        var dailyUsage = await _repository.GetDailyUsageAsync(userId, provider, DateTime.Today)
            .ConfigureAwait(false);
        var used = dailyUsage?.TokensUsed ?? 0;

        var percentUsed = dailyLimit > 0
            ? (decimal)used / dailyLimit * 100
            : 0;

        return new QuotaStatus
        {
            DailyLimit = dailyLimit,
            Used = used,
            Remaining = Math.Max(0, dailyLimit - used),
            PercentUsed = percentUsed,
            ResetsAt = DateTime.Today.AddDays(1) // Midnight UTC
        };
    }

    /// <inheritdoc />
    public async Task<DailyUsage> GetOrCreateDailyUsageAsync(string userId, string provider)
    {
        ValidationHelper.ValidateUserId(userId, nameof(userId));
        ValidationHelper.ValidateProvider(provider, nameof(provider));

        _logger.LogDebug("Getting or creating daily usage for user {UserId}, provider {Provider}",
            userId, provider);

        var dailyUsage = await _repository.GetDailyUsageAsync(userId, provider, DateTime.Today)
            .ConfigureAwait(false);

        if (dailyUsage != null)
        {
            _logger.LogDebug("Found existing daily usage for user {UserId}, provider {Provider}: {Tokens} tokens",
                userId, provider, dailyUsage.TokensUsed);
            return dailyUsage;
        }

        // Create new daily usage for today
        var newUsage = new DailyUsage
        {
            UserId = userId,
            Provider = provider,
            Date = DateTime.Today,
            TokensUsed = 0,
            RequestCount = 0,
            TotalCost = 0m
        };

        var created = await _repository.CreateDailyUsageAsync(newUsage).ConfigureAwait(false);

        _logger.LogDebug("Created new daily usage for user {UserId}, provider {Provider}",
            userId, provider);

        return created;
    }

    /// <inheritdoc />
    public async Task UpdateUsageAsync(string userId, string provider, int tokensUsed)
    {
        ValidationHelper.ValidateUserId(userId, nameof(userId));
        ValidationHelper.ValidateProvider(provider, nameof(provider));

        if (tokensUsed < 0)
        {
            throw new ArgumentException("Tokens used must be non-negative.", nameof(tokensUsed));
        }

        _logger.LogDebug("Updating usage for user {UserId}, provider {Provider}: +{Tokens} tokens",
            userId, provider, tokensUsed);

        // Get or create daily usage
        var dailyUsage = await GetOrCreateDailyUsageAsync(userId, provider).ConfigureAwait(false);

        // Update usage
        dailyUsage.TokensUsed += tokensUsed;

        await _repository.UpdateDailyUsageAsync(dailyUsage).ConfigureAwait(false);

        // Check notification thresholds
        await CheckAndNotifyQuotaThresholdsAsync(userId, provider, dailyUsage.TokensUsed)
            .ConfigureAwait(false);

        _logger.LogDebug("Updated daily usage for user {UserId}, provider {Provider}: total {Tokens} tokens",
            userId, provider, dailyUsage.TokensUsed);
    }

    /// <summary>
    /// Получает квоту пользователя или возвращает null, если не настроена.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="provider">Название провайдера.</param>
    /// <returns>Квота пользователя или null.</returns>
    private async Task<UserQuota?> GetUserQuotaAsync(string userId, string provider)
    {
        // Try provider-specific quota first
        var quota = await _repository.GetUserQuotaAsync(userId, provider).ConfigureAwait(false);

        if (quota != null)
        {
            return quota;
        }

        // Try global quota (provider = null)
        quota = await _repository.GetUserQuotaAsync(userId, null).ConfigureAwait(false);

        return quota;
    }

    /// <summary>
    /// Проверяет пороги использования и отправляет уведомления при необходимости.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="provider">Название провайдера.</param>
    /// <param name="currentUsage">Текущее использование в токенах.</param>
    private async Task CheckAndNotifyQuotaThresholdsAsync(
        string userId,
        string provider,
        int currentUsage)
    {
        // Get user quota
        var quota = await GetUserQuotaAsync(userId, provider).ConfigureAwait(false);

        // Check if notifications are enabled
        if (quota?.NotificationsEnabled == false)
        {
            _logger.LogDebug("Notifications disabled for user {UserId}, skipping threshold checks", userId);
            return;
        }

        var dailyLimit = quota?.DailyTokenLimit ?? _defaultQuotas["Free"];
        var percentUsed = dailyLimit > 0
            ? (decimal)currentUsage / dailyLimit * 100
            : 0;

        _logger.LogDebug("Checking notification thresholds for user {UserId}: {PercentUsed}% used",
            userId, percentUsed);

        // Check thresholds (100% > 90% > 80%)
        if (percentUsed >= 100)
        {
            _logger.LogWarning("Quota exceeded for user {UserId}: {PercentUsed}%", userId, percentUsed);
            await _notificationService.SendQuotaExceededAsync(userId).ConfigureAwait(false);
        }
        else if (percentUsed >= 90)
        {
            _logger.LogInformation("Quota warning for user {UserId}: {PercentUsed}% (90% threshold)",
                userId, percentUsed);
            await _notificationService.SendQuotaWarningAsync(userId, percentUsed).ConfigureAwait(false);
        }
        else if (percentUsed >= 80)
        {
            _logger.LogInformation("Quota warning for user {UserId}: {PercentUsed}% (80% threshold)",
                userId, percentUsed);
            await _notificationService.SendQuotaWarningAsync(userId, percentUsed).ConfigureAwait(false);
        }
    }
}