#!/usr/bin/env pwsh

param(
    [string]$Configuration = "Release",
    [switch]$Clean = $false
)

Write-Host "🚀 Building DigitalMe MAUI for all platforms..." -ForegroundColor Green

$ProjectPath = Split-Path -Parent $PSScriptRoot
$ProjectFile = Join-Path $ProjectPath "DigitalMe.MAUI.csproj"

if ($Clean) {
    Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
    dotnet clean $ProjectFile -c $Configuration
}

$Platforms = @(
    @{ Name = "Android"; Framework = "net8.0-android"; Runtime = "" }
    @{ Name = "iOS"; Framework = "net8.0-ios"; Runtime = "" }
    @{ Name = "macOS"; Framework = "net8.0-maccatalyst"; Runtime = "" }
    @{ Name = "Windows"; Framework = "net8.0-windows10.0.19041.0"; Runtime = "win10-x64" }
)

$Results = @()

foreach ($Platform in $Platforms) {
    Write-Host "📱 Building for $($Platform.Name)..." -ForegroundColor Cyan
    
    $BuildArgs = @(
        "build"
        $ProjectFile
        "-c", $Configuration
        "-f", $Platform.Framework
    )
    
    if ($Platform.Runtime) {
        $BuildArgs += "-r", $Platform.Runtime
    }
    
    $StartTime = Get-Date
    
    try {
        $BuildOutput = & dotnet @BuildArgs 2>&1
        $ExitCode = $LASTEXITCODE
        
        $Duration = (Get-Date) - $StartTime
        
        if ($ExitCode -eq 0) {
            Write-Host "✅ $($Platform.Name) build succeeded in $($Duration.TotalSeconds)s" -ForegroundColor Green
            $Results += @{
                Platform = $Platform.Name
                Status = "Success"
                Duration = $Duration.TotalSeconds
                Output = $BuildOutput
            }
        } else {
            Write-Host "❌ $($Platform.Name) build failed" -ForegroundColor Red
            $Results += @{
                Platform = $Platform.Name
                Status = "Failed"
                Duration = $Duration.TotalSeconds
                Output = $BuildOutput
            }
        }
    }
    catch {
        Write-Host "❌ $($Platform.Name) build crashed: $($_.Exception.Message)" -ForegroundColor Red
        $Results += @{
            Platform = $Platform.Name
            Status = "Crashed"
            Duration = 0
            Output = $_.Exception.Message
        }
    }
}

Write-Host "`n📊 Build Summary:" -ForegroundColor Magenta
Write-Host "=================" -ForegroundColor Magenta

foreach ($Result in $Results) {
    $StatusColor = if ($Result.Status -eq "Success") { "Green" } else { "Red" }
    Write-Host "$($Result.Platform): $($Result.Status) ($($Result.Duration)s)" -ForegroundColor $StatusColor
}

$SuccessfulBuilds = ($Results | Where-Object { $_.Status -eq "Success" }).Count
$TotalBuilds = $Results.Count

Write-Host "`n🎯 $SuccessfulBuilds/$TotalBuilds platforms built successfully" -ForegroundColor $(if ($SuccessfulBuilds -eq $TotalBuilds) { "Green" } else { "Yellow" })

if ($SuccessfulBuilds -lt $TotalBuilds) {
    Write-Host "`n❗ Failed builds details:" -ForegroundColor Red
    foreach ($Failed in ($Results | Where-Object { $_.Status -ne "Success" })) {
        Write-Host "`n--- $($Failed.Platform) ---" -ForegroundColor Red
        Write-Host $Failed.Output
    }
}