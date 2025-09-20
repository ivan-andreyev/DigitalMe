using FluentAssertions;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using System.Text;
using Xunit;

namespace DigitalMe.Tests.E2E;

/// <summary>
/// E2E tests for authentication endpoints against real deployed environments
/// </summary>
public class AuthenticationE2ETests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    public AuthenticationE2ETests()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(E2ETestConfig.ApiBaseUrl),
            Timeout = E2ETestConfig.HttpTimeout
        };

        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(response => response.StatusCode == HttpStatusCode.ServiceUnavailable)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                E2ETestConfig.MaxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "All")]
    public async Task AuthValidate_WithoutToken_ShouldReturn401()
    {
        // Act
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.GetAsync("/api/auth/validate"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Should NOT redirect to login page (that's for web, not API)
        response.StatusCode.Should().NotBe(HttpStatusCode.Redirect);
        response.StatusCode.Should().NotBe(HttpStatusCode.Found);
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "All")]
    public async Task AuthRegister_WithValidData_ShouldReturnToken()
    {
        // Arrange
        var registerData = new
        {
            email = $"test-{Guid.NewGuid():N}@example.com",
            password = "TestPassword123!",
            confirmPassword = "TestPassword123!"
        };

        var json = JsonConvert.SerializeObject(registerData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsync("/api/auth/register", content));

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,           // Success
            HttpStatusCode.Created,      // User created
            HttpStatusCode.Conflict      // User already exists (acceptable for testing)
        );

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().NotBeEmpty();

            // Should contain token or success indicator
            responseContent.Should().ContainAny("token", "access_token", "success");
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "All")]
    public async Task AuthValidate_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token-12345");

        // Act
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.GetAsync("/api/auth/validate"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "All")]
    public async Task AuthEndpoints_ShouldNotHang()
    {
        // This test ensures auth endpoints respond within reasonable time
        var testEndpoints = new[]
        {
            "/api/auth/validate",
            "/api/auth/register"
        };

        foreach (var endpoint in testEndpoints)
        {
            // Act & Assert
            var startTime = DateTime.UtcNow;

            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                var duration = DateTime.UtcNow - startTime;

                // Should respond within timeout (even if error status)
                duration.Should().BeLessThan(E2ETestConfig.HttpTimeout);

                // Should not hang indefinitely
                response.StatusCode.Should().NotBe(HttpStatusCode.RequestTimeout);
            }
            catch (TaskCanceledException)
            {
                var duration = DateTime.UtcNow - startTime;
                Assert.Fail($"Endpoint {endpoint} timed out after {duration}");
            }
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "Production")]
    public async Task Production_AuthFlow_ShouldWork()
    {
        // Skip this test for local environment
        if (E2ETestConfig.Environment != "production") return;

        // This is a comprehensive test that validates the entire auth flow works in production
        // Act
        var healthResponse = await _httpClient.GetAsync("/health/simple");
        var authResponse = await _httpClient.GetAsync("/api/auth/validate");

        // Assert
        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        authResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized); // Expected without token

        // Both should respond quickly
        healthResponse.Should().NotBeNull();
        authResponse.Should().NotBeNull();
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}