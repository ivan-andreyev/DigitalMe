using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Xunit;
using DigitalMe.Data;

namespace DigitalMe.Tests.Integration;

/// <summary>
/// TDD Tests to ensure correct database provider selection in different environments
/// These tests prevent SQLite from being used in production when PostgreSQL should be used
/// </summary>
public class DatabaseProviderSelectionTests : IClassFixture<DatabaseProviderSelectionTests.TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public DatabaseProviderSelectionTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "DatabaseProvider")]
    public async Task Production_WithPostgresConnectionString_ShouldUsePostgreSQL()
    {
        // Arrange
        var postgresConnectionString = "Host=localhost;Database=digitalme;Username=postgres;Password=postgres";

        using var factory = new TestWebApplicationFactory()
            .WithEnvironment("Production")
            .WithConnectionString(postgresConnectionString);

        // Act
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
        var providerName = context.Database.ProviderName;

        // Assert
        providerName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL",
            "Production environment with PostgreSQL connection string should use PostgreSQL provider");
    }

    [Fact]
    [Trait("Category", "DatabaseProvider")]
    public async Task Production_WithCloudSqlConnectionString_ShouldUsePostgreSQL()
    {
        // Arrange - Cloud SQL format connection string
        var cloudSqlConnectionString = "Host=/cloudsql/project:region:instance;Database=digitalme;Username=postgres;Password=postgres";

        using var factory = new TestWebApplicationFactory()
            .WithEnvironment("Production")
            .WithConnectionString(cloudSqlConnectionString);

        // Act
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
        var providerName = context.Database.ProviderName;

        // Assert
        providerName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL",
            "Production environment with Cloud SQL connection string should use PostgreSQL provider");
    }

    [Fact]
    [Trait("Category", "DatabaseProvider")]
    public async Task Production_WithDatabaseUrlEnvironmentVariable_ShouldUsePostgreSQL()
    {
        // Arrange - DATABASE_URL format (common in cloud environments)
        var databaseUrl = "postgresql://postgres:password@localhost:5432/digitalme";

        using var factory = new TestWebApplicationFactory()
            .WithEnvironment("Production")
            .WithEnvironmentVariable("DATABASE_URL", databaseUrl);

        // Act
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
        var providerName = context.Database.ProviderName;

        // Assert
        providerName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL",
            "Production environment with DATABASE_URL should use PostgreSQL provider");
    }

    [Fact]
    [Trait("Category", "DatabaseProvider")]
    public async Task Production_WithoutConnectionString_ShouldThrowException()
    {
        // Arrange - Production without any database configuration
        using var factory = new TestWebApplicationFactory()
            .WithEnvironment("Production")
            .WithConnectionString(null)
            .WithSkipTestingCheck();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
        });

        exception.Message.Should().Contain("PostgreSQL connection string is required in production",
            "Production environment should fail fast without PostgreSQL configuration");
    }

    [Fact]
    [Trait("Category", "DatabaseProvider")]
    public async Task Development_WithoutConnectionString_ShouldUseSQLite()
    {
        // Arrange - Development environment without connection string
        using var factory = new TestWebApplicationFactory()
            .WithEnvironment("Development")
            .WithConnectionString(null)
            .WithSkipTestingCheck();

        // Act
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
        var providerName = context.Database.ProviderName;

        // Assert
        providerName.Should().Be("Microsoft.EntityFrameworkCore.Sqlite",
            "Development environment without connection string should use SQLite as fallback");
    }

    [Fact]
    [Trait("Category", "DatabaseProvider")]
    public async Task Development_WithPostgresConnectionString_ShouldUsePostgreSQL()
    {
        // Arrange - Development can also use PostgreSQL if configured
        var postgresConnectionString = "Host=localhost;Database=digitalme_dev;Username=postgres;Password=postgres";

        using var factory = new TestWebApplicationFactory()
            .WithEnvironment("Development")
            .WithConnectionString(postgresConnectionString);

        // Act
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
        var providerName = context.Database.ProviderName;

        // Assert
        providerName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL",
            "Development environment with PostgreSQL connection string should use PostgreSQL provider");
    }

    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private string? _environment = "Testing";
        private string? _connectionString;
        private readonly Dictionary<string, string?> _environmentVariables = new();
        private readonly List<string> _setEnvironmentVariables = new();

        public TestWebApplicationFactory WithEnvironment(string environment)
        {
            _environment = environment;
            return this;
        }

        public TestWebApplicationFactory WithConnectionString(string? connectionString)
        {
            _connectionString = connectionString;
            return this;
        }

        public TestWebApplicationFactory WithEnvironmentVariable(string key, string value)
        {
            _environmentVariables[key] = value;
            return this;
        }

        public TestWebApplicationFactory WithSkipTestingCheck()
        {
            _environmentVariables["SKIP_TESTING_CHECK"] = "true";
            return this;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(_environment);

            // Clean up any environment variables from previous tests FIRST
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
            Environment.SetEnvironmentVariable("DATABASE_URL", null);
            Environment.SetEnvironmentVariable("POSTGRES_CONNECTION_STRING", null);
            Environment.SetEnvironmentVariable("SKIP_TESTING_CHECK", null);

            // Set actual environment variables (DatabaseProviderConfigurator checks these FIRST)
            // Always set to clear any previous test's value (null clears it)
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _connectionString);
            _setEnvironmentVariables.Add("ConnectionStrings__DefaultConnection");

            foreach (var kvp in _environmentVariables)
            {
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
                _setEnvironmentVariables.Add(kvp.Key);
            }

            builder.ConfigureAppConfiguration((context, config) =>
            {
                var configValues = new Dictionary<string, string?>
                {
                    ["ASPNETCORE_ENVIRONMENT"] = _environment,
                    ["JWT:Key"] = "test-super-secret-key-12345678901234567890123456789012",
                    ["JWT:Issuer"] = "TestIssuer",
                    ["JWT:Audience"] = "TestAudience",
                    ["JWT:ExpireHours"] = "24",
                    ["ANTHROPIC_API_KEY"] = "test-api-key"
                };

                config.AddInMemoryCollection(configValues);
            });

            // NO ConfigureServices override!
            // Program.cs DatabaseProviderConfigurator will handle database provider selection
            // based on environment and configuration we set above
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Clean up environment variables to avoid test pollution
                foreach (var varName in _setEnvironmentVariables)
                {
                    Environment.SetEnvironmentVariable(varName, null);
                }
            }

            base.Dispose(disposing);
        }
    }
}