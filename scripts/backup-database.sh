#!/bin/bash
# ===================================================================
# DigitalMe Database Backup Script
# SQLite backup strategy for production and development environments
# ===================================================================

set -euo pipefail

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" &> /dev/null && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Environment detection
if [ "${RUNNING_IN_CONTAINER:-false}" == "true" ]; then
    # Container paths
    DB_PATH="/app/data/digitalme.db"
    BACKUP_DIR="/app/backups"
    LOG_PATH="/app/logs/backup.log"
else
    # Local development paths
    DB_PATH="$PROJECT_ROOT/digitalme.db"
    BACKUP_DIR="$PROJECT_ROOT/backups"
    LOG_PATH="$PROJECT_ROOT/logs/backup.log"
fi

# Backup configuration
RETENTION_DAYS=7
MAX_BACKUPS=30
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="digitalme_${TIMESTAMP}.db"
BACKUP_PATH="$BACKUP_DIR/$BACKUP_FILE"

# Logging function
log() {
    local level="$1"
    shift
    local message="$*"
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    echo "[$timestamp] [$level] $message" | tee -a "$LOG_PATH"
}

# Create directories
create_directories() {
    mkdir -p "$BACKUP_DIR" "$LOG_PATH"
    log "INFO" "Backup directories created: $BACKUP_DIR"
}

# Validate database exists and is accessible
validate_database() {
    if [ ! -f "$DB_PATH" ]; then
        log "ERROR" "Database file not found: $DB_PATH"
        exit 1
    fi
    
    # Check if database is locked
    if ! sqlite3 "$DB_PATH" "SELECT 1;" > /dev/null 2>&1; then
        log "ERROR" "Database is locked or corrupted: $DB_PATH"
        exit 1
    fi
    
    log "INFO" "Database validation passed: $DB_PATH"
}

# Create SQLite backup using .backup command (hot backup)
create_sqlite_backup() {
    log "INFO" "Starting SQLite backup: $DB_PATH -> $BACKUP_PATH"
    
    # Use SQLite .backup command for hot backup (VACUUM INTO for older versions)
    if sqlite3 "$DB_PATH" ".backup $BACKUP_PATH" 2>&1; then
        log "INFO" "SQLite backup completed successfully"
        
        # Verify backup integrity
        if sqlite3 "$BACKUP_PATH" "PRAGMA integrity_check;" | grep -q "ok"; then
            log "INFO" "Backup integrity verification passed"
            
            # Get backup file size
            backup_size=$(du -h "$BACKUP_PATH" | cut -f1)
            log "INFO" "Backup file size: $backup_size"
            
            return 0
        else
            log "ERROR" "Backup integrity verification failed"
            rm -f "$BACKUP_PATH"
            return 1
        fi
    else
        log "ERROR" "SQLite backup failed"
        return 1
    fi
}

# Cleanup old backups based on retention policy
cleanup_old_backups() {
    log "INFO" "Starting backup cleanup (retention: ${RETENTION_DAYS} days, max: ${MAX_BACKUPS} files)"
    
    # Remove backups older than retention days
    if command -v find > /dev/null 2>&1; then
        local removed_count=$(find "$BACKUP_DIR" -name "digitalme_*.db" -mtime +${RETENTION_DAYS} -type f -print | wc -l)
        find "$BACKUP_DIR" -name "digitalme_*.db" -mtime +${RETENTION_DAYS} -type f -delete
        log "INFO" "Removed $removed_count backup(s) older than ${RETENTION_DAYS} days"
    fi
    
    # Keep only the latest MAX_BACKUPS files
    local total_backups=$(ls -1 "$BACKUP_DIR"/digitalme_*.db 2>/dev/null | wc -l)
    if [ "$total_backups" -gt "$MAX_BACKUPS" ]; then
        local excess_count=$((total_backups - MAX_BACKUPS))
        ls -1t "$BACKUP_DIR"/digitalme_*.db | tail -n "$excess_count" | xargs rm -f
        log "INFO" "Removed $excess_count excess backup(s) to maintain max limit of $MAX_BACKUPS"
    fi
    
    # Report current backup status
    local remaining_backups=$(ls -1 "$BACKUP_DIR"/digitalme_*.db 2>/dev/null | wc -l)
    log "INFO" "Backup cleanup completed. Remaining backups: $remaining_backups"
}

# Generate backup report
generate_backup_report() {
    log "INFO" "Generating backup report"
    
    echo "# DigitalMe Database Backup Report" > "$BACKUP_DIR/backup_report_${TIMESTAMP}.txt"
    echo "Generated: $(date '+%Y-%m-%d %H:%M:%S %Z')" >> "$BACKUP_DIR/backup_report_${TIMESTAMP}.txt"
    echo "" >> "$BACKUP_DIR/backup_report_${TIMESTAMP}.txt"
    
    echo "## Backup Details" >> "$BACKUP_DIR/backup_report_${TIMESTAMP}.txt"
    echo "- Source Database: $DB_PATH" >> "$BACKUP_DIR/backup_report_${TIMESTAMP}.txt"
    echo "- Backup File: $BACKUP_PATH" >> "$BACKUP_DIR/backup_report_${TIMESTAMP}.txt"
    echo "- Backup Size: $(du -h "$BACKUP_PATH" 2>/dev/null | cut -f1 || echo 'N/A')" >> "$BACKUP_DIR/backup_report_${TIMESTAMP}.txt"
    echo "- Retention Policy: ${RETENTION_DAYS} days / ${MAX_BACKUPS} files max" >> "$BACKUP_DIR/backup_report_${TIMESTAMP}.txt"
    
    echo "" >> "$BACKUP_DIR/backup_report_${TIMESTAMP}.txt"
    echo "## Available Backups" >> "$BACKUP_DIR/backup_report_${TIMESTAMP}.txt"
    ls -lah "$BACKUP_DIR"/digitalme_*.db 2>/dev/null >> "$BACKUP_DIR/backup_report_${TIMESTAMP}.txt" || echo "No backups found" >> "$BACKUP_DIR/backup_report_${TIMESTAMP}.txt"
    
    log "INFO" "Backup report generated: backup_report_${TIMESTAMP}.txt"
}

# Main backup process
main() {
    log "INFO" "=== DigitalMe Database Backup Started ==="
    log "INFO" "Environment: ${RUNNING_IN_CONTAINER:-local}"
    log "INFO" "Database: $DB_PATH"
    log "INFO" "Backup Directory: $BACKUP_DIR"
    
    create_directories
    validate_database
    
    if create_sqlite_backup; then
        cleanup_old_backups
        generate_backup_report
        log "INFO" "=== Backup Process Completed Successfully ==="
        echo "SUCCESS: Database backup completed: $BACKUP_PATH"
        exit 0
    else
        log "ERROR" "=== Backup Process Failed ==="
        echo "ERROR: Database backup failed. Check logs: $LOG_PATH"
        exit 1
    fi
}

# Handle script arguments
case "${1:-backup}" in
    backup|--backup)
        main
        ;;
    status|--status)
        echo "Backup Directory: $BACKUP_DIR"
        echo "Available Backups:"
        ls -lah "$BACKUP_DIR"/digitalme_*.db 2>/dev/null || echo "No backups found"
        ;;
    help|--help)
        echo "Usage: $0 [backup|status|help]"
        echo "  backup  - Perform database backup (default)"
        echo "  status  - Show backup status and available backups"
        echo "  help    - Show this help message"
        ;;
    *)
        echo "Unknown option: $1"
        echo "Use '$0 help' for usage information"
        exit 1
        ;;
esac