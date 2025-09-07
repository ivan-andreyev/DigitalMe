# Database Migration Procedures

> **STATUS**: ✅ **ACTIVE** - Updated 2025-09-07  
> **SCOPE**: Development & Production Migration Guidelines  
> **PURPOSE**: Prevent migration synchronization issues and regression

---

## 🎯 OVERVIEW

This document outlines standardized procedures for managing Entity Framework Core database migrations in the DigitalMe project. These procedures prevent migration synchronization issues, ensure data consistency, and provide recovery mechanisms for common problems.

## 🚨 CRITICAL PRINCIPLES

### **1. NEVER DELETE MIGRATIONS IN PRODUCTION**
- ❌ **NEVER** delete migration files that have been applied to production
- ❌ **NEVER** modify existing migration files after they've been applied
- ✅ **ALWAYS** create new migrations to fix issues

### **2. MIGRATION SYNCHRONIZATION**
- Database state MUST match migration history in `__EFMigrationsHistory`
- Any mismatch can cause production failures
- Recovery procedures are environment-specific

### **3. TESTING REQUIREMENTS**
- Test migrations in development BEFORE production deployment
- Verify rollback procedures work correctly
- Validate data integrity after migration

---

## 🛠️ STANDARD PROCEDURES

### **Creating New Migrations**

```bash
# Navigate to project root
cd C:/Sources/DigitalMe

# Create new migration
dotnet ef migrations add MigrationName --project DigitalMe

# Review generated migration files
# - DigitalMe/Migrations/{timestamp}_MigrationName.cs
# - DigitalMe/Migrations/DigitalMeDbContextModelSnapshot.cs

# Apply migration to development database
dotnet ef database update --project DigitalMe
```

### **Applying Migrations**

```bash
# Check current migration status
dotnet ef migrations list --project DigitalMe

# Apply all pending migrations
dotnet ef database update --project DigitalMe

# Apply specific migration
dotnet ef database update MigrationName --project DigitalMe
```

### **Reverting Migrations** ⚠️ **DANGEROUS**

```bash
# Revert to specific migration (removes newer migrations)
dotnet ef database update PreviousMigrationName --project DigitalMe

# Remove last migration (if not applied to production)
dotnet ef migrations remove --project DigitalMe --force
```

---

## 🔧 SYNCHRONIZATION ISSUE RECOVERY

### **Problem**: Database exists but migration history is out of sync

**Symptoms:**
- `SQLite Error 1: 'table XYZ already exists'`
- Migration history shows incorrect state
- Application fails to start

### **Solution 1: Clean Recreation (Development Only)**

```bash
# DEVELOPMENT ONLY - NEVER IN PRODUCTION
cd C:/Sources/DigitalMe/DigitalMe
rm -f digitalme.db*

# Remove and recreate migration
cd ..
dotnet ef migrations remove --project DigitalMe --force
dotnet ef migrations add InitialClean --project DigitalMe
dotnet ef database update --project DigitalMe
```

### **Solution 2: Manual History Sync (Production)**

```sql
-- Check migration history
SELECT * FROM __EFMigrationsHistory;

-- If missing, manually insert migration record
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20250906225126_Initial', '8.0.8');
```

### **Solution 3: Automated Recovery (Implemented in Program.cs)**

The application now includes automatic detection and recovery:

- **Detection**: Compares applied vs pending vs total migrations
- **Logging**: Detailed migration status logging
- **Recovery**: Environment-specific recovery strategies
- **Verification**: Post-migration consistency checks

---

## 🏗️ ENHANCED MIGRATION HANDLING

### **Automatic Features in Program.cs**

1. **Migration History Consistency Check**
   ```csharp
   var appliedMigrations = context.Database.GetAppliedMigrations().ToList();
   var pendingMigrations = context.Database.GetPendingMigrations().ToList();
   var allMigrations = context.Database.GetMigrations().ToList();
   ```

2. **Gap Detection**
   ```csharp
   var hasGapInHistory = appliedMigrations.Count + pendingMigrations.Count != allMigrations.Count;
   ```

3. **Environment-Specific Recovery**
   ```csharp
   if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Testing")
   {
       // Aggressive recovery for development
       context.Database.Migrate();
   }
   ```

4. **Verification After Apply**
   ```csharp
   var remainingPending = context.Database.GetPendingMigrations().ToList();
   if (remainingPending.Any())
   {
       logger.LogWarning("Some migrations still pending: {Pending}", remainingPending);
   }
   ```

---

## 🎯 ENVIRONMENT-SPECIFIC PROCEDURES

### **Development Environment**

- ✅ Safe to delete and recreate database
- ✅ Safe to remove/recreate migrations
- ✅ Automatic recovery enabled
- ❌ Never use production data

```bash
# Quick reset for development
rm -f DigitalMe/digitalme.db*
dotnet ef migrations remove --project DigitalMe --force
dotnet ef migrations add Fresh --project DigitalMe  
dotnet ef database update --project DigitalMe
```

### **Testing Environment**

- ✅ Use InMemory database for unit tests
- ✅ Clean database for each test run
- ✅ Seed test data programmatically
- ❌ Never persist test data

```csharp
// Test database configuration
if (context.Database.ProviderName?.Contains("InMemory") == true)
{
    context.Database.EnsureCreated(); // Skip migrations for InMemory
}
```

### **Production Environment**

- ❌ **NEVER** delete migration files
- ❌ **NEVER** delete database
- ✅ Always backup before migrations
- ✅ Test migrations on staging first
- ✅ Manual intervention required for sync issues

```bash
# Production migration process
# 1. Backup database
# 2. Test on staging
# 3. Apply during maintenance window
# 4. Verify application health
```

---

## 📋 TROUBLESHOOTING CHECKLIST

### **Before Migration**
- [ ] Database backup created (production)
- [ ] Migration tested on staging
- [ ] Application health checks pass
- [ ] Rollback procedure documented

### **During Migration**
- [ ] Monitor application logs
- [ ] Watch for migration errors
- [ ] Verify database connectivity
- [ ] Check migration history consistency

### **After Migration**
- [ ] Application starts successfully
- [ ] Health checks pass
- [ ] Data seeding completes
- [ ] No pending migrations remain
- [ ] Performance metrics normal

### **If Migration Fails**
- [ ] Review error logs
- [ ] Check database connectivity
- [ ] Verify migration file integrity
- [ ] Consider rollback if critical
- [ ] Document issue for future prevention

---

## 🔍 MONITORING & LOGGING

### **Key Log Messages**

```
✅ STEP 15: Database is up to date - no migrations to apply
🔄 STEP 15: Applying {Count} database migrations...
✅ STEP 16: Database migrations applied successfully!
⚠️ MIGRATION SYNCHRONIZATION ISSUE DETECTED
❌ Migration check failed: {ErrorMessage}
🔧 Attempting SQLite synchronization recovery...
```

### **Health Check Integration**

The application includes migration status in health checks:

```http
GET /health
{
  "databaseMigrations": {
    "status": "Healthy",
    "appliedCount": 1,
    "pendingCount": 0,
    "lastMigration": "20250907_InitialClean"
  }
}
```

---

## 📚 REFERENCE COMMANDS

### **Essential EF Core Commands**
```bash
# List all migrations
dotnet ef migrations list --project DigitalMe

# Get migration status
dotnet ef database list --project DigitalMe

# Generate SQL script for migrations
dotnet ef migrations script --project DigitalMe

# Create migration bundle for deployment
dotnet ef migrations bundle --project DigitalMe
```

### **Database Information**
```bash
# Check database info
dotnet ef dbcontext info --project DigitalMe

# Generate DbContext scaffold
dotnet ef dbcontext scaffold "Data Source=digitalme.db" Microsoft.EntityFrameworkCore.Sqlite
```

---

## 🎉 SUCCESS CRITERIA

### **Healthy Migration State**
- ✅ No pending migrations
- ✅ Database schema matches model
- ✅ Migration history is consistent
- ✅ Application starts without errors
- ✅ Data seeding completes successfully

### **Recovery Success**
- ✅ Database operations work correctly
- ✅ No data loss occurred
- ✅ Application performance normal
- ✅ Future migrations apply cleanly

---

**Last Updated**: 2025-09-07  
**Status**: ✅ **ACTIVE** - Enhanced migration handling implemented  
**Next Review**: When migration issues occur or new EF Core version adopted