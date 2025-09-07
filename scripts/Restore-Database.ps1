# DigitalMe Database Restore PowerShell Script
# This script restores the database from a backup file

param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$BackupFile,
    
    [switch]$Force,
    [switch]$Test,
    [switch]$PreBackup = $true,
    [switch]$NoPreBackup,
    [switch]$Verify = $true,
    [switch]$NoVerify,
    [string]$BackupDir,
    [string]$DataDir,
    [switch]$Help
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Get script directory and project root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

# Default directories
$DefaultDataDir = Join-Path $ProjectRoot "DigitalMe\data"
$DefaultBackupDir = Join-Path $ProjectRoot "backups"
$DatabaseFile = "digitalme.db"

# Apply custom directories if provided
$ActualDataDir = if ($DataDir) { $DataDir } else { $DefaultDataDir }
$ActualBackupDir = if ($BackupDir) { $BackupDir } else { $DefaultBackupDir }
$DatabasePath = Join-Path $ActualDataDir $DatabaseFile

# Handle switches
if ($NoPreBackup) { $PreBackup = $false }
if ($NoVerify) { $Verify = $false }

# Help function
function Show-Help {
    @"
DigitalMe Database Restore Script (PowerShell)

USAGE:
    .\Restore-Database.ps1 [OPTIONS] -BackupFile <backup-file>

PARAMETERS:
    -BackupFile         Path to the backup file to restore from
                       Can be absolute path or filename (looks in backup directory)

OPTIONS:
    -Help              Show this help message
    -Force             Skip confirmations and proceed with restore
    -Test              Test restore process without actually restoring
    -PreBackup         Create pre-recovery backup before restore (default: true)
    -NoPreBackup       Skip pre-recovery backup
    -Verify            Verify restored database integrity (default: true)
    -NoVerify          Skip database verification
    -BackupDir DIR     Custom backup directory (default: $DefaultBackupDir)
    -DataDir DIR       Custom data directory (default: $ActualDataDir)

EXAMPLES:
    # Restore from backup file (with confirmation)
    .\Restore-Database.ps1 -BackupFile "digitalme_20231201_120000.db"

    # Test restore process
    .\Restore-Database.ps1 -BackupFile "digitalme_20231201_120000.db" -Test

    # Force restore with pre-backup and verification
    .\Restore-Database.ps1 -BackupFile "C:\path\to\backup.db" -Force -PreBackup -Verify

SAFETY:
    - Always creates pre-recovery backup unless -NoPreBackup is specified
    - Verifies backup integrity before restore
    - Validates restored database after restore
    - Requires confirmation unless -Force is used

"@
}

# Logging functions
function Write-Log {
    param([string]$Message)
    Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $Message" -ForegroundColor Blue
}

function Write-Error-Log {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Write-Warning-Log {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

# Show help if requested
if ($Help) {
    Show-Help
    exit 0
}

# Resolve backup file path
if (-not (Test-Path $BackupFile)) {
    $CandidatePath = Join-Path $ActualBackupDir $BackupFile
    if (Test-Path $CandidatePath) {
        $BackupFile = $CandidatePath
    } else {
        Write-Error-Log "Backup file not found: $BackupFile"
        Write-Error-Log "Also checked: $CandidatePath"
        exit 1
    }
}

# Get absolute path
$BackupFile = Resolve-Path $BackupFile | Select-Object -ExpandProperty Path

Write-Log "DigitalMe Database Restore Starting"
Write-Log "Backup file: $BackupFile"
Write-Log "Database path: $DatabasePath"
Write-Log "Test mode: $Test"
Write-Log "Force mode: $Force"

# Validate backup file
function Test-BackupFile {
    param([string]$BackupPath)
    
    Write-Log "Validating backup file..."
    
    if (-not (Test-Path $BackupPath)) {
        Write-Error-Log "Backup file does not exist: $BackupPath"
        return $false
    }
    
    $FileInfo = Get-Item $BackupPath
    if ($FileInfo.Length -eq 0) {
        Write-Error-Log "Backup file is empty: $BackupPath"
        return $false
    }
    
    # Test SQLite database integrity using sqlite3 if available
    $sqlite3Path = Get-Command "sqlite3" -ErrorAction SilentlyContinue
    if ($sqlite3Path) {
        try {
            $result = & sqlite3 $BackupPath "PRAGMA integrity_check;" 2>$null
            if ($LASTEXITCODE -ne 0) {
                Write-Error-Log "Backup file integrity check failed"
                return $false
            }
        } catch {
            Write-Warning-Log "Could not verify backup integrity (sqlite3 error)"
        }
    } else {
        Write-Warning-Log "sqlite3 not found in PATH, skipping integrity check"
    }
    
    Write-Success "Backup file validation passed"
    return $true
}

# Check disk space
function Test-DiskSpace {
    param([string]$BackupPath, [string]$TargetDir)
    
    Write-Log "Checking disk space requirements..."
    
    $BackupSize = (Get-Item $BackupPath).Length
    $CurrentSize = 0
    if (Test-Path $DatabasePath) {
        $CurrentSize = (Get-Item $DatabasePath).Length
    }
    
    # Need space for current DB backup + new restore (2x backup size as safety margin)
    $RequiredSpace = $BackupSize * 2
    
    # Get available space
    $Drive = Get-PSDrive -Name (Split-Path $TargetDir -Qualifier).Replace(":", "") -ErrorAction SilentlyContinue
    if ($Drive) {
        $AvailableSpace = $Drive.Free
    } else {
        $AvailableSpace = $RequiredSpace + 1 # Assume we have enough space
    }
    
    Write-Log "Backup size: $([math]::Round($BackupSize / 1MB, 2)) MB"
    Write-Log "Required space: $([math]::Round($RequiredSpace / 1MB, 2)) MB"
    Write-Log "Available space: $([math]::Round($AvailableSpace / 1MB, 2)) MB"
    
    if ($AvailableSpace -lt $RequiredSpace) {
        Write-Error-Log "Insufficient disk space!"
        Write-Error-Log "Required: $([math]::Round($RequiredSpace / 1MB, 2)) MB, Available: $([math]::Round($AvailableSpace / 1MB, 2)) MB"
        return $false
    }
    
    Write-Success "Disk space check passed"
    return $true
}

# Create pre-recovery backup
function New-PreRecoveryBackup {
    if (-not (Test-Path $DatabasePath)) {
        Write-Log "No existing database found, skipping pre-recovery backup"
        return $null
    }
    
    $Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $PreBackupPath = Join-Path $ActualBackupDir "digitalme_pre_recovery_$Timestamp.db"
    
    Write-Log "Creating pre-recovery backup: $PreBackupPath"
    
    # Ensure backup directory exists
    if (-not (Test-Path $ActualBackupDir)) {
        New-Item -ItemType Directory -Path $ActualBackupDir -Force | Out-Null
    }
    
    try {
        Copy-Item $DatabasePath $PreBackupPath -Force
        Write-Success "Pre-recovery backup created: $(Split-Path $PreBackupPath -Leaf)"
        return $PreBackupPath
    } catch {
        Write-Error-Log "Failed to create pre-recovery backup: $_"
        return $null
    }
}

# Perform the restore
function Invoke-DatabaseRestore {
    param([string]$BackupPath)
    
    Write-Log "Starting database restore..."
    
    # Ensure data directory exists
    if (-not (Test-Path $ActualDataDir)) {
        New-Item -ItemType Directory -Path $ActualDataDir -Force | Out-Null
    }
    
    # Backup current database if it exists and pre-backup is enabled
    $PreBackupPath = $null
    if ($PreBackup) {
        $PreBackupPath = New-PreRecoveryBackup
        if (-not $PreBackupPath -and (Test-Path $DatabasePath)) {
            Write-Error-Log "Pre-recovery backup failed"
            return $false
        }
    }
    
    Write-Log "Preparing for database replacement..."
    
    # Remove current database file
    if (Test-Path $DatabasePath) {
        Write-Log "Removing current database..."
        try {
            Remove-Item $DatabasePath -Force
        } catch {
            Write-Error-Log "Failed to remove current database: $_"
            return $false
        }
    }
    
    # Copy backup to database location
    Write-Log "Copying backup to database location..."
    try {
        Copy-Item $BackupPath $DatabasePath -Force
        Write-Success "Database file restored"
    } catch {
        Write-Error-Log "Failed to copy backup file: $_"
        
        # Attempt to restore from pre-recovery backup
        if ($PreBackupPath -and (Test-Path $PreBackupPath)) {
            Write-Warning-Log "Attempting to restore from pre-recovery backup..."
            try {
                Copy-Item $PreBackupPath $DatabasePath -Force
                Write-Error-Log "Restore failed, but pre-recovery backup was restored"
            } catch {
                Write-Error-Log "Failed to restore pre-recovery backup as well: $_"
            }
        }
        return $false
    }
    
    # Verify restored database if requested
    if ($Verify) {
        Write-Log "Verifying restored database..."
        if (Test-BackupFile $DatabasePath) {
            Write-Success "Restored database verification passed"
        } else {
            Write-Error-Log "Restored database verification failed"
            
            # Attempt to restore from pre-recovery backup
            if ($PreBackupPath -and (Test-Path $PreBackupPath)) {
                Write-Warning-Log "Restoring from pre-recovery backup due to verification failure..."
                try {
                    Copy-Item $PreBackupPath $DatabasePath -Force
                    Write-Warning-Log "Pre-recovery backup restored"
                } catch {
                    Write-Error-Log "Failed to restore pre-recovery backup: $_"
                }
            }
            return $false
        }
    }
    
    Write-Success "Database restore completed successfully"
    
    if ($PreBackupPath) {
        Write-Log "Pre-recovery backup saved at: $PreBackupPath"
    }
    
    return $true
}

# Test restore process
function Test-RestoreProcess {
    param([string]$BackupPath)
    
    Write-Log "Testing restore process (no actual changes will be made)..."
    
    # Validate backup
    if (-not (Test-BackupFile $BackupPath)) {
        Write-Error-Log "Backup validation failed"
        return $false
    }
    
    # Check disk space
    if (-not (Test-DiskSpace $BackupPath $ActualDataDir)) {
        Write-Error-Log "Disk space check failed"
        return $false
    }
    
    # Check database accessibility
    if (Test-Path $DatabasePath) {
        if (Test-BackupFile $DatabasePath) {
            Write-Success "Current database is accessible and valid"
        } else {
            Write-Warning-Log "Current database may be corrupted"
        }
    } else {
        Write-Log "No existing database found"
    }
    
    # Estimate restore time (very rough)
    $BackupSize = (Get-Item $BackupPath).Length
    $EstimatedSeconds = [Math]::Max(1, [Math]::Ceiling($BackupSize / 10MB)) # Assume 10MB/s
    
    Write-Success "Test restore completed successfully"
    Write-Log "Estimated restore time: $EstimatedSeconds seconds"
    Write-Log "Pre-recovery backup would be created: $PreBackup"
    Write-Log "Database verification would be performed: $Verify"
    
    return $true
}

# Main execution
try {
    # Test mode
    if ($Test) {
        $success = Test-RestoreProcess $BackupFile
        exit if ($success) { 0 } else { 1 }
    }
    
    # Validate backup first
    if (-not (Test-BackupFile $BackupFile)) {
        exit 1
    }
    
    # Check disk space
    if (-not (Test-DiskSpace $BackupFile $ActualDataDir)) {
        exit 1
    }
    
    # Confirmation (unless force mode)
    if (-not $Force) {
        Write-Warning-Log "DATABASE RESTORE WARNING"
        Write-Warning-Log "This operation will replace the current database with the backup."
        Write-Warning-Log "Current database: $DatabasePath"
        Write-Warning-Log "Backup file: $BackupFile"
        Write-Warning-Log "Pre-recovery backup: $PreBackup"
        Write-Host ""
        $response = Read-Host "Are you sure you want to proceed? (y/N)"
        if ($response -notmatch '^[Yy]$') {
            Write-Log "Restore cancelled by user"
            exit 0
        }
    }
    
    # Perform restore
    if (Invoke-DatabaseRestore $BackupFile) {
        Write-Success "DigitalMe database restore completed successfully!"
        Write-Log "You may now start the DigitalMe application"
    } else {
        Write-Error-Log "Database restore failed!"
        exit 1
    }
} catch {
    Write-Error-Log "Unexpected error: $_"
    exit 1
}