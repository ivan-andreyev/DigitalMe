# 🌥️ Google Cloud Run Deployment Guide

Complete guide for deploying DigitalMe to Google Cloud Run with serverless auto-scaling.

## 🎯 Why Cloud Run?

- **💰 Pay-per-use**: Only pay when requests are processed (~$10-50/month)
- **🚀 Auto-scaling**: 0 to 1000 instances automatically
- **🔧 Fully managed**: No infrastructure management
- **🌍 Global**: Deploy to any Google Cloud region
- **🔒 Secure**: Built-in HTTPS and IAM integration

## 📋 Prerequisites

1. **Google Cloud Account** with billing enabled
2. **Google Cloud SDK** installed locally
3. **Docker** installed locally
4. **Git repository** with DigitalMe code

### Install Google Cloud SDK
```bash
# Linux/macOS
curl https://sdk.cloud.google.com | bash
exec -l $SHELL

# Windows (PowerShell)
(New-Object Net.WebClient).DownloadFile("https://dl.google.com/dl/cloudsdk/channels/rapid/GoogleCloudSDKInstaller.exe", "$env:Temp\GoogleCloudSDKInstaller.exe")
& $env:Temp\GoogleCloudSDKInstaller.exe
```

## 🚀 Quick Deployment

### Option 1: One-Command Deploy (Recommended)

```bash
# 1. Clone your repository
git clone <your-digitalme-repo> digitalme-cloudrun
cd digitalme-cloudrun

# 2. Deploy to Cloud Run
./scripts/deploy-cloudrun.sh YOUR_PROJECT_ID us-central1

# Script will prompt for:
# - Anthropic API key (required for Digital Clone)
# - GitHub token (optional)
# - Telegram bot token (optional)
```

### Option 2: Manual Step-by-Step

```bash
# 1. Authenticate with Google Cloud
gcloud auth login
gcloud auth configure-docker

# 2. Set project
gcloud config set project YOUR_PROJECT_ID

# 3. Enable APIs
gcloud services enable cloudbuild.googleapis.com run.googleapis.com containerregistry.googleapis.com secretmanager.googleapis.com

# 4. Create secrets
echo -n "YOUR_ANTHROPIC_API_KEY" | gcloud secrets create anthropic-api-key --data-file=-
echo -n "YOUR_GITHUB_TOKEN" | gcloud secrets create github-token --data-file=-
echo -n "YOUR_TELEGRAM_TOKEN" | gcloud secrets create telegram-bot-token --data-file=-

# 5. Build and deploy
gcloud builds submit --config=cloudbuild.yaml .
```

## 🔐 Security Configuration

### API Keys Management

Cloud Run uses Google Secret Manager for secure API key storage:

```bash
# Update Anthropic API key
echo -n "sk-ant-new-key-here" | gcloud secrets versions add anthropic-api-key --data-file=-

# Update GitHub token  
echo -n "ghp_new-token-here" | gcloud secrets versions add github-token --data-file=-

# Update Telegram bot token
echo -n "new-bot-token-here" | gcloud secrets versions add telegram-bot-token --data-file=-
```

### Service Permissions

The services are configured with minimal required permissions:
- Backend API: Access to Secret Manager for API keys
- Web Frontend: No special permissions needed
- Both services: Allow unauthenticated (public access)

## 🌐 Custom Domain Setup

### Option 1: Cloud Run Domain Mapping

```bash
# Map custom domain to your services
gcloud run domain-mappings create --service=digitalme-web --domain=your-domain.com --region=us-central1

# Follow the instructions to verify domain ownership
```

### Option 2: Load Balancer + Custom Domain

```bash
# Create external load balancer (more advanced setup)
gcloud compute addresses create digitalme-ip --global
gcloud compute ssl-certificates create digitalme-ssl --domains=your-domain.com
```

## 📊 Monitoring & Logging

### View Logs
```bash
# API service logs
gcloud logs tail "projects/YOUR_PROJECT_ID/logs/run.googleapis.com%2Fstdout" --filter="resource.labels.service_name=digitalme-api"

# Web service logs  
gcloud logs tail "projects/YOUR_PROJECT_ID/logs/run.googleapis.com%2Fstdout" --filter="resource.labels.service_name=digitalme-web"
```

### Monitoring Dashboard
```bash
# Open Cloud Console monitoring
echo "https://console.cloud.google.com/run/detail/us-central1/digitalme-api/metrics?project=YOUR_PROJECT_ID"
```

### Alerts Setup
```bash
# Create alerting policy for high error rate
gcloud alpha monitoring policies create --policy-from-file=monitoring/error-rate-policy.yaml
```

## 💰 Cost Optimization

### Resource Configuration

**Backend API:**
- Memory: 512Mi (can reduce to 256Mi for lower traffic)
- CPU: 1 vCPU (auto-scales based on requests)
- Max instances: 10 (adjust based on expected traffic)

**Web Frontend:**
- Memory: 256Mi (sufficient for Blazor)
- CPU: 1 vCPU
- Max instances: 5

### Cost Estimates

| Traffic Level | Monthly Cost | Notes |
|---------------|--------------|-------|
| **Low** (1K requests/month) | $5-10 | Mostly free tier |
| **Medium** (100K requests/month) | $15-30 | Light commercial use |
| **High** (1M requests/month) | $40-80 | Heavy commercial use |

### Cost Monitoring
```bash
# Set up budget alerts
gcloud billing budgets create --billing-account=YOUR_BILLING_ACCOUNT --display-name="DigitalMe Budget" --budget-amount=50USD
```

## 🔄 CI/CD Setup

### GitHub Actions Integration

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Cloud Run

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - uses: google-github-actions/setup-gcloud@v1
      with:
        service_account_key: ${{ secrets.GCP_SA_KEY }}
        project_id: ${{ secrets.GCP_PROJECT_ID }}
    
    - run: gcloud builds submit --config=cloudbuild.yaml .
```

### Automated Testing

```bash
# Add testing step to cloudbuild.yaml
- name: 'gcr.io/cloud-builders/dotnet'
  args: ['test', 'tests/']
  id: 'run-tests'
```

## 🛠️ Updates & Maintenance

### Rolling Updates
```bash
# Deploy new version (zero-downtime)
gcloud builds submit --config=cloudbuild.yaml .

# Traffic split for gradual rollout
gcloud run services update-traffic digitalme-api --to-revisions=LATEST=50,PREVIOUS=50 --region=us-central1
```

### Rollback
```bash
# List revisions
gcloud run revisions list --service=digitalme-api --region=us-central1

# Rollback to previous version
gcloud run services update-traffic digitalme-api --to-revisions=digitalme-api-00002-abc=100 --region=us-central1
```

### Database Backup

Since SQLite runs in container storage:

```bash
# Regular backup script (run via Cloud Scheduler)
gcloud scheduler jobs create http digitalme-backup \
  --schedule="0 2 * * *" \
  --uri="https://digitalme-api-xyz.run.app/admin/backup" \
  --http-method=POST
```

## 🚨 Troubleshooting

### Common Issues

**1. Build Failures:**
```bash
# Check build logs
gcloud builds log BUILD_ID

# Common fix: Increase build timeout
gcloud builds submit --config=cloudbuild.yaml --timeout=20m .
```

**2. Service Not Starting:**
```bash
# Check service logs
gcloud logs read "projects/YOUR_PROJECT_ID/logs/run.googleapis.com%2Fstdout" --filter="resource.labels.service_name=digitalme-api" --limit=50

# Common fix: Check Dockerfile port configuration
```

**3. Database Issues:**
```bash
# Cloud Run containers are stateless - database will reset on restart
# Solution: Use Cloud SQL or external database for persistence
```

### Health Checks

```bash
# Test API health
curl -f "https://digitalme-api-xyz.run.app/health"

# Test Web application
curl -f "https://digitalme-web-xyz.run.app"
```

## 🎯 Production Checklist

- [ ] ✅ **API Keys Configured**: Anthropic API key set in Secret Manager
- [ ] ✅ **Custom Domain**: Domain mapped and SSL configured
- [ ] ✅ **Monitoring**: Cloud Monitoring enabled with alerts
- [ ] ✅ **Backup Strategy**: Regular database backups configured
- [ ] ✅ **CI/CD Pipeline**: Automated deployments from Git
- [ ] ✅ **Cost Monitoring**: Budget alerts configured
- [ ] ✅ **Load Testing**: Performance tested under expected load
- [ ] ✅ **Error Handling**: Proper error pages and logging

## 📚 Additional Resources

- [Cloud Run Documentation](https://cloud.google.com/run/docs)
- [Cloud Build Configuration](https://cloud.google.com/build/docs/configuring-builds/create-basic-configuration)
- [Secret Manager](https://cloud.google.com/secret-manager/docs)
- [Cloud Monitoring](https://cloud.google.com/monitoring/docs)

---

## 🎉 Success!

After deployment, your DigitalMe will be available at:

- **Backend API**: `https://digitalme-api-[hash].run.app`
- **Web App**: `https://digitalme-web-[hash].run.app`  
- **Swagger Docs**: `https://digitalme-api-[hash].run.app/swagger`

**Demo credentials:** `demo@digitalme.ai` / `Ivan2024!`

Your Digital Clone of Ivan is now running serverless on Google Cloud! 🤖☁️