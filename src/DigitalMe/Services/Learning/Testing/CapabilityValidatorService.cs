using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalMe.Services.Learning;
using DigitalMe.Services.Learning.Testing.ResultsAnalysis;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Learning.Testing;

/// <summary>
/// Dedicated capability validation service implementation
/// Focused on learned capability validation and performance benchmarking
/// Following Single Responsibility Principle
/// </summary>
public class CapabilityValidatorService : ICapabilityValidator
{
    private readonly ILogger<CapabilityValidatorService> _logger;
    private readonly IResultsAnalyzer _resultsAnalyzer;

    public CapabilityValidatorService(
        ILogger<CapabilityValidatorService> logger,
        IResultsAnalyzer resultsAnalyzer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resultsAnalyzer = resultsAnalyzer ?? throw new ArgumentNullException(nameof(resultsAnalyzer));
    }

    /// <inheritdoc />
    public async Task<CapabilityValidationResult> ValidateLearnedCapabilityAsync(string apiName, LearnedCapability capability)
    {
        _logger.LogDebug("Validating learned capability: {CapabilityName} for API: {ApiName}", capability?.Name ?? "Unknown", apiName);
        return await _resultsAnalyzer.ValidateLearnedCapabilityAsync(apiName, capability);
    }

    /// <inheritdoc />
    public async Task<PerformanceBenchmarkResult> BenchmarkNewSkillAsync(string skillName, List<TestExecutionResult> testResults)
    {
        _logger.LogDebug("Benchmarking new skill: {SkillName} with {TestCount} test results", skillName, testResults?.Count ?? 0);
        return await _resultsAnalyzer.BenchmarkNewSkillAsync(skillName, testResults);
    }
}