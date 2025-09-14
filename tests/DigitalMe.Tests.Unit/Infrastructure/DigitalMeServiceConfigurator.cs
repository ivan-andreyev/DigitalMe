using DigitalMe.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalMe.Tests.Unit.Infrastructure;

public class DigitalMeServiceConfigurator : ITestServiceConfigurator
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register DigitalMe services using the same extension method as production
        // This ensures test environment matches production service registrations
        services.AddDigitalMeServices(configuration);
    }
}