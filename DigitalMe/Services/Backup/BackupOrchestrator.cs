using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Backup;

/// <summary>
/// Backup orchestrator service that coordinates backup operations
/// </summary>
public class BackupOrchestrator : IDatabaseBackupService
{
    private readonly ILogger<BackupOrchestrator> _logger;
    private readonly IBackupExecutor _executor;
    private readonly IBackupValidator _validator;
    private readonly IBackupCleanup _cleanup;

    public BackupOrchestrator(
        ILogger<BackupOrchestrator> logger,
        IBackupExecutor executor,
        IBackupValidator validator,
        IBackupCleanup cleanup)
    {
        _logger = logger;
        _executor = executor;
        _validator = validator;
        _cleanup = cleanup;
    }

    public async Task<BackupResult> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Orchestrating backup creation");
        return await _executor.CreateBackupAsync(cancellationToken);
    }

    public async Task<IEnumerable<BackupInfo>> GetAvailableBackupsAsync()
    {
        _logger.LogDebug("Orchestrating backup discovery");
        return await _executor.GetAvailableBackupsAsync();
    }

    public async Task<BackupValidationResult> ValidateBackupAsync(string backupPath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Orchestrating backup validation for {BackupPath}", backupPath);
        return await _validator.ValidateBackupAsync(backupPath, cancellationToken);
    }

    public async Task<BackupCleanupResult> CleanupBackupsAsync(int retentionDays = 7, int maxBackups = 30, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Orchestrating backup cleanup with retention: {Days} days, max: {Max} backups", retentionDays, maxBackups);
        return await _cleanup.CleanupBackupsAsync(retentionDays, maxBackups, cancellationToken);
    }

    public async Task<BackupHealthStatus> GetBackupHealthAsync()
    {
        _logger.LogDebug("Orchestrating backup health check");
        return await _validator.GetBackupHealthAsync();
    }

    public async Task<RecoveryResult> RestoreFromBackupAsync(string backupPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Orchestrating database recovery from {BackupPath}", backupPath);
        return await _executor.RestoreFromBackupAsync(backupPath, cancellationToken);
    }

    public async Task<RecoveryTestResult> TestRecoveryAsync(string backupPath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Orchestrating recovery test for {BackupPath}", backupPath);
        return await _executor.TestRecoveryAsync(backupPath, cancellationToken);
    }

    public async Task<BackupResult> CreatePreRecoveryBackupAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Orchestrating pre-recovery backup creation");
        return await _executor.CreatePreRecoveryBackupAsync(cancellationToken);
    }
}