using System.Collections.Generic;
using System.Threading.Tasks;

namespace DigitalMe.Services.Learning;

/// <summary>
/// Interface for auto-documentation parsing service - Phase 1 Advanced Cognitive Capabilities
/// Enables agent to automatically learn new APIs by analyzing documentation
/// </summary>
public interface IAutoDocumentationParser
{
    /// <summary>
    /// Parse API documentation and extract usage patterns
    /// </summary>
    Task<DocumentationParseResult> ParseApiDocumentationAsync(string documentationUrl, string apiName);
    
    /// <summary>
    /// Extract code examples from documentation and analyze patterns
    /// </summary>
    Task<List<CodeExample>> ExtractCodeExamplesAsync(string documentationContent);
    
    /// <summary>
    /// Analyze usage patterns from extracted examples
    /// </summary>
    Task<UsagePatternAnalysis> AnalyzeUsagePatternsAsync(List<CodeExample> examples);
    
    /// <summary>
    /// Generate test cases based on discovered patterns
    /// </summary>
    Task<List<GeneratedTestCase>> GenerateTestCasesAsync(UsagePatternAnalysis patterns);
}

/// <summary>
/// Result of documentation parsing operation
/// </summary>
public class DocumentationParseResult
{
    public string ApiName { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public List<ApiEndpoint> Endpoints { get; set; } = new();
    public List<CodeExample> Examples { get; set; } = new();
    public Dictionary<string, string> Configuration { get; set; } = new();
    public List<string> RequiredHeaders { get; set; } = new();
    public AuthenticationMethod Authentication { get; set; } = AuthenticationMethod.None;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ParsedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents an API endpoint discovered in documentation
/// </summary>
public class ApiEndpoint
{
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ApiParameter> Parameters { get; set; } = new();
    public string ResponseFormat { get; set; } = string.Empty;
    public List<string> RequiredPermissions { get; set; } = new();
}

/// <summary>
/// Parameter for an API endpoint
/// </summary>
public class ApiParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Required { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public List<string> AllowedValues { get; set; } = new();
}

/// <summary>
/// Code example extracted from documentation
/// </summary>
public class CodeExample
{
    public string Language { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public Dictionary<string, object> ExtractedValues { get; set; } = new();
}

/// <summary>
/// Analysis of usage patterns from code examples
/// </summary>
public class UsagePatternAnalysis
{
    public List<CommonPattern> Patterns { get; set; } = new();
    public Dictionary<string, int> MethodFrequency { get; set; } = new();
    public List<string> CommonHeaders { get; set; } = new();
    public List<ParameterPattern> ParameterPatterns { get; set; } = new();
    public ErrorHandlingPattern ErrorHandling { get; set; } = new();
}

/// <summary>
/// Common usage pattern identified in examples
/// </summary>
public class CommonPattern
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Frequency { get; set; }
    public List<string> Steps { get; set; } = new();
    public Dictionary<string, string> TypicalValues { get; set; } = new();
}

/// <summary>
/// Pattern for how parameters are typically used
/// </summary>
public class ParameterPattern
{
    public string ParameterName { get; set; } = string.Empty;
    public List<string> TypicalValues { get; set; } = new();
    public string ValidationPattern { get; set; } = string.Empty;
    public bool RequiredInMostCases { get; set; }
}

/// <summary>
/// Error handling patterns discovered in documentation
/// </summary>
public class ErrorHandlingPattern
{
    public List<string> CommonErrorCodes { get; set; } = new();
    public Dictionary<string, string> ErrorMessages { get; set; } = new();
    public List<string> RetryStrategies { get; set; } = new();
}

/// <summary>
/// Test case generated based on learned patterns
/// </summary>
public class GeneratedTestCase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public Dictionary<string, string> Headers { get; set; } = new();
    public string ExpectedResponsePattern { get; set; } = string.Empty;
    public List<string> ValidationSteps { get; set; } = new();
}

/// <summary>
/// Authentication method for the API
/// </summary>
public enum AuthenticationMethod
{
    None,
    ApiKey,
    Bearer,
    Basic,
    OAuth,
    Custom
}