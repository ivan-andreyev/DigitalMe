using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DigitalMe.Data;
using DigitalMe.Services;
using DigitalMe.Repositories;

namespace DigitalMe.Tests.Unit.Controllers;

public class TestWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
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

            // Add DbContext using an in-memory database for testing
            services.AddDbContext<DigitalMeDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName: "InMemoryDbForTesting");
                options.EnableSensitiveDataLogging();
            });
        });

        builder.UseEnvironment("Testing");
    }
}