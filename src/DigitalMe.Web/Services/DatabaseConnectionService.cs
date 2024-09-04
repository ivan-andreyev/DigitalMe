using Microsoft.Extensions.Configuration;

namespace DigitalMe.Web.Services;

public interface IDatabaseConnectionService
{
    Task<string> GetReadConnectionStringAsync();
    Task<string> GetWriteConnectionStringAsync();
}

public class DatabaseConnectionService : IDatabaseConnectionService
{
    private readonly IConfiguration _config;
    
    public DatabaseConnectionService(IConfiguration config)
    {
        _config = config;
    }
    
    public Task<string> GetReadConnectionStringAsync()
    {
        var connectionString = _config.GetConnectionString("ReadReplica") 
                              ?? _config.GetConnectionString("DefaultConnection")
                              ?? throw new InvalidOperationException("No database connection string configured");
        return Task.FromResult(connectionString);
    }
    
    public Task<string> GetWriteConnectionStringAsync()
    {
        var connectionString = _config.GetConnectionString("DefaultConnection")
                              ?? throw new InvalidOperationException("No default database connection string configured");
        return Task.FromResult(connectionString);
    }
}