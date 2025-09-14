using System.Collections.Generic;
using System.Threading.Tasks;

namespace DigitalMe.Services.Learning.Testing.TestGeneration;

/// <summary>
/// Interface for test case generation service
/// Responsible for generating various types of test cases from API documentation
/// Single Responsibility: Test case generation logic only
/// </summary>
public interface ITestCaseGenerator
{
    /// <summary>
    /// Generate test cases based on API documentation
    /// </summary>
    /// <param name="apiDocumentation">Parsed API documentation</param>
    /// <returns>List of generated test cases</returns>
    Task<List<SelfGeneratedTestCase>> GenerateTestCasesAsync(DocumentationParseResult apiDocumentation);

    /// <summary>
    /// Generate test cases for a specific API endpoint
    /// </summary>
    /// <param name="apiDoc">API documentation context</param>
    /// <param name="endpoint">Specific endpoint to generate tests for</param>
    /// <returns>List of endpoint-specific test cases</returns>
    Task<List<SelfGeneratedTestCase>> GenerateEndpointTestCasesAsync(DocumentationParseResult apiDoc, ApiEndpoint endpoint);

    /// <summary>
    /// Generate test cases based on documentation examples
    /// </summary>
    /// <param name="apiDoc">API documentation with examples</param>
    /// <returns>List of example-based test cases</returns>
    Task<List<SelfGeneratedTestCase>> GenerateExampleBasedTestCasesAsync(DocumentationParseResult apiDoc);

    /// <summary>
    /// Generate error handling test cases
    /// </summary>
    /// <param name="apiDoc">API documentation context</param>
    /// <returns>List of error handling test cases</returns>
    List<SelfGeneratedTestCase> GenerateErrorHandlingTestCases(DocumentationParseResult apiDoc);

    /// <summary>
    /// Generate authentication-specific test cases
    /// </summary>
    /// <param name="apiDoc">API documentation with authentication info</param>
    /// <returns>List of authentication test cases</returns>
    List<SelfGeneratedTestCase> GenerateAuthenticationTestCases(DocumentationParseResult apiDoc);
}