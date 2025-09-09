using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using DigitalMe.Tests.Unit.Infrastructure;

namespace DigitalMe.Tests.Unit.Controllers;

public class TestWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    private readonly ITestServiceConfigurator[] _serviceConfigurators;

    public TestWebApplicationFactory()
    {
        _serviceConfigurators = CreateDefaultConfigurators();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices((context, services) =>
        {
            foreach (var configurator in _serviceConfigurators)
            {
                configurator.ConfigureServices(services, context.Configuration);
            }
        });

        builder.UseEnvironment("Testing");
    }

    private static ITestServiceConfigurator[] CreateDefaultConfigurators()
    {
        var databaseName = $"TestDb_{Guid.NewGuid():N}";
        return
        [
            new DatabaseServiceConfigurator(databaseName),
            new DigitalMeServiceConfigurator()
        ];
    }
}
