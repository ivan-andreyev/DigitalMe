# Task 4: Implement AuditableBaseEntity Abstract Class
**Duration:** 2 hours  
**Focus:** Auditable entity base implementation  
**File:** `C:\Sources\DigitalMe\DigitalMe\Data\Entities\AuditableBaseEntity.cs`

## Implementation Specification:
```csharp
using System;
using DigitalMe.Data.Contracts;

namespace DigitalMe.Data.Entities
{
    /// <summary>
    /// Base abstract class for entities requiring audit trail functionality.
    /// Extends BaseEntity with creation and modification tracking.
    /// </summary>
    public abstract class AuditableBaseEntity : BaseEntity, IAuditableEntity
    {
        /// <summary>
        /// Timestamp when entity was created. Set once during construction.
        /// Protected setter prevents modification after creation.
        /// </summary>
        public DateTime CreatedAt { get; protected set; }
        
        /// <summary>
        /// Timestamp when entity was last updated. 
        /// Public setter allows modification tracking.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Default constructor initializes audit timestamps to current UTC time.
        /// </summary>
        protected AuditableBaseEntity() : base()
        {
            var now = DateTime.UtcNow;
            CreatedAt = now;
            UpdatedAt = now;
        }

        /// <summary>
        /// Constructor for entity rehydration with existing timestamps.
        /// Used during database materialization.
        /// </summary>
        /// <param name="id">Entity identifier</param>
        /// <param name="createdAt">Original creation timestamp</param>
        /// <param name="updatedAt">Last modification timestamp</param>
        protected AuditableBaseEntity(Guid id, DateTime createdAt, DateTime updatedAt) : base(id)
        {
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        /// <summary>
        /// Updates the modification timestamp to current UTC time.
        /// Should be called before any entity modifications.
        /// </summary>
        public virtual void MarkAsModified()
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
```

## Success Criteria (Measurable):
1. **File Creation Verification:** `Test-Path "C:\Sources\DigitalMe\DigitalMe\Data\Entities\AuditableBaseEntity.cs"` returns `True`
2. **Inheritance Chain:** Class extends BaseEntity and implements IAuditableEntity
3. **Property Count:** Exactly 2 audit properties (CreatedAt, UpdatedAt) beyond BaseEntity
4. **Constructor Validation:** 2 constructors + 1 public method (MarkAsModified)
5. **Property Accessors:** CreatedAt has protected setter, UpdatedAt has public setter
6. **Method Presence:** MarkAsModified method exists and is public virtual
7. **Compilation Success:** `dotnet build DigitalMe.csproj` returns exit code 0

## Prerequisites:
- Tasks 1-3 completed (IEntity, IAuditableEntity, BaseEntity exist)