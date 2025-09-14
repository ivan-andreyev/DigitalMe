using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalMe.Services.Learning;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Learning.Documentation.TestGeneration;

/// <summary>
/// API test case generator implementation
/// Single responsibility: Generate test cases from analyzed usage patterns
/// Extracted from AutoDocumentationParser to resolve SRP violations
/// </summary>
public class ApiTestCaseGenerator : IApiTestCaseGenerator
{
    private readonly ILogger<ApiTestCaseGenerator> _logger;

    public ApiTestCaseGenerator(ILogger<ApiTestCaseGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<List<GeneratedTestCase>> GenerateTestCasesAsync(UsagePatternAnalysis patterns)
    {
        var testCases = new List<GeneratedTestCase>();

        try
        {
            _logger.LogInformation("Generating test cases based on {PatternCount} identified patterns", patterns.Patterns.Count);

            // Generate test cases from patterns
            foreach (var pattern in patterns.Patterns)
            {
                var testCase = new GeneratedTestCase
                {
                    Name = $"Test_{pattern.Name.Replace(" ", "_")}",
                    Description = $"Test case for {pattern.Description}",
                    Method = InferMethodFromPattern(pattern),
                    Headers = patterns.CommonHeaders.ToDictionary(h => h, _ => "test_value"),
                    Parameters = pattern.TypicalValues.ToDictionary(kv => kv.Key, kv => (object)kv.Value),
                    ExpectedResponsePattern = InferResponsePattern(pattern),
                    ValidationSteps = GenerateValidationSteps(pattern)
                };

                testCases.Add(testCase);
                _logger.LogDebug("Generated test case: {TestCaseName}", testCase.Name);
            }

            // Generate method-specific test cases
            foreach (var methodFreq in patterns.MethodFrequency)
            {
                var methodTestCase = new GeneratedTestCase
                {
                    Name = $"Test_{methodFreq.Key}_Request",
                    Description = $"Test {methodFreq.Key} request functionality",
                    Method = methodFreq.Key,
                    Headers = patterns.CommonHeaders.ToDictionary(h => h, h => GetTypicalHeaderValue(h)),
                    Parameters = GetTypicalParametersForMethod(methodFreq.Key, patterns),
                    ExpectedResponsePattern = GetExpectedResponseForMethod(methodFreq.Key),
                    ValidationSteps = GetValidationStepsForMethod(methodFreq.Key)
                };

                testCases.Add(methodTestCase);
            }

            // Generate parameter validation test cases
            foreach (var paramPattern in patterns.ParameterPatterns)
            {
                var paramTestCase = new GeneratedTestCase
                {
                    Name = $"Test_{paramPattern.ParameterName}_Validation",
                    Description = $"Test parameter validation for {paramPattern.ParameterName}",
                    Method = "POST", // Default to POST for parameter validation
                    Parameters = new Dictionary<string, object> { [paramPattern.ParameterName] = paramPattern.TypicalValues.FirstOrDefault() ?? "test_value" },
                    ExpectedResponsePattern = paramPattern.RequiredInMostCases ? "200" : "400",
                    ValidationSteps = new List<string> 
                    { 
                        $"Validate {paramPattern.ParameterName} parameter",
                        $"Check response matches pattern: {paramPattern.ValidationPattern}"
                    }
                };

                testCases.Add(paramTestCase);
            }

            // Generate error handling test cases
            foreach (var errorCode in patterns.ErrorHandling.CommonErrorCodes)
            {
                var errorTestCase = new GeneratedTestCase
                {
                    Name = $"Test_Error_{errorCode}",
                    Description = $"Test error handling for {errorCode}",
                    Method = "GET", // Default method for error testing
                    ExpectedResponsePattern = errorCode,
                    ValidationSteps = new List<string> 
                    { 
                        $"Verify {errorCode} error is handled gracefully",
                        "Check error message is informative",
                        "Ensure proper error response format"
                    }
                };

                testCases.Add(errorTestCase);
            }

            _logger.LogInformation("Generated {TestCaseCount} test cases", testCases.Count);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating test cases");
        }

        return testCases;
    }

    #region Private Helper Methods

    private string InferMethodFromPattern(CommonPattern pattern)
    {
        var patternName = pattern.Name.ToLowerInvariant();
        
        if (patternName.Contains("post") || pattern.Steps.Any(s => s.ToLowerInvariant().Contains("post")))
            return "POST";
        if (patternName.Contains("put") || pattern.Steps.Any(s => s.ToLowerInvariant().Contains("put")))
            return "PUT";
        if (patternName.Contains("delete") || pattern.Steps.Any(s => s.ToLowerInvariant().Contains("delete")))
            return "DELETE";
        if (patternName.Contains("patch") || pattern.Steps.Any(s => s.ToLowerInvariant().Contains("patch")))
            return "PATCH";
        
        return "GET"; // Default to GET
    }

    private string InferResponsePattern(CommonPattern pattern)
    {
        // Infer expected response pattern based on pattern characteristics
        if (pattern.TypicalValues.ContainsKey("hasJson"))
            return "{\".*\"}"; // JSON response pattern
        
        if (pattern.Name.ToLowerInvariant().Contains("auth"))
            return "token|Bearer|success"; // Auth response pattern
        
        return "200"; // Default success status
    }

    private List<string> GenerateValidationSteps(CommonPattern pattern)
    {
        var steps = new List<string>
        {
            "Verify response status code",
            "Validate response format"
        };

        // Add pattern-specific validation steps
        foreach (var step in pattern.Steps)
        {
            if (!steps.Any(s => s.ToLowerInvariant().Contains(step.ToLowerInvariant())))
            {
                steps.Add($"Verify: {step}");
            }
        }

        // Add common validation steps
        steps.Add("Check response time is acceptable");
        steps.Add("Validate required fields are present");

        return steps;
    }

    private string GetTypicalHeaderValue(string headerName)
    {
        return headerName.ToLowerInvariant() switch
        {
            "authorization" => "Bearer test_token",
            "content-type" => "application/json",
            "accept" => "application/json",
            "x-api-key" => "test_api_key",
            _ => "test_value"
        };
    }

    private Dictionary<string, object> GetTypicalParametersForMethod(string method, UsagePatternAnalysis patterns)
    {
        var parameters = new Dictionary<string, object>();

        // Add common parameters based on method
        switch (method.ToUpperInvariant())
        {
            case "GET":
                parameters["page"] = 1;
                parameters["limit"] = 10;
                break;
            case "POST":
                parameters["name"] = "test_name";
                parameters["value"] = "test_value";
                break;
            case "PUT":
            case "PATCH":
                parameters["id"] = "test_id";
                parameters["name"] = "updated_name";
                break;
            case "DELETE":
                parameters["id"] = "test_id";
                break;
        }

        // Add parameters from pattern analysis
        foreach (var paramPattern in patterns.ParameterPatterns)
        {
            if (!parameters.ContainsKey(paramPattern.ParameterName))
            {
                var typicalValue = paramPattern.TypicalValues.FirstOrDefault() ?? "test_value";
                parameters[paramPattern.ParameterName] = typicalValue;
            }
        }

        return parameters;
    }

    private string GetExpectedResponseForMethod(string method)
    {
        return method.ToUpperInvariant() switch
        {
            "GET" => "200",
            "POST" => "201",
            "PUT" => "200",
            "PATCH" => "200", 
            "DELETE" => "204",
            _ => "200"
        };
    }

    private List<string> GetValidationStepsForMethod(string method)
    {
        var baseSteps = new List<string>
        {
            "Verify response status code",
            "Validate response headers",
            "Check response time"
        };

        var methodSpecificSteps = method.ToUpperInvariant() switch
        {
            "GET" => new[] { "Validate response data structure", "Check pagination if applicable" },
            "POST" => new[] { "Verify resource was created", "Validate created resource ID" },
            "PUT" => new[] { "Verify resource was updated", "Check all fields were modified" },
            "PATCH" => new[] { "Verify partial update was applied", "Check only specified fields changed" },
            "DELETE" => new[] { "Verify resource was deleted", "Confirm resource no longer accessible" },
            _ => new[] { "Validate response format" }
        };

        baseSteps.AddRange(methodSpecificSteps);
        return baseSteps;
    }

    #endregion
}