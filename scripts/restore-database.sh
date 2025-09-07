#!/bin/bash
# DigitalMe Database Restore Script
# This script restores the database from a backup file

set -euo pipefail

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
DATA_DIR="${PROJECT_ROOT}/DigitalMe/data"
BACKUP_DIR="${PROJECT_ROOT}/backups"
DATABASE_FILE="digitalme.db"
DATABASE_PATH="${DATA_DIR}/${DATABASE_FILE}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging function
log() {
    echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')]${NC} $1"
}

error() {
    echo -e "${RED}[ERROR]${NC} $1" >&2
}

warn() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

# Help function
show_help() {
    cat << EOF
DigitalMe Database Restore Script

USAGE:
    $0 [OPTIONS] <backup-file>

ARGUMENTS:
    backup-file         Path to the backup file to restore from
                       Can be absolute path or filename (looks in backup directory)

OPTIONS:
    -h, --help         Show this help message
    -f, --force        Skip confirmations and proceed with restore
    -t, --test         Test restore process without actually restoring
    -b, --pre-backup   Create pre-recovery backup before restore
    -v, --verify       Verify restored database integrity
    --backup-dir DIR   Custom backup directory (default: ${BACKUP_DIR})
    --data-dir DIR     Custom data directory (default: ${DATA_DIR})

EXAMPLES:
    # Restore from backup file (with confirmation)
    $0 digitalme_20231201_120000.db

    # Test restore process
    $0 --test digitalme_20231201_120000.db

    # Force restore with pre-backup and verification
    $0 --force --pre-backup --verify /path/to/backup.db

SAFETY:
    - Always creates pre-recovery backup unless --no-pre-backup is specified
    - Verifies backup integrity before restore
    - Validates restored database after restore
    - Requires confirmation unless --force is used

EOF
}

# Parse command line arguments
BACKUP_FILE=""
FORCE=false
TEST_ONLY=false
PRE_BACKUP=true
VERIFY=true
CUSTOM_BACKUP_DIR=""
CUSTOM_DATA_DIR=""

while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            show_help
            exit 0
            ;;
        -f|--force)
            FORCE=true
            shift
            ;;
        -t|--test)
            TEST_ONLY=true
            shift
            ;;
        -b|--pre-backup)
            PRE_BACKUP=true
            shift
            ;;
        --no-pre-backup)
            PRE_BACKUP=false
            shift
            ;;
        -v|--verify)
            VERIFY=true
            shift
            ;;
        --no-verify)
            VERIFY=false
            shift
            ;;
        --backup-dir)
            CUSTOM_BACKUP_DIR="$2"
            shift 2
            ;;
        --data-dir)
            CUSTOM_DATA_DIR="$2"
            shift 2
            ;;
        -*)
            error "Unknown option: $1"
            exit 1
            ;;
        *)
            if [[ -z "$BACKUP_FILE" ]]; then
                BACKUP_FILE="$1"
            else
                error "Multiple backup files specified. Only one is allowed."
                exit 1
            fi
            shift
            ;;
    esac
done

# Apply custom directories if provided
if [[ -n "$CUSTOM_BACKUP_DIR" ]]; then
    BACKUP_DIR="$CUSTOM_BACKUP_DIR"
fi
if [[ -n "$CUSTOM_DATA_DIR" ]]; then
    DATA_DIR="$CUSTOM_DATA_DIR"
    DATABASE_PATH="${DATA_DIR}/${DATABASE_FILE}"
fi

# Validate arguments
if [[ -z "$BACKUP_FILE" ]]; then
    error "Backup file is required."
    echo "Use '$0 --help' for usage information."
    exit 1
fi

# Resolve backup file path
if [[ ! -f "$BACKUP_FILE" ]]; then
    # Try in backup directory
    CANDIDATE="${BACKUP_DIR}/${BACKUP_FILE}"
    if [[ -f "$CANDIDATE" ]]; then
        BACKUP_FILE="$CANDIDATE"
    else
        error "Backup file not found: $BACKUP_FILE"
        error "Also checked: $CANDIDATE"
        exit 1
    fi
fi

# Get absolute path
BACKUP_FILE="$(readlink -f "$BACKUP_FILE")"

log "DigitalMe Database Restore Starting"
log "Backup file: $BACKUP_FILE"
log "Database path: $DATABASE_PATH"
log "Test mode: $TEST_ONLY"
log "Force mode: $FORCE"

# Validate backup file
validate_backup() {
    local backup_path="$1"
    
    log "Validating backup file..."
    
    if [[ ! -f "$backup_path" ]]; then
        error "Backup file does not exist: $backup_path"
        return 1
    fi
    
    if [[ ! -s "$backup_path" ]]; then
        error "Backup file is empty: $backup_path"
        return 1
    fi
    
    # Test SQLite database integrity
    if ! sqlite3 "$backup_path" "PRAGMA integrity_check;" >/dev/null 2>&1; then
        error "Backup file integrity check failed"
        return 1
    fi
    
    # Check if it's a valid DigitalMe database
    if ! sqlite3 "$backup_path" "SELECT name FROM sqlite_master WHERE type='table';" | grep -q "Users\|Conversations\|Messages"; then
        warn "Backup file may not be a DigitalMe database (missing expected tables)"
    fi
    
    success "Backup file validation passed"
    return 0
}

# Check disk space
check_disk_space() {
    local backup_path="$1"
    local target_dir="$2"
    
    log "Checking disk space requirements..."
    
    local backup_size=$(stat -f%z "$backup_path" 2>/dev/null || stat -c%s "$backup_path")
    local current_size=0
    if [[ -f "$DATABASE_PATH" ]]; then
        current_size=$(stat -f%z "$DATABASE_PATH" 2>/dev/null || stat -c%s "$DATABASE_PATH")
    fi
    
    # Need space for current DB backup + new restore (2x backup size as safety margin)
    local required_space=$((backup_size * 2))
    
    # Get available space
    local available_space
    if command -v df >/dev/null 2>&1; then
        if [[ "$OSTYPE" == "darwin"* ]]; then
            available_space=$(df -f "$target_dir" | tail -1 | awk '{print $4 * 512}')
        else
            available_space=$(df -B1 "$target_dir" | tail -1 | awk '{print $4}')
        fi
    else
        available_space=$((required_space + 1)) # Assume we have enough space
    fi
    
    log "Backup size: $(numfmt --to=iec "$backup_size")"
    log "Required space: $(numfmt --to=iec "$required_space")"
    log "Available space: $(numfmt --to=iec "$available_space")"
    
    if [[ $available_space -lt $required_space ]]; then
        error "Insufficient disk space!"
        error "Required: $(numfmt --to=iec "$required_space"), Available: $(numfmt --to=iec "$available_space")"
        return 1
    fi
    
    success "Disk space check passed"
    return 0
}

# Create pre-recovery backup
create_pre_recovery_backup() {
    if [[ ! -f "$DATABASE_PATH" ]]; then
        log "No existing database found, skipping pre-recovery backup"
        return 0
    fi
    
    local timestamp=$(date +%Y%m%d_%H%M%S)
    local pre_backup_path="${BACKUP_DIR}/digitalme_pre_recovery_${timestamp}.db"
    
    log "Creating pre-recovery backup: $pre_backup_path"
    
    mkdir -p "$BACKUP_DIR"
    
    if cp "$DATABASE_PATH" "$pre_backup_path"; then
        success "Pre-recovery backup created: $(basename "$pre_backup_path")"
        echo "$pre_backup_path"
        return 0
    else
        error "Failed to create pre-recovery backup"
        return 1
    fi
}

# Perform the restore
perform_restore() {
    local backup_path="$1"
    
    log "Starting database restore..."
    
    # Ensure data directory exists
    mkdir -p "$DATA_DIR"
    
    # Backup current database if it exists and pre-backup is enabled
    local pre_backup_path=""
    if [[ "$PRE_BACKUP" == "true" ]]; then
        pre_backup_path=$(create_pre_recovery_backup)
        if [[ $? -ne 0 ]]; then
            error "Pre-recovery backup failed"
            return 1
        fi
    fi
    
    # Stop any running DigitalMe processes (if possible)
    log "Preparing for database replacement..."
    
    # Replace database file
    if [[ -f "$DATABASE_PATH" ]]; then
        log "Removing current database..."
        rm -f "$DATABASE_PATH"
    fi
    
    log "Copying backup to database location..."
    if cp "$backup_path" "$DATABASE_PATH"; then
        success "Database file restored"
    else
        error "Failed to copy backup file"
        
        # Attempt to restore from pre-recovery backup
        if [[ -n "$pre_backup_path" && -f "$pre_backup_path" ]]; then
            warn "Attempting to restore from pre-recovery backup..."
            cp "$pre_backup_path" "$DATABASE_PATH"
            error "Restore failed, but pre-recovery backup was restored"
        fi
        return 1
    fi
    
    # Verify restored database if requested
    if [[ "$VERIFY" == "true" ]]; then
        log "Verifying restored database..."
        if validate_backup "$DATABASE_PATH"; then
            success "Restored database verification passed"
        else
            error "Restored database verification failed"
            
            # Attempt to restore from pre-recovery backup
            if [[ -n "$pre_backup_path" && -f "$pre_backup_path" ]]; then
                warn "Restoring from pre-recovery backup due to verification failure..."
                cp "$pre_backup_path" "$DATABASE_PATH"
                warn "Pre-recovery backup restored"
            fi
            return 1
        fi
    fi
    
    success "Database restore completed successfully"
    
    if [[ -n "$pre_backup_path" ]]; then
        log "Pre-recovery backup saved at: $pre_backup_path"
    fi
    
    return 0
}

# Test restore process
test_restore() {
    local backup_path="$1"
    
    log "Testing restore process (no actual changes will be made)..."
    
    # Validate backup
    if ! validate_backup "$backup_path"; then
        error "Backup validation failed"
        return 1
    fi
    
    # Check disk space
    if ! check_disk_space "$backup_path" "$DATA_DIR"; then
        error "Disk space check failed"
        return 1
    fi
    
    # Check database accessibility
    if [[ -f "$DATABASE_PATH" ]]; then
        if ! sqlite3 "$DATABASE_PATH" "PRAGMA integrity_check;" >/dev/null 2>&1; then
            warn "Current database may be corrupted"
        else
            success "Current database is accessible"
        fi
    else
        log "No existing database found"
    fi
    
    # Estimate restore time (very rough)
    local backup_size=$(stat -f%z "$backup_path" 2>/dev/null || stat -c%s "$backup_path")
    local estimated_seconds=$((backup_size / 10000000)) # Assume 10MB/s
    if [[ $estimated_seconds -lt 1 ]]; then
        estimated_seconds=1
    fi
    
    success "Test restore completed successfully"
    log "Estimated restore time: ${estimated_seconds} seconds"
    log "Pre-recovery backup would be created: $PRE_BACKUP"
    log "Database verification would be performed: $VERIFY"
    
    return 0
}

# Main execution
main() {
    # Test mode
    if [[ "$TEST_ONLY" == "true" ]]; then
        test_restore "$BACKUP_FILE"
        exit $?
    fi
    
    # Validate backup first
    if ! validate_backup "$BACKUP_FILE"; then
        exit 1
    fi
    
    # Check disk space
    if ! check_disk_space "$BACKUP_FILE" "$DATA_DIR"; then
        exit 1
    fi
    
    # Confirmation (unless force mode)
    if [[ "$FORCE" != "true" ]]; then
        warn "DATABASE RESTORE WARNING"
        warn "This operation will replace the current database with the backup."
        warn "Current database: $DATABASE_PATH"
        warn "Backup file: $BACKUP_FILE"
        warn "Pre-recovery backup: $PRE_BACKUP"
        warn ""
        read -p "Are you sure you want to proceed? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            log "Restore cancelled by user"
            exit 0
        fi
    fi
    
    # Perform restore
    if perform_restore "$BACKUP_FILE"; then
        success "DigitalMe database restore completed successfully!"
        log "You may now start the DigitalMe application"
    else
        error "Database restore failed!"
        exit 1
    fi
}

# Run main function
main "$@"