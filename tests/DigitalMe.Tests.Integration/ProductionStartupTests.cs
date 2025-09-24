using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using System.Net;
using Xunit;
using DigitalMe.Data;

namespace DigitalMe.Tests.Integration;

/// <summary>
/// TDD Tests to reproduce and fix production startup crashes
/// These tests simulate production environment conditions that cause app.Run() to fail
/// </summary>
public class ProductionStartupTests : IClassFixture<ProductionStartupTests.ProductionWebApplicationFactory>
{
    private readonly ProductionWebApplicationFactory _factory;

    public ProductionStartupTests(ProductionWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// FAILING TEST: Reproduce SIGABRT crash at Program.cs:779 (app.Run())
    /// This test should reproduce the exact production startup failure
    /// </summary>
    [Fact]
    [Trait("Category", "ProductionStartup")]
    [Trait("Bug", "SIGABRT-779")]
    public async Task Application_WithProductionConfiguration_ShouldStartWithoutCrashing()
    {
        // Arrange - Production-like environment
        // This test should FAIL initially, reproducing the SIGABRT crash

        // Act & Assert - App should start without crashing
        var exception = await Record.ExceptionAsync(async () =>
        {
            var client = _factory.CreateClient();

            // Try to make any request - this will force the app to fully start
            var response = await client.GetAsync("/health");

            // If we get here without exception, startup succeeded
        });

        // Assert - Should not crash during startup
        exception.Should().BeNull("Application should start without SIGABRT crash at app.Run()");
    }

    /// <summary>
    /// Test health endpoint works after successful startup
    /// </summary>
    [Fact]
    [Trait("Category", "ProductionStartup")]
    public async Task HealthEndpoint_AfterSuccessfulStartup_ShouldReturnHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBe("Unhealthy");
    }

    public class ProductionWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");

            // Configure services to use invalid database configuration
            // This will cause DataConsistencyHealthCheck to fail gracefully
            builder.ConfigureServices(services =>
            {
                // Remove all existing DbContext registrations
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<DigitalMeDbContext>) ||
                    d.ServiceType == typeof(DigitalMeDbContext) ||
                    d.ImplementationType == typeof(DigitalMeDbContext)).ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Add DbContext with invalid configuration - simulating Cloud Run with no DB access
                services.AddDbContext<DigitalMeDbContext>(options =>
                {
                    // Use non-existent file path that will cause connection issues
                    options.UseSqlite("Data Source=/nonexistent/readonly/path/test.db");
                });
            });

            // Configure like production environment WITHOUT connection string
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Simulate production config that might cause crashes
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Production",
                    ["ASPNETCORE_URLS"] = "http://+:8080",

                    // JWT settings (required)
                    ["JWT:Key"] = "production-super-secret-key-12345678901234567890123456789012",
                    ["JWT:Issuer"] = "DigitalMe-Production",
                    ["JWT:Audience"] = "DigitalMe-API",
                    ["JWT:ExpireHours"] = "24",

                    // DATABASE CONNECTION STRING MISSING - this should cause the crash
                    // ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",

                    // API Keys
                    ["ANTHROPIC_API_KEY"] = "test-production-api-key",
                });
            });
        }
    }
}