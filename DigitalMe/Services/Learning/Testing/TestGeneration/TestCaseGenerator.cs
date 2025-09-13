using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalMe.Services.Learning.Testing.TestGeneration;

/// <summary>
/// Implementation of test case generation service
/// Responsible for generating various types of test cases from API documentation
/// Follows Single Responsibility Principle - only handles test case generation
/// </summary>
public class TestCaseGenerator : ITestCaseGenerator
{
    private readonly ILogger<TestCaseGenerator> _logger;

    public TestCaseGenerator(ILogger<TestCaseGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<List<SelfGeneratedTestCase>> GenerateTestCasesAsync(DocumentationParseResult apiDocumentation)
    {
        try
        {
            _logger.LogInformation("Generating test cases for API: {ApiName}", apiDocumentation.ApiName);

            var testCases = new List<SelfGeneratedTestCase>();

            // Generate test cases for each endpoint
            foreach (var endpoint in apiDocumentation.Endpoints)
            {
                testCases.AddRange(await GenerateEndpointTestCasesAsync(apiDocumentation, endpoint));
            }

            // Generate integration test cases based on examples
            testCases.AddRange(await GenerateExampleBasedTestCasesAsync(apiDocumentation));

            // Generate error handling test cases
            testCases.AddRange(GenerateErrorHandlingTestCases(apiDocumentation));

            // Generate authentication test cases
            if (apiDocumentation.Authentication != AuthenticationMethod.None)
            {
                testCases.AddRange(GenerateAuthenticationTestCases(apiDocumentation));
            }

            _logger.LogInformation("Generated {TestCaseCount} test cases for API: {ApiName}", 
                testCases.Count, apiDocumentation.ApiName);

            return testCases;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating test cases for API: {ApiName}", apiDocumentation.ApiName);
            return new List<SelfGeneratedTestCase>();
        }
    }

    /// <inheritdoc />
    public async Task<List<SelfGeneratedTestCase>> GenerateEndpointTestCasesAsync(
        DocumentationParseResult apiDoc, ApiEndpoint endpoint)
    {
        var testCases = new List<SelfGeneratedTestCase>();

        // Happy path test
        var happyPathTest = new SelfGeneratedTestCase
        {
            Name = $"{endpoint.Method}_{endpoint.Path.Replace("/", "_")}_HappyPath",
            Description = $"Test successful {endpoint.Method} request to {endpoint.Path}",
            ApiName = apiDoc.ApiName,
            Endpoint = endpoint.Path,
            HttpMethod = endpoint.Method,
            Priority = TestPriority.High,
            Headers = TestCaseGeneratorHelpers.CreateStandardHeaders(apiDoc)
        };

        // Add required parameters
        foreach (var param in endpoint.Parameters.Where(p => p.Required))
        {
            happyPathTest.Parameters[param.Name] = TestCaseGeneratorHelpers.GenerateTestValue(param);
        }

        // Add standard assertions
        happyPathTest.Assertions.Add(new TestAssertion
        {
            Name = "Status Code Success",
            Type = AssertionType.StatusCode,
            ExpectedValue = "200,201,202",
            Operator = ComparisonOperator.Contains
        });

        testCases.Add(happyPathTest);

        // Parameter validation tests
        foreach (var param in endpoint.Parameters.Where(p => p.Required))
        {
            var paramTest = new SelfGeneratedTestCase
            {
                Name = $"{endpoint.Method}_{endpoint.Path.Replace("/", "_")}_Missing_{param.Name}",
                Description = $"Test {endpoint.Method} request with missing required parameter {param.Name}",
                ApiName = apiDoc.ApiName,
                Endpoint = endpoint.Path,
                HttpMethod = endpoint.Method,
                Priority = TestPriority.Medium,
                Headers = TestCaseGeneratorHelpers.CreateStandardHeaders(apiDoc)
            };

            // Add other required parameters but skip this one
            foreach (var otherParam in endpoint.Parameters.Where(p => p.Required && p.Name != param.Name))
            {
                paramTest.Parameters[otherParam.Name] = TestCaseGeneratorHelpers.GenerateTestValue(otherParam);
            }

            paramTest.Assertions.Add(new TestAssertion
            {
                Name = "Bad Request Status",
                Type = AssertionType.StatusCode,
                ExpectedValue = "400",
                Operator = ComparisonOperator.Equals
            });

            testCases.Add(paramTest);
        }

        return testCases;
    }

    /// <inheritdoc />
    public async Task<List<SelfGeneratedTestCase>> GenerateExampleBasedTestCasesAsync(DocumentationParseResult apiDoc)
    {
        var testCases = new List<SelfGeneratedTestCase>();

        foreach (var example in apiDoc.Examples.Take(5)) // Limit to 5 examples
        {
            var testCase = new SelfGeneratedTestCase
            {
                Name = $"Example_Based_Test_{testCases.Count + 1}",
                Description = $"Test based on documentation example: {example.Description}",
                ApiName = apiDoc.ApiName,
                Endpoint = example.Endpoint,
                Priority = TestPriority.High,
                Headers = TestCaseGeneratorHelpers.CreateStandardHeaders(apiDoc)
            };

            // Extract method from example code
            testCase.HttpMethod = TestCaseGeneratorHelpers.ExtractHttpMethod(example.Code);

            // Use extracted values as parameters
            testCase.Parameters = example.ExtractedValues.ToDictionary(
                kv => kv.Key, 
                kv => kv.Value);

            // Add response validation
            testCase.Assertions.Add(new TestAssertion
            {
                Name = "Response Received",
                Type = AssertionType.StatusCode,
                ExpectedValue = "200,201,202",
                Operator = ComparisonOperator.Contains
            });

            testCases.Add(testCase);
        }

        return testCases;
    }

    /// <inheritdoc />
    public List<SelfGeneratedTestCase> GenerateErrorHandlingTestCases(DocumentationParseResult apiDoc)
    {
        var testCases = new List<SelfGeneratedTestCase>();

        // Unauthorized test
        if (apiDoc.Authentication != AuthenticationMethod.None)
        {
            testCases.Add(new SelfGeneratedTestCase
            {
                Name = "Unauthorized_Access_Test",
                Description = "Test API response when authentication is missing",
                ApiName = apiDoc.ApiName,
                Endpoint = apiDoc.Endpoints.FirstOrDefault()?.Path ?? "/",
                HttpMethod = "GET",
                Priority = TestPriority.High,
                // Intentionally no auth headers
                Assertions = new List<TestAssertion>
                {
                    new TestAssertion
                    {
                        Name = "Unauthorized Status",
                        Type = AssertionType.StatusCode,
                        ExpectedValue = "401",
                        Operator = ComparisonOperator.Equals
                    }
                }
            });
        }

        // Not Found test
        testCases.Add(new SelfGeneratedTestCase
        {
            Name = "Not_Found_Test",
            Description = "Test API response for non-existent endpoint",
            ApiName = apiDoc.ApiName,
            Endpoint = "/non-existent-endpoint",
            HttpMethod = "GET",
            Priority = TestPriority.Medium,
            Headers = TestCaseGeneratorHelpers.CreateStandardHeaders(apiDoc),
            Assertions = new List<TestAssertion>
            {
                new TestAssertion
                {
                    Name = "Not Found Status",
                    Type = AssertionType.StatusCode,
                    ExpectedValue = "404",
                    Operator = ComparisonOperator.Equals
                }
            }
        });

        return testCases;
    }

    /// <inheritdoc />
    public List<SelfGeneratedTestCase> GenerateAuthenticationTestCases(DocumentationParseResult apiDoc)
    {
        var testCases = new List<SelfGeneratedTestCase>();

        // Valid authentication test
        testCases.Add(new SelfGeneratedTestCase
        {
            Name = "Valid_Authentication_Test",
            Description = "Test API with valid authentication",
            ApiName = apiDoc.ApiName,
            Endpoint = apiDoc.Endpoints.FirstOrDefault()?.Path ?? "/",
            HttpMethod = "GET",
            Priority = TestPriority.Critical,
            Headers = TestCaseGeneratorHelpers.CreateStandardHeaders(apiDoc),
            Assertions = new List<TestAssertion>
            {
                new TestAssertion
                {
                    Name = "Authentication Success",
                    Type = AssertionType.StatusCode,
                    ExpectedValue = "401",
                    Operator = ComparisonOperator.NotEquals
                }
            }
        });

        return testCases;
    }

}