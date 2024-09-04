# Task 7: Update Entity Framework Configuration
**Duration:** 2.5 hours  
**Focus:** Configure base entity types in DbContext  
**Files Modified:** `C:\Sources\DigitalMe\DigitalMe\Data\DigitalMeDbContext.cs`

## Current Code Analysis:
- **Lines 24-28:** PersonalityProfile GUID configuration (MODIFY to use base entity config)
- **Lines 30-33:** PersonalityTrait GUID configuration (MODIFY to use base entity config)
- **Lines 72-79:** PersonalityProfile DateTime configurations (MODIFY)
- **Lines 81-84:** PersonalityTrait DateTime configurations (MODIFY)

## Implementation Specification:

**ADD at line 2 (after existing usings):**
```csharp
using DigitalMe.Data.Entities;
using DigitalMe.Data.Contracts;
```

**ADD new method after line 148 (before closing class bracket):**
```csharp
    /// <summary>
    /// Configures base entity types for consistent GUID and timestamp handling.
    /// Called automatically by OnModelCreating for all entity types.
    /// </summary>
    /// <typeparam name="T">Entity type implementing IEntity</typeparam>
    /// <param name="builder">Entity type builder</param>
    private void ConfigureBaseEntity<T>(EntityTypeBuilder<T> builder) where T : class, IEntity
    {
        builder.Property(e => e.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");
    }

    /// <summary>
    /// Configures auditable entity types for timestamp handling.
    /// Called automatically by OnModelCreating for auditable entity types.
    /// </summary>
    /// <typeparam name="T">Entity type implementing IAuditableEntity</typeparam>
    /// <param name="builder">Entity type builder</param>
    private void ConfigureAuditableEntity<T>(EntityTypeBuilder<T> builder) where T : class, IAuditableEntity
    {
        ConfigureBaseEntity(builder);
        
        builder.Property(e => e.CreatedAt)
            .HasColumnType("timestamptz");
            
        builder.Property(e => e.UpdatedAt)
            .HasColumnType("timestamptz");
    }
```

**REPLACE lines 24-33 with:**
```csharp
        // Configure base entity types
        ConfigureAuditableEntity(modelBuilder.Entity<PersonalityProfile>());
        ConfigureBaseEntity(modelBuilder.Entity<PersonalityTrait>());
            
        modelBuilder.Entity<PersonalityTrait>()
            .Property(e => e.PersonalityProfileId)
            .HasColumnType("uuid");
```

**REPLACE lines 72-84 with:**
```csharp
        // PersonalityTrait CreatedAt field (not auditable, so manual config)
        modelBuilder.Entity<PersonalityTrait>()
            .Property(e => e.CreatedAt)
            .HasColumnType("timestamptz");
```

## Success Criteria (Measurable):
1. **Method Addition:** 2 new private methods (ConfigureBaseEntity, ConfigureAuditableEntity) exist
2. **Using Statements:** Both new using statements added at file top
3. **Configuration Calls:** PersonalityProfile uses ConfigureAuditableEntity
4. **Configuration Calls:** PersonalityTrait uses ConfigureBaseEntity + manual CreatedAt config
5. **Line Count Reduction:** OnModelCreating method has approximately 15 fewer lines
6. **Compilation Success:** `dotnet build DigitalMe.csproj` returns exit code 0
7. **Migration Generation:** `dotnet ef migrations add BaseEntityRefactor` succeeds

## Prerequisites:
- Tasks 1-6 completed (all entities refactored)