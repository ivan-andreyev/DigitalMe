# Task 2: Create IAuditableEntity Interface Extension  
**Duration:** 1.5 hours  
**Focus:** Audit trail contract definition  
**File:** `C:\Sources\DigitalMe\DigitalMe\Data\Contracts\IAuditableEntity.cs`

## Implementation Specification:
```csharp
using System;

namespace DigitalMe.Data.Contracts
{
    /// <summary>
    /// Contract for entities that require audit trail tracking.
    /// Extends IEntity with creation and modification timestamps.
    /// </summary>
    public interface IAuditableEntity : IEntity
    {
        /// <summary>
        /// Timestamp when the entity was first created. Set once during construction.
        /// Uses PostgreSQL timestamptz for timezone-aware storage.
        /// </summary>
        DateTime CreatedAt { get; }
        
        /// <summary>
        /// Timestamp when the entity was last modified. Updated on every change.
        /// Uses PostgreSQL timestamptz for timezone-aware storage.
        /// </summary>
        DateTime UpdatedAt { get; set; }
    }
}
```

## Success Criteria (Measurable):
1. **File Creation Verification:** `Test-Path "C:\Sources\DigitalMe\DigitalMe\Data\Contracts\IAuditableEntity.cs"` returns `True`
2. **Inheritance Check:** Interface extends `IEntity` correctly
3. **Property Count Validation:** Interface defines exactly 2 additional properties beyond IEntity
4. **Compilation Verification:** `dotnet build DigitalMe.csproj` succeeds with no errors

## Prerequisites:
- Task 1 must be completed (IEntity interface exists)