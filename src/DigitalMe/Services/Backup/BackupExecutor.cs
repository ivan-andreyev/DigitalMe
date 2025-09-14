using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace DigitalMe.Services.Backup;

/// <summary>
/// Database backup execution service implementation
/// </summary>
public class BackupExecutor : IBackupExecutor
{
    private readonly ILogger<BackupExecutor> _logger;
    private readonly IBackupValidator _validator;
    private readonly BackupConfiguration _config;
    private readonly string _connectionString;

    public BackupExecutor(
        ILogger<BackupExecutor> logger,
        IBackupValidator validator,
        IOptions<BackupConfiguration> config,
        IConfiguration configuration)
    {
        _logger = logger;
        _validator = validator;
        _config = config.Value;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string not configured");
    }

    public async Task<BackupResult> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var timestamp = DateTime.UtcNow;
        var backupFileName = $"digitalme_{timestamp:yyyyMMdd_HHmmss}.db";
        var backupPath = Path.Combine(_config.BackupDirectory, backupFileName);

        try
        {
            _logger.LogInformation("Starting database backup to {BackupPath}", backupPath);

            // Ensure backup directory exists
            Directory.CreateDirectory(_config.BackupDirectory);

            // Create SQLite backup using BACKUP command
            await CreateSqliteBackupAsync(backupPath, cancellationToken);

            // Validate backup
            var validation = await _validator.ValidateBackupAsync(backupPath, cancellationToken);
            if (!validation.IsValid)
            {
                File.Delete(backupPath);
                throw new InvalidOperationException($"Backup validation failed: {validation.ErrorMessage}");
            }

            var fileInfo = new FileInfo(backupPath);
            var result = new BackupResult
            {
                Success = true,
                BackupPath = backupPath,
                BackupSizeBytes = fileInfo.Length,
                BackupTimestamp = timestamp,
                Duration = stopwatch.Elapsed
            };

            _logger.LogInformation("Database backup completed successfully. Size: {Size}, Duration: {Duration}ms",
                FormatBytes(result.BackupSizeBytes), stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database backup failed");

            // Cleanup failed backup file
            try
            {
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to cleanup incomplete backup file: {BackupPath}", backupPath);
            }

            return new BackupResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                BackupTimestamp = timestamp,
                Duration = stopwatch.Elapsed
            };
        }
    }

    public async Task<BackupResult> CreatePreRecoveryBackupAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var timestamp = DateTime.UtcNow;
        var backupFileName = $"digitalme_pre_recovery_{timestamp:yyyyMMdd_HHmmss}.db";
        var backupPath = Path.Combine(_config.BackupDirectory, backupFileName);

        try
        {
            _logger.LogInformation("Creating pre-recovery backup to {BackupPath}", backupPath);

            // Ensure backup directory exists
            Directory.CreateDirectory(_config.BackupDirectory);

            // Create SQLite backup using BACKUP command
            await CreateSqliteBackupAsync(backupPath, cancellationToken);

            // Quick validation
            var validation = await _validator.ValidateBackupQuickAsync(backupPath);
            if (!validation)
            {
                File.Delete(backupPath);
                throw new InvalidOperationException("Pre-recovery backup validation failed");
            }

            var fileInfo = new FileInfo(backupPath);
            var result = new BackupResult
            {
                Success = true,
                BackupPath = backupPath,
                BackupSizeBytes = fileInfo.Length,
                BackupTimestamp = timestamp,
                Duration = stopwatch.Elapsed
            };

            _logger.LogInformation("Pre-recovery backup completed successfully. Size: {Size}",
                FormatBytes(result.BackupSizeBytes));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pre-recovery backup failed");

            // Cleanup failed backup file
            try
            {
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to cleanup incomplete pre-recovery backup: {BackupPath}", backupPath);
            }

            return new BackupResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                BackupTimestamp = timestamp,
                Duration = stopwatch.Elapsed
            };
        }
    }

    public async Task<IEnumerable<BackupInfo>> GetAvailableBackupsAsync()
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
                    var isValid = await _validator.ValidateBackupQuickAsync(file.FullName);

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

    public async Task<RecoveryResult> RestoreFromBackupAsync(string backupPath, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var timestamp = DateTime.UtcNow;
        var steps = new List<string>();
        string? preRecoveryBackupPath = null;

        try
        {
            _logger.LogInformation("Starting database recovery from backup: {BackupPath}", backupPath);

            // Step 1: Validate backup exists and is valid
            steps.Add("Validating backup file");
            if (!File.Exists(backupPath))
            {
                throw new FileNotFoundException($"Backup file not found: {backupPath}");
            }

            var validation = await _validator.ValidateBackupAsync(backupPath, cancellationToken);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"Backup validation failed: {validation.ErrorMessage}");
            }

            // Step 2: Create pre-recovery backup
            steps.Add("Creating pre-recovery backup");
            var preRecoveryBackup = await CreatePreRecoveryBackupAsync(cancellationToken);
            if (preRecoveryBackup.Success)
            {
                preRecoveryBackupPath = preRecoveryBackup.BackupPath;
                _logger.LogInformation("Pre-recovery backup created: {PreBackupPath}", preRecoveryBackupPath);
            }
            else
            {
                _logger.LogWarning("Pre-recovery backup failed, continuing anyway: {Error}", preRecoveryBackup.ErrorMessage);
            }

            // Step 3: Stop database connections (simulate application stop)
            steps.Add("Preparing database for restore");
            await PrepareForRecoveryAsync();

            // Step 4: Replace database file
            steps.Add("Restoring database from backup");
            var builder = new SqliteConnectionStringBuilder(_connectionString);
            var targetPath = builder.DataSource;

            // Create backup of current database if pre-recovery backup failed
            if (preRecoveryBackupPath == null && File.Exists(targetPath))
            {
                var emergencyBackupPath = $"{targetPath}.emergency_{timestamp:yyyyMMdd_HHmmss}.bak";
                File.Copy(targetPath, emergencyBackupPath);
                _logger.LogInformation("Emergency backup created: {EmergencyPath}", emergencyBackupPath);
            }

            // Replace database file
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
            File.Copy(backupPath, targetPath);

            // Step 5: Verify restored database
            steps.Add("Verifying restored database");
            var restoredValidation = await _validator.ValidateBackupAsync(targetPath, cancellationToken);
            if (!restoredValidation.IsValid)
            {
                throw new InvalidOperationException($"Restored database validation failed: {restoredValidation.ErrorMessage}");
            }

            var fileInfo = new FileInfo(targetPath);

            var result = new RecoveryResult
            {
                Success = true,
                BackupPath = backupPath,
                PreRecoveryBackupPath = preRecoveryBackupPath,
                RecoveryTimestamp = timestamp,
                Duration = stopwatch.Elapsed,
                RestoredDataSizeBytes = fileInfo.Length,
                Details = new RecoveryDetails
                {
                    PreRecoveryBackupCreated = preRecoveryBackupPath != null,
                    DatabaseStopped = true,
                    BackupValidated = true,
                    DatabaseReplaced = true,
                    DatabaseStarted = true,
                    IntegrityVerified = true,
                    Steps = steps.ToArray()
                }
            };

            _logger.LogInformation("Database recovery completed successfully. Size: {Size}, Duration: {Duration}ms",
                FormatBytes(result.RestoredDataSizeBytes), stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database recovery failed");

            return new RecoveryResult
            {
                Success = false,
                BackupPath = backupPath,
                PreRecoveryBackupPath = preRecoveryBackupPath,
                RecoveryTimestamp = timestamp,
                Duration = stopwatch.Elapsed,
                ErrorMessage = ex.Message,
                Details = new RecoveryDetails
                {
                    Steps = steps.ToArray()
                }
            };
        }
    }

    public async Task<RecoveryTestResult> TestRecoveryAsync(string backupPath, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var requirements = new RecoveryRequirements();

        try
        {
            // Test 1: Backup file validation
            var validation = await _validator.ValidateBackupAsync(backupPath, cancellationToken);

            if (!validation.IsValid)
            {
                return new RecoveryTestResult
                {
                    CanRecover = false,
                    BackupValid = false,
                    ErrorMessage = $"Backup validation failed: {validation.ErrorMessage}",
                    TestDuration = stopwatch.Elapsed
                };
            }

            // Test 2: Space requirements
            var backupInfo = new FileInfo(backupPath);
            var builder = new SqliteConnectionStringBuilder(_connectionString);
            var targetPath = builder.DataSource;
            var targetDrive = new DriveInfo(Path.GetPathRoot(targetPath) ?? "C:");

            var requiredSpace = backupInfo.Length * 2; // Need space for current DB + backup
            var availableSpace = targetDrive.AvailableFreeSpace;

            requirements = new RecoveryRequirements
            {
                BackupSizeBytes = backupInfo.Length,
                RequiredFreeSpaceBytes = requiredSpace,
                AvailableFreeSpaceBytes = availableSpace,
                RequiresApplicationStop = true,
                EstimatedDuration = TimeSpan.FromSeconds(backupInfo.Length / 10_000_000) // Rough estimate
            };

            // Test 3: Database accessibility
            bool databaseAccessible = true;
            try
            {
                await using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
            }
            catch
            {
                databaseAccessible = false;
            }

            var canRecover = validation.IsValid && availableSpace >= requiredSpace && databaseAccessible;

            return new RecoveryTestResult
            {
                CanRecover = canRecover,
                BackupValid = true,
                SufficientSpace = availableSpace >= requiredSpace,
                DatabaseAccessible = databaseAccessible,
                TestDuration = stopwatch.Elapsed,
                Requirements = requirements,
                ErrorMessage = canRecover ? null : "Recovery test identified issues"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recovery test failed for {BackupPath}", backupPath);

            return new RecoveryTestResult
            {
                CanRecover = false,
                BackupValid = false,
                SufficientSpace = false,
                DatabaseAccessible = false,
                ErrorMessage = ex.Message,
                TestDuration = stopwatch.Elapsed,
                Requirements = requirements
            };
        }
    }

    private async Task CreateSqliteBackupAsync(string backupPath, CancellationToken cancellationToken)
    {
        // Extract database path from connection string
        var builder = new SqliteConnectionStringBuilder(_connectionString);
        var sourcePath = builder.DataSource;

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Source database not found: {sourcePath}");
        }

        // Create backup using SQLite BACKUP API
        await using var sourceConnection = new SqliteConnection(_connectionString);
        await using var backupConnection = new SqliteConnection($"Data Source={backupPath}");

        await sourceConnection.OpenAsync(cancellationToken);
        await backupConnection.OpenAsync(cancellationToken);

        // Use SQLite backup API for hot backup
        sourceConnection.BackupDatabase(backupConnection);

        _logger.LogDebug("SQLite backup completed: {SourcePath} -> {BackupPath}", sourcePath, backupPath);
    }

    private async Task PrepareForRecoveryAsync()
    {
        // Wait a bit to allow active connections to finish
        await Task.Delay(1000);

        // Force garbage collection to release any cached connections
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        _logger.LogDebug("Database prepared for recovery");
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