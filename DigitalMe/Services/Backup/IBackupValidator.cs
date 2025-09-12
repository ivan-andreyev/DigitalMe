namespace DigitalMe.Services.Backup;

/// <summary>
/// Service interface for backup validation operations
/// </summary>
public interface IBackupValidator
{
    /// <summary>
    /// Validates backup integrity
    /// </summary>
    /// <param name="backupPath">Path to backup file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<BackupValidationResult> ValidateBackupAsync(string backupPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs quick validation check on backup file
    /// </summary>
    /// <param name="backupPath">Path to backup file</param>
    /// <returns>True if backup appears valid, false otherwise</returns>
    Task<bool> ValidateBackupQuickAsync(string backupPath);

    /// <summary>
    /// Gets backup statistics and health information
    /// </summary>
    /// <returns>Backup health status</returns>
    Task<BackupHealthStatus> GetBackupHealthAsync();
}