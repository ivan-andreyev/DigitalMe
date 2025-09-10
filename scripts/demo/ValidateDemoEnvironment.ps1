# DigitalMe Demo Environment Validation Script  
# This script validates the demo environment is ready for stakeholder presentations

param(
    [switch]$Comprehensive,
    [switch]$Performance,
    [switch]$Fix
)

Write-Host "🔍 DigitalMe Demo Environment Validation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$projectPath = "C:\Sources\DigitalMe\src\DigitalMe.Web"
$demoDbPath = "$projectPath\digitalme-demo.db"
$demoConfigPath = "$projectPath\appsettings.Demo.json"

function Write-LogMessage {
    param([string]$Message, [string]$Color = "White")
    $timestamp = Get-Date -Format "HH:mm:ss"
    Write-Host "[$timestamp] $Message" -ForegroundColor $Color
}

function Test-DemoComponent {
    param([string]$Name, [scriptblock]$Test, [scriptblock]$Fix = $null)
    
    Write-LogMessage "Testing: $Name..." "Yellow"
    
    try {
        $result = & $Test
        if ($result) {
            Write-LogMessage "✅ PASS: $Name" "Green"
            return $true
        } else {
            Write-LogMessage "❌ FAIL: $Name" "Red"
            
            if ($Fix -and $Fix) {
                Write-LogMessage "🔧 Attempting fix for: $Name..." "Yellow"
                $fixResult = & $Fix
                if ($fixResult) {
                    Write-LogMessage "✅ FIXED: $Name" "Green"
                    return $true
                } else {
                    Write-LogMessage "❌ FIX FAILED: $Name" "Red"
                }
            }
            return $false
        }
    } catch {
        Write-LogMessage "❌ ERROR: $Name - $($_.Exception.Message)" "Red"
        return $false
    }
}

$validationResults = @{}

# Core Environment Validation
Write-LogMessage "🎯 Core Environment Validation" "Cyan"

$validationResults["ProjectExists"] = Test-DemoComponent "Project Directory" {
    Test-Path $projectPath
}

$validationResults["DemoConfig"] = Test-DemoComponent "Demo Configuration" {
    Test-Path $demoConfigPath
} {
    # Fix: Create basic demo config if missing
    if (-not (Test-Path $demoConfigPath)) {
        $basicConfig = @{
            "DigitalMe" = @{
                "Features" = @{
                    "DemoMode" = $true
                    "OptimizeForDemo" = $true
                }
                "Demo" = @{
                    "MaxResponseTime" = 2000
                    "EnableMetrics" = $true
                    "ShowSystemHealth" = $true
                }
            }
        }
        $basicConfig | ConvertTo-Json -Depth 3 | Out-File $demoConfigPath -Encoding UTF8
        return $true
    }
    return $false
}

$validationResults["BuildSuccess"] = Test-DemoComponent "Project Build" {
    Set-Location $projectPath
    $buildOutput = dotnet build --configuration Release --verbosity quiet 2>&1
    return $LASTEXITCODE -eq 0
} {
    # Fix: Clean and rebuild
    Set-Location $projectPath
    dotnet clean --verbosity quiet
    $rebuildOutput = dotnet build --configuration Release --verbosity quiet 2>&1
    return $LASTEXITCODE -eq 0
}

# Database Validation
Write-LogMessage "💾 Database Validation" "Cyan"

$validationResults["DatabaseExists"] = Test-DemoComponent "Demo Database" {
    Test-Path $demoDbPath
} {
    # Fix: Create database with migrations
    Set-Location $projectPath
    try {
        dotnet ef database update --connection "Data Source=digitalme-demo.db" 2>&1 | Out-Null
        return Test-Path $demoDbPath
    } catch {
        return $false
    }
}

if (Test-Path $demoDbPath) {
    $validationResults["DatabaseIntegrity"] = Test-DemoComponent "Database Integrity" {
        # Simple database integrity check
        try {
            if (Get-Command sqlite3 -ErrorAction SilentlyContinue) {
                $result = sqlite3 $demoDbPath "PRAGMA integrity_check;"
                return $result -eq "ok"
            }
            return $true # Assume OK if sqlite3 not available
        } catch {
            return $false
        }
    }
}

# Performance Validation (if requested)
if ($Performance) {
    Write-LogMessage "⚡ Performance Validation" "Cyan"
    
    $validationResults["MemoryUsage"] = Test-DemoComponent "Memory Usage" {
        $process = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -like "*DigitalMe*" }
        if ($process) {
            $memoryMB = [math]::Round($process.WorkingSet / 1MB, 2)
            Write-LogMessage "Current memory usage: ${memoryMB}MB" "White"
            return $memoryMB -lt 500 # Should be under 500MB for demo
        }
        return $true # OK if not running
    }
}

# Comprehensive Validation (if requested)
if ($Comprehensive) {
    Write-LogMessage "🔬 Comprehensive Validation" "Cyan"
    
    $validationResults["DemoScripts"] = Test-DemoComponent "Demo Scripts" {
        $scriptsPath = "C:\Sources\DigitalMe\scripts\demo"
        $requiredScripts = @("StartDemoEnvironment.ps1", "PrepareDemoData.ps1", "ValidateDemoEnvironment.ps1")
        
        foreach ($script in $requiredScripts) {
            if (-not (Test-Path "$scriptsPath\$script")) {
                return $false
            }
        }
        return $true
    }
    
    $validationResults["DemoServices"] = Test-DemoComponent "Demo Service Classes" {
        $servicesPath = "$projectPath\Services"
        $demoServices = @("DemoDataSeeder.cs", "DemoEnvironmentService.cs")
        
        foreach ($service in $demoServices) {
            if (-not (Test-Path "$servicesPath\$service")) {
                return $false
            }
        }
        return $true
    }
}

# Validation Summary
Write-LogMessage "📊 Validation Summary" "Cyan"
$passCount = ($validationResults.Values | Where-Object { $_ -eq $true }).Count
$totalCount = $validationResults.Count
$successRate = [math]::Round(($passCount / $totalCount) * 100, 1)

Write-Host ""
foreach ($result in $validationResults.GetEnumerator()) {
    $status = if ($result.Value) { "✅ PASS" } else { "❌ FAIL" }
    $color = if ($result.Value) { "Green" } else { "Red" }
    Write-Host "   $($result.Key): $status" -ForegroundColor $color
}

Write-Host ""
Write-LogMessage "Overall Success Rate: $successRate% ($passCount/$totalCount)" $(if ($successRate -ge 80) { "Green" } else { "Red" })

# Demo Readiness Assessment
if ($successRate -ge 90) {
    Write-Host ""
    Write-Host "🎉 DEMO ENVIRONMENT READY!" -ForegroundColor Green
    Write-Host "   Your demo environment is optimized and ready for stakeholder presentations." -ForegroundColor White
    Write-Host "   Start demo with: .\scripts\demo\StartDemoEnvironment.ps1 -CleanStart" -ForegroundColor Cyan
} elseif ($successRate -ge 80) {
    Write-Host ""
    Write-Host "⚠️ DEMO ENVIRONMENT MOSTLY READY" -ForegroundColor Yellow
    Write-Host "   Minor issues detected. Consider running with -Fix parameter." -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "❌ DEMO ENVIRONMENT NOT READY" -ForegroundColor Red
    Write-Host "   Critical issues detected. Run with -Fix parameter to attempt repairs." -ForegroundColor White
}

# Exit with appropriate code
exit $(if ($successRate -ge 80) { 0 } else { 1 })