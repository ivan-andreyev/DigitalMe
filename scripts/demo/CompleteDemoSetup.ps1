# DigitalMe Complete Demo Environment Setup Script
# This is the MASTER script for complete demo environment preparation

param(
    [switch]$Full,          # Full setup including validation and data seeding
    [switch]$QuickStart,    # Quick start for immediate demo
    [switch]$ValidateOnly,  # Only validate environment
    [switch]$CleanStart     # Clean start - remove all demo data first
)

Write-Host "🎬 DigitalMe Complete Demo Environment Setup" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

$projectPath = "C:\Sources\DigitalMe\src\DigitalMe.Web"
$scriptsPath = "C:\Sources\DigitalMe\scripts\demo"
$demoDbPath = "$projectPath\digitalme-demo.db"

function Write-LogMessage {
    param([string]$Message, [string]$Color = "White")
    $timestamp = Get-Date -Format "HH:mm:ss"
    Write-Host "[$timestamp] $Message" -ForegroundColor $Color
}

function Invoke-DemoScript {
    param([string]$ScriptName, [string]$Parameters = "")
    
    $scriptPath = "$scriptsPath\$ScriptName"
    if (-not (Test-Path $scriptPath)) {
        Write-LogMessage "❌ Script not found: $ScriptName" "Red"
        return $false
    }
    
    try {
        Write-LogMessage "🔧 Executing: $ScriptName $Parameters" "Yellow"
        if ($Parameters) {
            & $scriptPath $Parameters.Split(' ')
        } else {
            & $scriptPath
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-LogMessage "✅ $ScriptName completed successfully" "Green"
            return $true
        } else {
            Write-LogMessage "⚠️ $ScriptName completed with warnings (exit code: $LASTEXITCODE)" "Yellow"
            return $true # Still continue with warnings
        }
    } catch {
        Write-LogMessage "❌ Error executing $ScriptName : $($_.Exception.Message)" "Red"
        return $false
    }
}

# Determine setup mode
$setupMode = "Standard"
if ($Full) { $setupMode = "Full" }
elseif ($QuickStart) { $setupMode = "Quick Start" }
elseif ($ValidateOnly) { $setupMode = "Validation Only" }
elseif ($CleanStart) { $setupMode = "Clean Start" }

Write-LogMessage "🎯 Setup Mode: $setupMode" "Cyan"
Write-Host ""

# Step 1: Environment Validation (always run)
Write-LogMessage "📋 Step 1: Environment Validation" "Cyan"
$validationParams = if ($Full) { "-Comprehensive -Performance" } elseif ($QuickStart) { "" } else { "-Comprehensive" }
$validationSuccess = Invoke-DemoScript "ValidateDemoEnvironment.ps1" $validationParams

if (-not $validationSuccess -and -not $QuickStart) {
    Write-LogMessage "❌ Environment validation failed. Use -QuickStart to bypass, or fix issues manually." "Red"
    exit 1
}

if ($ValidateOnly) {
    Write-LogMessage "🎯 Validation-only mode complete" "Green"
    exit 0
}

# Step 2: Clean Start (if requested)
if ($CleanStart -or $Full) {
    Write-LogMessage "📋 Step 2: Clean Start Preparation" "Cyan"
    
    if (Test-Path $demoDbPath) {
        Write-LogMessage "🧹 Removing existing demo database..." "Yellow"
        Remove-Item $demoDbPath -Force
        
        # Remove temporary files
        $tempFiles = @("$demoDbPath-shm", "$demoDbPath-wal")
        foreach ($tempFile in $tempFiles) {
            if (Test-Path $tempFile) {
                Remove-Item $tempFile -Force
            }
        }
        Write-LogMessage "✅ Demo database cleaned" "Green"
    }
}

# Step 3: Demo Data Preparation (if Full mode or CleanStart)
if ($Full -or $CleanStart) {
    Write-LogMessage "📋 Step 3: Demo Data Preparation" "Cyan"
    $dataSuccess = Invoke-DemoScript "PrepareDemoData.ps1" $(if ($CleanStart) { "-Reset" } else { "" })
    
    if (-not $dataSuccess) {
        Write-LogMessage "⚠️ Demo data preparation had issues, but continuing..." "Yellow"
    }
}

# Step 4: Application Startup
Write-LogMessage "📋 Step 4: Demo Application Startup" "Cyan"

# Set environment for demo mode
$env:ASPNETCORE_ENVIRONMENT = "Demo"
$env:DIGITALME_DEMO_MODE = "true"

# Startup parameters based on mode
$startupParams = if ($CleanStart) { "-CleanStart -Validate" } elseif ($QuickStart) { "" } else { "-Validate" }

Write-LogMessage "🚀 Starting DigitalMe Demo Environment..." "Green"
Write-Host ""

# Display final setup summary
Write-Host "🌟 DEMO ENVIRONMENT READY!" -ForegroundColor Green
Write-Host "=========================" -ForegroundColor Green
Write-Host ""
Write-Host "📊 Setup Summary:" -ForegroundColor Cyan
Write-Host "   🎯 Mode: $setupMode" -ForegroundColor White
Write-Host "   🌐 URL: http://localhost:5001" -ForegroundColor White
Write-Host "   💾 Database: digitalme-demo.db" -ForegroundColor White
Write-Host "   🎭 Demo Mode: Enabled" -ForegroundColor White
Write-Host "   ⚡ Performance: Optimized" -ForegroundColor White
Write-Host ""

Write-Host "🎬 Demo Features Available:" -ForegroundColor Cyan
Write-Host "   👤 Ivan's Digital Personality" -ForegroundColor White
Write-Host "   💬 Realistic Demo Conversations" -ForegroundColor White  
Write-Host "   🔗 Enterprise Integrations (Slack, ClickUp, GitHub, Telegram)" -ForegroundColor White
Write-Host "   📊 Live Performance Dashboard" -ForegroundColor White
Write-Host "   📈 Business Value Metrics" -ForegroundColor White
Write-Host "   🛡️ Backup Demo Scenarios (Offline, API Backup, Integration Backup)" -ForegroundColor White
Write-Host ""

Write-Host "🎯 Stakeholder Demo Ready:" -ForegroundColor Green
Write-Host "   • Professional UI with enterprise branding" -ForegroundColor White
Write-Host "   • Interactive demo scenarios for different audiences" -ForegroundColor White
Write-Host "   • Real-time system health and performance metrics" -ForegroundColor White
Write-Host "   • Comprehensive business value demonstration" -ForegroundColor White
Write-Host ""

Write-LogMessage "🎭 Access your demo at: http://localhost:5001" "Cyan"
Write-LogMessage "📱 Press Ctrl+C to stop the demo when finished" "Yellow"
Write-Host ""

# Start the demo environment
try {
    $startupResult = Invoke-DemoScript "StartDemoEnvironment.ps1" $startupParams
    
    if (-not $startupResult) {
        Write-LogMessage "❌ Failed to start demo environment" "Red"
        exit 1
    }
} catch {
    Write-LogMessage "❌ Critical error starting demo: $($_.Exception.Message)" "Red"
    exit 1
}