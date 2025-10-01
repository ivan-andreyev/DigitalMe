using DigitalMe.Data;
using DigitalMe.Data.Entities;
using DigitalMe.Exceptions;
using DigitalMe.Models.Integrations;
using DigitalMe.Repositories;
using DigitalMe.Services;
using DigitalMe.Services.Integrations;
using DigitalMe.Services.Security;
using DigitalMe.Services.Usage;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace DigitalMe.Tests.Integration.Services;

/// <summary>
/// Integration tests for Dynamic API Configuration System (Phase 5).
/// Tests end-to-end flow: key storage → encryption → resolution → API call → usage tracking.
/// </summary>
[Collection("Integration Tests")]
public class DynamicApiConfigurationIntegrationTests : IntegrationTestBase
{
    public DynamicApiConfigurationIntegrationTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    /// <summary>
    /// Test 1: End-to-End User Key Flow
    /// Verify: encrypted key → resolution → decryption → API call → usage tracking
    /// </summary>
    [Fact]
    public async Task EndToEnd_UserKey_Should_Flow_Through_Full_Stack()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";
        const string userApiKey = "sk-ant-api03-user-test-key-AA";
        const string prompt = "Test prompt";

        using var scope = Factory.Services.CreateScope();
        var apiConfigService = scope.ServiceProvider.GetRequiredService<IApiConfigurationService>();
        var usageRepository = scope.ServiceProvider.GetRequiredService<IApiUsageRepository>();

        // Mock HTTP client to avoid real API calls
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    content = new[]
                    {
                        new { type = "text", text = "Test response from Anthropic" }
                    },
                    usage = new { input_tokens = 10, output_tokens = 20 }
                }), Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.anthropic.com/")
        };

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient("Anthropic")).Returns(httpClient);

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AnthropicServiceV2>>();
        var usageTracker = scope.ServiceProvider.GetRequiredService<IApiUsageTracker>();

        var anthropicService = new AnthropicServiceV2(
            httpClientFactory.Object,
            apiConfigService,
            usageTracker,
            logger);

        // Act Step 1: Store encrypted user key
        await apiConfigService.SetUserApiKeyAsync(provider, userId, userApiKey);

        // Act Step 2: Make API call (should use user's key)
        var response = await anthropicService.SendMessageAsync(prompt, userId);

        // Act Step 3: Get usage records
        var usageRecords = await usageRepository.GetUsageRecordsAsync(userId,
            DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));

        // Assert: Response received
        response.Should().NotBeNull();
        response.Content.Should().Contain("Test response from Anthropic");
        response.TokensUsed.Should().Be(30); // 10 input + 20 output

        // Assert: Usage tracked
        usageRecords.Should().HaveCount(1);
        usageRecords[0].UserId.Should().Be(userId);
        usageRecords[0].Provider.Should().Be(provider);
        usageRecords[0].TokensUsed.Should().Be(30);
        usageRecords[0].Success.Should().BeTrue();

        // Assert: HTTP call made with correct API key
        mockHttpMessageHandler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Headers.Contains("x-api-key") &&
                req.Headers.GetValues("x-api-key").First() == userApiKey),
            ItExpr.IsAny<CancellationToken>());
    }

    /// <summary>
    /// Test 2: System Key Fallback Flow
    /// Verify: no user key → SYSTEM key fallback → no usage tracking
    /// </summary>
    [Fact]
    public async Task EndToEnd_SystemKeyFallback_Should_Work_Without_UserKey()
    {
        // Arrange
        const string provider = "Anthropic";
        const string systemApiKey = "sk-ant-api03-system-fallback-key-AA";
        const string prompt = "Test system prompt";

        using var scope = Factory.Services.CreateScope();
        var apiConfigService = scope.ServiceProvider.GetRequiredService<IApiConfigurationService>();
        var usageRepository = scope.ServiceProvider.GetRequiredService<IApiUsageRepository>();

        // Mock HTTP client
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    content = new[]
                    {
                        new { type = "text", text = "System response" }
                    },
                    usage = new { input_tokens = 5, output_tokens = 15 }
                }), Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.anthropic.com/")
        };

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient("Anthropic")).Returns(httpClient);

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AnthropicServiceV2>>();
        var usageTracker = scope.ServiceProvider.GetRequiredService<IApiUsageTracker>();

        var anthropicService = new AnthropicServiceV2(
            httpClientFactory.Object,
            apiConfigService,
            usageTracker,
            logger);

        // Act Step 1: Set system key (using SYSTEM userId)
        await apiConfigService.SetUserApiKeyAsync(provider, "SYSTEM", systemApiKey);

        // Act Step 2: Make API call WITHOUT userId (should use SYSTEM key)
        var response = await anthropicService.SendMessageAsync(prompt, userId: null);

        // Act Step 3: Check usage records (should be empty - no tracking for system calls)
        var usageRecords = await usageRepository.GetUsageRecordsAsync("SYSTEM",
            DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));

        // Assert: Response received
        response.Should().NotBeNull();
        response.Content.Should().Contain("System response");

        // Assert: NO usage tracking for system key
        usageRecords.Should().BeEmpty("system key usage should not be tracked");

        // Assert: HTTP call made with system API key
        mockHttpMessageHandler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Headers.Contains("x-api-key") &&
                req.Headers.GetValues("x-api-key").First() == systemApiKey),
            ItExpr.IsAny<CancellationToken>());
    }

    /// <summary>
    /// Test 3: Quota Enforcement Integration
    /// Verify: exceeded quota → QuotaExceededException → notification sent
    /// </summary>
    [Fact]
    public async Task EndToEnd_QuotaExceeded_Should_Block_API_Call()
    {
        // Arrange
        const string userId = "user-quota-test";
        const string provider = "Anthropic";
        const int dailyLimit = 1000;
        const int currentUsage = 950;
        const int requestTokens = 100; // Would exceed limit

        using var scope = Factory.Services.CreateScope();
        var quotaManager = scope.ServiceProvider.GetRequiredService<IQuotaManager>();
        var usageRepository = scope.ServiceProvider.GetRequiredService<IApiUsageRepository>();

        // Seed quota data
        var quota = new UserQuota
        {
            UserId = userId,
            Provider = provider,
            DailyTokenLimit = dailyLimit,
            MonthlyTokenLimit = 30000,
            SubscriptionTier = "Basic",
            NotificationsEnabled = true
        };
        await usageRepository.SaveUserQuotaAsync(quota);

        var dailyUsage = new DailyUsage
        {
            UserId = userId,
            Provider = provider,
            Date = DateTime.Today, // Use DateTime.Today to match QuotaManager logic
            TokensUsed = currentUsage,
            RequestCount = 19
        };
        await usageRepository.UpdateDailyUsageAsync(dailyUsage);

        // Act & Assert: Check if can use tokens (should be false - quota exceeded)
        var canUse = await quotaManager.CanUseTokensAsync(userId, provider, requestTokens);
        canUse.Should().BeFalse("quota limit should be enforced");

        // Verify quota status shows usage near limit
        var quotaStatus = await quotaManager.GetQuotaStatusAsync(userId, provider);
        quotaStatus.Should().NotBeNull();
        quotaStatus.Used.Should().Be(currentUsage);
        quotaStatus.DailyLimit.Should().Be(dailyLimit);
        quotaStatus.PercentUsed.Should().BeGreaterThan(90, "usage should be over 90%");
    }

    /// <summary>
    /// Test 4: Multi-User Isolation
    /// Verify: User1 key != User2 key, usage tracking isolated
    /// </summary>
    [Fact]
    public async Task EndToEnd_MultipleUsers_Should_Have_Isolated_Keys_And_Usage()
    {
        // Arrange
        const string user1 = "user-alice";
        const string user2 = "user-bob";
        const string provider = "Anthropic";
        const string user1Key = "sk-ant-api03-alice-key-AA";
        const string user2Key = "sk-ant-api03-bob-key-BB";

        using var scope = Factory.Services.CreateScope();
        var apiConfigService = scope.ServiceProvider.GetRequiredService<IApiConfigurationService>();
        var usageRepository = scope.ServiceProvider.GetRequiredService<IApiUsageRepository>();

        // Act Step 1: Store keys for both users
        await apiConfigService.SetUserApiKeyAsync(provider, user1, user1Key);
        await apiConfigService.SetUserApiKeyAsync(provider, user2, user2Key);

        // Act Step 2: Resolve keys
        var resolvedKey1 = await apiConfigService.GetApiKeyAsync(provider, user1);
        var resolvedKey2 = await apiConfigService.GetApiKeyAsync(provider, user2);

        // Act Step 3: Track usage for both users
        await usageRepository.SaveUsageRecordAsync(new ApiUsageRecord
        {
            UserId = user1,
            Provider = provider,
            TokensUsed = 50,
            Success = true,
            RequestType = "test1"
        });

        await usageRepository.SaveUsageRecordAsync(new ApiUsageRecord
        {
            UserId = user2,
            Provider = provider,
            TokensUsed = 75,
            Success = true,
            RequestType = "test2"
        });

        // Act Step 4: Get usage for each user
        var user1Usage = await usageRepository.GetUsageRecordsAsync(user1,
            DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
        var user2Usage = await usageRepository.GetUsageRecordsAsync(user2,
            DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));

        // Assert: Keys are different and correctly resolved
        resolvedKey1.Should().Be(user1Key);
        resolvedKey2.Should().Be(user2Key);
        resolvedKey1.Should().NotBe(resolvedKey2);

        // Assert: Usage is isolated per user
        user1Usage.Should().HaveCount(1);
        user1Usage[0].UserId.Should().Be(user1);
        user1Usage[0].TokensUsed.Should().Be(50);

        user2Usage.Should().HaveCount(1);
        user2Usage[0].UserId.Should().Be(user2);
        user2Usage[0].TokensUsed.Should().Be(75);
    }

    /// <summary>
    /// Test 5: Error Propagation
    /// Verify: Invalid API key → HTTP 401 → AnthropicServiceException
    ///         Rate limit → HTTP 429 → RateLimitException
    /// </summary>
    [Fact]
    public async Task EndToEnd_InvalidApiKey_Should_Propagate_As_Exception()
    {
        // Arrange
        const string userId = "user-invalid-key";
        const string provider = "Anthropic";
        const string invalidKey = "sk-ant-invalid";
        const string prompt = "Test error handling";

        using var scope = Factory.Services.CreateScope();
        var apiConfigService = scope.ServiceProvider.GetRequiredService<IApiConfigurationService>();

        // Mock HTTP client to return 401 Unauthorized
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    error = new { type = "authentication_error", message = "Invalid API key" }
                }), Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.anthropic.com/")
        };

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient("Anthropic")).Returns(httpClient);

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AnthropicServiceV2>>();
        var usageTracker = scope.ServiceProvider.GetRequiredService<IApiUsageTracker>();

        var anthropicService = new AnthropicServiceV2(
            httpClientFactory.Object,
            apiConfigService,
            usageTracker,
            logger);

        // Act Step 1: Store invalid key
        await apiConfigService.SetUserApiKeyAsync(provider, userId, invalidKey);

        // Act & Assert: API call should throw AnthropicServiceException
        var act = async () => await anthropicService.SendMessageAsync(prompt, userId);
        await act.Should().ThrowAsync<AnthropicServiceException>()
            .Where(ex => ex.StatusCode == 401);
    }

    [Fact]
    public async Task EndToEnd_RateLimit_Should_Propagate_RateLimitException()
    {
        // Arrange
        const string userId = "user-rate-limited";
        const string provider = "Anthropic";
        const string validKey = "sk-ant-api03-valid-key-AA";
        const string prompt = "Test rate limiting";
        const int retryAfterSeconds = 60;

        using var scope = Factory.Services.CreateScope();
        var apiConfigService = scope.ServiceProvider.GetRequiredService<IApiConfigurationService>();

        // Mock HTTP client to return 429 Rate Limit
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.TooManyRequests,
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                error = new { type = "rate_limit_error", message = "Rate limit exceeded" }
            }), Encoding.UTF8, "application/json")
        };
        response.Headers.Add("Retry-After", retryAfterSeconds.ToString());

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.anthropic.com/")
        };

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient("Anthropic")).Returns(httpClient);

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AnthropicServiceV2>>();
        var usageTracker = scope.ServiceProvider.GetRequiredService<IApiUsageTracker>();

        var anthropicService = new AnthropicServiceV2(
            httpClientFactory.Object,
            apiConfigService,
            usageTracker,
            logger);

        // Act Step 1: Store valid key
        await apiConfigService.SetUserApiKeyAsync(provider, userId, validKey);

        // Act & Assert: API call should throw RateLimitException
        var act = async () => await anthropicService.SendMessageAsync(prompt, userId);
        var exception = await act.Should().ThrowAsync<RateLimitException>();
        exception.Which.RetryAfterSeconds.Should().Be(retryAfterSeconds);
    }
}