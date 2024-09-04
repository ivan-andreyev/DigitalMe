using Microsoft.Extensions.Options;
using DigitalMe.Web.Models;
using System.Threading;

namespace DigitalMe.Web.Services;

public class RuntimeConfigurationService : IHostedService
{
    private readonly ILogger<RuntimeConfigurationService> _logger;
    private readonly ThreadPoolSettings _threadPoolSettings;

    public RuntimeConfigurationService(
        ILogger<RuntimeConfigurationService> logger, 
        IOptions<ThreadPoolSettings> threadPoolSettings)
    {
        _logger = logger;
        _threadPoolSettings = threadPoolSettings.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Configure ThreadPool settings
            ThreadPool.SetMinThreads(_threadPoolSettings.MinWorkerThreads, _threadPoolSettings.MinCompletionPortThreads);
            ThreadPool.SetMaxThreads(_threadPoolSettings.MaxWorkerThreads, _threadPoolSettings.MaxCompletionPortThreads);

            // Verify the settings were applied
            ThreadPool.GetMinThreads(out int minWorkerThreads, out int minCompletionPortThreads);
            ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);

            _logger.LogInformation("ThreadPool configuration applied successfully:");
            _logger.LogInformation("Min Worker Threads: {MinWorkerThreads}, Min I/O Threads: {MinCompletionPortThreads}", 
                minWorkerThreads, minCompletionPortThreads);
            _logger.LogInformation("Max Worker Threads: {MaxWorkerThreads}, Max I/O Threads: {MaxCompletionPortThreads}", 
                maxWorkerThreads, maxCompletionPortThreads);
            _logger.LogInformation("Processor Count: {ProcessorCount}", Environment.ProcessorCount);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure ThreadPool settings");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}