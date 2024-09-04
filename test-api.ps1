# API Testing Script for DigitalMe Milestone 1
Write-Host "=== DigitalMe API Testing ===" -ForegroundColor Green

$baseUrl = "http://localhost:5000"

# Ignore SSL certificate issues
add-type @"
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    public class TrustAllCertsPolicy : ICertificatePolicy {
        public bool CheckValidationResult(
            ServicePoint srvPoint, X509Certificate certificate,
            WebRequest request, int certificateProblem) {
            return true;
        }
    }
"@
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

# Start the application
Write-Host "Starting DigitalMe application..." -ForegroundColor Yellow
$app = Start-Process -FilePath "dotnet" -ArgumentList "run", "--urls", $baseUrl -WorkingDirectory ".\DigitalMe" -PassThru -WindowStyle Hidden

# Wait for application to start
Start-Sleep -Seconds 8

try {
    # Test 1: Health Check
    Write-Host "`n1. Testing Health Check endpoint..." -ForegroundColor Cyan
    try {
        $healthResponse = Invoke-RestMethod -Uri "$baseUrl/health" -Method GET
        Write-Host "✅ Health Check: OK" -ForegroundColor Green
        Write-Host "Response: $($healthResponse | ConvertTo-Json)" -ForegroundColor Gray
    } catch {
        Write-Host "❌ Health Check: FAILED - $($_.Exception.Message)" -ForegroundColor Red
    }

    # Test 2: Create Personality Profile
    Write-Host "`n2. Testing Create Personality Profile..." -ForegroundColor Cyan
    $personalityData = @{
        Name = "Ivan"
        Description = "34-year-old programmer and Head of R&D at EllyAnalytics"
    } | ConvertTo-Json
    
    try {
        $personalityResponse = Invoke-RestMethod -Uri "$baseUrl/api/personality" -Method POST -Body $personalityData -ContentType "application/json"
        Write-Host "✅ Create Personality: OK" -ForegroundColor Green
        Write-Host "Created personality ID: $($personalityResponse.Id)" -ForegroundColor Gray
        $personalityId = $personalityResponse.Id
    } catch {
        Write-Host "❌ Create Personality: FAILED - $($_.Exception.Message)" -ForegroundColor Red
    }

    # Test 3: Get Personality Profile
    Write-Host "`n3. Testing Get Personality Profile..." -ForegroundColor Cyan
    try {
        $getPersonalityResponse = Invoke-RestMethod -Uri "$baseUrl/api/personality/Ivan" -Method GET
        Write-Host "✅ Get Personality: OK" -ForegroundColor Green
        Write-Host "Name: $($getPersonalityResponse.Name), ID: $($getPersonalityResponse.Id)" -ForegroundColor Gray
    } catch {
        Write-Host "❌ Get Personality: FAILED - $($_.Exception.Message)" -ForegroundColor Red
    }

    # Test 4: Add Personality Trait
    Write-Host "`n4. Testing Add Personality Trait..." -ForegroundColor Cyan
    if ($personalityId) {
        $traitData = @{
            Category = "Technical"
            Name = "C# Expert"
            Description = "Deep expertise in C# and .NET development"
            Weight = 1.5
        } | ConvertTo-Json
        
        try {
            $traitResponse = Invoke-RestMethod -Uri "$baseUrl/api/personality/$personalityId/traits" -Method POST -Body $traitData -ContentType "application/json"
            Write-Host "✅ Add Trait: OK" -ForegroundColor Green
            Write-Host "Created trait: $($traitResponse.Name)" -ForegroundColor Gray
        } catch {
            Write-Host "❌ Add Trait: FAILED - $($_.Exception.Message)" -ForegroundColor Red
        }
    }

    # Test 5: Create Conversation
    Write-Host "`n5. Testing Create Conversation..." -ForegroundColor Cyan
    $conversationData = @{
        Title = "Test Conversation"
        Platform = "Web"
        UserId = "test-user"
    } | ConvertTo-Json
    
    try {
        $conversationResponse = Invoke-RestMethod -Uri "$baseUrl/api/conversation" -Method POST -Body $conversationData -ContentType "application/json"
        Write-Host "✅ Create Conversation: OK" -ForegroundColor Green
        Write-Host "Conversation ID: $($conversationResponse.Id)" -ForegroundColor Gray
        $conversationId = $conversationResponse.Id
    } catch {
        Write-Host "❌ Create Conversation: FAILED - $($_.Exception.Message)" -ForegroundColor Red
    }

    # Test 6: Add Message to Conversation
    Write-Host "`n6. Testing Add Message..." -ForegroundColor Cyan
    if ($conversationId) {
        $messageData = @{
            Role = "user"
            Content = "Hello, how are you?"
        } | ConvertTo-Json
        
        try {
            $messageResponse = Invoke-RestMethod -Uri "$baseUrl/api/conversation/$conversationId/messages" -Method POST -Body $messageData -ContentType "application/json"
            Write-Host "✅ Add Message: OK" -ForegroundColor Green
            Write-Host "Message ID: $($messageResponse.Id)" -ForegroundColor Gray
        } catch {
            Write-Host "❌ Add Message: FAILED - $($_.Exception.Message)" -ForegroundColor Red
        }
    }

    # Test 7: Chat Status
    Write-Host "`n7. Testing Chat Status..." -ForegroundColor Cyan
    try {
        $statusResponse = Invoke-RestMethod -Uri "$baseUrl/api/chat/status" -Method GET
        Write-Host "✅ Chat Status: OK" -ForegroundColor Green
        Write-Host "Status: $($statusResponse.Status), MCP Connected: $($statusResponse.McpConnected)" -ForegroundColor Gray
    } catch {
        Write-Host "❌ Chat Status: FAILED - $($_.Exception.Message)" -ForegroundColor Red
    }

    # Test 8: Enhanced Chat with Agent Behavior Engine
    Write-Host "`n8. Testing Enhanced Chat..." -ForegroundColor Cyan
    if ($personalityId) {
        $chatData = @{
            Message = "Привет! Как дела? Расскажи про архитектуру C# проектов."
            Platform = "Web"
            UserId = "test-user-advanced"
        } | ConvertTo-Json
        
        try {
            $chatResponse = Invoke-RestMethod -Uri "$baseUrl/api/chat/send" -Method POST -Body $chatData -ContentType "application/json"
            Write-Host "✅ Enhanced Chat: OK" -ForegroundColor Green
            Write-Host "Response: $($chatResponse.Content.Substring(0, [Math]::Min(100, $chatResponse.Content.Length)))..." -ForegroundColor Gray
            Write-Host "Mood: $($chatResponse.Metadata.mood), Confidence: $($chatResponse.Metadata.confidence)" -ForegroundColor Gray
        } catch {
            Write-Host "❌ Enhanced Chat: FAILED - $($_.Exception.Message)" -ForegroundColor Red
        }
    }

    # Test 9: MCP Tool Integration
    Write-Host "`n9. Testing MCP Tool Integration..." -ForegroundColor Cyan
    if ($personalityId) {
        $toolChatData = @{
            Message = "Расскажи о себе и своих чертах личности"
            Platform = "Web"
            UserId = "test-user-tools"
        } | ConvertTo-Json
        
        try {
            $toolResponse = Invoke-RestMethod -Uri "$baseUrl/api/chat/send" -Method POST -Body $toolChatData -ContentType "application/json"
            Write-Host "✅ MCP Tool Integration: OK" -ForegroundColor Green
            Write-Host "Tools Triggered: $($toolResponse.Metadata.triggered_tools -join ', ')" -ForegroundColor Gray
        } catch {
            Write-Host "❌ MCP Tool Integration: FAILED - $($_.Exception.Message)" -ForegroundColor Red
        }
    }

    Write-Host "`n=== Testing Complete ===" -ForegroundColor Green

} finally {
    # Stop the application
    Write-Host "`nStopping application..." -ForegroundColor Yellow
    Stop-Process -Id $app.Id -Force -ErrorAction SilentlyContinue
}

Write-Host "API testing finished." -ForegroundColor Green