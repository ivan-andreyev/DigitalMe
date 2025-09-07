# DigitalMe Backup & Recovery Operations Guide

> **PRODUCTION-READY** | Last Updated: 2025-09-07  
> **Status**: ✅ **OPERATIONAL** - Enterprise-grade backup and recovery system

---

## 🎯 OVERVIEW

This guide provides comprehensive documentation for the DigitalMe database backup and recovery system. The system is designed with enterprise-grade reliability, automated scheduling, safety mechanisms, and cross-platform support.

### **System Architecture**
- **Automated Backup**: Cron-scheduled background service
- **API Management**: REST endpoints for backup operations
- **Cross-Platform**: Windows PowerShell + Unix Bash scripts  
- **Safety First**: Pre-recovery backups, validation, integrity checks
- **Production Ready**: Container support, logging, monitoring

---

## 📋 QUICK REFERENCE

### **Emergency Recovery Commands**
```bash
# Test recovery process (safe - no changes)
./scripts/restore-database.sh --test backup_file.db

# Emergency recovery with safety checks
./scripts/restore-database.sh --force --pre-backup --verify backup_file.db
```

### **Manual Backup Commands**
```bash
# Create immediate backup (Bash)
./scripts/backup-database.sh

# Create immediate backup (PowerShell)
./scripts/backup-database.ps1

# View backup status
./scripts/backup-database.sh status
```

### **API Endpoints**
- **Create Backup**: `POST /api/backup`
- **List Backups**: `GET /api/backup` 
- **Backup Health**: `GET /api/backup/health`
- **Recovery Info**: `GET /api/backup/recovery-info/{filename}`
- **Restore Database**: `POST /api/backup/restore/{filename}?confirmRestore=true`

---

## 🔧 BACKUP SYSTEM

### **1. Automated Backup Service**

**Service**: `BackupSchedulerService` (Background Service)  
**Default Schedule**: Configurable via `BackupConfiguration.BackupSchedule` (cron format)  
**Retention Policy**: 7 days, max 30 backups (configurable)

#### **Configuration**
```json
{
  "Backup": {
    "AutoBackup": true,
    "BackupSchedule": "0 2 * * *",    // Daily at 2 AM
    "RetentionDays": 7,
    "MaxBackups": 30,
    "AutoCleanup": true,
    "BackupDirectory": "/app/backups"  // Container path
  }
}
```

#### **Service Features**
- **Hot Backup**: SQLite `.backup` command (no application downtime)
- **Integrity Validation**: Automatic backup verification after creation
- **Auto Cleanup**: Removes old backups based on retention policy
- **Health Monitoring**: Reports backup system health status
- **Error Recovery**: Robust error handling with detailed logging

#### **Backup File Naming**
```
digitalme_YYYYMMDD_HHMMSS.db
Example: digitalme_20250907_143022.db
```

### **2. Manual Backup Scripts**

#### **Bash Script** (`scripts/backup-database.sh`)
```bash
# Basic backup
./backup-database.sh backup

# Check backup status
./backup-database.sh status

# Help
./backup-database.sh help
```

**Features:**
- Cross-platform compatibility (Linux, macOS, Windows with WSL)
- Container environment detection
- SQLite integrity checking
- Automatic cleanup
- Detailed logging
- Backup reports

#### **PowerShell Script** (`scripts/backup-database.ps1`)
```powershell
# Basic backup
./backup-database.ps1 backup

# Check backup status  
./backup-database.ps1 status

# Help
./backup-database.ps1 help
```

**Features:**
- Windows native support
- Container environment detection
- SQLite3 detection with fallback
- Automatic cleanup
- Detailed logging
- Backup reports

### **3. Backup API Endpoints**

#### **Create Manual Backup**
```http
POST /api/backup
Authorization: Bearer <jwt-token>
```

**Response:**
```json
{
  "success": true,
  "backupPath": "/app/backups/digitalme_20250907_143022.db",
  "backupSizeBytes": 2048576,
  "backupTimestamp": "2025-09-07T14:30:22Z",
  "duration": "00:00:03.456"
}
```

#### **List Available Backups**
```http
GET /api/backup
Authorization: Bearer <jwt-token>
```

**Response:**
```json
[
  {
    "filePath": "/app/backups/digitalme_20250907_143022.db",
    "fileName": "digitalme_20250907_143022.db",
    "sizeBytes": 2048576,
    "formattedSize": "2.00 MB",
    "createdAt": "2025-09-07T14:30:22Z",
    "isValid": true
  }
]
```

#### **Backup System Health**
```http
GET /api/backup/health
Authorization: Bearer <jwt-token>
```

**Response:**
```json
{
  "isHealthy": true,
  "totalBackups": 15,
  "lastBackupTime": "2025-09-07T14:30:22Z",
  "totalBackupSizeBytes": 30720000,
  "formattedTotalSize": "30.72 MB",
  "timeSinceLastBackup": "02:15:30",
  "issues": []
}
```

---

## 🔄 RECOVERY SYSTEM

### **1. Recovery Scripts**

#### **Bash Recovery Script** (`scripts/restore-database.sh`)

**Basic Usage:**
```bash
# Test recovery (safe, no changes)
./restore-database.sh --test backup_file.db

# Interactive recovery with confirmation
./restore-database.sh backup_file.db

# Force recovery with safety features
./restore-database.sh --force --pre-backup --verify backup_file.db

# Custom directories
./restore-database.sh --backup-dir /custom/backups --data-dir /custom/data backup_file.db
```

**Safety Features:**
- **Pre-Recovery Backup**: Creates backup of current database before restore
- **Validation**: Verifies backup file integrity before restore
- **Disk Space Check**: Ensures sufficient disk space available
- **Confirmation Required**: Interactive confirmation unless `--force` used
- **Rollback**: Automatic rollback to pre-recovery backup if restore fails
- **Verification**: Optional post-restore database integrity check

#### **PowerShell Recovery Script** (`scripts/Restore-Database.ps1`)
```powershell
# Test recovery
./Restore-Database.ps1 -Test backup_file.db

# Interactive recovery
./Restore-Database.ps1 backup_file.db

# Force recovery with features
./Restore-Database.ps1 -Force -PreBackup -Verify backup_file.db
```

### **2. Recovery API Endpoints**

#### **Test Recovery (Safe)**
```http
POST /api/backup/test-recovery/digitalme_20250907_143022.db
Authorization: Bearer <jwt-token>
```

**Response:**
```json
{
  "canRecover": true,
  "backupValid": true,
  "sufficientSpace": true,
  "databaseAccessible": true,
  "testDuration": "00:00:01.234",
  "requirements": {
    "backupSizeBytes": 2048576,
    "requiredFreeSpaceBytes": 4097152,
    "availableFreeSpaceBytes": 104857600,
    "requiresApplicationStop": false,
    "estimatedDuration": "00:00:05.000"
  }
}
```

#### **Get Recovery Information**
```http
GET /api/backup/recovery-info/digitalme_20250907_143022.db
Authorization: Bearer <jwt-token>
```

**Response:**
```json
{
  "backupInfo": {
    "fileName": "digitalme_20250907_143022.db",
    "formattedSize": "2.00 MB",
    "createdAt": "2025-09-07T14:30:22Z",
    "isValid": true
  },
  "canRecover": true,
  "requirements": {
    "estimatedDuration": "00:00:05.000"
  },
  "recommendations": [],
  "preRecoveryBackupRecommended": true
}
```

#### **Restore Database**
```http
POST /api/backup/restore/digitalme_20250907_143022.db?confirmRestore=true
Authorization: Bearer <jwt-token>
```

**⚠️ WARNING**: This operation replaces the current database!

**Response:**
```json
{
  "success": true,
  "backupPath": "/app/backups/digitalme_20250907_143022.db",
  "preRecoveryBackupPath": "/app/backups/digitalme_pre_recovery_20250907_163045.db",
  "recoveryTimestamp": "2025-09-07T16:30:45Z",
  "duration": "00:00:04.567",
  "restoredDataSizeBytes": 2048576,
  "details": {
    "preRecoveryBackupCreated": true,
    "databaseStopped": false,
    "backupValidated": true,
    "databaseReplaced": true,
    "databaseStarted": false,
    "integrityVerified": true,
    "steps": [
      "Backup validation passed",
      "Pre-recovery backup created",
      "Database file replaced",
      "Integrity verification passed"
    ]
  }
}
```

---

## 🛠️ OPERATIONAL PROCEDURES

### **1. Daily Operations**

#### **Morning Backup Health Check**
```bash
# Check if automated backups are running
curl -H "Authorization: Bearer $JWT_TOKEN" \
     http://localhost:5000/api/backup/health

# Review backup logs
tail -f logs/backup.log
```

#### **Weekly Backup Verification**
```bash
# List all backups
curl -H "Authorization: Bearer $JWT_TOKEN" \
     http://localhost:5000/api/backup

# Test latest backup
LATEST_BACKUP=$(curl -s -H "Authorization: Bearer $JWT_TOKEN" \
                http://localhost:5000/api/backup | jq -r '.[0].fileName')

curl -X POST -H "Authorization: Bearer $JWT_TOKEN" \
     http://localhost:5000/api/backup/test-recovery/$LATEST_BACKUP
```

### **2. Emergency Recovery Procedures**

#### **Scenario 1: Database Corruption Detected**

1. **Immediate Response:**
   ```bash
   # Stop the application (if possible)
   # Test recovery capability
   ./scripts/restore-database.sh --test latest_backup.db
   ```

2. **Recovery Execution:**
   ```bash
   # Execute recovery with safety checks
   ./scripts/restore-database.sh --force --pre-backup --verify latest_backup.db
   ```

3. **Post-Recovery Validation:**
   ```bash
   # Restart application
   # Verify system functionality
   # Monitor logs for issues
   ```

#### **Scenario 2: Accidental Data Loss**

1. **Identify Recovery Point:**
   ```bash
   # List available backups
   ./scripts/backup-database.sh status
   
   # Or via API
   curl -H "Authorization: Bearer $JWT_TOKEN" \
        http://localhost:5000/api/backup
   ```

2. **Test Recovery:**
   ```bash
   # Validate backup before restore
   ./scripts/restore-database.sh --test selected_backup.db
   ```

3. **Execute Recovery:**
   ```bash
   # Perform recovery with confirmations
   ./scripts/restore-database.sh --pre-backup --verify selected_backup.db
   ```

#### **Scenario 3: Production Migration/Maintenance**

1. **Pre-Maintenance Backup:**
   ```bash
   # Create maintenance backup
   curl -X POST -H "Authorization: Bearer $JWT_TOKEN" \
        http://localhost:5000/api/backup
   ```

2. **Maintenance Window:**
   ```bash
   # Stop application
   # Perform maintenance tasks
   # Test application startup
   ```

3. **Rollback Plan (if needed):**
   ```bash
   # Identify pre-maintenance backup
   # Execute recovery if issues detected
   ./scripts/restore-database.sh --force pre_maintenance_backup.db
   ```

### **3. Container Environment Operations**

#### **Docker Container Backup**
```bash
# Backup from running container
docker exec digitalme-app /app/scripts/backup-database.sh

# Check backup status
docker exec digitalme-app /app/scripts/backup-database.sh status

# Copy backup out of container
docker cp digitalme-app:/app/backups/ ./local-backups/
```

#### **Container Recovery**
```bash
# Copy backup into container
docker cp local-backups/backup_file.db digitalme-app:/app/backups/

# Test recovery
docker exec digitalme-app /app/scripts/restore-database.sh --test backup_file.db

# Execute recovery
docker exec -it digitalme-app /app/scripts/restore-database.sh backup_file.db
```

### **4. Monitoring and Alerting**

#### **Backup Monitoring Metrics**
- Backup success rate
- Backup file sizes over time
- Time since last successful backup
- Available disk space in backup directory
- Backup operation duration

#### **Health Check Endpoints**
```bash
# Application health (includes backup system)
curl http://localhost:5000/health

# Detailed backup health
curl -H "Authorization: Bearer $JWT_TOKEN" \
     http://localhost:5000/api/backup/health
```

#### **Log Monitoring**
```bash
# Backup service logs
tail -f logs/backup.log

# Application logs (includes backup operations)
docker logs digitalme-app | grep -i backup

# System logs
tail -f /var/log/syslog | grep digitalme
```

---

## 📊 CONFIGURATION REFERENCE

### **1. Application Configuration**

#### **appsettings.json**
```json
{
  "Backup": {
    "AutoBackup": true,
    "BackupSchedule": "0 2 * * *",
    "RetentionDays": 7,
    "MaxBackups": 30,
    "AutoCleanup": true,
    "BackupDirectory": "/app/backups"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/app/data/digitalme.db"
  }
}
```

#### **appsettings.Production.json**
```json
{
  "Backup": {
    "BackupSchedule": "0 2 * * *",      // Daily at 2 AM
    "RetentionDays": 30,                // Keep 30 days in production
    "MaxBackups": 100,                  // More backups in production
    "BackupDirectory": "/app/backups"
  }
}
```

### **2. Environment Variables**

#### **Container Deployment**
```bash
# Docker environment variables
RUNNING_IN_CONTAINER=true
ASPNETCORE_ENVIRONMENT=Production
ANTHROPIC_API_KEY=your_api_key
JWT_KEY=your_jwt_key

# Backup configuration
BACKUP_SCHEDULE="0 2 * * *"
BACKUP_RETENTION_DAYS=30
BACKUP_MAX_FILES=100
```

### **3. Directory Structure**

#### **Local Development**
```
DigitalMe/
├── digitalme.db              # Main database
├── backups/                  # Backup directory
│   ├── digitalme_*.db       # Backup files
│   └── backup_report_*.txt  # Backup reports
├── logs/                     # Log directory
│   └── backup.log          # Backup logs
└── scripts/                  # Backup scripts
    ├── backup-database.sh
    ├── backup-database.ps1
    ├── restore-database.sh
    └── Restore-Database.ps1
```

#### **Container Deployment**
```
/app/
├── data/
│   └── digitalme.db         # Main database
├── backups/                 # Backup directory (volume mount)
│   ├── digitalme_*.db      # Backup files
│   └── backup_report_*.txt # Backup reports
├── logs/                    # Log directory (volume mount)
│   └── backup.log          # Backup logs
└── scripts/                 # Backup scripts
    ├── backup-database.sh
    └── restore-database.sh
```

---

## 🔒 SECURITY CONSIDERATIONS

### **1. Access Control**
- **API Authentication**: All backup/recovery endpoints require JWT authentication
- **Rate Limiting**: Backup endpoints have rate limiting enabled
- **File Permissions**: Backup files have restricted access permissions
- **Container Security**: Backup directory uses secure volume mounts

### **2. Data Protection**
- **Encryption**: Backup files stored with filesystem-level encryption
- **Network Security**: API endpoints use HTTPS in production
- **Audit Logging**: All backup/recovery operations are logged with user context
- **Secrets Management**: Database connection strings managed via secure configuration

### **3. Backup File Security**
```bash
# Set restrictive permissions on backup files
chmod 600 /app/backups/*.db

# Ensure backup directory is secure
chmod 700 /app/backups/

# Regular security audit of backup files
find /app/backups/ -name "*.db" -not -perm 600 -exec chmod 600 {} \;
```

---

## 🚨 TROUBLESHOOTING

### **1. Common Issues**

#### **"Database is locked" Error**
```bash
# Check for active connections
lsof digitalme.db

# Wait for operations to complete, then retry
./scripts/backup-database.sh backup
```

#### **"Insufficient disk space" Error**
```bash
# Check available space
df -h /app/backups/

# Clean up old backups manually
./scripts/backup-database.sh status
rm /app/backups/oldest_backup.db

# Or trigger automatic cleanup via API
curl -X POST -H "Authorization: Bearer $JWT_TOKEN" \
     http://localhost:5000/api/backup/cleanup
```

#### **"Backup integrity check failed" Error**
```bash
# Manually verify backup file
sqlite3 backup_file.db "PRAGMA integrity_check;"

# Check source database integrity
sqlite3 digitalme.db "PRAGMA integrity_check;"

# If source is corrupted, use last known good backup
./scripts/restore-database.sh --test last_good_backup.db
```

### **2. Recovery Failures**

#### **Recovery Process Interruption**
```bash
# If recovery was interrupted, check for pre-recovery backup
ls -la /app/backups/digitalme_pre_recovery_*.db

# Restore from pre-recovery backup if needed
./scripts/restore-database.sh --force pre_recovery_backup.db
```

#### **Post-Recovery Database Issues**
```bash
# Verify database integrity after recovery
sqlite3 digitalme.db "PRAGMA integrity_check;"

# Check application logs
tail -f logs/application.log

# Test application functionality
curl http://localhost:5000/health
```

### **3. Container-Specific Issues**

#### **Volume Mount Problems**
```bash
# Check volume mounts
docker inspect digitalme-app | grep Mounts -A 10

# Ensure backup directory is writable
docker exec digitalme-app ls -la /app/backups/
docker exec digitalme-app touch /app/backups/test
```

#### **Permission Issues**
```bash
# Fix backup directory permissions
docker exec digitalme-app chown -R app:app /app/backups/
docker exec digitalme-app chmod -R 755 /app/backups/
```

---

## 📞 SUPPORT CONTACTS

### **Escalation Procedures**
1. **Level 1**: Check logs and attempt automated recovery
2. **Level 2**: Contact system administrator with error details
3. **Level 3**: Contact development team with full system logs

### **Emergency Contacts**
- **System Administrator**: [Contact Info]
- **Development Team**: [Contact Info]  
- **Database Administrator**: [Contact Info]

### **Documentation Updates**
- **Last Updated**: 2025-09-07
- **Version**: 1.0
- **Next Review**: 2025-10-07

---

*This documentation covers the production-ready DigitalMe backup and recovery system. All procedures have been tested and validated for production use.*