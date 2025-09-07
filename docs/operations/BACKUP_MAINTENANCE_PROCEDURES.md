# DigitalMe Backup System Maintenance Procedures

> **MAINTENANCE GUIDE** | Scheduled Maintenance & Testing Procedures  
> **Last Updated**: 2025-09-07 | **Version**: 1.0

---

## 📅 MAINTENANCE SCHEDULE

### **Daily Automated Tasks**
- **02:00 AM**: Automated backup creation
- **02:15 AM**: Backup integrity validation  
- **02:30 AM**: Old backup cleanup (if enabled)
- **02:45 AM**: Health status reporting

### **Weekly Manual Tasks** (Sundays, 10:00 AM)
- Backup system health verification
- Recovery procedure testing
- Log review and analysis
- Disk space monitoring

### **Monthly Manual Tasks** (First Saturday, 2:00 PM)
- Complete backup restoration test
- Backup retention policy review
- Performance metrics analysis
- Documentation updates

### **Quarterly Tasks** (End of quarter)
- Full disaster recovery simulation
- Backup strategy review
- Security audit of backup files
- Staff training updates

---

## 🔧 DAILY MAINTENANCE

### **Morning Health Check (10 minutes)**

**1. Verify Automated Backup Success:**
```bash
# Check if last night's backup was successful
curl -s -H "Authorization: Bearer $JWT" \
     http://localhost:5000/api/backup/health | \
     jq '{lastBackupTime, isHealthy, issues}'

# Expected output:
# {
#   "lastBackupTime": "2025-09-07T02:00:22Z",
#   "isHealthy": true,
#   "issues": []
# }
```

**2. Review Backup Logs:**
```bash
# Check for errors in last 24 hours
tail -n 100 logs/backup.log | grep -i error

# Check backup completion status
tail -n 50 logs/backup.log | grep -E "Backup Process (Completed|Failed)"
```

**3. Verify Disk Space:**
```bash
# Backup directory usage
df -h /app/backups/

# Alert if usage > 80%
USAGE=$(df /app/backups/ | awk 'NR==2 {print $5}' | sed 's/%//')
if [ $USAGE -gt 80 ]; then
    echo "WARNING: Backup disk usage at ${USAGE}%"
    # Trigger cleanup or alert
fi
```

**4. Quick Service Status Check:**
```bash
# Application health (includes backup service)
curl -s http://localhost:5000/health | jq '.status'

# Database connectivity
sqlite3 /app/data/digitalme.db "SELECT 'OK' as status;"
```

### **Daily Monitoring Dashboard**
```bash
#!/bin/bash
# daily-backup-status.sh

echo "=== DigitalMe Backup Status Dashboard ==="
echo "Date: $(date '+%Y-%m-%d %H:%M:%S')"
echo ""

echo "🔍 SYSTEM HEALTH:"
curl -s -H "Authorization: Bearer $JWT" http://localhost:5000/api/backup/health | \
jq -r '"Health: " + (.isHealthy | tostring) + " | Backups: " + (.totalBackups | tostring) + " | Size: " + .formattedTotalSize + " | Last: " + .lastBackupTime'

echo ""
echo "💾 RECENT BACKUPS:"
curl -s -H "Authorization: Bearer $JWT" http://localhost:5000/api/backup | \
jq -r '.[:5][] | .createdAt + " | " + .fileName + " | " + .formattedSize'

echo ""
echo "📊 DISK USAGE:"
df -h /app/backups/ | awk 'NR==2 {print "Used: " $3 "/" $2 " (" $5 ")"}'

echo ""
echo "⚠️  LOG ERRORS (Last 24h):"
tail -n 1000 logs/backup.log | grep -i error | tail -5 || echo "No errors found"

echo ""
echo "✅ STATUS: $(curl -s http://localhost:5000/health | jq -r '.status')"
```

---

## 📊 WEEKLY MAINTENANCE

### **Sunday Health Verification (30 minutes)**

**1. Complete Backup System Test:**
```bash
#!/bin/bash
# weekly-backup-test.sh

echo "=== Weekly Backup System Test ==="
echo "Date: $(date)"

# Get latest backup for testing
LATEST_BACKUP=$(curl -s -H "Authorization: Bearer $JWT" \
                http://localhost:5000/api/backup | \
                jq -r '.[0].fileName')

echo "Testing backup: $LATEST_BACKUP"

# Test recovery capability
RECOVERY_TEST=$(curl -s -X POST -H "Authorization: Bearer $JWT" \
                "http://localhost:5000/api/backup/test-recovery/$LATEST_BACKUP")

CAN_RECOVER=$(echo $RECOVERY_TEST | jq -r '.canRecover')
BACKUP_VALID=$(echo $RECOVERY_TEST | jq -r '.backupValid')
SUFFICIENT_SPACE=$(echo $RECOVERY_TEST | jq -r '.sufficientSpace')

echo "Recovery Test Results:"
echo "- Can Recover: $CAN_RECOVER"
echo "- Backup Valid: $BACKUP_VALID"  
echo "- Sufficient Space: $SUFFICIENT_SPACE"

if [ "$CAN_RECOVER" = "true" ]; then
    echo "✅ Weekly test PASSED"
else
    echo "❌ Weekly test FAILED - Investigation required"
    echo "Error: $(echo $RECOVERY_TEST | jq -r '.errorMessage')"
fi
```

**2. Backup Statistics Review:**
```bash
# Weekly backup statistics
curl -s -H "Authorization: Bearer $JWT" \
     http://localhost:5000/api/backup/statistics | \
     jq '{
       totalBackups,
       validBackups, 
       invalidBackups,
       formattedTotalSize,
       isHealthy,
       backupsByDate: .backupsByDate[:7]
     }'
```

**3. Log Analysis:**
```bash
# Weekly log summary
echo "=== Weekly Log Analysis ==="

# Backup success rate
TOTAL_ATTEMPTS=$(grep -c "Backup Process Started" logs/backup.log)
SUCCESS_COUNT=$(grep -c "Backup Process Completed Successfully" logs/backup.log)

if [ $TOTAL_ATTEMPTS -gt 0 ]; then
    SUCCESS_RATE=$((SUCCESS_COUNT * 100 / TOTAL_ATTEMPTS))
    echo "Backup Success Rate: $SUCCESS_RATE% ($SUCCESS_COUNT/$TOTAL_ATTEMPTS)"
else
    echo "No backup attempts found in logs"
fi

# Recent errors
echo ""
echo "Recent Errors (Last 7 days):"
grep -i error logs/backup.log | tail -10 || echo "No errors found"
```

**4. Performance Metrics:**
```bash
# Backup performance metrics
echo "=== Backup Performance Metrics ==="

# Average backup duration (from logs)
grep "Backup Process Completed Successfully" logs/backup.log | \
tail -10 | grep -o 'Duration: [0-9]*ms' | \
awk -F': ' '{sum+=$2; count++} END {if(count>0) print "Average Duration: " sum/count "ms"}'

# Average backup size
curl -s -H "Authorization: Bearer $JWT" \
     http://localhost:5000/api/backup | \
     jq '[.[:10][].sizeBytes] | add / length | . / 1024 / 1024 | "Average Size: " + (. | tostring) + " MB"'
```

---

## 🧪 MONTHLY MAINTENANCE

### **First Saturday Complete Testing (2 hours)**

**1. Full Recovery Simulation:**
```bash
#!/bin/bash
# monthly-recovery-simulation.sh

echo "=== Monthly Recovery Simulation ==="
echo "Date: $(date)"

# Create temporary database copy for testing
cp /app/data/digitalme.db /app/data/digitalme_temp_backup.db

echo "✅ Created temporary database backup"

# Select backup from 1 week ago for testing
WEEK_OLD_BACKUP=$(curl -s -H "Authorization: Bearer $JWT" \
                  http://localhost:5000/api/backup | \
                  jq -r '[.[] | select(.createdAt < (now - 604800 | strftime("%Y-%m-%dT%H:%M:%SZ")))][0].fileName')

if [ "$WEEK_OLD_BACKUP" != "null" ] && [ -n "$WEEK_OLD_BACKUP" ]; then
    echo "Testing recovery with: $WEEK_OLD_BACKUP"
    
    # Test recovery process
    ./scripts/restore-database.sh --test $WEEK_OLD_BACKUP
    
    if [ $? -eq 0 ]; then
        echo "✅ Recovery simulation test PASSED"
        
        # Optional: Actually perform recovery in test environment
        # ./scripts/restore-database.sh --force --verify $WEEK_OLD_BACKUP
        
    else
        echo "❌ Recovery simulation test FAILED"
    fi
else
    echo "⚠️  No week-old backup found for testing"
fi

# Restore original database
mv /app/data/digitalme_temp_backup.db /app/data/digitalme.db
echo "✅ Restored original database"
```

**2. Backup Retention Policy Review:**
```bash
#!/bin/bash
# monthly-retention-review.sh

echo "=== Monthly Backup Retention Review ==="

# Current backup distribution by age
curl -s -H "Authorization: Bearer $JWT" http://localhost:5000/api/backup | \
jq -r '
  group_by(.createdAt[:10]) | 
  map({date: .[0].createdAt[:10], count: length, totalSize: (map(.sizeBytes) | add)}) |
  sort_by(.date) |
  reverse |
  .[:30] |
  map(.date + " | Count: " + (.count | tostring) + " | Size: " + ((.totalSize / 1024 / 1024 | floor | tostring) + " MB"))[]
'

echo ""
echo "Retention Policy Analysis:"

CURRENT_COUNT=$(curl -s -H "Authorization: Bearer $JWT" \
                http://localhost:5000/api/backup | jq '. | length')

CURRENT_SIZE=$(curl -s -H "Authorization: Bearer $JWT" \
               http://localhost:5000/api/backup/health | \
               jq -r '.formattedTotalSize')

echo "Current backups: $CURRENT_COUNT files, $CURRENT_SIZE total"
echo "Configured limits: 30 files, 7 days retention"

# Check if cleanup is needed
if [ $CURRENT_COUNT -gt 35 ]; then
    echo "⚠️  Consider running manual cleanup"
fi
```

**3. Performance Analysis:**
```bash
#!/bin/bash
# monthly-performance-analysis.sh

echo "=== Monthly Performance Analysis ==="

# Backup size trends (last 30 backups)
echo "Backup Size Trend:"
curl -s -H "Authorization: Bearer $JWT" http://localhost:5000/api/backup | \
jq -r '.[:30][] | .createdAt + " | " + (.sizeBytes / 1024 / 1024 | floor | tostring) + " MB"' | \
sort

echo ""
echo "System Resource Usage:"

# Database size growth
DB_SIZE=$(du -sh /app/data/digitalme.db | cut -f1)
echo "Current database size: $DB_SIZE"

# Backup directory usage
BACKUP_USAGE=$(du -sh /app/backups/ | cut -f1)
echo "Backup directory size: $BACKUP_USAGE"

# Available disk space
AVAILABLE_SPACE=$(df -h /app/backups/ | awk 'NR==2 {print $4}')
echo "Available space: $AVAILABLE_SPACE"
```

**4. Security and Compliance Check:**
```bash
#!/bin/bash
# monthly-security-check.sh

echo "=== Monthly Security & Compliance Check ==="

echo "File Permissions:"
ls -la /app/backups/*.db | head -5

echo ""
echo "Backup File Integrity (Random Sample):"
RANDOM_BACKUP=$(ls /app/backups/*.db | sort -R | head -1)
if [ -n "$RANDOM_BACKUP" ]; then
    echo "Testing: $(basename $RANDOM_BACKUP)"
    sqlite3 "$RANDOM_BACKUP" "PRAGMA integrity_check;" | head -1
else
    echo "No backup files found"
fi

echo ""
echo "Log File Security:"
ls -la logs/backup.log

echo ""
echo "Configuration Security:"
# Check for sensitive data in config (without exposing it)
grep -q "password\|secret\|key" /app/appsettings.json && \
    echo "⚠️  Sensitive data found in config - verify security" || \
    echo "✅ No obvious sensitive data in config"
```

---

## 🚨 QUARTERLY MAINTENANCE

### **End of Quarter Complete Review (4 hours)**

**1. Disaster Recovery Drill:**
```bash
#!/bin/bash
# quarterly-dr-drill.sh

echo "=== Quarterly Disaster Recovery Drill ==="
echo "Date: $(date)"
echo "This drill simulates complete system failure and recovery"

# Phase 1: Backup system verification
echo ""
echo "Phase 1: Backup System Verification"
./scripts/backup-database.sh backup
echo "✅ Fresh backup created"

# Phase 2: Recovery capability test
echo ""
echo "Phase 2: Recovery Capability Test"
LATEST=$(curl -s -H "Authorization: Bearer $JWT" \
         http://localhost:5000/api/backup | jq -r '.[0].fileName')
./scripts/restore-database.sh --test $LATEST
echo "✅ Recovery test completed"

# Phase 3: Documentation verification
echo ""
echo "Phase 3: Documentation Verification"
echo "- Backup procedures documented: ✅"
echo "- Recovery procedures documented: ✅"
echo "- Contact information current: [MANUAL VERIFICATION REQUIRED]"
echo "- Escalation procedures clear: [MANUAL VERIFICATION REQUIRED]"

# Phase 4: Performance benchmarks
echo ""
echo "Phase 4: Performance Benchmarks"

# Backup speed test
echo "Testing backup performance..."
START_TIME=$(date +%s)
./scripts/backup-database.sh backup
END_TIME=$(date +%s)
BACKUP_DURATION=$((END_TIME - START_TIME))
echo "Backup duration: ${BACKUP_DURATION} seconds"

# Recovery speed test
echo "Testing recovery performance..."
START_TIME=$(date +%s)
./scripts/restore-database.sh --test $LATEST > /dev/null 2>&1
END_TIME=$(date +%s)
RECOVERY_DURATION=$((END_TIME - START_TIME))
echo "Recovery test duration: ${RECOVERY_DURATION} seconds"

echo ""
echo "=== Quarterly DR Drill Complete ==="
```

**2. Backup Strategy Review:**
```bash
#!/bin/bash
# quarterly-strategy-review.sh

echo "=== Quarterly Backup Strategy Review ==="

echo "Current Configuration:"
grep -A 10 '"Backup":' /app/appsettings.json

echo ""
echo "Historical Performance (Last 90 Days):"
# Success rate
TOTAL_90=$(grep -c "Backup Process Started" logs/backup.log | tail -90)
SUCCESS_90=$(grep -c "Backup Process Completed Successfully" logs/backup.log | tail -90)
RATE_90=$((SUCCESS_90 * 100 / TOTAL_90))
echo "Success Rate (90 days): ${RATE_90}%"

# Storage usage trends
echo ""
echo "Storage Usage Trend:"
curl -s -H "Authorization: Bearer $JWT" \
     http://localhost:5000/api/backup/statistics | \
     jq '.backupsByDate | map("Date: " + .date + " | Count: " + (.count | tostring) + " | Size: " + .formattedTotalSize)'

# Recommendations
echo ""
echo "Recommendations:"
if [ $RATE_90 -lt 95 ]; then
    echo "- ⚠️  Backup success rate below 95% - investigate failures"
fi

BACKUP_COUNT=$(curl -s -H "Authorization: Bearer $JWT" \
               http://localhost:5000/api/backup | jq '. | length')
if [ $BACKUP_COUNT -gt 50 ]; then
    echo "- 💾 Consider increasing retention period or backup frequency"
fi

DB_SIZE_MB=$(du -m /app/data/digitalme.db | cut -f1)
if [ $DB_SIZE_MB -gt 1000 ]; then
    echo "- 📈 Database size over 1GB - consider archive strategies"
fi
```

**3. Staff Training and Documentation Update:**
```markdown
## Quarterly Training Checklist

### Staff Training Items:
- [ ] Review emergency procedures with operations team
- [ ] Verify all staff know escalation contacts  
- [ ] Practice backup/recovery procedures hands-on
- [ ] Update any changed procedures or commands
- [ ] Test emergency communication channels

### Documentation Updates:
- [ ] Review all backup documentation for accuracy
- [ ] Update contact information and escalation procedures
- [ ] Add any new procedures discovered during quarter
- [ ] Update screenshots and examples if UI changed
- [ ] Verify all links and references are current

### Compliance Verification:
- [ ] Ensure retention policies meet regulatory requirements
- [ ] Verify backup security meets company standards
- [ ] Document any compliance gaps and remediation plans
- [ ] Review audit trail and logging completeness
```

---

## 📈 PERFORMANCE BASELINES

### **Expected Performance Metrics**
```yaml
Backup Operations:
  - Creation Time: < 30 seconds (typical DB size)
  - Success Rate: > 99%
  - File Size: ~2-10MB (typical)
  - Integrity Check: < 5 seconds

Recovery Operations:
  - Test Recovery: < 15 seconds
  - Full Recovery: < 60 seconds (typical)
  - Validation: < 10 seconds

System Resources:
  - Disk Usage: < 80% of backup volume
  - Backup Directory: < 2GB total (30 files)
  - Memory Usage: < 100MB during operations
```

### **Performance Monitoring Queries**
```bash
# Database size growth rate (monthly)
du -m /app/data/digitalme.db

# Average backup size
curl -s -H "Authorization: Bearer $JWT" \
     http://localhost:5000/api/backup | \
     jq '[.[].sizeBytes] | add / length'

# Backup frequency analysis  
curl -s -H "Authorization: Bearer $JWT" \
     http://localhost:5000/api/backup | \
     jq 'group_by(.createdAt[:10]) | map({date: .[0].createdAt[:10], count: length})'
```

---

## 🔄 MAINTENANCE LOG TEMPLATE

```markdown
# Maintenance Log Entry

**Date**: YYYY-MM-DD  
**Technician**: [Name]  
**Type**: [Daily/Weekly/Monthly/Quarterly]  
**Duration**: [Start Time - End Time]

## Tasks Completed:
- [ ] Task 1 description
- [ ] Task 2 description  
- [ ] Task 3 description

## Issues Found:
- Issue 1: Description and resolution
- Issue 2: Description and resolution

## Performance Metrics:
- Backup Success Rate: XX%
- Average Backup Size: XX MB
- Average Backup Duration: XX seconds
- Disk Usage: XX%

## Recommendations:
- Recommendation 1
- Recommendation 2

## Next Actions:
- Action 1 (Due: Date)
- Action 2 (Due: Date)

**Status**: [Complete/Issues Found/Follow-up Required]
**Next Scheduled Maintenance**: [Date]
```

---

**Last Updated**: 2025-09-07 | **Version**: 1.0 | **Next Review**: 2025-12-07