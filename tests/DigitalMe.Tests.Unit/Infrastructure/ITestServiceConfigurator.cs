using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace DigitalMe.Tests.Unit.Infrastructure;

public interface ITestServiceConfigurator
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
}