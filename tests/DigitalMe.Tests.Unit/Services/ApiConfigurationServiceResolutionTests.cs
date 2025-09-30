using Xunit;
using FluentAssertions;
using DigitalMe.Services;
using DigitalMe.Services.Security;
using DigitalMe.Repositories;
using DigitalMe.Data.Entities;
using DigitalMe.Models.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace DigitalMe.Tests.Unit.Services;

/// <summary>
/// TDD Test Suite for ApiConfigurationService Key Resolution
/// RED PHASE: Tests should initially fail
/// Tests cover key resolution, decryption, fallback to system keys, and validation
/// </summary>
public class ApiConfigurationServiceResolutionTests
{
    private readonly Mock<IApiConfigurationRepository> _mockRepository;
    private readonly Mock<IKeyEncryptionService> _mockEncryptionService;
    private readonly IConfiguration _configuration;
    private readonly ApiConfigurationService _service;

    public ApiConfigurationServiceResolutionTests()
    {
        _mockRepository = new Mock<IApiConfigurationRepository>();
        _mockEncryptionService = new Mock<IKeyEncryptionService>();

        // Setup system keys in configuration
        var configData = new Dictionary<string, string>
        {
            ["ApiKeys:Anthropic"] = "sk-ant-system-key-12345",
            ["ApiKeys:OpenAI"] = "sk-openai-system-key-67890",
            ["ApiKeys:Google"] = "google-system-key-abcde"
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        _service = new ApiConfigurationService(
            _mockRepository.Object,
            _mockEncryptionService.Object,
            _configuration,
            NullLogger<ApiConfigurationService>.Instance);
    }

    #region GetApiKeyAsync - User Key Resolution Tests

    [Fact]
    public async Task GetApiKeyAsync_Should_Return_Decrypted_User_Key_When_Exists()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";
        const string decryptedKey = "sk-ant-user-personal-key";

        var userConfig = new ApiConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            EncryptedApiKey = "encrypted_data",
            EncryptionIV = "iv_data",
            EncryptionSalt = "salt_data",
            AuthenticationTag = "tag_data",
            KeyFingerprint = "fingerprint",
            IsActive = true,
            ValidationStatus = ApiConfigurationStatus.Valid
        };

        _mockRepository
            .Setup(r => r.GetByUserAndProviderAsync(userId, provider))
            .ReturnsAsync(userConfig);

        var encryptedInfo = new EncryptedKeyInfo(
            userConfig.EncryptedApiKey,
            userConfig.EncryptionIV,
            userConfig.EncryptionSalt,
            userConfig.AuthenticationTag,
            userConfig.KeyFingerprint);

        _mockEncryptionService
            .Setup(e => e.DecryptApiKeyAsync(It.IsAny<EncryptedKeyInfo>(), userId))
            .ReturnsAsync(decryptedKey);

        // Act
        var result = await _service.GetApiKeyAsync(provider, userId);

        // Assert
        result.Should().Be(decryptedKey, "should return decrypted user key");
        _mockEncryptionService.Verify(e => e.DecryptApiKeyAsync(
            It.Is<EncryptedKeyInfo>(info =>
                info.EncryptedData == userConfig.EncryptedApiKey &&
                info.IV == userConfig.EncryptionIV &&
                info.Salt == userConfig.EncryptionSalt &&
                info.Tag == userConfig.AuthenticationTag &&
                info.KeyFingerprint == userConfig.KeyFingerprint),
            userId), Times.Once);
    }

    [Fact]
    public async Task GetApiKeyAsync_Should_Fallback_To_System_Key_When_No_User_Key()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";

        _mockRepository
            .Setup(r => r.GetByUserAndProviderAsync(userId, provider))
            .ReturnsAsync((ApiConfiguration?)null);

        // Act
        var result = await _service.GetApiKeyAsync(provider, userId);

        // Assert
        result.Should().Be("sk-ant-system-key-12345", "should fallback to system key from configuration");
        _mockEncryptionService.Verify(e => e.DecryptApiKeyAsync(It.IsAny<EncryptedKeyInfo>(), It.IsAny<string>()), Times.Never,
            "should not attempt decryption when no user key exists");
    }

    [Fact]
    public async Task GetApiKeyAsync_Should_Fallback_When_User_Key_Is_Inactive()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "OpenAI";

        var inactiveConfig = new ApiConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            EncryptedApiKey = "encrypted_data",
            IsActive = false
        };

        _mockRepository
            .Setup(r => r.GetByUserAndProviderAsync(userId, provider))
            .ReturnsAsync(inactiveConfig);

        // Act
        var result = await _service.GetApiKeyAsync(provider, userId);

        // Assert
        result.Should().Be("sk-openai-system-key-67890", "should fallback to system key when user key is inactive");
    }

    [Fact]
    public async Task GetApiKeyAsync_Should_Fallback_When_Decryption_Fails()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Google";

        var userConfig = new ApiConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            EncryptedApiKey = "encrypted_data",
            EncryptionIV = "iv_data",
            EncryptionSalt = "salt_data",
            AuthenticationTag = "tag_data",
            KeyFingerprint = "fingerprint",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByUserAndProviderAsync(userId, provider))
            .ReturnsAsync(userConfig);

        _mockEncryptionService
            .Setup(e => e.DecryptApiKeyAsync(It.IsAny<EncryptedKeyInfo>(), userId))
            .ThrowsAsync(new System.Security.Cryptography.CryptographicException("Decryption failed"));

        // Act
        var result = await _service.GetApiKeyAsync(provider, userId);

        // Assert
        result.Should().Be("google-system-key-abcde", "should fallback to system key when decryption fails");
    }

    [Fact]
    public async Task GetApiKeyAsync_Should_Throw_When_No_Keys_Available()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "UnknownProvider";

        _mockRepository
            .Setup(r => r.GetByUserAndProviderAsync(userId, provider))
            .ReturnsAsync((ApiConfiguration?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.GetApiKeyAsync(provider, userId));
    }

    #endregion

    #region GetApiKeyAsync - Input Validation Tests

    [Theory]
    [InlineData(null, "user123")]
    [InlineData("", "user123")]
    [InlineData("  ", "user123")]
    public async Task GetApiKeyAsync_Should_Reject_Invalid_Provider(string provider, string userId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.GetApiKeyAsync(provider, userId));
    }

    [Theory]
    [InlineData("Anthropic", null)]
    [InlineData("Anthropic", "")]
    [InlineData("Anthropic", "  ")]
    public async Task GetApiKeyAsync_Should_Reject_Invalid_UserId(string provider, string userId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.GetApiKeyAsync(provider, userId));
    }

    #endregion

    #region SetUserApiKeyAsync Tests

    [Fact]
    public async Task SetUserApiKeyAsync_Should_Encrypt_And_Store_New_Key()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";
        const string plainApiKey = "sk-ant-user-new-key";

        var encryptedInfo = new EncryptedKeyInfo(
            "encrypted_data",
            "iv_data",
            "salt_data",
            "tag_data",
            "fingerprint");

        _mockEncryptionService
            .Setup(e => e.EncryptApiKeyAsync(plainApiKey, userId))
            .ReturnsAsync(encryptedInfo);

        _mockRepository
            .Setup(r => r.GetByUserAndProviderAsync(userId, provider))
            .ReturnsAsync((ApiConfiguration?)null);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<ApiConfiguration>()))
            .ReturnsAsync((ApiConfiguration config) => config);

        // Act
        await _service.SetUserApiKeyAsync(provider, userId, plainApiKey);

        // Assert
        _mockEncryptionService.Verify(e => e.EncryptApiKeyAsync(plainApiKey, userId), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.Is<ApiConfiguration>(c =>
            c.UserId == userId &&
            c.Provider == provider &&
            c.EncryptedApiKey == encryptedInfo.EncryptedData &&
            c.EncryptionIV == encryptedInfo.IV &&
            c.EncryptionSalt == encryptedInfo.Salt &&
            c.AuthenticationTag == encryptedInfo.Tag &&
            c.KeyFingerprint == encryptedInfo.KeyFingerprint &&
            c.IsActive == true)), Times.Once);
    }

    [Fact]
    public async Task SetUserApiKeyAsync_Should_Update_Existing_Key()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";
        const string plainApiKey = "sk-ant-user-updated-key";

        var existingConfig = new ApiConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            EncryptedApiKey = "old_encrypted_data",
            IsActive = true
        };

        var encryptedInfo = new EncryptedKeyInfo(
            "new_encrypted_data",
            "new_iv_data",
            "new_salt_data",
            "new_tag_data",
            "new_fingerprint");

        _mockEncryptionService
            .Setup(e => e.EncryptApiKeyAsync(plainApiKey, userId))
            .ReturnsAsync(encryptedInfo);

        _mockRepository
            .Setup(r => r.GetByUserAndProviderAsync(userId, provider))
            .ReturnsAsync(existingConfig);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ApiConfiguration>()))
            .ReturnsAsync((ApiConfiguration config) => config);

        // Act
        await _service.SetUserApiKeyAsync(provider, userId, plainApiKey);

        // Assert
        _mockEncryptionService.Verify(e => e.EncryptApiKeyAsync(plainApiKey, userId), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<ApiConfiguration>(c =>
            c.Id == existingConfig.Id &&
            c.EncryptedApiKey == encryptedInfo.EncryptedData &&
            c.EncryptionIV == encryptedInfo.IV &&
            c.EncryptionSalt == encryptedInfo.Salt &&
            c.AuthenticationTag == encryptedInfo.Tag &&
            c.KeyFingerprint == encryptedInfo.KeyFingerprint)), Times.Once);
    }

    [Theory]
    [InlineData(null, "user123", "key")]
    [InlineData("", "user123", "key")]
    [InlineData("Anthropic", null, "key")]
    [InlineData("Anthropic", "", "key")]
    [InlineData("Anthropic", "user123", null)]
    [InlineData("Anthropic", "user123", "")]
    public async Task SetUserApiKeyAsync_Should_Reject_Invalid_Inputs(string provider, string userId, string apiKey)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.SetUserApiKeyAsync(provider, userId, apiKey));
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task GetApiKeyAsync_Should_Complete_Within_Performance_Budget()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";

        _mockRepository
            .Setup(r => r.GetByUserAndProviderAsync(userId, provider))
            .ReturnsAsync((ApiConfiguration?)null);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await _service.GetApiKeyAsync(provider, userId);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50,
            "key resolution should complete within 50ms");
    }

    #endregion
}