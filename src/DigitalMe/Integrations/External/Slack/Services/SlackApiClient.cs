using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DigitalMe.Configuration;
using DigitalMe.Integrations.External.Slack.Models;
using Microsoft.Extensions.Options;

namespace DigitalMe.Integrations.External.Slack.Services;

/// <summary>
/// Low-level HTTP client for Slack API with rate limiting and authentication
/// </summary>
public class SlackApiClient : IDisposable
{
    private readonly ILogger<SlackApiClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SlackSettings _settings;

    private string _botToken = string.Empty;
    private const string SlackApiBaseUrl = "https://slack.com/api/";

    // Rate limiting - Slack allows 1+ requests per second per method
    private readonly SemaphoreSlim _rateLimitSemaphore;
    private DateTime _lastRequestTime = DateTime.MinValue;
    private readonly TimeSpan _rateLimitDelay = TimeSpan.FromMilliseconds(1100); // 1.1 seconds between requests

    public SlackApiClient(
        ILogger<SlackApiClient> logger,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<IntegrationSettings> integrationSettings)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _settings = integrationSettings.CurrentValue.Slack;
        _rateLimitSemaphore = new SemaphoreSlim(1, 1);
    }

    public void SetBotToken(string botToken)
    {
        _botToken = botToken;
    }

    /// <summary>
    /// Make a GET request to Slack API
    /// </summary>
    public async Task<T?> GetAsync<T>(string endpoint, Dictionary<string, string>? parameters = null, CancellationToken cancellationToken = default) where T : class
    {
        var url = BuildUrl(endpoint, parameters);

        using var httpClient = CreateHttpClient();

        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            _logger.LogDebug("Making GET request to Slack API: {Url}", url);
            var response = await httpClient.GetAsync(url, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Slack API GET request failed: {StatusCode} - {Content}", response.StatusCode, content);
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(content, JsonSerializerOptions.Web);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making GET request to Slack API: {Endpoint}", endpoint);
            return null;
        }
    }

    /// <summary>
    /// Make a POST request to Slack API
    /// </summary>
    public async Task<T?> PostAsync<T>(string endpoint, object? data = null, CancellationToken cancellationToken = default) where T : class
    {
        var url = SlackApiBaseUrl + endpoint;

        using var httpClient = CreateHttpClient();

        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            _logger.LogDebug("Making POST request to Slack API: {Url}", url);

            HttpContent content;
            if (data != null)
            {
                var json = JsonSerializer.Serialize(data, JsonSerializerOptions.Web);
                content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            else
            {
                content = new StringContent("", Encoding.UTF8, "application/json");
            }

            var response = await httpClient.PostAsync(url, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Slack API POST request failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(responseContent, JsonSerializerOptions.Web);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making POST request to Slack API: {Endpoint}", endpoint);
            return null;
        }
    }

    /// <summary>
    /// Upload a file to Slack API
    /// </summary>
    public async Task<T?> UploadFileAsync<T>(string endpoint, Stream fileStream, string filename, Dictionary<string, string>? formData = null, CancellationToken cancellationToken = default) where T : class
    {
        var url = SlackApiBaseUrl + endpoint;

        using var httpClient = CreateHttpClient();

        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            _logger.LogDebug("Uploading file to Slack API: {Url}, Filename: {Filename}", url, filename);

            using var content = new MultipartFormDataContent();

            // Add file
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", filename);

            // Add form data
            if (formData != null)
            {
                foreach (var kvp in formData)
                {
                    content.Add(new StringContent(kvp.Value), kvp.Key);
                }
            }

            var response = await httpClient.PostAsync(url, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Slack API file upload failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(responseContent, JsonSerializerOptions.Web);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to Slack API: {Endpoint}", endpoint);
            return null;
        }
    }

    private HttpClient CreateHttpClient()
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _botToken);
        httpClient.DefaultRequestHeaders.Add("User-Agent", "DigitalMe-SlackBot/1.0");
        return httpClient;
    }

    private static string BuildUrl(string endpoint, Dictionary<string, string>? parameters)
    {
        var url = SlackApiBaseUrl + endpoint;

        if (parameters != null && parameters.Count > 0)
        {
            var query = string.Join("&", parameters.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            url += "?" + query;
        }

        return url;
    }

    private async Task EnforceRateLimitAsync(CancellationToken cancellationToken)
    {
        await _rateLimitSemaphore.WaitAsync(cancellationToken);
        try
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            if (timeSinceLastRequest < _rateLimitDelay)
            {
                var waitTime = _rateLimitDelay - timeSinceLastRequest;
                _logger.LogDebug("Rate limiting: waiting {WaitTime}ms before next request", waitTime.TotalMilliseconds);
                await Task.Delay(waitTime, cancellationToken);
            }
            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    public void Dispose()
    {
        _rateLimitSemaphore?.Dispose();
    }
}