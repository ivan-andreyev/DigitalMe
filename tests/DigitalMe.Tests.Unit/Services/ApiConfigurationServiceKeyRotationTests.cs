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
/// TDD Test Suite for ApiConfigurationService Key Rotation
/// RED PHASE: Tests should initially fail
/// Tests cover key rotation, history tracking, and old key deactivation
/// </summary>
public class ApiConfigurationServiceKeyRotationTests
{
    private readonly Mock<IApiConfigurationRepository> _mockRepository;
    private readonly Mock<IKeyEncryptionService> _mockEncryptionService;
    private readonly IConfiguration _configuration;
    private readonly ApiConfigurationService _service;

    public ApiConfigurationServiceKeyRotationTests()
    {
        _mockRepository = new Mock<IApiConfigurationRepository>();
        _mockEncryptionService = new Mock<IKeyEncryptionService>();

        var configData = new Dictionary<string, string>
        {
            ["ApiKeys:Anthropic"] = "sk-ant-system-key"
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

    #region Key Rotation Tests

    [Fact]
    public async Task RotateUserApiKeyAsync_Should_Deactivate_Old_And_Create_New_Config()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";
        const string newPlainKey = "sk-ant-new-rotated-key";

        var existingConfig = new ApiConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            EncryptedApiKey = "old_encrypted_data",
            EncryptionIV = "old_iv",
            EncryptionSalt = "old_salt",
            AuthenticationTag = "old_tag",
            KeyFingerprint = "old_fingerprint",
            IsActive = true
        };

        var newEncryptedInfo = new EncryptedKeyInfo(
            "new_encrypted_data",
            "new_iv",
            "new_salt",
            "new_tag",
            "new_fingerprint");

        _mockRepository
            .Setup(r => r.GetByUserAndProviderAsync(userId, provider))
            .ReturnsAsync(existingConfig);

        _mockEncryptionService
            .Setup(e => e.EncryptApiKeyAsync(newPlainKey, userId))
            .ReturnsAsync(newEncryptedInfo);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ApiConfiguration>()))
            .ReturnsAsync((ApiConfiguration config) => config);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<ApiConfiguration>()))
            .ReturnsAsync((ApiConfiguration config) => config);

        // Act
        await _service.RotateUserApiKeyAsync(provider, userId, newPlainKey);

        // Assert
        // Verify encryption was called
        _mockEncryptionService.Verify(e => e.EncryptApiKeyAsync(newPlainKey, userId), Times.Once);

        // Verify old config was deactivated
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<ApiConfiguration>(c =>
            c.Id == existingConfig.Id &&
            c.IsActive == false)), Times.Once);

        // Verify new config was created with encrypted data
        _mockRepository.Verify(r => r.CreateAsync(It.Is<ApiConfiguration>(c =>
            c.UserId == userId &&
            c.Provider == provider &&
            c.EncryptedApiKey == newEncryptedInfo.EncryptedData &&
            c.EncryptionIV == newEncryptedInfo.IV &&
            c.EncryptionSalt == newEncryptedInfo.Salt &&
            c.AuthenticationTag == newEncryptedInfo.Tag &&
            c.KeyFingerprint == newEncryptedInfo.KeyFingerprint &&
            c.IsActive == true)), Times.Once);
    }

    [Fact]
    public async Task RotateUserApiKeyAsync_Should_Create_Config_If_Not_Exists()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";
        const string newPlainKey = "sk-ant-new-key";

        var newEncryptedInfo = new EncryptedKeyInfo(
            "encrypted_data",
            "iv",
            "salt",
            "tag",
            "fingerprint");

        _mockRepository
            .Setup(r => r.GetByUserAndProviderAsync(userId, provider))
            .ReturnsAsync((ApiConfiguration?)null);

        _mockEncryptionService
            .Setup(e => e.EncryptApiKeyAsync(newPlainKey, userId))
            .ReturnsAsync(newEncryptedInfo);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<ApiConfiguration>()))
            .ReturnsAsync((ApiConfiguration config) => config);

        // Act
        await _service.RotateUserApiKeyAsync(provider, userId, newPlainKey);

        // Assert
        _mockRepository.Verify(r => r.CreateAsync(It.Is<ApiConfiguration>(c =>
            c.UserId == userId &&
            c.Provider == provider &&
            c.EncryptedApiKey == newEncryptedInfo.EncryptedData &&
            c.IsActive == true)), Times.Once);
    }

    [Fact]
    public async Task RotateUserApiKeyAsync_Should_Verify_New_Key_After_Rotation()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";
        const string newPlainKey = "sk-ant-new-verified-key";

        var existingConfig = new ApiConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            EncryptedApiKey = "old_data",
            IsActive = true
        };

        var newEncryptedInfo = new EncryptedKeyInfo("new_data", "iv", "salt", "tag", "fingerprint");

        _mockRepository
            .Setup(r => r.GetByUserAndProviderAsync(userId, provider))
            .ReturnsAsync(existingConfig);

        _mockEncryptionService
            .Setup(e => e.EncryptApiKeyAsync(newPlainKey, userId))
            .ReturnsAsync(newEncryptedInfo);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ApiConfiguration>()))
            .ReturnsAsync((ApiConfiguration config) => config);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<ApiConfiguration>()))
            .ReturnsAsync((ApiConfiguration config) => config);

        // Act
        await _service.RotateUserApiKeyAsync(provider, userId, newPlainKey);

        // Verify the new key can be retrieved
        _mockEncryptionService
            .Setup(e => e.DecryptApiKeyAsync(It.IsAny<EncryptedKeyInfo>(), userId))
            .ReturnsAsync(newPlainKey);

        _mockRepository
            .Setup(r => r.GetByUserAndProviderAsync(userId, provider))
            .ReturnsAsync(new ApiConfiguration
            {
                UserId = userId,
                Provider = provider,
                EncryptedApiKey = newEncryptedInfo.EncryptedData,
                EncryptionIV = newEncryptedInfo.IV,
                EncryptionSalt = newEncryptedInfo.Salt,
                AuthenticationTag = newEncryptedInfo.Tag,
                KeyFingerprint = newEncryptedInfo.KeyFingerprint,
                IsActive = true
            });

        var retrievedKey = await _service.GetApiKeyAsync(provider, userId);

        // Assert
        retrievedKey.Should().Be(newPlainKey, "rotated key should be retrievable");
    }

    [Theory]
    [InlineData(null, "user123", "key")]
    [InlineData("", "user123", "key")]
    [InlineData("Anthropic", null, "key")]
    [InlineData("Anthropic", "", "key")]
    [InlineData("Anthropic", "user123", null)]
    [InlineData("Anthropic", "user123", "")]
    public async Task RotateUserApiKeyAsync_Should_Reject_Invalid_Inputs(string provider, string userId, string newKey)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.RotateUserApiKeyAsync(provider, userId, newKey));
    }

    #endregion

    #region Key History Tests

    [Fact]
    public async Task GetUserConfigurationsAsync_Should_Return_All_User_Configurations()
    {
        // Arrange
        const string userId = "user123";

        var configs = new List<ApiConfiguration>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Provider = "Anthropic", IsActive = true },
            new() { Id = Guid.NewGuid(), UserId = userId, Provider = "OpenAI", IsActive = true },
            new() { Id = Guid.NewGuid(), UserId = userId, Provider = "Google", IsActive = false }
        };

        _mockRepository
            .Setup(r => r.GetAllByUserAsync(userId))
            .ReturnsAsync(configs);

        // Act
        var result = await _service.GetUserConfigurationsAsync(userId);

        // Assert
        result.Should().HaveCount(3, "should return all configurations for user");
        result.Should().Contain(c => c.Provider == "Anthropic" && c.IsActive);
        result.Should().Contain(c => c.Provider == "OpenAI" && c.IsActive);
        result.Should().Contain(c => c.Provider == "Google" && !c.IsActive);
    }

    [Fact]
    public async Task GetUserConfigurationsAsync_Should_Include_Inactive_Keys()
    {
        // Arrange
        const string userId = "user123";

        var configs = new List<ApiConfiguration>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Provider = "Anthropic", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), UserId = userId, Provider = "Anthropic", IsActive = false, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };

        _mockRepository
            .Setup(r => r.GetAllByUserAsync(userId))
            .ReturnsAsync(configs);

        // Act
        var result = await _service.GetUserConfigurationsAsync(userId);

        // Assert
        result.Should().HaveCount(2, "should include both active and inactive keys");
        result.Count(c => c.IsActive).Should().Be(1, "only one active key");
        result.Count(c => !c.IsActive).Should().Be(1, "one inactive key from rotation history");
    }

    [Fact]
    public async Task GetKeyHistoryAsync_Should_Return_All_Configs_For_Provider()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";

        var now = DateTime.UtcNow;
        var allConfigs = new List<ApiConfiguration>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Provider = "Anthropic", IsActive = true, CreatedAt = now },
            new() { Id = Guid.NewGuid(), UserId = userId, Provider = "Anthropic", IsActive = false, CreatedAt = now.AddDays(-1) },
            new() { Id = Guid.NewGuid(), UserId = userId, Provider = "Anthropic", IsActive = false, CreatedAt = now.AddDays(-2) },
            new() { Id = Guid.NewGuid(), UserId = userId, Provider = "OpenAI", IsActive = true, CreatedAt = now }
        };

        _mockRepository
            .Setup(r => r.GetAllByUserAsync(userId))
            .ReturnsAsync(allConfigs);

        // Act
        var result = await _service.GetKeyHistoryAsync(userId, provider);

        // Assert
        result.Should().HaveCount(3, "should return all Anthropic configs (active + inactive)");
        result.Should().NotContain(c => c.Provider != provider, "should filter by provider");
        result.Should().BeInDescendingOrder(c => c.CreatedAt, "should be sorted newest first");
        result.Count(c => c.IsActive).Should().Be(1, "only one active key");
        result.Count(c => !c.IsActive).Should().Be(2, "two inactive keys from rotation history");
    }

    [Fact]
    public async Task GetKeyHistoryAsync_Should_Return_Empty_List_When_No_History()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";

        _mockRepository
            .Setup(r => r.GetAllByUserAsync(userId))
            .ReturnsAsync(new List<ApiConfiguration>());

        // Act
        var result = await _service.GetKeyHistoryAsync(userId, provider);

        // Assert
        result.Should().BeEmpty("no configurations exist for this user/provider");
    }

    [Theory]
    [InlineData(null, "Anthropic")]
    [InlineData("", "Anthropic")]
    [InlineData("user123", null)]
    [InlineData("user123", "")]
    public async Task GetKeyHistoryAsync_Should_Reject_Invalid_Inputs(string userId, string provider)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.GetKeyHistoryAsync(userId, provider));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Complete_Key_Rotation_Workflow_Should_Work()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";
        const string originalKey = "sk-ant-original-key";
        const string rotatedKey = "sk-ant-rotated-key";

        // Setup for original key
        var originalEncrypted = new EncryptedKeyInfo("orig_data", "orig_iv", "orig_salt", "orig_tag", "orig_fp");
        _mockEncryptionService
            .Setup(e => e.EncryptApiKeyAsync(originalKey, userId))
            .ReturnsAsync(originalEncrypted);

        _mockRepository
            .Setup(r => r.GetByUserAndProviderAsync(userId, provider))
            .ReturnsAsync((ApiConfiguration?)null);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<ApiConfiguration>()))
            .ReturnsAsync((ApiConfiguration config) => config);

        // Act 1: Set original key
        await _service.SetUserApiKeyAsync(provider, userId, originalKey);

        // Setup for rotation
        var existingConfig = new ApiConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            EncryptedApiKey = originalEncrypted.EncryptedData,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByUserAndProviderAsync(userId, provider))
            .ReturnsAsync(existingConfig);

        var rotatedEncrypted = new EncryptedKeyInfo("rot_data", "rot_iv", "rot_salt", "rot_tag", "rot_fp");
        _mockEncryptionService
            .Setup(e => e.EncryptApiKeyAsync(rotatedKey, userId))
            .ReturnsAsync(rotatedEncrypted);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ApiConfiguration>()))
            .ReturnsAsync((ApiConfiguration config) => config);

        // Act 2: Rotate key
        await _service.RotateUserApiKeyAsync(provider, userId, rotatedKey);

        // Assert
        _mockEncryptionService.Verify(e => e.EncryptApiKeyAsync(originalKey, userId), Times.Once, "original key should be encrypted");
        _mockEncryptionService.Verify(e => e.EncryptApiKeyAsync(rotatedKey, userId), Times.Once, "rotated key should be encrypted");
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<ApiConfiguration>()), Times.Exactly(2), "original config created + new config created on rotation");
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ApiConfiguration>()), Times.Once, "old config should be deactivated on rotation");
    }

    #endregion
}