using DigitalMe.Common;
using DigitalMe.Data.Entities;
using DigitalMe.Repositories;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services;

/// <summary>
/// Service implementation for managing API configuration business logic and orchestration.
/// Handles API key lifecycle, validation, usage tracking, and configuration management.
/// </summary>
public class ApiConfigurationService : IApiConfigurationService
{
    private readonly IApiConfigurationRepository _repository;
    private readonly ILogger<ApiConfigurationService> _logger;

    /// <summary>
    /// Initializes a new instance of the ApiConfigurationService.
    /// </summary>
    /// <param name="repository">The repository for data access operations.</param>
    /// <param name="logger">The logger for diagnostic information.</param>
    public ApiConfigurationService(
        IApiConfigurationRepository repository,
        ILogger<ApiConfigurationService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateConfigurationAsync(Guid configurationId)
    {
        _logger.LogInformation("Deactivating API configuration {ConfigurationId}", configurationId);

        var configuration = await _repository.GetByIdAsync(configurationId);
        if (configuration == null)
        {
            _logger.LogWarning("API configuration {ConfigurationId} not found for deactivation", configurationId);
            return false;
        }

        if (!configuration.IsActive)
        {
            _logger.LogInformation("API configuration {ConfigurationId} is already deactivated", configurationId);
            return true;
        }

        configuration.IsActive = false;
        await _repository.UpdateAsync(configuration);

        _logger.LogInformation("Successfully deactivated API configuration {ConfigurationId} for user {UserId}, provider {Provider}",
            configurationId, configuration.UserId, configuration.Provider);

        return true;
    }

    /// <inheritdoc />
    public async Task TrackUsageAsync(Guid configurationId)
    {
        _logger.LogDebug("Tracking usage for API configuration {ConfigurationId}", configurationId);

        var configuration = await _repository.GetByIdAsync(configurationId);
        ValidationHelper.EnsureEntityExists(configuration, configurationId);

        configuration!.LastUsedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(configuration);

        _logger.LogDebug("Updated LastUsedAt for API configuration {ConfigurationId}", configurationId);
    }

    /// <inheritdoc />
    public async Task ValidateConfigurationAsync(Guid configurationId, ApiConfigurationStatus status)
    {
        _logger.LogInformation("Validating API configuration {ConfigurationId} with status {Status}",
            configurationId, status);

        var configuration = await _repository.GetByIdAsync(configurationId);
        ValidationHelper.EnsureEntityExists(configuration, configurationId);

        configuration!.ValidationStatus = status;
        configuration.LastValidatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(configuration);

        _logger.LogInformation("Updated validation status for API configuration {ConfigurationId} to {Status}",
            configurationId, status);
    }

    /// <inheritdoc />
    public async Task<ApiConfiguration> GetOrCreateConfigurationAsync(
        string userId,
        string provider,
        string? encryptedApiKey = null,
        string? encryptionIV = null,
        string? encryptionSalt = null,
        string? keyFingerprint = null)
    {
        ValidationHelper.ValidateUserId(userId, nameof(userId));
        ValidationHelper.ValidateProvider(provider, nameof(provider));

        _logger.LogDebug("Getting or creating API configuration for user {UserId}, provider {Provider}",
            userId, provider);

        // Try to get existing configuration
        var existing = await _repository.GetByUserAndProviderAsync(userId, provider);
        if (existing != null)
        {
            _logger.LogDebug("Found existing API configuration {ConfigurationId} for user {UserId}, provider {Provider}",
                existing.Id, userId, provider);
            return existing;
        }

        // Validate required parameters for new configuration
        if (string.IsNullOrWhiteSpace(encryptedApiKey))
        {
            throw new ArgumentException("Encrypted API key is required when creating a new configuration.", nameof(encryptedApiKey));
        }

        if (string.IsNullOrWhiteSpace(encryptionIV))
        {
            throw new ArgumentException("Encryption IV is required when creating a new configuration.", nameof(encryptionIV));
        }

        if (string.IsNullOrWhiteSpace(encryptionSalt))
        {
            throw new ArgumentException("Encryption salt is required when creating a new configuration.", nameof(encryptionSalt));
        }

        if (string.IsNullOrWhiteSpace(keyFingerprint))
        {
            throw new ArgumentException("Key fingerprint is required when creating a new configuration.", nameof(keyFingerprint));
        }

        // Create new configuration
        var newConfiguration = new ApiConfiguration
        {
            UserId = userId,
            Provider = provider,
            EncryptedApiKey = encryptedApiKey,
            EncryptionIV = encryptionIV,
            EncryptionSalt = encryptionSalt,
            KeyFingerprint = keyFingerprint,
            IsActive = true,
            ValidationStatus = ApiConfigurationStatus.Unknown
        };

        var created = await _repository.CreateAsync(newConfiguration);

        _logger.LogInformation("Created new API configuration {ConfigurationId} for user {UserId}, provider {Provider}",
            created.Id, userId, provider);

        return created;
    }

    /// <inheritdoc />
    public async Task<ApiConfiguration> RotateApiKeyAsync(
        Guid configurationId,
        string newEncryptedApiKey,
        string newEncryptionIV,
        string newEncryptionSalt,
        string newKeyFingerprint)
    {
        ValidationHelper.ValidateNotNull(newEncryptedApiKey, nameof(newEncryptedApiKey));
        ValidationHelper.ValidateNotNull(newEncryptionIV, nameof(newEncryptionIV));
        ValidationHelper.ValidateNotNull(newEncryptionSalt, nameof(newEncryptionSalt));
        ValidationHelper.ValidateNotNull(newKeyFingerprint, nameof(newKeyFingerprint));

        _logger.LogInformation("Rotating API key for configuration {ConfigurationId}", configurationId);

        var configuration = await _repository.GetByIdAsync(configurationId);
        ValidationHelper.EnsureEntityExists(configuration, configurationId);

        // Update encryption components
        configuration!.EncryptedApiKey = newEncryptedApiKey;
        configuration.EncryptionIV = newEncryptionIV;
        configuration.EncryptionSalt = newEncryptionSalt;
        configuration.KeyFingerprint = newKeyFingerprint;
        configuration.ValidationStatus = ApiConfigurationStatus.Unknown; // Reset validation status
        configuration.LastValidatedAt = null; // Clear last validation timestamp

        var updated = await _repository.UpdateAsync(configuration);

        _logger.LogInformation("Successfully rotated API key for configuration {ConfigurationId}, user {UserId}, provider {Provider}",
            configurationId, updated.UserId, updated.Provider);

        return updated;
    }

    /// <inheritdoc />
    public async Task<ApiConfiguration?> GetActiveConfigurationAsync(string userId, string provider)
    {
        ValidationHelper.ValidateUserId(userId, nameof(userId));
        ValidationHelper.ValidateProvider(provider, nameof(provider));

        _logger.LogDebug("Getting active configuration for user {UserId}, provider {Provider}",
            userId, provider);

        return await _repository.GetByUserAndProviderAsync(userId, provider);
    }

    /// <inheritdoc />
    public async Task<List<ApiConfiguration>> GetUserConfigurationsAsync(string userId)
    {
        ValidationHelper.ValidateUserId(userId, nameof(userId));

        _logger.LogDebug("Getting all configurations for user {UserId}", userId);

        return await _repository.GetAllByUserAsync(userId);
    }
}