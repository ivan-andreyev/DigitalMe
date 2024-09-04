#!/usr/bin/env pwsh

Write-Host "🏢 Building DigitalMe MAUI for Windows..." -ForegroundColor Green

$ProjectPath = Split-Path -Parent $PSScriptRoot
$ProjectFile = Join-Path $ProjectPath "DigitalMe.MAUI.csproj"

Write-Host "📁 Project: $ProjectFile" -ForegroundColor Gray

# Clean previous build
Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean $ProjectFile -c Release

# Build for Windows
Write-Host "🔨 Building Windows version..." -ForegroundColor Cyan
$BuildArgs = @(
    "build"
    $ProjectFile
    "-c", "Release" 
    "-f", "net8.0-windows10.0.19041.0"
)

$StartTime = Get-Date
$BuildOutput = & dotnet @BuildArgs 2>&1
$ExitCode = $LASTEXITCODE
$Duration = (Get-Date) - $StartTime

if ($ExitCode -eq 0) {
    Write-Host "✅ Windows build succeeded in $($Duration.TotalSeconds)s" -ForegroundColor Green
    
    # Show output files
    $BinPath = Join-Path $ProjectPath "bin\Release\net8.0-windows10.0.19041.0\win10-x64"
    if (Test-Path $BinPath) {
        Write-Host "📦 Output files:" -ForegroundColor Magenta
        Get-ChildItem $BinPath -File | ForEach-Object {
            $Size = [math]::Round($_.Length / 1KB, 1)
            Write-Host "   $($_.Name) ($Size KB)" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "❌ Windows build failed" -ForegroundColor Red
    Write-Host $BuildOutput -ForegroundColor Red
    exit 1
}

Write-Host "🎯 Build completed successfully!" -ForegroundColor Green