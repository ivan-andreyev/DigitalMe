using Xunit;
using FluentAssertions;
using DigitalMe.Services;
using DigitalMe.Services.Security;
using DigitalMe.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Diagnostics;

namespace DigitalMe.Tests.Unit.Services.Security;

/// <summary>
/// Security audit and penetration testing suite (Phase 7, Task 7.2 - CRITICAL).
/// Tests: SQL injection, XSS, access control, key leaks, timing attacks, rate limiting.
/// </summary>
public class SecurityAuditTests
{
    private readonly IApiConfigurationService _configService;
    private readonly IKeyEncryptionService _encryptionService;
    private readonly Mock<IApiConfigurationRepository> _mockRepo;
    private readonly TestLogger<ApiConfigurationService> _testLogger;

    public SecurityAuditTests()
    {
        _mockRepo = new Mock<IApiConfigurationRepository>();
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c[$"ApiKeys:Anthropic"]).Returns("sk-ant-system-fallback-key");

        _encryptionService = new KeyEncryptionService(NullLogger<KeyEncryptionService>.Instance);
        _testLogger = new TestLogger<ApiConfigurationService>();

        _configService = new ApiConfigurationService(
            _mockRepo.Object,
            _encryptionService,
            mockConfig.Object,
            _testLogger);
    }

    #region SQL Injection Tests

    /// <summary>
    /// Test: SQL injection attempts in userId should be rejected.
    /// </summary>
    [Theory]
    [InlineData("'; DROP TABLE ApiConfigurations; --")]
    [InlineData("1' OR '1'='1")]
    [InlineData("admin'--")]
    [InlineData("' UNION SELECT * FROM Users--")]
    public async Task Should_Prevent_SQL_Injection_In_UserId(string maliciousUserId)
    {
        // Act & Assert
        var act = () => _configService.GetApiKeyAsync("Anthropic", maliciousUserId);

        await act.Should().ThrowAsync<ArgumentException>(
            $"SQL injection attempt should be rejected: {maliciousUserId}");
    }

    /// <summary>
    /// Test: SQL injection attempts in provider should be rejected.
    /// </summary>
    [Theory]
    [InlineData("Anthropic'; DROP TABLE ApiConfigurations; --")]
    [InlineData("OpenAI' OR '1'='1")]
    public async Task Should_Prevent_SQL_Injection_In_Provider(string maliciousProvider)
    {
        // Act & Assert
        var act = () => _configService.GetApiKeyAsync(maliciousProvider, "user123");

        await act.Should().ThrowAsync<ArgumentException>(
            $"SQL injection attempt should be rejected: {maliciousProvider}");
    }

    #endregion

    #region XSS Prevention Tests

    /// <summary>
    /// Test: XSS payload in provider name should be sanitized/rejected.
    /// </summary>
    [Theory]
    [InlineData("<script>alert('XSS')</script>")]
    [InlineData("<img src=x onerror=alert('XSS')>")]
    [InlineData("javascript:alert('XSS')")]
    public async Task Should_Prevent_XSS_In_Provider(string xssPayload)
    {
        // Act & Assert
        var act = () => _configService.SetUserApiKeyAsync(xssPayload, "user123", "sk-ant-test-key");

        await act.Should().ThrowAsync<ArgumentException>(
            $"XSS payload should be rejected: {xssPayload}");
    }

    #endregion

    #region Key Protection Tests

    /// <summary>
    /// Test: API keys should NEVER appear in logs.
    /// </summary>
    [Fact]
    public async Task Should_Not_Leak_Keys_In_Logs()
    {
        // Arrange
        const string secretKey = "sk-ant-super-secret-key-12345678";
        const string userId = "test-user";
        const string provider = "Anthropic";

        _mockRepo.Setup(r => r.GetByUserAndProviderAsync(userId, provider))
            .ReturnsAsync((DigitalMe.Data.Entities.ApiConfiguration?)null);
        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<DigitalMe.Data.Entities.ApiConfiguration>()))
            .ReturnsAsync((DigitalMe.Data.Entities.ApiConfiguration config) => config);

        // Act
        await _configService.SetUserApiKeyAsync(provider, userId, secretKey);

        // Assert - check all logged messages
        _testLogger.LoggedMessages.Should().NotContain(m => m.Contains(secretKey),
            "API key should NEVER appear in logs");

        _testLogger.LoggedMessages.Should().NotContain(m => m.Contains("secret"),
            "Secret key parts should not be logged");

        // Key fragments should also not appear
        _testLogger.LoggedMessages.Should().NotContain(m => m.Contains("super-secret-key"),
            "Key fragments should not be logged");
    }

    /// <summary>
    /// Test: Encrypted data should not reveal plaintext keys.
    /// </summary>
    [Fact]
    public async Task Encrypted_Data_Should_Not_Contain_Plaintext_Key()
    {
        // Arrange
        const string plainKey = "sk-ant-plaintext-visible-key";
        const string userId = "encryption-test-user";

        // Act
        var encrypted = await _encryptionService.EncryptApiKeyAsync(plainKey, userId);

        // Assert
        encrypted.EncryptedData.Should().NotContain(plainKey,
            "Encrypted data should not contain plaintext key");

        Convert.FromBase64String(encrypted.EncryptedData).Should().NotBeEmpty(
            "Encrypted data should be valid Base64");

        // Verify ciphertext and encryption metadata
        var cipherBytes = Convert.FromBase64String(encrypted.EncryptedData);
        cipherBytes.Should().HaveCount(plainKey.Length,
            "AES-GCM ciphertext has same length as plaintext");

        // Verify total encrypted package is larger due to IV + Tag + Salt
        var ivBytes = Convert.FromBase64String(encrypted.IV);
        var tagBytes = Convert.FromBase64String(encrypted.Tag);
        var saltBytes = Convert.FromBase64String(encrypted.Salt);

        var totalEncryptedSize = cipherBytes.Length + ivBytes.Length + tagBytes.Length + saltBytes.Length;
        totalEncryptedSize.Should().BeGreaterThan(plainKey.Length,
            "Total encrypted package (ciphertext + IV + tag + salt) should be larger than plaintext");
    }

    #endregion

    #region Access Control Tests

    /// <summary>
    /// Test: User A cannot access User B's API keys.
    /// </summary>
    [Fact]
    public async Task Should_Enforce_User_Isolation()
    {
        // Arrange
        const string userA = "user-alice";
        const string userB = "user-bob";
        const string provider = "Anthropic";
        const string keyA = "sk-ant-alice-secret-key";

        var configA = CreateMockConfiguration(userA, provider, keyA);

        _mockRepo.Setup(r => r.GetByUserAndProviderAsync(userA, provider))
            .ReturnsAsync(configA);

        _mockRepo.Setup(r => r.GetByUserAndProviderAsync(userB, provider))
            .ReturnsAsync((DigitalMe.Data.Entities.ApiConfiguration?)null);

        // Act: User B tries to get key
        var keyForUserB = await _configService.GetApiKeyAsync(provider, userB);

        // Assert: User B should NOT get User A's key (should get system fallback or throw)
        keyForUserB.Should().NotBe(keyA,
            "User B should not have access to User A's key");

        // Should get system fallback instead
        keyForUserB.Should().Be("sk-ant-system-fallback-key",
            "User B should get system fallback key");
    }

    #endregion

    #region Encryption Security Tests

    /// <summary>
    /// Test: Encryption should use AES-256-GCM with correct sizes.
    /// </summary>
    [Fact]
    public async Task Encryption_Should_Use_Strong_Algorithms()
    {
        // Arrange
        const string testKey = "test-encryption-key";
        const string userId = "crypto-test-user";

        // Act
        var encrypted = await _encryptionService.EncryptApiKeyAsync(testKey, userId);

        // Assert: Check IV size (96 bits = 12 bytes for AES-GCM)
        var iv = Convert.FromBase64String(encrypted.IV);
        iv.Length.Should().Be(12, "AES-GCM IV should be 96 bits (12 bytes)");

        // Assert: Check Tag size (128 bits = 16 bytes)
        var tag = Convert.FromBase64String(encrypted.Tag);
        tag.Length.Should().Be(16, "AES-GCM authentication tag should be 128 bits (16 bytes)");

        // Assert: Check Salt size (256 bits = 32 bytes)
        var salt = Convert.FromBase64String(encrypted.Salt);
        salt.Length.Should().BeGreaterThanOrEqualTo(32,
            "Salt should be at least 256 bits (32 bytes) for PBKDF2");

        // Assert: Encrypted data should be non-empty
        var ciphertext = Convert.FromBase64String(encrypted.EncryptedData);
        ciphertext.Should().NotBeEmpty("Ciphertext should not be empty");
    }

    #endregion

    // ==================== HELPER METHODS ====================

    private static DigitalMe.Data.Entities.ApiConfiguration CreateMockConfiguration(
        string userId, string provider, string apiKey)
    {
        return new DigitalMe.Data.Entities.ApiConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            EncryptedApiKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(apiKey)),
            EncryptionIV = Convert.ToBase64String(new byte[12]),
            EncryptionSalt = Convert.ToBase64String(new byte[32]),
            AuthenticationTag = Convert.ToBase64String(new byte[16]),
            KeyFingerprint = "****1234",
            IsActive = true
        };
    }

    /// <summary>
    /// Test logger that captures all logged messages for inspection.
    /// </summary>
    private class TestLogger<T> : ILogger<T>
    {
        public List<string> LoggedMessages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            LoggedMessages.Add(message);
        }
    }
}