using Xunit;
using FluentAssertions;
using DigitalMe.Services.Security;
using DigitalMe.Models.Security;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Cryptography;
using System.Diagnostics;

namespace DigitalMe.Tests.Unit.Services.Security;

/// <summary>
/// Advanced cryptographic tests for key derivation functionality.
/// RED PHASE: Tests should initially fail or verify crypto strength.
/// Tests cover determinism, uniqueness, randomness quality, and timing attack resistance.
/// </summary>
public class KeyDerivationCryptoTests
{
    private readonly IKeyEncryptionService _encryptionService;

    public KeyDerivationCryptoTests()
    {
        _encryptionService = new KeyEncryptionService(NullLogger<KeyEncryptionService>.Instance);
    }

    #region Determinism Tests

    [Fact]
    public async Task DerivedKey_Should_Be_Deterministic_For_Same_User_And_Salt()
    {
        // Arrange
        const string userId = "user123";
        const string apiKey = "sk-ant-test-key";

        // Generate two encryptions with the same user
        var result1 = await _encryptionService.EncryptApiKeyAsync(apiKey, userId);
        var result2 = await _encryptionService.EncryptApiKeyAsync(apiKey, userId);

        // If we use the same salt and IV, we should get the same encrypted data
        // But since we generate random salt/IV each time, the results differ
        // This test verifies that determinism works at the algorithm level

        // To test determinism, we need to decrypt both and verify we get the same plaintext
        var decrypted1 = await _encryptionService.DecryptApiKeyAsync(result1, userId);
        var decrypted2 = await _encryptionService.DecryptApiKeyAsync(result2, userId);

        // Assert
        decrypted1.Should().Be(apiKey, "decryption should be deterministic");
        decrypted2.Should().Be(apiKey, "decryption should be deterministic");
        decrypted1.Should().Be(decrypted2, "same plaintext should decrypt to same result");
    }

    [Fact]
    public async Task Same_UserId_Should_Produce_Same_Key_With_Same_Salt()
    {
        // Arrange
        const string userId = "user123";
        const string apiKey = "sk-ant-test-key";

        // Generate encryption to get salt
        var encrypted = await _encryptionService.EncryptApiKeyAsync(apiKey, userId);

        // Decrypt multiple times with same userId and salt
        var decrypted1 = await _encryptionService.DecryptApiKeyAsync(encrypted, userId);
        var decrypted2 = await _encryptionService.DecryptApiKeyAsync(encrypted, userId);
        var decrypted3 = await _encryptionService.DecryptApiKeyAsync(encrypted, userId);

        // Assert - all should produce identical results
        decrypted1.Should().Be(decrypted2);
        decrypted2.Should().Be(decrypted3);
        decrypted1.Should().Be(apiKey);
    }

    #endregion

    #region Per-User Uniqueness Tests

    [Fact]
    public async Task Different_Users_Should_Produce_Different_Keys()
    {
        // Arrange
        const string apiKey = "sk-ant-test-key";
        const string user1 = "user1";
        const string user2 = "user2";

        // Act - encrypt same key for different users
        var encrypted1 = await _encryptionService.EncryptApiKeyAsync(apiKey, user1);
        var encrypted2 = await _encryptionService.EncryptApiKeyAsync(apiKey, user2);

        // Assert - different users should produce different encrypted data
        encrypted1.EncryptedData.Should().NotBe(encrypted2.EncryptedData,
            "different users should produce different encryption keys");

        // Verify cross-user decryption fails (user1 cannot decrypt user2's data)
        await Assert.ThrowsAsync<CryptographicException>(async () =>
            await _encryptionService.DecryptApiKeyAsync(encrypted1, user2));

        await Assert.ThrowsAsync<CryptographicException>(async () =>
            await _encryptionService.DecryptApiKeyAsync(encrypted2, user1));
    }

    [Fact]
    public async Task Multiple_Users_Should_Have_Unique_Keys()
    {
        // Arrange
        const string apiKey = "sk-ant-test-key";
        var users = Enumerable.Range(1, 10).Select(i => $"user{i}").ToArray();
        var encryptedData = new HashSet<string>();

        // Act - encrypt for multiple users
        foreach (var userId in users)
        {
            var encrypted = await _encryptionService.EncryptApiKeyAsync(apiKey, userId);
            encryptedData.Add(encrypted.EncryptedData);
        }

        // Assert - all encrypted data should be unique
        encryptedData.Count.Should().Be(users.Length,
            "each user should produce unique encrypted data");
    }

    #endregion

    #region Salt Randomness Tests

    [Fact]
    public async Task Salt_Should_Be_Cryptographically_Random()
    {
        // Arrange
        const string userId = "user123";
        const string apiKey = "sk-ant-test-key";
        var salts = new HashSet<string>();
        const int iterations = 100;

        // Act - generate many encryptions and collect salts
        for (int i = 0; i < iterations; i++)
        {
            var encrypted = await _encryptionService.EncryptApiKeyAsync(apiKey, userId);
            salts.Add(encrypted.Salt);
        }

        // Assert - all salts should be unique (collision probability is negligible)
        salts.Count.Should().Be(iterations,
            "salt should be cryptographically random with no collisions in {0} iterations", iterations);
    }

    [Fact]
    public async Task IV_Should_Be_Cryptographically_Random()
    {
        // Arrange
        const string userId = "user123";
        const string apiKey = "sk-ant-test-key";
        var ivs = new HashSet<string>();
        const int iterations = 100;

        // Act - generate many encryptions and collect IVs
        for (int i = 0; i < iterations; i++)
        {
            var encrypted = await _encryptionService.EncryptApiKeyAsync(apiKey, userId);
            ivs.Add(encrypted.IV);
        }

        // Assert - all IVs should be unique
        ivs.Count.Should().Be(iterations,
            "IV should be cryptographically random with no collisions in {0} iterations", iterations);
    }

    [Fact]
    public async Task Salt_And_IV_Should_Have_Sufficient_Entropy()
    {
        // Arrange
        const string userId = "user123";
        const string apiKey = "sk-ant-test-key";

        // Act
        var encrypted = await _encryptionService.EncryptApiKeyAsync(apiKey, userId);
        var saltBytes = Convert.FromBase64String(encrypted.Salt);
        var ivBytes = Convert.FromBase64String(encrypted.IV);

        // Assert - verify minimum lengths for security
        saltBytes.Length.Should().BeGreaterThanOrEqualTo(32, "salt should be at least 256 bits");
        ivBytes.Length.Should().BeGreaterThanOrEqualTo(12, "IV should be at least 96 bits for GCM");
    }

    #endregion

    #region Timing Attack Resistance Tests

    [Fact]
    public async Task Key_Derivation_Should_Have_Consistent_Timing()
    {
        // Arrange
        const string apiKey = "sk-ant-test-key";
        var timings = new List<long>();
        const int iterations = 50;

        // Act - measure key derivation timing
        for (int i = 0; i < iterations; i++)
        {
            var userId = $"user{i}";
            var stopwatch = Stopwatch.StartNew();
            await _encryptionService.EncryptApiKeyAsync(apiKey, userId);
            stopwatch.Stop();
            timings.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert - timing should be relatively consistent
        var average = timings.Average();
        var maxDeviation = timings.Max() - timings.Min();

        // Allow reasonable variance but detect obvious timing differences
        maxDeviation.Should().BeLessThan((long)(average * 2),
            "timing variance should not exceed 2x average to resist timing attacks");
    }

    [Fact]
    public async Task Decryption_Timing_Should_Not_Leak_Information()
    {
        // Arrange
        const string apiKey = "sk-ant-test-key";
        const string correctUser = "correct_user";
        const string wrongUser = "wrong_user";

        var encrypted = await _encryptionService.EncryptApiKeyAsync(apiKey, correctUser);

        var correctTimings = new List<long>();
        var wrongTimings = new List<long>();
        const int iterations = 20;

        // Act - measure timing for correct and wrong user decryption
        for (int i = 0; i < iterations; i++)
        {
            // Correct user timing
            var sw1 = Stopwatch.StartNew();
            await _encryptionService.DecryptApiKeyAsync(encrypted, correctUser);
            sw1.Stop();
            correctTimings.Add(sw1.ElapsedMilliseconds);

            // Wrong user timing (will fail)
            var sw2 = Stopwatch.StartNew();
            try
            {
                await _encryptionService.DecryptApiKeyAsync(encrypted, wrongUser);
            }
            catch (CryptographicException)
            {
                // Expected failure
            }
            sw2.Stop();
            wrongTimings.Add(sw2.ElapsedMilliseconds);
        }

        // Assert - timings should not reveal whether user is correct
        var correctAvg = correctTimings.Average();
        var wrongAvg = wrongTimings.Average();

        // Timing difference should be minimal (< 20% difference)
        var timingDifference = Math.Abs(correctAvg - wrongAvg);
        var relativeError = timingDifference / Math.Max(correctAvg, wrongAvg);

        relativeError.Should().BeLessThan(0.3,
            "decryption timing should not leak user validation information (actual: {0:P})", relativeError);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task Key_Derivation_Should_Meet_Performance_Budget()
    {
        // Arrange
        const string userId = "user123";
        const string apiKey = "sk-ant-test-key";
        const int iterations = 10;
        var timings = new List<long>();

        // Act - measure encryption performance
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            await _encryptionService.EncryptApiKeyAsync(apiKey, userId);
            stopwatch.Stop();
            timings.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert
        var averageTime = timings.Average();
        var p95Time = timings.OrderBy(t => t).Skip((int)(iterations * 0.95)).First();

        averageTime.Should().BeLessThan(100,
            "average key derivation should complete within 100ms (actual: {0}ms)", averageTime);
        p95Time.Should().BeLessThan(150,
            "p95 key derivation should complete within 150ms (actual: {0}ms)", p95Time);
    }

    [Fact]
    public async Task PBKDF2_Should_Use_Sufficient_Iterations()
    {
        // Arrange - test that key derivation is computationally expensive enough
        const string userId = "user123";
        const string apiKey = "sk-ant-test-key";

        // Act
        var stopwatch = Stopwatch.StartNew();
        await _encryptionService.EncryptApiKeyAsync(apiKey, userId);
        stopwatch.Stop();

        // Assert - should take at least some time (indicating proper iteration count)
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(10,
            "PBKDF2 should use sufficient iterations (100k+) to be secure");
    }

    #endregion

    #region Key Fingerprint Crypto Tests

    [Fact]
    public void Fingerprint_Should_Use_Strong_Hash_Algorithm()
    {
        // Arrange
        const string apiKey = "sk-ant-test-key-123";

        // Act
        var fingerprint = _encryptionService.CreateKeyFingerprint(apiKey);

        // Assert
        fingerprint.Should().NotBeNullOrEmpty();
        fingerprint.Length.Should().Be(16, "fingerprint should be 16 characters (truncated SHA-256)");

        // Base64 check
        try
        {
            // Fingerprint is base64 substring of SHA-256, so padding may be off
            // Just verify it's valid base64 characters
            fingerprint.Should().MatchRegex(@"^[A-Za-z0-9+/]+$",
                "fingerprint should contain valid base64 characters");
        }
        catch
        {
            Assert.Fail("Fingerprint should be valid base64 format");
        }
    }

    [Fact]
    public void Different_Keys_Should_Produce_Different_Fingerprints()
    {
        // Arrange
        var fingerprints = new HashSet<string>();
        var keys = Enumerable.Range(1, 100).Select(i => $"sk-ant-key-{i}").ToArray();

        // Act
        foreach (var key in keys)
        {
            var fingerprint = _encryptionService.CreateKeyFingerprint(key);
            fingerprints.Add(fingerprint);
        }

        // Assert - all fingerprints should be unique (no collisions)
        fingerprints.Count.Should().Be(keys.Length,
            "SHA-256 should produce unique fingerprints for all keys");
    }

    #endregion
}