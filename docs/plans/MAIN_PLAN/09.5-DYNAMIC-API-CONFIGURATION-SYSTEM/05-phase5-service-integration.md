# 📋 PHASE 5: SERVICE INTEGRATION (TDD)

**Parent Plan**: [09.5-DYNAMIC-API-CONFIGURATION-SYSTEM.md](../09.5-DYNAMIC-API-CONFIGURATION-SYSTEM.md)

**Phase Status**: PENDING
**Priority**: CRITICAL
**Estimated Duration**: 3 days
**Dependencies**: Phase 4 Complete

---

## Phase Objectives

Integrate the dynamic API configuration system with existing services (PersonalityService, AnthropicService, etc.). Ensure backward compatibility while enabling per-user API key configuration.

---

## Task 5.1: Modify PersonalityService for Dynamic Configuration

**Status**: PENDING
**Priority**: CRITICAL
**Estimated**: 60 minutes
**Dependencies**: Phase 4 complete

### TDD Cycle

#### 1. RED: Create integration tests
File: `tests/DigitalMe.Tests.Integration/Services/PersonalityServiceIntegrationTests.cs`

```csharp
public class PersonalityServiceIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task PersonalityService_Should_Use_Dynamic_Configuration()
    {
        // Arrange
        var userId = "test-user";
        var userKey = "sk-ant-user-test-key";

        // Set user-specific key
        var configService = ServiceProvider.GetRequiredService<IApiConfigurationService>();
        await configService.SetUserApiKeyAsync(userId, "Anthropic", userKey);

        // Create personality service with user context
        var personalityService = ServiceProvider.GetRequiredService<IPersonalityService>();

        // Act
        var result = await personalityService.ProcessWithUserContextAsync(
            "Test prompt", userId);

        // Assert
        // Verify that user key was used (check logs or usage records)
        var usageRecords = await GetUsageRecordsAsync(userId);
        usageRecords.Should().ContainSingle(r => r.Provider == "Anthropic");
    }

    [Fact]
    public async Task PersonalityService_Should_Fallback_To_System_Key()
    {
        // Arrange
        var userId = "user-without-key";
        var personalityService = ServiceProvider.GetRequiredService<IPersonalityService>();

        // Act
        var result = await personalityService.ProcessWithUserContextAsync(
            "Test prompt", userId);

        // Assert
        result.Should().NotBeNull();
        // Should work with system key fallback
    }

    [Fact]
    public async Task PersonalityService_Should_Track_Usage()
    {
        // Arrange
        var userId = "tracked-user";
        var personalityService = ServiceProvider.GetRequiredService<IPersonalityService>();

        // Act
        await personalityService.ProcessWithUserContextAsync("Test", userId);

        // Assert
        var tracker = ServiceProvider.GetRequiredService<IApiUsageTracker>();
        var stats = await tracker.GetUsageStatsAsync(userId, DateTime.Today, DateTime.Now);
        stats.RequestCount.Should().Be(1);
    }
}
```

#### 2. GREEN: Modify PersonalityService
File: `src/DigitalMe/Services/PersonalityService.cs` (modified)

```csharp
public class PersonalityService : IPersonalityService
{
    private readonly IApiConfigurationService _apiConfigService;
    private readonly IApiUsageTracker _usageTracker;
    private readonly IAnthropicService _anthropicService;
    private readonly ILogger<PersonalityService> _logger;

    public PersonalityService(
        IApiConfigurationService apiConfigService,
        IApiUsageTracker usageTracker,
        IAnthropicService anthropicService,
        ILogger<PersonalityService> logger)
    {
        _apiConfigService = apiConfigService;
        _usageTracker = usageTracker;
        _anthropicService = anthropicService;
        _logger = logger;
    }

    public async Task<Result<string>> ProcessWithUserContextAsync(
        string prompt, string? userId = null)
    {
        try
        {
            // Get API key dynamically
            var apiKey = await _apiConfigService.GetApiKeyAsync("Anthropic", userId);
            if (string.IsNullOrEmpty(apiKey))
            {
                return Result<string>.Failure("No API key available for Anthropic");
            }

            // Make API call with dynamic key
            var startTime = DateTime.UtcNow;
            var response = await _anthropicService.SendRequestAsync(prompt, apiKey);
            var responseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            // Track usage
            if (!string.IsNullOrEmpty(userId))
            {
                await _usageTracker.RecordUsageAsync(userId, "Anthropic",
                    new UsageDetails
                    {
                        RequestType = "personality.process",
                        TokensUsed = response.TokensUsed,
                        ResponseTime = responseTime,
                        Success = true
                    });
            }

            return Result<string>.Success(response.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process with user context");

            // Track failure
            if (!string.IsNullOrEmpty(userId))
            {
                await _usageTracker.RecordUsageAsync(userId, "Anthropic",
                    new UsageDetails
                    {
                        RequestType = "personality.process",
                        Success = false,
                        ErrorType = ex.GetType().Name
                    });
            }

            return Result<string>.Failure($"Processing failed: {ex.Message}");
        }
    }

    // Backward compatibility - use system key
    public async Task<Result<string>> ProcessAsync(string prompt)
    {
        return await ProcessWithUserContextAsync(prompt, userId: null);
    }
}
```

### Acceptance Criteria
- ✅ Dynamic configuration working
- ✅ Backward compatibility maintained
- ✅ Usage tracking integrated
- ✅ All existing tests still pass

---

## Task 5.2: Create/Modify AnthropicService Integration

**Status**: PENDING
**Priority**: HIGH
**Estimated**: 90 minutes
**Dependencies**: Task 5.1

### TDD Cycle for AnthropicService

#### 1. RED: Create AnthropicService tests
File: `tests/DigitalMe.Tests.Unit/Services/AnthropicServiceTests.cs`

```csharp
public class AnthropicServiceTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpFactory;
    private readonly Mock<IApiConfigurationService> _mockConfigService;
    private readonly AnthropicService _service;

    [Fact]
    public async Task SendRequestAsync_Should_Use_Provided_ApiKey()
    {
        // Arrange
        var mockHttpClient = new Mock<HttpClient>();
        _mockHttpFactory.Setup(f => f.CreateClient("Anthropic"))
            .Returns(mockHttpClient.Object);

        var apiKey = "sk-ant-test-key";

        // Act
        await _service.SendRequestAsync("Test prompt", apiKey);

        // Assert
        mockHttpClient.Verify(c => c.DefaultRequestHeaders.Add(
            "x-api-key", apiKey), Times.Once);
    }

    [Fact]
    public async Task SendRequestAsync_Should_Parse_Token_Usage()
    {
        // Arrange
        var responseContent = @"{
            ""completion"": ""Test response"",
            ""usage"": {
                ""input_tokens"": 100,
                ""output_tokens"": 50
            }
        }";

        SetupMockHttpResponse(responseContent);

        // Act
        var result = await _service.SendRequestAsync("Test", "key");

        // Assert
        result.TokensUsed.Should().Be(150);
        result.Content.Should().Be("Test response");
    }

    [Fact]
    public async Task Should_Handle_Rate_Limiting()
    {
        // Arrange
        SetupMockHttpResponse("", HttpStatusCode.TooManyRequests);

        // Act & Assert
        await Assert.ThrowsAsync<RateLimitException>(() =>
            _service.SendRequestAsync("Test", "key"));
    }
}
```

#### 2. GREEN: Implement AnthropicService
File: `src/DigitalMe/Services/AnthropicService.cs`

```csharp
public class AnthropicService : IAnthropicService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AnthropicService> _logger;
    private const string ApiEndpoint = "https://api.anthropic.com/v1/messages";

    public AnthropicService(
        IHttpClientFactory httpClientFactory,
        ILogger<AnthropicService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<AnthropicResponse> SendRequestAsync(string prompt, string apiKey)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("Anthropic");
            httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var request = new
            {
                model = "claude-3-opus-20240229",
                max_tokens = 1024,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(ApiEndpoint, content);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? 60;
                throw new RateLimitException($"Rate limit exceeded. Retry after {retryAfter} seconds");
            }

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var anthropicResponse = JsonSerializer.Deserialize<AnthropicApiResponse>(responseContent);

            return new AnthropicResponse
            {
                Content = anthropicResponse.content[0].text,
                TokensUsed = anthropicResponse.usage.input_tokens + anthropicResponse.usage.output_tokens,
                Model = anthropicResponse.model,
                StopReason = anthropicResponse.stop_reason
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request to Anthropic API failed");
            throw new AnthropicServiceException("Failed to communicate with Anthropic API", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request to Anthropic API timed out");
            throw new AnthropicServiceException("Request timed out", ex);
        }
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        try
        {
            // Simple validation call with minimal tokens
            var result = await SendRequestAsync("Hi", apiKey);
            return !string.IsNullOrEmpty(result.Content);
        }
        catch
        {
            return false;
        }
    }
}

// Response models
public class AnthropicResponse
{
    public string Content { get; set; }
    public int TokensUsed { get; set; }
    public string Model { get; set; }
    public string StopReason { get; set; }
}

public class AnthropicApiResponse
{
    public List<ContentBlock> content { get; set; }
    public UsageInfo usage { get; set; }
    public string model { get; set; }
    public string stop_reason { get; set; }
}

public class ContentBlock
{
    public string text { get; set; }
    public string type { get; set; }
}

public class UsageInfo
{
    public int input_tokens { get; set; }
    public int output_tokens { get; set; }
}
```

### Acceptance Criteria
- ✅ AnthropicService implemented
- ✅ Token usage tracked
- ✅ Rate limiting handled
- ✅ API key validation working
- ✅ Error handling comprehensive

---

## Task 5.3: Update Dependency Injection

**Status**: PENDING
**Priority**: HIGH
**Estimated**: 30 minutes
**Dependencies**: Task 5.2

### DI Configuration Updates

```csharp
// In Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // API Configuration
    services.AddScoped<IApiConfigurationRepository, ApiConfigurationRepository>();
    services.AddScoped<IApiConfigurationService, ApiConfigurationService>();
    services.AddScoped<IKeyEncryptionService, KeyEncryptionService>();

    // Usage Tracking
    services.AddScoped<IApiUsageRepository, ApiUsageRepository>();
    services.AddScoped<IApiUsageTracker, ApiUsageTracker>();
    services.AddScoped<IQuotaManager, QuotaManager>();

    // API Services
    services.AddScoped<IAnthropicService, AnthropicService>();
    services.AddScoped<IApiKeyValidator, ApiKeyValidator>();

    // Updated PersonalityService with new dependencies
    services.AddScoped<IPersonalityService, PersonalityService>();

    // HTTP Client Factory for API calls
    services.AddHttpClient("Anthropic", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });
}
```

### Acceptance Criteria
- ✅ All services registered
- ✅ Scoped lifetime correct
- ✅ HTTP clients configured
- ✅ No circular dependencies

---

## Phase Completion Checklist

- [ ] PersonalityService integrated
- [ ] AnthropicService created/updated
- [ ] Usage tracking working end-to-end
- [ ] Backward compatibility verified
- [ ] Integration tests passing
- [ ] DI configuration updated
- [ ] 85%+ test coverage
- [ ] Documentation updated

---

## Output Artifacts

1. **Services**: Updated `PersonalityService.cs`, new `AnthropicService.cs`
2. **Models**: `AnthropicResponse.cs`, `AnthropicApiResponse.cs`
3. **Exceptions**: `RateLimitException.cs`, `AnthropicServiceException.cs`
4. **Configuration**: Updated DI registration
5. **Tests**: Full integration test suite

---

## Next Phase Dependencies

Phase 6 (UI Layer) depends on:
- All services integrated
- API configuration working end-to-end
- Usage tracking operational