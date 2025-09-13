using System.Threading.Tasks;

namespace DigitalMe.Services.Learning.Documentation.HttpContentFetching;

/// <summary>
/// Interface for fetching documentation content from HTTP sources
/// Focused responsibility: HTTP request handling and content retrieval
/// Part of AutoDocumentationParser refactoring to resolve SRP violations
/// </summary>
public interface IDocumentationFetcher
{
    /// <summary>
    /// Fetches documentation content from the specified URL
    /// </summary>
    /// <param name="url">The documentation URL to fetch content from</param>
    /// <returns>Raw HTML/text content or null if fetch fails</returns>
    Task<string?> FetchDocumentationContentAsync(string url);
}