using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace DigitalMe.Tests.Integration;

/// <summary>
/// TDD Tests to ensure production environment properly validates database configuration
/// These tests prevent production from starting with SQLite or without proper PostgreSQL config
/// </summary>
public class ProductionDatabaseValidationTests
{
    [Fact]
    [Trait("Category", "ProductionValidation")]
    [Trait("Environment", "LocalOnly")]
    public void Production_WithoutDatabaseConfiguration_ShouldFailToStart()
    {
        // Skip in CI where ConnectionStrings__DefaultConnection is already set
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")) ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")))
        {
            // Test is not applicable in CI environment
            return;
        }

        // Arrange - Production without any database configuration
        var builder = new WebApplicationFactoryWithoutDatabase();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var factory = builder;
            var client = factory.CreateClient(); // This should trigger the exception
        });

        // Assert
        exception.Message.Should().Contain("PostgreSQL connection string is required in production");
        exception.Message.Should().Contain("DATABASE_URL");
        exception.Message.Should().Contain("POSTGRES_CONNECTION_STRING");
        exception.Message.Should().Contain("ConnectionStrings__DefaultConnection");
    }

    [Fact]
    [Trait("Category", "ProductionValidation")]
    [Trait("Environment", "LocalOnly")]
    public void Production_WithSQLiteConnectionString_ShouldFailToStart()
    {
        // Skip in CI where ConnectionStrings__DefaultConnection is already set
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")) ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")))
        {
            // Test is not applicable in CI environment
            return;
        }

        // Arrange - Production with SQLite connection string (should not be allowed)
        var builder = new WebApplicationFactoryWithSQLite();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var factory = builder;
            var client = factory.CreateClient(); // This should trigger the exception
        });

        // Assert
        exception.Message.Should().Contain("PostgreSQL connection string is required in production");
    }

    [Fact]
    [Trait("Category", "ProductionValidation")]
    public void Development_WithoutDatabaseConfiguration_ShouldStartWithSQLite()
    {
        // Arrange - Development without database configuration should work
        var builder = new WebApplicationFactoryDevelopment();

        // Act - Should not throw
        using var factory = builder;
        var client = factory.CreateClient();

        // Assert - Client should be created successfully
        client.Should().NotBeNull();
    }

    private class WebApplicationFactoryWithoutDatabase : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                // No database configuration provided
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Production",
                    ["JWT:Key"] = "test-super-secret-key-12345678901234567890123456789012",
                    ["JWT:Issuer"] = "TestIssuer",
                    ["JWT:Audience"] = "TestAudience",
                    ["ANTHROPIC_API_KEY"] = "test-api-key"
                });
            });
        }
    }

    private class WebApplicationFactoryWithSQLite : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Production",
                    ["ConnectionStrings:DefaultConnection"] = "Data Source=test.db", // SQLite connection string
                    ["JWT:Key"] = "test-super-secret-key-12345678901234567890123456789012",
                    ["JWT:Issuer"] = "TestIssuer",
                    ["JWT:Audience"] = "TestAudience",
                    ["ANTHROPIC_API_KEY"] = "test-api-key"
                });
            });
        }
    }

    private class WebApplicationFactoryDevelopment : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Development",
                    ["JWT:Key"] = "test-super-secret-key-12345678901234567890123456789012",
                    ["JWT:Issuer"] = "TestIssuer",
                    ["JWT:Audience"] = "TestAudience",
                    ["ANTHROPIC_API_KEY"] = "test-api-key"
                });
            });
        }
    }
}