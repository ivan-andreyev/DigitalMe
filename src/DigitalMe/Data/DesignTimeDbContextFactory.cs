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

        // Default to PostgreSQL for development migrations
        var connectionString = "Host=localhost;Database=digitalme_dev;Username=postgres;Password=postgres";

        // Check for PostgreSQL connection string in arguments
        if (args.Length > 0 && args[0].StartsWith("--connection"))
        {
            connectionString = args[0].Substring("--connection=".Length);
        }

        // Always use PostgreSQL
        optionsBuilder.UseNpgsql(connectionString);

        return new DigitalMeDbContext(optionsBuilder.Options);
    }
}