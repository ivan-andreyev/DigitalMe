namespace DigitalMe.Services.Backup;

/// <summary>
/// Service interface for backup cleanup and retention operations
/// </summary>
public interface IBackupCleanup
{
    /// <summary>
    /// Cleans up old backups based on retention policy
    /// </summary>
    /// <param name="retentionDays">Number of days to retain backups</param>
    /// <param name="maxBackups">Maximum number of backups to keep</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cleanup result</returns>
    Task<BackupCleanupResult> CleanupBackupsAsync(int retentionDays = 7, int maxBackups = 30, CancellationToken cancellationToken = default);
}