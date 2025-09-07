# DigitalMe Backup & Recovery Runbook

> **EMERGENCY REFERENCE** | Production Operations Runbook  
> **Last Updated**: 2025-09-07

---

## 🚨 EMERGENCY PROCEDURES

### **CRITICAL DATABASE CORRUPTION**

**⚠️ IMMEDIATE ACTIONS (5 minutes):**

1. **Stop Traffic** (if possible):
   ```bash
   # Stop load balancer or reverse proxy
   # Or scale down application instances
   ```

2. **Identify Latest Backup**:
   ```bash
   # Quick backup check
   curl -s -H "Authorization: Bearer $JWT" http://localhost:5000/api/backup | jq '.[0]'
   
   # Or via script
   ./scripts/backup-database.sh status | head -5
   ```

3. **Test Recovery**:
   ```bash
   # CRITICAL: Test before restore!
   ./scripts/restore-database.sh --test latest_backup.db
   ```

4. **Execute Recovery** (if test passes):
   ```bash
   # Force recovery with safety features
   ./scripts/restore-database.sh --force --pre-backup --verify latest_backup.db
   ```

5. **Restart Services**:
   ```bash
   # Restart application
   # Resume traffic
   # Monitor logs
   ```

**⏱️ Expected Recovery Time: 5-15 minutes**

---

### **DATA LOSS INCIDENT**

**📋 RESPONSE CHECKLIST:**

- [ ] Identify incident time and scope
- [ ] Find backup closest to incident time
- [ ] Create current state backup (if database accessible)
- [ ] Test recovery process
- [ ] Execute recovery with confirmation
- [ ] Validate restored data
- [ ] Document incident and resolution

**🔍 BACKUP SELECTION:**
```bash
# List backups by date
curl -s -H "Authorization: Bearer $JWT" \
     http://localhost:5000/api/backup | \
     jq -r '.[] | "\(.createdAt) \(.fileName) \(.formattedSize)"' | \
     sort -r
```

---

## ⚡ QUICK COMMANDS

### **Backup Operations**
```bash
# Create immediate backup
curl -X POST -H "Authorization: Bearer $JWT" http://localhost:5000/api/backup

# Check backup health
curl -s -H "Authorization: Bearer $JWT" http://localhost:5000/api/backup/health | jq

# Manual backup (script)
./scripts/backup-database.sh backup
```

### **Recovery Operations**
```bash
# Test recovery (ALWAYS run first)
./scripts/restore-database.sh --test backup_file.db

# Quick recovery with safety
./scripts/restore-database.sh --force --pre-backup --verify backup_file.db

# Recovery via API
curl -X POST -H "Authorization: Bearer $JWT" \
     "http://localhost:5000/api/backup/restore/backup_file.db?confirmRestore=true"
```

### **Health Checks**
```bash
# Application health
curl http://localhost:5000/health

# Database connectivity
sqlite3 digitalme.db "SELECT COUNT(*) FROM Users;"

# Backup system status
curl -H "Authorization: Bearer $JWT" http://localhost:5000/api/backup/health
```

---

## 📊 MONITORING COMMANDS

### **System Status**
```bash
# Disk space
df -h /app/backups/

# Backup directory size
du -sh /app/backups/

# Latest backup info
ls -lah /app/backups/ | head -5

# Log tail
tail -f logs/backup.log
```

### **Container Environment**
```bash
# Container backup status
docker exec digitalme-app /app/scripts/backup-database.sh status

# Container logs
docker logs digitalme-app --since=1h | grep -i backup

# Container health
docker exec digitalme-app curl http://localhost/health
```

---

## 🔧 MAINTENANCE PROCEDURES

### **Weekly Backup Validation**
```bash
#!/bin/bash
# Weekly backup validation script

echo "=== Weekly Backup Validation ==="
echo "Date: $(date)"

# List all backups
echo "Available backups:"
curl -s -H "Authorization: Bearer $JWT" http://localhost:5000/api/backup | \
jq -r '.[] | "\(.fileName) \(.formattedSize) \(.createdAt)"'

# Test latest backup
LATEST=$(curl -s -H "Authorization: Bearer $JWT" http://localhost:5000/api/backup | jq -r '.[0].fileName')
echo "Testing latest backup: $LATEST"

curl -X POST -H "Authorization: Bearer $JWT" \
     "http://localhost:5000/api/backup/test-recovery/$LATEST" | \
     jq '.canRecover'
```

### **Monthly Cleanup**
```bash
#!/bin/bash
# Monthly backup cleanup

echo "=== Monthly Backup Cleanup ==="

# Current backup stats
curl -s -H "Authorization: Bearer $JWT" http://localhost:5000/api/backup/health | \
jq '{totalBackups, formattedTotalSize, lastBackupTime}'

# Trigger cleanup (remove old backups)
curl -X POST -H "Authorization: Bearer $JWT" \
     "http://localhost:5000/api/backup/cleanup?retentionDays=30&maxBackups=50"

# Post-cleanup stats
echo "After cleanup:"
curl -s -H "Authorization: Bearer $JWT" http://localhost:5000/api/backup/health | \
jq '{totalBackups, formattedTotalSize}'
```

---

## 🎯 TROUBLESHOOTING FLOWCHART

### **Backup Fails**
```
Backup Failed
    ↓
├── Database locked?
│   ├── YES → Wait/retry → Success
│   └── NO → Check disk space
│       ├── Full → Clean up → Retry
│       └── OK → Check permissions → Fix → Retry
├── Permission denied?
│   └── YES → Fix permissions → Retry
└── Other error?
    └── Check logs → Contact admin
```

### **Recovery Fails**
```
Recovery Failed
    ↓
├── Test recovery first
│   ├── PASS → Check confirmation
│   └── FAIL → Check backup integrity
├── Backup corrupted?
│   ├── YES → Use older backup
│   └── NO → Check disk space
├── Space insufficient?
│   ├── YES → Free up space → Retry
│   └── NO → Check permissions
└── Still failing?
    └── Emergency escalation
```

---

## 📱 ESCALATION CONTACTS

### **Incident Severity Levels**

**🔴 CRITICAL (0-15 min response)**
- Database completely inaccessible
- Data corruption affecting all users
- Production down

**🟡 HIGH (0-30 min response)**
- Backup failures for >24 hours
- Recovery test failures
- Performance degradation

**🟢 MEDIUM (0-2 hour response)**
- Backup cleanup issues
- Non-critical errors
- Documentation updates needed

### **Contact Information**
```
Level 1: System Administrator
- Phone: [REDACTED]
- Email: [REDACTED]
- Slack: #system-alerts

Level 2: Development Team  
- On-call: [REDACTED]
- Email: [REDACTED]
- Slack: #development

Level 3: Database Team
- Emergency: [REDACTED]
- Email: [REDACTED]
```

---

## 📋 CHECKLISTS

### **Pre-Maintenance Checklist**
- [ ] Create manual backup
- [ ] Test backup integrity  
- [ ] Verify recovery procedure
- [ ] Document maintenance window
- [ ] Prepare rollback plan
- [ ] Notify stakeholders

### **Post-Incident Checklist**
- [ ] Verify system functionality
- [ ] Check data integrity
- [ ] Review logs for issues
- [ ] Update monitoring alerts
- [ ] Document incident details
- [ ] Schedule post-mortem review

### **Monthly Review Checklist**
- [ ] Validate backup success rate
- [ ] Review disk space usage
- [ ] Test recovery procedures
- [ ] Update documentation
- [ ] Review escalation procedures
- [ ] Check monitoring alerts

---

## 📁 FILE LOCATIONS

### **Configuration Files**
- **App Config**: `/app/appsettings.json`
- **Production Config**: `/app/appsettings.Production.json`
- **Environment**: `/.env`

### **Script Locations**
- **Bash Scripts**: `/app/scripts/backup-database.sh`, `/app/scripts/restore-database.sh`
- **PowerShell Scripts**: `/app/scripts/backup-database.ps1`, `/app/scripts/Restore-Database.ps1`

### **Data Locations**
- **Database**: `/app/data/digitalme.db`
- **Backups**: `/app/backups/digitalme_*.db`
- **Logs**: `/app/logs/backup.log`

### **Documentation**
- **Full Guide**: `/docs/operations/BACKUP_RECOVERY_GUIDE.md`
- **This Runbook**: `/docs/operations/BACKUP_RUNBOOK.md`

---

## 🔑 AUTHENTICATION

### **API Authentication**
```bash
# Get JWT token (replace with your auth endpoint)
JWT=$(curl -X POST http://localhost:5000/auth/login \
      -H "Content-Type: application/json" \
      -d '{"username":"admin","password":"your_password"}' | \
      jq -r '.token')

# Use in backup API calls
curl -H "Authorization: Bearer $JWT" http://localhost:5000/api/backup
```

### **Script Authentication**
- Scripts run with application service account
- Container scripts have required permissions
- Production scripts require sudo/elevated permissions

---

**⚠️ REMEMBER**: Always test recovery procedures before executing them in production!

**Last Updated**: 2025-09-07 | **Next Review**: 2025-10-07