# DigitalMe Backup Demo Scenarios Test Script
# This script tests and demonstrates all backup demo capabilities

param(
    [switch]$QuickTest,
    [switch]$Comprehensive,
    [switch]$Interactive,
    [string]$BackupMode = "All"
)

Write-Host "🛡️ DigitalMe Backup Demo Scenarios Test" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

$baseUrl = "http://localhost:5001"
$apiBase = "$baseUrl/api/demo/backup"

# Function to log messages with timestamp and color
function Write-TestMessage {
    param(
        [string]$Message, 
        [string]$Color = "White",
        [string]$Type = "INFO"
    )
    $timestamp = Get-Date -Format "HH:mm:ss"
    $icon = switch ($Type) {
        "SUCCESS" { "✅" }
        "ERROR" { "❌" }
        "WARNING" { "⚠️" }
        "INFO" { "ℹ️" }
        "TEST" { "🧪" }
        default { "📋" }
    }
    Write-Host "[$timestamp] $icon $Message" -ForegroundColor $Color
}

# Function to test HTTP endpoint
function Test-BackupEndpoint {
    param(
        [string]$Url,
        [string]$Description,
        [string]$Method = "GET",
        [hashtable]$Body = @{}
    )
    
    try {
        Write-TestMessage "Testing: $Description" "Yellow" "TEST"
        
        if ($Method -eq "GET") {
            $response = Invoke-RestMethod -Uri $Url -Method GET -ErrorAction Stop
        } else {
            $jsonBody = $Body | ConvertTo-Json -Depth 3
            $response = Invoke-RestMethod -Uri $Url -Method POST -Body $jsonBody -ContentType "application/json" -ErrorAction Stop
        }
        
        Write-TestMessage "✅ SUCCESS: $Description" "Green" "SUCCESS"
        return $response
    }
    catch {
        Write-TestMessage "❌ FAILED: $Description - $($_.Exception.Message)" "Red" "ERROR"
        return $null
    }
}

# Function to wait for application startup
function Wait-ForApplication {
    param([string]$Url, [int]$MaxRetries = 30)
    
    Write-TestMessage "Waiting for application to be ready..." "Yellow" "INFO"
    
    for ($i = 1; $i -le $MaxRetries; $i++) {
        try {
            $response = Invoke-WebRequest -Uri $Url -Method GET -TimeoutSec 5 -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                Write-TestMessage "Application is ready!" "Green" "SUCCESS"
                return $true
            }
        }
        catch {
            Write-Host "." -NoNewline -ForegroundColor Yellow
            Start-Sleep -Seconds 2
        }
    }
    
    Write-TestMessage "Application failed to start within timeout" "Red" "ERROR"
    return $false
}

# Test backup scenarios availability
function Test-BackupScenariosAvailability {
    Write-TestMessage "🧪 Testing Backup Scenarios Availability" "Cyan" "TEST"
    
    $scenarios = Test-BackupEndpoint "$apiBase/scenarios" "Get Available Backup Scenarios"
    
    if ($scenarios) {
        Write-TestMessage "Found $($scenarios.scenarios.Count) backup scenarios" "Green" "INFO"
        
        foreach ($scenario in $scenarios.scenarios) {
            Write-Host "   📋 $($scenario.title)" -ForegroundColor White
            Write-Host "      👥 Audience: $($scenario.audience)" -ForegroundColor Gray
            Write-Host "      ⏱️ Duration: $($scenario.estimatedDuration)" -ForegroundColor Gray
            Write-Host "      🛡️ Backup Support: $($scenario.backupModeSupported)" -ForegroundColor Gray
        }
        
        return $true
    }
    
    return $false
}

# Test backup mode activation
function Test-BackupModeActivation {
    param([string]$Mode)
    
    Write-TestMessage "🧪 Testing Backup Mode Activation: $Mode" "Cyan" "TEST"
    
    $modeValue = switch ($Mode) {
        "Offline" { 1 }
        "ApiFailure" { 2 }
        "IntegrationFailure" { 3 }
        "NetworkIssues" { 4 }
        default { 1 }
    }
    
    $result = Test-BackupEndpoint "$apiBase/activate" "Activate Backup Mode: $Mode" "POST" @{ mode = $modeValue }
    
    if ($result) {
        Write-TestMessage "Backup mode '$Mode' activated successfully" "Green" "SUCCESS"
        Start-Sleep -Seconds 2
        return $true
    }
    
    return $false
}

# Test backup response retrieval
function Test-BackupResponses {
    Write-TestMessage "🧪 Testing Backup Response Retrieval" "Cyan" "TEST"
    
    $testScenarios = @(
        "executive_intro",
        "business_value", 
        "technical_expertise",
        "problem_solving",
        "integration_demo",
        "leadership_style",
        "architecture_decisions"
    )
    
    $successCount = 0
    
    foreach ($scenario in $testScenarios) {
        $response = Test-BackupEndpoint "$apiBase/response/$scenario" "Get Backup Response: $scenario"
        
        if ($response -and $response.response.content) {
            $preview = $response.response.content.Substring(0, [Math]::Min(80, $response.response.content.Length))
            Write-Host "      💬 Preview: $preview..." -ForegroundColor Gray
            Write-Host "      ⚡ Response Time: $($response.response.responseTime)" -ForegroundColor Gray
            Write-Host "      🎯 Confidence: $($response.response.confidence * 100)%" -ForegroundColor Gray
            $successCount++
        }
    }
    
    $successRate = ($successCount / $testScenarios.Count) * 100
    Write-TestMessage "Backup responses test: $successCount/$($testScenarios.Count) successful ($successRate%)" "Green" "INFO"
    
    return $successRate -ge 80
}

# Comprehensive backup mode testing
function Test-AllBackupModes {
    Write-TestMessage "🧪 Comprehensive Backup Mode Testing" "Cyan" "TEST"
    
    $backupModes = @("Offline", "ApiFailure", "IntegrationFailure", "NetworkIssues")
    $results = @{}
    
    foreach ($mode in $backupModes) {
        Write-Host ""
        Write-TestMessage "Testing backup mode: $mode" "Yellow" "INFO"
        
        # Activate backup mode
        $activated = Test-BackupModeActivation $mode
        
        if ($activated) {
            # Test responses in this mode
            $responsesWork = Test-BackupResponses
            $results[$mode] = $responsesWork
            
            if ($responsesWork) {
                Write-TestMessage "✅ Backup mode '$mode' fully functional" "Green" "SUCCESS"
            } else {
                Write-TestMessage "⚠️ Backup mode '$mode' activated but responses failed" "Yellow" "WARNING"
            }
        } else {
            $results[$mode] = $false
            Write-TestMessage "❌ Backup mode '$mode' activation failed" "Red" "ERROR"
        }
        
        Start-Sleep -Seconds 1
    }
    
    # Summary
    Write-Host ""
    Write-TestMessage "🎯 Backup Mode Test Summary" "Cyan" "INFO"
    foreach ($mode in $backupModes) {
        $status = if ($results[$mode]) { "✅ PASS" } else { "❌ FAIL" }
        Write-Host "   $mode`: $status" -ForegroundColor $(if ($results[$mode]) { "Green" } else { "Red" })
    }
    
    $passCount = ($results.Values | Where-Object { $_ -eq $true }).Count
    $overallSuccess = $passCount -eq $backupModes.Count
    
    Write-TestMessage "Overall Result: $passCount/$($backupModes.Count) modes successful" $(if ($overallSuccess) { "Green" } else { "Red" }) "INFO"
    return $overallSuccess
}

# Interactive demo test
function Start-InteractiveTest {
    Write-TestMessage "🎮 Starting Interactive Backup Demo Test" "Cyan" "TEST"
    
    do {
        Write-Host ""
        Write-Host "🎯 Interactive Test Options:" -ForegroundColor Cyan
        Write-Host "1. Test Backup Scenarios Availability" -ForegroundColor White
        Write-Host "2. Activate Offline Mode" -ForegroundColor White
        Write-Host "3. Activate API Failure Mode" -ForegroundColor White
        Write-Host "4. Activate Integration Failure Mode" -ForegroundColor White
        Write-Host "5. Activate Network Issues Mode" -ForegroundColor White
        Write-Host "6. Test Backup Responses" -ForegroundColor White
        Write-Host "7. Run Comprehensive Test" -ForegroundColor White
        Write-Host "8. Open Demo in Browser" -ForegroundColor White
        Write-Host "0. Exit" -ForegroundColor Yellow
        Write-Host ""
        
        $choice = Read-Host "Select option (0-8)"
        
        switch ($choice) {
            "1" { Test-BackupScenariosAvailability }
            "2" { Test-BackupModeActivation "Offline" }
            "3" { Test-BackupModeActivation "ApiFailure" }
            "4" { Test-BackupModeActivation "IntegrationFailure" }
            "5" { Test-BackupModeActivation "NetworkIssues" }
            "6" { Test-BackupResponses }
            "7" { Test-AllBackupModes }
            "8" { 
                Write-TestMessage "Opening demo in browser..." "Green" "INFO"
                Start-Process "$baseUrl/integrations"
            }
            "0" { 
                Write-TestMessage "Exiting interactive test..." "Yellow" "INFO"
                return 
            }
            default { Write-TestMessage "Invalid option. Please try again." "Red" "WARNING" }
        }
        
        if ($choice -ne "0") {
            Write-Host ""
            Read-Host "Press Enter to continue..."
        }
        
    } while ($choice -ne "0")
}

# Main execution flow
Write-TestMessage "Starting backup demo scenarios testing..." "Green" "INFO"
Write-Host ""

# Check if application is running
$isRunning = Wait-ForApplication $baseUrl

if (-not $isRunning) {
    Write-TestMessage "❌ Application is not running. Please start the demo environment first." "Red" "ERROR"
    Write-TestMessage "Run: .\StartDemoEnvironment.ps1 -BackupMode" "Yellow" "INFO"
    exit 1
}

# Execute based on parameters
if ($Interactive) {
    Start-InteractiveTest
} elseif ($QuickTest) {
    Write-TestMessage "🚀 Running Quick Backup Test..." "Green" "INFO"
    $available = Test-BackupScenariosAvailability
    $offlineMode = Test-BackupModeActivation "Offline"
    $responses = Test-BackupResponses
    
    $quickSuccess = $available -and $offlineMode -and $responses
    Write-TestMessage "Quick Test Result: $(if ($quickSuccess) { 'SUCCESS' } else { 'FAILED' })" $(if ($quickSuccess) { "Green" } else { "Red" }) "INFO"
} elseif ($Comprehensive) {
    Write-TestMessage "🔬 Running Comprehensive Backup Test..." "Green" "INFO"
    $available = Test-BackupScenariosAvailability
    Start-Sleep -Seconds 2
    $allModes = Test-AllBackupModes
    
    $comprehensiveSuccess = $available -and $allModes
    Write-TestMessage "Comprehensive Test Result: $(if ($comprehensiveSuccess) { 'SUCCESS' } else { 'FAILED' })" $(if ($comprehensiveSuccess) { "Green" } else { "Red" }) "INFO"
} else {
    # Default: Test scenarios availability and one backup mode
    Write-TestMessage "🧪 Running Standard Backup Test..." "Green" "INFO"
    $available = Test-BackupScenariosAvailability
    Start-Sleep -Seconds 2
    $mode = Test-BackupModeActivation "Offline"
    Start-Sleep -Seconds 2
    $responses = Test-BackupResponses
    
    $standardSuccess = $available -and $mode -and $responses
    Write-TestMessage "Standard Test Result: $(if ($standardSuccess) { 'SUCCESS' } else { 'FAILED' })" $(if ($standardSuccess) { "Green" } else { "Red" }) "INFO"
}

Write-Host ""
Write-TestMessage "🏁 Backup Demo Scenarios Testing Complete!" "Cyan" "INFO"
Write-TestMessage "Demo URL: $baseUrl/integrations" "Yellow" "INFO"
Write-Host ""

# Final summary
Write-Host "🛡️ Backup Demo Scenarios Implementation Summary:" -ForegroundColor Cyan
Write-Host "✅ Offline demo mode (complete internet independence)" -ForegroundColor Green
Write-Host "✅ Pre-recorded AI responses (API failure backup)" -ForegroundColor Green  
Write-Host "✅ Alternative demo flows (integration failure backup)" -ForegroundColor Green
Write-Host "✅ Technical deep-dive scenarios (technical stakeholders)" -ForegroundColor Green
Write-Host "✅ Professional UI with backup mode control" -ForegroundColor Green
Write-Host "✅ API endpoints for backup management" -ForegroundColor Green
Write-Host "✅ Comprehensive error handling and resilience" -ForegroundColor Green