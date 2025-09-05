# Deployment Guides & Production Setup

> **Parent Plan**: [MAIN_PLAN.md](../MAIN_PLAN.md)  
> **Section**: Deployment Information  
> **Purpose**: Comprehensive deployment procedures, production configuration, and operational guides

---

## 🚀 DEPLOYMENT OVERVIEW

### **Target Environments**
- **Development**: Local SQLite, development Claude API keys
- **Staging**: PostgreSQL, staging environment configuration
- **Production**: Cloud Run, PostgreSQL Cloud SQL, production API keys

---

## 🐳 CONTAINERIZATION

### **Dockerfile**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DigitalMe/DigitalMe.csproj", "DigitalMe/"]
RUN dotnet restore "DigitalMe/DigitalMe.csproj"
COPY . .
WORKDIR "/src/DigitalMe"
RUN dotnet build "DigitalMe.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DigitalMe.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DigitalMe.dll"]
```

### **Docker Compose (Development)**
```yaml
version: '3.8'
services:
  digitalme:
    build: .
    ports:
      - "5000:80"
      - "5001:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - CLAUDE_API_KEY=${CLAUDE_API_KEY}
      - DATABASE_URL=${DATABASE_URL}
    depends_on:
      - postgres
    
  postgres:
    image: postgres:14
    environment:
      - POSTGRES_DB=digitalme
      - POSTGRES_USER=digitalme
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

---

## ☁️ CLOUD RUN DEPLOYMENT

### **Cloud Run Configuration**
```yaml
# cloud-run.yaml
apiVersion: serving.knative.dev/v1
kind: Service
metadata:
  name: digitalme
  annotations:
    run.googleapis.com/ingress: all
spec:
  template:
    metadata:
      annotations:
        run.googleapis.com/cpu-throttling: "false"
        run.googleapis.com/memory: "512Mi"
        run.googleapis.com/execution-environment: gen2
    spec:
      containerConcurrency: 100
      containers:
      - image: gcr.io/PROJECT_ID/digitalme:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: CLAUDE_API_KEY
          valueFrom:
            secretKeyRef:
              name: claude-api-key
              key: key
        - name: DATABASE_URL
          valueFrom:
            secretKeyRef:
              name: database-url
              key: url
        resources:
          limits:
            cpu: "1"
            memory: "512Mi"
```

### **Deployment Script**
```bash
#!/bin/bash
# deploy.sh

PROJECT_ID="your-project-id"
SERVICE_NAME="digitalme"
REGION="us-central1"

# Build and push container
docker build -t gcr.io/$PROJECT_ID/$SERVICE_NAME:latest .
docker push gcr.io/$PROJECT_ID/$SERVICE_NAME:latest

# Deploy to Cloud Run
gcloud run deploy $SERVICE_NAME \
  --image gcr.io/$PROJECT_ID/$SERVICE_NAME:latest \
  --region $REGION \
  --platform managed \
  --allow-unauthenticated \
  --memory 512Mi \
  --cpu 1 \
  --min-instances 0 \
  --max-instances 10 \
  --port 8080
```

---

## 🗄️ DATABASE DEPLOYMENT

### **PostgreSQL Cloud SQL Setup**
```bash
# Create Cloud SQL instance
gcloud sql instances create digitalme-postgres \
  --database-version=POSTGRES_14 \
  --tier=db-f1-micro \
  --region=us-central1 \
  --root-password=your-root-password

# Create application database
gcloud sql databases create digitalme \
  --instance=digitalme-postgres

# Create application user
gcloud sql users create digitalme-user \
  --instance=digitalme-postgres \
  --password=your-app-password
```

### **Migration Deployment**
```bash
# Apply migrations in production
dotnet ef database update \
  --connection "Host=your-cloud-sql-ip;Database=digitalme;Username=digitalme-user;Password=your-password"

# Or via Cloud SQL Proxy
./cloud_sql_proxy -instances=PROJECT:REGION:digitalme-postgres=tcp:5432 &
dotnet ef database update
```

---

## 🔐 PRODUCTION SECRETS MANAGEMENT

### **Google Secret Manager**
```bash
# Store Claude API key
gcloud secrets create claude-api-key \
  --data-file=claude-api-key.txt

# Store database connection string  
gcloud secrets create database-url \
  --data-file=database-url.txt

# Grant Cloud Run access to secrets
gcloud projects add-iam-policy-binding PROJECT_ID \
  --member="serviceAccount:your-cloudrun-sa@PROJECT_ID.iam.gserviceaccount.com" \
  --role="roles/secretmanager.secretAccessor"
```

### **Environment-Specific Configuration**
```json
// appsettings.Production.json
{
  "Claude": {
    "ApiKey": "", // Read from environment
    "Model": "claude-3-sonnet-20240229",
    "MaxTokens": 4096,
    "RetryPolicy": {
      "MaxRetries": 5,
      "BaseDelay": "00:00:03"
    }
  },
  "Database": {
    "ConnectionString": "", // Read from environment
    "EnableRetryOnFailure": true,
    "MaxRetryCount": 5,
    "CommandTimeout": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

---

## 📊 MONITORING & OBSERVABILITY

### **Health Checks**
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddCheck<ClaudeApiHealthCheck>("claude-api")
    .AddCheck<PersonalityEngineHealthCheck>("personality-engine");

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### **Application Insights Integration**
```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry();

// Custom telemetry
public class PersonalityEngineMetrics
{
    private readonly TelemetryClient _telemetryClient;
    
    public void TrackPersonalityGeneration(TimeSpan duration, bool success)
    {
        _telemetryClient.TrackDependency("PersonalityEngine", "GenerateResponse", 
            DateTime.UtcNow.Subtract(duration), duration, success);
    }
}
```

---

## 🔧 OPERATIONAL PROCEDURES

### **Zero-Downtime Deployment**
1. **Blue-Green Deployment**: Deploy to staging environment first
2. **Health Check Validation**: Verify all health endpoints pass
3. **Database Migration**: Apply migrations with rollback plan
4. **Traffic Switching**: Gradual traffic shift to new version
5. **Monitoring**: Watch metrics during deployment

### **Backup & Recovery**
```bash
# Database backup
gcloud sql export sql digitalme-postgres \
  gs://your-backup-bucket/digitalme-backup-$(date +%Y%m%d).sql \
  --database=digitalme

# Restore from backup
gcloud sql import sql digitalme-postgres \
  gs://your-backup-bucket/digitalme-backup-20250905.sql \
  --database=digitalme
```

### **Scaling Configuration**
```yaml
# Cloud Run scaling
apiVersion: serving.knative.dev/v1
kind: Service
spec:
  template:
    metadata:
      annotations:
        # Scale up quickly under load
        run.googleapis.com/cpu-throttling: "false"
        autoscaling.knative.dev/minScale: "1"
        autoscaling.knative.dev/maxScale: "50"
        # Target 70% CPU utilization
        autoscaling.knative.dev/targetUtilizationPercentage: "70"
```

---

## 📋 PRE-PRODUCTION CHECKLIST

### **Security Checklist**
- [ ] API keys stored in Secret Manager (not environment variables)
- [ ] HTTPS enabled with proper certificates
- [ ] Database connections use SSL/TLS
- [ ] Input validation on all API endpoints
- [ ] Rate limiting configured
- [ ] CORS policies properly configured

### **Performance Checklist**
- [ ] Database indexes created for common queries
- [ ] Connection pooling configured
- [ ] Caching enabled for personality profiles
- [ ] Claude API rate limiting implemented
- [ ] Memory usage optimized
- [ ] Load testing completed

### **Reliability Checklist**
- [ ] Health checks implemented and tested
- [ ] Retry policies configured for external APIs
- [ ] Circuit breakers for Claude API calls
- [ ] Graceful degradation when Claude API unavailable
- [ ] Database migration rollback plan
- [ ] Backup and recovery procedures tested

---

## 🎯 POST-DEPLOYMENT VALIDATION

### **Smoke Tests**
```bash
# Basic health check
curl https://your-app.run.app/health

# Personality engine test
curl -X POST https://your-app.run.app/api/personality/generate \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello, how are you today?", "profileId": "ivan-profile-id"}'

# Database connectivity test
curl https://your-app.run.app/api/personality/profiles
```

### **Performance Validation**
- Response time <2s for personality-aware responses
- Throughput >100 requests/minute
- Memory usage <512MB under normal load
- CPU utilization <80% under normal load

---

**Referenced by**: [MAIN_PLAN.md](../MAIN_PLAN.md) - Deployment Information section  
**Last Updated**: 2025-09-05