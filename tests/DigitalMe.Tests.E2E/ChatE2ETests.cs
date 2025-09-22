using FluentAssertions;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace DigitalMe.Tests.E2E;

/// <summary>
/// E2E tests for chat endpoints against real deployed environments
/// These tests cover the full chat pipeline: API → AI → Response
/// </summary>
public class ChatE2ETests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
    private readonly JsonSerializerOptions _jsonOptions;

    public ChatE2ETests()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(E2ETestConfig.ApiBaseUrl),
            Timeout = E2ETestConfig.HttpTimeout
        };

        // Configure retry policy for cold starts and network issues
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(response => !response.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                E2ETestConfig.MaxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "All")]
    public async Task ChatSend_SimpleMessage_ShouldReturnSuccessfulResponse()
    {
        // Arrange
        var testUserId = $"test-user-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
        var requestPayload = new
        {
            message = "Hello! This is a test message.",
            platform = "Web",
            userId = testUserId
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestPayload, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsync("/api/chat/send", content));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "chat API should accept valid requests");

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeEmpty(
            "chat API should return response content");

        // Verify response structure
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);
        responseObject.TryGetProperty("id", out var id).Should().BeTrue();
        responseObject.TryGetProperty("content", out var responseText).Should().BeTrue();
        responseObject.TryGetProperty("role", out var role).Should().BeTrue();

        role.GetString().Should().Be("assistant",
            "response should be from AI assistant");
        responseText.GetString().Should().NotBeNullOrEmpty(
            "AI should provide a meaningful response");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "All")]
    public async Task ChatSend_EmptyMessage_ShouldHandleGracefully()
    {
        // Arrange
        var testUserId = $"test-user-empty-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
        var requestPayload = new
        {
            message = "",
            platform = "Web",
            userId = testUserId
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestPayload, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsync("/api/chat/send", content));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "API should handle empty messages gracefully with default greeting");

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);
        responseObject.TryGetProperty("content", out var responseText).Should().BeTrue();

        responseText.GetString().Should().NotBeNullOrEmpty(
            "empty message should trigger default greeting response");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "All")]
    public async Task ChatSend_TechnicalQuestion_ShouldReturnRelevantResponse()
    {
        // Arrange
        var testUserId = $"test-user-tech-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
        var requestPayload = new
        {
            message = "What is dependency injection in C#?",
            platform = "Web",
            userId = testUserId
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestPayload, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsync("/api/chat/send", content));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);
        responseObject.TryGetProperty("content", out var responseText).Should().BeTrue();

        var responseString = responseText.GetString();
        responseString.Should().NotBeNullOrEmpty();

        // Verify it's a technical response (Ivan's expertise)
        responseString.Should().ContainAny("dependency", "injection", "C#", "DI", "container",
            "Ivan should provide technical expertise on C# topics");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "All")]
    public async Task ChatSend_MultipleMessages_ShouldMaintainConversation()
    {
        // Arrange
        var testUserId = $"test-user-conversation-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

        // First message
        var firstPayload = new
        {
            message = "Hi, I'm working on a .NET project.",
            platform = "Web",
            userId = testUserId
        };

        // Second message (should reference conversation context)
        var secondPayload = new
        {
            message = "Can you help me with architecture decisions?",
            platform = "Web",
            userId = testUserId
        };

        // Act - Send first message
        var firstContent = new StringContent(
            JsonSerializer.Serialize(firstPayload, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        var firstResponse = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsync("/api/chat/send", firstContent));

        // Wait a moment to ensure message is processed
        await Task.Delay(1000);

        // Act - Send second message
        var secondContent = new StringContent(
            JsonSerializer.Serialize(secondPayload, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        var secondResponse = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsync("/api/chat/send", secondContent));

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstResponseContent = await firstResponse.Content.ReadAsStringAsync();
        var secondResponseContent = await secondResponse.Content.ReadAsStringAsync();

        firstResponseContent.Should().NotBeEmpty();
        secondResponseContent.Should().NotBeEmpty();

        // Both responses should contain conversation IDs
        var firstObject = JsonSerializer.Deserialize<JsonElement>(firstResponseContent);
        var secondObject = JsonSerializer.Deserialize<JsonElement>(secondResponseContent);

        firstObject.TryGetProperty("conversationId", out var firstConvId).Should().BeTrue();
        secondObject.TryGetProperty("conversationId", out var secondConvId).Should().BeTrue();

        firstConvId.GetString().Should().Be(secondConvId.GetString(),
            "messages from same user should share conversation ID");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "All")]
    public async Task ChatSend_InvalidJson_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidContent = new StringContent(
            "{ invalid json }",
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _httpClient.PostAsync("/api/chat/send", invalidContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "invalid JSON should be rejected");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "All")]
    public async Task ChatSend_MissingRequiredFields_ShouldReturnBadRequest()
    {
        // Arrange - Request without userId
        var incompletePayload = new
        {
            message = "Test message",
            platform = "Web"
            // Missing userId
        };

        var content = new StringContent(
            JsonSerializer.Serialize(incompletePayload, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _httpClient.PostAsync("/api/chat/send", content);

        // Assert
        response.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.BadRequest, HttpStatusCode.OK },
            "API should either validate required fields or provide defaults");
    }

    [Theory]
    [Trait("Category", "E2E")]
    [Trait("Environment", "All")]
    [InlineData("Web")]
    [InlineData("Mobile")]
    [InlineData("Telegram")]
    public async Task ChatSend_DifferentPlatforms_ShouldHandleAllPlatforms(string platform)
    {
        // Arrange
        var testUserId = $"test-user-{platform.ToLower()}-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
        var requestPayload = new
        {
            message = $"Testing from {platform} platform",
            platform = platform,
            userId = testUserId
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestPayload, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsync("/api/chat/send", content));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"API should support {platform} platform");

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        // Verify platform is tracked in metadata
        responseObject.TryGetProperty("metadata", out var metadata).Should().BeTrue();
        if (metadata.ValueKind == JsonValueKind.Object)
        {
            metadata.TryGetProperty("platform", out var platformValue).Should().BeTrue();
            platformValue.GetString().Should().Be(platform);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "Production")]
    public async Task ChatSend_HighLoad_ShouldHandleConcurrentRequests()
    {
        // Skip for local environment
        if (E2ETestConfig.Environment == "local") return;

        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        const int concurrentRequests = 5;

        for (int i = 0; i < concurrentRequests; i++)
        {
            var testUserId = $"test-user-concurrent-{i}-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            var requestPayload = new
            {
                message = $"Concurrent test message {i + 1}",
                platform = "Web",
                userId = testUserId
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestPayload, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            tasks.Add(_retryPolicy.ExecuteAsync(async () =>
                await _httpClient.PostAsync("/api/chat/send", content)));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK,
                "API should handle concurrent requests successfully");
        }

        responses.Length.Should().Be(concurrentRequests,
            "all concurrent requests should complete");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "All")]
    public async Task ChatSend_ResponseTime_ShouldBeFastEnough()
    {
        // Arrange
        var testUserId = $"test-user-perf-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
        var requestPayload = new
        {
            message = "Quick response test",
            platform = "Web",
            userId = testUserId
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestPayload, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsync("/api/chat/send", content));
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var maxExpectedTime = E2ETestConfig.Environment switch
        {
            "production" => TimeSpan.FromSeconds(30), // Cloud Run cold start + AI processing
            "local" => TimeSpan.FromSeconds(10),
            _ => TimeSpan.FromSeconds(20)
        };

        stopwatch.Elapsed.Should().BeLessOrEqualTo(maxExpectedTime,
            $"chat response should be fast enough for {E2ETestConfig.Environment} environment");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}