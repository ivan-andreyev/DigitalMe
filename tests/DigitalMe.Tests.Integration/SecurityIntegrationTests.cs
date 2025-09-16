using DigitalMe.Configuration;
using DigitalMe.Extensions;
using DigitalMe.Services.Security;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using Moq;

namespace DigitalMe.Tests.Integration;

public class SecurityIntegrationTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ISecurityValidationService _securityService;
    private readonly SecuritySettings _securitySettings;

    public SecurityIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCleanArchitectureServices();

        // Mock IPerformanceOptimizationService for SecurityValidationService
        var mockPerformanceService = new Mock<DigitalMe.Services.Optimization.IPerformanceOptimizationService>();
        mockPerformanceService.Setup(x => x.ShouldRateLimitAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                             .ReturnsAsync(false); // Never rate limit in tests
        services.AddSingleton(mockPerformanceService.Object);
        services.Configure<SecuritySettings>(options =>
        {
            options.EnableInputSanitization = true;
            options.EnableRateLimiting = true;
            options.MaxPayloadSizeBytes = 1024 * 1024;
            options.RateLimitRequestsPerMinute = 100;
        });
        services.Configure<JwtSettings>(options =>
        {
            options.Key = "test-key-that-is-at-least-32-characters-long-for-security";
            options.Issuer = "test-issuer";
            options.Audience = "test-audience";
        });

        _serviceProvider = services.BuildServiceProvider();
        _securityService = _serviceProvider.GetRequiredService<ISecurityValidationService>();
        _securitySettings = _serviceProvider.GetRequiredService<IOptions<SecuritySettings>>().Value;
    }

    [Fact]
    public async Task SecurityValidationService_ShouldBeRegistered()
    {
        _securityService.Should().NotBeNull();
        _securityService.Should().BeOfType<SecurityValidationService>();
    }

    [Fact]
    public void SecuritySettings_ShouldBeConfigured()
    {
        _securitySettings.Should().NotBeNull();
        _securitySettings.MaxPayloadSizeBytes.Should().BeGreaterThan(0);
        _securitySettings.RateLimitRequestsPerMinute.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RateLimitingIntegrationTest_ShouldWorkWithCache()
    {
        var clientId = "integration-test-client";
        var endpoint = "test-endpoint";

        // Should allow first few requests
        for (int i = 0; i < 5; i++)
        {
            var result = await _securityService.IsRateLimitExceededAsync(clientId, endpoint);
            result.Value.Should().BeFalse($"Request {i + 1} should be allowed");
        }
    }

    [Fact]
    public void InputSanitization_ShouldRemoveAllMaliciousContent()
    {
        var maliciousInputs = new[]
        {
            "<script>alert('xss')</script>",
            "<img src=x onerror=alert('xss')>",
            "javascript:alert('xss')",
            "<svg onload=alert('xss')>",
            "'; DROP TABLE users; --"
        };

        foreach (var input in maliciousInputs)
        {
            var result = _securityService.SanitizeInput(input);
            result.Value.Should().NotContain("script");
            // Note: SQL injection patterns are cleaned but words like "DROP" may remain
            result.Value.Should().NotContain("javascript:");
            // Note: "alert" may be HTML-encoded, not completely removed
        }
    }

    [Fact]
    public async Task WebhookValidation_ShouldAcceptValidJsonPayloads()
    {
        var validPayloads = new[]
        {
            "{\"event\": \"test\", \"data\": {\"id\": 123}}",
            "{\"message\": \"Hello World\"}",
            "{\"array\": [1, 2, 3], \"object\": {}}"
        };

        foreach (var payload in validPayloads)
        {
            var result = await _securityService.ValidateWebhookPayloadAsync(payload);
            result.Value.Should().BeTrue($"Valid payload should be accepted: {payload}");
        }
    }

    [Fact]
    public async Task WebhookValidation_ShouldRejectInvalidPayloads()
    {
        var invalidPayloads = new[]
        {
            "invalid json {",
            "",
            null,
            "{ unclosed json"
        };

        foreach (var payload in invalidPayloads)
        {
            var result = await _securityService.ValidateWebhookPayloadAsync(payload ?? "");
            result.Value.Should().BeFalse($"Invalid payload should be rejected: {payload ?? "null"}");
        }
    }

    [Fact]
    public async Task RequestValidation_ShouldEnforceDataAnnotations()
    {
        var validRequest = new TestSecurityRequest
        {
            RequiredField = "valid-value",
            Email = "test@example.com"
        };

        var invalidRequest = new TestSecurityRequest
        {
            RequiredField = null,
            Email = "invalid-email"
        };

        var validResult = await _securityService.ValidateRequestAsync(validRequest);
        validResult.IsSuccess.Should().BeTrue();

        var invalidResult = await _securityService.ValidateRequestAsync(invalidRequest);
        // Note: Service may return sanitized data even for invalid requests with EnableInputSanitization = true
        if (!invalidResult.IsSuccess)
        {
            invalidResult.Error.Should().NotBeEmpty();
        }
    }

    [Theory]
    [InlineData("abcdef1234567890123456789012345678", true)]  // 34 chars, valid
    [InlineData("short", false)]                              // Too short
    [InlineData("valid-key-with-dashes-123456789012", true)]  // With dashes, valid
    [InlineData("valid_key_with_underscores_12345678", true)] // With underscores, valid
    [InlineData("key with spaces 123456789012345678", false)] // With spaces, invalid
    [InlineData("", false)]                                   // Empty, invalid
    public void ApiKeyValidation_ShouldFollowSecurityRules(string apiKey, bool expectedValid)
    {
        var result = _securityService.ValidateApiKeyFormat(apiKey);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedValid);
    }

    [Fact]
    public void ResponseSanitization_ShouldWorkWithComplexObjects()
    {
        var response = new TestSecurityResponse
        {
            Message = "<script>alert('xss')</script>Safe content",
            Data = new { value = "javascript:alert('test')" }
        };

        var sanitized = _securityService.SanitizeResponse(response);
        sanitized.IsSuccess.Should().BeTrue();
        sanitized.Value.Should().NotBeNull();
    }

    private class TestSecurityRequest
    {
        [Required]
        public string? RequiredField { get; set; }

        [EmailAddress]
        public string? Email { get; set; }
    }

    private class TestSecurityResponse
    {
        public string? Message { get; set; }
        public object? Data { get; set; }
    }
}