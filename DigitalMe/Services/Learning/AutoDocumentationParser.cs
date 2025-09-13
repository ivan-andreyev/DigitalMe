using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace DigitalMe.Services.Learning;

/// <summary>
/// Implementation of auto-documentation parser for Phase 1 Advanced Cognitive Capabilities
/// Enables agent to learn new APIs by analyzing documentation automatically
/// Following Clean Architecture patterns with dependency injection
/// </summary>
public class AutoDocumentationParser : IAutoDocumentationParser
{
    private readonly ILogger<AutoDocumentationParser> _logger;
    private readonly HttpClient _httpClient;
    
    // Regex patterns for common API documentation structures
    private static readonly Regex EndpointPattern = new(@"(?:GET|POST|PUT|DELETE|PATCH)\s+(/[^\s]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CodeBlockPattern = new(@"```(\w+)?\s*(.*?)\s*```", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex JsonPattern = new(@"\{.*\}", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex UrlPattern = new(@"https?://[^\s/$.?#].[^\s]*", RegexOptions.Compiled);

    public AutoDocumentationParser(ILogger<AutoDocumentationParser> logger, HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<DocumentationParseResult> ParseApiDocumentationAsync(string documentationUrl, string apiName)
    {
        try
        {
            _logger.LogInformation("Starting documentation parsing for API: {ApiName} at {Url}", apiName, documentationUrl);

            var content = await FetchDocumentationContentAsync(documentationUrl);
            if (string.IsNullOrEmpty(content))
            {
                return new DocumentationParseResult 
                { 
                    Success = false, 
                    ErrorMessage = "Failed to fetch documentation content",
                    ApiName = apiName
                };
            }

            var endpoints = await ExtractEndpointsAsync(content);
            var examples = await ExtractCodeExamplesAsync(content);
            var config = ExtractConfiguration(content);
            var auth = DetectAuthenticationMethod(content);

            _logger.LogInformation("Documentation parsing completed. Found {EndpointCount} endpoints and {ExampleCount} examples", 
                endpoints.Count, examples.Count);

            return new DocumentationParseResult
            {
                ApiName = apiName,
                BaseUrl = ExtractBaseUrl(content),
                Endpoints = endpoints,
                Examples = examples,
                Configuration = config,
                RequiredHeaders = ExtractRequiredHeaders(content),
                Authentication = auth,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing documentation for API: {ApiName}", apiName);
            return new DocumentationParseResult 
            { 
                Success = false, 
                ErrorMessage = ex.Message,
                ApiName = apiName
            };
        }
    }

    /// <inheritdoc />
    public async Task<List<CodeExample>> ExtractCodeExamplesAsync(string documentationContent)
    {
        var examples = new List<CodeExample>();

        try
        {
            // Extract code blocks using regex
            var codeBlockMatches = CodeBlockPattern.Matches(documentationContent);
            
            foreach (Match match in codeBlockMatches)
            {
                var language = match.Groups[1].Value.ToLowerInvariant();
                var code = match.Groups[2].Value.Trim();

                if (string.IsNullOrEmpty(code))
                {
                    continue;
                }

                // Skip if it's not a relevant programming language
                if (!IsRelevantLanguage(language) && !ContainsApiCall(code))
                {
                    continue;
                }

                var example = new CodeExample
                {
                    Language = language,
                    Code = code,
                    Description = ExtractDescriptionNearCode(documentationContent, match.Index),
                    Endpoint = ExtractEndpointFromCode(code),
                    ExtractedValues = ExtractValuesFromCode(code)
                };

                examples.Add(example);
                _logger.LogDebug("Extracted {Language} code example for endpoint: {Endpoint}", language, example.Endpoint);
            }

            // Also look for inline code examples
            await ExtractInlineCodeExamples(documentationContent, examples);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting code examples");
        }

        return examples;
    }

    /// <inheritdoc />
    public async Task<UsagePatternAnalysis> AnalyzeUsagePatternsAsync(List<CodeExample> examples)
    {
        try
        {
            _logger.LogInformation("Analyzing usage patterns from {ExampleCount} code examples", examples.Count);

            var analysis = new UsagePatternAnalysis();

            // Analyze method frequency
            analysis.MethodFrequency = examples
                .Select(e => ExtractHttpMethodFromCode(e.Code))
                .Where(m => !string.IsNullOrEmpty(m))
                .GroupBy(m => m)
                .ToDictionary(g => g.Key, g => g.Count());

            // Extract common headers
            analysis.CommonHeaders = examples
                .SelectMany(e => ExtractHeadersFromCode(e.Code))
                .GroupBy(h => h)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            // Identify common patterns
            analysis.Patterns = await IdentifyCommonPatternsAsync(examples);

            // Analyze parameter patterns
            analysis.ParameterPatterns = AnalyzeParameterPatterns(examples);

            // Extract error handling patterns
            analysis.ErrorHandling = ExtractErrorHandlingPatterns(examples);

            _logger.LogInformation("Pattern analysis completed. Found {PatternCount} patterns and {HeaderCount} common headers", 
                analysis.Patterns.Count, analysis.CommonHeaders.Count);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing usage patterns");
            return new UsagePatternAnalysis();
        }
    }

    /// <inheritdoc />
    public async Task<List<GeneratedTestCase>> GenerateTestCasesAsync(UsagePatternAnalysis patterns)
    {
        var testCases = new List<GeneratedTestCase>();

        try
        {
            _logger.LogInformation("Generating test cases based on {PatternCount} identified patterns", patterns.Patterns.Count);

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
            }

            // Generate error handling test cases
            foreach (var errorCode in patterns.ErrorHandling.CommonErrorCodes)
            {
                testCases.Add(new GeneratedTestCase
                {
                    Name = $"Test_Error_{errorCode}",
                    Description = $"Test error handling for {errorCode}",
                    ExpectedResponsePattern = errorCode,
                    ValidationSteps = new List<string> { $"Verify {errorCode} error is handled gracefully" }
                });
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

    private async Task<string> FetchDocumentationContentAsync(string url)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch documentation from {Url}", url);
            return string.Empty;
        }
    }

    private async Task<List<ApiEndpoint>> ExtractEndpointsAsync(string content)
    {
        var endpoints = new List<ApiEndpoint>();
        var matches = EndpointPattern.Matches(content);

        foreach (Match match in matches)
        {
            var fullMatch = match.Value;
            var path = match.Groups[1].Value;
            var method = fullMatch.Split(' ')[0].ToUpperInvariant();

            endpoints.Add(new ApiEndpoint
            {
                Path = path,
                Method = method,
                Description = ExtractEndpointDescription(content, match.Index),
                Parameters = await ExtractParametersForEndpoint(content, path)
            });
        }

        return endpoints;
    }

    private Dictionary<string, string> ExtractConfiguration(string content)
    {
        var config = new Dictionary<string, string>();

        // Look for common configuration patterns
        var baseUrlMatch = Regex.Match(content, @"base[_\s]*url[:\s]*([^\s\n]+)", RegexOptions.IgnoreCase);
        if (baseUrlMatch.Success)
        {
            config["BaseUrl"] = baseUrlMatch.Groups[1].Value;
        }

        var apiKeyMatch = Regex.Match(content, @"api[_\s]*key[:\s]*([^\s\n]+)", RegexOptions.IgnoreCase);
        if (apiKeyMatch.Success)
        {
            config["ApiKeyHeader"] = apiKeyMatch.Groups[1].Value;
        }

        return config;
    }

    private AuthenticationMethod DetectAuthenticationMethod(string content)
    {
        var lowerContent = content.ToLowerInvariant();

        if (lowerContent.Contains("bearer") || lowerContent.Contains("authorization: bearer"))
            return AuthenticationMethod.Bearer;
        
        if (lowerContent.Contains("api key") || lowerContent.Contains("x-api-key"))
            return AuthenticationMethod.ApiKey;
        
        if (lowerContent.Contains("basic auth"))
            return AuthenticationMethod.Basic;
        
        if (lowerContent.Contains("oauth"))
            return AuthenticationMethod.OAuth;

        return AuthenticationMethod.None;
    }

    private string ExtractBaseUrl(string content)
    {
        var urlMatches = UrlPattern.Matches(content);
        return urlMatches.Count > 0 ? urlMatches[0].Value : string.Empty;
    }

    private List<string> ExtractRequiredHeaders(string content)
    {
        var headers = new List<string>();
        var headerPatterns = new[]
        {
            @"Authorization",
            @"Content-Type",
            @"X-API-Key",
            @"Accept"
        };

        foreach (var pattern in headerPatterns)
        {
            if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
            {
                headers.Add(pattern);
            }
        }

        return headers;
    }

    private bool IsRelevantLanguage(string language)
    {
        var relevantLanguages = new[] { "javascript", "js", "python", "csharp", "c#", "curl", "bash", "json", "http" };
        return relevantLanguages.Contains(language.ToLowerInvariant());
    }

    private bool ContainsApiCall(string code)
    {
        var apiIndicators = new[] { "http", "fetch", "request", "get(", "post(", "curl", "api" };
        var lowerCode = code.ToLowerInvariant();
        return apiIndicators.Any(indicator => lowerCode.Contains(indicator));
    }

    private string ExtractDescriptionNearCode(string content, int codeIndex)
    {
        // Look for text before the code block that might be a description
        var beforeCode = content.Substring(Math.Max(0, codeIndex - 200), Math.Min(200, codeIndex));
        var sentences = beforeCode.Split(new[] { '.', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return sentences.LastOrDefault()?.Trim() ?? string.Empty;
    }

    private string ExtractEndpointFromCode(string code)
    {
        var endpointMatch = Regex.Match(code, @"['""]([/][^'""]*)['""]");
        return endpointMatch.Success ? endpointMatch.Groups[1].Value : string.Empty;
    }

    private Dictionary<string, object> ExtractValuesFromCode(string code)
    {
        var values = new Dictionary<string, object>();

        // Extract JSON objects
        var jsonMatches = JsonPattern.Matches(code);
        foreach (Match match in jsonMatches)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(match.Value);
                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    values[property.Name] = property.Value.ToString();
                }
            }
            catch
            {
                // Ignore malformed JSON
            }
        }

        return values;
    }

    private async Task ExtractInlineCodeExamples(string content, List<CodeExample> examples)
    {
        // Look for inline code that might contain API calls
        var inlineCodePattern = new Regex(@"`([^`]+)`", RegexOptions.Compiled);
        var matches = inlineCodePattern.Matches(content);

        foreach (Match match in matches)
        {
            var code = match.Groups[1].Value;
            if (ContainsApiCall(code))
            {
                examples.Add(new CodeExample
                {
                    Language = "inline",
                    Code = code,
                    Description = "Inline code example",
                    ExtractedValues = ExtractValuesFromCode(code)
                });
            }
        }
    }

    private string ExtractHttpMethodFromCode(string code)
    {
        var methods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH" };
        var upperCode = code.ToUpperInvariant();
        
        return methods.FirstOrDefault(method => upperCode.Contains(method)) ?? string.Empty;
    }

    private List<string> ExtractHeadersFromCode(string code)
    {
        var headers = new List<string>();
        var headerPattern = new Regex(@"['""]([A-Za-z-]+)['""]:\s*['""]", RegexOptions.Compiled);
        var matches = headerPattern.Matches(code);

        foreach (Match match in matches)
        {
            headers.Add(match.Groups[1].Value);
        }

        return headers;
    }

    private async Task<List<CommonPattern>> IdentifyCommonPatternsAsync(List<CodeExample> examples)
    {
        var patterns = new List<CommonPattern>();

        // Group examples by similar structure
        var grouped = examples
            .GroupBy(e => e.Language)
            .Where(g => g.Count() > 1);

        foreach (var group in grouped)
        {
            var pattern = new CommonPattern
            {
                Name = $"{group.Key} API Usage",
                Description = $"Common pattern for {group.Key} API interactions",
                Frequency = group.Count(),
                Steps = ExtractCommonSteps(group.ToList()),
                TypicalValues = MergeTypicalValues(group.Select(e => e.ExtractedValues).ToList())
            };

            patterns.Add(pattern);
        }

        return patterns;
    }

    private List<string> ExtractCommonSteps(List<CodeExample> examples)
    {
        // Simplified step extraction - in real implementation, this would be more sophisticated
        var commonSteps = new List<string>();
        
        if (examples.Any(e => e.Code.ToLowerInvariant().Contains("auth")))
            commonSteps.Add("Authenticate with API");
            
        if (examples.Any(e => e.Code.ToLowerInvariant().Contains("get")))
            commonSteps.Add("Make GET request");
            
        if (examples.Any(e => e.Code.ToLowerInvariant().Contains("post")))
            commonSteps.Add("Make POST request");
            
        commonSteps.Add("Handle response");

        return commonSteps;
    }

    private Dictionary<string, string> MergeTypicalValues(List<Dictionary<string, object>> valueSets)
    {
        var merged = new Dictionary<string, string>();
        
        foreach (var valueSet in valueSets)
        {
            foreach (var kv in valueSet)
            {
                if (!merged.ContainsKey(kv.Key))
                {
                    merged[kv.Key] = kv.Value?.ToString() ?? string.Empty;
                }
            }
        }

        return merged;
    }

    private List<ParameterPattern> AnalyzeParameterPatterns(List<CodeExample> examples)
    {
        var parameterPatterns = new List<ParameterPattern>();

        // Extract parameters from all examples and analyze patterns
        var allParameters = examples
            .SelectMany(e => e.ExtractedValues)
            .GroupBy(kv => kv.Key)
            .Where(g => g.Count() > 1);

        foreach (var paramGroup in allParameters)
        {
            var pattern = new ParameterPattern
            {
                ParameterName = paramGroup.Key,
                TypicalValues = paramGroup.Select(p => p.Value?.ToString() ?? string.Empty).Distinct().ToList(),
                RequiredInMostCases = paramGroup.Count() > examples.Count / 2
            };

            parameterPatterns.Add(pattern);
        }

        return parameterPatterns;
    }

    private ErrorHandlingPattern ExtractErrorHandlingPatterns(List<CodeExample> examples)
    {
        var errorPattern = new ErrorHandlingPattern();

        // Look for error codes in examples
        var errorCodePattern = new Regex(@"[45]\d{2}", RegexOptions.Compiled);
        
        foreach (var example in examples)
        {
            var matches = errorCodePattern.Matches(example.Code);
            foreach (Match match in matches)
            {
                errorPattern.CommonErrorCodes.Add(match.Value);
            }
        }

        // Look for common retry patterns
        if (examples.Any(e => e.Code.ToLowerInvariant().Contains("retry")))
            errorPattern.RetryStrategies.Add("Automatic retry");

        return errorPattern;
    }

    private string ExtractEndpointDescription(string content, int index)
    {
        // Extract description near the endpoint mention
        var surroundingText = content.Substring(Math.Max(0, index - 100), Math.Min(200, content.Length - Math.Max(0, index - 100)));
        var sentences = surroundingText.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return sentences.FirstOrDefault(s => s.Length > 10)?.Trim() ?? string.Empty;
    }

    private async Task<List<ApiParameter>> ExtractParametersForEndpoint(string content, string endpoint)
    {
        var parameters = new List<ApiParameter>();
        
        // Look for parameter documentation near the endpoint
        var endpointIndex = content.IndexOf(endpoint, StringComparison.OrdinalIgnoreCase);
        if (endpointIndex > -1)
        {
            var section = content.Substring(endpointIndex, Math.Min(1000, content.Length - endpointIndex));
            
            // Look for parameter patterns
            var paramPattern = new Regex(@"(\w+)\s*\((\w+)\):\s*(.+)", RegexOptions.Compiled | RegexOptions.Multiline);
            var matches = paramPattern.Matches(section);

            foreach (Match match in matches)
            {
                parameters.Add(new ApiParameter
                {
                    Name = match.Groups[1].Value,
                    Type = match.Groups[2].Value,
                    Description = match.Groups[3].Value.Split('\n')[0].Trim(),
                    Required = match.Groups[3].Value.ToLowerInvariant().Contains("required")
                });
            }
        }

        return parameters;
    }

    private string InferMethodFromPattern(CommonPattern pattern)
    {
        if (pattern.Steps.Any(s => s.ToLowerInvariant().Contains("post")))
            return "POST";
        if (pattern.Steps.Any(s => s.ToLowerInvariant().Contains("put")))
            return "PUT";
        if (pattern.Steps.Any(s => s.ToLowerInvariant().Contains("delete")))
            return "DELETE";
        
        return "GET"; // Default
    }

    private string InferResponsePattern(CommonPattern pattern)
    {
        if (pattern.TypicalValues.ContainsKey("success"))
            return @"\{""success"":\s*true\}";
        if (pattern.TypicalValues.ContainsKey("data"))
            return @"\{""data"":\s*\[.*\]\}";
            
        return @"\{.*\}"; // Generic JSON response
    }

    private List<string> GenerateValidationSteps(CommonPattern pattern)
    {
        var steps = new List<string>
        {
            "Verify response status is 200",
            "Validate response format matches expected pattern"
        };

        foreach (var expectedField in pattern.TypicalValues.Keys)
        {
            steps.Add($"Verify '{expectedField}' field is present in response");
        }

        return steps;
    }

    #endregion
}