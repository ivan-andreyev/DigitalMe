using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Learning.Documentation.HttpContentFetching;

/// <summary>
/// HTTP documentation content fetcher implementation
/// Single responsibility: Retrieve documentation content from HTTP sources
/// Extracted from AutoDocumentationParser to resolve SRP violations
/// </summary>
public class DocumentationFetcher : IDocumentationFetcher
{
    private readonly ILogger<DocumentationFetcher> _logger;
    private readonly HttpClient _httpClient;

    public DocumentationFetcher(ILogger<DocumentationFetcher> logger, HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<string?> FetchDocumentationContentAsync(string url)
    {
        try
        {
            _logger.LogDebug("Fetching documentation content from {Url}", url);
            
            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Successfully fetched {ContentLength} characters from {Url}", content.Length, url);
            
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch documentation from {Url}", url);
            return null;
        }
    }
}