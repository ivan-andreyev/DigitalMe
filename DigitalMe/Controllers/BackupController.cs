using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DigitalMe.Services.Backup;
using Microsoft.AspNetCore.RateLimiting;

namespace DigitalMe.Controllers;

/// <summary>
/// Controller for database backup management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("api")]
public class BackupController : ControllerBase
{
    private readonly ILogger<BackupController> _logger;
    private readonly IDatabaseBackupService _backupService;
    private readonly BackupSchedulerService _schedulerService;

    public BackupController(
        ILogger<BackupController> logger,
        IDatabaseBackupService backupService,
        BackupSchedulerService schedulerService)
    {
        _logger = logger;
        _backupService = backupService;
        _schedulerService = schedulerService;
    }

    /// <summary>
    /// Create a manual database backup
    /// </summary>
    /// <returns>Backup operation result</returns>
    [HttpPost]
    [ProducesResponseType(typeof(BackupResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BackupResult>> CreateBackup(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Manual backup requested by user");
            
            var result = await _schedulerService.TriggerBackupAsync(cancellationToken);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual backup request failed");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Backup operation failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Get list of available database backups
    /// </summary>
    /// <returns>List of backup information</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BackupInfo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BackupInfo>>> GetBackups()
    {
        try
        {
            var backups = await _backupService.GetAvailableBackupsAsync();
            return Ok(backups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve backup list");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Failed to retrieve backups", message = ex.Message });
        }
    }

    /// <summary>
    /// Get backup system health status
    /// </summary>
    /// <returns>Backup health information</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(BackupHealthStatus), StatusCodes.Status200OK)]
    public async Task<ActionResult<BackupHealthStatus>> GetBackupHealth()
    {
        try
        {
            var health = await _backupService.GetBackupHealthAsync();
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve backup health status");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Failed to retrieve backup health", message = ex.Message });
        }
    }

    /// <summary>
    /// Validate a specific backup file
    /// </summary>
    /// <param name="backupFileName">Name of the backup file to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate/{backupFileName}")]
    [ProducesResponseType(typeof(BackupValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BackupValidationResult>> ValidateBackup(
        string backupFileName, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get available backups to find the full path
            var backups = await _backupService.GetAvailableBackupsAsync();
            var backup = backups.FirstOrDefault(b => b.FileName.Equals(backupFileName, StringComparison.OrdinalIgnoreCase));
            
            if (backup == null)
            {
                return NotFound(new { error = "Backup file not found", fileName = backupFileName });
            }

            var validation = await _backupService.ValidateBackupAsync(backup.FilePath, cancellationToken);
            return Ok(validation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup validation failed for {BackupFileName}", backupFileName);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Backup validation failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Clean up old backups based on retention policy
    /// </summary>
    /// <param name="retentionDays">Number of days to retain (optional, uses config default)</param>
    /// <param name="maxBackups">Maximum number of backups to keep (optional, uses config default)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cleanup operation result</returns>
    [HttpPost("cleanup")]
    [ProducesResponseType(typeof(BackupCleanupResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<BackupCleanupResult>> CleanupBackups(
        [FromQuery] int? retentionDays = null,
        [FromQuery] int? maxBackups = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Backup cleanup requested by user. RetentionDays: {RetentionDays}, MaxBackups: {MaxBackups}", 
                retentionDays, maxBackups);

            var result = await _backupService.CleanupBackupsAsync(
                retentionDays ?? 7, 
                maxBackups ?? 30, 
                cancellationToken);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup cleanup failed");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Backup cleanup failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Download a specific backup file
    /// </summary>
    /// <param name="backupFileName">Name of the backup file to download</param>
    /// <returns>Backup file for download</returns>
    [HttpGet("download/{backupFileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EnableRateLimiting("auth")] // More restrictive rate limiting for downloads
    public async Task<IActionResult> DownloadBackup(string backupFileName)
    {
        try
        {
            // Get available backups to find the full path
            var backups = await _backupService.GetAvailableBackupsAsync();
            var backup = backups.FirstOrDefault(b => b.FileName.Equals(backupFileName, StringComparison.OrdinalIgnoreCase));
            
            if (backup == null)
            {
                return NotFound(new { error = "Backup file not found", fileName = backupFileName });
            }

            if (!System.IO.File.Exists(backup.FilePath))
            {
                _logger.LogWarning("Backup file not found on disk: {FilePath}", backup.FilePath);
                return NotFound(new { error = "Backup file not found on disk", fileName = backupFileName });
            }

            _logger.LogInformation("Backup download requested: {BackupFileName}", backupFileName);

            var fileStream = new FileStream(backup.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var contentType = "application/vnd.sqlite3";
            
            return File(fileStream, contentType, backupFileName, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download backup: {BackupFileName}", backupFileName);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Failed to download backup", message = ex.Message });
        }
    }

    /// <summary>
    /// Get backup statistics and metrics
    /// </summary>
    /// <returns>Backup statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetBackupStatistics()
    {
        try
        {
            var backups = await _backupService.GetAvailableBackupsAsync();
            var backupList = backups.ToList();
            var health = await _backupService.GetBackupHealthAsync();

            var statistics = new
            {
                TotalBackups = backupList.Count,
                ValidBackups = backupList.Count(b => b.IsValid),
                InvalidBackups = backupList.Count(b => !b.IsValid),
                TotalSize = health.TotalBackupSizeBytes,
                FormattedTotalSize = health.FormattedTotalSize,
                LastBackupTime = health.LastBackupTime,
                TimeSinceLastBackup = health.TimeSinceLastBackup,
                IsHealthy = health.IsHealthy,
                Issues = health.Issues,
                BackupsByDate = backupList
                    .GroupBy(b => b.CreatedAt.Date)
                    .OrderByDescending(g => g.Key)
                    .Take(30) // Last 30 days
                    .Select(g => new 
                    { 
                        Date = g.Key, 
                        Count = g.Count(), 
                        TotalSize = g.Sum(b => b.SizeBytes),
                        FormattedTotalSize = FormatBytes(g.Sum(b => b.SizeBytes))
                    })
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve backup statistics");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Failed to retrieve backup statistics", message = ex.Message });
        }
    }

    /// <summary>
    /// Test recovery procedure without actually performing the restore
    /// </summary>
    /// <param name="backupFileName">Name of the backup file to test recovery for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recovery test result</returns>
    [HttpPost("test-recovery/{backupFileName}")]
    [ProducesResponseType(typeof(RecoveryTestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EnableRateLimiting("auth")] // More restrictive for recovery operations
    public async Task<ActionResult<RecoveryTestResult>> TestRecovery(
        string backupFileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Recovery test requested for backup: {BackupFileName}", backupFileName);

            // Get available backups to find the full path
            var backups = await _backupService.GetAvailableBackupsAsync();
            var backup = backups.FirstOrDefault(b => b.FileName.Equals(backupFileName, StringComparison.OrdinalIgnoreCase));
            
            if (backup == null)
            {
                return NotFound(new { error = "Backup file not found", fileName = backupFileName });
            }

            var result = await _backupService.TestRecoveryAsync(backup.FilePath, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recovery test failed for {BackupFileName}", backupFileName);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Recovery test failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Restore database from a backup file
    /// WARNING: This operation will replace the current database
    /// </summary>
    /// <param name="backupFileName">Name of the backup file to restore from</param>
    /// <param name="confirmRestore">Confirmation parameter to prevent accidental restores</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recovery operation result</returns>
    [HttpPost("restore/{backupFileName}")]
    [ProducesResponseType(typeof(RecoveryResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EnableRateLimiting("auth")] // Most restrictive for actual recovery
    public async Task<ActionResult<RecoveryResult>> RestoreFromBackup(
        string backupFileName,
        [FromQuery] bool confirmRestore = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!confirmRestore)
            {
                return BadRequest(new 
                { 
                    error = "Confirmation required", 
                    message = "Set confirmRestore=true to confirm database restore operation",
                    warning = "This operation will replace the current database with the backup"
                });
            }

            _logger.LogWarning("DATABASE RESTORE OPERATION INITIATED - Backup: {BackupFileName} by user", backupFileName);

            // Get available backups to find the full path
            var backups = await _backupService.GetAvailableBackupsAsync();
            var backup = backups.FirstOrDefault(b => b.FileName.Equals(backupFileName, StringComparison.OrdinalIgnoreCase));
            
            if (backup == null)
            {
                return NotFound(new { error = "Backup file not found", fileName = backupFileName });
            }

            // Test recovery first
            var testResult = await _backupService.TestRecoveryAsync(backup.FilePath, cancellationToken);
            if (!testResult.CanRecover)
            {
                return BadRequest(new 
                { 
                    error = "Recovery test failed", 
                    message = testResult.ErrorMessage,
                    testResult = testResult
                });
            }

            var result = await _backupService.RestoreFromBackupAsync(backup.FilePath, cancellationToken);
            
            if (result.Success)
            {
                _logger.LogWarning("DATABASE RESTORE COMPLETED SUCCESSFULLY - Backup: {BackupFileName}", backupFileName);
                return Ok(result);
            }
            else
            {
                _logger.LogError("DATABASE RESTORE FAILED - Backup: {BackupFileName}, Error: {Error}", backupFileName, result.ErrorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database restore operation failed for {BackupFileName}", backupFileName);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Database restore failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Create a pre-recovery backup of the current database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pre-recovery backup result</returns>
    [HttpPost("pre-recovery-backup")]
    [ProducesResponseType(typeof(BackupResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BackupResult>> CreatePreRecoveryBackup(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Pre-recovery backup requested by user");
            
            var result = await _backupService.CreatePreRecoveryBackupAsync(cancellationToken);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pre-recovery backup request failed");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Pre-recovery backup failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Get recovery requirements for a specific backup
    /// </summary>
    /// <param name="backupFileName">Name of the backup file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recovery requirements and recommendations</returns>
    [HttpGet("recovery-info/{backupFileName}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetRecoveryInfo(
        string backupFileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get available backups to find the full path
            var backups = await _backupService.GetAvailableBackupsAsync();
            var backup = backups.FirstOrDefault(b => b.FileName.Equals(backupFileName, StringComparison.OrdinalIgnoreCase));
            
            if (backup == null)
            {
                return NotFound(new { error = "Backup file not found", fileName = backupFileName });
            }

            var testResult = await _backupService.TestRecoveryAsync(backup.FilePath, cancellationToken);
            
            var recoveryInfo = new
            {
                BackupInfo = backup,
                CanRecover = testResult.CanRecover,
                Requirements = testResult.Requirements,
                Issues = testResult.CanRecover ? new string[0] : new[] { testResult.ErrorMessage ?? "Unknown issue" },
                Recommendations = new[]
                {
                    testResult.Requirements.RequiresApplicationStop ? "Application must be stopped during recovery" : null,
                    !testResult.SufficientSpace ? "Insufficient disk space available" : null,
                    !testResult.BackupValid ? "Backup file validation failed" : null,
                    !testResult.DatabaseAccessible ? "Current database is not accessible" : null
                }.Where(r => r != null).ToArray(),
                EstimatedDowntime = testResult.Requirements.EstimatedDuration,
                PreRecoveryBackupRecommended = true
            };

            return Ok(recoveryInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recovery info for {BackupFileName}", backupFileName);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Failed to get recovery information", message = ex.Message });
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