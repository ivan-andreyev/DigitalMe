using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Backup;

/// <summary>
/// Service interface for database backup execution operations
/// </summary>
public interface IBackupExecutor
{
    /// <summary>
    /// Creates a backup of the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Backup result with file path and status</returns>
    Task<BackupResult> CreateBackupAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a pre-recovery backup of current database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pre-recovery backup result</returns>
    Task<BackupResult> CreatePreRecoveryBackupAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available database backups
    /// </summary>
    /// <returns>Collection of backup information</returns>
    Task<IEnumerable<BackupInfo>> GetAvailableBackupsAsync();

    /// <summary>
    /// Restores database from a backup file
    /// </summary>
    /// <param name="backupPath">Path to backup file to restore from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recovery result</returns>
    Task<RecoveryResult> RestoreFromBackupAsync(string backupPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests recovery procedure without actually performing the restore
    /// </summary>
    /// <param name="backupPath">Path to backup file to test</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recovery test result</returns>
    Task<RecoveryTestResult> TestRecoveryAsync(string backupPath, CancellationToken cancellationToken = default);
}