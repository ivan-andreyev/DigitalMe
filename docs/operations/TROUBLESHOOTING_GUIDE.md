# 🔧 DigitalMe Troubleshooting Guide

**Version**: 1.0  
**Last Updated**: 2025-09-07  
**Purpose**: Systematic problem diagnosis and resolution for DigitalMe production issues  
**Environment**: Production  

---

## 🚨 Emergency Quick Reference

### Critical Issue Response (Response Time: <15 minutes)

**Service Completely Down:**
```bash
# 1. Quick restart
docker-compose -f docker-compose.production.yml restart

# 2. Check status
docker-compose -f docker-compose.production.yml ps
curl -f http://localhost/health

# 3. If still down, check logs
docker-compose -f docker-compose.production.yml logs --tail=100
```

**Database Corruption:**
```bash
# 1. Stop service immediately
docker-compose -f docker-compose.production.yml stop digitalme

# 2. Restore from latest backup
docker run --rm -v digitalme_digitalme-data:/data -v $(pwd):/backup alpine cp /backup/latest-backup.db /data/digitalme.db

# 3. Restart service
docker-compose -f docker-compose.production.yml start digitalme
```

---

## 📊 Diagnostic Decision Tree

### Problem Classification Matrix

| **Symptom** | **Likely Cause** | **Quick Check** | **Section** |
|-------------|------------------|-----------------|-------------|
| Service won't start | Environment/Config | `docker logs` | [Container Issues](#-container-issues) |
| Health checks failing | Database/API | `curl /health` | [Health Check Issues](#-health-check-issues) |
| Slow responses | Performance | `docker stats` | [Performance Issues](#-performance-issues) |
| SSL/Certificate errors | Certificate config | `openssl verify` | [SSL Issues](#-ssl-certificate-issues) |
| Authentication failing | JWT/API keys | Check environment | [Security Issues](#-security-issues) |
| High resource usage | Memory leaks/scaling | `top`, `free -h` | [Performance Issues](#-performance-issues) |

---

## 🐳 Container Issues

### Issue: Container Won't Start

**Symptoms:**
- Container exits immediately
- "Exited (1)" status in `docker-compose ps`
- Application not responding on configured ports

**Diagnostic Commands:**
```bash
# Check container logs
docker-compose -f docker-compose.production.yml logs digitalme

# Check container details
docker inspect digitalme-digitalme-1

# Test image directly (bypass compose)
docker run --rm -e ANTHROPIC_API_KEY=test-key your-registry/digitalme:latest
```

**Common Root Causes & Solutions:**

**1. Missing Required Environment Variables:**
```bash
# Check environment file
cat .env | grep -E "(ANTHROPIC_API_KEY|JWT_KEY)"

# Verify API key format
echo $ANTHROPIC_API_KEY | grep -E "^sk-ant-"

# Fix: Add missing variables
echo "ANTHROPIC_API_KEY=sk-ant-your-key-here" >> .env
echo "JWT_KEY=$(openssl rand -hex 32)" >> .env
```

**2. JWT Key Too Short:**
```bash
# Check JWT key length
echo $JWT_KEY | wc -c  # Should be >32 characters

# Fix: Generate proper JWT key
sed -i 's/JWT_KEY=.*/JWT_KEY='$(openssl rand -hex 32)'/' .env
```

**3. Database Volume Issues:**
```bash
# Check volume mounts
docker volume ls | grep digitalme

# Check permissions
docker exec digitalme-digitalme-1 ls -la /app/data/

# Fix: Reset volume permissions
docker exec digitalme-digitalme-1 chown -R digitalme:digitalme /app/data
```

**4. Port Conflicts:**
```bash
# Check what's using ports 80/443
sudo netstat -tlnp | grep -E ":80|:443"

# Fix: Change ports in docker-compose.production.yml
ports:
  - "8080:80"   # Instead of "80:80"
  - "8443:443"  # Instead of "443:443"
```

### Issue: Container Keeps Restarting

**Symptoms:**
- Container starts then stops repeatedly
- Health check failures
- "Restarting" status in docker ps

**Diagnostic Flow:**
```bash
# 1. Check restart reason
docker inspect digitalme-digitalme-1 | jq '.[0].State'

# 2. Monitor real-time logs
docker-compose -f docker-compose.production.yml logs -f digitalme

# 3. Check resource limits
docker stats digitalme-digitalme-1

# 4. Verify health check
curl -v http://localhost/health
```

**Solutions by Error Pattern:**

**Memory Issues:**
```bash
# Check memory usage
free -h
docker stats --no-stream digitalme-digitalme-1

# Solution: Increase memory limits
# Edit docker-compose.production.yml:
deploy:
  resources:
    limits:
      memory: 4G
```

**Database Lock Issues:**
```bash
# Check database locks
docker exec digitalme-digitalme-1 sqlite3 /app/data/digitalme.db "PRAGMA locking_mode;"

# Solution: Stop all instances accessing DB
docker-compose -f docker-compose.production.yml scale digitalme=0
sleep 5
docker-compose -f docker-compose.production.yml scale digitalme=1
```

---

## ❤️ Health Check Issues

### Issue: Health Endpoint Returning Errors

**Symptoms:**
- `/health` returns 500 status
- Health check shows "Unhealthy" status
- Load balancer removing instance from rotation

**Diagnostic Commands:**
```bash
# Test health endpoint directly
curl -v http://localhost/health
curl -v http://localhost/health/ready

# Check detailed health response
curl http://localhost/health | jq '.'

# Test specific health checks
curl http://localhost/info | jq '.database'
```

**Root Cause Analysis:**

**1. Database Connectivity Issues:**
```bash
# Check database file
docker exec digitalme-digitalme-1 ls -la /app/data/digitalme.db

# Test database access
docker exec digitalme-digitalme-1 sqlite3 /app/data/digitalme.db ".databases"

# Check database integrity
docker exec digitalme-digitalme-1 sqlite3 /app/data/digitalme.db "PRAGMA integrity_check;"

# Solution: Restore from backup if corrupted
docker run --rm -v digitalme_digitalme-data:/data -v $(pwd):/backup alpine cp /backup/latest-backup.db /data/digitalme.db
```

**2. Claude API Connectivity:**
```bash
# Test API key manually
curl -H "Content-Type: application/json" \
     -H "x-api-key: $ANTHROPIC_API_KEY" \
     -d '{"model":"claude-3-5-sonnet-20241022","max_tokens":10,"messages":[{"role":"user","content":"test"}]}' \
     https://api.anthropic.com/v1/messages

# Check API key in container
docker exec digitalme-digitalme-1 env | grep ANTHROPIC_API_KEY

# Solution: Update API key
sed -i 's/ANTHROPIC_API_KEY=.*/ANTHROPIC_API_KEY=sk-ant-your-new-key/' .env
docker-compose -f docker-compose.production.yml up -d
```

**3. External Service Dependencies:**
```bash
# Test GitHub API (if configured)
curl -H "Authorization: token $GITHUB_TOKEN" https://api.github.com/user

# Test Slack API (if configured)  
curl -H "Authorization: Bearer $SLACK_BOT_TOKEN" https://slack.com/api/auth.test

# Solution: Update service credentials
nano .env  # Update relevant tokens
docker-compose -f docker-compose.production.yml restart
```

---

## ⚡ Performance Issues

### Issue: Slow Response Times

**Symptoms:**
- API responses >5 seconds
- Timeouts in client applications
- High CPU/memory usage

**Performance Diagnostic Steps:**
```bash
# 1. Measure response times
curl -w "@curl-format.txt" -o /dev/null -s http://localhost/info

# 2. Check system resources
top -bn1
free -h
df -h

# 3. Monitor container resources
docker stats digitalme-digitalme-1

# 4. Check application logs for slow operations
docker-compose logs | grep -i "slow\|timeout\|performance"
```

**Performance Analysis by Metrics:**

**High Memory Usage (>85%):**
```bash
# Check memory leaks
docker exec digitalme-digitalme-1 cat /proc/meminfo

# Check application memory
docker exec digitalme-digitalme-1 ps aux | grep dotnet

# Solutions:
# 1. Restart application (immediate)
docker-compose -f docker-compose.production.yml restart digitalme

# 2. Increase memory limits (if needed)
# Edit docker-compose.production.yml:
deploy:
  resources:
    limits:
      memory: 4G
```

**High CPU Usage (>90%):**
```bash
# Identify CPU-intensive processes
docker exec digitalme-digitalme-1 top -bn1

# Check for infinite loops in logs
docker-compose logs --tail=1000 | grep -i "loop\|recursive\|infinite"

# Solutions:
# 1. Restart if CPU spike is temporary
docker-compose -f docker-compose.production.yml restart digitalme

# 2. Scale up if consistently high
# Edit docker-compose.production.yml:
deploy:
  resources:
    limits:
      cpus: '2.0'
```

**Database Performance Issues:**
```bash
# Check database size
docker exec digitalme-digitalme-1 du -h /app/data/digitalme.db

# Optimize database
docker exec digitalme-digitalme-1 sqlite3 /app/data/digitalme.db "VACUUM;"
docker exec digitalme-digitalme-1 sqlite3 /app/data/digitalme.db "REINDEX;"

# Check for slow queries (if logging enabled)
docker-compose logs | grep -i "query.*slow"
```

### Issue: Memory Leaks

**Symptoms:**
- Memory usage continuously increasing
- Container gets killed (OOM)
- System becomes unresponsive

**Memory Leak Diagnostic:**
```bash
# Monitor memory over time (run for 10 minutes)
for i in {1..10}; do
  echo "$(date): $(docker stats --no-stream digitalme-digitalme-1 | awk 'NR==2 {print $4}')"
  sleep 60
done

# Check for memory-intensive operations
docker exec digitalme-digitalme-1 cat /proc/$(pidof dotnet)/status | grep -E "VmSize|VmRSS"

# Solutions:
# 1. Immediate: Restart service
docker-compose -f docker-compose.production.yml restart digitalme

# 2. Long-term: Add memory limits and restart policy
# In docker-compose.production.yml:
deploy:
  resources:
    limits:
      memory: 2G
restart: always
```

---

## 🔒 SSL Certificate Issues

### Issue: HTTPS Not Working

**Symptoms:**
- Certificate errors in browser
- SSL handshake failures
- HTTPS requests timing out

**SSL Diagnostic Commands:**
```bash
# 1. Check certificate files exist
ls -la certs/

# 2. Verify certificate validity
openssl x509 -in certs/aspnetapp.crt -text -noout | grep -E "Not Before|Not After|Subject"

# 3. Test SSL connection
openssl s_client -connect localhost:443 -servername localhost < /dev/null

# 4. Check certificate in container
docker exec digitalme-digitalme-1 ls -la /https/
```

**Common SSL Issues & Solutions:**

**1. Certificate Expired:**
```bash
# Check expiration date
openssl x509 -in certs/aspnetapp.crt -enddate -noout

# Solution: Renew Let's Encrypt certificate
sudo certbot renew
sudo cp /etc/letsencrypt/live/your-domain.com/fullchain.pem certs/aspnetapp.crt
sudo cp /etc/letsencrypt/live/your-domain.com/privkey.pem certs/aspnetapp.key
openssl pkcs12 -export -out certs/aspnetapp.pfx -inkey certs/aspnetapp.key -in certs/aspnetapp.crt -password pass:DigitalMe2024!
```

**2. Wrong Certificate Format:**
```bash
# Check certificate format
file certs/aspnetapp.*

# Convert to PFX if needed
openssl pkcs12 -export -out certs/aspnetapp.pfx -inkey certs/aspnetapp.key -in certs/aspnetapp.crt -password pass:DigitalMe2024!
```

**3. Certificate Permission Issues:**
```bash
# Check permissions
ls -la certs/

# Fix permissions
chmod 600 certs/*
chown $(whoami):$(whoami) certs/*
```

**4. Self-Signed Certificate Issues:**
```bash
# Regenerate self-signed certificate
openssl req -x509 -newkey rsa:4096 -keyout certs/aspnetapp.key -out certs/aspnetapp.crt -days 365 -nodes -subj "/CN=localhost"
openssl pkcs12 -export -out certs/aspnetapp.pfx -inkey certs/aspnetapp.key -in certs/aspnetapp.crt -password pass:DigitalMe2024!

# Restart services
docker-compose -f docker-compose.production.yml restart
```

---

## 🔐 Security Issues

### Issue: Authentication Failures

**Symptoms:**
- Login requests returning 401 Unauthorized
- JWT token validation errors
- "Invalid token" errors in logs

**Authentication Diagnostic:**
```bash
# 1. Check JWT configuration
docker exec digitalme-digitalme-1 env | grep JWT_KEY

# 2. Test token generation endpoint (if available)
curl -X POST http://localhost/api/auth/login -H "Content-Type: application/json" -d '{"username":"test","password":"test"}'

# 3. Check authentication logs
docker-compose logs | grep -i "auth\|jwt\|token"
```

**Common Authentication Issues:**

**1. JWT Key Issues:**
```bash
# Check JWT key length
echo $JWT_KEY | wc -c  # Should be >32 characters

# Verify JWT key in environment
grep JWT_KEY .env

# Solution: Generate new JWT key
echo "JWT_KEY=$(openssl rand -hex 32)" >> .env
docker-compose -f docker-compose.production.yml restart
```

**2. Token Expiration Issues:**
```bash
# Check token expiration in configuration
docker exec digitalme-digitalme-1 cat appsettings.Production.json | grep -i exp

# Solution: Adjust expiration time
# Edit appsettings.Production.json:
# "JWT": { "ExpireHours": 24 }
```

### Issue: API Key Problems

**Symptoms:**
- External API calls failing
- "Invalid API key" errors
- Service integration failures

**API Key Diagnostic:**
```bash
# 1. Verify Anthropic API key format
echo $ANTHROPIC_API_KEY | grep -E "^sk-ant-"

# 2. Test API key validity
curl -H "Content-Type: application/json" \
     -H "x-api-key: $ANTHROPIC_API_KEY" \
     -d '{"model":"claude-3-5-sonnet-20241022","max_tokens":10,"messages":[{"role":"user","content":"test"}]}' \
     https://api.anthropic.com/v1/messages

# 3. Check other service keys
echo "GitHub: ${GITHUB_TOKEN:0:10}..."
echo "Slack: ${SLACK_BOT_TOKEN:0:10}..."
```

**API Key Solutions:**
```bash
# Update Anthropic API key
sed -i 's/ANTHROPIC_API_KEY=.*/ANTHROPIC_API_KEY=sk-ant-your-new-key/' .env

# Update GitHub token
sed -i 's/GITHUB_TOKEN=.*/GITHUB_TOKEN=ghp_your-new-token/' .env

# Restart to apply changes
docker-compose -f docker-compose.production.yml restart
```

---

## 🌐 Network Connectivity Issues

### Issue: External API Calls Failing

**Symptoms:**
- TimeoutException in logs
- "Connection refused" errors
- External service integrations not working

**Network Diagnostic:**
```bash
# 1. Test basic connectivity from container
docker exec digitalme-digitalme-1 ping google.com
docker exec digitalme-digitalme-1 nslookup api.anthropic.com

# 2. Test HTTPS connectivity
docker exec digitalme-digitalme-1 curl -I https://api.anthropic.com/v1/messages

# 3. Check firewall rules
sudo ufw status
sudo iptables -L
```

**Network Solutions:**
```bash
# 1. DNS issues
echo "nameserver 8.8.8.8" | docker exec -i digitalme-digitalme-1 tee -a /etc/resolv.conf

# 2. Proxy configuration (if applicable)
# Add to docker-compose.production.yml:
environment:
  - HTTP_PROXY=http://proxy.company.com:8080
  - HTTPS_PROXY=http://proxy.company.com:8080

# 3. Firewall rules
sudo ufw allow out 443
sudo ufw allow out 80
```

---

## 📊 Log Analysis Patterns

### Critical Error Patterns

**Fatal Application Errors:**
```bash
# Search for critical errors
docker-compose logs | grep -E "FATAL|ERROR|Exception" | tail -20

# Common patterns to look for:
docker-compose logs | grep -E "OutOfMemoryException|StackOverflowException|AccessViolationException"
```

**Database Errors:**
```bash
# Database-related errors
docker-compose logs | grep -i "database\|sqlite" | grep -i error

# Connection issues
docker-compose logs | grep -E "connection.*timeout|database.*lock"
```

**Security-Related Errors:**
```bash
# Authentication failures
docker-compose logs | grep -E "Unauthorized|Forbidden|Invalid.*token"

# Suspicious activity
docker-compose logs | grep -E "brute.*force|repeated.*failed|suspicious"
```

### Log Rotation and Management

**Configure Log Rotation:**
```bash
# Set Docker daemon log rotation
echo '{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  }
}' | sudo tee /etc/docker/daemon.json

sudo systemctl restart docker
```

**Manual Log Cleanup:**
```bash
# Clean old logs
docker system prune -f

# Archive important logs
docker-compose logs --since 24h > /var/log/digitalme-$(date +%Y%m%d).log
```

---

## 📋 Troubleshooting Checklist

### Quick Diagnostic Checklist (5 minutes)

- [ ] **Container Status**: `docker-compose ps` shows "Up"
- [ ] **Health Check**: `curl -f http://localhost/health` returns 200
- [ ] **Basic Connectivity**: Can access application endpoints
- [ ] **Database Present**: Database file exists and is accessible
- [ ] **Recent Logs**: No critical errors in last 100 log lines
- [ ] **System Resources**: Memory <85%, Disk <80%

### Comprehensive Diagnostic Checklist (15 minutes)

- [ ] **Environment Variables**: All required vars present and valid
- [ ] **Certificate Status**: SSL certificates valid and not expired
- [ ] **API Key Validation**: External service API keys working
- [ ] **Database Integrity**: Database passes integrity check
- [ ] **Backup Status**: Recent backups available and valid
- [ ] **Performance Metrics**: Response times within acceptable ranges
- [ ] **Security**: No unauthorized access attempts
- [ ] **External Dependencies**: All external services reachable

### Post-Resolution Validation

- [ ] **Service Restoration**: All endpoints responding correctly
- [ ] **Data Integrity**: No data corruption or loss
- [ ] **Performance**: Response times back to normal
- [ ] **Monitoring**: All monitoring systems showing healthy status
- [ ] **Documentation**: Issue documented with root cause and solution
- [ ] **Prevention**: Measures taken to prevent recurrence

---

## 📞 When to Escalate

### Level 1 → Level 2 Escalation (Engineering Team)
- Issue persists after following troubleshooting steps
- Multiple services affected
- Performance degradation affecting users
- Security-related incidents

### Level 2 → Level 3 Escalation (Emergency Response)
- Data loss or corruption
- Security breach suspected
- Complete service outage >30 minutes
- Cannot restore from backup

---

**End of Troubleshooting Guide**

*This guide covers common issues and their resolutions. For deployment procedures, see PRODUCTION_DEPLOYMENT_GUIDE.md. For routine operations, see OPERATIONS_MANUAL.md.*