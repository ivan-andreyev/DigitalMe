using DigitalMe.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalMe.Tests.Unit.Infrastructure;

public class DatabaseServiceConfigurator : ITestServiceConfigurator
{
    private readonly string _databaseName;

    public DatabaseServiceConfigurator(string databaseName)
    {
        this._databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        RemoveExistingDbContext(services);
        this.AddInMemoryDbContext(services);
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
            options.UseInMemoryDatabase(databaseName: this._databaseName);
            options.EnableSensitiveDataLogging();
            options.EnableServiceProviderCaching(true);
        });
    }
}
