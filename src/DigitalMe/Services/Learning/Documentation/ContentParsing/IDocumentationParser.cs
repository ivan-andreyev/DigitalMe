using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalMe.Services.Learning;

namespace DigitalMe.Services.Learning.Documentation.ContentParsing;

/// <summary>
/// Interface for parsing documentation content and extracting structured data
/// Focused responsibility: Content parsing and data extraction
/// Part of AutoDocumentationParser refactoring to resolve SRP violations
/// </summary>
public interface IDocumentationParser
{
    /// <summary>
    /// Extracts API endpoints from documentation content
    /// </summary>
    /// <param name="content">Raw documentation content</param>
    /// <returns>List of discovered API endpoints</returns>
    Task<List<ApiEndpoint>> ExtractEndpointsAsync(string content);

    /// <summary>
    /// Extracts code examples from documentation content
    /// </summary>
    /// <param name="content">Raw documentation content</param>
    /// <returns>List of code examples with language and content</returns>
    Task<List<CodeExample>> ExtractCodeExamplesAsync(string content);

    /// <summary>
    /// Extracts configuration information from documentation
    /// </summary>
    /// <param name="content">Raw documentation content</param>
    /// <returns>Configuration parameters and settings</returns>
    DocumentationConfig ExtractConfiguration(string content);

    /// <summary>
    /// Extracts parameters for a specific API endpoint
    /// </summary>
    /// <param name="content">Raw documentation content</param>
    /// <param name="endpoint">Target endpoint to extract parameters for</param>
    /// <returns>List of parameters for the endpoint</returns>
    Task<List<ApiParameter>> ExtractParametersForEndpointAsync(string content, string endpoint);

    /// <summary>
    /// Detects authentication method used by the API
    /// </summary>
    /// <param name="content">Raw documentation content</param>
    /// <returns>Detected authentication method</returns>
    AuthenticationMethod DetectAuthenticationMethod(string content);

    /// <summary>
    /// Extracts base URL from documentation content
    /// </summary>
    /// <param name="content">Raw documentation content</param>
    /// <returns>Base URL of the API</returns>
    string ExtractBaseUrl(string content);

    /// <summary>
    /// Extracts required headers from documentation content
    /// </summary>
    /// <param name="content">Raw documentation content</param>
    /// <returns>List of required headers</returns>
    List<string> ExtractRequiredHeaders(string content);
}

/// <summary>
/// Configuration extracted from documentation content
/// </summary>
public class DocumentationConfig
{
    public Dictionary<string, string> Settings { get; set; } = new();
    public string BaseUrl { get; set; } = string.Empty;
    public AuthenticationMethod AuthenticationMethod { get; set; } = AuthenticationMethod.None;
    public List<string> RequiredHeaders { get; set; } = new();
}