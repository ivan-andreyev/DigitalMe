using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace DigitalMe.Services.Backup;

/// <summary>
/// Database backup validation service implementation
/// </summary>
public class BackupValidator : IBackupValidator
{
    private readonly ILogger<BackupValidator> _logger;
    private readonly BackupConfiguration _config;

    public BackupValidator(
        ILogger<BackupValidator> logger,
        IOptions<BackupConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    public async Task<BackupValidationResult> ValidateBackupAsync(string backupPath, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!File.Exists(backupPath))
            {
                return new BackupValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Backup file does not exist",
                    ValidationDuration = stopwatch.Elapsed
                };
            }

            var fileInfo = new FileInfo(backupPath);
            if (fileInfo.Length == 0)
            {
                return new BackupValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Backup file is empty",
                    FileSizeBytes = 0,
                    ValidationDuration = stopwatch.Elapsed
                };
            }

            // Test SQLite database integrity
            var connectionString = $"Data Source={backupPath};Mode=ReadOnly";

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Run PRAGMA integrity_check
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check";

            var result = await command.ExecuteScalarAsync(cancellationToken);
            var integrityCheckPassed = result?.ToString() == "ok";

            return new BackupValidationResult
            {
                IsValid = integrityCheckPassed,
                FileSizeBytes = fileInfo.Length,
                IntegrityCheckPassed = integrityCheckPassed,
                ValidationDuration = stopwatch.Elapsed,
                ErrorMessage = integrityCheckPassed ? null : "Database integrity check failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup validation failed for {BackupPath}", backupPath);

            return new BackupValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message,
                ValidationDuration = stopwatch.Elapsed
            };
        }
    }

    public async Task<bool> ValidateBackupQuickAsync(string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath) || new FileInfo(backupPath).Length == 0)
            {
                return false;
            }

            // Quick check: try to open the database
            var connectionString = $"Data Source={backupPath};Mode=ReadOnly";
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            // Simple query to verify it's a valid SQLite database
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' LIMIT 1";
            await command.ExecuteScalarAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<BackupHealthStatus> GetBackupHealthAsync()
    {
        try
        {
            var backups = await GetAvailableBackupsAsync();
            var backupList = backups.ToList();

            var totalBackups = backupList.Count;
            var lastBackup = backupList.OrderByDescending(b => b.CreatedAt).FirstOrDefault();
            var totalSize = backupList.Sum(b => b.SizeBytes);
            var timeSinceLastBackup = lastBackup != null ? (TimeSpan?)(DateTime.UtcNow - lastBackup.CreatedAt) : null;

            var issues = new List<string>();

            // Check for issues
            if (totalBackups == 0)
            {
                issues.Add("No backups found");
            }

            if (lastBackup == null || timeSinceLastBackup?.TotalHours > 24)
            {
                issues.Add("No recent backup (last 24 hours)");
            }

            var invalidBackups = backupList.Count(b => !b.IsValid);
            if (invalidBackups > 0)
            {
                issues.Add($"{invalidBackups} invalid backup(s) found");
            }

            var isHealthy = issues.Count == 0;

            return new BackupHealthStatus
            {
                IsHealthy = isHealthy,
                TotalBackups = totalBackups,
                LastBackupTime = lastBackup?.CreatedAt,
                TotalBackupSizeBytes = totalSize,
                TimeSinceLastBackup = timeSinceLastBackup,
                Issues = issues
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get backup health status");

            return new BackupHealthStatus
            {
                IsHealthy = false,
                Issues = new[] { $"Health check failed: {ex.Message}" }
            };
        }
    }

    private async Task<IEnumerable<BackupInfo>> GetAvailableBackupsAsync()
    {
        try
        {
            if (!Directory.Exists(_config.BackupDirectory))
            {
                return Enumerable.Empty<BackupInfo>();
            }

            var backupFiles = Directory.GetFiles(_config.BackupDirectory, "digitalme_*.db")
                .Select(path => new FileInfo(path))
                .OrderByDescending(fi => fi.CreationTime);

            var backupInfos = new List<BackupInfo>();

            foreach (var file in backupFiles)
            {
                try
                {
                    // Quick validation to check if file is a valid SQLite database
                    var isValid = await ValidateBackupQuickAsync(file.FullName);

                    backupInfos.Add(new BackupInfo
                    {
                        FilePath = file.FullName,
                        FileName = file.Name,
                        SizeBytes = file.Length,
                        CreatedAt = file.CreationTime,
                        IsValid = isValid
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get info for backup file: {FilePath}", file.FullName);

                    backupInfos.Add(new BackupInfo
                    {
                        FilePath = file.FullName,
                        FileName = file.Name,
                        SizeBytes = file.Length,
                        CreatedAt = file.CreationTime,
                        IsValid = false
                    });
                }
            }

            return backupInfos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available backups");
            return Enumerable.Empty<BackupInfo>();
        }
    }
}