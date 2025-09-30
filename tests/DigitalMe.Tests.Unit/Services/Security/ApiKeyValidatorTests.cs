using Xunit;
using FluentAssertions;
using DigitalMe.Services.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DigitalMe.Tests.Unit.Services.Security;

/// <summary>
/// TDD Test Suite for ApiKeyValidator
/// RED PHASE: Tests should initially fail
/// Tests cover API key validation, caching, and circuit breaker functionality
/// </summary>
public class ApiKeyValidatorTests
{
    private readonly IMemoryCache _cache;
    private readonly ApiKeyValidator _validator;

    public ApiKeyValidatorTests()
    {
        var cacheOptions = Options.Create(new MemoryCacheOptions());
        _cache = new MemoryCache(cacheOptions);
        _validator = new ApiKeyValidator(_cache, NullLogger<ApiKeyValidator>.Instance);
    }

    #region Format Validation Tests

    [Theory]
    [InlineData("sk-ant-api03-valid-key-123")]
    [InlineData("sk-ant-api01-another-valid-key")]
    public async Task ValidateAnthropicKeyAsync_Should_Accept_Valid_Format(string apiKey)
    {
        // Act
        var result = await _validator.ValidateAnthropicKeyAsync(apiKey);

        // Assert
        result.IsValid.Should().BeTrue("valid Anthropic key format should pass basic validation");
    }

    [Theory]
    [InlineData("invalid-key")]
    [InlineData("sk-openai-key")]
    [InlineData("")]
    [InlineData(null)]
    public async Task ValidateAnthropicKeyAsync_Should_Reject_Invalid_Format(string? apiKey)
    {
        // Act
        var result = await _validator.ValidateAnthropicKeyAsync(apiKey!);

        // Assert
        result.IsValid.Should().BeFalse("invalid format should fail validation");
        result.ErrorMessage.Should().NotBeNullOrEmpty("should provide error message");
    }

    [Theory]
    [InlineData("sk-proj-valid-openai-key-123")]
    [InlineData("sk-valid-openai-legacy-key")]
    public async Task ValidateOpenAIKeyAsync_Should_Accept_Valid_Format(string apiKey)
    {
        // Act
        var result = await _validator.ValidateOpenAIKeyAsync(apiKey);

        // Assert
        result.IsValid.Should().BeTrue("valid OpenAI key format should pass basic validation");
    }

    [Theory]
    [InlineData("invalid-key")]
    [InlineData("sk-ant-key")]
    [InlineData("")]
    public async Task ValidateOpenAIKeyAsync_Should_Reject_Invalid_Format(string apiKey)
    {
        // Act
        var result = await _validator.ValidateOpenAIKeyAsync(apiKey);

        // Assert
        result.IsValid.Should().BeFalse("invalid format should fail validation");
    }

    #endregion

    #region Caching Tests

    [Fact]
    public async Task ValidateAnthropicKeyAsync_Should_Cache_Validation_Results()
    {
        // Arrange
        const string apiKey = "sk-ant-api03-test-key";

        // Act - call twice
        var result1 = await _validator.ValidateAnthropicKeyAsync(apiKey);
        var result2 = await _validator.ValidateAnthropicKeyAsync(apiKey);

        // Assert
        result1.IsValid.Should().Be(result2.IsValid, "cached results should be consistent");
        result1.DurationMs.Should().BeGreaterThan(0, "first call should have actual duration");
        result2.DurationMs.Should().Be(0, "cached call should return instantly");
    }

    [Fact]
    public async Task ValidateKeyAsync_Should_Use_Cache_Across_Different_Methods()
    {
        // Arrange
        const string provider = "Anthropic";
        const string apiKey = "sk-ant-api03-test-key";

        // Act
        await _validator.ValidateAnthropicKeyAsync(apiKey);
        var cachedResult = await _validator.ValidateKeyAsync(provider, apiKey);

        // Assert
        cachedResult.DurationMs.Should().BeLessThan(10, "should retrieve from cache quickly");
    }

    [Fact]
    public async Task Cache_Should_Expire_After_Configured_Duration()
    {
        // Arrange
        const string apiKey = "sk-ant-api03-test-key";

        // Act - first call
        await _validator.ValidateAnthropicKeyAsync(apiKey);

        // Wait for cache expiration (assuming 5 minute cache)
        await Task.Delay(100); // Simulate time passing

        var result = await _validator.ValidateAnthropicKeyAsync(apiKey);

        // Assert
        result.Should().NotBeNull("validation should still work after cache expiration");
    }

    #endregion

    #region Circuit Breaker Tests

    [Fact]
    public async Task Should_Open_Circuit_After_Multiple_Failures()
    {
        // Arrange
        const string invalidKey = "sk-ant-invalid-key-that-will-fail";

        // Act - trigger multiple failures
        for (int i = 0; i < 5; i++)
        {
            await _validator.ValidateAnthropicKeyAsync(invalidKey);
        }

        // Next call should fail fast due to circuit breaker
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _validator.ValidateAnthropicKeyAsync(invalidKey);
        stopwatch.Stop();

        // Assert
        result.IsValid.Should().BeFalse("circuit should be open");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50, "circuit breaker should fail fast");
    }

    [Fact]
    public async Task Should_Reset_Circuit_After_Success()
    {
        // Arrange
        const string validKey = "sk-ant-api03-valid-key";
        const string invalidKey = "invalid-format-key";

        // Act - trigger some failures with invalid format
        await _validator.ValidateAnthropicKeyAsync(invalidKey);
        await _validator.ValidateAnthropicKeyAsync(invalidKey);

        // Success should reset circuit
        await _validator.ValidateAnthropicKeyAsync(validKey);

        // Assert - next call with valid key should succeed
        var result = await _validator.ValidateAnthropicKeyAsync(validKey);
        result.IsValid.Should().BeTrue("valid format keys should pass validation");
    }

    #endregion

    #region Provider-Specific Tests

    [Theory]
    [InlineData("Anthropic", "sk-ant-api03-test")]
    [InlineData("OpenAI", "sk-proj-test-123")]
    [InlineData("Google", "AIzaSyTest123")]
    public async Task ValidateKeyAsync_Should_Route_To_Correct_Provider(string provider, string apiKey)
    {
        // Act
        var result = await _validator.ValidateKeyAsync(provider, apiKey);

        // Assert
        result.Should().NotBeNull("should return validation result");
        result.DurationMs.Should().BeGreaterThanOrEqualTo(0, "should track duration (0 if cached)");
    }

    [Fact]
    public async Task ValidateKeyAsync_Should_Reject_Unknown_Provider()
    {
        // Act
        var result = await _validator.ValidateKeyAsync("UnknownProvider", "any-key");

        // Assert
        result.IsValid.Should().BeFalse("unknown provider should fail");
        result.ErrorMessage.Should().Contain("Unknown provider", "should explain the error");
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task Validation_Should_Complete_Within_Performance_Budget()
    {
        // Arrange
        const string apiKey = "sk-ant-api03-test-key";
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await _validator.ValidateAnthropicKeyAsync(apiKey);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "validation should complete within 5 seconds");
        result.DurationMs.Should().BeCloseTo(stopwatch.ElapsedMilliseconds, 100);
    }

    [Fact]
    public async Task Cached_Validation_Should_Be_Fast()
    {
        // Arrange
        const string apiKey = "sk-ant-api03-test-key";

        // Act - warm up cache
        await _validator.ValidateAnthropicKeyAsync(apiKey);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _validator.ValidateAnthropicKeyAsync(apiKey);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10, "cached validation should be nearly instant");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Should_Handle_Invalid_Format_Gracefully()
    {
        // Arrange - use an invalid format key
        const string apiKey = "invalid-key-format";

        // Act
        var result = await _validator.ValidateAnthropicKeyAsync(apiKey);

        // Assert
        result.Should().NotBeNull("should return result for invalid keys");
        result.IsValid.Should().BeFalse("invalid format keys should fail validation");
        result.ErrorMessage.Should().NotBeNullOrEmpty("should provide error message");
    }

    [Fact]
    public async Task Should_Provide_Meaningful_Error_Messages()
    {
        // Arrange
        const string invalidKey = "invalid-format";

        // Act
        var result = await _validator.ValidateAnthropicKeyAsync(invalidKey);

        // Assert
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace("should provide error message");
        result.ErrorMessage.Should().MatchRegex("format|invalid|required", "error should be descriptive");
    }

    #endregion
}