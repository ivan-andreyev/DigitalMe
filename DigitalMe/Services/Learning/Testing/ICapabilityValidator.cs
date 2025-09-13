using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalMe.Services.Learning;

namespace DigitalMe.Services.Learning.Testing;

/// <summary>
/// Capability validation interface - Phase 1 Advanced Cognitive Capabilities
/// Handles learned capability validation and performance benchmarking
/// Following ISP: ≤5 methods focused on validation concerns
/// </summary>
public interface ICapabilityValidator
{
    /// <summary>
    /// Validate learned capabilities by running comprehensive tests
    /// </summary>
    Task<CapabilityValidationResult> ValidateLearnedCapabilityAsync(string apiName, LearnedCapability capability);

    /// <summary>
    /// Generate performance benchmarks for new skills
    /// </summary>
    Task<PerformanceBenchmarkResult> BenchmarkNewSkillAsync(string skillName, List<TestExecutionResult> testResults);
}