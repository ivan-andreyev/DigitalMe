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

        if (capability == null)
        {
            _logger.LogWarning("Cannot validate capability: capability is null for API {ApiName}", apiName);
            return new CapabilityValidationResult
            {
                CapabilityName = apiName,
                IsValid = false,
                ConfidenceScore = 0.0,
                ImprovementSuggestions = new List<string> { "Capability is null" }
            };
        }

        return await _resultsAnalyzer.ValidateLearnedCapabilityAsync(apiName, capability);
    }

    /// <inheritdoc />
    public async Task<PerformanceBenchmarkResult> BenchmarkNewSkillAsync(string skillName, List<TestExecutionResult> testResults)
    {
        _logger.LogDebug("Benchmarking new skill: {SkillName} with {TestCount} test results", skillName, testResults?.Count ?? 0);

        if (testResults == null)
        {
            _logger.LogWarning("Cannot benchmark skill: testResults list is null for skill {SkillName}", skillName);
            return new PerformanceBenchmarkResult
            {
                SkillName = skillName,
                SuccessRate = 0.0,
                Grade = PerformanceGrade.F,
                PerformanceRecommendations = new List<string> { "Test results list is null" }
            };
        }

        return await _resultsAnalyzer.BenchmarkNewSkillAsync(skillName, testResults);
    }
}