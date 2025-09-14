using DigitalMe.Services.Learning.Documentation.ContentParsing;
using DigitalMe.Services.Learning.Documentation.HttpContentFetching;
using DigitalMe.Services.Learning.Documentation.PatternAnalysis;
using DigitalMe.Services.Learning.Documentation.TestGeneration;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Learning;

/// <summary>
/// Orchestrator implementation of auto-documentation parser for Phase 1 Advanced Cognitive Capabilities
/// Delegates responsibilities to focused services following Single Responsibility Principle
/// Refactored from God Class (612 lines) to orchestrator pattern for SOLID compliance
/// 
/// Original class had 4 responsibilities:
/// 1. HTTP content fetching -> IDocumentationFetcher
/// 2. Content parsing and extraction -> IDocumentationParser  
/// 3. Usage pattern analysis -> IUsagePatternAnalyzer
/// 4. Test case generation -> IApiTestCaseGenerator
/// </summary>
public class AutoDocumentationParser : IAutoDocumentationParser
{
    private readonly ILogger<AutoDocumentationParser> _logger;
    private readonly IDocumentationFetcher _documentationFetcher;
    private readonly IDocumentationParser _documentationParser;
    private readonly IUsagePatternAnalyzer _usagePatternAnalyzer;
    private readonly IApiTestCaseGenerator _apiTestCaseGenerator;

    public AutoDocumentationParser(
        ILogger<AutoDocumentationParser> logger,
        IDocumentationFetcher documentationFetcher,
        IDocumentationParser documentationParser,
        IUsagePatternAnalyzer usagePatternAnalyzer,
        IApiTestCaseGenerator apiTestCaseGenerator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _documentationFetcher = documentationFetcher ?? throw new ArgumentNullException(nameof(documentationFetcher));
        _documentationParser = documentationParser ?? throw new ArgumentNullException(nameof(documentationParser));
        _usagePatternAnalyzer = usagePatternAnalyzer ?? throw new ArgumentNullException(nameof(usagePatternAnalyzer));
        _apiTestCaseGenerator = apiTestCaseGenerator ?? throw new ArgumentNullException(nameof(apiTestCaseGenerator));
    }

    /// <inheritdoc />
    public async Task<DocumentationParseResult> ParseApiDocumentationAsync(string documentationUrl, string apiName)
    {
        try
        {
            _logger.LogInformation("Starting documentation parsing for API: {ApiName} at {Url}", apiName, documentationUrl);

            // Step 1: Fetch documentation content using dedicated service
            var content = await _documentationFetcher.FetchDocumentationContentAsync(documentationUrl);
            if (string.IsNullOrEmpty(content))
            {
                return new DocumentationParseResult 
                { 
                    Success = false, 
                    ErrorMessage = "Failed to fetch documentation content",
                    ApiName = apiName
                };
            }

            // Step 2: Parse content for endpoints, examples, and configuration using dedicated service
            var endpoints = await _documentationParser.ExtractEndpointsAsync(content);
            var examples = await _documentationParser.ExtractCodeExamplesAsync(content);
            var documentationConfig = _documentationParser.ExtractConfiguration(content);
            var auth = _documentationParser.DetectAuthenticationMethod(content);
            var baseUrl = _documentationParser.ExtractBaseUrl(content);
            var requiredHeaders = _documentationParser.ExtractRequiredHeaders(content);

            _logger.LogInformation("Documentation parsing completed. Found {EndpointCount} endpoints and {ExampleCount} examples", 
                endpoints.Count, examples.Count);

            return new DocumentationParseResult
            {
                ApiName = apiName,
                BaseUrl = baseUrl,
                Endpoints = endpoints,
                Examples = examples,
                Configuration = documentationConfig.Settings, // Extract Dictionary from DocumentationConfig
                RequiredHeaders = requiredHeaders,
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
        // Delegate to documentation parser service
        return await _documentationParser.ExtractCodeExamplesAsync(documentationContent);
    }

    /// <inheritdoc />
    public async Task<UsagePatternAnalysis> AnalyzeUsagePatternsAsync(List<CodeExample> examples)
    {
        // Delegate to usage pattern analyzer service
        return await _usagePatternAnalyzer.AnalyzeUsagePatternsAsync(examples);
    }

    /// <inheritdoc />
    public async Task<List<GeneratedTestCase>> GenerateTestCasesAsync(UsagePatternAnalysis patterns)
    {
        // Delegate to API test case generator service
        return await _apiTestCaseGenerator.GenerateTestCasesAsync(patterns);
    }
}