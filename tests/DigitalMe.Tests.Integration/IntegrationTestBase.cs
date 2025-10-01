using DigitalMe.Data;
using DigitalMe.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

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
                // Remove ALL DbContext-related registrations to prevent provider conflicts
                // INCLUDING DbContextOptions<DigitalMeDbContext> which holds the provider configuration
                var descriptorsToRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<DigitalMeDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(DigitalMeDbContext) ||
                    d.ImplementationType == typeof(DigitalMeDbContext) ||
                    (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))).ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Add InMemory database for testing (clean slate - no provider conflicts)
                services.AddDbContext<DigitalMeDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    options.EnableSensitiveDataLogging(true);
                });

                // ⚡ OPTIMIZATION: Removed AddCleanArchitectureServices()
                // Services already registered in Program.cs - double registration caused slow startup

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