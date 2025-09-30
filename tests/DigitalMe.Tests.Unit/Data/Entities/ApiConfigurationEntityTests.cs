using DigitalMe.Data.Entities;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace DigitalMe.Tests.Unit.Data.Entities;

/// <summary>
/// TDD tests for ApiConfiguration entity.
/// These tests define the entity requirements before implementation.
/// </summary>
public class ApiConfigurationEntityTests
{
    [Fact]
    public void ApiConfiguration_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var config = new ApiConfiguration
        {
            UserId = "user123",
            Provider = "Anthropic",
            EncryptedApiKey = "encrypted_key_data",
            EncryptionIV = "iv_data",
            EncryptionSalt = "salt_data",
            KeyFingerprint = "fingerprint",
            IsActive = true
        };

        // Assert
        config.UserId.Should().Be("user123");
        config.Provider.Should().Be("Anthropic");
        config.EncryptedApiKey.Should().NotBeNullOrWhiteSpace();
        config.EncryptionIV.Should().NotBeNullOrWhiteSpace();
        config.EncryptionSalt.Should().NotBeNullOrWhiteSpace();
        config.KeyFingerprint.Should().NotBeNullOrWhiteSpace();
        config.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "Provider", "key", "iv", "salt", "fingerprint")]
    [InlineData("user", null, "key", "iv", "salt", "fingerprint")]
    [InlineData("user", "Provider", null, "iv", "salt", "fingerprint")]
    [InlineData("user", "Provider", "key", null, "salt", "fingerprint")]
    [InlineData("user", "Provider", "key", "iv", null, "fingerprint")]
    [InlineData("user", "Provider", "key", "iv", "salt", null)]
    public void ApiConfiguration_Should_Require_Mandatory_Fields(
        string userId,
        string provider,
        string encryptedKey,
        string encryptionIV,
        string encryptionSalt,
        string keyFingerprint)
    {
        // Arrange
        var config = new ApiConfiguration
        {
            UserId = userId!,
            Provider = provider!,
            EncryptedApiKey = encryptedKey!,
            EncryptionIV = encryptionIV!,
            EncryptionSalt = encryptionSalt!,
            KeyFingerprint = keyFingerprint!
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(config);
        var isValid = Validator.TryValidateObject(config, context, validationResults, true);

        // Assert
        isValid.Should().BeFalse("entity should fail validation when required fields are null");
        validationResults.Should().NotBeEmpty("validation errors should be reported");
    }

    [Fact]
    public void ApiConfiguration_Should_Inherit_From_BaseEntity()
    {
        // Arrange & Act
        var config = new ApiConfiguration();

        // Assert
        config.Should().BeAssignableTo<BaseEntity>("ApiConfiguration must inherit from BaseEntity");
        config.Id.Should().NotBeEmpty("BaseEntity provides Id");
        config.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5), "BaseEntity provides CreatedAt");
        config.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5), "BaseEntity provides UpdatedAt");
    }

    [Fact]
    public void ApiConfiguration_Should_Default_IsActive_To_True()
    {
        // Arrange & Act
        var config = new ApiConfiguration
        {
            UserId = "user",
            Provider = "Anthropic",
            EncryptedApiKey = "key",
            EncryptionIV = "iv",
            EncryptionSalt = "salt",
            KeyFingerprint = "fingerprint"
        };

        // Assert
        config.IsActive.Should().BeTrue("IsActive should default to true for new configurations");
    }

    [Fact]
    public void ApiConfiguration_Should_Default_ValidationStatus_To_Unknown()
    {
        // Arrange & Act
        var config = new ApiConfiguration
        {
            UserId = "user",
            Provider = "Anthropic",
            EncryptedApiKey = "key",
            EncryptionIV = "iv",
            EncryptionSalt = "salt",
            KeyFingerprint = "fingerprint"
        };

        // Assert
        config.ValidationStatus.Should().Be(ApiConfigurationStatus.Unknown, "ValidationStatus should default to 'Unknown'");
    }

    [Fact]
    public void ApiConfiguration_Should_Allow_Nullable_OptionalFields()
    {
        // Arrange & Act
        var config = new ApiConfiguration
        {
            UserId = "user",
            Provider = "Anthropic",
            EncryptedApiKey = "key",
            EncryptionIV = "iv",
            EncryptionSalt = "salt",
            KeyFingerprint = "fingerprint",
            DisplayName = null,
            LastUsedAt = null,
            LastValidatedAt = null
        };

        // Assert
        config.DisplayName.Should().BeNull("DisplayName is optional");
        config.LastUsedAt.Should().BeNull("LastUsedAt is optional");
        config.LastValidatedAt.Should().BeNull("LastValidatedAt is optional");
    }

    [Fact]
    public void ApiConfiguration_Should_Store_DisplayName()
    {
        // Arrange & Act
        var config = new ApiConfiguration
        {
            UserId = "user",
            Provider = "Anthropic",
            DisplayName = "My Personal Anthropic Key",
            EncryptedApiKey = "key",
            EncryptionIV = "iv",
            EncryptionSalt = "salt",
            KeyFingerprint = "fingerprint"
        };

        // Assert
        config.DisplayName.Should().Be("My Personal Anthropic Key");
    }

    [Fact]
    public void ApiConfiguration_Should_Track_LastUsedAt()
    {
        // Arrange
        var lastUsed = DateTime.UtcNow.AddMinutes(-30);

        // Act
        var config = new ApiConfiguration
        {
            UserId = "user",
            Provider = "Anthropic",
            EncryptedApiKey = "key",
            EncryptionIV = "iv",
            EncryptionSalt = "salt",
            KeyFingerprint = "fingerprint",
            LastUsedAt = lastUsed
        };

        // Assert
        config.LastUsedAt.Should().NotBeNull();
        config.LastUsedAt.Should().BeCloseTo(lastUsed, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ApiConfiguration_Should_Track_LastValidatedAt()
    {
        // Arrange
        var lastValidated = DateTime.UtcNow.AddHours(-1);

        // Act
        var config = new ApiConfiguration
        {
            UserId = "user",
            Provider = "Anthropic",
            EncryptedApiKey = "key",
            EncryptionIV = "iv",
            EncryptionSalt = "salt",
            KeyFingerprint = "fingerprint",
            LastValidatedAt = lastValidated,
            ValidationStatus = ApiConfigurationStatus.Valid
        };

        // Assert
        config.LastValidatedAt.Should().NotBeNull();
        config.LastValidatedAt.Should().BeCloseTo(lastValidated, TimeSpan.FromSeconds(1));
        config.ValidationStatus.Should().Be(ApiConfigurationStatus.Valid);
    }

    [Fact]
    public void ApiConfiguration_Should_Support_Multiple_Providers()
    {
        // Arrange & Act - Test different provider values
        var anthropicConfig = new ApiConfiguration
        {
            UserId = "user",
            Provider = "Anthropic",
            EncryptedApiKey = "key1",
            EncryptionIV = "iv",
            EncryptionSalt = "salt",
            KeyFingerprint = "fingerprint1"
        };

        var openaiConfig = new ApiConfiguration
        {
            UserId = "user",
            Provider = "OpenAI",
            EncryptedApiKey = "key2",
            EncryptionIV = "iv",
            EncryptionSalt = "salt",
            KeyFingerprint = "fingerprint2"
        };

        // Assert
        anthropicConfig.Provider.Should().Be("Anthropic");
        openaiConfig.Provider.Should().Be("OpenAI");
    }

    [Fact]
    public void ApiConfiguration_Should_Store_Encryption_Components()
    {
        // Arrange & Act
        var config = new ApiConfiguration
        {
            UserId = "user",
            Provider = "Anthropic",
            EncryptedApiKey = "encrypted_base64_data_here",
            EncryptionIV = "initialization_vector_here",
            EncryptionSalt = "salt_value_here",
            KeyFingerprint = "sha256_fingerprint_here"
        };

        // Assert - All encryption components should be stored
        config.EncryptedApiKey.Should().Be("encrypted_base64_data_here");
        config.EncryptionIV.Should().Be("initialization_vector_here");
        config.EncryptionSalt.Should().Be("salt_value_here");
        config.KeyFingerprint.Should().Be("sha256_fingerprint_here");
    }

    [Fact]
    public void ApiConfiguration_Should_Allow_Deactivation()
    {
        // Arrange
        var config = new ApiConfiguration
        {
            UserId = "user",
            Provider = "Anthropic",
            EncryptedApiKey = "key",
            EncryptionIV = "iv",
            EncryptionSalt = "salt",
            KeyFingerprint = "fingerprint",
            IsActive = true
        };

        // Act - Deactivate the configuration
        config.IsActive = false;

        // Assert
        config.IsActive.Should().BeFalse("configuration should be deactivatable");
    }
}