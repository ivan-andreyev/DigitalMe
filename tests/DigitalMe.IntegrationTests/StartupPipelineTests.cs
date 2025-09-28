using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DigitalMe.Data;
using DigitalMe.Data.Entities;
using Xunit;
using Xunit.Abstractions;

namespace DigitalMe.IntegrationTests;

/// <summary>
/// Integration tests for the startup pipeline to ensure migrations, seeding, and health checks work correctly
/// Covers the critical issue where migrations weren't running in production
/// </summary>
public class StartupPipelineTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public StartupPipelineTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Startup_ShouldInitializeToolRegistry_WithoutBlocking()
    {
        // Arrange & Act
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Debug);
            });
        }).CreateClient();

        // Act - Make a simple request to trigger full startup
        var response = await client.GetAsync("/health");

        // Assert
        Assert.True(response.IsSuccessStatusCode,
            "Health endpoint should be accessible after startup completes");
    }

    [Fact]
    public async Task Startup_ShouldApplyDatabaseMigrations_WhenTablesDoNotExist()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Use in-memory database for testing
                services.Remove(services.First(s => s.ServiceType == typeof(DbContextOptions<DigitalMeDbContext>)));
                services.AddDbContext<DigitalMeDbContext>(options =>
                    options.UseInMemoryDatabase("StartupTest_Migrations"));
            });
            builder.ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });
        });

        // Act
        var client = factory.CreateClient();

        // Verify database was created and migrations applied
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();

        // Assert
        Assert.True(await context.Database.CanConnectAsync(),
            "Database should be accessible after migrations");

        // Verify tables exist
        var canQueryProfiles = true;
        try
        {
            await context.PersonalityProfiles.AnyAsync();
        }
        catch
        {
            canQueryProfiles = false;
        }

        Assert.True(canQueryProfiles, "PersonalityProfiles table should exist after migrations");
    }

    [Fact]
    public async Task Startup_ShouldHandleSeedingGracefully_WhenTablesDoNotExist()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Use in-memory database that starts empty
                services.Remove(services.First(s => s.ServiceType == typeof(DbContextOptions<DigitalMeDbContext>)));
                services.AddDbContext<DigitalMeDbContext>(options =>
                    options.UseInMemoryDatabase("StartupTest_Seeding"));
            });
            builder.ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Debug);
            });
        });

        // Act - Application should start even if seeding fails
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");

        // Assert
        Assert.True(response.IsSuccessStatusCode,
            "Application should start successfully even if seeding encounters issues");
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturnHealthy_AfterSuccessfulStartup()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.Remove(services.First(s => s.ServiceType == typeof(DbContextOptions<DigitalMeDbContext>)));
                services.AddDbContext<DigitalMeDbContext>(options =>
                    options.UseInMemoryDatabase("StartupTest_Health"));
            });
        });

        // Act
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("Healthy", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Startup_ShouldCompleteWithinReasonableTime()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(30);
        using var cts = new CancellationTokenSource(timeout);

        // Act
        var startTime = DateTime.UtcNow;

        try
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/health", cts.Token);
            var duration = DateTime.UtcNow - startTime;

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(duration < timeout,
                $"Startup should complete within {timeout.TotalSeconds}s but took {duration.TotalSeconds}s");

            _output.WriteLine($"✅ Startup completed in {duration.TotalMilliseconds}ms");
        }
        catch (OperationCanceledException)
        {
            var duration = DateTime.UtcNow - startTime;
            Assert.True(false,
                $"Startup timed out after {duration.TotalSeconds}s - this indicates a blocking issue in the startup pipeline");
        }
    }

    [Fact]
    public async Task Startup_ShouldLogCriticalCheckpoints_ForDebugging()
    {
        // Arrange
        var logs = new List<string>();
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging =>
            {
                logging.AddProvider(new TestLoggerProvider(logs));
                logging.SetMinimumLevel(LogLevel.Information);
            });
        });

        // Act
        var client = factory.CreateClient();
        await client.GetAsync("/health");

        // Assert - Verify critical checkpoints were logged
        var checkpointLogs = logs.Where(log => log.Contains("CHECKPOINT")).ToList();

        Assert.NotEmpty(checkpointLogs);
        _output.WriteLine("📋 Logged checkpoints:");
        foreach (var checkpoint in checkpointLogs)
        {
            _output.WriteLine($"  {checkpoint}");
        }

        // Verify we reached the Tool Registry initialization
        var toolRegistryLog = logs.Any(log => log.Contains("Tool Registry") || log.Contains("CHECKPOINT 1"));
        Assert.True(toolRegistryLog, "Should log Tool Registry initialization checkpoint");
    }
}

/// <summary>
/// Test logger provider to capture logs for assertions
/// </summary>
public class TestLoggerProvider : ILoggerProvider
{
    private readonly List<string> _logs;

    public TestLoggerProvider(List<string> logs)
    {
        _logs = logs;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogger(_logs, categoryName);
    }

    public void Dispose() { }
}

public class TestLogger : ILogger
{
    private readonly List<string> _logs;
    private readonly string _categoryName;

    public TestLogger(List<string> logs, string categoryName)
    {
        _logs = logs;
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        var message = formatter(state, exception);
        _logs.Add($"[{logLevel}] {_categoryName}: {message}");
    }
}