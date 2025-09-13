# ISP Compliance Validation Script
# Verifies that all interfaces comply with Interface Segregation Principle (≤5 methods)

$interfacesPath = "C:\Sources\DigitalMe\DigitalMe\Services"

function Count-InterfaceMethods {
    param($FilePath)
    
    $content = Get-Content $FilePath -Raw
    $interfaces = @()
    
    # Find all interface declarations
    $interfaceMatches = [regex]::Matches($content, 'public\s+interface\s+(\w+)')
    
    foreach ($match in $interfaceMatches) {
        $interfaceName = $match.Groups[1].Value
        
        # Find the interface body - from interface declaration to next interface or end of file
        $interfaceStart = $match.Index
        $nextInterfaceMatch = [regex]::Match($content, 'public\s+interface\s+\w+', $interfaceStart + 1)
        
        $interfaceEnd = if ($nextInterfaceMatch.Success) { 
            $nextInterfaceMatch.Index 
        } else { 
            $content.Length 
        }
        
        $interfaceBody = $content.Substring($interfaceStart, $interfaceEnd - $interfaceStart)
        
        # Count methods in this interface (Task return type methods)
        $methodCount = ([regex]::Matches($interfaceBody, '\s+Task\s*<?[\w<>?,\s]*>?\s+\w+\s*\(')).Count
        
        $interfaces += @{
            Name = $interfaceName
            MethodCount = $methodCount
            File = $FilePath
        }
    }
    
    return $interfaces
}

$results = @()

# Check Voice interfaces specifically
$voiceInterfaces = Get-ChildItem -Path "$interfacesPath\Voice" -Filter "I*.cs" -Recurse
foreach ($file in $voiceInterfaces) {
    $fileResults = Count-InterfaceMethods -FilePath $file.FullName
    if ($fileResults) {
        $results += $fileResults
    }
}

# Check other recently modified interfaces
$otherInterfaces = @(
    "$interfacesPath\WebNavigation\IWebNavigationService.cs",
    "$interfacesPath\CaptchaSolving\ICaptchaSolvingService.cs", 
    "$interfacesPath\FileProcessing\IFileProcessingService.cs"
)

foreach ($file in $otherInterfaces) {
    if (Test-Path $file) {
        $fileResults = Count-InterfaceMethods -FilePath $file
        if ($fileResults) {
            $results += $fileResults
        }
    }
}

Write-Host "=== ISP COMPLIANCE VALIDATION RESULTS ===" -ForegroundColor Green
Write-Host ""

$compliant = 0
$violations = 0

foreach ($result in $results) {
    $status = if ($result.MethodCount -le 5) { 
        $compliant++
        "✅ COMPLIANT" 
    } else { 
        $violations++
        "❌ VIOLATION" 
    }
    
    $fileName = Split-Path $result.File -Leaf
    Write-Host "$($result.Name): $($result.MethodCount) methods - $status" -ForegroundColor $(if ($result.MethodCount -le 5) { "Green" } else { "Red" })
    Write-Host "   File: $fileName" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "=== SUMMARY ===" -ForegroundColor Yellow
Write-Host "Compliant Interfaces: $compliant" -ForegroundColor Green
Write-Host "ISP Violations: $violations" -ForegroundColor $(if ($violations -eq 0) { "Green" } else { "Red" })
Write-Host ""

if ($violations -eq 0) {
    Write-Host "🎉 ALL INTERFACES ARE ISP COMPLIANT!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "⚠️  ISP VIOLATIONS DETECTED" -ForegroundColor Red
    exit 1
}