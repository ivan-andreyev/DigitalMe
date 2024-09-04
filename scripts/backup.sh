#!/bin/bash

# DigitalMe Backup Script
# Usage: ./scripts/backup.sh [backup-directory]

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
BACKUP_DIR="${1:-$PROJECT_DIR/backups}"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_NAME="digitalme_backup_$TIMESTAMP"

echo "💾 DigitalMe Backup Script"
echo "=========================="
echo "Backup directory: $BACKUP_DIR"
echo "Backup name: $BACKUP_NAME"
echo ""

# Create backup directory
mkdir -p "$BACKUP_DIR"

# Navigate to project directory
cd "$PROJECT_DIR"

# Create backup directory for this backup
FULL_BACKUP_PATH="$BACKUP_DIR/$BACKUP_NAME"
mkdir -p "$FULL_BACKUP_PATH"

echo "📋 Backing up database..."
# Backup SQLite database
if docker-compose ps | grep -q digitalme-api; then
    docker cp $(docker-compose ps -q digitalme-api):/app/data/digitalme.db "$FULL_BACKUP_PATH/digitalme.db"
    echo "✅ Database backed up"
else
    echo "⚠️  Warning: digitalme-api container not running, copying from volume..."
    # Try to backup from mounted volume if container is down
    if [ -f "data/digitalme.db" ]; then
        cp data/digitalme.db "$FULL_BACKUP_PATH/digitalme.db"
        echo "✅ Database backed up from volume"
    else
        echo "❌ Database file not found"
    fi
fi

echo "📂 Backing up configuration files..."
# Backup configuration files
cp .env "$FULL_BACKUP_PATH/.env" 2>/dev/null || echo "⚠️  .env file not found"
cp docker-compose.yml "$FULL_BACKUP_PATH/docker-compose.yml"
cp -r nginx "$FULL_BACKUP_PATH/nginx" 2>/dev/null || echo "⚠️  nginx directory not found"

echo "🔒 Backing up SSL certificates..."
# Backup SSL certificates
if [ -d "certs" ]; then
    cp -r certs "$FULL_BACKUP_PATH/certs"
    echo "✅ SSL certificates backed up"
else
    echo "⚠️  No SSL certificates found"
fi

echo "📊 Backing up logs..."
# Backup logs if they exist
if [ -d "nginx/logs" ]; then
    mkdir -p "$FULL_BACKUP_PATH/logs"
    cp -r nginx/logs/* "$FULL_BACKUP_PATH/logs/" 2>/dev/null || echo "⚠️  No nginx logs found"
fi

# Backup application logs
if docker-compose ps | grep -q digitalme-api; then
    mkdir -p "$FULL_BACKUP_PATH/app-logs"
    docker logs $(docker-compose ps -q digitalme-api) > "$FULL_BACKUP_PATH/app-logs/digitalme-api.log" 2>&1 || echo "⚠️  Could not backup API logs"
    docker logs $(docker-compose ps -q digitalme-web) > "$FULL_BACKUP_PATH/app-logs/digitalme-web.log" 2>&1 || echo "⚠️  Could not backup Web logs"
fi

echo "📦 Creating compressed archive..."
# Create compressed archive
cd "$BACKUP_DIR"
tar -czf "$BACKUP_NAME.tar.gz" "$BACKUP_NAME"
rm -rf "$BACKUP_NAME"

echo "🧹 Cleaning up old backups..."
# Keep only last 7 backups
ls -t digitalme_backup_*.tar.gz 2>/dev/null | tail -n +8 | xargs rm -f

echo ""
echo "✅ Backup completed successfully!"
echo "================================"
echo "📁 Backup file: $BACKUP_DIR/$BACKUP_NAME.tar.gz"
echo "📊 Backup size: $(du -h "$BACKUP_DIR/$BACKUP_NAME.tar.gz" | cut -f1)"
echo ""
echo "📋 Backup contents:"
echo "- digitalme.db (SQLite database)"
echo "- .env (configuration)"
echo "- docker-compose.yml (deployment config)"
echo "- certs/ (SSL certificates)"
echo "- nginx/ (nginx configuration)"
echo "- logs/ (application logs)"
echo ""
echo "🔄 To restore this backup:"
echo "  ./scripts/restore.sh $BACKUP_DIR/$BACKUP_NAME.tar.gz"