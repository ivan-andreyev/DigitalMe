using DigitalMe.Integrations.External.Slack.Models;

namespace DigitalMe.Integrations.External.Slack.Services;

/// <summary>
/// Service for Slack file operations
/// </summary>
public interface ISlackFileService
{
    /// <summary>
    /// Upload a file to a Slack channel
    /// </summary>
    Task<SlackFileResponse> UploadFileAsync(string channel, Stream file, string filename, string? title = null, string? initialComment = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a file
    /// </summary>
    Task<bool> DeleteFileAsync(string fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get file information
    /// </summary>
    Task<SlackFile?> GetFileInfoAsync(string fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of files in a channel
    /// </summary>
    Task<IEnumerable<SlackFile>> GetChannelFilesAsync(string channel, int limit = 100, CancellationToken cancellationToken = default);
}