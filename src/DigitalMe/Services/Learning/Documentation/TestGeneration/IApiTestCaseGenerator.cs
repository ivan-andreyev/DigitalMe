using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalMe.Services.Learning;

namespace DigitalMe.Services.Learning.Documentation.TestGeneration;

/// <summary>
/// Interface for generating API test cases from usage pattern analysis
/// Focused responsibility: Test case generation based on learned patterns
/// Part of AutoDocumentationParser refactoring to resolve SRP violations
/// </summary>
public interface IApiTestCaseGenerator
{
    /// <summary>
    /// Generates test cases based on analyzed usage patterns
    /// </summary>
    /// <param name="patterns">Usage pattern analysis results</param>
    /// <returns>List of generated test cases</returns>
    Task<List<GeneratedTestCase>> GenerateTestCasesAsync(UsagePatternAnalysis patterns);
}