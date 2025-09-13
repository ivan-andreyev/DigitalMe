using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DigitalMe.Services.Learning;

namespace DigitalMe.Services.Learning.Documentation.PatternAnalysis;

/// <summary>
/// Usage pattern analyzer implementation  
/// Single responsibility: Analyze and identify patterns in API usage examples
/// Extracted from AutoDocumentationParser to resolve SRP violations
/// </summary>
public class UsagePatternAnalyzer : IUsagePatternAnalyzer
{
    private readonly ILogger<UsagePatternAnalyzer> _logger;

    public UsagePatternAnalyzer(ILogger<UsagePatternAnalyzer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
    public async Task<List<CommonPattern>> IdentifyCommonPatternsAsync(List<CodeExample> examples)
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

    #region Private Helper Methods

    private string ExtractHttpMethodFromCode(string code)
    {
        var methodPatterns = new[]
        {
            @"\b(GET|POST|PUT|DELETE|PATCH)\b",
            @"\.get\(",
            @"\.post\(",
            @"\.put\(",
            @"\.delete\(",
            @"\.patch\("
        };

        foreach (var pattern in methodPatterns)
        {
            var match = Regex.Match(code, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var method = match.Groups[1].Success ? match.Groups[1].Value : match.Value.Replace(".", "").Replace("(", "");
                return method.ToUpperInvariant();
            }
        }

        return string.Empty;
    }

    private List<string> ExtractHeadersFromCode(string code)
    {
        var headers = new List<string>();
        
        // Common header patterns
        var headerPatterns = new[]
        {
            @"Authorization['""]?\s*[:=]\s*['""]?([^'"";\n]+)",
            @"Content-Type['""]?\s*[:=]\s*['""]?([^'"";\n]+)",
            @"X-API-Key['""]?\s*[:=]\s*['""]?([^'"";\n]+)",
            @"Accept['""]?\s*[:=]\s*['""]?([^'"";\n]+)"
        };

        foreach (var pattern in headerPatterns)
        {
            var matches = Regex.Matches(code, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var headerValue = match.Groups[0].Value.Split(':')[0].Trim();
                    if (!headers.Contains(headerValue))
                    {
                        headers.Add(headerValue);
                    }
                }
            }
        }

        return headers;
    }

    private List<ParameterPattern> AnalyzeParameterPatterns(List<CodeExample> examples)
    {
        var parameterPatterns = new List<ParameterPattern>();
        var parameterCounts = new Dictionary<string, List<string>>();

        foreach (var example in examples)
        {
            var parameters = ExtractParametersFromCode(example.Code);
            
            foreach (var param in parameters)
            {
                if (!parameterCounts.ContainsKey(param.Key))
                {
                    parameterCounts[param.Key] = new List<string>();
                }
                parameterCounts[param.Key].Add(param.Value);
            }
        }

        foreach (var paramGroup in parameterCounts.Where(p => p.Value.Count > 1))
        {
            var pattern = new ParameterPattern
            {
                ParameterName = paramGroup.Key,
                TypicalValues = paramGroup.Value.Distinct().ToList(),
                RequiredInMostCases = paramGroup.Value.Count > examples.Count / 2,
                ValidationPattern = DeriveValidationPattern(paramGroup.Value)
            };

            parameterPatterns.Add(pattern);
        }

        return parameterPatterns;
    }

    private Dictionary<string, string> ExtractParametersFromCode(string code)
    {
        var parameters = new Dictionary<string, string>();
        
        // Extract JSON parameters
        var jsonMatch = Regex.Match(code, @"\{([^}]+)\}");
        if (jsonMatch.Success)
        {
            var jsonContent = jsonMatch.Groups[1].Value;
            var paramMatches = Regex.Matches(jsonContent, @"['""](\w+)['""]:\s*['""]?([^'"",:}\n]+)");
            
            foreach (Match match in paramMatches)
            {
                if (match.Groups.Count >= 3)
                {
                    parameters[match.Groups[1].Value] = match.Groups[2].Value.Trim();
                }
            }
        }

        // Extract URL parameters
        var urlParamMatches = Regex.Matches(code, @"[\?&](\w+)=([^&\s]+)");
        foreach (Match match in urlParamMatches)
        {
            if (match.Groups.Count >= 3)
            {
                parameters[match.Groups[1].Value] = match.Groups[2].Value;
            }
        }

        return parameters;
    }

    private string DeriveValidationPattern(List<string> values)
    {
        // Simple pattern derivation - could be more sophisticated
        if (values.All(v => Regex.IsMatch(v, @"^\d+$")))
        {
            return @"^\d+$"; // All numeric
        }
        
        if (values.All(v => Regex.IsMatch(v, @"^[a-zA-Z]+$")))
        {
            return @"^[a-zA-Z]+$"; // All alphabetic
        }
        
        if (values.All(v => v.Contains("@")))
        {
            return @"^[^@]+@[^@]+$"; // Email-like pattern
        }

        return @".*"; // Any string
    }

    private ErrorHandlingPattern ExtractErrorHandlingPatterns(List<CodeExample> examples)
    {
        var errorHandling = new ErrorHandlingPattern();
        var errorCodes = new List<string>();
        var errorMessages = new Dictionary<string, string>();
        var retryStrategies = new List<string>();

        foreach (var example in examples)
        {
            var code = example.Code.ToLowerInvariant();
            
            // Look for error codes
            var errorCodeMatches = Regex.Matches(code, @"\b(4\d\d|5\d\d)\b");
            foreach (Match match in errorCodeMatches)
            {
                errorCodes.Add(match.Value);
            }

            // Look for error handling keywords
            if (code.Contains("try") && code.Contains("catch"))
            {
                retryStrategies.Add("try-catch");
            }
            
            if (code.Contains("retry") || code.Contains("attempt"))
            {
                retryStrategies.Add("retry-logic");
            }

            // Look for error messages
            var errorMsgMatches = Regex.Matches(example.Code, @"['""]([^'""]*error[^'""]*)['""]", RegexOptions.IgnoreCase);
            foreach (Match match in errorMsgMatches)
            {
                var errorMsg = match.Groups[1].Value;
                if (!errorMessages.ContainsKey("generic"))
                {
                    errorMessages["generic"] = errorMsg;
                }
            }
        }

        errorHandling.CommonErrorCodes = errorCodes.Distinct().ToList();
        errorHandling.ErrorMessages = errorMessages;
        errorHandling.RetryStrategies = retryStrategies.Distinct().ToList();

        return errorHandling;
    }

    private List<string> ExtractCommonSteps(List<CodeExample> examples)
    {
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

    #endregion
}