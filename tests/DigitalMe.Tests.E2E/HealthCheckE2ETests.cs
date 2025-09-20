using FluentAssertions;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using Xunit;

namespace DigitalMe.Tests.E2E;

/// <summary>
/// E2E tests for health check endpoints against real deployed environments
/// </summary>
public class HealthCheckE2ETests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    public HealthCheckE2ETests()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(E2ETestConfig.ApiBaseUrl),
            Timeout = E2ETestConfig.HttpTimeout
        };

        // Configure retry policy for cold starts
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(response => !response.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                E2ETestConfig.MaxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "All")]
    public async Task HealthCheck_Simple_ShouldReturnHealthy()
    {
        // Act
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.GetAsync("/health/simple"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "All")]
    public async Task HealthCheck_Detailed_ShouldReturnHealthy()
    {
        // Act
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.GetAsync("/health"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();

        // Should contain health information
        content.Should().ContainAny("Healthy", "\"status\"", "\"checks\"");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "Production")]
    public async Task HealthCheck_Database_ShouldBeHealthy()
    {
        // Skip for local environment as it might not have real DB
        if (E2ETestConfig.Environment == "local") return;

        // Act
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.GetAsync("/health"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        // Should indicate database is healthy
        content.Should().NotContain("Unhealthy");
        content.Should().NotContain("Degraded");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "All")]
    public async Task Api_Root_ShouldNotReturn404()
    {
        // Act
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.GetAsync("/"));

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Redirect,
            HttpStatusCode.TemporaryRedirect,
            HttpStatusCode.PermanentRedirect
        );
    }

    [Theory]
    [Trait("Category", "E2E")]
    [Trait("Environment", "All")]
    [InlineData("/health/simple")]
    [InlineData("/api/personality")]
    public async Task CriticalEndpoints_ShouldNotReturnServerErrors(string endpoint)
    {
        // Act
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.GetAsync(endpoint));

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.Should().NotBe(HttpStatusCode.BadGateway);
        response.StatusCode.Should().NotBe(HttpStatusCode.ServiceUnavailable);
        response.StatusCode.Should().NotBe(HttpStatusCode.GatewayTimeout);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}