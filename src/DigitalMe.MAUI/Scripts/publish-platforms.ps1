#!/usr/bin/env pwsh

param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "..\..\dist\maui",
    [switch]$CreateZip = $true
)

Write-Host "📦 Publishing DigitalMe MAUI for distribution..." -ForegroundColor Green

$ProjectPath = Split-Path -Parent $PSScriptRoot
$ProjectFile = Join-Path $ProjectPath "DigitalMe.MAUI.csproj"
$OutputBase = Join-Path $ProjectPath $OutputPath

# Ensure output directory exists
if (-not (Test-Path $OutputBase)) {
    New-Item -ItemType Directory -Path $OutputBase -Force | Out-Null
}

$PublishTargets = @(
    @{ 
        Name = "Android" 
        Framework = "net8.0-android"
        Runtime = ""
        Args = @("--no-restore")
    }
    @{ 
        Name = "iOS" 
        Framework = "net8.0-ios"
        Runtime = ""
        Args = @("--no-restore")
    }
    @{ 
        Name = "macOS" 
        Framework = "net8.0-maccatalyst"
        Runtime = ""
        Args = @("--no-restore")
    }
    @{ 
        Name = "Windows" 
        Framework = "net8.0-windows10.0.19041.0"
        Runtime = "win10-x64"
        Args = @("--self-contained", "true", "--no-restore")
    }
)

$Results = @()

foreach ($Target in $PublishTargets) {
    Write-Host "📱 Publishing $($Target.Name)..." -ForegroundColor Cyan
    
    $PlatformOutput = Join-Path $OutputBase $Target.Name
    
    $PublishArgs = @(
        "publish"
        $ProjectFile
        "-c", $Configuration
        "-f", $Target.Framework
        "-o", $PlatformOutput
    ) + $Target.Args
    
    if ($Target.Runtime) {
        $PublishArgs += "-r", $Target.Runtime
    }
    
    $StartTime = Get-Date
    
    try {
        Write-Host "   Command: dotnet $($PublishArgs -join ' ')" -ForegroundColor Gray
        $PublishOutput = & dotnet @PublishArgs 2>&1
        $ExitCode = $LASTEXITCODE
        
        $Duration = (Get-Date) - $StartTime
        
        if ($ExitCode -eq 0) {
            Write-Host "✅ $($Target.Name) published in $($Duration.TotalSeconds)s" -ForegroundColor Green
            
            # Create distribution archive
            if ($CreateZip -and (Test-Path $PlatformOutput)) {
                $ArchivePath = Join-Path $OutputBase "DigitalMe-$($Target.Name)-v1.0.0.zip"
                Write-Host "📄 Creating archive: $ArchivePath" -ForegroundColor Yellow
                
                Compress-Archive -Path "$PlatformOutput\*" -DestinationPath $ArchivePath -Force
                
                if (Test-Path $ArchivePath) {
                    $ArchiveSize = (Get-Item $ArchivePath).Length / 1MB
                    Write-Host "   Archive created: $([math]::Round($ArchiveSize, 1)) MB" -ForegroundColor Green
                }
            }
            
            $Results += @{
                Platform = $Target.Name
                Status = "Success"
                Duration = $Duration.TotalSeconds
                OutputPath = $PlatformOutput
            }
        } else {
            Write-Host "❌ $($Target.Name) publish failed" -ForegroundColor Red
            Write-Host $PublishOutput -ForegroundColor Red
            $Results += @{
                Platform = $Target.Name
                Status = "Failed"
                Duration = $Duration.TotalSeconds
                OutputPath = $null
            }
        }
    }
    catch {
        Write-Host "❌ $($Target.Name) publish crashed: $($_.Exception.Message)" -ForegroundColor Red
        $Results += @{
            Platform = $Target.Name
            Status = "Crashed" 
            Duration = 0
            OutputPath = $null
        }
    }
}

Write-Host "`n📊 Publish Summary:" -ForegroundColor Magenta
Write-Host "==================" -ForegroundColor Magenta

foreach ($Result in $Results) {
    $StatusColor = if ($Result.Status -eq "Success") { "Green" } else { "Red" }
    Write-Host "$($Result.Platform): $($Result.Status) ($($Result.Duration)s)" -ForegroundColor $StatusColor
    if ($Result.OutputPath) {
        Write-Host "   📂 $($Result.OutputPath)" -ForegroundColor Gray
    }
}

$SuccessfulPublishes = ($Results | Where-Object { $_.Status -eq "Success" }).Count
$TotalPublishes = $Results.Count

Write-Host "`n🎯 $SuccessfulPublishes/$TotalPublishes platforms published successfully" -ForegroundColor $(if ($SuccessfulPublishes -eq $TotalPublishes) { "Green" } else { "Yellow" })
Write-Host "📂 Output directory: $OutputBase" -ForegroundColor Cyan