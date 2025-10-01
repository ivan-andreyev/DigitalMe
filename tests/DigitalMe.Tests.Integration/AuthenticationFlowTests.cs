using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using DigitalMe;
using DigitalMe.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DigitalMe.Tests.Integration;

public class AuthenticationFlowTests : IClassFixture<AuthenticationFlowTests.CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthenticationFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove all existing DbContext registrations
                var descriptors = services.Where(d => d.ServiceType == typeof(DbContextOptions<DigitalMeDbContext>) ||
                                                     d.ServiceType == typeof(DigitalMeDbContext) ||
                                                     d.ImplementationType == typeof(DigitalMeDbContext)).ToList();
                
                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Use in-memory database for testing
                services.AddDbContext<DigitalMeDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb-{Guid.NewGuid()}");
                });
            });

            builder.UseEnvironment("Testing");
            
            // Configure test settings
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JWT:Key"] = "test-super-secret-key-12345678901234567890123456789012",
                    ["JWT:Issuer"] = "TestIssuer",
                    ["JWT:Audience"] = "TestAudience",
                    ["JWT:ExpireHours"] = "24",
                    ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:"
                });
            });
        }

    }

    [Fact]
    public async Task RegisterUser_ValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var registerRequest = new
        {
            email = "test.user@example.com",
            password = "Test123@",
            confirmPassword = "Test123@"
        };

        var json = JsonSerializer.Serialize(registerRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/register", content);

        // Assert - Should return 200 OK with JWT token
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(authResponse);
        Assert.True(authResponse.Success);
        Assert.NotNull(authResponse.Token);
        Assert.NotEmpty(authResponse.Token);
    }

    [Fact]
    public async Task AuthValidate_WithoutToken_Returns401NotRedirect()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/validate");

        // Assert - Should return 401, NOT 302 redirect
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Found, response.StatusCode); // 302 Found = redirect
    }

    [Fact(Skip = "Test isolation issue: Passes in isolation but fails when run with other tests. IClassFixture shares WebApplicationFactory/database state across tests. Fix: Convert to IAsyncLifetime or create factory per test.")]
    public async Task AuthValidate_WithValidToken_Returns200()
    {
        // TODO: Test passes when run in isolation (dotnet test --filter AuthValidate_WithValidToken_Returns200)
        // but fails when run with all integration tests (returns 401 instead of 200).
        //
        // Root cause: IClassFixture<CustomWebApplicationFactory> shares the factory and in-memory database
        // across all tests in this class. This creates test interdependencies.
        //
        // Potential solutions:
        // 1. Convert to IAsyncLifetime and create new factory per test
        // 2. Use separate in-memory database per test (not per class)
        // 3. Ensure proper cleanup between tests
        // 4. Use xUnit Collection to isolate this test class

        // Arrange - Create dedicated client for this test to avoid shared state pollution
        using var testClient = _factory.CreateClient();

        // Step 1: Register and get token
        var registerRequest = new
        {
            email = "validate.test@example.com",
            password = "Test123@",
            confirmPassword = "Test123@"
        };

        var json = JsonSerializer.Serialize(registerRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var registerResponse = await testClient.PostAsync("/api/auth/register", content);

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(registerContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Act - Use token to validate (set on dedicated client)
        testClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.Token);

        var validateResponse = await testClient.GetAsync("/api/auth/validate");

        // Assert - Should return 200 OK
        Assert.Equal(HttpStatusCode.OK, validateResponse.StatusCode);
    }

    [Fact]
    public async Task SignalRHub_Negotiate_ReturnsConnectionInfo()
    {
        // Act
        var response = await _client.PostAsync("/chathub/negotiate", 
            new StringContent("", Encoding.UTF8, "application/json"));

        // Assert - Should return 200 with SignalR connection info
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("connectionId", content);
        Assert.Contains("availableTransports", content);
    }

    private class AuthResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}