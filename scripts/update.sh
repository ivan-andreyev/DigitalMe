#!/bin/bash

# DigitalMe Update Script
# Usage: ./scripts/update.sh [--backup]

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
CREATE_BACKUP=false

# Parse command line arguments
for arg in "$@"; do
    case $arg in
        --backup)
            CREATE_BACKUP=true
            shift
            ;;
        *)
            echo "❌ Unknown argument: $arg"
            echo "Usage: $0 [--backup]"
            exit 1
            ;;
    esac
done

echo "🔄 DigitalMe Update Script"
echo "========================="
echo "Project directory: $PROJECT_DIR"
echo "Create backup: $CREATE_BACKUP"
echo ""

# Navigate to project directory
cd "$PROJECT_DIR"

# Create backup if requested
if [ "$CREATE_BACKUP" = true ]; then
    echo "💾 Creating backup before update..."
    ./scripts/backup.sh
    echo ""
fi

echo "📥 Fetching latest changes from repository..."
git fetch origin

# Check if there are updates
CURRENT_COMMIT=$(git rev-parse HEAD)
LATEST_COMMIT=$(git rev-parse origin/master)

if [ "$CURRENT_COMMIT" = "$LATEST_COMMIT" ]; then
    echo "✅ Already up to date!"
    exit 0
fi

echo "📋 Changes to be applied:"
git log --oneline $CURRENT_COMMIT..$LATEST_COMMIT

echo ""
read -p "Continue with update? (y/n): " confirm
if [ "$confirm" != "y" ] && [ "$confirm" != "Y" ]; then
    echo "❌ Update cancelled"
    exit 1
fi

echo "⬇️  Pulling latest changes..."
git pull origin master

echo "🛑 Stopping services..."
docker-compose down

echo "🔨 Rebuilding Docker images..."
docker-compose build --no-cache

echo "🧹 Cleaning up unused Docker resources..."
docker system prune -f

echo "🚀 Starting updated services..."
docker-compose up -d

echo "⏳ Waiting for services to be ready..."
sleep 15

echo "🩺 Performing health checks..."
HEALTH_CHECK_PASSED=0

if curl -k -f "http://localhost:5000/health" >/dev/null 2>&1; then
    echo "✅ API service is healthy"
    HEALTH_CHECK_PASSED=$((HEALTH_CHECK_PASSED + 1))
else
    echo "❌ API service health check failed"
fi

if curl -k -f "http://localhost:8080" >/dev/null 2>&1; then
    echo "✅ Web application is accessible"
    HEALTH_CHECK_PASSED=$((HEALTH_CHECK_PASSED + 1))
else
    echo "❌ Web application check failed"
fi

echo ""
if [ $HEALTH_CHECK_PASSED -eq 2 ]; then
    echo "🎉 Update completed successfully!"
    echo "================================"
    echo "✅ All services are running and healthy"
else
    echo "⚠️  Update completed with warnings"
    echo "================================="
    echo "❌ Some health checks failed"
    echo "📋 Check logs with: docker-compose logs -f"
fi

echo ""
echo "📊 Service Status:"
docker-compose ps

echo ""
echo "📋 Recent logs:"
docker-compose logs --tail=10

echo ""
echo "🔧 Post-update commands:"
echo "  View logs:    docker-compose logs -f"
echo "  Restart:      docker-compose restart"
echo "  Full restart: docker-compose down && docker-compose up -d"
echo ""

if [ "$CREATE_BACKUP" = true ]; then
    echo "💾 Backup was created before update"
    echo "   Location: ./backups/"
    echo "   Restore:  ./scripts/restore.sh [backup-file]"
fi