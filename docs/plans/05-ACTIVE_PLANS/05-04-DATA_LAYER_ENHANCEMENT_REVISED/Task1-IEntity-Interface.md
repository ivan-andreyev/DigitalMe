# Task 1: Create IEntity Interface Contract
**Duration:** 1.5 hours  
**Focus:** Single interface definition  
**File:** `C:\Sources\DigitalMe\DigitalMe\Data\Contracts\IEntity.cs`

## Implementation Specification:
```csharp
using System;

namespace DigitalMe.Data.Contracts
{
    /// <summary>
    /// Base contract for all entities in the DigitalMe system.
    /// Defines the minimum required properties for entity identification and tracking.
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// Unique identifier for the entity. Generated as GUID for PostgreSQL compatibility.
        /// </summary>
        Guid Id { get; }
    }
}
```

## Success Criteria (Measurable):
1. **File Creation Verification:** `Test-Path "C:\Sources\DigitalMe\DigitalMe\Data\Contracts\IEntity.cs"` returns `True`
2. **Compilation Check:** `dotnet build DigitalMe.csproj --verbosity quiet` returns exit code 0
3. **Interface Structure Validation:** File contains exactly 1 interface definition with 1 property
4. **Namespace Verification:** Interface resides in `DigitalMe.Data.Contracts` namespace

## Prerequisites:
- Create `Data\Contracts\` directory if not exists
- Ensure project compiles before starting