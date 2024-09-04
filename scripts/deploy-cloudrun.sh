#!/bin/bash

# Google Cloud Run Deployment Script for DigitalMe
# Usage: ./scripts/deploy-cloudrun.sh PROJECT_ID [REGION]

set -e

PROJECT_ID="$1"
REGION="${2:-us-central1}"

if [ -z "$PROJECT_ID" ]; then
    echo "❌ Usage: $0 PROJECT_ID [REGION]"
    echo "Example: $0 my-digitalme-project us-central1"
    exit 1
fi

echo "🚀 DigitalMe Google Cloud Run Deployment"
echo "========================================"
echo "Project ID: $PROJECT_ID"
echo "Region: $REGION"
echo ""

# Check prerequisites
echo "📋 Checking prerequisites..."
command -v gcloud >/dev/null 2>&1 || { echo "❌ Google Cloud SDK is required but not installed. Exiting." >&2; exit 1; }
command -v docker >/dev/null 2>&1 || { echo "❌ Docker is required but not installed. Exiting." >&2; exit 1; }

# Set project and authenticate
echo "🔧 Setting up Google Cloud project..."
gcloud config set project $PROJECT_ID

# Enable required APIs
echo "🔌 Enabling required APIs..."
gcloud services enable cloudbuild.googleapis.com
gcloud services enable run.googleapis.com
gcloud services enable containerregistry.googleapis.com
gcloud services enable secretmanager.googleapis.com

# Create secrets (if they don't exist)
echo "🔐 Setting up secrets..."

# Check if secrets exist, create if not
if ! gcloud secrets describe anthropic-api-key >/dev/null 2>&1; then
    echo "Creating anthropic-api-key secret..."
    read -s -p "Enter your Anthropic API key: " ANTHROPIC_KEY
    echo ""
    echo -n "$ANTHROPIC_KEY" | gcloud secrets create anthropic-api-key --data-file=-
else
    echo "✅ anthropic-api-key secret already exists"
fi

if ! gcloud secrets describe github-token >/dev/null 2>&1; then
    read -s -p "Enter your GitHub token (or press Enter to skip): " GITHUB_TOKEN
    echo ""
    if [ ! -z "$GITHUB_TOKEN" ]; then
        echo -n "$GITHUB_TOKEN" | gcloud secrets create github-token --data-file=-
    else
        echo "dummy" | gcloud secrets create github-token --data-file=-
    fi
else
    echo "✅ github-token secret already exists"
fi

if ! gcloud secrets describe telegram-bot-token >/dev/null 2>&1; then
    read -s -p "Enter your Telegram bot token (or press Enter to skip): " TELEGRAM_TOKEN
    echo ""
    if [ ! -z "$TELEGRAM_TOKEN" ]; then
        echo -n "$TELEGRAM_TOKEN" | gcloud secrets create telegram-bot-token --data-file=-
    else
        echo "dummy" | gcloud secrets create telegram-bot-token --data-file=-
    fi
else
    echo "✅ telegram-bot-token secret already exists"
fi

# Build and deploy using Cloud Build
echo "🔨 Building and deploying with Cloud Build..."
gcloud builds submit --config=cloudbuild.yaml .

# Get service URLs
echo "🌐 Getting service URLs..."
API_URL=$(gcloud run services describe digitalme-api --region=$REGION --format="value(status.url)")
WEB_URL=$(gcloud run services describe digitalme-web --region=$REGION --format="value(status.url)")

# Update Web service with API URL
echo "🔗 Configuring Web service to use API URL..."
gcloud run services update digitalme-web \
  --region=$REGION \
  --set-env-vars="DigitalMe__BaseApiUrl=$API_URL,DigitalMe__SignalRUrl=$API_URL/chatHub"

echo ""
echo "🎉 Deployment completed successfully!"
echo "===================================="
echo ""
echo "🌐 Your DigitalMe services:"
echo "   Backend API:      $API_URL"
echo "   Web Application:  $WEB_URL"
echo "   API Documentation: $API_URL/swagger"
echo ""
echo "🔑 Demo Credentials:"
echo "   Email:    demo@digitalme.ai"
echo "   Password: Ivan2024!"
echo ""
echo "📊 Cloud Run Console:"
echo "   https://console.cloud.google.com/run?project=$PROJECT_ID"
echo ""
echo "💰 Expected monthly cost: ~\$10-50 (pay-per-use)"
echo ""
echo "📝 Next Steps:"
echo "1. Test your Digital Clone at: $WEB_URL"
echo "2. Configure custom domain (optional)"
echo "3. Set up monitoring and alerting"
echo "4. Update secrets with real API keys if needed"
echo ""
echo "✨ Your Digital Clone of Ivan is live on Google Cloud! 🤖"