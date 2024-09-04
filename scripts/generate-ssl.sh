#!/bin/bash

# SSL Certificate Generation Script for DigitalMe
# Usage: ./scripts/generate-ssl.sh [domain-name]

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
DOMAIN="${1:-localhost}"

echo "🔒 Generating SSL certificates for: $DOMAIN"
echo "============================================="

# Create certs directory
mkdir -p "$PROJECT_DIR/certs"
cd "$PROJECT_DIR/certs"

# Generate private key
echo "🔑 Generating private key..."
openssl genrsa -out aspnetapp.key 2048

# Generate certificate signing request
echo "📝 Generating certificate signing request..."
openssl req -new -key aspnetapp.key -out aspnetapp.csr -subj "/C=GE/ST=Adjara/L=Batumi/O=DigitalMe/OU=IT/CN=$DOMAIN"

# Generate self-signed certificate
echo "📜 Generating self-signed certificate..."
openssl x509 -req -in aspnetapp.csr -signkey aspnetapp.key -out aspnetapp.crt -days 365

# Generate PFX certificate for ASP.NET Core
echo "📦 Generating PFX certificate for ASP.NET Core..."
openssl pkcs12 -export -out aspnetapp.pfx -inkey aspnetapp.key -in aspnetapp.crt -password pass:DigitalMe2024!

# Set proper permissions
chmod 600 aspnetapp.key aspnetapp.pfx
chmod 644 aspnetapp.crt

echo ""
echo "✅ SSL certificates generated successfully!"
echo "Certificate details:"
echo "==================="
openssl x509 -in aspnetapp.crt -text -noout | grep -E "(Subject:|Not Before:|Not After:)"

echo ""
echo "📁 Generated files:"
echo "- aspnetapp.key (Private key)"
echo "- aspnetapp.crt (Certificate)"
echo "- aspnetapp.pfx (PKCS#12 for ASP.NET Core)"
echo ""
echo "⚠️  Note: These are self-signed certificates for development/testing."
echo "   For production, consider using Let's Encrypt: ./scripts/setup-letsencrypt.sh $DOMAIN"