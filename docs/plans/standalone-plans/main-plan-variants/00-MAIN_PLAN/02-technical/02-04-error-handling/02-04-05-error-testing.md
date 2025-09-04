# Validation and Error Testing

**Родительский план**: [../02-04-error-handling.md](../02-04-error-handling.md)

## Error Handling Unit Tests
**Файл**: `tests/DigitalMe.Tests.Unit/ErrorHandlingTests.cs:1-80`

```csharp
namespace DigitalMe.Tests.Unit;

public class ErrorHandlingTests
{
    [Fact]
    public async Task PersonalityService_GetPersonalityAsync_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var mockRepo = new Mock<IPersonalityRepository>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new PersonalityService(mockRepo.Object, NullLogger<PersonalityService>.Instance, cache);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.GetPersonalityAsync(""));
        Assert.Equal("name", exception.ParamName);
        Assert.Contains("cannot be empty", exception.Message);
    }
    
    [Fact]
    public async Task PersonalityService_GetPersonalityAsync_WithNonExistentProfile_ThrowsPersonalityServiceException()
    {
        // Arrange
        var mockRepo = new Mock<IPersonalityRepository>();
        mockRepo.Setup(r => r.GetByNameAsync("NonExistent")).ReturnsAsync((PersonalityProfile?)null);
        
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new PersonalityService(mockRepo.Object, NullLogger<PersonalityService>.Instance, cache);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<PersonalityServiceException>(() => service.GetPersonalityAsync("NonExistent"));
        Assert.Equal(PersonalityServiceException.PROFILE_NOT_FOUND, exception.ErrorCode);
        Assert.Contains("NonExistent", exception.Message);
    }
    
    [Fact]
    public async Task MCPService_SendWithRetryAsync_WithTimeout_ThrowsMCPException()
    {
        // Arrange
        var httpClient = new HttpClient(new TimeoutHttpMessageHandler(TimeSpan.FromMilliseconds(1)));
        var options = Options.Create(new MCPOptions { TimeoutMs = 100, MaxRetries = 1 });
        var service = new MCPService(httpClient, options, NullLogger<MCPService>.Instance, null);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<MCPException>(() => 
            service.SendMessageAsync("test", new PersonalityContext()));
        Assert.Equal(MCPException.REQUEST_TIMEOUT, exception.ErrorCode);
    }
}

// Mock HTTP handler for testing timeouts
public class TimeoutHttpMessageHandler : HttpMessageHandler
{
    private readonly TimeSpan _delay;
    
    public TimeoutHttpMessageHandler(TimeSpan delay)
    {
        _delay = delay;
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Task.Delay(_delay, cancellationToken);
        throw new TaskCanceledException("Request timeout");
    }
}
```

## Integration Error Tests
**Файл**: `tests/DigitalMe.Tests.Integration/ErrorHandlingIntegrationTests.cs:1-60`

```csharp
namespace DigitalMe.Tests.Integration;

public class ErrorHandlingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public ErrorHandlingIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task API_GetPersonality_WithEmptyName_Returns400BadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/personality/");
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        Assert.NotNull(errorResponse);
        Assert.Equal("INVALID_ARGUMENT", errorResponse.ErrorCode);
        Assert.Contains("empty", errorResponse.Message);
    }
    
    [Fact]
    public async Task API_GetPersonality_WithNonExistentProfile_Returns404NotFound()
    {
        // Act  
        var response = await _client.GetAsync("/api/personality/NonExistentProfile");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        Assert.NotNull(errorResponse);
        Assert.Equal(PersonalityServiceException.PROFILE_NOT_FOUND, errorResponse.ErrorCode);
        Assert.Contains("NonExistentProfile", errorResponse.Message);
    }
}
```

## Error Testing Commands and Validation

**Команды для проверки error handling**:
```bash
# Test exceptions handling
dotnet test tests/DigitalMe.Tests.Unit/ErrorHandlingTests.cs --logger console

# Test API error responses  
curl -X POST "http://localhost:5000/api/personality/get" -H "Content-Type: application/json" -d '{"name":""}'
# Expected: HTTP 400, {"errorCode":"INVALID_ARGUMENT","message":"Profile name cannot be empty"}

# Test database connection error
# (Stop postgres and make request)
curl -X GET "http://localhost:5000/api/personality/Ivan"  
# Expected: HTTP 500, {"errorCode":"INTERNAL_SERVER_ERROR"}
```

## Circuit Breaker Testing
**Файл**: `tests/DigitalMe.Tests.Unit/CircuitBreakerTests.cs:1-40`

```csharp
public class CircuitBreakerTests
{
    [Fact]
    public void CircuitBreaker_InitialState_IsClosed()
    {
        // Arrange & Act
        var circuitBreaker = new CircuitBreaker(failureThreshold: 3);
        
        // Assert
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
        Assert.True(circuitBreaker.ShouldAttemptRequest());
    }
    
    [Fact]
    public void CircuitBreaker_AfterThresholdFailures_OpensCircuit()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(failureThreshold: 3);
        
        // Act - Record failures to reach threshold
        circuitBreaker.RecordFailure();
        circuitBreaker.RecordFailure();
        circuitBreaker.RecordFailure();
        
        // Assert
        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);
        Assert.False(circuitBreaker.ShouldAttemptRequest());
    }
    
    [Fact]
    public void CircuitBreaker_AfterTimeout_TransitionsToHalfOpen()
    {
        // Arrange
        var shortTimeout = TimeSpan.FromMilliseconds(100);
        var circuitBreaker = new CircuitBreaker(failureThreshold: 1, timeout: shortTimeout);
        
        // Act - Trip circuit breaker
        circuitBreaker.RecordFailure();
        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);
        
        // Wait for timeout
        Thread.Sleep(150);
        
        // Assert - Should transition to half-open
        Assert.True(circuitBreaker.ShouldAttemptRequest());
    }
}
```

## Performance and Load Error Testing
**Файл**: `tests/DigitalMe.Tests.Load/ErrorHandlingLoadTests.cs:1-30`

```csharp
public class ErrorHandlingLoadTests
{
    [Fact]
    public async Task ErrorHandling_UnderLoad_DoesNotLeakResources()
    {
        // Arrange
        var factory = new WebApplicationFactory<Program>();
        var clients = Enumerable.Range(0, 50).Select(_ => factory.CreateClient()).ToList();
        
        // Act - Send concurrent requests that will fail
        var tasks = clients.Select(async client =>
        {
            try
            {
                await client.GetAsync("/api/personality/NonExistent");
            }
            catch
            {
                // Expected to fail
            }
        }).ToArray();
        
        await Task.WhenAll(tasks);
        
        // Assert - Verify server is still responsive
        using var testClient = factory.CreateClient();
        var healthResponse = await testClient.GetAsync("/health");
        
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        
        // Cleanup
        foreach (var client in clients)
        {
            client.Dispose();
        }
        factory.Dispose();
    }
}
```