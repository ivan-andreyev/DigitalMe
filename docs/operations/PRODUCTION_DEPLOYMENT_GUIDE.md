# 🚀 DigitalMe Production Deployment Guide

**Version**: 1.0  
**Last Updated**: 2025-09-07  
**Target Environment**: Production-ready deployment with enterprise operational standards  
**MVP Phase**: Phase 6 - Production Readiness  

---

## 📋 Quick Reference

| **Deployment Type** | **Recommended For** | **Setup Time** | **Complexity** |
|------------------|-----------------|--------------|-------------|
| **Docker Standalone** | Small-medium deployments | 15 minutes | Simple |
| **Docker + Reverse Proxy** | Production with custom domain | 30 minutes | Medium |
| **Cloud Container** | Auto-scaling, managed infrastructure | 45 minutes | Medium |
| **Kubernetes** | Large-scale, enterprise deployments | 2 hours | Advanced |

**🎯 Recommended for most users: Docker Standalone → upgrade to Docker + Reverse Proxy as needed**

---

## 🎯 Production Deployment Options

### Option 1: Docker Standalone (Recommended)

**Best for:** Most production deployments, VPS hosting, quick setup  
**Infrastructure:** Single container with health monitoring  
**Scalability:** Handles 100-500 concurrent users  

### Option 2: Docker + Reverse Proxy

**Best for:** Custom domains, SSL termination, load balancing  
**Infrastructure:** Application container + Nginx/Traefik  
**Scalability:** Handles 500-2000 concurrent users  

### Option 3: Cloud Container Services

**Best for:** Managed infrastructure, auto-scaling, zero server maintenance  
**Infrastructure:** Azure Container Instances, AWS Fargate, Google Cloud Run  
**Scalability:** Auto-scales based on demand  

### Option 4: Kubernetes Deployment

**Best for:** Enterprise environments, high availability, complex scaling requirements  
**Infrastructure:** Kubernetes cluster with multiple replicas  
**Scalability:** Unlimited horizontal scaling  

---

## 🚀 Option 1: Docker Standalone Deployment

### Prerequisites

**System Requirements:**
- **OS:** Linux (Ubuntu 20.04+), Windows Server 2019+, or macOS 10.15+
- **RAM:** 2GB minimum, 4GB recommended
- **Storage:** 10GB minimum, 20GB recommended
- **Network:** Public IP or domain name
- **Docker:** Version 20.10+ with Docker Compose

**API Requirements:**
- **Anthropic API Key** (Required) - Get from [Anthropic Console](https://console.anthropic.com/)
- External service API keys (Optional) - GitHub, Slack, ClickUp, Telegram

### Step 1: System Preparation

```bash
# Update system packages
sudo apt update && sudo apt upgrade -y

# Install Docker and Docker Compose
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Add current user to docker group
sudo usermod -aG docker $USER
newgrp docker

# Verify installation
docker --version
docker-compose --version
```

### Step 2: Application Setup

```bash
# Clone repository
git clone https://github.com/your-org/digitalme.git
cd digitalme

# Create production environment file
cp .env.example .env
nano .env
```

### Step 3: Environment Configuration

**Edit `.env` file with your production values:**

```bash
# Essential Configuration (Required)
ANTHROPIC_API_KEY=sk-ant-your-actual-anthropic-key-here
ANTHROPIC_MODEL=claude-3-5-sonnet-20241022

# JWT Security (Generate a secure key)
JWT_KEY=your-super-secure-jwt-key-minimum-32-characters-required

# Production Environment
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80;https://+:443

# Database Configuration
DATABASE_PATH=/app/data/digitalme.db

# Optional External Services (leave empty if not used)
GITHUB_TOKEN=ghp_your-github-token-here
GOOGLE_CLIENT_ID=your-google-oauth-client-id
GOOGLE_CLIENT_SECRET=your-google-oauth-client-secret
TELEGRAM_BOT_TOKEN=your-telegram-bot-token-here
SLACK_BOT_TOKEN=xoxb-your-slack-bot-token-here
CLICKUP_API_TOKEN=pk_your-clickup-api-token-here

# Security Configuration (Advanced)
MAX_PAYLOAD_SIZE=1048576
RATE_LIMIT_REQUESTS_PER_MINUTE=60
ENABLE_INPUT_SANITIZATION=true
ENABLE_RATE_LIMITING=true
```

### Step 4: SSL Certificate Setup

**Option A: Self-Signed Certificate (Development/Testing)**
```bash
# Create certificates directory
mkdir -p certs

# Generate self-signed certificate
openssl req -x509 -newkey rsa:4096 -keyout certs/aspnetapp.key -out certs/aspnetapp.crt -days 365 -nodes -subj "/CN=localhost"

# Create PFX certificate for .NET
openssl pkcs12 -export -out certs/aspnetapp.pfx -inkey certs/aspnetapp.key -in certs/aspnetapp.crt -password pass:DigitalMe2024!
```

**Option B: Let's Encrypt Certificate (Production with Domain)**
```bash
# Install Certbot
sudo apt install certbot -y

# Stop any running web servers
sudo systemctl stop nginx apache2 2>/dev/null || true

# Generate certificate (replace your-domain.com)
sudo certbot certonly --standalone -d your-domain.com --email your-email@domain.com --agree-tos --non-interactive

# Copy certificates
sudo cp /etc/letsencrypt/live/your-domain.com/fullchain.pem certs/aspnetapp.crt
sudo cp /etc/letsencrypt/live/your-domain.com/privkey.pem certs/aspnetapp.key

# Convert to PFX
sudo openssl pkcs12 -export -out certs/aspnetapp.pfx -inkey certs/aspnetapp.key -in certs/aspnetapp.crt -password pass:DigitalMe2024!

# Set permissions
sudo chown -R $USER:$USER certs/
chmod 600 certs/*
```

### Step 5: Production Deployment

**Simple Docker Compose for standalone deployment:**

Create `docker-compose.production.yml`:

```yaml
version: '3.8'

services:
  digitalme:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "80:80"      # HTTP
      - "443:443"    # HTTPS
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80;https://+:443
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
      - ASPNETCORE_Kestrel__Certificates__Default__Password=DigitalMe2024!
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/digitalme.db
      - Anthropic__ApiKey=${ANTHROPIC_API_KEY}
      - JWT__Key=${JWT_KEY}
      # External services (if configured)
      - GitHub__PersonalAccessToken=${GITHUB_TOKEN}
      - Google__ClientId=${GOOGLE_CLIENT_ID}
      - Google__ClientSecret=${GOOGLE_CLIENT_SECRET}
      - Telegram__BotToken=${TELEGRAM_BOT_TOKEN}
      - Integrations__Slack__BotToken=${SLACK_BOT_TOKEN}
      - Integrations__ClickUp__ApiToken=${CLICKUP_API_TOKEN}
    volumes:
      - digitalme-data:/app/data
      - digitalme-backups:/app/backups
      - digitalme-logs:/app/logs
      - ./certs:/https:ro
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

volumes:
  digitalme-data:
    driver: local
  digitalme-backups:
    driver: local
  digitalme-logs:
    driver: local
```

**Deploy the application:**

```bash
# Build and start the application
docker-compose -f docker-compose.production.yml up -d

# Check deployment status
docker-compose -f docker-compose.production.yml ps

# View logs
docker-compose -f docker-compose.production.yml logs -f
```

### Step 6: Deployment Verification

```bash
# 1. Check container status
docker-compose -f docker-compose.production.yml ps

# Expected output:
# NAME                    COMMAND                  SERVICE      STATUS          PORTS
# digitalme-digitalme-1   "dotnet DigitalMe.dll"   digitalme    Up 2 minutes    0.0.0.0:80->80/tcp, 0.0.0.0:443->443/tcp

# 2. Test health endpoints
curl -f http://localhost/health
curl -f http://localhost/info

# 3. Check SSL (if configured)
curl -k https://localhost/health

# 4. Verify database
docker exec digitalme-digitalme-1 ls -la /app/data/

# 5. Check logs for errors
docker-compose -f docker-compose.production.yml logs --tail=50
```

**Expected Health Check Response:**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.123",
  "entries": {
    "digitalme-db": {
      "status": "Healthy",
      "description": "Database is accessible"
    },
    "claude-api": {
      "status": "Healthy",
      "description": "Claude API key is configured"
    }
  }
}
```

---

## 🔧 Option 2: Docker + Reverse Proxy

### Nginx Reverse Proxy Setup

**Create `nginx/nginx.conf`:**

```nginx
events {
    worker_connections 1024;
}

http {
    upstream digitalme {
        server digitalme:80;
    }

    # Rate limiting
    limit_req_zone $binary_remote_addr zone=api:10m rate=10r/s;
    limit_req_zone $binary_remote_addr zone=general:10m rate=100r/m;

    server {
        listen 80;
        server_name your-domain.com;

        # Redirect HTTP to HTTPS
        return 301 https://$server_name$request_uri;
    }

    server {
        listen 443 ssl http2;
        server_name your-domain.com;

        # SSL Configuration
        ssl_certificate /etc/ssl/certs/aspnetapp.crt;
        ssl_certificate_key /etc/ssl/certs/aspnetapp.key;
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers HIGH:!aNULL:!MD5;

        # Security Headers
        add_header X-Frame-Options "DENY" always;
        add_header X-Content-Type-Options "nosniff" always;
        add_header X-XSS-Protection "1; mode=block" always;
        add_header Strict-Transport-Security "max-age=63072000" always;

        # API Routes with rate limiting
        location /api/ {
            limit_req zone=api burst=20 nodelay;
            proxy_pass http://digitalme;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }

        # General routes
        location / {
            limit_req zone=general burst=50 nodelay;
            proxy_pass http://digitalme;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }

        # Health check (no rate limiting)
        location /health {
            proxy_pass http://digitalme;
            access_log off;
        }
    }
}
```

**Extended Docker Compose with Nginx:**

```yaml
version: '3.8'

services:
  digitalme:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "5000:80"  # Internal port
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/digitalme.db
      - Anthropic__ApiKey=${ANTHROPIC_API_KEY}
      - JWT__Key=${JWT_KEY}
    volumes:
      - digitalme-data:/app/data
      - digitalme-backups:/app/backups
      - digitalme-logs:/app/logs
    restart: unless-stopped
    networks:
      - digitalme-network

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./certs:/etc/ssl/certs:ro
      - nginx-logs:/var/log/nginx
    depends_on:
      - digitalme
    restart: unless-stopped
    networks:
      - digitalme-network

volumes:
  digitalme-data:
  digitalme-backups:
  digitalme-logs:
  nginx-logs:

networks:
  digitalme-network:
    driver: bridge
```

---

## ☁️ Option 3: Cloud Container Services

### Azure Container Instances

```bash
# Login to Azure
az login

# Create resource group
az group create --name digitalme-rg --location eastus

# Create container instance
az container create \
  --resource-group digitalme-rg \
  --name digitalme \
  --image your-registry/digitalme:latest \
  --cpu 2 --memory 4 \
  --ports 80 443 \
  --environment-variables \
    ASPNETCORE_ENVIRONMENT=Production \
    ANTHROPIC_API_KEY=$ANTHROPIC_API_KEY \
    JWT_KEY=$JWT_KEY \
  --secure-environment-variables \
    ConnectionStrings__DefaultConnection="Data Source=/app/data/digitalme.db" \
  --azure-file-volume-account-name mystorageaccount \
  --azure-file-volume-account-key $STORAGE_KEY \
  --azure-file-volume-share-name digitalme-data \
  --azure-file-volume-mount-path /app/data

# Get public IP
az container show --resource-group digitalme-rg --name digitalme --query ipAddress.ip --output tsv
```

### AWS Fargate

**Create `fargate-task-definition.json`:**

```json
{
  "family": "digitalme",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "1024",
  "memory": "2048",
  "executionRoleArn": "arn:aws:iam::YOUR-ACCOUNT:role/ecsTaskExecutionRole",
  "containerDefinitions": [
    {
      "name": "digitalme",
      "image": "your-registry/digitalme:latest",
      "portMappings": [
        {
          "containerPort": 80,
          "protocol": "tcp"
        }
      ],
      "environment": [
        {
          "name": "ASPNETCORE_ENVIRONMENT",
          "value": "Production"
        }
      ],
      "secrets": [
        {
          "name": "ANTHROPIC_API_KEY",
          "valueFrom": "arn:aws:secretsmanager:region:account:secret:digitalme/anthropic-key"
        }
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/digitalme",
          "awslogs-region": "us-east-1",
          "awslogs-stream-prefix": "ecs"
        }
      }
    }
  ]
}
```

### Google Cloud Run

```bash
# Build and push image
gcloud builds submit --tag gcr.io/YOUR-PROJECT/digitalme .

# Deploy to Cloud Run
gcloud run deploy digitalme \
  --image gcr.io/YOUR-PROJECT/digitalme \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated \
  --set-env-vars ASPNETCORE_ENVIRONMENT=Production \
  --set-env-vars ANTHROPIC_API_KEY=$ANTHROPIC_API_KEY \
  --memory 2Gi \
  --cpu 2 \
  --concurrency 80 \
  --min-instances 1 \
  --max-instances 10
```

---

## 🔒 Security Configuration

### Essential Security Settings

**1. Environment Variables Security:**
```bash
# Never commit secrets to git
echo ".env" >> .gitignore
echo "*.key" >> .gitignore
echo "*.pfx" >> .gitignore

# Set proper file permissions
chmod 600 .env
chmod 600 certs/*
```

**2. Firewall Configuration (Linux):**
```bash
# Enable UFW firewall
sudo ufw enable

# Allow SSH (change port if using non-standard)
sudo ufw allow 22/tcp

# Allow HTTP/HTTPS
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# Block all other incoming traffic
sudo ufw default deny incoming
sudo ufw default allow outgoing

# Show status
sudo ufw status verbose
```

**3. Database Security:**
```bash
# Create database backup directory with proper permissions
mkdir -p /app/backups
chmod 755 /app/backups

# Database file permissions (handled by Docker, but for reference)
# chmod 644 /app/data/digitalme.db
```

**4. SSL/TLS Configuration:**

For production environments, always use valid SSL certificates:

```bash
# Let's Encrypt automatic renewal
echo "0 12 * * * /usr/bin/certbot renew --quiet" | sudo crontab -

# Check certificate expiration
sudo certbot certificates
```

### JWT Security Best Practices

**Generate secure JWT keys:**
```bash
# Generate a cryptographically secure key (64 characters)
openssl rand -hex 32

# Add to .env file
echo "JWT_KEY=$(openssl rand -hex 32)" >> .env
```

**JWT Configuration in appsettings.Production.json:**
```json
{
  "JWT": {
    "ExpireHours": 8,
    "Issuer": "DigitalMe.API",
    "Audience": "DigitalMe.Client"
  }
}
```

---

## 📊 Environment Configuration Reference

### Required Environment Variables

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `ANTHROPIC_API_KEY` | Anthropic Claude API key | `sk-ant-api03-...` | ✅ |
| `JWT_KEY` | JWT signing key (min 32 chars) | `your-secure-key-here...` | ✅ |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core environment | `Production` | ✅ |

### Optional Environment Variables

| Variable | Description | Example | Service |
|----------|-------------|---------|---------|
| `GITHUB_TOKEN` | GitHub Personal Access Token | `ghp_...` | GitHub Integration |
| `GOOGLE_CLIENT_ID` | Google OAuth Client ID | `123456789.apps.googleusercontent.com` | Google Auth |
| `GOOGLE_CLIENT_SECRET` | Google OAuth Client Secret | `GOCSPX-...` | Google Auth |
| `TELEGRAM_BOT_TOKEN` | Telegram Bot Token | `123456:ABC-DEF...` | Telegram Bot |
| `SLACK_BOT_TOKEN` | Slack Bot User OAuth Token | `xoxb-...` | Slack Integration |
| `CLICKUP_API_TOKEN` | ClickUp API Token | `pk_...` | ClickUp Integration |

### Database Configuration

**SQLite (Default):**
```bash
DATABASE_PATH=/app/data/digitalme.db
```

**PostgreSQL (Advanced):**
```bash
DATABASE_PROVIDER=PostgreSQL
DATABASE_CONNECTION_STRING="Host=localhost;Database=digitalme;Username=digitalme;Password=your-password"
```

**SQL Server (Enterprise):**
```bash
DATABASE_PROVIDER=SqlServer
DATABASE_CONNECTION_STRING="Server=localhost;Database=DigitalMe;Trusted_Connection=true"
```

---

## 🔍 Health Monitoring & Monitoring Setup

### Health Check Endpoints

| Endpoint | Purpose | Response Time | Frequency |
|----------|---------|---------------|-----------|
| `/health` | Application health | <1s | Every 30s |
| `/health/ready` | Readiness probe | <1s | Every 10s |
| `/info` | System information | <1s | On-demand |

### Monitoring Commands

**Container Health:**
```bash
# Check container status
docker-compose ps

# View health check logs
docker inspect --format='{{json .State.Health}}' digitalme-digitalme-1 | jq

# Check resource usage
docker stats digitalme-digitalme-1
```

**Application Health:**
```bash
# Health check
curl -f http://localhost/health

# Detailed system info
curl http://localhost/info | jq

# Check specific services
curl http://localhost/health/ready
```

**Database Health:**
```bash
# Check database file
docker exec digitalme-digitalme-1 ls -la /app/data/

# Database size
docker exec digitalme-digitalme-1 du -h /app/data/digitalme.db

# Check backups
docker exec digitalme-digitalme-1 ls -la /app/backups/
```

### Performance Monitoring

**System Resource Monitoring:**
```bash
# Memory usage
free -h

# Disk usage
df -h

# CPU usage
top -bn1 | grep "Cpu(s)"

# Network connections
netstat -tlnp | grep :80
```

**Application Metrics:**
```bash
# Request metrics (if monitoring enabled)
curl http://localhost/metrics

# Application logs
docker-compose logs -f --tail=100

# Error rate monitoring
docker-compose logs | grep -i error | tail -20
```

---

## 🔄 Backup Procedures

### Automated Backup Configuration

The application includes automated backup functionality:

**Backup Configuration (appsettings.json):**
```json
{
  "Backup": {
    "BackupDirectory": "/app/backups",
    "RetentionDays": 7,
    "MaxBackups": 30,
    "AutoCleanup": true,
    "AutoBackup": true,
    "BackupSchedule": "0 2 * * *"
  }
}
```

### Manual Backup Procedures

**Database Backup:**
```bash
# Manual database backup
docker exec digitalme-digitalme-1 cp /app/data/digitalme.db /app/backups/manual-backup-$(date +%Y%m%d-%H%M%S).db

# Copy backup to host system
docker cp digitalme-digitalme-1:/app/backups/manual-backup-$(date +%Y%m%d-%H%M%S).db ./backup-$(date +%Y%m%d).db
```

**Full System Backup:**
```bash
# Backup all volumes
docker run --rm -v digitalme_digitalme-data:/data -v $(pwd):/backup alpine tar czf /backup/digitalme-data-backup-$(date +%Y%m%d).tar.gz -C /data .
docker run --rm -v digitalme_digitalme-backups:/data -v $(pwd):/backup alpine tar czf /backup/digitalme-backups-backup-$(date +%Y%m%d).tar.gz -C /data .
```

**Backup Verification:**
```bash
# List available backups
docker exec digitalme-digitalme-1 ls -la /app/backups/

# Check backup integrity
docker exec digitalme-digitalme-1 sqlite3 /app/backups/backup-file.db "PRAGMA integrity_check;"
```

### Backup Restoration

**Database Restoration:**
```bash
# Stop the application
docker-compose -f docker-compose.production.yml down

# Restore database
docker run --rm -v digitalme_digitalme-data:/data -v $(pwd):/backup alpine cp /backup/backup-file.db /data/digitalme.db

# Start the application
docker-compose -f docker-compose.production.yml up -d

# Verify restoration
curl -f http://localhost/health
```

---

## 🚨 Troubleshooting Guide

### Common Deployment Issues

**1. Container Won't Start**

**Symptoms:**
- Container exits immediately
- "Exited (1)" status in `docker-compose ps`

**Diagnostics:**
```bash
# Check container logs
docker-compose logs digitalme

# Check container details
docker inspect digitalme-digitalme-1

# Test image directly
docker run --rm -e ANTHROPIC_API_KEY=test your-image:latest
```

**Common Causes & Solutions:**
```bash
# Missing API key
grep ANTHROPIC_API_KEY .env

# Invalid JWT key (less than 32 characters)
echo "JWT_KEY=$(openssl rand -hex 32)" >> .env

# Database permission issues
docker exec digitalme-digitalme-1 chown -R digitalme:digitalme /app/data
```

**2. Health Checks Failing**

**Symptoms:**
- Container shows "unhealthy" status
- `/health` endpoint returns errors

**Diagnostics:**
```bash
# Check health endpoint directly
curl -v http://localhost/health

# Check application logs
docker-compose logs -f digitalme

# Test database connectivity
docker exec digitalme-digitalme-1 sqlite3 /app/data/digitalme.db ".databases"
```

**Common Solutions:**
```bash
# Database not found
docker exec digitalme-digitalme-1 ls -la /app/data/

# Claude API key issues
curl -H "x-api-key: your-key" https://api.anthropic.com/v1/messages

# Network connectivity
docker exec digitalme-digitalme-1 ping google.com
```

**3. SSL/Certificate Issues**

**Symptoms:**
- HTTPS not working
- Certificate errors in browser
- SSL handshake failures

**Diagnostics:**
```bash
# Check certificate files
ls -la certs/

# Verify certificate validity
openssl x509 -in certs/aspnetapp.crt -text -noout

# Test SSL connection
openssl s_client -connect localhost:443 -servername localhost
```

**Common Solutions:**
```bash
# Regenerate self-signed certificate
openssl req -x509 -newkey rsa:4096 -keyout certs/aspnetapp.key -out certs/aspnetapp.crt -days 365 -nodes -subj "/CN=localhost"

# Fix certificate permissions
chmod 600 certs/*

# Update Let's Encrypt certificate
sudo certbot renew
```

**4. Performance Issues**

**Symptoms:**
- Slow response times
- High memory usage
- Container restarts frequently

**Diagnostics:**
```bash
# Check resource usage
docker stats digitalme-digitalme-1

# Monitor system resources
top
free -h
df -h

# Check application logs for errors
docker-compose logs | grep -i "error\|exception\|timeout"
```

**Performance Optimizations:**
```bash
# Increase container memory limit
docker-compose -f docker-compose.production.yml up -d --scale digitalme=1 --memory 4g

# Enable production optimizations in appsettings.Production.json
{
  "RuntimeOptimizations": {
    "GCServer": true,
    "GCConcurrent": true,
    "ThreadPoolMinWorkerThreads": 50
  }
}
```

### Log Analysis

**Important Log Patterns to Monitor:**
```bash
# Application startup
docker-compose logs | grep "Application started"

# Authentication failures
docker-compose logs | grep "Unauthorized"

# API errors
docker-compose logs | grep "Claude API"

# Database errors
docker-compose logs | grep -i "database\|sqlite"

# Performance warnings
docker-compose logs | grep -i "timeout\|slow"
```

**Log Rotation:**
```bash
# Configure Docker log rotation
echo '{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  }
}' | sudo tee /etc/docker/daemon.json

sudo systemctl restart docker
```

---

## 📈 Performance Optimization

### Production Configuration Tuning

**Kestrel Web Server Optimization:**
```json
{
  "Kestrel": {
    "Limits": {
      "MaxConcurrentConnections": 1000,
      "MaxConcurrentUpgradedConnections": 100,
      "MaxRequestBodySize": 30000000,
      "KeepAliveTimeout": "00:02:00",
      "RequestHeadersTimeout": "00:00:30"
    }
  }
}
```

**Runtime Optimizations:**
```json
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

### Docker Resource Limits

**Memory and CPU Limits:**
```yaml
services:
  digitalme:
    deploy:
      resources:
        limits:
          memory: 4G
          cpus: '2.0'
        reservations:
          memory: 2G
          cpus: '1.0'
```

### Database Optimization

**SQLite Optimization:**
```bash
# Database optimization commands (run periodically)
docker exec digitalme-digitalme-1 sqlite3 /app/data/digitalme.db "VACUUM;"
docker exec digitalme-digitalme-1 sqlite3 /app/data/digitalme.db "REINDEX;"
```

---

## 🔄 Update Procedures

### Application Updates

**Rolling Update Process:**
```bash
# 1. Create backup
docker exec digitalme-digitalme-1 cp /app/data/digitalme.db /app/backups/pre-update-$(date +%Y%m%d).db

# 2. Pull latest code
git pull origin main

# 3. Build new image
docker-compose -f docker-compose.production.yml build --no-cache

# 4. Update with minimal downtime
docker-compose -f docker-compose.production.yml up -d --no-deps digitalme

# 5. Verify update
curl -f http://localhost/health
curl http://localhost/info | jq '.version'
```

**Zero-Downtime Update (with Load Balancer):**
```bash
# Scale up to 2 instances
docker-compose -f docker-compose.production.yml up -d --scale digitalme=2

# Update one instance at a time
docker-compose -f docker-compose.production.yml stop digitalme_1
docker-compose -f docker-compose.production.yml up -d --no-deps digitalme_1

# Verify and update second instance
curl -f http://localhost/health
docker-compose -f docker-compose.production.yml stop digitalme_2
docker-compose -f docker-compose.production.yml up -d --no-deps digitalme_2

# Scale back to 1 instance
docker-compose -f docker-compose.production.yml up -d --scale digitalme=1
```

### Database Migrations

**Manual Migration Process:**
```bash
# Stop application
docker-compose -f docker-compose.production.yml stop digitalme

# Run migration
docker run --rm -v digitalme_digitalme-data:/data your-image:latest dotnet ef database update

# Start application
docker-compose -f docker-compose.production.yml start digitalme
```

---

## 📊 Production Checklist

### Pre-Deployment Checklist

**Security:**
- [ ] Strong JWT key configured (32+ characters)
- [ ] All API keys configured via environment variables
- [ ] SSL certificates installed and valid
- [ ] Firewall configured properly
- [ ] Database file permissions set correctly
- [ ] No secrets committed to version control

**Configuration:**
- [ ] Production environment variables set
- [ ] Database connection string configured
- [ ] Logging level set to Warning/Error for production
- [ ] Health check endpoints accessible
- [ ] Rate limiting configured
- [ ] CORS policy configured for production domains

**Infrastructure:**
- [ ] Sufficient server resources (2GB RAM minimum)
- [ ] Docker and Docker Compose installed
- [ ] Backup directory configured
- [ ] Log rotation configured
- [ ] Monitoring solution in place

### Post-Deployment Verification

**Functional Testing:**
- [ ] Application starts successfully
- [ ] Health endpoints return 200 OK
- [ ] Database connectivity verified
- [ ] Claude API integration working
- [ ] Authentication flow functional
- [ ] External service integrations operational (if configured)

**Performance Testing:**
- [ ] Response times under acceptable limits (<2s)
- [ ] Memory usage within expected ranges (<2GB)
- [ ] No memory leaks after 24h operation
- [ ] SSL handshake performance acceptable

**Security Verification:**
- [ ] HTTPS enforced (HTTP redirects to HTTPS)
- [ ] Security headers present
- [ ] Rate limiting active
- [ ] No sensitive information in logs
- [ ] Backup procedures tested

---

## 🎯 Success Metrics

### Key Performance Indicators

**Availability Metrics:**
- **Uptime Target:** 99.9% (8.76 hours downtime per year)
- **Health Check Success Rate:** >99.5%
- **SSL Certificate Validity:** Always valid (30+ days remaining)

**Performance Metrics:**
- **Response Time:** <2 seconds for 95% of requests
- **Memory Usage:** <2GB steady state
- **CPU Usage:** <70% average load
- **Database Size:** Monitor growth, plan scaling at 1GB

**Security Metrics:**
- **Failed Authentication Attempts:** <10 per hour
- **Rate Limit Triggers:** <1% of total requests
- **SSL Grade:** A+ rating (use SSL Labs test)
- **Security Headers:** All recommended headers present

### Monitoring Dashboard

**Essential Monitoring Points:**
```bash
# Application health
curl -s http://localhost/health | jq '.status'

# System resources
docker stats --no-stream digitalme-digitalme-1

# Database size
docker exec digitalme-digitalme-1 du -h /app/data/digitalme.db

# Backup status
docker exec digitalme-digitalme-1 ls -la /app/backups/ | tail -1
```

---

## 📞 Support & Escalation

### Support Contacts

**Technical Support:**
- **System Administrator:** `admin@your-domain.com`
- **On-Call Engineer:** `+1-xxx-xxx-xxxx`
- **Emergency Escalation:** `emergency@your-domain.com`

### Issue Escalation Matrix

| **Severity** | **Response Time** | **Resolution Time** | **Escalation** |
|--------------|-------------------|---------------------|----------------|
| **Critical** | 15 minutes | 2 hours | Immediate |
| **High** | 1 hour | 8 hours | 4 hours |
| **Medium** | 4 hours | 24 hours | 8 hours |
| **Low** | 24 hours | 72 hours | 48 hours |

### Emergency Procedures

**Critical System Failure:**
1. **Immediate Actions:**
   ```bash
   # Check system status
   docker-compose -f docker-compose.production.yml ps
   
   # Restart services
   docker-compose -f docker-compose.production.yml restart
   
   # Check logs
   docker-compose -f docker-compose.production.yml logs --tail=100
   ```

2. **Escalation:**
   - Notify on-call engineer
   - Create incident ticket
   - Document all actions taken

3. **Recovery:**
   - Restore from latest backup if needed
   - Validate system functionality
   - Post-incident review

---

## 📚 Additional Resources

### Documentation References

- **Main Documentation:** `docs/operations/README.md`
- **Backup & Recovery:** `docs/operations/BACKUP_RECOVERY_GUIDE.md`
- **Security Audit:** `docs/security/DigitalMe-Security-Audit-Report-2025-09-07.md`
- **API Documentation:** Available at `/swagger` endpoint

### External Resources

- **Docker Documentation:** https://docs.docker.com/
- **Let's Encrypt:** https://letsencrypt.org/getting-started/
- **Anthropic API:** https://docs.anthropic.com/claude/reference
- **ASP.NET Core Production:** https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/

### Training Materials

- **Docker for Beginners:** https://docker-curriculum.com/
- **SSL Certificate Management:** https://www.ssllabs.com/projects/best-practices/
- **ASP.NET Core Security:** https://docs.microsoft.com/en-us/aspnet/core/security/

---

**End of Production Deployment Guide**

*This guide provides comprehensive deployment procedures for DigitalMe production environments. For questions or issues not covered in this guide, contact the technical support team.*