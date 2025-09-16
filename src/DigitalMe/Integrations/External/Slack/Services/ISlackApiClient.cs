using DigitalMe.Integrations.External.Slack.Models;

namespace DigitalMe.Integrations.External.Slack.Services;

/// <summary>
/// Interface for low-level Slack API HTTP client
/// </summary>
public interface ISlackApiClient : IDisposable
{
    /// <summary>
    /// Set the bot token for authentication
    /// </summary>
    void SetBotToken(string botToken);

    /// <summary>
    /// Make a GET request to Slack API
    /// </summary>
    Task<T?> GetAsync<T>(string endpoint, Dictionary<string, string>? parameters = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Make a POST request to Slack API
    /// </summary>
    Task<T?> PostAsync<T>(string endpoint, object? data = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Upload a file to Slack API
    /// </summary>
    Task<T?> UploadFileAsync<T>(string endpoint, Stream fileStream, string filename, Dictionary<string, string>? formData = null, CancellationToken cancellationToken = default) where T : class;
}