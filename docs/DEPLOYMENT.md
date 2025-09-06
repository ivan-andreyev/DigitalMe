# 🚀 DigitalMe Deployment Guide

Comprehensive deployment guide for DigitalMe Digital Clone system with multiple deployment options.

## 📊 System Overview

**Architecture:** Multi-container .NET 8 application
- **Backend API** (DigitalMe) - Core business logic + Claude integration  
- **Web Frontend** (DigitalMe.Web) - Blazor Server application
- **Reverse Proxy** (Nginx) - Load balancing + SSL termination
- **Database** - SQLite (file-based, no separate container needed)

## 🎯 Deployment Options

### **Option 1: VPS Deployment** ⭐ *Recommended for you*
**Best for:** Full control, cost-effective, your existing VPS

### **Option 2: Cloud Deployment** 
**Best for:** Auto-scaling, managed services, zero maintenance

### **Option 3: Local Development**
**Best for:** Testing, development, feature validation

---

## 🔧 Option 1: VPS Deployment

### **Prerequisites**
- Ubuntu 20.04+ or similar Linux distribution
- 2GB+ RAM, 20GB+ storage
- Docker and Docker Compose installed
- Domain name (optional, can use IP)

### **Quick Setup (5 minutes)**

1. **Clone repository on VPS:**
```bash
git clone <your-repo-url> digitalme
cd digitalme
```

2. **Create environment file:**
```bash
cp .env.example .env
nano .env
```

3. **Configure API keys in `.env`:**
```env
# Essential for Digital Clone functionality
ANTHROPIC_API_KEY=sk-ant-your-key-here

# Optional external integrations  
GITHUB_TOKEN=ghp_your-token-here
GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret
TELEGRAM_BOT_TOKEN=your-telegram-bot-token

# SSL Configuration
DOMAIN=your-domain.com
# or use IP if no domain: DOMAIN=your.vps.ip.address
```

4. **Generate SSL certificates:**
```bash
# Self-signed certificates (for testing)
./scripts/generate-ssl.sh

# OR Let's Encrypt (for production with domain)
./scripts/setup-letsencrypt.sh your-domain.com
```

5. **Deploy with Docker Compose:**
```bash
docker-compose up -d
```

6. **Verify deployment:**
```bash
# Check all containers are running
docker-compose ps

# Check logs
docker-compose logs -f

# Test health endpoint
curl -k https://your-domain.com/health
```

**✅ Your DigitalMe clone is now running!**
- **Web UI:** https://your-domain.com
- **API:** https://your-domain.com/api  
- **API Documentation:** https://your-domain.com/swagger

### **VPS Monitoring & Maintenance**

```bash
# View logs
docker-compose logs -f digitalme-api
docker-compose logs -f digitalme-web

# Update deployment
git pull
docker-compose build --no-cache
docker-compose up -d

# Backup database
docker cp digitalme_digitalme-api_1:/app/data/digitalme.db ./backup-$(date +%Y%m%d).db

# Resource monitoring
docker stats
df -h  # disk usage
free -m  # memory usage
```

---

## ☁️ Option 2: Cloud Deployment

### **A. Azure Container Instances** (Easiest cloud option)

1. **Install Azure CLI:**
```bash
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
az login
```

2. **Deploy to Azure:**
```bash
# Create resource group
az group create --name digitalme-rg --location eastus

# Deploy with our template
az deployment group create \
  --resource-group digitalme-rg \
  --template-file azure/digitalme-template.json \
  --parameters @azure/digitalme-parameters.json

# Get public IP
az container show --resource-group digitalme-rg --name digitalme --query ipAddress.ip --output tsv
```

### **B. Google Cloud Run** (Serverless, auto-scaling)

```bash
# Build and deploy API
gcloud builds submit --tag gcr.io/PROJECT-ID/digitalme-api DigitalMe/
gcloud run deploy digitalme-api --image gcr.io/PROJECT-ID/digitalme-api --platform managed

# Build and deploy Web
gcloud builds submit --tag gcr.io/PROJECT-ID/digitalme-web src/DigitalMe.Web/
gcloud run deploy digitalme-web --image gcr.io/PROJECT-ID/digitalme-web --platform managed
```

### **C. AWS ECS Fargate** (Managed containers)

```bash
# Use AWS CDK template
npm install -g aws-cdk
cd aws-deployment/
cdk bootstrap
cdk deploy DigitalMeStack
```

---

## 🖥️ Option 3: Local Development

### **Quick Development Setup**

1. **Install .NET 8 SDK + Docker Desktop**

2. **Clone and run locally:**
```bash
git clone <repo-url> digitalme
cd digitalme

# Start with Docker Compose
docker-compose -f docker-compose.dev.yml up -d

# OR run directly with .NET
dotnet run --project DigitalMe  # API on :5000
dotnet run --project src/DigitalMe.Web  # Web on :8080
```

3. **Access applications:**
- **API:** http://localhost:5000
- **Web:** http://localhost:8080
- **Swagger:** http://localhost:5000/swagger

---

## 🔐 Security Configuration

### **Essential Security Steps**

1. **API Keys Management:**
```bash
# Use environment variables, never commit keys to git
echo "ANTHROPIC_API_KEY=your-key" >> .env
echo ".env" >> .gitignore
```

2. **SSL/TLS Configuration:**
```bash
# Production: Use Let's Encrypt
./scripts/setup-letsencrypt.sh your-domain.com

# Development: Use self-signed
./scripts/generate-ssl.sh
```

3. **Firewall Setup (VPS):**
```bash
sudo ufw allow 22    # SSH
sudo ufw allow 80    # HTTP
sudo ufw allow 443   # HTTPS
sudo ufw enable
```

4. **Database Security:**
```bash
# SQLite file permissions
chmod 600 /app/data/digitalme.db

# Regular backups
echo "0 2 * * * docker cp digitalme_digitalme-api_1:/app/data/digitalme.db /backup/digitalme-\$(date +\%Y\%m\%d).db" | crontab -
```

---

## 📊 Configuration Examples

### **Production .env file:**
```env
# Core API Configuration
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000;https://+:5001

# Digital Clone Integration
ANTHROPIC_API_KEY=sk-ant-your-anthropic-key
ANTHROPIC_MODEL=claude-3-5-sonnet-20241022

# External Services (Optional)
GITHUB_TOKEN=ghp_your-github-token
GOOGLE_CLIENT_ID=your-google-oauth-client-id
GOOGLE_CLIENT_SECRET=your-google-oauth-secret
TELEGRAM_BOT_TOKEN=your-telegram-bot-token

# SSL Configuration
SSL_CERT_PATH=/etc/ssl/certs/aspnetapp.crt
SSL_KEY_PATH=/etc/ssl/certs/aspnetapp.key

# Database
DATABASE_PATH=/app/data/digitalme.db
```

### **Nginx Configuration for Custom Domain:**
```nginx
server {
    listen 443 ssl;
    server_name yourdomain.com;
    
    ssl_certificate /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;
    
    location / {
        proxy_pass http://digitalme-web:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

---

## 🚨 Troubleshooting

### **Common Issues & Solutions**

1. **API Key Issues:**
```bash
# Check if API key is loaded
docker exec digitalme_digitalme-api_1 env | grep ANTHROPIC

# Test API key manually
curl -H "x-api-key: your-key" https://api.anthropic.com/v1/messages
```

2. **Database Permissions:**
```bash
# Fix SQLite permissions
docker exec digitalme_digitalme-api_1 chown app:app /app/data/digitalme.db
docker exec digitalme_digitalme-api_1 chmod 644 /app/data/digitalme.db
```

3. **Memory Issues:**
```bash
# Check container memory usage
docker stats

# Restart services if needed
docker-compose restart digitalme-api
```

4. **SSL Certificate Issues:**
```bash
# Regenerate self-signed certificates
./scripts/generate-ssl.sh

# Check certificate validity
openssl x509 -in certs/aspnetapp.crt -text -noout
```

### **Health Checks**

```bash
# API Health
curl -k https://your-domain.com/health

# Web Application
curl -k https://your-domain.com

# SignalR Hub
curl -k https://your-domain.com/chatHub

# Container Status
docker-compose ps
docker-compose logs -f --tail=50
```

---

## 💡 Recommendations

### **For Your VPS Setup:**

1. **Start with VPS Option 1** - full control, cost-effective
2. **Use real domain name** - easier SSL setup with Let's Encrypt  
3. **Get Anthropic API key first** - essential for Digital Clone functionality
4. **Setup monitoring** - use the provided health checks
5. **Regular backups** - SQLite database is file-based, easy to backup

### **Scaling Considerations:**

- **Current setup:** Handles 100+ concurrent users
- **If you need more:** Move to cloud option with auto-scaling
- **Database:** SQLite → PostgreSQL for heavy loads
- **Caching:** Add Redis for session management

### **Cost Estimates:**

- **VPS Deployment:** $5-20/month (your existing VPS)
- **Azure Container:** $30-100/month  
- **Google Cloud Run:** $10-50/month (pay-per-use)
- **AWS Fargate:** $20-80/month

**🎯 Recommended: Start with VPS, migrate to cloud if you need scaling.**