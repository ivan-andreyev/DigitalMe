# Phase Completion Check Script
# Validates completion of DRY violations elimination phases
# Usage: .\scripts\phase-completion-check.ps1 --phase 1

param(
    [Parameter(Mandatory=$true)]
    [int]$Phase,
    [switch]$Verbose,
    [string]$ReportPath = "phase-$Phase-completion-report.txt"
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path $PSScriptRoot -Parent

Write-Host "🎯 PHASE $Phase COMPLETION CHECK - DRY Violations Elimination" -ForegroundColor Cyan
Write-Host "📅 Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

Set-Location $ProjectRoot

# Phase-specific targets and criteria
$PhaseTargets = @{
    1 = @{
        Name = "Foundation Layer (Controllers & Core Services)"
        ArgumentNullException = @{ Target = 35; Description = "30% reduction from baseline 51" }
        CatchException = @{ Target = 85; Description = "28% reduction from baseline 119" }
        TimeSpanFromSeconds = @{ Target = 25; Description = "24% reduction from baseline 33" }
        TestPassRate = 95
        Files = @(
            "src/DigitalMe/Controllers/MVPConversationController.cs",
            "src/DigitalMe/Controllers/ErrorLearningController.cs",
            "src/DigitalMe/Controllers/SlackWebhookController.cs",
            "src/DigitalMe/Controllers/LearningController.cs",
            "src/DigitalMe/Controllers/AdvancedSuggestionController.cs",
            "src/DigitalMe/Services/MVPMessageProcessor.cs",
            "src/DigitalMe/Services/Database/DatabaseMigrationService.cs",
            "src/DigitalMe/Services/CaptchaSolving/CaptchaSolvingService.cs",
            "src/DigitalMe/Services/Learning/AutoDocumentationParser.cs"
        )
    }
    2 = @{
        Name = "Business Logic Layer (Service Implementation)"
        ArgumentNullException = @{ Target = 15; Description = "70% reduction from baseline 51" }
        CatchException = @{ Target = 35; Description = "71% reduction from baseline 119" }
        TimeSpanFromSeconds = @{ Target = 10; Description = "70% reduction from baseline 33" }
        TestPassRate = 95
        Files = @(
            "src/DigitalMe/Services/Learning/ErrorLearning/Repositories/ErrorPatternRepository.cs",
            "src/DigitalMe/Services/Learning/ErrorLearning/Repositories/LearningHistoryRepository.cs",
            "src/DigitalMe/Services/Learning/ErrorLearning/Repositories/OptimizationSuggestionRepository.cs",
            "src/DigitalMe/Integrations/External/Slack/SlackService.cs",
            "src/DigitalMe/Integrations/External/GitHub/GitHubService.cs",
            "src/DigitalMe/Integrations/External/ClickUp/ClickUpService.cs"
        )
    }
    3 = @{
        Name = "Extension & Configuration Layer (Support Components)"
        ArgumentNullException = @{ Target = 5; Description = "90% reduction from baseline 51" }
        CatchException = @{ Target = 10; Description = "92% reduction from baseline 119" }
        TimeSpanFromSeconds = @{ Target = 3; Description = "91% reduction from baseline 33" }
        TestPassRate = 95
        Files = @(
            "src/DigitalMe/Extensions/ServiceCollectionExtensions.cs",
            "tests/DigitalMe.Tests.Unit/Services/TestExecutorTests.cs",
            "src/DigitalMe.MAUI/Services/NotificationService.cs",
            "src/DigitalMe.Web/Services/OptimizedDataService.cs"
        )
    }
}

if (-not $PhaseTargets.ContainsKey($Phase)) {
    Write-Error "Invalid phase number: $Phase. Valid phases are 1, 2, 3."
    exit 1
}

$PhaseInfo = $PhaseTargets[$Phase]

Write-Host "🎯 Validating Phase $Phase: $($PhaseInfo.Name)" -ForegroundColor Green
Write-Host ""

# Run violations audit
Write-Host "📊 Running DRY violations audit..." -ForegroundColor Yellow
$AuditScript = Join-Path $PSScriptRoot "dry-violations-audit.ps1"
if (Test-Path $AuditScript) {
    & $AuditScript -ReportPath "temp-audit.txt"
    $AuditResults = Get-Content "temp-audit.txt" -Raw
    Remove-Item "temp-audit.txt" -ErrorAction SilentlyContinue
} else {
    Write-Warning "DRY violations audit script not found. Performing manual check..."
    $AuditResults = "Manual audit required"
}

# Check violation counts
$ViolationResults = @{}
$AllTargetsMet = $true

foreach ($ViolationType in @("ArgumentNullException", "CatchException", "TimeSpanFromSeconds")) {
    $Pattern = switch ($ViolationType) {
        "ArgumentNullException" { "ArgumentNullException" }
        "CatchException" { "catch \(Exception" }
        "TimeSpanFromSeconds" { "TimeSpan\.FromSeconds" }
    }

    try {
        $SearchResults = & rg $Pattern --type cs --count 2>$null
        $CurrentCount = if ($SearchResults) { ($SearchResults | Measure-Object).Count } else { 0 }
    } catch {
        $CurrentCount = -1
    }

    $Target = $PhaseInfo[$ViolationType].Target
    $TargetMet = $CurrentCount -le $Target -and $CurrentCount -ge 0

    $ViolationResults[$ViolationType] = @{
        Current = $CurrentCount
        Target = $Target
        Met = $TargetMet
        Description = $PhaseInfo[$ViolationType].Description
    }

    if (-not $TargetMet) { $AllTargetsMet = $false }
}

# Check test pass rate
Write-Host "🧪 Running test suite to validate quality..." -ForegroundColor Yellow
try {
    $TestOutput = & dotnet test --no-build --logger "console;verbosity=minimal" 2>&1
    $TestPassed = $LASTEXITCODE -eq 0

    # Parse test results for pass rate
    $TestSummaryLine = $TestOutput | Where-Object { $_ -match "Passed:|Failed:" } | Select-Object -Last 1
    if ($TestSummaryLine -match "Passed!\s*-\s*Failed:\s*(\d+),\s*Passed:\s*(\d+)") {
        $FailedTests = [int]$Matches[1]
        $PassedTests = [int]$Matches[2]
        $TotalTests = $FailedTests + $PassedTests
        $PassRate = if ($TotalTests -gt 0) { [Math]::Round(($PassedTests / $TotalTests) * 100, 1) } else { 0 }
    } else {
        $PassRate = if ($TestPassed) { 100 } else { 0 }
        $TotalTests = "Unknown"
    }
} catch {
    Write-Warning "Test execution failed: $_"
    $TestPassed = $false
    $PassRate = 0
    $TotalTests = "Error"
}

$TestTargetMet = $PassRate -ge $PhaseInfo.TestPassRate

# Check specific files migration (sample check)
Write-Host "📁 Checking key files migration status..." -ForegroundColor Yellow
$FilesMigrated = 0
$FilesTotal = $PhaseInfo.Files.Count

foreach ($FilePath in $PhaseInfo.Files) {
    if (Test-Path $FilePath) {
        $FileContent = Get-Content $FilePath -Raw

        # Check for DRY infrastructure usage patterns
        $UsesGuard = $FileContent -match "Guard\."
        $UsesBaseService = $FileContent -match "BaseService" -or $FileContent -match "ExecuteAsync"
        $UsesConstants = $FileContent -match "HttpConstants" -or $FileContent -match "TimeoutConstants"

        if ($UsesGuard -or $UsesBaseService -or $UsesConstants) {
            $FilesMigrated++
            if ($Verbose) {
                Write-Host "   ✅ $FilePath - DRY patterns detected" -ForegroundColor Green
            }
        } else {
            if ($Verbose) {
                Write-Host "   ❌ $FilePath - No DRY patterns found" -ForegroundColor Red
            }
        }
    } else {
        if ($Verbose) {
            Write-Host "   ⚠️  $FilePath - File not found" -ForegroundColor Yellow
        }
    }
}

$FilesMigrationRate = if ($FilesTotal -gt 0) { [Math]::Round(($FilesMigrated / $FilesTotal) * 100, 1) } else { 0 }
$FileTargetMet = $FilesMigrationRate -ge 70  # 70% of key files should show DRY patterns

# Overall assessment
$OverallSuccess = $AllTargetsMet -and $TestTargetMet -and $FileTargetMet

Write-Host ""
Write-Host "📋 PHASE $Phase COMPLETION RESULTS" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Violations results
foreach ($ViolationType in $ViolationResults.Keys) {
    $Result = $ViolationResults[$ViolationType]
    $Status = if ($Result.Met) { "✅ TARGET MET" } else { "❌ TARGET MISSED" }
    $Color = if ($Result.Met) { "Green" } else { "Red" }

    Write-Host ""
    Write-Host "$ViolationType Violations:" -ForegroundColor White
    Write-Host "   Current: $($Result.Current) | Target: ≤$($Result.Target) | $Status" -ForegroundColor $Color
    Write-Host "   Goal: $($Result.Description)" -ForegroundColor Gray
}

# Test results
$TestStatus = if ($TestTargetMet) { "✅ TARGET MET" } else { "❌ TARGET MISSED" }
$TestColor = if ($TestTargetMet) { "Green" } else { "Red" }
Write-Host ""
Write-Host "Test Suite Quality:" -ForegroundColor White
Write-Host "   Pass Rate: $PassRate% | Target: ≥$($PhaseInfo.TestPassRate)% | $TestStatus" -ForegroundColor $TestColor
Write-Host "   Total Tests: $TotalTests" -ForegroundColor Gray

# File migration results
$FileStatus = if ($FileTargetMet) { "✅ TARGET MET" } else { "❌ TARGET MISSED" }
$FileColor = if ($FileTargetMet) { "Green" } else { "Red" }
Write-Host ""
Write-Host "Key Files Migration:" -ForegroundColor White
Write-Host "   Migrated: $FilesMigrated/$FilesTotal ($FilesMigrationRate%) | Target: ≥70% | $FileStatus" -ForegroundColor $FileColor

# Overall result
Write-Host ""
Write-Host "🎯 OVERALL PHASE $Phase STATUS:" -ForegroundColor Magenta
if ($OverallSuccess) {
    Write-Host "✅ PHASE $Phase COMPLETED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "   All targets met. Ready to proceed to next phase." -ForegroundColor Gray
} else {
    Write-Host "❌ PHASE $Phase INCOMPLETE" -ForegroundColor Red
    Write-Host "   Some targets missed. Review and continue migration work." -ForegroundColor Gray
}

# Generate detailed report
if ($ReportPath) {
    $ReportContent = @"
PHASE $Phase COMPLETION CHECK REPORT
===================================
Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Phase: $Phase - $($PhaseInfo.Name)
Overall Status: $(if ($OverallSuccess) { "COMPLETED" } else { "INCOMPLETE" })

VIOLATION TARGETS:
"@

    foreach ($ViolationType in $ViolationResults.Keys) {
        $Result = $ViolationResults[$ViolationType]
        $Status = if ($Result.Met) { "MET" } else { "MISSED" }
        $ReportContent += "`n$ViolationType: $($Result.Current)/$($Result.Target) - $Status"
    }

    $ReportContent += @"

TEST QUALITY:
Pass Rate: $PassRate% (Target: ≥$($PhaseInfo.TestPassRate)%) - $(if ($TestTargetMet) { "MET" } else { "MISSED" })

FILE MIGRATION:
Key Files: $FilesMigrated/$FilesTotal ($FilesMigrationRate%) - $(if ($FileTargetMet) { "MET" } else { "MISSED" })

AUDIT RESULTS:
$AuditResults
"@

    $ReportContent | Out-File -FilePath $ReportPath -Encoding UTF8
    Write-Host ""
    Write-Host "📄 Detailed report saved to: $ReportPath" -ForegroundColor Green
}

Write-Host ""
if ($OverallSuccess) {
    Write-Host "🚀 Next Steps: Begin Phase $($Phase + 1) or proceed to final validation if this was Phase 3." -ForegroundColor Green
} else {
    Write-Host "🔧 Next Steps: Address missed targets and re-run phase completion check." -ForegroundColor Yellow
    Write-Host "   Use: .\scripts\dry-violations-audit.ps1 -ShowFiles" -ForegroundColor Gray
    Write-Host "   Focus on files not yet migrated to DRY infrastructure." -ForegroundColor Gray
}

# Exit with appropriate code
if ($OverallSuccess) {
    exit 0  # Phase completed
} else {
    exit 1  # Phase incomplete
}