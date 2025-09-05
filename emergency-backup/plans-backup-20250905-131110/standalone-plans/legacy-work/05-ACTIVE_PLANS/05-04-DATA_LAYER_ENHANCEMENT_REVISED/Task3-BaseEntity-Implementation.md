# Task 3: Implement BaseEntity Abstract Class
**Duration:** 2 hours  
**Focus:** Core base entity implementation  
**File:** `C:\Sources\DigitalMe\DigitalMe\Data\Entities\BaseEntity.cs`

## Implementation Specification:
```csharp
using System;
using DigitalMe.Data.Contracts;

namespace DigitalMe.Data.Entities
{
    /// <summary>
    /// Base abstract class for all entities requiring basic identification.
    /// Provides GUID-based identity with PostgreSQL compatibility.
    /// </summary>
    public abstract class BaseEntity : IEntity
    {
        /// <summary>
        /// Unique identifier generated as GUID. Protected setter prevents external modification.
        /// </summary>
        public Guid Id { get; protected set; } = Guid.NewGuid();

        /// <summary>
        /// Protected constructor ensures proper initialization of derived classes.
        /// </summary>
        protected BaseEntity()
        {
            // Id is auto-initialized via property initializer
        }

        /// <summary>
        /// Constructor for entity rehydration from database with existing ID.
        /// </summary>
        /// <param name="id">Existing entity identifier</param>
        protected BaseEntity(Guid id)
        {
            Id = id;
        }
    }
}
```

## Success Criteria (Measurable):
1. **File Creation Verification:** `Test-Path "C:\Sources\DigitalMe\DigitalMe\Data\Entities\BaseEntity.cs"` returns `True`
2. **Abstract Class Validation:** Class is marked as abstract
3. **Interface Implementation Check:** Class implements IEntity interface
4. **Constructor Count:** Exactly 2 constructors defined (default and with id parameter)
5. **Property Accessibility:** Id property has public getter and protected setter
6. **Compilation Success:** `dotnet build DigitalMe.csproj` returns exit code 0

## Prerequisites:
- Task 1 completed (IEntity interface exists)
- Create `Data\Entities\` directory if not exists