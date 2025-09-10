# DigitalMe Demo Environment Startup Script
# This script prepares and starts the optimized demo environment

param(
    [switch]$CleanStart,
    [switch]$Validate,
    [switch]$BackupMode,
    [string]$Environment = "Demo"
)

Write-Host "🚀 DigitalMe Demo Environment Setup" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

$projectPath = "C:\Sources\DigitalMe\src\DigitalMe.Web"
$demoDbPath = "$projectPath\digitalme-demo.db"

# Function to log messages with timestamp
function Write-LogMessage {
    param([string]$Message, [string]$Color = "White")
    $timestamp = Get-Date -Format "HH:mm:ss"
    Write-Host "[$timestamp] $Message" -ForegroundColor $Color
}

# Clean start - remove demo database for fresh setup
if ($CleanStart) {
    Write-LogMessage "🧹 Performing clean start..." "Yellow"
    
    if (Test-Path $demoDbPath) {
        Remove-Item $demoDbPath -Force
        Write-LogMessage "✅ Demo database removed for clean setup" "Green"
    }
    
    # Remove any temporary files
    $tempFiles = @("$demoDbPath-shm", "$demoDbPath-wal")
    foreach ($tempFile in $tempFiles) {
        if (Test-Path $tempFile) {
            Remove-Item $tempFile -Force
            Write-LogMessage "✅ Temporary file removed: $(Split-Path $tempFile -Leaf)" "Green"
        }
    }
}

# Set environment variables for demo mode
Write-LogMessage "🔧 Configuring demo environment..." "Yellow"
$env:ASPNETCORE_ENVIRONMENT = $Environment
$env:DIGITALME_DEMO_MODE = "true"
$env:DIGITALME_OPTIMIZED = "true"

if ($BackupMode) {
    $env:DIGITALME_BACKUP_MODE = "true"
    Write-LogMessage "🛡️ Backup mode enabled for offline demo capability" "Yellow"
}

# Validate environment if requested
if ($Validate) {
    Write-LogMessage "🔍 Validating demo environment..." "Yellow"
    
    # Check if project exists
    if (-not (Test-Path $projectPath)) {
        Write-LogMessage "❌ Project path not found: $projectPath" "Red"
        exit 1
    }
    
    # Check if appsettings.Demo.json exists
    $demoConfig = "$projectPath\appsettings.Demo.json"
    if (-not (Test-Path $demoConfig)) {
        Write-LogMessage "❌ Demo configuration not found: $demoConfig" "Red"
        exit 1
    }
    
    Write-LogMessage "✅ Environment validation passed" "Green"
}

# Build the project in release mode for optimal performance
Write-LogMessage "🔨 Building project in Release mode for optimal demo performance..." "Yellow"
Set-Location $projectPath

try {
    $buildOutput = dotnet build --configuration Release --verbosity quiet 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-LogMessage "✅ Project built successfully" "Green"
    } else {
        Write-LogMessage "❌ Build failed: $buildOutput" "Red"
        exit 1
    }
} catch {
    Write-LogMessage "❌ Build error: $($_.Exception.Message)" "Red"
    exit 1
}

# Prepare database and seed demo data
Write-LogMessage "📚 Preparing demo database and seeding data..." "Yellow"

try {
    # Run EF migrations for demo database
    $migrateOutput = dotnet ef database update --connection "Data Source=digitalme-demo.db" 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-LogMessage "✅ Database migrations applied" "Green"
    } else {
        Write-LogMessage "⚠️ Migration warning (may be expected): $migrateOutput" "Yellow"
    }
} catch {
    Write-LogMessage "⚠️ Migration issue (proceeding anyway): $($_.Exception.Message)" "Yellow"
}

# Display demo configuration
Write-LogMessage "⚙️ Demo Configuration:" "Cyan"
Write-Host "   📍 Environment: $Environment" -ForegroundColor White
Write-Host "   🔗 URL: http://localhost:5001" -ForegroundColor White  
Write-Host "   💾 Database: digitalme-demo.db" -ForegroundColor White
Write-Host "   🎭 Demo Mode: Enabled" -ForegroundColor White
Write-Host "   ⚡ Performance: Optimized" -ForegroundColor White

if ($BackupMode) {
    Write-Host "   🛡️ Backup Mode: Enabled" -ForegroundColor White
}

Write-LogMessage "🎬 Starting DigitalMe Demo Environment..." "Green"
Write-Host ""
Write-Host "🌟 Demo Environment Ready!" -ForegroundColor Green
Write-Host "   Access your demo at: http://localhost:5001" -ForegroundColor Cyan
Write-Host "   Press Ctrl+C to stop the demo" -ForegroundColor Yellow
Write-Host ""

# Start the application
try {
    dotnet run --configuration Release --environment $Environment --urls "http://localhost:5001"
} catch {
    Write-LogMessage "❌ Failed to start application: $($_.Exception.Message)" "Red"
    exit 1
}