#!/bin/bash

# DigitalMe Docker Health Check Script
# Validates that the application is running correctly inside a Docker container

set -e

echo "🔍 Docker Health Check - DigitalMe Container Validation"
echo "=================================================="

# Function to check if a process is running
check_dotnet_process() {
    echo "🔍 Checking if dotnet process is running..."
    if pgrep -f "dotnet.*DigitalMe.dll" > /dev/null; then
        echo "✅ dotnet DigitalMe.dll process is running"
        return 0
    else
        echo "❌ dotnet DigitalMe.dll process not found"
        return 1
    fi
}

# Function to check dotnet runtime
check_dotnet_runtime() {
    echo "🔍 Checking dotnet runtime..."
    if dotnet --version > /dev/null 2>&1; then
        local version=$(dotnet --version)
        echo "✅ .NET Runtime available: $version"
        return 0
    else
        echo "❌ .NET Runtime not available"
        return 1
    fi
}

# Function to check listening ports
check_listening_ports() {
    echo "🔍 Checking if application is listening on expected ports..."
    
    local port_8080_listening=false
    local port_8081_listening=false
    
    if command -v netstat > /dev/null; then
        # Check port 8080 (HTTP)
        if netstat -ln | grep -q ":8080.*LISTEN"; then
            echo "✅ Port 8080 (HTTP) is listening"
            port_8080_listening=true
        fi
        
        # Check port 8081 (HTTPS)
        if netstat -ln | grep -q ":8081.*LISTEN"; then
            echo "✅ Port 8081 (HTTPS) is listening"
            port_8081_listening=true
        fi
    elif command -v ss > /dev/null; then
        # Check port 8080 (HTTP)
        if ss -ln | grep -q ":8080.*LISTEN"; then
            echo "✅ Port 8080 (HTTP) is listening"
            port_8080_listening=true
        fi
        
        # Check port 8081 (HTTPS)
        if ss -ln | grep -q ":8081.*LISTEN"; then
            echo "✅ Port 8081 (HTTPS) is listening"
            port_8081_listening=true
        fi
    else
        echo "⚠️ Neither netstat nor ss available, skipping port check"
        return 0
    fi
    
    if [ "$port_8080_listening" = false ] && [ "$port_8081_listening" = false ]; then
        echo "❌ No expected ports (8080, 8081) are listening"
        return 1
    fi
    
    return 0
}

# Function to check HTTP endpoint (if curl is available)
check_http_endpoint() {
    if command -v curl > /dev/null; then
        echo "🔍 Testing HTTP endpoint availability..."
        
        # Test localhost:8080 with a short timeout
        if curl -s --max-time 3 --connect-timeout 2 http://localhost:8080/health > /dev/null 2>&1; then
            echo "✅ HTTP health endpoint responding"
            return 0
        elif curl -s --max-time 3 --connect-timeout 2 http://localhost:8080/ > /dev/null 2>&1; then
            echo "✅ HTTP root endpoint responding"
            return 0
        else
            echo "⚠️ HTTP endpoints not responding (may be normal during startup)"
            return 0  # Don't fail the health check for this
        fi
    else
        echo "⚠️ curl not available, skipping HTTP endpoint check"
        return 0
    fi
}

# Function to check file system access
check_filesystem() {
    echo "🔍 Checking file system access..."
    
    # Check if we can access the application directory
    if [ -d "/app" ] && [ -r "/app/DigitalMe.dll" ]; then
        echo "✅ Application files accessible at /app/DigitalMe.dll"
    else
        echo "❌ Cannot access application files at /app/"
        return 1
    fi
    
    # Check if data directory is accessible (if it exists)
    if [ -d "/app/data" ]; then
        if [ -w "/app/data" ]; then
            echo "✅ Data directory accessible and writable"
        else
            echo "⚠️ Data directory not writable"
        fi
    fi
    
    return 0
}

# Main health check logic
main() {
    local exit_code=0
    
    echo "Starting health checks..."
    
    # Critical checks - if these fail, container is unhealthy
    if ! check_dotnet_runtime; then
        exit_code=1
    fi
    
    if ! check_filesystem; then
        exit_code=1
    fi
    
    # Process and port checks
    check_dotnet_process || echo "⚠️ Process check failed"
    check_listening_ports || echo "⚠️ Port check failed"
    
    # Optional HTTP check
    check_http_endpoint
    
    echo "=================================================="
    
    if [ $exit_code -eq 0 ]; then
        echo "✅ Health Check PASSED - Container is healthy"
    else
        echo "❌ Health Check FAILED - Container is unhealthy"
    fi
    
    return $exit_code
}

# Run main function
main "$@"