using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DigitalMe.Services.Learning.Testing;

namespace DigitalMe.Services.Learning.ErrorLearning.Integration;

/// <summary>
/// Factory for creating test orchestrators with optional learning capabilities
/// Provides flexibility to enable or disable error learning integration
/// </summary>
public class TestOrchestratorFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TestOrchestratorFactory> _logger;

    public TestOrchestratorFactory(
        IServiceProvider serviceProvider,
        ILogger<TestOrchestratorFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a test orchestrator with learning capabilities enabled
    /// </summary>
    /// <returns>Learning-enabled test orchestrator</returns>
    public ITestOrchestrator CreateLearningEnabledOrchestrator()
    {
        _logger.LogDebug("Creating learning-enabled test orchestrator");

        var baseOrchestrator = _serviceProvider.GetRequiredService<TestOrchestratorService>();
        var testFailureCapture = _serviceProvider.GetRequiredService<ITestFailureCapture>();
        var logger = _serviceProvider.GetRequiredService<ILogger<LearningEnabledTestOrchestrator>>();

        return new LearningEnabledTestOrchestrator(logger, baseOrchestrator, testFailureCapture);
    }

    /// <summary>
    /// Creates a basic test orchestrator without learning capabilities
    /// </summary>
    /// <returns>Basic test orchestrator</returns>
    public ITestOrchestrator CreateBasicOrchestrator()
    {
        _logger.LogDebug("Creating basic test orchestrator");
        return _serviceProvider.GetRequiredService<TestOrchestratorService>();
    }

    /// <summary>
    /// Creates appropriate orchestrator based on configuration
    /// </summary>
    /// <param name="enableLearning">Whether to enable error learning</param>
    /// <returns>Configured test orchestrator</returns>
    public ITestOrchestrator CreateOrchestrator(bool enableLearning = true)
    {
        return enableLearning
            ? CreateLearningEnabledOrchestrator()
            : CreateBasicOrchestrator();
    }
}