# Cloud SQL PostgreSQL Setup

## 1. Create Cloud SQL Instance

```bash
# Set project
gcloud config set project digitalme-470613

# Create PostgreSQL instance (small tier for MVP)
gcloud sql instances create digitalme-postgres \
  --database-version=POSTGRES_14 \
  --tier=db-f1-micro \
  --region=us-central1 \
  --storage-size=10GB \
  --storage-type=SSD \
  --backup-start-time=02:00 \
  --backup-location=us \
  --enable-bin-log \
  --maintenance-window-day=SUN \
  --maintenance-window-hour=03 \
  --maintenance-release-channel=production \
  --deletion-protection

# Create database
gcloud sql databases create digitalmedb --instance=digitalme-postgres

# Create user (replace YOUR_PASSWORD with secure password)
gcloud sql users create digitalmeuser \
  --instance=digitalme-postgres \
  --password=YOUR_PASSWORD

# Get connection name for Cloud Run
gcloud sql instances describe digitalme-postgres --format="value(connectionName)"
```

## 2. Connection String Format

For Cloud Run with Cloud SQL Proxy:
```
Host=/cloudsql/digitalme-470613:us-central1:digitalme-postgres;Database=digitalmedb;Username=digitalmeuser;Password=YOUR_PASSWORD
```

## 3. Cloud Run Environment Variables

Set in Cloud Run service:
```bash
gcloud run services update digitalme-api \
  --add-cloudsql-instances=digitalme-470613:us-central1:digitalme-postgres \
  --set-env-vars="ConnectionStrings__DefaultConnection=Host=/cloudsql/digitalme-470613:us-central1:digitalme-postgres;Database=digitalmedb;Username=digitalmeuser;Password=YOUR_PASSWORD" \
  --region=us-central1
```

## 4. Database Migration

```bash
# Run EF migrations to create tables
cd src/DigitalMe
dotnet ef database update --connection "Host=/cloudsql/digitalme-470613:us-central1:digitalme-postgres;Database=digitalmedb;Username=digitalmeuser;Password=YOUR_PASSWORD"
```

## 5. Verification

The application will automatically detect PostgreSQL connection string and use Npgsql provider instead of SQLite.