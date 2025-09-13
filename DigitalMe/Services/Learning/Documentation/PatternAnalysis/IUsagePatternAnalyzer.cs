using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalMe.Services.Learning;

namespace DigitalMe.Services.Learning.Documentation.PatternAnalysis;

/// <summary>
/// Interface for analyzing usage patterns from extracted code examples
/// Focused responsibility: Pattern recognition and analysis
/// Part of AutoDocumentationParser refactoring to resolve SRP violations
/// </summary>
public interface IUsagePatternAnalyzer
{
    /// <summary>
    /// Analyzes usage patterns from a collection of code examples
    /// </summary>
    /// <param name="examples">Code examples to analyze</param>
    /// <returns>Comprehensive usage pattern analysis</returns>
    Task<UsagePatternAnalysis> AnalyzeUsagePatternsAsync(List<CodeExample> examples);

    /// <summary>
    /// Identifies common patterns across multiple code examples
    /// </summary>
    /// <param name="examples">Code examples to analyze</param>
    /// <returns>List of identified common patterns</returns>
    Task<List<CommonPattern>> IdentifyCommonPatternsAsync(List<CodeExample> examples);
}