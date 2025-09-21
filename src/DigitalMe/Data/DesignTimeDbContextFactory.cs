using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DigitalMe.Data;

/// <summary>
/// Design-time factory for creating DbContext instances during EF migrations
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DigitalMeDbContext>
{
    public DigitalMeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DigitalMeDbContext>();

        // Default to SQLite for development migrations
        var connectionString = "Data Source=digitalme.db";

        // Check for PostgreSQL connection string in arguments
        if (args.Length > 0 && args[0].StartsWith("--connection"))
        {
            connectionString = args[0].Substring("--connection=".Length);
        }

        // Auto-detect provider based on connection string
        if (connectionString.Contains("Host=") || connectionString.Contains("/cloudsql/") || connectionString.Contains("Server="))
        {
            // PostgreSQL
            optionsBuilder.UseNpgsql(connectionString);
        }
        else
        {
            // SQLite
            optionsBuilder.UseSqlite(connectionString);
        }

        return new DigitalMeDbContext(optionsBuilder.Options);
    }
}