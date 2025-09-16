using DigitalMe.Integrations.External.Slack.Models;

namespace DigitalMe.Integrations.External.Slack.Services;

/// <summary>
/// Service for Slack file operations
/// </summary>
public class SlackFileService : ISlackFileService
{
    private readonly SlackApiClient _apiClient;
    private readonly ILogger<SlackFileService> _logger;

    public SlackFileService(SlackApiClient apiClient, ILogger<SlackFileService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<SlackFileResponse> UploadFileAsync(string channel, Stream file, string filename, string? title = null, string? initialComment = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📁 Uploading file {Filename} to channel {Channel}", filename, channel);

            var formData = new Dictionary<string, string>
            {
                ["channels"] = channel,
                ["filename"] = filename
            };

            if (!string.IsNullOrEmpty(title))
            {
                formData["title"] = title;
            }

            if (!string.IsNullOrEmpty(initialComment))
            {
                formData["initial_comment"] = initialComment;
            }

            var response = await _apiClient.UploadFileAsync<SlackFileResponse>("files.upload", file, filename, formData, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ File uploaded successfully: {Filename} to channel {Channel}",
                    filename, channel);
                return response;
            }
            else
            {
                _logger.LogError("❌ Failed to upload file {Filename} to channel {Channel}: {Error}",
                    filename, channel, response?.Error ?? "Unknown error");
                return new SlackFileResponse { Ok = false, Error = response?.Error ?? "Failed to upload file" };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception uploading file {Filename} to channel {Channel}", filename, channel);
            return new SlackFileResponse { Ok = false, Error = $"Exception: {ex.Message}" };
        }
    }

    public async Task<bool> DeleteFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🗑️ Deleting file {FileId}", fileId);

            var request = new
            {
                file = fileId
            };

            var response = await _apiClient.PostAsync<SlackApiResponse>("files.delete", request, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ File deleted successfully: {FileId}", fileId);
                return true;
            }
            else
            {
                _logger.LogError("❌ Failed to delete file {FileId}: {Error}",
                    fileId, response?.Error ?? "Unknown error");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception deleting file {FileId}", fileId);
            return false;
        }
    }

    public async Task<SlackFile?> GetFileInfoAsync(string fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📄 Getting file info for {FileId}", fileId);

            var parameters = new Dictionary<string, string>
            {
                ["file"] = fileId
            };

            var response = await _apiClient.GetAsync<SlackFileInfoResponse>("files.info", parameters, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ Retrieved file info for {FileId}: {FileName}",
                    fileId, response.File?.Name ?? "Unknown");
                return response.File;
            }
            else
            {
                _logger.LogError("❌ Failed to get file info for {FileId}: {Error}",
                    fileId, response?.Error ?? "Unknown error");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception getting file info for {FileId}", fileId);
            return null;
        }
    }

    public async Task<IEnumerable<SlackFile>> GetChannelFilesAsync(string channel, int limit = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📂 Getting files from channel {Channel}, limit: {Limit}", channel, limit);

            var parameters = new Dictionary<string, string>
            {
                ["channel"] = channel,
                ["count"] = limit.ToString()
            };

            var response = await _apiClient.GetAsync<SlackFilesResponse>("files.list", parameters, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ Retrieved {FileCount} files from channel {Channel}",
                    response.Files?.Count() ?? 0, channel);
                return response.Files ?? Enumerable.Empty<SlackFile>();
            }
            else
            {
                _logger.LogError("❌ Failed to get files from channel {Channel}: {Error}",
                    channel, response?.Error ?? "Unknown error");
                return Enumerable.Empty<SlackFile>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception getting files from channel {Channel}", channel);
            return Enumerable.Empty<SlackFile>();
        }
    }
}