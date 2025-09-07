using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DigitalMe.Data;
using DigitalMe.Services;
using DigitalMe.Repositories;
using DigitalMe.Extensions;

namespace DigitalMe.Tests.Unit.Controllers;

public class TestWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    // Use a unique database name per factory instance to ensure complete isolation
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices((context, services) =>
        {
            // Remove the app's DbContext registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DigitalMeDbContext>));
            
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Remove the DbContext itself
            var dbContextServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DigitalMeDbContext));
            
            if (dbContextServiceDescriptor != null)
            {
                services.Remove(dbContextServiceDescriptor);
            }

            // Add DbContext using an in-memory database for testing with shared database name per test class
            services.AddDbContext<DigitalMeDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName: _databaseName);
                options.EnableSensitiveDataLogging();
            });

            // Register DigitalMe services using the same extension method as production
            // This ensures test environment matches production service registrations
            services.AddDigitalMeServices(context.Configuration);
        });

        builder.UseEnvironment("Testing");
    }
}