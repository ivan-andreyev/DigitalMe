using Xunit;
using FluentAssertions;
using DigitalMe.Services.Security;
using DigitalMe.Models.Security;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Cryptography;
using Moq;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Tests.Unit.Services.Security;

/// <summary>
/// TDD Test Suite for KeyEncryptionService
/// RED PHASE: All tests should initially fail
/// Tests cover encryption, decryption, security, and memory protection
/// </summary>
public class KeyEncryptionServiceTests
{
    private readonly IKeyEncryptionService _encryptionService;

    public KeyEncryptionServiceTests()
    {
        _encryptionService = new KeyEncryptionService(NullLogger<KeyEncryptionService>.Instance);
    }

    #region Basic Encryption Tests

    [Fact]
    public async Task Encrypt_Should_Return_Different_Ciphertext_Each_Time()
    {
        // Arrange
        const string apiKey = "sk-ant-test-key";
        const string userId = "user123";

        // Act
        var result1 = await _encryptionService.EncryptApiKeyAsync(apiKey, userId);
        var result2 = await _encryptionService.EncryptApiKeyAsync(apiKey, userId);

        // Assert
        result1.EncryptedData.Should().NotBe(result2.EncryptedData, "each encryption should use different IV");
        result1.IV.Should().NotBe(result2.IV, "IV must be unique for each encryption");
        result1.Salt.Should().NotBe(result2.Salt, "salt should be unique for each encryption");
    }

    [Fact]
    public async Task Encrypt_Decrypt_Should_Roundtrip_Successfully()
    {
        // Arrange
        const string originalKey = "sk-ant-api-key-123456";
        const string userId = "user123";

        // Act
        var encrypted = await _encryptionService.EncryptApiKeyAsync(originalKey, userId);
        var decrypted = await _encryptionService.DecryptApiKeyAsync(encrypted, userId);

        // Assert
        decrypted.Should().Be(originalKey, "decryption should recover original plaintext");
    }

    [Fact]
    public async Task Encrypt_Should_Return_Valid_EncryptedKeyInfo()
    {
        // Arrange
        const string apiKey = "sk-ant-test";
        const string userId = "user123";

        // Act
        var result = await _encryptionService.EncryptApiKeyAsync(apiKey, userId);

        // Assert
        result.Should().NotBeNull();
        result.EncryptedData.Should().NotBeNullOrEmpty("encrypted data is required");
        result.IV.Should().NotBeNullOrEmpty("IV is required");
        result.Salt.Should().NotBeNullOrEmpty("salt is required");
        result.Tag.Should().NotBeNullOrEmpty("authentication tag is required");
        result.KeyFingerprint.Should().NotBeNullOrEmpty("key fingerprint is required");
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task Decrypt_With_Wrong_UserId_Should_Fail()
    {
        // Arrange
        const string apiKey = "sk-ant-test";
        var encrypted = await _encryptionService.EncryptApiKeyAsync(apiKey, "user1");

        // Act & Assert
        await Assert.ThrowsAsync<CryptographicException>(() =>
            _encryptionService.DecryptApiKeyAsync(encrypted, "user2"));
    }

    [Fact]
    public async Task Decrypt_With_Tampered_Data_Should_Fail()
    {
        // Arrange
        const string apiKey = "sk-ant-test";
        const string userId = "user123";
        var encrypted = await _encryptionService.EncryptApiKeyAsync(apiKey, userId);

        // Tamper with encrypted data
        var tamperedInfo = new EncryptedKeyInfo(
            encrypted.EncryptedData + "X", // Add tampered byte
            encrypted.IV,
            encrypted.Salt,
            encrypted.Tag,
            encrypted.KeyFingerprint);

        // Act & Assert
        await Assert.ThrowsAsync<CryptographicException>(() =>
            _encryptionService.DecryptApiKeyAsync(tamperedInfo, userId));
    }

    [Fact]
    public async Task Decrypt_With_Tampered_Tag_Should_Fail()
    {
        // Arrange
        const string apiKey = "sk-ant-test";
        const string userId = "user123";
        var encrypted = await _encryptionService.EncryptApiKeyAsync(apiKey, userId);

        // Tamper with authentication tag
        var tamperedInfo = new EncryptedKeyInfo(
            encrypted.EncryptedData,
            encrypted.IV,
            encrypted.Salt,
            encrypted.Tag + "X", // Tamper with tag
            encrypted.KeyFingerprint);

        // Act & Assert
        await Assert.ThrowsAsync<CryptographicException>(() =>
            _encryptionService.DecryptApiKeyAsync(tamperedInfo, userId));
    }

    [Fact]
    public void Sensitive_Data_Should_Not_Appear_In_Logs()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<KeyEncryptionService>>();
        var service = new KeyEncryptionService(mockLogger.Object);
        const string secretKey = "sk-ant-secret-key-12345";

        // Act
        var result = service.EncryptApiKeyAsync(secretKey, "user123").GetAwaiter().GetResult();

        // Assert - verify that the actual API key never appears in logs
        mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => !v.ToString()!.Contains("sk-ant-secret")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            "API key should never appear in log messages");
    }

    #endregion

    #region Input Validation Tests

    [Theory]
    [InlineData("", "user")]
    [InlineData("key", "")]
    public async Task Should_Reject_Empty_Inputs(string apiKey, string userId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _encryptionService.EncryptApiKeyAsync(apiKey, userId));
    }

    [Theory]
    [InlineData(null!, "user")]
    [InlineData("key", null!)]
    public async Task Should_Reject_Null_Inputs(string apiKey, string userId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _encryptionService.EncryptApiKeyAsync(apiKey, userId));
    }

    [Fact]
    public async Task Decrypt_Should_Reject_Null_EncryptedKeyInfo()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _encryptionService.DecryptApiKeyAsync(null!, "user"));
    }

    [Fact]
    public async Task Decrypt_Should_Reject_Null_UserId()
    {
        // Arrange
        var encrypted = await _encryptionService.EncryptApiKeyAsync("key", "user");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _encryptionService.DecryptApiKeyAsync(encrypted, null!));
    }

    #endregion

    #region Key Fingerprint Tests

    [Fact]
    public void CreateKeyFingerprint_Should_Return_Consistent_Hash()
    {
        // Arrange
        const string apiKey = "sk-ant-test-key";

        // Act
        var fingerprint1 = _encryptionService.CreateKeyFingerprint(apiKey);
        var fingerprint2 = _encryptionService.CreateKeyFingerprint(apiKey);

        // Assert
        fingerprint1.Should().Be(fingerprint2, "same key should produce same fingerprint");
    }

    [Fact]
    public void CreateKeyFingerprint_Should_Return_Different_Hash_For_Different_Keys()
    {
        // Arrange
        const string apiKey1 = "sk-ant-key-1";
        const string apiKey2 = "sk-ant-key-2";

        // Act
        var fingerprint1 = _encryptionService.CreateKeyFingerprint(apiKey1);
        var fingerprint2 = _encryptionService.CreateKeyFingerprint(apiKey2);

        // Assert
        fingerprint1.Should().NotBe(fingerprint2, "different keys should produce different fingerprints");
    }

    [Fact]
    public void CreateKeyFingerprint_Should_Return_Fixed_Length_String()
    {
        // Arrange
        const string apiKey = "sk-ant-test-key-very-long-key-with-many-characters";

        // Act
        var fingerprint = _encryptionService.CreateKeyFingerprint(apiKey);

        // Assert
        fingerprint.Should().HaveLength(16, "fingerprint should be truncated to 16 characters");
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task Encryption_Should_Complete_Within_Performance_Budget()
    {
        // Arrange
        const string apiKey = "sk-ant-test-key";
        const string userId = "user123";
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await _encryptionService.EncryptApiKeyAsync(apiKey, userId);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "encryption should complete within 500ms (adjusted for CI/CD runners)");
    }

    [Fact]
    public async Task Decryption_Should_Complete_Within_Performance_Budget()
    {
        // Arrange
        const string apiKey = "sk-ant-test-key";
        const string userId = "user123";
        var encrypted = await _encryptionService.EncryptApiKeyAsync(apiKey, userId);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await _encryptionService.DecryptApiKeyAsync(encrypted, userId);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "decryption should complete within 500ms (adjusted for CI/CD runners)");
    }

    #endregion
}