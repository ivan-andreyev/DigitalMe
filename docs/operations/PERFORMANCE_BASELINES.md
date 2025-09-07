# 📊 DigitalMe Performance Baselines

**Version**: 1.0  
**Last Updated**: 2025-09-07  
**Purpose**: Performance metrics, baselines, and monitoring targets for production DigitalMe deployment  
**Environment**: Production  

---

## 🎯 Performance Targets Overview

| **Metric Category** | **Target** | **Warning Threshold** | **Critical Threshold** |
|-------------------|------------|---------------------|----------------------|
| **Response Time** | <2s (95th percentile) | >3s | >5s |
| **Availability** | >99.5% uptime | <99% | <95% |
| **Memory Usage** | <2GB steady state | >2.5GB | >3GB |
| **CPU Usage** | <70% average | >85% | >95% |
| **Disk I/O** | <100MB/s | >500MB/s | >1GB/s |

---

## 🚀 Application Performance Baselines

### Response Time Baselines

**API Endpoint Performance Targets:**

| **Endpoint** | **Target (95th percentile)** | **Warning** | **Critical** | **Baseline Method** |
|-------------|------------------------------|-------------|--------------|-------------------|
| `/health` | <500ms | >1s | >2s | Simple DB query |
| `/info` | <1s | >2s | >3s | System info aggregation |
| `/api/chat` | <3s | >5s | >10s | Claude API + processing |
| `/api/auth/login` | <1s | >2s | >3s | JWT generation |
| Static files | <200ms | >500ms | >1s | File serving |

**Performance Measurement Commands:**

```bash
# Basic response time measurement
curl -w "@curl-format.txt" -o /dev/null -s http://localhost/health

# Where curl-format.txt contains:
#     time_namelookup:  %{time_namelookup}\n
#     time_connect:     %{time_connect}\n  
#     time_starttransfer: %{time_starttransfer}\n
#     time_total:       %{time_total}\n

# Automated performance testing script
#!/bin/bash
for endpoint in health info api/chat; do
    echo "Testing /$endpoint:"
    for i in {1..10}; do
        curl -w "%{time_total}\n" -o /dev/null -s http://localhost/$endpoint
    done | awk '{sum+=$1} END {print "Average: " sum/NR "s"}'
done
```

### Throughput Baselines

**Concurrent User Capacity:**

| **Deployment Type** | **Concurrent Users** | **Requests/Second** | **95th Percentile Response** |
|-------------------|-------------------|------------------|----------------------------|
| **Single Container (2GB RAM)** | 100-200 | 50-100 | <2s |
| **Single Container (4GB RAM)** | 200-500 | 100-250 | <1.5s |
| **Load Balanced (2x containers)** | 500-1000 | 250-500 | <1s |
| **Kubernetes (3x replicas)** | 1000+ | 500+ | <1s |

**Load Testing Commands:**

```bash
# Simple load test with Apache Bench
ab -n 1000 -c 10 http://localhost/health

# More comprehensive load test with wrk
wrk -t4 -c40 -d30s --timeout 10s http://localhost/health

# Custom load test script
#!/bin/bash
echo "Starting load test..."
for i in {1..100}; do
    curl -s http://localhost/health > /dev/null &
    if (( i % 10 == 0 )); then
        wait
        echo "Completed $i requests"
    fi
done
wait
echo "Load test complete"
```

---

## 💾 System Resource Baselines

### Memory Usage Baselines

**Memory Allocation Targets:**

| **Component** | **Idle State** | **Normal Load** | **High Load** | **Critical Limit** |
|---------------|----------------|-----------------|---------------|-------------------|
| **Application Process** | 200-400MB | 600-1200MB | 1200-2000MB | 2500MB |
| **Database Buffer** | 50-100MB | 100-300MB | 300-500MB | 800MB |
| **System Reserve** | 500MB | 500MB | 500MB | 500MB |
| **Total Container** | 800MB-1GB | 1.2GB-2GB | 2GB-3GB | 4GB |

**Memory Monitoring Commands:**

```bash
# Container memory usage
docker stats --no-stream digitalme-digitalme-1

# Detailed memory breakdown
docker exec digitalme-digitalme-1 cat /proc/meminfo | head -20

# Application memory usage
docker exec digitalme-digitalme-1 ps aux | grep dotnet

# Memory usage over time (monitoring script)
#!/bin/bash
echo "Timestamp,Container_Memory_MB,App_Memory_MB" > memory_usage.csv
for i in {1..144}; do  # 24 hours, every 10 minutes
    timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    container_mem=$(docker stats --no-stream --format "{{.MemUsage}}" digitalme-digitalme-1 | cut -d'/' -f1 | sed 's/MiB//')
    app_mem=$(docker exec digitalme-digitalme-1 ps -o pid,rss -C dotnet | awk 'NR>1 {sum+=$2} END {print sum/1024}')
    echo "$timestamp,$container_mem,$app_mem" >> memory_usage.csv
    sleep 600  # 10 minutes
done
```

### CPU Usage Baselines

**CPU Utilization Targets:**

| **Load Condition** | **CPU Usage** | **Warning** | **Critical** | **Duration** |
|-------------------|---------------|-------------|--------------|--------------|
| **Idle** | 5-15% | >30% | >50% | Sustained >5min |
| **Normal Operations** | 20-40% | >60% | >80% | Sustained >10min |
| **High Load** | 40-70% | >85% | >95% | Sustained >2min |
| **Startup/Initialization** | 60-90% | N/A | N/A | <2min acceptable |

**CPU Monitoring Commands:**

```bash
# Real-time CPU usage
top -bn1 | grep "Cpu(s)"

# Container CPU usage
docker stats --no-stream digitalme-digitalme-1 | awk '{print $3}'

# CPU usage history
#!/bin/bash
echo "Timestamp,System_CPU,Container_CPU" > cpu_usage.csv
for i in {1..144}; do  # 24 hours
    timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    system_cpu=$(top -bn1 | grep "Cpu(s)" | awk '{print $2}' | cut -d'%' -f1)
    container_cpu=$(docker stats --no-stream --format "{{.CPUPerc}}" digitalme-digitalme-1 | cut -d'%' -f1)
    echo "$timestamp,$system_cpu,$container_cpu" >> cpu_usage.csv
    sleep 600
done
```

---

## 🗄️ Database Performance Baselines

### Database Size and Growth Baselines

**Database Size Targets:**

| **Timeline** | **Expected Size** | **Warning Size** | **Critical Size** | **Action Required** |
|-------------|------------------|------------------|-------------------|-------------------|
| **Initial** | 10-50MB | >100MB | >500MB | Investigate data model |
| **1 Month** | 100-300MB | >500MB | >1GB | Plan archiving strategy |
| **6 Months** | 300MB-1GB | >2GB | >5GB | Implement archiving |
| **1 Year** | 1-3GB | >5GB | >10GB | Consider migration to PostgreSQL |

**Database Performance Monitoring:**

```bash
# Database file size
docker exec digitalme-digitalme-1 du -h /app/data/digitalme.db

# Database operations performance
docker exec digitalme-digitalme-1 sqlite3 /app/data/digitalme.db "EXPLAIN QUERY PLAN SELECT COUNT(*) FROM conversation_history;"

# Database optimization commands
docker exec digitalme-digitalme-1 sqlite3 /app/data/digitalme.db "VACUUM;"
docker exec digitalme-digitalme-1 sqlite3 /app/data/digitalme.db "REINDEX;"
docker exec digitalme-digitalme-1 sqlite3 /app/data/digitalme.db "PRAGMA optimize;"

# Database integrity check
docker exec digitalme-digitalme-1 sqlite3 /app/data/digitalme.db "PRAGMA integrity_check;"
```

### Query Performance Baselines

**Query Performance Targets:**

| **Query Type** | **Target Time** | **Warning** | **Critical** | **Example** |
|---------------|----------------|-------------|--------------|-------------|
| **Simple SELECT** | <10ms | >50ms | >100ms | User lookup |
| **JOIN Queries** | <50ms | >200ms | >500ms | Conversation with user |
| **COUNT Operations** | <100ms | >500ms | >1s | Statistics |
| **INSERT/UPDATE** | <50ms | >200ms | >500ms | Save conversation |

---

## 🌐 Network Performance Baselines

### External API Performance

**Claude API Performance Baselines:**

| **Operation** | **Expected Latency** | **Warning** | **Critical** | **Timeout Setting** |
|--------------|-------------------|-------------|--------------|-------------------|
| **Simple Query** | 1-3s | >5s | >10s | 15s |
| **Complex Analysis** | 3-8s | >10s | >15s | 30s |
| **Code Generation** | 2-5s | >8s | >12s | 20s |
| **Long Conversation** | 5-15s | >20s | >30s | 45s |

**Network Monitoring Commands:**

```bash
# Test external API latency
#!/bin/bash
test_claude_api() {
    start_time=$(date +%s.%3N)
    response=$(curl -s -H "Content-Type: application/json" \
        -H "x-api-key: $ANTHROPIC_API_KEY" \
        -d '{"model":"claude-3-5-sonnet-20241022","max_tokens":10,"messages":[{"role":"user","content":"test"}]}' \
        https://api.anthropic.com/v1/messages)
    end_time=$(date +%s.%3N)
    latency=$(echo "$end_time - $start_time" | bc)
    echo "Claude API latency: ${latency}s"
}

# Test GitHub API (if configured)
test_github_api() {
    start_time=$(date +%s.%3N)
    curl -s -H "Authorization: token $GITHUB_TOKEN" https://api.github.com/user > /dev/null
    end_time=$(date +%s.%3N)
    latency=$(echo "$end_time - $start_time" | bc)
    echo "GitHub API latency: ${latency}s"
}
```

---

## 📈 Performance Monitoring Setup

### Automated Performance Monitoring Script

**Complete monitoring script** (`/opt/digitalme/performance_monitor.sh`):

```bash
#!/bin/bash

# DigitalMe Performance Monitoring Script
# Run every 5 minutes via cron

LOG_DIR="/var/log/digitalme"
TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S')
DATE=$(date '+%Y%m%d')

# Create log directory if it doesn't exist
mkdir -p $LOG_DIR

# Function to log metrics
log_metric() {
    local metric_name=$1
    local value=$2
    local unit=$3
    echo "[$TIMESTAMP] $metric_name: $value$unit" >> $LOG_DIR/performance-${DATE}.log
}

# Check application response time
response_time=$(curl -w '%{time_total}' -o /dev/null -s http://localhost/health)
log_metric "response_time_health" $response_time "s"

# Check memory usage
container_memory=$(docker stats --no-stream --format "{{.MemUsage}}" digitalme-digitalme-1 | cut -d'/' -f1 | sed 's/MiB//')
log_metric "container_memory" $container_memory "MB"

# Check CPU usage  
container_cpu=$(docker stats --no-stream --format "{{.CPUPerc}}" digitalme-digitalme-1 | cut -d'%' -f1)
log_metric "container_cpu" $container_cpu "%"

# Check disk usage
disk_usage=$(df -h | grep -E "/$" | awk '{print $5}' | cut -d'%' -f1)
log_metric "disk_usage" $disk_usage "%"

# Check database size
db_size=$(docker exec digitalme-digitalme-1 du -m /app/data/digitalme.db | cut -f1)
log_metric "database_size" $db_size "MB"

# Alert if thresholds exceeded
if (( $(echo "$response_time > 3" | bc -l) )); then
    echo "[$TIMESTAMP] ALERT: High response time: ${response_time}s" >> $LOG_DIR/alerts-${DATE}.log
fi

if (( container_memory > 2500 )); then
    echo "[$TIMESTAMP] ALERT: High memory usage: ${container_memory}MB" >> $LOG_DIR/alerts-${DATE}.log
fi

if (( $(echo "$container_cpu > 85" | bc -l) )); then
    echo "[$TIMESTAMP] ALERT: High CPU usage: ${container_cpu}%" >> $LOG_DIR/alerts-${DATE}.log
fi
```

### Performance Baseline Validation

**Daily Performance Validation Script:**

```bash
#!/bin/bash
# Daily performance baseline validation

echo "=== DigitalMe Performance Baseline Validation ===" 
echo "Date: $(date)"
echo ""

# Test response times
echo "Testing response times..."
health_time=$(curl -w '%{time_total}' -o /dev/null -s http://localhost/health)
info_time=$(curl -w '%{time_total}' -o /dev/null -s http://localhost/info)

echo "Health endpoint: ${health_time}s (target: <0.5s)"
echo "Info endpoint: ${info_time}s (target: <1s)"

# Check resource usage
echo ""
echo "Checking resource usage..."
memory_usage=$(docker stats --no-stream --format "{{.MemUsage}}" digitalme-digitalme-1)
cpu_usage=$(docker stats --no-stream --format "{{.CPUPerc}}" digitalme-digitalme-1)

echo "Memory usage: $memory_usage (target: <2GB)"
echo "CPU usage: $cpu_usage (target: <70%)"

# Database metrics
echo ""
echo "Database metrics..."
db_size=$(docker exec digitalme-digitalme-1 du -h /app/data/digitalme.db)
echo "Database size: $db_size"

# Check database integrity
integrity_check=$(docker exec digitalme-digitalme-1 sqlite3 /app/data/digitalme.db "PRAGMA integrity_check;")
echo "Database integrity: $integrity_check"

echo ""
echo "=== Performance Validation Complete ==="
```

---

## 🔧 Performance Optimization Procedures

### Memory Optimization

**Memory Usage Optimization Steps:**

1. **Monitor Memory Patterns:**
```bash
# Track memory usage over 24 hours
docker exec digitalme-digitalme-1 cat /proc/meminfo | grep -E "MemTotal|MemFree|MemAvailable" > memory_baseline_$(date +%Y%m%d).log
```

2. **Optimize .NET Runtime:**
```json
// Add to appsettings.Production.json
{
  "RuntimeOptimizations": {
    "GCServer": true,
    "GCConcurrent": true,
    "GCRetainVM": true,
    "ThreadPoolMinWorkerThreads": 50,
    "ThreadPoolMinCompletionPortThreads": 50
  }
}
```

3. **Database Memory Optimization:**
```bash
# SQLite memory optimization
docker exec digitalme-digitalme-1 sqlite3 /app/data/digitalme.db "PRAGMA cache_size = -64000;"  # 64MB cache
docker exec digitalme-digitalme-1 sqlite3 /app/data/digitalme.db "PRAGMA temp_store = MEMORY;"
```

### CPU Optimization

**CPU Performance Optimization:**

1. **Container Resource Limits:**
```yaml
# docker-compose.production.yml
deploy:
  resources:
    limits:
      cpus: '2.0'
      memory: 4G
    reservations:
      cpus: '1.0'
      memory: 2G
```

2. **Kestrel Server Optimization:**
```json
{
  "Kestrel": {
    "Limits": {
      "MaxConcurrentConnections": 1000,
      "MaxRequestBodySize": 30000000,
      "KeepAliveTimeout": "00:02:00"
    }
  }
}
```

---

## 📊 Performance Dashboard Template

### Key Metrics Dashboard

**Daily Performance Summary Template:**

```
╔══════════════════════════════════════════════════════════════╗
║                 DigitalMe Performance Dashboard               ║
║                    Date: $(date +%Y-%m-%d)                   ║
╠══════════════════════════════════════════════════════════════╣
║ Response Times                                               ║
║ • Health Endpoint:     ${health_time}s  (Target: <0.5s)     ║
║ • Info Endpoint:       ${info_time}s   (Target: <1.0s)      ║
║ • API Endpoints:       ${api_time}s    (Target: <2.0s)      ║
╠══════════════════════════════════════════════════════════════╣
║ Resource Usage                                               ║
║ • Memory Usage:        ${memory_usage}  (Target: <2GB)      ║
║ • CPU Usage:           ${cpu_usage}     (Target: <70%)      ║
║ • Disk Usage:          ${disk_usage}    (Target: <80%)      ║
╠══════════════════════════════════════════════════════════════╣
║ Database Metrics                                             ║
║ • Database Size:       ${db_size}       (Target: <1GB)      ║
║ • Query Performance:   ${query_time}ms  (Target: <100ms)    ║
║ • Integrity Status:    ${integrity}     (Target: OK)        ║
╠══════════════════════════════════════════════════════════════╣
║ Availability                                                 ║
║ • Uptime Today:        ${uptime}        (Target: >99.5%)    ║
║ • Health Check Pass:   ${health_pass}%  (Target: >99%)      ║
║ • Error Rate:          ${error_rate}%   (Target: <1%)       ║
╚══════════════════════════════════════════════════════════════╝
```

---

## 📞 Performance Escalation Matrix

### Performance Alert Levels

**Level 1 - Warning (Response: 1 hour):**
- Response time >3s for 5+ minutes
- Memory usage >2.5GB sustained
- CPU usage >85% for 10+ minutes
- Error rate >2%

**Level 2 - Critical (Response: 15 minutes):**
- Response time >5s for 2+ minutes  
- Memory usage >3GB
- CPU usage >95% for 5+ minutes
- Error rate >5%
- Service unavailable

**Level 3 - Emergency (Response: Immediate):**
- Complete service outage
- Database corruption
- Memory exhaustion (OOM kills)
- Security performance anomalies

---

**End of Performance Baselines Document**

*This document defines performance targets and monitoring procedures. For troubleshooting performance issues, see TROUBLESHOOTING_GUIDE.md. For daily operations, see OPERATIONS_MANUAL.md.*