using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Backup;

/// <summary>
/// Service interface for database backup operations
/// </summary>
public interface IDatabaseBackupService
{
    /// <summary>
    /// Creates a backup of the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Backup result with file path and status</returns>
    Task<BackupResult> CreateBackupAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available database backups
    /// </summary>
    /// <returns>Collection of backup information</returns>
    Task<IEnumerable<BackupInfo>> GetAvailableBackupsAsync();

    /// <summary>
    /// Validates backup integrity
    /// </summary>
    /// <param name="backupPath">Path to backup file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<BackupValidationResult> ValidateBackupAsync(string backupPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old backups based on retention policy
    /// </summary>
    /// <param name="retentionDays">Number of days to retain backups</param>
    /// <param name="maxBackups">Maximum number of backups to keep</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cleanup result</returns>
    Task<BackupCleanupResult> CleanupBackupsAsync(int retentionDays = 7, int maxBackups = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets backup statistics and health information
    /// </summary>
    /// <returns>Backup health status</returns>
    Task<BackupHealthStatus> GetBackupHealthAsync();

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

    /// <summary>
    /// Creates a pre-recovery backup of current database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pre-recovery backup result</returns>
    Task<BackupResult> CreatePreRecoveryBackupAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a backup operation
/// </summary>
public record BackupResult
{
    public bool Success { get; init; }
    public string? BackupPath { get; init; }
    public long BackupSizeBytes { get; init; }
    public DateTime BackupTimestamp { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Information about a backup file
/// </summary>
public record BackupInfo
{
    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsValid { get; init; } = true;
    
    public string FormattedSize => FormatBytes(SizeBytes);
    
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

/// <summary>
/// Result of backup validation
/// </summary>
public record BackupValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public long FileSizeBytes { get; init; }
    public bool IntegrityCheckPassed { get; init; }
    public TimeSpan ValidationDuration { get; init; }
}

/// <summary>
/// Result of backup cleanup operation
/// </summary>
public record BackupCleanupResult
{
    public bool Success { get; init; }
    public int BackupsRemoved { get; init; }
    public int BackupsRetained { get; init; }
    public long SpaceFreedBytes { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Backup system health status
/// </summary>
public record BackupHealthStatus
{
    public bool IsHealthy { get; init; }
    public int TotalBackups { get; init; }
    public DateTime? LastBackupTime { get; init; }
    public long TotalBackupSizeBytes { get; init; }
    public TimeSpan? TimeSinceLastBackup { get; init; }
    public IEnumerable<string> Issues { get; init; } = Enumerable.Empty<string>();
    
    public string FormattedTotalSize => FormatBytes(TotalBackupSizeBytes);
    
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

/// <summary>
/// Result of a database recovery operation
/// </summary>
public record RecoveryResult
{
    public bool Success { get; init; }
    public string? BackupPath { get; init; }
    public string? PreRecoveryBackupPath { get; init; }
    public DateTime RecoveryTimestamp { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
    public long RestoredDataSizeBytes { get; init; }
    public RecoveryDetails Details { get; init; } = new();
}

/// <summary>
/// Detailed information about recovery operation
/// </summary>
public record RecoveryDetails
{
    public bool PreRecoveryBackupCreated { get; init; }
    public bool DatabaseStopped { get; init; }
    public bool BackupValidated { get; init; }
    public bool DatabaseReplaced { get; init; }
    public bool DatabaseStarted { get; init; }
    public bool IntegrityVerified { get; init; }
    public string[] Steps { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Result of recovery test operation
/// </summary>
public record RecoveryTestResult
{
    public bool CanRecover { get; init; }
    public bool BackupValid { get; init; }
    public bool SufficientSpace { get; init; }
    public bool DatabaseAccessible { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan TestDuration { get; init; }
    public RecoveryRequirements Requirements { get; init; } = new();
}

/// <summary>
/// Requirements for recovery operation
/// </summary>
public record RecoveryRequirements
{
    public long BackupSizeBytes { get; init; }
    public long RequiredFreeSpaceBytes { get; init; }
    public long AvailableFreeSpaceBytes { get; init; }
    public bool RequiresApplicationStop { get; init; }
    public TimeSpan EstimatedDuration { get; init; }
}