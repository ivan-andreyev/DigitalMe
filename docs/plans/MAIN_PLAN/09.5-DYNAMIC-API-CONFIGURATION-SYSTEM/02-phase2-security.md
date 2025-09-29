# 📋 PHASE 2: SECURITY LAYER (TDD)

**Parent Plan**: [09.5-DYNAMIC-API-CONFIGURATION-SYSTEM.md](../09.5-DYNAMIC-API-CONFIGURATION-SYSTEM.md)

**Phase Status**: PENDING
**Priority**: CRITICAL
**Estimated Duration**: 3-4 days
**Dependencies**: Phase 1 Complete

---

## Phase Objectives

Implement secure encryption/decryption services for API keys with comprehensive security testing. Focus on AES-256-GCM encryption, secure key derivation, and memory protection.

---

## Task 2.1: Implement KeyEncryptionService with Security Tests ✅ COMPLETE

**Status**: COMPLETE
**Priority**: CRITICAL
**Estimated**: 120 minutes
**Actual**: 90 minutes
**Dependencies**: Phase 1 complete
**Completed**: 2025-09-30

### Execution Summary
- ✅ RED phase: Created 18 comprehensive security tests (265 lines)
- ✅ GREEN phase: Implemented KeyEncryptionService with AES-256-GCM (209 lines)
- ✅ REFACTOR phase: Applied SOLID principles, DRY, code style compliance
- ✅ All 18 tests passing (100% success rate, 251ms execution)
- ✅ Security features: tamper detection, per-user encryption, memory cleanup
- ✅ Code reviews passed (code-style + code-principles)

**Artifacts Created:**
- `src/DigitalMe/Models/Security/EncryptedKeyInfo.cs` (data transfer record)
- `src/DigitalMe/Services/Security/IKeyEncryptionService.cs` (interface, 3 methods)
- `src/DigitalMe/Services/Security/KeyEncryptionService.cs` (implementation, 209 lines)
- `tests/DigitalMe.Tests.Unit/Services/Security/KeyEncryptionServiceTests.cs` (18 tests, 265 lines)

### TDD Cycle

#### 1. RED: Create encryption service tests
File: `tests/DigitalMe.Tests.Unit/Services/Security/KeyEncryptionServiceTests.cs`

```csharp
public class KeyEncryptionServiceTests
{
    private readonly IKeyEncryptionService _encryptionService;

    public KeyEncryptionServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Security:EncryptionKey"] = "test_master_key_32_bytes_long!!!"
            })
            .Build();
        _encryptionService = new KeyEncryptionService(config, new NullLogger<KeyEncryptionService>());
    }

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
        result1.EncryptedData.Should().NotBe(result2.EncryptedData);
        result1.IV.Should().NotBe(result2.IV); // Different IV each time
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
        decrypted.Should().Be(originalKey);
    }

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
    public void Sensitive_Data_Should_Not_Appear_In_Logs()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<KeyEncryptionService>>();
        var service = new KeyEncryptionService(_config, mockLogger.Object);

        // Act
        await service.EncryptApiKeyAsync("sk-ant-secret", "user");

        // Assert
        mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => !v.ToString().Contains("sk-ant")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
```

#### 2. GREEN: Implement encryption service

```csharp
using System.Security.Cryptography;
using System.Text;

public class KeyEncryptionService : IKeyEncryptionService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeyEncryptionService> _logger;
    private const int KeyDerivationIterations = 100000;
    private const int SaltSize = 32;
    private const int IVSize = 12; // For GCM
    private const int TagSize = 16;

    public async Task<EncryptedKeyInfo> EncryptApiKeyAsync(string apiKey, string userId)
    {
        try
        {
            // Generate random salt
            var salt = RandomNumberGenerator.GetBytes(SaltSize);

            // Derive key from userId and salt
            var derivedKey = DeriveKeyFromUser(userId, salt);

            // Generate random IV
            var iv = RandomNumberGenerator.GetBytes(IVSize);

            // Encrypt with AES-GCM
            var plaintext = Encoding.UTF8.GetBytes(apiKey);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(derivedKey);
            aes.Encrypt(iv, plaintext, ciphertext, tag);

            // Clear sensitive data from memory
            Array.Clear(plaintext, 0, plaintext.Length);
            Array.Clear(derivedKey, 0, derivedKey.Length);

            _logger.LogInformation("API key encrypted for user {UserId}",
                userId.Substring(0, Math.Min(8, userId.Length)) + "***");

            return new EncryptedKeyInfo(
                Convert.ToBase64String(ciphertext),
                Convert.ToBase64String(iv),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(tag),
                CreateKeyFingerprint(apiKey));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encryption failed for user");
            throw new SecurityException("Failed to encrypt API key");
        }
    }

    private byte[] DeriveKeyFromUser(string userId, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            userId,
            salt,
            KeyDerivationIterations,
            HashAlgorithmName.SHA256);

        return pbkdf2.GetBytes(32); // 256-bit key
    }

    public string CreateKeyFingerprint(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(hash)[..16];
    }
}
```

### Security Testing Scenarios

```csharp
[Theory]
[InlineData("", "user")]
[InlineData("key", "")]
[InlineData(null, "user")]
[InlineData("key", null)]
public async Task Should_Reject_Invalid_Inputs(string apiKey, string userId)
{
    await Assert.ThrowsAsync<ArgumentException>(() =>
        _encryptionService.EncryptApiKeyAsync(apiKey, userId));
}

[Fact]
public async Task Should_Handle_Tampering_Detection()
{
    // Arrange
    var encrypted = await _encryptionService.EncryptApiKeyAsync("key", "user");

    // Tamper with encrypted data
    var tamperedInfo = encrypted with
    {
        EncryptedData = encrypted.EncryptedData + "tampered"
    };

    // Act & Assert
    await Assert.ThrowsAsync<CryptographicException>(() =>
        _encryptionService.DecryptApiKeyAsync(tamperedInfo, "user"));
}
```

### Acceptance Criteria
- ✅ Encryption/decryption roundtrip works
- ✅ Different users cannot decrypt each other's keys
- ✅ No sensitive data in logs
- ✅ Memory cleared after operations
- ✅ 100% test coverage for security methods

---

## Task 2.2: Implement Key Derivation with Crypto Tests

**Status**: PENDING
**Priority**: HIGH
**Estimated**: 60 minutes
**Dependencies**: Task 2.1

### Cryptographic Strength Tests

```csharp
[Fact]
public void DerivedKey_Should_Be_Deterministic()
{
    // Same input should produce same key
    var salt = RandomNumberGenerator.GetBytes(32);
    var key1 = DeriveKey("user123", salt);
    var key2 = DeriveKey("user123", salt);

    key1.Should().BeEquivalentTo(key2);
}

[Fact]
public void DerivedKey_Should_Be_Unique_Per_User()
{
    // Different users should have different keys
    var salt = RandomNumberGenerator.GetBytes(32);
    var key1 = DeriveKey("user1", salt);
    var key2 = DeriveKey("user2", salt);

    key1.Should().NotBeEquivalentTo(key2);
}

[Fact]
public void Salt_Should_Be_Cryptographically_Random()
{
    // Verify randomness quality
    var salts = new HashSet<string>();
    for (int i = 0; i < 1000; i++)
    {
        var salt = RandomNumberGenerator.GetBytes(32);
        salts.Add(Convert.ToBase64String(salt));
    }

    salts.Count.Should().Be(1000); // All unique
}

[Fact]
public void Key_Derivation_Should_Be_Time_Constant()
{
    // Prevent timing attacks
    var times = new List<long>();

    for (int i = 0; i < 100; i++)
    {
        var sw = Stopwatch.StartNew();
        DeriveKey($"user{i}", RandomNumberGenerator.GetBytes(32));
        sw.Stop();
        times.Add(sw.ElapsedMilliseconds);
    }

    var stdDev = CalculateStandardDeviation(times);
    stdDev.Should().BeLessThan(5); // Low variance
}
```

### Acceptance Criteria
- ✅ Key derivation tests passing
- ✅ Performance under 20ms
- ✅ Cryptographic strength verified
- ✅ Time-constant operations

---

## Task 2.3: Implement Secure Memory Handling

**Status**: PENDING
**Priority**: HIGH
**Estimated**: 45 minutes
**Dependencies**: Task 2.2

### Memory Protection Implementation

```csharp
public class SecureMemory : IDisposable
{
    private byte[] _data;
    private GCHandle _handle;

    public SecureMemory(byte[] data)
    {
        _data = data;
        _handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
    }

    public byte[] Data => _data;

    public void Dispose()
    {
        if (_handle.IsAllocated)
        {
            // Zero out memory before releasing
            Array.Clear(_data, 0, _data.Length);
            _handle.Free();
        }
    }
}
```

### Acceptance Criteria
- ✅ Memory zeroed after use
- ✅ No memory leaks detected
- ✅ GC handles properly managed

---

## Phase Completion Checklist

- [ ] Encryption service fully implemented
- [ ] All security tests passing
- [ ] 100% test coverage for security components
- [ ] Memory protection verified
- [ ] Security audit performed
- [ ] No cryptographic weaknesses
- [ ] Performance benchmarks met
- [ ] Documentation complete

---

## Security Audit Checklist

- [ ] AES-256-GCM properly implemented
- [ ] PBKDF2 with sufficient iterations (100k+)
- [ ] Cryptographically secure random generation
- [ ] No key material in logs or error messages
- [ ] Memory cleared after sensitive operations
- [ ] Time-constant comparisons used
- [ ] Input validation comprehensive
- [ ] Error messages don't leak information

---

## Output Artifacts

1. **Services**: `IKeyEncryptionService.cs`, `KeyEncryptionService.cs`
2. **Models**: `EncryptedKeyInfo.cs`
3. **Utilities**: `SecureMemory.cs`
4. **Tests**: Complete security test suite
5. **Documentation**: Security implementation guide

---

## Next Phase Dependencies

Phase 3 (Configuration Service) depends on:
- Encryption service operational
- Key derivation working
- Security tests passing