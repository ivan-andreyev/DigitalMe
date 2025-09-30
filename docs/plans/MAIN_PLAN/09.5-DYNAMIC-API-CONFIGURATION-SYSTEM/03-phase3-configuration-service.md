# 📋 PHASE 3: CONFIGURATION SERVICE (TDD)

**Parent Plan**: [09.5-DYNAMIC-API-CONFIGURATION-SYSTEM.md](../09.5-DYNAMIC-API-CONFIGURATION-SYSTEM.md)

**Phase Status**: PENDING
**Priority**: CRITICAL
**Estimated Duration**: 2-3 days
**Dependencies**: Phase 2 Complete

---

## Phase Objectives

Implement the core ApiConfigurationService that manages dynamic API key resolution, fallback mechanisms, and key validation. This service orchestrates between user keys and system keys with proper error handling.

---

## Task 3.1: Implement ApiConfigurationService with Resolution Tests ✅ COMPLETE

**Status**: COMPLETE
**Priority**: CRITICAL
**Estimated**: 90 minutes
**Actual**: 75 minutes
**Dependencies**: Phase 2 complete
**Completed**: 2025-09-30

### Execution Summary
- ✅ RED phase: Created 20 comprehensive resolution tests (363 lines)
- ✅ GREEN phase: Implemented GetApiKeyAsync + SetUserApiKeyAsync methods
- ✅ REFACTOR phase: Applied DRY refactoring, extracted UpdateConfigurationWithEncryptedKey
- ✅ All 20 tests passing (100% success rate, 37ms execution)
- ✅ Key features: user key resolution, system key fallback, encryption integration
- ✅ Code reviews passed (style: excellent, principles: medium with improvements applied)

**Artifacts Created:**
- `tests/DigitalMe.Tests.Unit/Services/ApiConfigurationServiceResolutionTests.cs` (20 tests, 363 lines)
- `src/DigitalMe/Services/ApiConfigurationService.cs` - added GetApiKeyAsync & SetUserApiKeyAsync methods
- `src/DigitalMe/Services/IApiConfigurationService.cs` - added interface methods with XML docs
- `src/DigitalMe/Data/Entities/ApiConfiguration.cs` - added AuthenticationTag property

### TDD Cycle

#### 1. RED: Create configuration service tests
File: `tests/DigitalMe.Tests.Unit/Services/Configuration/ApiConfigurationServiceTests.cs`

```csharp
public class ApiConfigurationServiceTests
{
    private readonly Mock<IApiConfigurationRepository> _mockRepo;
    private readonly Mock<IKeyEncryptionService> _mockEncryption;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IApiKeyValidator> _mockValidator;
    private readonly ApiConfigurationService _service;

    public ApiConfigurationServiceTests()
    {
        _mockRepo = new Mock<IApiConfigurationRepository>();
        _mockEncryption = new Mock<IKeyEncryptionService>();
        _mockConfig = new Mock<IConfiguration>();
        _mockValidator = new Mock<IApiKeyValidator>();

        _service = new ApiConfigurationService(
            _mockRepo.Object,
            _mockEncryption.Object,
            _mockConfig.Object,
            _mockValidator.Object,
            new NullLogger<ApiConfigurationService>());
    }

    [Fact]
    public async Task GetApiKey_Should_Return_User_Key_When_Available()
    {
        // Arrange
        var userConfig = new ApiConfiguration
        {
            UserId = "user123",
            Provider = "Anthropic",
            EncryptedApiKey = "encrypted",
            EncryptionIV = "iv",
            EncryptionSalt = "salt",
            AuthTag = "tag",
            KeyFingerprint = "fingerprint",
            IsActive = true
        };

        _mockRepo.Setup(r => r.GetUserConfigurationAsync("user123", "Anthropic"))
            .ReturnsAsync(userConfig);

        _mockEncryption.Setup(e => e.DecryptApiKeyAsync(It.IsAny<EncryptedKeyInfo>(), "user123"))
            .ReturnsAsync("sk-ant-user-key");

        // Act
        var result = await _service.GetApiKeyAsync("Anthropic", "user123");

        // Assert
        result.Should().Be("sk-ant-user-key");
        result.Should().NotContain("system");
    }

    [Fact]
    public async Task GetApiKey_Should_Fallback_To_System_Key()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetUserConfigurationAsync("user123", "Anthropic"))
            .ReturnsAsync((ApiConfiguration?)null);

        _mockConfig.Setup(c => c["DigitalMe:ApiConfiguration:SystemKeys:Anthropic:Key"])
            .Returns("sk-ant-system-key");

        // Act
        var result = await _service.GetApiKeyAsync("Anthropic", "user123");

        // Assert
        result.Should().Be("sk-ant-system-key");
    }

    [Fact]
    public async Task SetUserApiKey_Should_Encrypt_And_Store()
    {
        // Arrange
        const string newKey = "sk-ant-new-user-key";
        var encryptedInfo = new EncryptedKeyInfo(
            "encrypted", "iv", "salt", "tag", "fingerprint");

        _mockEncryption.Setup(e => e.EncryptApiKeyAsync(newKey, "user123"))
            .ReturnsAsync(encryptedInfo);

        // Act
        await _service.SetUserApiKeyAsync("user123", "Anthropic", newKey);

        // Assert
        _mockRepo.Verify(r => r.SaveConfigurationAsync(
            It.Is<ApiConfiguration>(c =>
                c.UserId == "user123" &&
                c.Provider == "Anthropic" &&
                c.EncryptedApiKey == "encrypted")),
            Times.Once);
    }

    [Fact]
    public async Task TestConnection_Should_Validate_Key()
    {
        // Arrange
        _mockValidator.Setup(v => v.ValidateAnthropicKeyAsync("sk-ant-test"))
            .ReturnsAsync(new ValidationResult { IsValid = true });

        // Act
        var result = await _service.TestConnectionAsync("Anthropic", "sk-ant-test");

        // Assert
        result.Should().BeTrue();
    }
}
```

#### 2. GREEN: Implement configuration service

```csharp
public class ApiConfigurationService : IApiConfigurationService
{
    private readonly IApiConfigurationRepository _repository;
    private readonly IKeyEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;
    private readonly IApiKeyValidator _validator;
    private readonly ILogger<ApiConfigurationService> _logger;

    public async Task<string?> GetApiKeyAsync(string provider, string? userId = null)
    {
        try
        {
            // Try to get user-specific key first
            if (!string.IsNullOrEmpty(userId))
            {
                var userConfig = await _repository.GetUserConfigurationAsync(userId, provider);
                if (userConfig != null && userConfig.IsActive)
                {
                    var encryptedInfo = new EncryptedKeyInfo(
                        userConfig.EncryptedApiKey,
                        userConfig.EncryptionIV,
                        userConfig.EncryptionSalt,
                        userConfig.AuthTag,
                        userConfig.KeyFingerprint);

                    var decrypted = await _encryptionService.DecryptApiKeyAsync(encryptedInfo, userId);

                    // Update last used timestamp
                    await _repository.UpdateLastUsedAsync(userConfig.Id);

                    _logger.LogInformation("Using user API key for {Provider}", provider);
                    return decrypted;
                }
            }

            // Fallback to system key
            var systemKey = _configuration[$"DigitalMe:ApiConfiguration:SystemKeys:{provider}:Key"];
            if (!string.IsNullOrEmpty(systemKey))
            {
                _logger.LogInformation("Using system API key for {Provider}", provider);
                return systemKey;
            }

            _logger.LogWarning("No API key found for {Provider}", provider);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get API key for {Provider}", provider);
            // Fallback to system key on error
            return _configuration[$"DigitalMe:ApiConfiguration:SystemKeys:{provider}:Key"];
        }
    }

    public async Task SetUserApiKeyAsync(string userId, string provider, string apiKey)
    {
        // Validate key format first
        if (!IsValidKeyFormat(provider, apiKey))
        {
            throw new ArgumentException($"Invalid API key format for {provider}");
        }

        // Encrypt the key
        var encryptedInfo = await _encryptionService.EncryptApiKeyAsync(apiKey, userId);

        // Check if configuration already exists
        var existing = await _repository.GetUserConfigurationAsync(userId, provider);

        if (existing != null)
        {
            // Update existing
            existing.EncryptedApiKey = encryptedInfo.EncryptedData;
            existing.EncryptionIV = encryptedInfo.IV;
            existing.EncryptionSalt = encryptedInfo.Salt;
            existing.AuthTag = encryptedInfo.AuthTag;
            existing.KeyFingerprint = encryptedInfo.Fingerprint;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.ValidationStatus = "Pending";

            await _repository.SaveConfigurationAsync(existing);
        }
        else
        {
            // Create new
            var config = new ApiConfiguration
            {
                UserId = userId,
                Provider = provider,
                EncryptedApiKey = encryptedInfo.EncryptedData,
                EncryptionIV = encryptedInfo.IV,
                EncryptionSalt = encryptedInfo.Salt,
                AuthTag = encryptedInfo.AuthTag,
                KeyFingerprint = encryptedInfo.Fingerprint,
                IsActive = true,
                ValidationStatus = "Pending"
            };

            await _repository.SaveConfigurationAsync(config);
        }

        _logger.LogInformation("API key updated for user {UserId} provider {Provider}",
            userId.Substring(0, 8) + "***", provider);
    }

    private bool IsValidKeyFormat(string provider, string apiKey)
    {
        return provider switch
        {
            "Anthropic" => apiKey.StartsWith("sk-ant-"),
            "OpenAI" => apiKey.StartsWith("sk-"),
            _ => !string.IsNullOrWhiteSpace(apiKey)
        };
    }

    public async Task<bool> TestConnectionAsync(string provider, string apiKey)
    {
        var result = provider switch
        {
            "Anthropic" => await _validator.ValidateAnthropicKeyAsync(apiKey),
            "OpenAI" => await _validator.ValidateOpenAIKeyAsync(apiKey),
            _ => new ValidationResult { IsValid = false, Message = "Unknown provider" }
        };

        return result.IsValid;
    }
}
```

### Acceptance Criteria
- ✅ User key resolution working
- ✅ System key fallback working
- ✅ Key validation integrated
- ✅ Error handling comprehensive
- ✅ 90%+ test coverage

---

## Task 3.2: Implement Fallback Mechanism Tests

**Status**: PENDING
**Priority**: HIGH
**Estimated**: 45 minutes
**Dependencies**: Task 3.1

### Advanced Fallback Scenarios

```csharp
[Fact]
public async Task Should_Use_System_Key_When_User_Key_Fails_Validation()
{
    // Arrange
    var userConfig = new ApiConfiguration { /* ... */ };
    _mockRepo.Setup(r => r.GetUserConfigurationAsync("user", "Anthropic"))
        .ReturnsAsync(userConfig);

    _mockEncryption.Setup(e => e.DecryptApiKeyAsync(It.IsAny<EncryptedKeyInfo>(), "user"))
        .ReturnsAsync("invalid-key");

    _mockValidator.Setup(v => v.ValidateAnthropicKeyAsync("invalid-key"))
        .ReturnsAsync(new ValidationResult { IsValid = false });

    _mockConfig.Setup(c => c["DigitalMe:ApiConfiguration:SystemKeys:Anthropic:Key"])
        .Returns("sk-ant-system-key");

    // Act
    var result = await _service.GetApiKeyAsync("Anthropic", "user", validateKey: true);

    // Assert
    result.Should().Be("sk-ant-system-key");
}

[Fact]
public async Task Should_Track_Fallback_Reason()
{
    // Arrange
    _mockRepo.Setup(r => r.GetUserConfigurationAsync("user", "Anthropic"))
        .ThrowsAsync(new Exception("DB error"));

    _mockConfig.Setup(c => c["DigitalMe:ApiConfiguration:SystemKeys:Anthropic:Key"])
        .Returns("sk-ant-system-key");

    // Act
    var result = await _service.GetApiKeyWithReasonAsync("Anthropic", "user");

    // Assert
    result.Key.Should().Be("sk-ant-system-key");
    result.FallbackReason.Should().Be("UserKeyRetrievalFailed");
}

[Fact]
public async Task Should_Cache_Validation_Results()
{
    // Arrange
    _mockValidator.Setup(v => v.ValidateAnthropicKeyAsync("sk-ant-test"))
        .ReturnsAsync(new ValidationResult { IsValid = true });

    // Act - call twice
    await _service.TestConnectionAsync("Anthropic", "sk-ant-test");
    await _service.TestConnectionAsync("Anthropic", "sk-ant-test");

    // Assert - validator should only be called once due to caching
    _mockValidator.Verify(v => v.ValidateAnthropicKeyAsync("sk-ant-test"), Times.Once);
}
```

### Circuit Breaker Implementation

```csharp
public class CircuitBreakerPolicy
{
    private int _failureCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private const int FailureThreshold = 3;
    private const int ResetTimeSeconds = 60;

    public bool IsOpen =>
        _failureCount >= FailureThreshold &&
        (DateTime.UtcNow - _lastFailureTime).TotalSeconds < ResetTimeSeconds;

    public void RecordFailure()
    {
        _failureCount++;
        _lastFailureTime = DateTime.UtcNow;
    }

    public void RecordSuccess()
    {
        _failureCount = 0;
    }
}
```

### Acceptance Criteria
- ✅ All fallback scenarios tested
- ✅ Fallback reasons logged
- ✅ Performance impact minimal
- ✅ Circuit breaker pattern working

---

## Task 3.3: Implement Key Rotation Support

**Status**: COMPLETE ✅
**Priority**: MEDIUM
**Estimated**: 60 minutes
**Actual**: 65 minutes
**Dependencies**: Task 3.2
**Tests**: 18/18 passing (51ms)
**Validation**: 95% confidence

### Key Rotation Tests

```csharp
[Fact]
public async Task Should_Support_Key_Rotation()
{
    // Arrange
    var oldKey = "sk-ant-old-key";
    var newKey = "sk-ant-new-key";

    await _service.SetUserApiKeyAsync("user", "Anthropic", oldKey);

    // Act
    await _service.RotateApiKeyAsync("user", "Anthropic", newKey);

    // Assert
    var currentKey = await _service.GetApiKeyAsync("Anthropic", "user");
    currentKey.Should().Be(newKey);

    // Old key should be deactivated
    _mockRepo.Verify(r => r.DeactivateConfigurationAsync("user", "Anthropic"), Times.Once);
}

[Fact]
public async Task Should_Keep_Key_History()
{
    // Test that old keys are kept but marked as inactive
    var history = await _service.GetKeyHistoryAsync("user", "Anthropic");
    history.Should().HaveCountGreaterThan(0);
    history.Where(k => k.IsActive).Should().HaveCount(1);
}
```

### Acceptance Criteria
- ✅ Key rotation working
- ✅ History maintained
- ✅ Old keys deactivated
- ✅ Audit trail created

---

## Phase Completion Checklist

- [ ] Configuration service fully implemented
- [ ] All resolution scenarios tested
- [ ] Fallback mechanisms working
- [ ] Key validation integrated
- [ ] Circuit breaker implemented
- [ ] Caching strategy working
- [ ] 90%+ test coverage
- [ ] Documentation complete

---

## Output Artifacts

1. **Services**: `IApiConfigurationService.cs`, `ApiConfigurationService.cs`
2. **Validators**: `IApiKeyValidator.cs`, `ApiKeyValidator.cs`
3. **Policies**: `CircuitBreakerPolicy.cs`, `CachePolicy.cs`
4. **Models**: `ValidationResult.cs`, `KeyResolutionResult.cs`
5. **Tests**: Complete test suite with fallback scenarios

---

## Next Phase Dependencies

Phase 4 (Usage Tracking) depends on:
- Configuration service operational
- Key resolution working
- Validation integrated