# EF Core DbContext Implementation

**Родительский план**: [../02-01-database-design.md](../02-01-database-design.md)

## Overview
Implementation specifications for Entity Framework Core DbContext with PostgreSQL JSONB support and comprehensive entity relationships for the DigitalMe platform.

## DbContext Implementation

### Primary DbContext Class
**File**: `src/DigitalMe.Data/Context/DigitalMeContext.cs`

```csharp
public class DigitalMeContext : DbContext
{
    public DigitalMeContext(DbContextOptions<DigitalMeContext> options) : base(options) { }

    // Core Personality System
    public DbSet<PersonalityProfile> PersonalityProfiles { get; set; }
    public DbSet<PersonalityTrait> PersonalityTraits { get; set; }
    
    // Communication System
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    
    // External Integrations
    public DbSet<TelegramMessage> TelegramMessages { get; set; }
    public DbSet<CalendarEvent> CalendarEvents { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<GitHubRepository> GitHubRepositories { get; set; }
    public DbSet<EmailMessage> EmailMessages { get; set; }
    
    // System Monitoring
    public DbSet<AgentAction> AgentActions { get; set; }
    public DbSet<IntegrationLog> IntegrationLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure JSONB support for PostgreSQL
        modelBuilder.HasPostgresExtension("uuid-ossp");
        
        // Apply entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DigitalMeContext).Assembly);
        
        base.OnModelCreating(modelBuilder);
    }
}
```

## Configuration Requirements

### Connection String Configuration
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=digitalme;Username=postgres;Password=dev123;Include Error Detail=true;Pooling=true;MinPoolSize=5;MaxPoolSize=20;"
  }
}
```

### PostgreSQL Extensions
```sql
-- Required PostgreSQL extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
```

## Migration Strategy

### Initial Migration
```bash
# Create initial migration
dotnet ef migrations add InitialCreate --project src/DigitalMe.Data --startup-project src/DigitalMe.API

# Update database
dotnet ef database update --project src/DigitalMe.Data --startup-project src/DigitalMe.API
```

### Migration Naming Convention
- Use descriptive names: `Add_PersonalityTraits_Table`
- Include timestamp for ordering: `20240901_Add_JSONB_Support`
- Version increments: `V1_1_PersonalitySystem`, `V1_2_IntegrationLogs`

## Health Check Integration

### Database Health Check
```csharp
services.AddHealthChecks()
    .AddDbContextCheck<DigitalMeContext>("database", 
        customTestQuery: async (context, cancellationToken) =>
        {
            await context.PersonalityProfiles.FirstOrDefaultAsync(cancellationToken);
            return HealthCheckResult.Healthy("Database connection successful");
        });
```

## Performance Optimizations

### Connection Pooling
- **MinPoolSize**: 5 connections
- **MaxPoolSize**: 20 connections  
- **Connection Lifetime**: 300 seconds
- **Command Timeout**: 30 seconds

### Indexing Strategy
- Primary keys: Clustered indexes on all Id columns
- Foreign keys: Non-clustered indexes on relationship columns
- JSON queries: GIN indexes on JSONB columns for trait queries
- Search optimization: Full-text search indexes on content columns

## Success Criteria
- [ ] DbContext compiles without errors
- [ ] All entity relationships properly configured
- [ ] PostgreSQL connection established successfully
- [ ] Health check endpoint returns success
- [ ] Initial migration creates all required tables
- [ ] JSONB support functional for personality traits

## Navigation
- **Parent**: [Database Design Specification](../02-01-database-design.md)
- **Next**: [Entity Models Configuration](./02-01-02-entity-models.md)
- **Related**: [Migration Scripts](./02-01-03-migrations.md)