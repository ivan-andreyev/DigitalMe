using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalMe.Tests.Unit.Infrastructure;

public interface ITestServiceConfigurator
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
}