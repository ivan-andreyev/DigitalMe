#!/bin/bash

# DigitalMe VPS Quick Deployment - One Command Deploy
# Usage: curl -sSL https://raw.githubusercontent.com/your-repo/digitalme/master/scripts/quick-deploy-vps.sh | bash

set -e

REPO_URL="https://github.com/your-username/digitalme.git"  # Update with your repo URL
DOMAIN="${1:-$(curl -s ifconfig.me)}"  # Use public IP if no domain provided
INSTALL_DIR="/opt/digitalme"

echo "🚀 DigitalMe VPS Quick Deployment"
echo "=================================="
echo "Domain/IP: $DOMAIN"
echo "Install directory: $INSTALL_DIR"
echo ""

# Check if running as root
if [ "$EUID" -eq 0 ]; then 
    echo "⚠️  Running as root. Creating digitalme user for security..."
    if ! id digitalme &>/dev/null; then
        useradd -m -s /bin/bash digitalme
        usermod -aG docker digitalme
    fi
    SUDO_CMD="sudo -u digitalme"
else
    SUDO_CMD=""
fi

# Install prerequisites
echo "📦 Installing prerequisites..."
sudo apt update
sudo apt install -y curl git docker.io docker-compose openssl

# Start Docker service
sudo systemctl start docker
sudo systemctl enable docker

# Add current user to docker group
sudo usermod -aG docker $USER

# Clone repository
echo "📥 Cloning DigitalMe repository..."
sudo rm -rf $INSTALL_DIR
sudo git clone $REPO_URL $INSTALL_DIR
sudo chown -R ${USER}:${USER} $INSTALL_DIR
cd $INSTALL_DIR

# Make scripts executable
chmod +x scripts/*.sh

# Create .env from example
cp .env.example .env

echo ""
echo "⚙️  CONFIGURATION REQUIRED"
echo "=========================="
echo "Edit the .env file to configure your API keys:"
echo ""
echo "Required for Digital Clone functionality:"
echo "  ANTHROPIC_API_KEY=sk-ant-your-key-here"
echo ""
echo "Optional external integrations:"
echo "  GITHUB_TOKEN=ghp_your-token-here"
echo "  GOOGLE_CLIENT_ID=your-google-client-id"
echo "  TELEGRAM_BOT_TOKEN=your-telegram-bot-token"
echo ""

# Interactive configuration
read -p "Do you want to configure API keys now? (y/n): " configure_now
if [ "$configure_now" = "y" ] || [ "$configure_now" = "Y" ]; then
    echo ""
    read -p "Enter your Anthropic API key (or press Enter to skip): " anthropic_key
    if [ ! -z "$anthropic_key" ]; then
        sed -i "s/ANTHROPIC_API_KEY=.*/ANTHROPIC_API_KEY=$anthropic_key/" .env
        echo "✅ Anthropic API key configured"
    fi
    
    read -p "Enter your GitHub token (or press Enter to skip): " github_token
    if [ ! -z "$github_token" ]; then
        sed -i "s/GITHUB_TOKEN=.*/GITHUB_TOKEN=$github_token/" .env
        echo "✅ GitHub token configured"
    fi
    
    read -p "Enter your Telegram bot token (or press Enter to skip): " telegram_token
    if [ ! -z "$telegram_token" ]; then
        sed -i "s/TELEGRAM_BOT_TOKEN=.*/TELEGRAM_BOT_TOKEN=$telegram_token/" .env
        echo "✅ Telegram bot token configured"
    fi
fi

# Update domain in .env
sed -i "s/DOMAIN=.*/DOMAIN=$DOMAIN/" .env

# Deploy application
echo ""
echo "🚀 Deploying DigitalMe..."
./scripts/deploy.sh $DOMAIN

# Setup firewall
echo "🔥 Configuring firewall..."
sudo ufw allow 22    # SSH
sudo ufw allow 80    # HTTP
sudo ufw allow 443   # HTTPS
sudo ufw --force enable

echo ""
echo "🎉 DigitalMe VPS deployment completed!"
echo "====================================="
echo ""
echo "🌐 Access URLs:"
echo "  Web Application: https://$DOMAIN"
echo "  API Endpoint:    https://$DOMAIN/api"
echo "  Documentation:   https://$DOMAIN/swagger"
echo ""
echo "🔑 Demo Credentials:"
echo "  Email:    demo@digitalme.ai"
echo "  Password: Ivan2024!"
echo ""
echo "📊 Management Commands:"
echo "  Status:   cd $INSTALL_DIR && docker-compose ps"
echo "  Logs:     cd $INSTALL_DIR && docker-compose logs -f"
echo "  Restart:  cd $INSTALL_DIR && docker-compose restart"
echo "  Update:   cd $INSTALL_DIR && git pull && docker-compose build && docker-compose up -d"
echo ""
echo "🔒 Security Notes:"
echo "  - Change default demo credentials in production"
echo "  - Consider setting up Let's Encrypt: ./scripts/setup-letsencrypt.sh $DOMAIN"
echo "  - Regular backups: docker cp digitalme_digitalme-api_1:/app/data/digitalme.db ./backup.db"
echo ""
echo "✨ Your Digital Clone of Ivan is ready!"
echo "   Happy chatting! 🤖"