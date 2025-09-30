using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using DigitalMe.Exceptions;
using DigitalMe.Models.Integrations;
using DigitalMe.Models.Usage;
using DigitalMe.Services.Usage;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Integrations;

/// <summary>
/// Реализация сервиса для работы с Anthropic API с динамической конфигурацией.
/// Использует IApiConfigurationService для получения API ключей и IApiUsageTracker для отслеживания.
/// </summary>
public class AnthropicServiceV2 : IAnthropicServiceV2
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IApiConfigurationService _apiConfigService;
    private readonly IApiUsageTracker _usageTracker;
    private readonly ILogger<AnthropicServiceV2> _logger;

    private const string ApiVersion = "2023-06-01";
    private const string DefaultModel = "claude-3-5-sonnet-20241022";
    private const int DefaultMaxTokens = 1000;
    private const string SystemUserId = "SYSTEM"; // Special userId for system key fallback

    public AnthropicServiceV2(
        IHttpClientFactory httpClientFactory,
        IApiConfigurationService apiConfigService,
        IApiUsageTracker usageTracker,
        ILogger<AnthropicServiceV2> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _apiConfigService = apiConfigService ?? throw new ArgumentNullException(nameof(apiConfigService));
        _usageTracker = usageTracker ?? throw new ArgumentNullException(nameof(usageTracker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<AnthropicResponse> SendMessageAsync(
        string prompt,
        string? userId = null,
        string? systemPrompt = null)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be empty.", nameof(prompt));
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Get API key dynamically (use SYSTEM userId for system key fallback)
            var effectiveUserId = userId ?? SystemUserId;
            var apiKey = await _apiConfigService.GetApiKeyAsync("Anthropic", effectiveUserId)
                .ConfigureAwait(false);

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new AnthropicServiceException("No API key available for Anthropic");
            }

            _logger.LogDebug("Sending message to Anthropic API (userId: {UserId})", userId ?? "system");

            // Prepare HTTP client
            var httpClient = _httpClientFactory.CreateClient("Anthropic");

            // Build request
            var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages");
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", ApiVersion);

            // Build request body
            var requestBody = new
            {
                model = DefaultModel,
                max_tokens = DefaultMaxTokens,
                system = systemPrompt,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            // Send request
            var response = await httpClient.SendAsync(request).ConfigureAwait(false);

            stopwatch.Stop();

            // Handle rate limiting
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? 60;
                _logger.LogWarning("Rate limit exceeded. Retry after {RetryAfter} seconds", retryAfter);

                // Track failure
                if (!string.IsNullOrEmpty(userId))
                {
                    await TrackFailureAsync(userId, stopwatch.ElapsedMilliseconds, "RateLimitException")
                        .ConfigureAwait(false);
                }

                throw new RateLimitException($"Rate limit exceeded. Retry after {retryAfter} seconds", (int)retryAfter);
            }

            // Handle other errors
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogError("Anthropic API error {StatusCode}: {Error}",
                    response.StatusCode, errorContent);

                // Track failure
                if (!string.IsNullOrEmpty(userId))
                {
                    await TrackFailureAsync(userId, stopwatch.ElapsedMilliseconds, "AnthropicServiceException")
                        .ConfigureAwait(false);
                }

                throw new AnthropicServiceException(
                    $"Anthropic API returned {response.StatusCode}: {response.ReasonPhrase}",
                    (int)response.StatusCode);
            }

            // Parse response
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var apiResponse = JsonSerializer.Deserialize<AnthropicApiResponse>(responseText);

            if (apiResponse == null || apiResponse.Content.Count == 0)
            {
                throw new AnthropicServiceException("Invalid response from Anthropic API");
            }

            var content = string.Join("\n", apiResponse.Content.Select(c => c.Text));
            var inputTokens = apiResponse.Usage.InputTokens;
            var outputTokens = apiResponse.Usage.OutputTokens;
            var totalTokens = inputTokens + outputTokens;

            _logger.LogInformation("Anthropic API success: {InputTokens} in, {OutputTokens} out, {TotalMs}ms",
                inputTokens, outputTokens, stopwatch.ElapsedMilliseconds);

            // Track usage
            if (!string.IsNullOrEmpty(userId))
            {
                await _usageTracker.RecordUsageAsync(userId, "Anthropic", new UsageDetails
                {
                    RequestType = "anthropic.message",
                    TokensUsed = totalTokens,
                    ResponseTime = stopwatch.ElapsedMilliseconds,
                    Success = true
                }).ConfigureAwait(false);
            }

            return new AnthropicResponse
            {
                Content = content,
                TokensUsed = totalTokens,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (RateLimitException)
        {
            throw; // Re-throw without wrapping
        }
        catch (AnthropicServiceException)
        {
            throw; // Re-throw without wrapping
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Failed to send message to Anthropic API");

            // Track failure
            if (!string.IsNullOrEmpty(userId))
            {
                await TrackFailureAsync(userId, stopwatch.ElapsedMilliseconds, ex.GetType().Name)
                    .ConfigureAwait(false);
            }

            throw new AnthropicServiceException($"Failed to send message: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsConnectedAsync(string? userId = null)
    {
        try
        {
            // Get API key (use SYSTEM userId for system key fallback)
            var effectiveUserId = userId ?? SystemUserId;
            var apiKey = await _apiConfigService.GetApiKeyAsync("Anthropic", effectiveUserId)
                .ConfigureAwait(false);

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogDebug("No API key available for connectivity check");
                return false;
            }

            // Simple connectivity test with minimal tokens
            var httpClient = _httpClientFactory.CreateClient("Anthropic");

            var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages");
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", ApiVersion);

            var testRequest = new
            {
                model = DefaultModel,
                max_tokens = 10,
                messages = new[]
                {
                    new { role = "user", content = "Hi" }
                }
            };

            var json = JsonSerializer.Serialize(testRequest);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);

            _logger.LogDebug("Anthropic API connectivity check: {StatusCode}", response.StatusCode);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Anthropic API connectivity check failed");
            return false;
        }
    }

    private async Task TrackFailureAsync(string userId, long responseTime, string errorType)
    {
        try
        {
            await _usageTracker.RecordUsageAsync(userId, "Anthropic", new UsageDetails
            {
                RequestType = "anthropic.message",
                TokensUsed = 0,
                ResponseTime = responseTime,
                Success = false,
                ErrorType = errorType
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to track usage failure for user {UserId}", userId);
            // Don't throw - usage tracking shouldn't break the main flow
        }
    }
}