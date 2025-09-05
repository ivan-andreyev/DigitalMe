# Database Error Handling

**Родительский план**: [../02-04-error-handling.md](../02-04-error-handling.md)

## Repository Error Handling with Specific Exception Mapping
**Файл**: `src/DigitalMe.Data/Repositories/PersonalityRepository.cs:15-40` (error handling)

```csharp
public async Task<PersonalityProfile?> GetByNameAsync(string name)
{
    try
    {
        _logger.LogDebug("Fetching personality profile: {Name}", name);
        
        var profile = await _context.PersonalityProfiles
            .Include(p => p.PersonalityTraits)
            .AsNoTracking() // Performance optimization for read-only
            .FirstOrDefaultAsync(p => p.Name == name);
            
        return profile;
    }
    catch (PostgresException pgEx) when (pgEx.SqlState == "08000") // Connection failure
    {
        _logger.LogError(pgEx, "Database connection failed for profile: {Name}", name);
        throw new InvalidOperationException("Database connection failed", pgEx);
    }
    catch (PostgresException pgEx) when (pgEx.SqlState == "57014") // Query timeout  
    {
        _logger.LogError(pgEx, "Database query timeout for profile: {Name}", name);
        throw new TimeoutException("Database query timeout", pgEx);
    }
    catch (PostgresException pgEx) when (pgEx.SqlState.StartsWith("23")) // Constraint violation
    {
        _logger.LogError(pgEx, "Database constraint violation for profile: {Name}", name);
        throw new InvalidOperationException($"Data integrity violation: {pgEx.MessageText}", pgEx);
    }
    catch (DbUpdateException dbEx)
    {
        _logger.LogError(dbEx, "Entity Framework error for profile: {Name}", name);
        throw new InvalidOperationException("Database update failed", dbEx);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected database error for profile: {Name}", name);
        throw new InvalidOperationException($"Database operation failed: {ex.Message}", ex);
    }
}
```

## Database Connection Health Monitoring
**Файл**: `src/DigitalMe.API/HealthChecks/DatabaseHealthCheck.cs:1-30`

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using DigitalMe.Data.Context;

namespace DigitalMe.API.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly DigitalMeContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;
    
    public DatabaseHealthCheck(DigitalMeContext context, ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple connection test
            await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            
            _logger.LogDebug("Database health check passed");
            return HealthCheckResult.Healthy("Database connection is healthy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}
```

## PostgreSQL Specific Error Codes
**Reference**: Common PostgreSQL error states for exception handling

```csharp
// Connection errors
"08000" - Connection exception (general)
"08003" - Connection does not exist
"08006" - Connection failure
"08001" - SQL client unable to establish SQL connection
"08004" - SQL server rejected establishment of SQL connection

// Query execution errors  
"57014" - Query cancelled (timeout)
"53300" - Too many connections
"53400" - Configuration limit exceeded

// Data integrity errors
"23000" - Integrity constraint violation (general)
"23001" - Restrict violation
"23502" - Not null violation
"23503" - Foreign key violation
"23505" - Unique violation
"23514" - Check violation

// Transaction errors
"25000" - Invalid transaction state
"25001" - Active SQL transaction
"25002" - Branch transaction already active
"25008" - Held cursor requires same isolation level
```

## Error Recovery Strategies

### Connection Pool Recovery
```csharp
// In Program.cs - connection pool configuration for resilience
builder.Services.AddDbContext<DigitalMeContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.CommandTimeout(30);
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
    
    // Connection pooling settings
    options.EnableServiceProviderCaching();
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
});
```

### Transaction Retry Pattern
```csharp
public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    
    try
    {
        var result = await operation();
        await transaction.CommitAsync();
        return result;
    }
    catch (PostgresException pgEx) when (pgEx.SqlState == "40001") // Serialization failure
    {
        await transaction.RollbackAsync();
        _logger.LogWarning("Transaction serialization failure, retrying...");
        
        // Retry with exponential backoff
        await Task.Delay(Random.Next(100, 500));
        return await ExecuteInTransactionAsync(operation);
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```