using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalMe.Services.Backup;

/// <summary>
/// Database backup cleanup service implementation
/// </summary>
public class BackupCleanup : IBackupCleanup
{
    private readonly ILogger<BackupCleanup> _logger;
    private readonly BackupConfiguration _config;

    public BackupCleanup(
        ILogger<BackupCleanup> logger,
        IOptions<BackupConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    public async Task<BackupCleanupResult> CleanupBackupsAsync(int retentionDays = 7, int maxBackups = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(_config.BackupDirectory))
            {
                return new BackupCleanupResult
                {
                    Success = true,
                    BackupsRemoved = 0,
                    BackupsRetained = 0,
                    SpaceFreedBytes = 0
                };
            }

            var backupFiles = Directory.GetFiles(_config.BackupDirectory, "digitalme_*.db")
                .Select(path => new FileInfo(path))
                .OrderByDescending(fi => fi.CreationTime)
                .ToList();

            var cutoffDate = DateTime.Now.AddDays(-retentionDays);
            var filesToDelete = new List<FileInfo>();
            long spaceToFree = 0;

            // Remove backups older than retention days
            var oldBackups = backupFiles.Where(f => f.CreationTime < cutoffDate).ToList();
            filesToDelete.AddRange(oldBackups);

            // Remove excess backups beyond maxBackups limit
            if (backupFiles.Count > maxBackups)
            {
                var excessBackups = backupFiles.Skip(maxBackups).ToList();
                filesToDelete.AddRange(excessBackups.Except(oldBackups));
            }

            // Calculate space to be freed
            spaceToFree = filesToDelete.Sum(f => f.Length);

            // Delete files
            foreach (var file in filesToDelete)
            {
                try
                {
                    file.Delete();
                    _logger.LogDebug("Deleted backup file: {FileName}", file.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete backup file: {FileName}", file.Name);
                }
            }

            var remainingBackups = Directory.GetFiles(_config.BackupDirectory, "digitalme_*.db").Length;

            _logger.LogInformation("Backup cleanup completed. Removed: {Removed}, Retained: {Retained}, Space freed: {Space}",
                filesToDelete.Count, remainingBackups, FormatBytes(spaceToFree));

            return new BackupCleanupResult
            {
                Success = true,
                BackupsRemoved = filesToDelete.Count,
                BackupsRetained = remainingBackups,
                SpaceFreedBytes = spaceToFree
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup cleanup failed");

            return new BackupCleanupResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private static string FormatBytes(long bytes)
    {
        return bytes switch
        {
            >= 1_000_000_000 => $"{bytes / 1_000_000_000.0:F2} GB",
            >= 1_000_000 => $"{bytes / 1_000_000.0:F2} MB",
            >= 1_000 => $"{bytes / 1_000.0:F2} KB",
            _ => $"{bytes} bytes"
        };
    }
}