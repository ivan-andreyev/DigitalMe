using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using DigitalMe.Data;
using DigitalMe.Extensions;

namespace DigitalMe.Tests.Integration;

public abstract class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            
            builder.ConfigureServices(services =>
            {
                // Remove the default DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DigitalMeDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add InMemory database for testing
                services.AddDbContext<DigitalMeDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });

                // Add Clean Architecture services that were missing
                services.AddCleanArchitectureServices();

                // Reduce logging noise during tests
                services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
            });
        });

        Client = Factory.CreateClient();
    }

    protected Task<T> GetServiceAsync<T>() where T : class
    {
        using var scope = Factory.Services.CreateScope();
        return Task.FromResult(scope.ServiceProvider.GetRequiredService<T>());
    }

    protected async Task SeedDatabaseAsync(Func<DigitalMeDbContext, Task> seedAction)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
        await seedAction(context);
        await context.SaveChangesAsync();
    }
}