# DRY Violations Audit Script
# Tracks progress during DRY violations elimination roadmap
# Usage: .\scripts\dry-violations-audit.ps1

param(
    [switch]$ShowFiles,
    [string]$ReportPath = "dry-audit-report.txt"
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path $PSScriptRoot -Parent

Write-Host "🔍 DRY VIOLATIONS AUDIT - DigitalMe Project" -ForegroundColor Cyan
Write-Host "📅 Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host "📁 Project Root: $ProjectRoot" -ForegroundColor Gray
Write-Host ""

# Change to project root
Set-Location $ProjectRoot

# Define violation patterns
$Violations = @{
    "ArgumentNullException" = @{
        Pattern = "ArgumentNullException"
        Description = "Manual ArgumentNullException implementations"
        Target = "Guard.NotNull() usage"
    }
    "CatchException" = @{
        Pattern = "catch \(Exception"
        Description = "Generic exception handling blocks"
        Target = "BaseService.ExecuteAsync() usage"
    }
    "TimeSpanFromSeconds" = @{
        Pattern = "TimeSpan\.FromSeconds"
        Description = "Hardcoded timeout magic values"
        Target = "HttpConstants/TimeoutConstants usage"
    }
}

# Baseline targets for progress tracking
$BaselineTargets = @{
    "ArgumentNullException" = 51
    "CatchException" = 119
    "TimeSpanFromSeconds" = 33
}

$Results = @{}
$TotalViolations = 0
$TotalBaseline = 0

Write-Host "🎯 SCANNING FOR DRY VIOLATIONS..." -ForegroundColor Yellow

foreach ($ViolationType in $Violations.Keys) {
    $Violation = $Violations[$ViolationType]
    $Pattern = $Violation.Pattern

    Write-Host "   Scanning: $($Violation.Description)..." -ForegroundColor Gray

    try {
        # Use ripgrep for fast searching, fallback to findstr
        try {
            $SearchResults = & rg $Pattern --type cs --count 2>$null
        } catch {
            # Fallback to PowerShell search if rg not available
            $CsFiles = Get-ChildItem -Path . -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notmatch "\\bin\\|\\obj\\" }
            $SearchResults = @()
            foreach ($File in $CsFiles) {
                $Content = Get-Content $File.FullName -Raw -ErrorAction SilentlyContinue
                if ($Content -and $Content -match $Pattern) {
                    $MatchCount = ([regex]::Matches($Content, $Pattern)).Count
                    $SearchResults += "$($File.FullName):$MatchCount"
                }
            }
        }

        if ($SearchResults) {
            $FileCount = ($SearchResults | Measure-Object).Count
            $OccurrenceCount = 0
            foreach ($Result in $SearchResults) {
                $parts = $Result -split ':'
                if ($parts.Length -ge 2) {
                    try {
                        $OccurrenceCount += [int]$parts[-1]  # Last part should be count
                    } catch {
                        $OccurrenceCount += 1  # If parsing fails, assume 1 occurrence
                    }
                }
            }
        } else {
            $FileCount = 0
            $OccurrenceCount = 0
        }

        $FilesWithMatches = @()
        if ($ShowFiles) {
            try {
                $FilesWithMatches = & rg $Pattern --type cs --files-with-matches 2>$null
            } catch {
                # Fallback: extract file paths from search results
                $FilesWithMatches = $SearchResults | ForEach-Object {
                    ($_ -split ':')[0]
                } | Sort-Object -Unique
            }
        }

        $Results[$ViolationType] = @{
            FileCount = $FileCount
            OccurrenceCount = $OccurrenceCount
            Baseline = $BaselineTargets[$ViolationType]
            Files = $FilesWithMatches
        }

        $TotalViolations += $FileCount
        $TotalBaseline += $BaselineTargets[$ViolationType]

    } catch {
        Write-Warning "Error scanning for ${ViolationType}: $_"
        $Results[$ViolationType] = @{
            FileCount = -1
            OccurrenceCount = -1
            Baseline = $BaselineTargets[$ViolationType]
            Files = @()
        }
    }
}

# Calculate overall progress
$OverallReduction = if ($TotalBaseline -gt 0) {
    [Math]::Round((($TotalBaseline - $TotalViolations) / $TotalBaseline) * 100, 1)
} else { 0 }

$SuccessTarget = [Math]::Round($TotalBaseline * 0.2, 0) # 80% reduction = 20% remaining

Write-Host ""
Write-Host "📊 DRY VIOLATIONS SUMMARY" -ForegroundColor Green
Write-Host "=========================" -ForegroundColor Green

foreach ($ViolationType in $Violations.Keys) {
    $Result = $Results[$ViolationType]
    $Violation = $Violations[$ViolationType]
    $Baseline = $Result.Baseline
    $Current = $Result.FileCount

    if ($Current -ge 0) {
        $Reduction = if ($Baseline -gt 0) {
            [Math]::Round((($Baseline - $Current) / $Baseline) * 100, 1)
        } else { 0 }

        $Status = if ($Current -le $Baseline * 0.2) { "✅ TARGET MET" }
                 elseif ($Reduction -gt 50) { "🟡 GOOD PROGRESS" }
                 elseif ($Reduction -gt 0) { "🔵 SOME PROGRESS" }
                 else { "🔴 NO PROGRESS" }

        Write-Host ""
        Write-Host "🎯 $($Violation.Description)" -ForegroundColor White
        Write-Host "   Files: $Current (was $Baseline) | Reduction: $Reduction% | $Status" -ForegroundColor Gray
        Write-Host "   Occurrences: $($Result.OccurrenceCount)" -ForegroundColor Gray
        Write-Host "   Target: $($Violation.Target)" -ForegroundColor Gray

        if ($ShowFiles -and $Result.Files.Count -gt 0) {
            Write-Host "   Files with violations:" -ForegroundColor DarkGray
            $Result.Files | Select-Object -First 10 | ForEach-Object {
                Write-Host "     - $_" -ForegroundColor DarkGray
            }
            if ($Result.Files.Count -gt 10) {
                Write-Host "     ... and $($Result.Files.Count - 10) more files" -ForegroundColor DarkGray
            }
        }
    } else {
        Write-Host ""
        Write-Host "🎯 $($Violation.Description)" -ForegroundColor White
        Write-Host "   Status: ❌ SCAN ERROR" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "📈 OVERALL PROGRESS" -ForegroundColor Cyan
Write-Host "==================" -ForegroundColor Cyan
Write-Host "Total Files: $TotalViolations (was $TotalBaseline)" -ForegroundColor White
Write-Host "Overall Reduction: $OverallReduction%" -ForegroundColor White
Write-Host "Success Target: ≤$SuccessTarget files (80% reduction)" -ForegroundColor White

$OverallStatus = if ($TotalViolations -le $SuccessTarget) { "✅ SUCCESS TARGET ACHIEVED!" }
                elseif ($OverallReduction -ge 60) { "🟡 EXCELLENT PROGRESS" }
                elseif ($OverallReduction -ge 30) { "🔵 GOOD PROGRESS" }
                elseif ($OverallReduction -gt 0) { "🟠 SOME PROGRESS" }
                else { "🔴 NO PROGRESS YET" }

Write-Host "Status: $OverallStatus" -ForegroundColor $(
    if ($OverallStatus.StartsWith("✅")) { "Green" }
    elseif ($OverallStatus.StartsWith("🟡")) { "Yellow" }
    elseif ($OverallStatus.StartsWith("🔵")) { "Blue" }
    elseif ($OverallStatus.StartsWith("🟠")) { "DarkYellow" }
    else { "Red" }
)

# Generate report
if ($ReportPath) {
    $ReportContent = @"
DRY VIOLATIONS AUDIT REPORT
===========================
Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Project: DigitalMe
Command: $($MyInvocation.Line)

SUMMARY:
- Total Files with Violations: $TotalViolations (baseline: $TotalBaseline)
- Overall Reduction: $OverallReduction%
- Success Target: ≤$SuccessTarget files (80% reduction)
- Status: $OverallStatus

DETAILED RESULTS:
"@

    foreach ($ViolationType in $Violations.Keys) {
        $Result = $Results[$ViolationType]
        $Violation = $Violations[$ViolationType]
        $Baseline = $Result.Baseline
        $Current = $Result.FileCount

        if ($Current -ge 0) {
            $Reduction = if ($Baseline -gt 0) {
                [Math]::Round((($Baseline - $Current) / $Baseline) * 100, 1)
            } else { 0 }

            $ReportContent += @"

$($Violation.Description):
- Files: $Current (was $Baseline)
- Occurrences: $($Result.OccurrenceCount)
- Reduction: $Reduction%
- Target: $($Violation.Target)
"@
        }
    }

    $ReportContent | Out-File -FilePath $ReportPath -Encoding UTF8
    Write-Host ""
    Write-Host "📄 Report saved to: $ReportPath" -ForegroundColor Green
}

Write-Host ""
Write-Host "🎯 NEXT STEPS:" -ForegroundColor Magenta
if ($TotalViolations -le $SuccessTarget) {
    Write-Host "✅ DRY violations elimination completed successfully!" -ForegroundColor Green
    Write-Host "   Consider running architecture validation and final quality checks." -ForegroundColor Gray
} elseif ($OverallReduction -ge 60) {
    Write-Host "🔥 Excellent progress! Continue with current migration strategy." -ForegroundColor Yellow
    Write-Host "   Focus on remaining high-impact files in current phase." -ForegroundColor Gray
} else {
    Write-Host "📋 Continue systematic migration according to roadmap phases:" -ForegroundColor White
    Write-Host "   Phase 1: Controllers & Core Services" -ForegroundColor Gray
    Write-Host "   Phase 2: Business Logic Layer" -ForegroundColor Gray
    Write-Host "   Phase 3: Extensions & Configuration" -ForegroundColor Gray
}

Write-Host ""
Write-Host "💡 Usage:" -ForegroundColor Blue
Write-Host "   .\scripts\dry-violations-audit.ps1 -ShowFiles    # Show files with violations" -ForegroundColor Gray
Write-Host "   .\scripts\dry-violations-audit.ps1 -ReportPath report.txt  # Custom report location" -ForegroundColor Gray

# Exit with appropriate code
if ($TotalViolations -le $SuccessTarget) {
    exit 0  # Success
} elseif ($OverallReduction -gt 0) {
    exit 1  # Progress but not complete
} else {
    exit 2  # No progress
}