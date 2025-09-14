using DigitalMe.Data;
using DigitalMe.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Tests.Integration;

/// <summary>
/// Test fixture specifically for service-level integration tests (non-HTTP).
/// Sets up a lightweight DI container with all Ivan-Level services 
/// without the overhead of WebApplicationFactory.
/// </summary>
public class ServiceIntegrationTestFixture : IAsyncDisposable, IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }
    public IConfiguration Configuration { get; private set; } = null!;

    public ServiceIntegrationTestFixture()
    {
        ServiceProvider = CreateServiceProvider();
    }

    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        
        // Create test configuration
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=digitalme_test;Username=test;Password=test",
                ["Anthropic:ApiKey"] = "test-api-key",
                ["OpenAI:ApiKey"] = "sk-test1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef",
                ["TwoCaptcha:ApiKey"] = "0123456789abcdef0123456789abcdef",
                ["IvanProfile:DataFilePath"] = "C:\\Sources\\DigitalMe\\data\\profile\\IVAN_PROFILE_DATA.md",
                ["Voice:OpenAiApiKey"] = "sk-test1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef",
                ["Voice:DefaultTimeout"] = "30000",
                ["Voice:EnableDetailedLogging"] = "true",
                ["TwoCaptcha:DefaultTimeoutSeconds"] = "120",
                ["TwoCaptcha:DefaultPollingIntervalSeconds"] = "5"
            });

        Configuration = configBuilder.Build();
        services.AddSingleton(Configuration);

        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Add Entity Framework with in-memory database for testing
        services.AddDbContext<DigitalMeDbContext>(options =>
            options.UseInMemoryDatabase($"DigitalMeServiceTest_{Guid.NewGuid()}"));

        // Add all DigitalMe services using the extension method
        services.AddDigitalMeServices(Configuration);

        // Add Clean Architecture services (contains Learning Infrastructure Services)
        services.AddCleanArchitectureServices();

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();
        
        // Initialize database and seed Ivan's profile data
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
        context.Database.EnsureCreated();
        
        // Seed Ivan's personality data for integration tests
        DigitalMe.Data.Seeders.IvanDataSeeder.SeedBasicIvanProfile(context);

        return serviceProvider;
    }

    public async ValueTask DisposeAsync()
    {
        if (ServiceProvider is IAsyncDisposable asyncDisposableProvider)
        {
            await asyncDisposableProvider.DisposeAsync();
        }
        else if (ServiceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}