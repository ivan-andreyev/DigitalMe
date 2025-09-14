using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCrontab;

namespace DigitalMe.Services.Backup;

/// <summary>
/// Background service that schedules automatic database backups
/// </summary>
public class BackupSchedulerService : BackgroundService
{
    private readonly ILogger<BackupSchedulerService> _logger;
    private readonly IDatabaseBackupService _backupService;
    private readonly BackupConfiguration _config;
    private readonly IServiceProvider _serviceProvider;
    private CrontabSchedule? _schedule;
    private DateTime _nextRun;

    public BackupSchedulerService(
        ILogger<BackupSchedulerService> logger,
        IDatabaseBackupService backupService,
        IOptions<BackupConfiguration> config,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _backupService = backupService;
        _config = config.Value;
        _serviceProvider = serviceProvider;

        if (_config.AutoBackup)
        {
            try
            {
                _schedule = CrontabSchedule.Parse(_config.BackupSchedule);
                _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
                _logger.LogInformation("Backup scheduler initialized. Next backup: {NextRun}", _nextRun);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse backup schedule: {Schedule}", _config.BackupSchedule);
            }
        }
        else
        {
            _logger.LogInformation("Automatic backups disabled in configuration");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.AutoBackup || _schedule == null)
        {
            _logger.LogInformation("Automatic backup scheduling is disabled");
            return;
        }

        _logger.LogInformation("Backup scheduler service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;

                if (now >= _nextRun)
                {
                    _logger.LogInformation("Starting scheduled backup");

                    await PerformScheduledBackupAsync(stoppingToken);

                    // Calculate next run time
                    _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
                    _logger.LogInformation("Next scheduled backup: {NextRun}", _nextRun);
                }

                // Wait until the next check (check every minute)
                var delay = TimeSpan.FromMinutes(1);
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Backup scheduler service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in backup scheduler service");

                // Wait before retrying
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task PerformScheduledBackupAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Create backup
            var backupResult = await _backupService.CreateBackupAsync(cancellationToken);

            if (backupResult.Success)
            {
                _logger.LogInformation("Scheduled backup completed successfully. " +
                    "File: {BackupPath}, Size: {Size}, Duration: {Duration}ms",
                    backupResult.BackupPath,
                    FormatBytes(backupResult.BackupSizeBytes),
                    backupResult.Duration.TotalMilliseconds);

                // Perform cleanup if enabled
                if (_config.AutoCleanup)
                {
                    await PerformBackupCleanupAsync(cancellationToken);
                }

                // Report backup health
                await ReportBackupHealthAsync();
            }
            else
            {
                _logger.LogError("Scheduled backup failed: {Error}", backupResult.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled backup operation failed");
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogDebug("Scheduled backup operation completed in {Duration}ms", stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task PerformBackupCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            var cleanupResult = await _backupService.CleanupBackupsAsync(
                _config.RetentionDays,
                _config.MaxBackups,
                cancellationToken);

            if (cleanupResult.Success)
            {
                _logger.LogInformation("Backup cleanup completed. " +
                    "Removed: {Removed}, Retained: {Retained}, Space freed: {SpaceFreed}",
                    cleanupResult.BackupsRemoved,
                    cleanupResult.BackupsRetained,
                    FormatBytes(cleanupResult.SpaceFreedBytes));
            }
            else
            {
                _logger.LogWarning("Backup cleanup failed: {Error}", cleanupResult.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup cleanup operation failed");
        }
    }

    private async Task ReportBackupHealthAsync()
    {
        try
        {
            var healthStatus = await _backupService.GetBackupHealthAsync();

            if (healthStatus.IsHealthy)
            {
                _logger.LogInformation("Backup system healthy. " +
                    "Total backups: {TotalBackups}, Total size: {TotalSize}, Last backup: {LastBackup}",
                    healthStatus.TotalBackups,
                    healthStatus.FormattedTotalSize,
                    healthStatus.LastBackupTime);
            }
            else
            {
                _logger.LogWarning("Backup system health issues detected: {Issues}",
                    string.Join(", ", healthStatus.Issues));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check backup system health");
        }
    }

    /// <summary>
    /// Trigger an immediate backup (useful for testing or manual triggers)
    /// </summary>
    public async Task<BackupResult> TriggerBackupAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Manual backup triggered");

        try
        {
            var result = await _backupService.CreateBackupAsync(cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Manual backup completed successfully");

                // Perform cleanup if enabled
                if (_config.AutoCleanup)
                {
                    await PerformBackupCleanupAsync(cancellationToken);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual backup failed");

            return new BackupResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                BackupTimestamp = DateTime.UtcNow
            };
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping backup scheduler service");
        await base.StopAsync(cancellationToken);
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
