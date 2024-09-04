# Debugging and Deployment Procedures

**Дата создания:** 31 августа 2025  
**Версия:** 1.0

## 🔧 Debugging Commands

### Check Cloud Run Services Status
```bash
# List all services
gcloud run services list --region=us-central1

# Get service details
gcloud run services describe digitalme-api-v2 --region=us-central1

# Check service health
curl -X GET "https://digitalme-api-v2-223874653849.us-central1.run.app/health"
```

### View Application Logs
```bash
# API logs
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=digitalme-api-v2" --limit=20

# Web logs  
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=digitalme-web" --limit=20

# Filter by error level
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=digitalme-api-v2 AND severity>=ERROR" --limit=10

# Search for specific text
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=digitalme-api-v2" --limit=30 | grep "authentication\|error\|exception" -i
```

### Authentication Testing
```bash
# Test auth validation without token (should return 401)
curl -X GET "https://digitalme-api-v2-223874653849.us-central1.run.app/api/auth/validate" -w "\nHTTP Status: %{http_code}\n"

# Test registration
curl -X POST "https://digitalme-api-v2-223874653849.us-central1.run.app/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123@","confirmPassword":"Test123@"}'

# Test with valid token (replace TOKEN with actual JWT)
curl -X GET "https://digitalme-api-v2-223874653849.us-central1.run.app/api/auth/validate" \
  -H "Authorization: Bearer TOKEN" \
  -w "\nHTTP Status: %{http_code}\n"
```

### SignalR Testing  
```bash
# Test SignalR negotiate endpoint
curl -X POST "https://digitalme-api-v2-223874653849.us-central1.run.app/chathub/negotiate" \
  -H "Content-Type: application/json" \
  -d "" \
  -w "\nHTTP Status: %{http_code}\n"
```

### Database Connectivity
```bash
# Connect to Cloud SQL (requires gcloud auth)
gcloud sql connect digitalme-db --user=digitalme --database=digitalme

# Check database from API logs
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=digitalme-api-v2" --limit=10 | grep -i "database\|sql"
```

## 🚀 Deployment Commands

### Build and Deploy API
```bash
# Build and deploy with specific commit SHA
cd C:\Sources\DigitalMe
gcloud builds submit --config=cloudbuild-api-only.yaml --substitutions=COMMIT_SHA=your-version-name

# Monitor build progress
gcloud builds list --limit=5

# Check specific build status
gcloud builds describe BUILD_ID --format="value(status)"
```

### Environment Variables Check
```bash
# Verify service environment variables
gcloud run services describe digitalme-api-v2 --region=us-central1 --format="export" | grep -E "JWT|DATABASE|ANTHROPIC"
```

### Rolling Back Deployment
```bash
# List recent revisions
gcloud run revisions list --service=digitalme-api-v2 --region=us-central1 --limit=5

# Set traffic to specific revision
gcloud run services update-traffic digitalme-api-v2 --to-revisions=REVISION_NAME=100 --region=us-central1
```

## 🔍 Common Issues & Solutions

### Authentication Issues

**Problem**: 302 Redirects instead of 401 JSON
```bash
# Check logs for redirect attempts
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=digitalme-api-v2" --limit=20 | grep "302\|redirect"

# Solution: Verify JWT Bearer events configuration in Program.cs
```

**Problem**: JWT Token Validation Fails
```bash
# Check JWT configuration
gcloud run services describe digitalme-api-v2 --region=us-central1 --format="value(spec.template.spec.template.spec.containers[0].env[].value)" | grep JWT

# Common issues:
# - JWT:Key too short (min 32 characters)  
# - Issuer/Audience mismatch
# - Token expired
```

### SignalR Connection Issues

**Problem**: 404 on /chathub/negotiate
```bash
# Check SignalR mapping in Program.cs
# Should be: app.MapHub<ChatHub>("/chathub");

# Test endpoint
curl -I "https://digitalme-api-v2-223874653849.us-central1.run.app/chathub/negotiate"
```

**Problem**: WebSocket Connection Drops
```bash
# Check logs for connection issues
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=digitalme-api-v2" --limit=30 | grep -i "websocket\|signalr\|connection"

# Common causes:
# - Cloud Run timeout settings
# - Network issues
# - Authentication problems
```

### Chat Response Issues

**Problem**: Messages Sent But No Agent Response
```bash
# Check if messages reach ChatHub
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=digitalme-api-v2" --limit=20 | grep -i "received.*message\|sendmessage"

# Check Agent Behavior Engine
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=digitalme-api-v2" --limit=20 | grep -i "agent\|anthropic"

# Check for errors
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=digitalme-api-v2 AND severity>=ERROR" --limit=10
```

### Database Issues

**Problem**: Connection String Errors
```bash
# Check database connectivity in health endpoint
curl -s "https://digitalme-api-v2-223874653849.us-central1.run.app/health" | grep -i "database\|connected"

# Common issues:
# - Cloud SQL instance not accessible
# - Wrong connection string format
# - Authentication problems
```

## 📁 Important File Locations

### Configuration Files
- `C:\Sources\DigitalMe\cloudbuild-api-only.yaml` - API deployment config
- `C:\Sources\DigitalMe\DigitalMe\Program.cs` - Main application configuration
- `C:\Sources\DigitalMe\DigitalMe\appsettings.json` - Application settings

### Key Source Files
- `C:\Sources\DigitalMe\DigitalMe\Controllers\AuthController.cs` - Authentication endpoints
- `C:\Sources\DigitalMe\DigitalMe\Hubs\ChatHub.cs` - SignalR chat hub
- `C:\Sources\DigitalMe\DigitalMe\DTOs\MessageDto.cs` - Data transfer objects

### Migration Files
- `C:\Sources\DigitalMe\DigitalMe\Data\Migrations\` - Database migrations

## 🎯 Performance Monitoring

### Key Metrics to Monitor
```bash
# Response times
curl -w "@curl-format.txt" -o /dev/null -s "https://digitalme-api-v2-223874653849.us-central1.run.app/health"

# Error rates in logs
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=digitalme-api-v2 AND severity>=ERROR" --limit=50

# Memory/CPU usage (in Cloud Console)
# Navigate to: Cloud Run → digitalme-api-v2 → Metrics
```

### curl-format.txt (for response time testing)
```
     time_namelookup:  %{time_namelookup}\n
        time_connect:  %{time_connect}\n
     time_appconnect:  %{time_appconnect}\n
    time_pretransfer:  %{time_pretransfer}\n
       time_redirect:  %{time_redirect}\n
  time_starttransfer:  %{time_starttransfer}\n
                     ----------\n
          time_total:  %{time_total}\n
```

## 🛠️ Development Workflow

1. **Make Changes** → Edit source files
2. **Test Locally** → `dotnet run` (if possible)  
3. **Deploy** → `gcloud builds submit`
4. **Monitor** → Check logs and health endpoints
5. **Verify** → Test authentication and chat functionality
6. **Rollback** → If needed, using revision management

## 📞 Emergency Procedures

### Complete Service Failure
```bash
# 1. Check service status
gcloud run services list --region=us-central1

# 2. View recent logs for errors
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=digitalme-api-v2 AND severity>=ERROR" --limit=20

# 3. Rollback to last working revision
gcloud run revisions list --service=digitalme-api-v2 --region=us-central1 --limit=5
gcloud run services update-traffic digitalme-api-v2 --to-revisions=LAST_WORKING_REVISION=100 --region=us-central1

# 4. Verify rollback
curl -X GET "https://digitalme-api-v2-223874653849.us-central1.run.app/health"
```

### Database Connection Loss
```bash
# 1. Check Cloud SQL instance status
gcloud sql instances describe digitalme-db

# 2. Check service connectivity
curl -s "https://digitalme-api-v2-223874653849.us-central1.run.app/health" | grep database

# 3. Restart Cloud SQL if needed
gcloud sql instances restart digitalme-db
```

---

**Maintainer**: Claude Code Assistant  
**Last Updated**: 31 августа 2025  
**Next Review**: При изменении инфраструктуры