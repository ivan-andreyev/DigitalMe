using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using DigitalMe.Data;

namespace DigitalMe.Tests.Unit.Infrastructure;

public class DatabaseServiceConfigurator : ITestServiceConfigurator
{
    private readonly string _databaseName;

    public DatabaseServiceConfigurator(string databaseName)
    {
        _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        RemoveExistingDbContext(services);
        AddInMemoryDbContext(services);
    }

    private static void RemoveExistingDbContext(IServiceCollection services)
    {
        var dbContextDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<DigitalMeDbContext>));

        if (dbContextDescriptor != null)
        {
            services.Remove(dbContextDescriptor);
        }

        var dbContextServiceDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DigitalMeDbContext));

        if (dbContextServiceDescriptor != null)
        {
            services.Remove(dbContextServiceDescriptor);
        }
    }

    private void AddInMemoryDbContext(IServiceCollection services)
    {
        services.AddDbContext<DigitalMeDbContext>(options =>
        {
            options.UseInMemoryDatabase(databaseName: _databaseName);
            options.EnableSensitiveDataLogging();
        });
    }
}