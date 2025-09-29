using DigitalMe.Data.Entities;
using DigitalMe.Repositories;
using DigitalMe.Services;
using DigitalMe.Services.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DigitalMe.Tests.Unit.Services;

/// <summary>
/// Comprehensive test suite for ApiConfigurationService business logic.
/// Tests cover orchestration, validation workflows, and business operations.
/// </summary>
public class ApiConfigurationServiceTests : BaseTestWithDatabase
{
    private readonly Mock<ILogger<ApiConfigurationService>> _mockLogger;
    private readonly Mock<IKeyEncryptionService> _mockEncryptionService;
    private readonly IConfiguration _configuration;
    private readonly ApiConfigurationService _service;
    private readonly IApiConfigurationRepository _repository;

    public ApiConfigurationServiceTests()
    {
        _mockLogger = new Mock<ILogger<ApiConfigurationService>>();
        _mockEncryptionService = new Mock<IKeyEncryptionService>();
        _repository = new ApiConfigurationRepository(Context);

        // Setup minimal configuration for tests
        var configData = new Dictionary<string, string>
        {
            ["ApiKeys:Anthropic"] = "sk-ant-system-key",
            ["ApiKeys:OpenAI"] = "sk-openai-system-key"
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        _service = new ApiConfigurationService(
            _repository,
            _mockEncryptionService.Object,
            _configuration,
            _mockLogger.Object);
    }

    private ApiConfiguration CreateTestConfiguration(
        string userId = "user123",
        string provider = "Anthropic",
        bool isActive = true)
    {
        return new ApiConfiguration
        {
            UserId = userId,
            Provider = provider,
            DisplayName = $"{provider} Key",
            EncryptedApiKey = "encrypted_key_data_12345",
            EncryptionIV = "iv_data_12345",
            EncryptionSalt = "salt_data_12345",
            AuthenticationTag = "tag_data_12345",
            KeyFingerprint = "fingerprint_12345",
            IsActive = isActive,
            ValidationStatus = ApiConfigurationStatus.Unknown
        };
    }

    #region DeactivateConfigurationAsync Tests

    [Fact]
    public async Task DeactivateConfigurationAsync_Should_Deactivate_Active_Configuration()
    {
        // Arrange
        var config = CreateTestConfiguration(isActive: true);
        await Context.ApiConfigurations.AddAsync(config);
        await Context.SaveChangesAsync();
        Context.Entry(config).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

        // Act
        var result = await _service.DeactivateConfigurationAsync(config.Id);

        // Assert
        result.Should().BeTrue();

        // Verify in database
        var deactivated = await Context.ApiConfigurations.FindAsync(config.Id);
        deactivated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateConfigurationAsync_Should_Return_False_When_Configuration_Not_Found()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.DeactivateConfigurationAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateConfigurationAsync_Should_Be_Idempotent_For_Already_Inactive()
    {
        // Arrange
        var config = CreateTestConfiguration(isActive: false);
        await Context.ApiConfigurations.AddAsync(config);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.DeactivateConfigurationAsync(config.Id);

        // Assert
        result.Should().BeTrue(); // Idempotent behavior
        var stillInactive = await Context.ApiConfigurations.FindAsync(config.Id);
        stillInactive!.IsActive.Should().BeFalse();
    }

    #endregion

    #region TrackUsageAsync Tests

    [Fact]
    public async Task TrackUsageAsync_Should_Update_LastUsedAt_Timestamp()
    {
        // Arrange
        var config = CreateTestConfiguration();
        config.LastUsedAt = null;
        await Context.ApiConfigurations.AddAsync(config);
        await Context.SaveChangesAsync();
        Context.Entry(config).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

        var beforeTracking = DateTime.UtcNow.AddSeconds(-1);

        // Act
        await _service.TrackUsageAsync(config.Id);
        var afterTracking = DateTime.UtcNow.AddSeconds(1);

        // Assert
        var updated = await Context.ApiConfigurations.FindAsync(config.Id);
        updated!.LastUsedAt.Should().NotBeNull();
        updated.LastUsedAt!.Value.Should().BeAfter(beforeTracking);
        updated.LastUsedAt!.Value.Should().BeBefore(afterTracking);
    }

    [Fact]
    public async Task TrackUsageAsync_Should_Throw_InvalidOperationException_For_Nonexistent_Configuration()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.TrackUsageAsync(nonExistentId));
    }

    #endregion

    #region ValidateConfigurationAsync Tests

    [Fact]
    public async Task ValidateConfigurationAsync_Should_Update_ValidationStatus_And_Timestamp()
    {
        // Arrange
        var config = CreateTestConfiguration();
        config.ValidationStatus = ApiConfigurationStatus.Unknown;
        config.LastValidatedAt = null;
        await Context.ApiConfigurations.AddAsync(config);
        await Context.SaveChangesAsync();
        Context.Entry(config).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

        var beforeValidation = DateTime.UtcNow.AddSeconds(-1);

        // Act
        await _service.ValidateConfigurationAsync(config.Id, ApiConfigurationStatus.Valid);
        var afterValidation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        var updated = await Context.ApiConfigurations.FindAsync(config.Id);
        updated!.ValidationStatus.Should().Be(ApiConfigurationStatus.Valid);
        updated.LastValidatedAt.Should().NotBeNull();
        updated.LastValidatedAt!.Value.Should().BeAfter(beforeValidation);
        updated.LastValidatedAt!.Value.Should().BeBefore(afterValidation);
    }

    [Fact]
    public async Task ValidateConfigurationAsync_Should_Throw_InvalidOperationException_For_Nonexistent_Configuration()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.ValidateConfigurationAsync(nonExistentId, ApiConfigurationStatus.Valid));
    }

    #endregion

    #region GetOrCreateConfigurationAsync Tests

    [Fact]
    public async Task GetOrCreateConfigurationAsync_Should_Return_Existing_Configuration()
    {
        // Arrange
        var existing = CreateTestConfiguration();
        await Context.ApiConfigurations.AddAsync(existing);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetOrCreateConfigurationAsync("user123", "Anthropic");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(existing.Id);
        result.UserId.Should().Be("user123");
        result.Provider.Should().Be("Anthropic");

        // Should not create duplicate
        var count = Context.ApiConfigurations.Count();
        count.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateConfigurationAsync_Should_Create_New_Configuration()
    {
        // Arrange - no existing configuration
        // Act
        var result = await _service.GetOrCreateConfigurationAsync(
            "user456",
            "OpenAI",
            "new_encrypted_key",
            "new_iv",
            "new_salt",
            "new_fingerprint");

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be("user456");
        result.Provider.Should().Be("OpenAI");
        result.EncryptedApiKey.Should().Be("new_encrypted_key");
        result.EncryptionIV.Should().Be("new_iv");
        result.EncryptionSalt.Should().Be("new_salt");
        result.KeyFingerprint.Should().Be("new_fingerprint");
        result.IsActive.Should().BeTrue();
        result.ValidationStatus.Should().Be(ApiConfigurationStatus.Unknown);

        // Verify saved to database
        var saved = await Context.ApiConfigurations.FindAsync(result.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOrCreateConfigurationAsync_Should_Throw_ArgumentException_When_Creating_Without_EncryptedKey()
    {
        // Arrange - no existing configuration
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.GetOrCreateConfigurationAsync("user789", "Anthropic"));
    }

    [Theory]
    [InlineData(null, "Provider")]
    [InlineData("", "Provider")]
    [InlineData("user", null)]
    [InlineData("user", "")]
    public async Task GetOrCreateConfigurationAsync_Should_Throw_ArgumentException_For_Invalid_Parameters(
        string userId, string provider)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.GetOrCreateConfigurationAsync(userId, provider));
    }

    #endregion

    #region RotateApiKeyAsync Tests

    [Fact]
    public async Task RotateApiKeyAsync_Should_Update_Encryption_Components()
    {
        // Arrange
        var config = CreateTestConfiguration();
        config.ValidationStatus = ApiConfigurationStatus.Valid;
        config.LastValidatedAt = DateTime.UtcNow;
        await Context.ApiConfigurations.AddAsync(config);
        await Context.SaveChangesAsync();
        Context.Entry(config).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

        // Act
        var result = await _service.RotateApiKeyAsync(
            config.Id,
            "new_encrypted_key",
            "new_iv",
            "new_salt",
            "new_fingerprint");

        // Assert
        result.Should().NotBeNull();
        result.EncryptedApiKey.Should().Be("new_encrypted_key");
        result.EncryptionIV.Should().Be("new_iv");
        result.EncryptionSalt.Should().Be("new_salt");
        result.KeyFingerprint.Should().Be("new_fingerprint");
        result.ValidationStatus.Should().Be(ApiConfigurationStatus.Unknown, "validation status should reset");
        result.LastValidatedAt.Should().BeNull("validation timestamp should clear");

        // Verify in database
        var updated = await Context.ApiConfigurations.FindAsync(config.Id);
        updated!.EncryptedApiKey.Should().Be("new_encrypted_key");
    }

    [Fact]
    public async Task RotateApiKeyAsync_Should_Throw_InvalidOperationException_For_Nonexistent_Configuration()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.RotateApiKeyAsync(nonExistentId, "key", "iv", "salt", "fingerprint"));
    }

    [Theory]
    [InlineData(null, "iv", "salt", "fingerprint")]
    [InlineData("key", null, "salt", "fingerprint")]
    [InlineData("key", "iv", null, "fingerprint")]
    [InlineData("key", "iv", "salt", null)]
    public async Task RotateApiKeyAsync_Should_Throw_ArgumentNullException_For_Invalid_Parameters(
        string encryptedKey, string iv, string salt, string fingerprint)
    {
        // Arrange
        var configId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _service.RotateApiKeyAsync(configId, encryptedKey, iv, salt, fingerprint));
    }

    #endregion

    #region GetActiveConfigurationAsync Tests

    [Fact]
    public async Task GetActiveConfigurationAsync_Should_Return_Active_Configuration()
    {
        // Arrange
        var active = CreateTestConfiguration(isActive: true);
        await Context.ApiConfigurations.AddAsync(active);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetActiveConfigurationAsync("user123", "Anthropic");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(active.Id);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveConfigurationAsync_Should_Return_Null_When_Not_Found()
    {
        // Act
        var result = await _service.GetActiveConfigurationAsync("nonexistent", "Unknown");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null, "Provider")]
    [InlineData("", "Provider")]
    [InlineData("user", null)]
    [InlineData("user", "")]
    public async Task GetActiveConfigurationAsync_Should_Throw_ArgumentException_For_Invalid_Parameters(
        string userId, string provider)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.GetActiveConfigurationAsync(userId, provider));
    }

    #endregion

    #region GetUserConfigurationsAsync Tests

    [Fact]
    public async Task GetUserConfigurationsAsync_Should_Return_All_User_Configurations()
    {
        // Arrange
        var config1 = CreateTestConfiguration(provider: "Anthropic");
        var config2 = CreateTestConfiguration(provider: "OpenAI");
        var config3 = CreateTestConfiguration(userId: "other_user", provider: "Google");

        await Context.ApiConfigurations.AddRangeAsync(config1, config2, config3);
        await Context.SaveChangesAsync();

        // Act
        var results = await _service.GetUserConfigurationsAsync("user123");

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(c => c.UserId == "user123");
        results.Select(c => c.Provider).Should().Contain(new[] { "Anthropic", "OpenAI" });
    }

    [Fact]
    public async Task GetUserConfigurationsAsync_Should_Return_Empty_List_When_No_Configurations()
    {
        // Act
        var results = await _service.GetUserConfigurationsAsync("nonexistent_user");

        // Assert
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetUserConfigurationsAsync_Should_Throw_ArgumentException_For_Invalid_UserId(string userId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.GetUserConfigurationsAsync(userId));
    }

    #endregion
}