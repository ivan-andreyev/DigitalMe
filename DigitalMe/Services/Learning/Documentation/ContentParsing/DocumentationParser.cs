using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DigitalMe.Services.Learning;
using HtmlAgilityPack;

namespace DigitalMe.Services.Learning.Documentation.ContentParsing;

/// <summary>
/// Documentation content parser implementation
/// Single responsibility: Parse and extract structured data from documentation content
/// Extracted from AutoDocumentationParser to resolve SRP violations
/// </summary>
public class DocumentationParser : IDocumentationParser
{
    private readonly ILogger<DocumentationParser> _logger;

    // Regex patterns for common API documentation structures
    private static readonly Regex EndpointPattern = new(@"(?:GET|POST|PUT|DELETE|PATCH)\s+(/[^\s]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CodeBlockPattern = new(@"```(\w+)?\s*(.*?)\s*```", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex JsonPattern = new(@"\{.*\}", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex UrlPattern = new(@"https?://[^\s/$.?#].[^\s]*", RegexOptions.Compiled);

    public DocumentationParser(ILogger<DocumentationParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<List<ApiEndpoint>> ExtractEndpointsAsync(string content)
    {
        var endpoints = new List<ApiEndpoint>();
        var matches = EndpointPattern.Matches(content);

        _logger.LogDebug("Found {EndpointCount} potential endpoints in documentation", matches.Count);

        foreach (Match match in matches)
        {
            var fullMatch = match.Value;
            var path = match.Groups[1].Value;
            var method = fullMatch.Split(' ')[0].ToUpperInvariant();

            var endpoint = new ApiEndpoint
            {
                Path = path,
                Method = method,
                Description = ExtractEndpointDescription(content, match.Index),
                Parameters = await ExtractParametersForEndpointAsync(content, path)
            };

            endpoints.Add(endpoint);
            _logger.LogDebug("Extracted endpoint: {Method} {Path}", method, path);
        }

        return endpoints;
    }

    /// <inheritdoc />
    public async Task<List<CodeExample>> ExtractCodeExamplesAsync(string content)
    {
        var examples = new List<CodeExample>();

        try
        {
            // Extract code blocks using regex
            var codeBlockMatches = CodeBlockPattern.Matches(content);
            
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
                    Description = ExtractDescriptionNearCode(content, match.Index),
                    Endpoint = ExtractEndpointFromCode(code),
                    ExtractedValues = ExtractValuesFromCode(code)
                };

                examples.Add(example);
                _logger.LogDebug("Extracted {Language} code example for endpoint: {Endpoint}", language, example.Endpoint);
            }

            // Also look for inline code examples
            await ExtractInlineCodeExamples(content, examples);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting code examples");
        }

        return examples;
    }

    /// <inheritdoc />
    public DocumentationConfig ExtractConfiguration(string content)
    {
        var config = new DocumentationConfig();
        config.Settings = new Dictionary<string, string>();

        // Look for common configuration patterns
        var baseUrlMatch = Regex.Match(content, @"base[_\s]*url[:\s]*([^\s\n]+)", RegexOptions.IgnoreCase);
        if (baseUrlMatch.Success)
        {
            config.Settings["BaseUrl"] = baseUrlMatch.Groups[1].Value;
            config.BaseUrl = baseUrlMatch.Groups[1].Value;
        }

        var apiKeyMatch = Regex.Match(content, @"api[_\s]*key[:\s]*([^\s\n]+)", RegexOptions.IgnoreCase);
        if (apiKeyMatch.Success)
        {
            config.Settings["ApiKeyHeader"] = apiKeyMatch.Groups[1].Value;
        }

        // Detect authentication method
        config.AuthenticationMethod = DetectAuthenticationMethod(content);

        // Extract required headers
        config.RequiredHeaders = ExtractRequiredHeaders(content);

        // Extract base URL if not found in config patterns
        if (string.IsNullOrEmpty(config.BaseUrl))
        {
            config.BaseUrl = ExtractBaseUrl(content);
        }

        return config;
    }

    /// <inheritdoc />
    public async Task<List<ApiParameter>> ExtractParametersForEndpointAsync(string content, string endpoint)
    {
        var parameters = new List<ApiParameter>();

        try
        {
            // Find sections that describe this endpoint
            var endpointIndex = content.IndexOf(endpoint, StringComparison.OrdinalIgnoreCase);
            if (endpointIndex == -1) return parameters;

            // Look for parameter descriptions in the next 500 characters
            var searchSection = content.Substring(endpointIndex, Math.Min(500, content.Length - endpointIndex));
            
            // Common parameter patterns
            var paramPatterns = new[]
            {
                @"(\w+)\s*:\s*(\w+)\s*-\s*(.+?)(?=\n|\r|$)",  // name: type - description
                @"(\w+)\s*\((\w+)\)\s*-\s*(.+?)(?=\n|\r|$)", // name(type) - description  
                @"""(\w+)""\s*:\s*""([^""]*)""\s*,?\s*//\s*(.+?)(?=\n|\r|$)" // JSON with comments
            };

            foreach (var pattern in paramPatterns)
            {
                var matches = Regex.Matches(searchSection, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                
                foreach (Match match in matches)
                {
                    if (match.Groups.Count >= 4)
                    {
                        var param = new ApiParameter
                        {
                            Name = match.Groups[1].Value,
                            Type = match.Groups[2].Value,
                            Description = match.Groups[3].Value,
                            Required = searchSection.Contains("required", StringComparison.OrdinalIgnoreCase)
                        };

                        if (!parameters.Any(p => p.Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            parameters.Add(param);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting parameters for endpoint {Endpoint}", endpoint);
        }

        return parameters;
    }

    #region Private Helper Methods

    private string ExtractEndpointDescription(string content, int matchIndex)
    {
        try
        {
            // Look backwards for description (previous 200 chars)
            var startIndex = Math.Max(0, matchIndex - 200);
            var beforeText = content.Substring(startIndex, matchIndex - startIndex);
            
            // Look for description patterns
            var lines = beforeText.Split('\n').Reverse().Take(5);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed) && 
                    !trimmed.StartsWith("GET") && !trimmed.StartsWith("POST") && 
                    !trimmed.StartsWith("PUT") && !trimmed.StartsWith("DELETE"))
                {
                    return trimmed;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not extract description for endpoint at index {Index}", matchIndex);
        }

        return string.Empty;
    }

    private bool IsRelevantLanguage(string language)
    {
        var relevantLanguages = new[] { "javascript", "python", "csharp", "java", "php", "ruby", "curl", "bash", "json", "http" };
        return relevantLanguages.Contains(language.ToLowerInvariant());
    }

    private bool ContainsApiCall(string code)
    {
        var apiIndicators = new[] { "http", "api", "request", "fetch", "axios", "curl", "get", "post", "put", "delete" };
        return apiIndicators.Any(indicator => code.ToLowerInvariant().Contains(indicator));
    }

    private string ExtractDescriptionNearCode(string content, int codeIndex)
    {
        try
        {
            var startIndex = Math.Max(0, codeIndex - 150);
            var beforeCode = content.Substring(startIndex, codeIndex - startIndex);
            
            var lines = beforeCode.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).TakeLast(2);
            return string.Join(" ", lines).Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    private string ExtractEndpointFromCode(string code)
    {
        var urlMatch = Regex.Match(code, @"['""]([^'""]*/[^'""]*)['""]");
        if (urlMatch.Success)
        {
            var url = urlMatch.Groups[1].Value;
            if (url.StartsWith("/")) return url;
            
            var uri = new Uri(url);
            return uri.AbsolutePath;
        }
        return string.Empty;
    }

    private Dictionary<string, object> ExtractValuesFromCode(string code)
    {
        var values = new Dictionary<string, object>();
        
        try
        {
            // Extract JSON objects
            var jsonMatches = JsonPattern.Matches(code);
            if (jsonMatches.Count > 0)
            {
                values["hasJson"] = true;
                values["jsonCount"] = jsonMatches.Count;
            }

            // Extract URLs
            var urlMatches = UrlPattern.Matches(code);
            if (urlMatches.Count > 0)
            {
                values["urls"] = urlMatches.Cast<Match>().Select(m => m.Value).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting values from code");
        }

        return values;
    }

    private async Task ExtractInlineCodeExamples(string content, List<CodeExample> examples)
    {
        try
        {
            // Look for code patterns outside of code blocks
            var inlinePatterns = new[]
            {
                @"curl\s+-[^\n]+",
                @"GET\s+/[^\s]+",
                @"POST\s+/[^\s]+",
                @"\$\s*curl[^\n]+"
            };

            foreach (var pattern in inlinePatterns)
            {
                var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                foreach (Match match in matches)
                {
                    var code = match.Value.Trim();
                    if (code.Length > 10 && !examples.Any(e => e.Code.Contains(code)))
                    {
                        examples.Add(new CodeExample
                        {
                            Language = "curl",
                            Code = code,
                            Description = "Inline example",
                            Endpoint = ExtractEndpointFromCode(code),
                            ExtractedValues = ExtractValuesFromCode(code)
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting inline code examples");
        }
        
        await Task.CompletedTask; // Make async for consistency
    }

    public AuthenticationMethod DetectAuthenticationMethod(string content)
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

    public string ExtractBaseUrl(string content)
    {
        var urlMatches = UrlPattern.Matches(content);
        return urlMatches.Count > 0 ? urlMatches[0].Value : string.Empty;
    }

    public List<string> ExtractRequiredHeaders(string content)
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

    #endregion

}