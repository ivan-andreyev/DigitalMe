# ===================================================================
# DigitalMe Database Backup Script (PowerShell)
# SQLite backup strategy for production and development environments
# Compatible with Windows and cross-platform deployment
# ===================================================================

param(
    [Parameter(Position=0)]
    [ValidateSet("backup", "status", "help")]
    [string]$Action = "backup"
)

# Configuration
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

# Environment detection
$RunningInContainer = $env:RUNNING_IN_CONTAINER -eq "true"

if ($RunningInContainer) {
    # Container paths
    $DbPath = "/app/data/digitalme.db"
    $BackupDir = "/app/backups"
    $LogPath = "/app/logs/backup.log"
} else {
    # Local development paths
    $DbPath = Join-Path $ProjectRoot "digitalme.db"
    $BackupDir = Join-Path $ProjectRoot "backups"
    $LogDir = Join-Path $ProjectRoot "logs"
    $LogPath = Join-Path $LogDir "backup.log"
}

# Backup configuration
$RetentionDays = 7
$MaxBackups = 30
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupFile = "digitalme_$Timestamp.db"
$BackupPath = Join-Path $BackupDir $BackupFile

# Logging function
function Write-Log {
    param(
        [string]$Level,
        [string]$Message
    )
    
    $LogTimestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $LogEntry = "[$LogTimestamp] [$Level] $Message"
    
    Write-Host $LogEntry
    
    # Ensure log directory exists
    $LogDirectory = Split-Path -Parent $LogPath
    if (-not (Test-Path $LogDirectory)) {
        New-Item -ItemType Directory -Path $LogDirectory -Force | Out-Null
    }
    
    Add-Content -Path $LogPath -Value $LogEntry -Encoding UTF8
}

# Create directories
function New-BackupDirectories {
    @($BackupDir, (Split-Path -Parent $LogPath)) | ForEach-Object {
        if (-not (Test-Path $_)) {
            New-Item -ItemType Directory -Path $_ -Force | Out-Null
        }
    }
    Write-Log "INFO" "Backup directories created: $BackupDir"
}

# Validate database exists and is accessible
function Test-Database {
    if (-not (Test-Path $DbPath)) {
        Write-Log "ERROR" "Database file not found: $DbPath"
        throw "Database file not found"
    }
    
    # Test SQLite connectivity
    try {
        if (Get-Command sqlite3 -ErrorAction SilentlyContinue) {
            $result = sqlite3 $DbPath "SELECT 1;" 2>&1
            if ($LASTEXITCODE -ne 0) {
                throw "SQLite query failed"
            }
        } else {
            Write-Log "WARNING" "sqlite3 command not found, skipping database connectivity test"
        }
        
        Write-Log "INFO" "Database validation passed: $DbPath"
    }
    catch {
        Write-Log "ERROR" "Database is locked or corrupted: $DbPath"
        throw "Database validation failed: $_"
    }
}

# Create SQLite backup using .backup command (hot backup)
function New-SqliteBackup {
    Write-Log "INFO" "Starting SQLite backup: $DbPath -> $BackupPath"
    
    try {
        # Use SQLite .backup command for hot backup
        if (Get-Command sqlite3 -ErrorAction SilentlyContinue) {
            $backupCmd = "sqlite3 `"$DbPath`" `".backup $BackupPath`""
            Invoke-Expression $backupCmd
            
            if ($LASTEXITCODE -ne 0) {
                throw "SQLite backup command failed with exit code $LASTEXITCODE"
            }
        } else {
            # Fallback to file copy (not ideal for hot backup)
            Write-Log "WARNING" "sqlite3 not found, using file copy (not recommended for active database)"
            Copy-Item $DbPath $BackupPath
        }
        
        Write-Log "INFO" "SQLite backup completed successfully"
        
        # Verify backup file exists and has size > 0
        if (Test-Path $BackupPath) {
            $backupSize = (Get-Item $BackupPath).Length
            if ($backupSize -gt 0) {
                $backupSizeFormatted = if ($backupSize -gt 1MB) { 
                    "{0:N2} MB" -f ($backupSize / 1MB) 
                } elseif ($backupSize -gt 1KB) { 
                    "{0:N2} KB" -f ($backupSize / 1KB) 
                } else { 
                    "$backupSize bytes" 
                }
                
                Write-Log "INFO" "Backup file size: $backupSizeFormatted"
                
                # Verify backup integrity if sqlite3 is available
                if (Get-Command sqlite3 -ErrorAction SilentlyContinue) {
                    $integrityResult = sqlite3 $BackupPath "PRAGMA integrity_check;" 2>&1
                    if ($integrityResult -match "ok") {
                        Write-Log "INFO" "Backup integrity verification passed"
                        return $true
                    } else {
                        Write-Log "ERROR" "Backup integrity verification failed: $integrityResult"
                        Remove-Item $BackupPath -Force
                        return $false
                    }
                } else {
                    Write-Log "WARNING" "sqlite3 not available, skipping integrity check"
                    return $true
                }
            } else {
                Write-Log "ERROR" "Backup file is empty"
                Remove-Item $BackupPath -Force
                return $false
            }
        } else {
            Write-Log "ERROR" "Backup file was not created"
            return $false
        }
    }
    catch {
        Write-Log "ERROR" "SQLite backup failed: $_"
        if (Test-Path $BackupPath) {
            Remove-Item $BackupPath -Force
        }
        return $false
    }
}

# Cleanup old backups based on retention policy
function Remove-OldBackups {
    Write-Log "INFO" "Starting backup cleanup (retention: $RetentionDays days, max: $MaxBackups files)"
    
    if (-not (Test-Path $BackupDir)) {
        Write-Log "INFO" "Backup directory does not exist, skipping cleanup"
        return
    }
    
    # Get all backup files
    $backupFiles = Get-ChildItem -Path $BackupDir -Filter "digitalme_*.db" | Sort-Object LastWriteTime -Descending
    
    # Remove backups older than retention days
    $cutoffDate = (Get-Date).AddDays(-$RetentionDays)
    $oldBackups = $backupFiles | Where-Object { $_.LastWriteTime -lt $cutoffDate }
    
    if ($oldBackups) {
        $removedCount = $oldBackups.Count
        $oldBackups | Remove-Item -Force
        Write-Log "INFO" "Removed $removedCount backup(s) older than $RetentionDays days"
    }
    
    # Keep only the latest MAX_BACKUPS files
    $remainingBackups = Get-ChildItem -Path $BackupDir -Filter "digitalme_*.db" | Sort-Object LastWriteTime -Descending
    
    if ($remainingBackups.Count -gt $MaxBackups) {
        $excessBackups = $remainingBackups | Select-Object -Skip $MaxBackups
        $excessCount = $excessBackups.Count
        $excessBackups | Remove-Item -Force
        Write-Log "INFO" "Removed $excessCount excess backup(s) to maintain max limit of $MaxBackups"
    }
    
    # Report current backup status
    $finalCount = (Get-ChildItem -Path $BackupDir -Filter "digitalme_*.db" -ErrorAction SilentlyContinue).Count
    Write-Log "INFO" "Backup cleanup completed. Remaining backups: $finalCount"
}

# Generate backup report
function New-BackupReport {
    Write-Log "INFO" "Generating backup report"
    
    $reportPath = Join-Path $BackupDir "backup_report_$Timestamp.txt"
    $reportContent = @()
    
    $reportContent += "# DigitalMe Database Backup Report"
    $reportContent += "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss K')"
    $reportContent += ""
    
    $reportContent += "## Backup Details"
    $reportContent += "- Source Database: $DbPath"
    $reportContent += "- Backup File: $BackupPath"
    
    if (Test-Path $BackupPath) {
        $backupSize = (Get-Item $BackupPath).Length
        $backupSizeFormatted = if ($backupSize -gt 1MB) { 
            "{0:N2} MB" -f ($backupSize / 1MB) 
        } elseif ($backupSize -gt 1KB) { 
            "{0:N2} KB" -f ($backupSize / 1KB) 
        } else { 
            "$backupSize bytes" 
        }
        $reportContent += "- Backup Size: $backupSizeFormatted"
    } else {
        $reportContent += "- Backup Size: N/A"
    }
    
    $reportContent += "- Retention Policy: $RetentionDays days / $MaxBackups files max"
    $reportContent += ""
    
    $reportContent += "## Available Backups"
    $backupFiles = Get-ChildItem -Path $BackupDir -Filter "digitalme_*.db" -ErrorAction SilentlyContinue
    
    if ($backupFiles) {
        $backupFiles | Sort-Object LastWriteTime -Descending | ForEach-Object {
            $size = if ($_.Length -gt 1MB) { 
                "{0:N2} MB" -f ($_.Length / 1MB) 
            } elseif ($_.Length -gt 1KB) { 
                "{0:N2} KB" -f ($_.Length / 1KB) 
            } else { 
                "$($_.Length) bytes" 
            }
            $reportContent += "- $($_.Name) | $size | $($_.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))"
        }
    } else {
        $reportContent += "No backups found"
    }
    
    $reportContent | Out-File -FilePath $reportPath -Encoding UTF8
    Write-Log "INFO" "Backup report generated: backup_report_$Timestamp.txt"
}

# Show backup status
function Show-BackupStatus {
    Write-Host "Backup Directory: $BackupDir"
    Write-Host "Available Backups:"
    
    if (Test-Path $BackupDir) {
        $backupFiles = Get-ChildItem -Path $BackupDir -Filter "digitalme_*.db" -ErrorAction SilentlyContinue
        
        if ($backupFiles) {
            $backupFiles | Sort-Object LastWriteTime -Descending | ForEach-Object {
                $size = if ($_.Length -gt 1MB) { 
                    "{0:N2} MB" -f ($_.Length / 1MB) 
                } elseif ($_.Length -gt 1KB) { 
                    "{0:N2} KB" -f ($_.Length / 1KB) 
                } else { 
                    "$($_.Length) bytes" 
                }
                Write-Host "  $($_.Name) | $size | $($_.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))"
            }
        } else {
            Write-Host "  No backups found"
        }
    } else {
        Write-Host "  Backup directory does not exist"
    }
}

# Main backup process
function Start-BackupProcess {
    Write-Log "INFO" "=== DigitalMe Database Backup Started ==="
    Write-Log "INFO" "Environment: $(if ($RunningInContainer) { 'container' } else { 'local' })"
    Write-Log "INFO" "Database: $DbPath"
    Write-Log "INFO" "Backup Directory: $BackupDir"
    
    try {
        New-BackupDirectories
        Test-Database
        
        if (New-SqliteBackup) {
            Remove-OldBackups
            New-BackupReport
            Write-Log "INFO" "=== Backup Process Completed Successfully ==="
            Write-Host "SUCCESS: Database backup completed: $BackupPath"
            exit 0
        } else {
            Write-Log "ERROR" "=== Backup Process Failed ==="
            Write-Host "ERROR: Database backup failed. Check logs: $LogPath"
            exit 1
        }
    }
    catch {
        Write-Log "ERROR" "=== Backup Process Failed ==="
        Write-Log "ERROR" "Exception: $_"
        Write-Host "ERROR: Database backup failed with exception: $_"
        Write-Host "Check logs: $LogPath"
        exit 1
    }
}

# Show help
function Show-Help {
    Write-Host "Usage: .\backup-database.ps1 [backup|status|help]"
    Write-Host "  backup  - Perform database backup (default)"
    Write-Host "  status  - Show backup status and available backups"  
    Write-Host "  help    - Show this help message"
}

# Handle script arguments
switch ($Action) {
    "backup" { Start-BackupProcess }
    "status" { Show-BackupStatus }
    "help" { Show-Help }
    default { 
        Write-Host "Unknown option: $Action"
        Show-Help
        exit 1
    }
}