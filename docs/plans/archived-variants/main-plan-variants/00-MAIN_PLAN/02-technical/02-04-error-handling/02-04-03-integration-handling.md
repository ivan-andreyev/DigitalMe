# Integration Error Handling

**Родительский план**: [../02-04-error-handling.md](../02-04-error-handling.md)

## MCP Service Error Handling with Circuit Breaker
**Файл**: `src/DigitalMe.Integrations/MCP/MCPService.cs:66-120` (error handling methods)

```csharp
private async Task<MCPResponse> SendWithRetryAsync(MCPRequest request)
{
    // Circuit breaker pattern - line 68-75
    if (_circuitBreaker.State == CircuitBreakerState.Open)
    {
        _logger.LogWarning("MCP circuit breaker is OPEN, rejecting request");
        throw new MCPException(MCPException.CONNECTION_FAILED, "MCP service is temporarily unavailable");
    }
    
    var policy = Policy
        .Handle<HttpRequestException>()
        .Or<TaskCanceledException>()
        .Or<MCPException>(ex => ex.ErrorCode == MCPException.REQUEST_TIMEOUT)
        .WaitAndRetryAsync(
            retryCount: _options.Value.MaxRetries,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                _logger.LogWarning("MCP request failed, retry {RetryCount}/{MaxRetries} in {Delay}ms: {Error}", 
                    retryCount, _options.Value.MaxRetries, timespan.TotalMilliseconds, outcome.Exception?.Message);
            });

    try
    {
        var response = await policy.ExecuteAsync(async () =>
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_options.Value.TimeoutMs));
            
            // Prepare request - line 90-100
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.Add("Authorization", $"Bearer {_options.Value.ApiKey}");
            
            if (!string.IsNullOrEmpty(_sessionId))
            {
                content.Headers.Add("X-Session-ID", _sessionId);
            }
            
            // Send HTTP request - line 101-115
            try
            {
                var httpResponse = await _httpClient.PostAsync(_options.Value.Endpoint, content, cts.Token);
                
                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorBody = await httpResponse.Content.ReadAsStringAsync(cts.Token);
                    _logger.LogError("MCP HTTP error {StatusCode}: {Body}", httpResponse.StatusCode, errorBody);
                    
                    throw httpResponse.StatusCode switch
                    {
                        HttpStatusCode.Unauthorized => new MCPException(MCPException.SESSION_EXPIRED, 
                            "MCP session expired or invalid API key"),
                        HttpStatusCode.TooManyRequests => new MCPException(MCPException.REQUEST_TIMEOUT, 
                            "MCP rate limit exceeded"),
                        HttpStatusCode.ServiceUnavailable => new MCPException(MCPException.CONNECTION_FAILED, 
                            "MCP service temporarily unavailable"),
                        _ => new MCPException(MCPException.CONNECTION_FAILED, 
                            $"MCP request failed with status {httpResponse.StatusCode}")
                    };
                }
                
                var responseJson = await httpResponse.Content.ReadAsStringAsync(cts.Token);
                var mcpResponse = JsonSerializer.Deserialize<MCPResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                // Validate response - line 116-125
                if (mcpResponse == null)
                {
                    throw new MCPException(MCPException.INVALID_RESPONSE, "Empty response from MCP service");
                }
                
                if (mcpResponse.Error != null)
                {
                    _logger.LogError("MCP protocol error: {ErrorCode} - {ErrorMessage}", 
                        mcpResponse.Error.Code, mcpResponse.Error.Message);
                    throw new MCPException(MCPException.INVALID_RESPONSE, 
                        $"MCP error: {mcpResponse.Error.Message}");
                }
                
                _circuitBreaker.RecordSuccess(); // Circuit breaker success
                return mcpResponse;
            }
            catch (TaskCanceledException) when (cts.Token.IsCancellationRequested)
            {
                throw MCPException.Timeout(_options.Value.TimeoutMs);
            }
        });
        
        return response;
    }
    catch (MCPException)
    {
        _circuitBreaker.RecordFailure(); // Circuit breaker failure
        throw; // Re-throw MCP exceptions as-is
    }
    catch (Exception ex)
    {
        _circuitBreaker.RecordFailure();
        _logger.LogError(ex, "Unexpected error during MCP request");
        throw new MCPException(MCPException.CONNECTION_FAILED, "MCP request failed due to system error", ex);
    }
}
```

## Simple Circuit Breaker Implementation
**Файл**: `src/DigitalMe.Core/Infrastructure/CircuitBreaker.cs:1-60`

```csharp
namespace DigitalMe.Core.Infrastructure;

public enum CircuitBreakerState
{
    Closed,   // Normal operation
    Open,     // Failing, rejecting requests
    HalfOpen  // Testing if service recovered
}

public class CircuitBreaker
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _timeout;
    private int _failureCount;
    private DateTime _lastFailureTime;
    private CircuitBreakerState _state;
    private readonly object _lock = new();
    
    public CircuitBreakerState State => _state;
    
    public CircuitBreaker(int failureThreshold = 5, TimeSpan? timeout = null)
    {
        _failureThreshold = failureThreshold;
        _timeout = timeout ?? TimeSpan.FromMinutes(1);
        _state = CircuitBreakerState.Closed;
    }
    
    public void RecordSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _state = CircuitBreakerState.Closed;
        }
    }
    
    public void RecordFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;
            
            if (_failureCount >= _failureThreshold)
            {
                _state = CircuitBreakerState.Open;
            }
        }
    }
    
    public bool ShouldAttemptRequest()
    {
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitBreakerState.Closed:
                    return true;
                    
                case CircuitBreakerState.Open:
                    if (DateTime.UtcNow - _lastFailureTime > _timeout)
                    {
                        _state = CircuitBreakerState.HalfOpen;
                        return true;
                    }
                    return false;
                    
                case CircuitBreakerState.HalfOpen:
                    return true;
                    
                default:
                    return false;
            }
        }
    }
}
```