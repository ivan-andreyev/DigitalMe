using DigitalMe.Common;
using DigitalMe.Data.Entities;
using DigitalMe.Models.Security;
using DigitalMe.Repositories;
using DigitalMe.Services.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace DigitalMe.Services;

/// <summary>
/// Service implementation for managing API configuration business logic and orchestration.
/// Handles API key lifecycle, validation, usage tracking, and configuration management.
/// Integrates encryption services for secure key storage and resolution.
/// </summary>
public class ApiConfigurationService : IApiConfigurationService
{
    private readonly IApiConfigurationRepository _repository;
    private readonly IKeyEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiConfigurationService> _logger;

    /// <summary>
    /// Initializes a new instance of the ApiConfigurationService.
    /// </summary>
    /// <param name="repository">The repository for data access operations.</param>
    /// <param name="encryptionService">The encryption service for key security.</param>
    /// <param name="configuration">The configuration for system-wide settings.</param>
    /// <param name="logger">The logger for diagnostic information.</param>
    public ApiConfigurationService(
        IApiConfigurationRepository repository,
        IKeyEncryptionService encryptionService,
        IConfiguration configuration,
        ILogger<ApiConfigurationService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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

    /// <inheritdoc />
    public async Task<string> GetApiKeyAsync(string provider, string userId)
    {
        ValidationHelper.ValidateProvider(provider, nameof(provider));
        ValidationHelper.ValidateUserId(userId, nameof(userId));
        ValidationHelper.ValidateNoSqlInjection(provider, nameof(provider));
        ValidationHelper.ValidateNoSqlInjection(userId, nameof(userId));

        _logger.LogDebug("Resolving API key for provider {Provider}, user {UserId}", provider, userId);

        // Try to get user's personal configuration
        var userConfig = await _repository.GetByUserAndProviderAsync(userId, provider);

        // If user has active configuration, try to decrypt their personal key
        if (userConfig != null && userConfig.IsActive)
        {
            try
            {
                _logger.LogDebug("Found active user configuration {ConfigurationId}, attempting decryption", userConfig.Id);

                var encryptedInfo = new EncryptedKeyInfo(
                    userConfig.EncryptedApiKey,
                    userConfig.EncryptionIV,
                    userConfig.EncryptionSalt,
                    userConfig.AuthenticationTag,
                    userConfig.KeyFingerprint);

                var decryptedKey = await _encryptionService.DecryptApiKeyAsync(encryptedInfo, userId);

                _logger.LogInformation("Successfully resolved user API key for provider {Provider}, user {UserId}", provider, userId);
                return decryptedKey;
            }
            catch (CryptographicException ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt user API key for provider {Provider}, user {UserId}, falling back to system key",
                    provider, userId);
                // Fall through to system key fallback
            }
        }

        // Fallback to system-wide key from configuration
        var systemKey = _configuration[$"ApiKeys:{provider}"];
        if (string.IsNullOrWhiteSpace(systemKey))
        {
            _logger.LogError("No API key available for provider {Provider}, user {UserId}", provider, userId);
            throw new InvalidOperationException($"No API key configured for provider '{provider}'");
        }

        _logger.LogInformation("Using system API key for provider {Provider}, user {UserId}", provider, userId);
        return systemKey;
    }

    /// <inheritdoc />
    public async Task SetUserApiKeyAsync(string provider, string userId, string plainApiKey)
    {
        ValidationHelper.ValidateProvider(provider, nameof(provider));
        ValidationHelper.ValidateUserId(userId, nameof(userId));
        ValidationHelper.ValidateNoSqlInjection(provider, nameof(provider));
        ValidationHelper.ValidateNoSqlInjection(userId, nameof(userId));
        ValidationHelper.ValidateNoXss(provider, nameof(provider));

        if (string.IsNullOrWhiteSpace(plainApiKey))
        {
            throw new ArgumentException("API key cannot be null or whitespace.", nameof(plainApiKey));
        }

        _logger.LogInformation("Setting user API key for provider {Provider}, user {UserId}", provider, userId);

        // Encrypt the API key
        var encryptedInfo = await _encryptionService.EncryptApiKeyAsync(plainApiKey, userId);

        // Check if configuration already exists
        var existingConfig = await _repository.GetByUserAndProviderAsync(userId, provider);

        if (existingConfig != null)
        {
            _logger.LogDebug("Updating existing configuration {ConfigurationId}", existingConfig.Id);

            // Update existing configuration
            UpdateConfigurationWithEncryptedKey(existingConfig, encryptedInfo);

            await _repository.UpdateAsync(existingConfig);

            _logger.LogInformation("Updated API key for configuration {ConfigurationId}, provider {Provider}, user {UserId}",
                existingConfig.Id, provider, userId);
        }
        else
        {
            _logger.LogDebug("Creating new configuration for provider {Provider}, user {UserId}", provider, userId);

            // Create new configuration
            var newConfig = new ApiConfiguration
            {
                UserId = userId,
                Provider = provider
            };

            UpdateConfigurationWithEncryptedKey(newConfig, encryptedInfo);

            var created = await _repository.CreateAsync(newConfig);

            _logger.LogInformation("Created new API configuration {ConfigurationId} for provider {Provider}, user {UserId}",
                created.Id, provider, userId);
        }
    }

    /// <inheritdoc />
    public async Task RotateUserApiKeyAsync(string provider, string userId, string newPlainApiKey)
    {
        ValidationHelper.ValidateProvider(provider, nameof(provider));
        ValidationHelper.ValidateUserId(userId, nameof(userId));

        if (string.IsNullOrWhiteSpace(newPlainApiKey))
        {
            throw new ArgumentException("New API key cannot be null or whitespace.", nameof(newPlainApiKey));
        }

        _logger.LogInformation("Rotating API key for provider {Provider}, user {UserId}", provider, userId);

        // Find existing active configuration
        var existingConfig = await _repository.GetByUserAndProviderAsync(userId, provider);

        // If existing configuration exists, deactivate it to preserve history
        if (existingConfig != null)
        {
            _logger.LogDebug("Deactivating existing configuration {ConfigurationId} before rotation", existingConfig.Id);
            existingConfig.IsActive = false;
            await _repository.UpdateAsync(existingConfig);
        }

        // Encrypt the new API key
        var encryptedInfo = await _encryptionService.EncryptApiKeyAsync(newPlainApiKey, userId);

        // Create new configuration with the rotated key
        var newConfig = new ApiConfiguration
        {
            UserId = userId,
            Provider = provider
        };

        UpdateConfigurationWithEncryptedKey(newConfig, encryptedInfo);

        var created = await _repository.CreateAsync(newConfig);

        _logger.LogInformation("Successfully rotated API key for provider {Provider}, user {UserId}, new configuration {ConfigurationId}",
            provider, userId, created.Id);
    }

    /// <inheritdoc />
    public async Task<List<ApiConfiguration>> GetKeyHistoryAsync(string userId, string provider)
    {
        ValidationHelper.ValidateUserId(userId, nameof(userId));
        ValidationHelper.ValidateProvider(provider, nameof(provider));

        _logger.LogDebug("Retrieving key history for user {UserId}, provider {Provider}", userId, provider);

        // Get all configurations for user
        var allConfigs = await _repository.GetAllByUserAsync(userId);

        // Filter by provider and sort by creation date descending (newest first)
        var history = allConfigs
            .Where(c => c.Provider == provider)
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        _logger.LogDebug("Found {Count} historical configurations for user {UserId}, provider {Provider}",
            history.Count, userId, provider);

        return history;
    }

    /// <summary>
    /// Updates a configuration entity with encrypted key information.
    /// </summary>
    /// <param name="configuration">The configuration to update.</param>
    /// <param name="encryptedInfo">The encrypted key information.</param>
    private static void UpdateConfigurationWithEncryptedKey(
        ApiConfiguration configuration,
        EncryptedKeyInfo encryptedInfo)
    {
        configuration.EncryptedApiKey = encryptedInfo.EncryptedData;
        configuration.EncryptionIV = encryptedInfo.IV;
        configuration.EncryptionSalt = encryptedInfo.Salt;
        configuration.AuthenticationTag = encryptedInfo.Tag;
        configuration.KeyFingerprint = encryptedInfo.KeyFingerprint;
        configuration.IsActive = true;
        configuration.ValidationStatus = ApiConfigurationStatus.Unknown;
        configuration.LastValidatedAt = null;
    }
}