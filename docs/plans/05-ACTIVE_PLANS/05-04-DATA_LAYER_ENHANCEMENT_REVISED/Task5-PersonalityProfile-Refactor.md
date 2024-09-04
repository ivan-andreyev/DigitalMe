# Task 5: Refactor PersonalityProfile Entity
**Duration:** 2 hours  
**Focus:** Convert PersonalityProfile to use AuditableBaseEntity  
**Files Modified:** `C:\Sources\DigitalMe\DigitalMe\Models\PersonalityProfile.cs`

## Current Code Analysis:
- **Lines 8-9:** `public Guid Id { get; set; }` (REMOVE)
- **Lines 20-22:** `public DateTime CreatedAt/UpdatedAt { get; set; }` (REMOVE)  
- **Lines 28-31:** Constructor with Id/timestamp initialization (MODIFY)
- **Line 6:** Class declaration (ADD inheritance)

## Before/After Implementation:

**BEFORE (lines 6-31):**
```csharp
public class PersonalityProfile
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;
    
    public string Traits { get; set; } = "{}";
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public virtual ICollection<PersonalityTrait> PersonalityTraits { get; set; } = new List<PersonalityTrait>();
    
    public PersonalityProfile()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

**AFTER (lines 6-24):**
```csharp
public class PersonalityProfile : AuditableBaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;
    
    public string Traits { get; set; } = "{}";
    
    public virtual ICollection<PersonalityTrait> PersonalityTraits { get; set; } = new List<PersonalityTrait>();
    
    public PersonalityProfile() : base()
    {
        // Base constructor handles Id, CreatedAt, UpdatedAt initialization
    }

    public PersonalityProfile(string name, string description = "", string traits = "{}") : base()
    {
        Name = name;
        Description = description;
        Traits = traits;
    }
}
```

## Required File Modifications:
1. **Line 1:** Add `using DigitalMe.Data.Entities;`
2. **Line 6:** Change `public class PersonalityProfile` to `public class PersonalityProfile : AuditableBaseEntity`
3. **Lines 8-9:** Remove `[Key]` attribute and `public Guid Id { get; set; }`
4. **Lines 20-22:** Remove `public DateTime CreatedAt { get; set; }` and `public DateTime UpdatedAt { get; set; }`
5. **Lines 26-31:** Replace constructor content with base() call and add convenience constructor

## Success Criteria (Measurable):
1. **Inheritance Verification:** Class extends AuditableBaseEntity
2. **Property Removal Confirmation:** Id, CreatedAt, UpdatedAt properties removed from class
3. **Constructor Modification:** Base constructor calls properly implemented
4. **Line Count Reduction:** File has approximately 7 fewer lines than original
5. **Compilation Success:** `dotnet build DigitalMe.csproj` returns exit code 0
6. **No Key Attribute:** [Key] attribute removed (inherited from base)

## Prerequisites:
- Tasks 1-4 completed (all base classes and interfaces exist)