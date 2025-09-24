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
    /// Test health endpoint responds after successful startup
    /// Note: May return ServiceUnavailable (503) if some health checks fail
    /// </summary>
    [Fact]
    [Trait("Category", "ProductionStartup")]
    public async Task HealthEndpoint_AfterSuccessfulStartup_ShouldReturnHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert - Health endpoint should respond (even if unhealthy)
        // In test environment, some health checks may fail (e.g., Claude API key)
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,                    // Healthy
            HttpStatusCode.ServiceUnavailable     // Unhealthy or Degraded
        );
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    public class ProductionWebApplicationFactory : WebApplicationFactory<Program>
    {
        static ProductionWebApplicationFactory()
        {
            // Only set environment variables if not already set (for CI environment compatibility)
            // In CI, these are set properly by the workflow
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_URL")) &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")))
            {
                // Use in-memory database for local testing when no database is configured
                // This simulates a production environment with a working database
                Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection",
                    "Host=localhost;Port=5432;Database=digitalme_test;Username=postgres;Password=postgres");
            }

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT_KEY")))
            {
                Environment.SetEnvironmentVariable("JWT_KEY", "production-super-secret-key-12345678901234567890123456789012");
            }

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
            {
                Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", "test-production-api-key");
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");

            // Additional configuration if needed
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Any additional config can go here
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Production",
                    ["JWT:Issuer"] = "DigitalMe-Production",
                    ["JWT:Audience"] = "DigitalMe-API",
                    ["JWT:ExpireHours"] = "24",
                });
            });

            // Override database configuration to use in-memory database for testing
            // This prevents connection failures when PostgreSQL is not available
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registrations
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<DigitalMeDbContext>) ||
                    d.ServiceType == typeof(DigitalMeDbContext) ||
                    d.ImplementationType == typeof(DigitalMeDbContext)).ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<DigitalMeDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase");
                });
            });
        }
    }
}