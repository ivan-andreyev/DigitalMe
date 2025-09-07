# 📊 DigitalMe Operations Manual

**Version**: 1.0  
**Last Updated**: 2025-09-07  
**Purpose**: Day-to-day operational procedures for production DigitalMe deployment  
**Environment**: Production  

---

## 📋 Daily Operations Checklist

### Morning Health Check (Daily - 09:00)

```bash
# 1. Check container status
docker-compose -f docker-compose.production.yml ps

# 2. Verify application health
curl -f http://localhost/health
curl -f http://localhost/info

# 3. Check SSL status (if applicable)
curl -k https://localhost/health

# 4. Verify database accessibility
docker exec digitalme-digitalme-1 ls -la /app/data/

# 5. Check backup status
docker exec digitalme-digitalme-1 ls -la /app/backups/ | tail -5
```

**Expected Results:**
- All containers: `Up` status
- Health endpoints: Return 200 OK with "Healthy" status
- Database file: Present with recent timestamp
- Backups: At least 3 recent backups visible

### Resource Monitoring (Every 4 hours)

```bash
# System resource monitoring
free -h                # Memory usage
df -h                  # Disk usage  
top -bn1 | grep "Cpu"  # CPU usage

# Container resource monitoring
docker stats --no-stream digitalme-digitalme-1

# Network connectivity
netstat -tlnp | grep :80
netstat -tlnp | grep :443
```

**Alert Thresholds:**
- **Memory Usage**: >85% total system memory
- **Disk Usage**: >80% on any partition
- **CPU Usage**: >90% for more than 5 minutes
- **Container Memory**: >2GB for application container

---

## 🔍 Monitoring Dashboard Setup

### Health Check Endpoints Reference

| Endpoint | Purpose | Expected Response Time | Check Frequency |
|----------|---------|----------------------|-----------------|
| `/health` | Application health | <1s | Every 30s |
| `/health/ready` | Readiness probe | <1s | Every 10s |
| `/info` | System information | <1s | On-demand |

### Health Check Implementation

**Automated Health Monitoring Script** (`/opt/digitalme/monitor.sh`):

```bash
#!/bin/bash
# DigitalMe Health Monitor
# Run every 5 minutes via cron

LOG_FILE="/var/log/digitalme-monitor.log"
TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S')

# Function to log with timestamp
log() {
    echo "[$TIMESTAMP] $1" >> $LOG_FILE
}

# Check container status
if ! docker-compose -f /opt/digitalme/docker-compose.production.yml ps | grep -q "Up"; then
    log "ERROR: Container not running"
    # Send alert here
    exit 1
fi

# Check health endpoint
if ! curl -f -s http://localhost/health > /dev/null; then
    log "ERROR: Health check failed"
    # Send alert here
    exit 1
fi

# Check database
if ! docker exec digitalme-digitalme-1 test -f /app/data/digitalme.db; then
    log "ERROR: Database file missing"
    # Send alert here
    exit 1
fi

log "INFO: All health checks passed"
```

**Setup Cron Job:**
```bash
# Install monitoring script
sudo cp monitor.sh /opt/digitalme/
sudo chmod +x /opt/digitalme/monitor.sh

# Add to crontab
(crontab -l 2>/dev/null; echo "*/5 * * * * /opt/digitalme/monitor.sh") | crontab -
```

---

## 🔄 Routine Maintenance Procedures

### Weekly Maintenance (Every Sunday - 02:00)

**1. Log Rotation and Cleanup:**
```bash
# Rotate application logs
docker-compose -f docker-compose.production.yml exec digitalme logrotate /etc/logrotate.conf

# Clean old Docker images
docker image prune -f

# Clean old containers
docker container prune -f
```

**2. Database Maintenance:**
```bash
# Optimize database
docker exec digitalme-digitalme-1 sqlite3 /app/data/digitalme.db "VACUUM;"
docker exec digitalme-digitalme-1 sqlite3 /app/data/digitalme.db "REINDEX;"

# Check database integrity
docker exec digitalme-digitalme-1 sqlite3 /app/data/digitalme.db "PRAGMA integrity_check;"
```

**3. Security Updates:**
```bash
# Update base system
sudo apt update && sudo apt upgrade -y

# Update Docker images
docker-compose -f docker-compose.production.yml pull

# Restart services with updates
docker-compose -f docker-compose.production.yml up -d
```

### Monthly Maintenance (First Sunday - 01:00)

**1. Certificate Renewal:**
```bash
# Check certificate expiration
sudo certbot certificates

# Renew certificates if needed
sudo certbot renew --quiet

# Restart services to pick up new certificates
docker-compose -f docker-compose.production.yml restart
```

**2. Backup Verification:**
```bash
# Test backup restoration process
docker exec digitalme-digitalme-1 cp /app/backups/latest-backup.db /tmp/test-restore.db
docker exec digitalme-digitalme-1 sqlite3 /tmp/test-restore.db "PRAGMA integrity_check;"
```

**3. Performance Review:**
```bash
# Analyze logs for performance patterns
docker-compose logs --since 720h | grep -i "slow\|timeout\|performance"

# Review resource usage trends
docker stats --no-stream digitalme-digitalme-1
```

---

## 📊 Performance Monitoring Setup

### Key Performance Metrics

**Application Performance:**
```bash
# Response time monitoring
curl -w "@curl-format.txt" -o /dev/null -s http://localhost/info

# Where curl-format.txt contains:
#     time_namelookup:  %{time_namelookup}\n
#     time_connect:     %{time_connect}\n
#     time_appconnect:  %{time_appconnect}\n
#     time_pretransfer: %{time_pretransfer}\n
#     time_redirect:    %{time_redirect}\n
#     time_starttransfer: %{time_starttransfer}\n
#     time_total:       %{time_total}\n
```

**Resource Utilization:**
```bash
# Container resource monitoring
docker exec digitalme-digitalme-1 cat /proc/meminfo | grep MemAvailable
docker exec digitalme-digitalme-1 cat /proc/loadavg
```

**Database Performance:**
```bash
# Database size monitoring
docker exec digitalme-digitalme-1 du -h /app/data/digitalme.db

# Query performance analysis (if needed)
docker exec digitalme-digitalme-1 sqlite3 /app/data/digitalme.db "EXPLAIN QUERY PLAN SELECT * FROM main_table LIMIT 1;"
```

---

## 🚨 Alert Configuration

### Critical Alerts (Immediate Response)

**Container Down:**
```bash
# Check command that should trigger immediate alert
! docker-compose -f docker-compose.production.yml ps | grep -q "Up"
```

**Health Check Failure:**
```bash
# Health endpoint returning non-200 status
! curl -f -s http://localhost/health > /dev/null
```

**Database Unavailable:**
```bash
# Database file missing or corrupted
! docker exec digitalme-digitalme-1 test -f /app/data/digitalme.db
```

### Warning Alerts (Response within 1 hour)

**High Resource Usage:**
```bash
# Memory usage >85%
free | awk 'NR==2{printf "%.2f%%\n", $3*100/$2}' | cut -d'%' -f1 | awk '{if($1>85) print "HIGH_MEMORY"}'

# Disk usage >80%
df -h | awk 'NR>1 {gsub(/%/,"",$5); if($5>80) print $0}'

# CPU usage >90% (sustained)
top -bn1 | grep "Cpu(s)" | awk '{print $2}' | cut -d'%' -f1 | awk '{if($1>90) print "HIGH_CPU"}'
```

### Integration with External Monitoring

**Webhook Integration Example:**
```bash
#!/bin/bash
# Send alert to monitoring system
send_alert() {
    local severity=$1
    local message=$2
    
    curl -X POST https://your-monitoring-system.com/webhook \
        -H "Content-Type: application/json" \
        -d "{
            \"severity\": \"$severity\",
            \"service\": \"DigitalMe\",
            \"message\": \"$message\",
            \"timestamp\": \"$(date -Iseconds)\"
        }"
}

# Usage examples:
# send_alert "critical" "Container down"
# send_alert "warning" "High memory usage: 87%"
```

---

## 📝 Standard Operating Procedures

### Application Restart Procedure

**Standard Restart (Planned Maintenance):**
```bash
# 1. Notify users (if applicable)
echo "Maintenance window started" | wall

# 2. Create backup before restart
docker exec digitalme-digitalme-1 cp /app/data/digitalme.db /app/backups/pre-restart-$(date +%Y%m%d-%H%M%S).db

# 3. Graceful restart
docker-compose -f docker-compose.production.yml restart digitalme

# 4. Verify restart
sleep 10
curl -f http://localhost/health

# 5. Document restart
echo "$(date): Planned restart completed successfully" >> /var/log/digitalme-operations.log
```

**Emergency Restart:**
```bash
# 1. Immediate restart
docker-compose -f docker-compose.production.yml stop digitalme
docker-compose -f docker-compose.production.yml start digitalme

# 2. Verify functionality
curl -f http://localhost/health
curl -f http://localhost/info

# 3. Check logs for errors
docker-compose -f docker-compose.production.yml logs --tail=50 digitalme

# 4. Document emergency action
echo "$(date): Emergency restart - investigating root cause" >> /var/log/digitalme-operations.log
```

### Configuration Change Procedure

**Environment Variable Updates:**
```bash
# 1. Backup current configuration
cp .env .env.backup.$(date +%Y%m%d)

# 2. Update configuration
nano .env

# 3. Validate configuration
docker-compose -f docker-compose.production.yml config

# 4. Apply changes
docker-compose -f docker-compose.production.yml up -d

# 5. Verify changes
curl -f http://localhost/health
docker-compose -f docker-compose.production.yml logs --tail=20
```

---

## 📞 Operational Contacts

### Escalation Matrix

**Level 1 - System Administrator:**
- **Response Time**: 15 minutes
- **Scope**: Routine issues, planned maintenance
- **Contact**: `sysadmin@your-domain.com`

**Level 2 - Engineering Team:**
- **Response Time**: 1 hour  
- **Scope**: Application issues, performance problems
- **Contact**: `engineering@your-domain.com`

**Level 3 - Emergency Response:**
- **Response Time**: Immediate
- **Scope**: Critical system failures, security incidents
- **Contact**: `emergency@your-domain.com`

### Maintenance Windows

**Standard Maintenance:**
- **Time**: Sunday 02:00 - 04:00 UTC
- **Frequency**: Weekly
- **Scope**: Routine updates, optimization

**Emergency Maintenance:**
- **Time**: As needed
- **Duration**: Minimize downtime <30 minutes
- **Approval**: Engineering Manager

---

**End of Operations Manual**

*This manual covers day-to-day operations. For deployment procedures, see PRODUCTION_DEPLOYMENT_GUIDE.md. For troubleshooting, see TROUBLESHOOTING_GUIDE.md.*