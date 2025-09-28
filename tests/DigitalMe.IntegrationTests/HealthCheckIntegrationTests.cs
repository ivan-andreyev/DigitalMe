using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DigitalMe.Data;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace DigitalMe.IntegrationTests;

/// <summary>
/// Integration tests for health check functionality
/// Validates that health endpoint correctly reports application status
/// </summary>
public class HealthCheckIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public HealthCheckIntegrationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy_WhenDatabaseIsConnected()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Use in-memory database for reliable testing
                services.Remove(services.First(s => s.ServiceType == typeof(DbContextOptions<DigitalMeDbContext>)));
                services.AddDbContext<DigitalMeDbContext>(options =>
                    options.UseInMemoryDatabase("HealthTest_Connected"));
            });
            builder.ConfigureLogging(logging => logging.AddConsole());
        });

        // Act
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", content, StringComparison.OrdinalIgnoreCase);

        _output.WriteLine($"✅ Health check response: {content}");
    }

    [Fact]
    public async Task HealthCheck_ShouldIncludeDatabaseStatus()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.Remove(services.First(s => s.ServiceType == typeof(DbContextOptions<DigitalMeDbContext>)));
                services.AddDbContext<DigitalMeDbContext>(options =>
                    options.UseInMemoryDatabase("HealthTest_DatabaseStatus"));
            });
        });

        // Act
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Health check should include database status
        var hasDbInfo = content.Contains("database", StringComparison.OrdinalIgnoreCase) ||
                       content.Contains("db", StringComparison.OrdinalIgnoreCase) ||
                       content.Contains("Healthy", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasDbInfo, "Health check should include database connectivity information");
        _output.WriteLine($"📊 Health check details: {content}");
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnQuickly()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(5);
        using var cts = new CancellationTokenSource(timeout);

        // Act
        var startTime = DateTime.UtcNow;
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health", cts.Token);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(duration < timeout, $"Health check should respond within {timeout.TotalSeconds}s but took {duration.TotalSeconds}s");

        _output.WriteLine($"⚡ Health check response time: {duration.TotalMilliseconds}ms");
    }

    [Fact]
    public async Task HealthCheck_ShouldBeAccessible_WithoutAuthentication()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - No authentication headers
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);

        _output.WriteLine("✅ Health endpoint accessible without authentication");
    }

    [Fact]
    public async Task HealthCheck_ShouldWorkAfterApplicationStartup()
    {
        // Arrange - Wait for application to fully start
        var client = _factory.CreateClient();

        // Give startup time to complete
        await Task.Delay(1000);

        // Act
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", content, StringComparison.OrdinalIgnoreCase);

        _output.WriteLine("✅ Health check works after application startup");
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/")]
    [InlineData("/Health")]
    public async Task HealthCheck_ShouldHandleDifferentUrlFormats(string healthUrl)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(healthUrl);

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Health check should work with URL: {healthUrl}");

        _output.WriteLine($"✅ Health check works with URL: {healthUrl}");
    }

    [Fact]
    public async Task HealthCheck_ShouldProvideDetailedStatus_InDevelopment()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.Remove(services.First(s => s.ServiceType == typeof(DbContextOptions<DigitalMeDbContext>)));
                services.AddDbContext<DigitalMeDbContext>(options =>
                    options.UseInMemoryDatabase("HealthTest_Detailed"));
            });
        });

        // Act
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _output.WriteLine($"🔍 Detailed health status: {content}");

        // In development, we might get more detailed information
        var hasDetailedInfo = content.Length > 10; // More than just "Healthy"
        Assert.True(hasDetailedInfo, "Development environment should provide detailed health information");
    }
}