using System.Collections.Generic;
using System.Linq;

namespace DigitalMe.Services.Learning.Testing.TestGeneration;

/// <summary>
/// Helper utilities for test case generation
/// Extracted to keep TestCaseGenerator within size limits
/// </summary>
internal static class TestCaseGeneratorHelpers
{
    /// <summary>
    /// Create standard headers for API requests
    /// </summary>
    public static Dictionary<string, string> CreateStandardHeaders(DocumentationParseResult apiDoc)
    {
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json",
            ["Accept"] = "application/json"
        };

        // Add authentication header based on detected method
        switch (apiDoc.Authentication)
        {
            case AuthenticationMethod.ApiKey:
                headers["X-API-Key"] = "test-api-key";
                break;
            case AuthenticationMethod.Bearer:
                headers["Authorization"] = "Bearer test-token";
                break;
            case AuthenticationMethod.Basic:
                headers["Authorization"] = "Basic dGVzdDp0ZXN0"; // test:test
                break;
        }

        return headers;
    }

    /// <summary>
    /// Generate appropriate test value based on parameter type
    /// </summary>
    public static object GenerateTestValue(ApiParameter parameter)
    {
        // Generate appropriate test values based on parameter type and constraints
        return parameter.Type.ToLowerInvariant() switch
        {
            "string" => parameter.AllowedValues.Any() ? parameter.AllowedValues.First() : "test_value",
            "integer" or "int" => 42,
            "boolean" or "bool" => true,
            "number" or "float" or "double" => 3.14,
            "array" => new[] { "test1", "test2" },
            _ => parameter.DefaultValue ?? "test_value"
        };
    }

    /// <summary>
    /// Extract HTTP method from example code
    /// </summary>
    public static string ExtractHttpMethod(string code)
    {
        var upperCode = code.ToUpperInvariant();
        if (upperCode.Contains("POST")) return "POST";
        if (upperCode.Contains("PUT")) return "PUT";
        if (upperCode.Contains("DELETE")) return "DELETE";
        if (upperCode.Contains("PATCH")) return "PATCH";
        return "GET";
    }
}