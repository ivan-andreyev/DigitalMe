#!/bin/bash

# DigitalMe Quick Deployment Script
# Usage: ./scripts/deploy.sh [domain-name]

set -e  # Exit on any error

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
DOMAIN="${1:-localhost}"

echo "🚀 DigitalMe Deployment Script"
echo "==============================="
echo "Domain: $DOMAIN"
echo "Project directory: $PROJECT_DIR"
echo ""

# Check prerequisites
echo "📋 Checking prerequisites..."
command -v docker >/dev/null 2>&1 || { echo "❌ Docker is required but not installed. Exiting." >&2; exit 1; }
command -v docker-compose >/dev/null 2>&1 || { echo "❌ Docker Compose is required but not installed. Exiting." >&2; exit 1; }

# Navigate to project directory
cd "$PROJECT_DIR"

# Check if .env exists, create from template if not
if [ ! -f .env ]; then
    echo "📝 Creating .env file from template..."
    cp .env.example .env
    echo "⚠️  IMPORTANT: Edit .env file and configure your API keys before continuing!"
    echo "   Required: ANTHROPIC_API_KEY for Digital Clone functionality"
    echo "   Optional: GITHUB_TOKEN, GOOGLE_CLIENT_ID, TELEGRAM_BOT_TOKEN"
    echo ""
    read -p "Press Enter after configuring .env file..."
fi

# Source environment variables
source .env

# Validate essential configuration
echo "🔍 Validating configuration..."
if [ -z "$ANTHROPIC_API_KEY" ] || [ "$ANTHROPIC_API_KEY" = "sk-ant-your-anthropic-api-key-here" ]; then
    echo "⚠️  WARNING: ANTHROPIC_API_KEY not configured. Digital Clone will use fallback responses."
    echo "   Get your API key from: https://console.anthropic.com/"
fi

# Generate SSL certificates if they don't exist
if [ ! -f "certs/aspnetapp.crt" ]; then
    echo "🔒 Generating SSL certificates..."
    ./scripts/generate-ssl.sh "$DOMAIN"
fi

# Create necessary directories
echo "📁 Creating necessary directories..."
mkdir -p nginx/logs
mkdir -p data

# Build and deploy with Docker Compose
echo "🔨 Building Docker images..."
docker-compose build --no-cache

echo "🚀 Starting services..."
docker-compose up -d

# Wait for services to be ready
echo "⏳ Waiting for services to start..."
sleep 10

# Health check
echo "🩺 Performing health checks..."
if curl -k -f "https://$DOMAIN/health" >/dev/null 2>&1; then
    echo "✅ API service is healthy"
else
    echo "⚠️  API service health check failed (this might be expected on first run)"
fi

if curl -k -f "https://$DOMAIN" >/dev/null 2>&1; then
    echo "✅ Web application is accessible"
else
    echo "⚠️  Web application check failed (this might be expected on first run)"
fi

# Display status and access information
echo ""
echo "🎉 Deployment completed!"
echo "========================"
echo "✅ Backend API: https://$DOMAIN/api"
echo "✅ Web Application: https://$DOMAIN"
echo "✅ API Documentation: https://$DOMAIN/swagger"
echo ""
echo "🔍 Service Status:"
docker-compose ps

echo ""
echo "📊 Container Resources:"
docker stats --no-stream

echo ""
echo "📝 Next Steps:"
echo "1. Access your Digital Clone at https://$DOMAIN"
echo "2. Use demo credentials: demo@digitalme.ai / Ivan2024!"
echo "3. Check logs with: docker-compose logs -f"
echo "4. Stop services with: docker-compose down"
echo ""

# Show logs for the first few seconds
echo "📋 Recent logs (last 20 lines):"
echo "================================"
docker-compose logs --tail=20

echo ""
echo "✨ Your DigitalMe Digital Clone is ready!"
echo "   Happy chatting with Ivan! 🤖"