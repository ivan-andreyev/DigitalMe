using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DigitalMe.Services.Tools;
using DigitalMe.Services.Tools.Strategies;

namespace DigitalMe.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that properly initializes Tool Registry for integration tests.
/// Registers only working tool strategies to avoid dependency issues in tests.
/// </summary>
public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove problematic tool strategies that have external dependencies
            var serviceDescriptors = services.Where(s => s.ServiceType == typeof(IToolStrategy)).ToList();
            foreach (var descriptor in serviceDescriptors)
            {
                services.Remove(descriptor);
            }
            
            // Add only the MemoryToolStrategy which has minimal dependencies (only logging)
            services.AddScoped<IToolStrategy, MemoryToolStrategy>();
        });
    }
    
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        
        // Initialize Tool Registry after the host is created (same as Program.cs)
        try
        {
            using var toolScope = host.Services.CreateScope();
            var toolRegistry = toolScope.ServiceProvider.GetRequiredService<IToolRegistry>();
            var toolStrategies = toolScope.ServiceProvider.GetServices<IToolStrategy>();
            
            foreach (var strategy in toolStrategies)
            {
                toolRegistry.RegisterTool(strategy);
            }
            
            var appLogger = host.Services.GetService<ILogger<CustomWebApplicationFactory<TStartup>>>();
            appLogger?.LogInformation("🔧 TEST TOOL REGISTRY INITIALIZED with {Count} strategies", toolStrategies.Count());
        }
        catch (Exception ex)
        {
            var appLogger = host.Services.GetService<ILogger<CustomWebApplicationFactory<TStartup>>>();
            appLogger?.LogError(ex, "❌ Failed to initialize Test Tool Registry");
        }
        
        return host;
    }
}