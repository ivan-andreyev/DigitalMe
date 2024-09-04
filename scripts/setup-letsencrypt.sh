#!/bin/bash

# Let's Encrypt SSL Setup for DigitalMe Production
# Usage: ./scripts/setup-letsencrypt.sh your-domain.com

set -e

DOMAIN="$1"

if [ -z "$DOMAIN" ]; then
    echo "❌ Usage: $0 your-domain.com"
    exit 1
fi

echo "🌐 Setting up Let's Encrypt SSL for: $DOMAIN"
echo "=============================================="

# Check if certbot is installed
if ! command -v certbot &> /dev/null; then
    echo "📦 Installing certbot..."
    sudo apt update
    sudo apt install -y certbot
fi

# Stop nginx if running to free port 80
echo "⏸️  Stopping services to obtain certificate..."
docker-compose down nginx || true

# Obtain certificate
echo "🔒 Obtaining SSL certificate from Let's Encrypt..."
sudo certbot certonly --standalone \
    --preferred-challenges http \
    --email admin@$DOMAIN \
    --agree-tos \
    --no-eff-email \
    -d $DOMAIN

# Copy certificates to project directory
echo "📋 Copying certificates to project directory..."
sudo cp /etc/letsencrypt/live/$DOMAIN/fullchain.pem certs/aspnetapp.crt
sudo cp /etc/letsencrypt/live/$DOMAIN/privkey.pem certs/aspnetapp.key

# Create PFX for ASP.NET Core
echo "📦 Creating PFX certificate for ASP.NET Core..."
sudo openssl pkcs12 -export -out certs/aspnetapp.pfx \
    -inkey certs/aspnetapp.key \
    -in certs/aspnetapp.crt \
    -password pass:DigitalMe2024!

# Set proper ownership and permissions
sudo chown $USER:$USER certs/*
chmod 600 certs/aspnetapp.key certs/aspnetapp.pfx
chmod 644 certs/aspnetapp.crt

# Setup automatic renewal
echo "♻️  Setting up automatic certificate renewal..."
echo "0 12 * * * /usr/bin/certbot renew --quiet && docker-compose restart nginx" | sudo tee -a /etc/crontab > /dev/null

echo ""
echo "✅ Let's Encrypt SSL certificates configured successfully!"
echo "Certificate details:"
echo "==================="
openssl x509 -in certs/aspnetapp.crt -text -noout | grep -E "(Subject:|Not Before:|Not After:)"

echo ""
echo "🔄 Restarting services with SSL..."
docker-compose up -d

echo ""
echo "✨ Your DigitalMe is now secured with Let's Encrypt SSL!"
echo "   Access at: https://$DOMAIN"
echo ""
echo "📝 Automatic renewal configured via cron job."
echo "   Certificates will auto-renew 30 days before expiration."