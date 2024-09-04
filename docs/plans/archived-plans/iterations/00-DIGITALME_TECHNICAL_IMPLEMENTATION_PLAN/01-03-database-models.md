# 01-03-database-models.md

## Complete Entity Framework Database Models

### Overview
Exact Entity Framework Core model definitions with all properties, relationships, constraints, and navigation properties. Every entity includes complete validation attributes, foreign key definitions, and index specifications.

---

## 1. Core Profile Entities

### 1.1 Profile Entity

```csharp
[Table("Profiles")]
[Index(nameof(Name), IsUnique = true)]
[Index(nameof(CreatedAt))]
public class Profile
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [StringLength(100, MinimumLength = 2)]
    [Column(TypeName = "varchar(100)")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    [Column(TypeName = "varchar(500)")]
    public string? Description { get; set; }
    
    [Required]
    [Column(TypeName = "varchar(50)")]
    public string Status { get; set; } = "Active"; // Active, Inactive, Archived
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    [StringLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string? CreatedBy { get; set; }
    
    [StringLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string? LastUpdatedBy { get; set; }
    
    [Required]
    public int Version { get; set; } = 1;
    
    // Navigation Properties
    public virtual ICollection<PersonalityTrait> PersonalityTraits { get; set; } = new List<PersonalityTrait>();
    public virtual ICollection<Memory> Memories { get; set; } = new List<Memory>();
    public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public virtual ICollection<ProfileAnalytics> Analytics { get; set; } = new List<ProfileAnalytics>();
    public virtual ICollection<InterviewSession> InterviewSessions { get; set; } = new List<InterviewSession>();
}
```

### 1.2 PersonalityTrait Entity

```csharp
[Table("PersonalityTraits")]
[Index(nameof(ProfileId), nameof(Category), nameof(Name), IsUnique = true)]
[Index(nameof(Category))]
[Index(nameof(LastUpdated))]
public class PersonalityTrait
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [ForeignKey(nameof(Profile))]
    public Guid ProfileId { get; set; }
    
    [Required]
    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string Category { get; set; } = string.Empty; // cognitive, behavioral, values, preferences, communication
    
    [Required]
    [StringLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "text")]
    public string Value { get; set; } = string.Empty;
    
    [Required]
    [Range(0.0, 1.0)]
    [Column(TypeName = "decimal(3,2)")]
    public decimal ConfidenceScore { get; set; } = 0.8m;
    
    [StringLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string? Source { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    [StringLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string? UpdatedBy { get; set; }
    
    [Required]
    public int Version { get; set; } = 1;
    
    // Navigation Properties
    public virtual Profile Profile { get; set; } = null!;
    public virtual ICollection<TraitHistory> History { get; set; } = new List<TraitHistory>();
}
```

### 1.3 TraitHistory Entity

```csharp
[Table("TraitHistory")]
[Index(nameof(PersonalityTraitId), nameof(CreatedAt))]
[Index(nameof(CreatedAt))]
public class TraitHistory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [ForeignKey(nameof(PersonalityTrait))]
    public Guid PersonalityTraitId { get; set; }
    
    [Column(TypeName = "text")]
    public string? OldValue { get; set; }
    
    [Required]
    [Column(TypeName = "text")]
    public string NewValue { get; set; } = string.Empty;
    
    [Range(0.0, 1.0)]
    [Column(TypeName = "decimal(3,2)")]
    public decimal? OldConfidenceScore { get; set; }
    
    [Required]
    [Range(0.0, 1.0)]
    [Column(TypeName = "decimal(3,2)")]
    public decimal NewConfidenceScore { get; set; }
    
    [StringLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string? Source { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string? CreatedBy { get; set; }
    
    [StringLength(500)]
    [Column(TypeName = "varchar(500)")]
    public string? ChangeReason { get; set; }
    
    // Navigation Properties
    public virtual PersonalityTrait PersonalityTrait { get; set; } = null!;
}
```

---

## 2. Memory Management Entities

### 2.1 Memory Entity

```csharp
[Table("Memories")]
[Index(nameof(ProfileId), nameof(CreatedAt))]
[Index(nameof(MemoryType))]
[Index(nameof(Importance))]
[Index(nameof(Timestamp))]
public class Memory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [ForeignKey(nameof(Profile))]
    public Guid ProfileId { get; set; }
    
    [Required]
    [Column(TypeName = "text")]
    [StringLength(10000)]
    public string Content { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string MemoryType { get; set; } = "experience"; // experience, preference, skill, relationship, goal
    
    [Required]
    [Range(1, 10)]
    public int Importance { get; set; } = 5;
    
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string? CreatedBy { get; set; }
    
    [StringLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string? Source { get; set; }
    
    // Vector embedding for semantic search (stored as binary blob)
    [Column(TypeName = "blob")]
    public byte[]? EmbeddingVector { get; set; }
    
    [Required]
    public bool IsEmbeddingGenerated { get; set; } = false;
    
    // Navigation Properties
    public virtual Profile Profile { get; set; } = null!;
    public virtual ICollection<MemoryTag> Tags { get; set; } = new List<MemoryTag>();
    public virtual ICollection<MemoryReference> References { get; set; } = new List<MemoryReference>();
}
```

### 2.2 MemoryTag Entity

```csharp
[Table("MemoryTags")]
[Index(nameof(MemoryId), nameof(TagName), IsUnique = true)]
[Index(nameof(TagName))]
public class MemoryTag
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [ForeignKey(nameof(Memory))]
    public Guid MemoryId { get; set; }
    
    [Required]
    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string TagName { get; set; } = string.Empty;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public virtual Memory Memory { get; set; } = null!;
}
```

### 2.3 MemoryReference Entity

```csharp
[Table("MemoryReferences")]
[Index(nameof(SourceMemoryId), nameof(TargetMemoryId), IsUnique = true)]
[Index(nameof(TargetMemoryId))]
[Index(nameof(ReferenceType))]
public class MemoryReference
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [ForeignKey(nameof(SourceMemory))]
    public Guid SourceMemoryId { get; set; }
    
    [Required]
    [ForeignKey(nameof(TargetMemory))]
    public Guid TargetMemoryId { get; set; }
    
    [Required]
    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string ReferenceType { get; set; } = string.Empty; // related, contradicts, confirms, follows, precedes
    
    [Range(0.0, 1.0)]
    [Column(TypeName = "decimal(3,2)")]
    public decimal? Strength { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string? CreatedBy { get; set; }
    
    // Navigation Properties
    public virtual Memory SourceMemory { get; set; } = null!;
    public virtual Memory TargetMemory { get; set; } = null!;
}
```

---

## 3. Conversation Entities

### 3.1 Conversation Entity

```csharp
[Table("Conversations")]
[Index(nameof(ProfileId), nameof(StartedAt))]
[Index(nameof(Status))]
[Index(nameof(ConversationType))]
public class Conversation
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [ForeignKey(nameof(Profile))]
    public Guid ProfileId { get; set; }
    
    [StringLength(200)]
    [Column(TypeName = "varchar(200)")]
    public string? Title { get; set; }
    
    [Required]
    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string ConversationType { get; set; } = "chat"; // chat, interview, analysis, training
    
    [Required]
    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string Status { get; set; } = "active"; // active, completed, paused, archived
    
    [Required]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? EndedAt { get; set; }
    
    [StringLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string? InitiatedBy { get; set; }
    
    [Column(TypeName = "text")]
    public string? Context { get; set; }
    
    [Column(TypeName = "text")]
    public string? Summary { get; set; }
    
    [Required]
    public int MessageCount { get; set; } = 0;
    
    // Navigation Properties
    public virtual Profile Profile { get; set; } = null!;
    public virtual ICollection<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();
}
```

### 3.2 ConversationMessage Entity

```csharp
[Table("ConversationMessages")]
[Index(nameof(ConversationId), nameof(Timestamp))]
[Index(nameof(Role))]
[Index(nameof(Timestamp))]
public class ConversationMessage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [ForeignKey(nameof(Conversation))]
    public Guid ConversationId { get; set; }
    
    [Required]
    [StringLength(20)]
    [Column(TypeName = "varchar(20)")]
    public string Role { get; set; } = string.Empty; // user, assistant, system
    
    [Required]
    [Column(TypeName = "text")]
    public string Content { get; set; } = string.Empty;
    
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [Range(0.0, 1.0)]
    [Column(TypeName = "decimal(3,2)")]
    public decimal? ConfidenceScore { get; set; }
    
    [Column(TypeName = "text")]
    public string? Metadata { get; set; } // JSON metadata
    
    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string? ResponseStyle { get; set; } // casual, formal, technical, emotional
    
    [Required]
    public int ProcessingTimeMs { get; set; } = 0;
    
    // Navigation Properties
    public virtual Conversation Conversation { get; set; } = null!;
}
```

---

## 4. Analytics Entities

### 4.1 ProfileAnalytics Entity

```csharp
[Table("ProfileAnalytics")]
[Index(nameof(ProfileId), nameof(AnalysisDate))]
[Index(nameof(AnalysisType))]
[Index(nameof(AnalysisDate))]
public class ProfileAnalytics
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [ForeignKey(nameof(Profile))]
    public Guid ProfileId { get; set; }
    
    [Required]
    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string AnalysisType { get; set; } = string.Empty; // personality_summary, memory_analysis, conversation_insights
    
    [Required]
    public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
    
    [Required]
    [Column(TypeName = "text")]
    public string Results { get; set; } = string.Empty; // JSON results
    
    [Range(0.0, 1.0)]
    [Column(TypeName = "decimal(3,2)")]
    public decimal? ConfidenceScore { get; set; }
    
    [StringLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string? GeneratedBy { get; set; }
    
    [Required]
    public int ProcessingTimeMs { get; set; } = 0;
    
    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string? Version { get; set; }
    
    // Navigation Properties
    public virtual Profile Profile { get; set; } = null!;
}
```

### 4.2 AnalyticsMetric Entity

```csharp
[Table("AnalyticsMetrics")]
[Index(nameof(ProfileId), nameof(MetricName), nameof(Timestamp))]
[Index(nameof(MetricName))]
[Index(nameof(Timestamp))]
public class AnalyticsMetric
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [ForeignKey(nameof(Profile))]
    public Guid ProfileId { get; set; }
    
    [Required]
    [StringLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string MetricName { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "decimal(18,6)")]
    public decimal Value { get; set; }
    
    [StringLength(20)]
    [Column(TypeName = "varchar(20)")]
    public string? Unit { get; set; }
    
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string? Category { get; set; }
    
    [Column(TypeName = "text")]
    public string? Metadata { get; set; } // JSON metadata
    
    // Navigation Properties
    public virtual Profile Profile { get; set; } = null!;
}
```

---

## 5. Interview System Entities

### 5.1 InterviewSession Entity

```csharp
[Table("InterviewSessions")]
[Index(nameof(ProfileId), nameof(StartedAt))]
[Index(nameof(Status))]
[Index(nameof(InterviewType))]
public class InterviewSession
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [ForeignKey(nameof(Profile))]
    public Guid ProfileId { get; set; }
    
    [Required]
    [StringLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string InterviewType { get; set; } = string.Empty; // autobiographical, targeted, validation, deep_dive
    
    [Required]
    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string Status { get; set; } = "planned"; // planned, in_progress, completed, paused, cancelled
    
    [Required]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedAt { get; set; }
    
    [StringLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string? ConductedBy { get; set; }
    
    [Column(TypeName = "text")]
    public string? Objectives { get; set; }
    
    [Column(TypeName = "text")]
    public string? Notes { get; set; }
    
    [Required]
    public int QuestionCount { get; set; } = 0;
    
    [Required]
    public int CompletedQuestions { get; set; } = 0;
    
    [Range(0, 100)]
    public int? CompletionPercentage { get; set; }
    
    // Navigation Properties
    public virtual Profile Profile { get; set; } = null!;
    public virtual ICollection<InterviewQuestion> Questions { get; set; } = new List<InterviewQuestion>();
    public virtual ICollection<InterviewResponse> Responses { get; set; } = new List<InterviewResponse>();
}
```

### 5.2 InterviewQuestion Entity

```csharp
[Table("InterviewQuestions")]
[Index(nameof(InterviewSessionId), nameof(QuestionOrder))]
[Index(nameof(Category))]
[Index(nameof(Status))]
public class InterviewQuestion
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [ForeignKey(nameof(InterviewSession))]
    public Guid InterviewSessionId { get; set; }
    
    [Required]
    [Column(TypeName = "text")]
    public string QuestionText { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string Category { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    [Column(TypeName = "varchar(20)")]
    public string Status { get; set; } = "pending"; // pending, asked, answered, skipped
    
    [Required]
    public int QuestionOrder { get; set; }
    
    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string? QuestionType { get; set; } // open, scale, choice, boolean
    
    [Column(TypeName = "text")]
    public string? Context { get; set; }
    
    [Column(TypeName = "text")]
    public string? ExpectedOutcome { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? AskedAt { get; set; }
    
    // Navigation Properties
    public virtual InterviewSession InterviewSession { get; set; } = null!;
    public virtual InterviewResponse? Response { get; set; }
}
```

### 5.3 InterviewResponse Entity

```csharp
[Table("InterviewResponses")]
[Index(nameof(InterviewSessionId), nameof(RespondedAt))]
[Index(nameof(InterviewQuestionId), IsUnique = true)]
[Index(nameof(RespondedAt))]
public class InterviewResponse
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [ForeignKey(nameof(InterviewSession))]
    public Guid InterviewSessionId { get; set; }
    
    [Required]
    [ForeignKey(nameof(InterviewQuestion))]
    public Guid InterviewQuestionId { get; set; }
    
    [Required]
    [Column(TypeName = "text")]
    public string ResponseText { get; set; } = string.Empty;
    
    [Required]
    public DateTime RespondedAt { get; set; } = DateTime.UtcNow;
    
    [Column(TypeName = "text")]
    public string? ExtractedInsights { get; set; } // JSON array of insights
    
    [Column(TypeName = "text")]
    public string? GeneratedTraits { get; set; } // JSON array of traits generated from response
    
    [Range(0.0, 1.0)]
    [Column(TypeName = "decimal(3,2)")]
    public decimal? ConfidenceScore { get; set; }
    
    [Required]
    public bool IsProcessed { get; set; } = false;
    
    public DateTime? ProcessedAt { get; set; }
    
    [StringLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string? ProcessedBy { get; set; }
    
    // Navigation Properties
    public virtual InterviewSession InterviewSession { get; set; } = null!;
    public virtual InterviewQuestion InterviewQuestion { get; set; } = null!;
}
```

---

## 6. Database Context Configuration

### 6.1 DigitalMeDbContext

```csharp
public class DigitalMeDbContext : DbContext
{
    public DigitalMeDbContext(DbContextOptions<DigitalMeDbContext> options) : base(options) { }
    
    // DbSets
    public DbSet<Profile> Profiles { get; set; } = null!;
    public DbSet<PersonalityTrait> PersonalityTraits { get; set; } = null!;
    public DbSet<TraitHistory> TraitHistory { get; set; } = null!;
    public DbSet<Memory> Memories { get; set; } = null!;
    public DbSet<MemoryTag> MemoryTags { get; set; } = null!;
    public DbSet<MemoryReference> MemoryReferences { get; set; } = null!;
    public DbSet<Conversation> Conversations { get; set; } = null!;
    public DbSet<ConversationMessage> ConversationMessages { get; set; } = null!;
    public DbSet<ProfileAnalytics> ProfileAnalytics { get; set; } = null!;
    public DbSet<AnalyticsMetric> AnalyticsMetrics { get; set; } = null!;
    public DbSet<InterviewSession> InterviewSessions { get; set; } = null!;
    public DbSet<InterviewQuestion> InterviewQuestions { get; set; } = null!;
    public DbSet<InterviewResponse> InterviewResponses { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure relationships and constraints
        ConfigureProfileRelationships(modelBuilder);
        ConfigureMemoryRelationships(modelBuilder);
        ConfigureConversationRelationships(modelBuilder);
        ConfigureAnalyticsRelationships(modelBuilder);
        ConfigureInterviewRelationships(modelBuilder);
        
        // Configure value conversions
        ConfigureValueConverters(modelBuilder);
        
        // Seed default data
        SeedDefaultData(modelBuilder);
    }
    
    private void ConfigureProfileRelationships(ModelBuilder modelBuilder)
    {
        // Profile -> PersonalityTraits (One-to-Many)
        modelBuilder.Entity<PersonalityTrait>()
            .HasOne(pt => pt.Profile)
            .WithMany(p => p.PersonalityTraits)
            .HasForeignKey(pt => pt.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // PersonalityTrait -> TraitHistory (One-to-Many)
        modelBuilder.Entity<TraitHistory>()
            .HasOne(th => th.PersonalityTrait)
            .WithMany(pt => pt.History)
            .HasForeignKey(th => th.PersonalityTraitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
    
    private void ConfigureMemoryRelationships(ModelBuilder modelBuilder)
    {
        // Profile -> Memories (One-to-Many)
        modelBuilder.Entity<Memory>()
            .HasOne(m => m.Profile)
            .WithMany(p => p.Memories)
            .HasForeignKey(m => m.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Memory -> MemoryTags (One-to-Many)
        modelBuilder.Entity<MemoryTag>()
            .HasOne(mt => mt.Memory)
            .WithMany(m => m.Tags)
            .HasForeignKey(mt => mt.MemoryId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // MemoryReference self-referencing relationships
        modelBuilder.Entity<MemoryReference>()
            .HasOne(mr => mr.SourceMemory)
            .WithMany()
            .HasForeignKey(mr => mr.SourceMemoryId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<MemoryReference>()
            .HasOne(mr => mr.TargetMemory)
            .WithMany(m => m.References)
            .HasForeignKey(mr => mr.TargetMemoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    
    private void ConfigureConversationRelationships(ModelBuilder modelBuilder)
    {
        // Profile -> Conversations (One-to-Many)
        modelBuilder.Entity<Conversation>()
            .HasOne(c => c.Profile)
            .WithMany(p => p.Conversations)
            .HasForeignKey(c => c.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Conversation -> Messages (One-to-Many)
        modelBuilder.Entity<ConversationMessage>()
            .HasOne(cm => cm.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(cm => cm.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
    
    private void ConfigureAnalyticsRelationships(ModelBuilder modelBuilder)
    {
        // Profile -> Analytics (One-to-Many)
        modelBuilder.Entity<ProfileAnalytics>()
            .HasOne(pa => pa.Profile)
            .WithMany(p => p.Analytics)
            .HasForeignKey(pa => pa.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Profile -> Metrics (One-to-Many)
        modelBuilder.Entity<AnalyticsMetric>()
            .HasOne(am => am.Profile)
            .WithMany()
            .HasForeignKey(am => am.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
    
    private void ConfigureInterviewRelationships(ModelBuilder modelBuilder)
    {
        // Profile -> InterviewSessions (One-to-Many)
        modelBuilder.Entity<InterviewSession>()
            .HasOne(is => is.Profile)
            .WithMany(p => p.InterviewSessions)
            .HasForeignKey(is => is.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // InterviewSession -> Questions (One-to-Many)
        modelBuilder.Entity<InterviewQuestion>()
            .HasOne(iq => iq.InterviewSession)
            .WithMany(is => is.Questions)
            .HasForeignKey(iq => iq.InterviewSessionId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // InterviewSession -> Responses (One-to-Many)
        modelBuilder.Entity<InterviewResponse>()
            .HasOne(ir => ir.InterviewSession)
            .WithMany(is => is.Responses)
            .HasForeignKey(ir => ir.InterviewSessionId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // InterviewQuestion -> Response (One-to-One)
        modelBuilder.Entity<InterviewResponse>()
            .HasOne(ir => ir.InterviewQuestion)
            .WithOne(iq => iq.Response)
            .HasForeignKey<InterviewResponse>(ir => ir.InterviewQuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    
    private void ConfigureValueConverters(ModelBuilder modelBuilder)
    {
        // Configure enum-like string values with check constraints
        modelBuilder.Entity<Profile>()
            .HasCheckConstraint("CK_Profile_Status", 
                "Status IN ('Active', 'Inactive', 'Archived')");
        
        modelBuilder.Entity<PersonalityTrait>()
            .HasCheckConstraint("CK_PersonalityTrait_Category", 
                "Category IN ('cognitive', 'behavioral', 'values', 'preferences', 'communication')");
        
        modelBuilder.Entity<Memory>()
            .HasCheckConstraint("CK_Memory_MemoryType", 
                "MemoryType IN ('experience', 'preference', 'skill', 'relationship', 'goal')");
        
        modelBuilder.Entity<Conversation>()
            .HasCheckConstraint("CK_Conversation_Status", 
                "Status IN ('active', 'completed', 'paused', 'archived')");
        
        modelBuilder.Entity<ConversationMessage>()
            .HasCheckConstraint("CK_ConversationMessage_Role", 
                "Role IN ('user', 'assistant', 'system')");
    }
    
    private void SeedDefaultData(ModelBuilder modelBuilder)
    {
        // Seed a default profile for testing
        var defaultProfileId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        
        modelBuilder.Entity<Profile>().HasData(
            new Profile
            {
                Id = defaultProfileId,
                Name = "DefaultProfile",
                Description = "Default profile for testing and development",
                Status = "Active",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedBy = "System",
                LastUpdatedBy = "System",
                Version = 1
            }
        );
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update LastUpdated timestamp for entities that have this property
        var changedEntries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
            
        foreach (var entry in changedEntries)
        {
            if (entry.Entity is Profile profile)
            {
                profile.LastUpdated = DateTime.UtcNow;
                if (entry.State == EntityState.Modified)
                    profile.Version++;
            }
            else if (entry.Entity is PersonalityTrait trait)
            {
                trait.LastUpdated = DateTime.UtcNow;
                if (entry.State == EntityState.Modified)
                    trait.Version++;
            }
        }
        
        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

---

## 7. Connection String Configuration

### 7.1 SQLite Configuration (Development)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=digitalme.db;Cache=Shared",
    "ReadOnlyConnection": "Data Source=digitalme.db;Mode=ReadOnly;Cache=Shared"
  }
}
```

### 7.2 PostgreSQL Configuration (Production)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=digitalme;Username=digitalme_user;Password=secure_password;Pooling=true;MinPoolSize=5;MaxPoolSize=100",
    "ReadOnlyConnection": "Host=localhost;Database=digitalme;Username=digitalme_readonly;Password=readonly_password;Pooling=true;MinPoolSize=2;MaxPoolSize=20"
  }
}
```

---

## 8. Entity Validation Rules

### 8.1 Custom Validation Attributes

```csharp
public class ValidCategoryAttribute : ValidationAttribute
{
    private static readonly string[] ValidCategories = 
        { "cognitive", "behavioral", "values", "preferences", "communication" };
    
    public override bool IsValid(object? value)
    {
        if (value is string category)
            return ValidCategories.Contains(category, StringComparer.OrdinalIgnoreCase);
        return false;
    }
    
    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be one of: {string.Join(", ", ValidCategories)}";
    }
}

public class ValidMemoryTypeAttribute : ValidationAttribute
{
    private static readonly string[] ValidTypes = 
        { "experience", "preference", "skill", "relationship", "goal" };
    
    public override bool IsValid(object? value)
    {
        if (value is string type)
            return ValidTypes.Contains(type, StringComparer.OrdinalIgnoreCase);
        return false;
    }
    
    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be one of: {string.Join(", ", ValidTypes)}";
    }
}
```

---

## Success Criteria

**Entity Compilation**:
- [ ] All entity classes compile without errors
- [ ] All navigation properties are properly defined
- [ ] All foreign key relationships are configured correctly

**Database Creation**:
- [ ] Database migrations generate successfully
- [ ] All tables are created with correct schema
- [ ] All indexes are created as specified
- [ ] All constraints are applied correctly

**Data Validation**:
- [ ] Entity validation attributes work correctly
- [ ] Check constraints prevent invalid data
- [ ] Foreign key constraints maintain referential integrity
- [ ] Custom validation attributes function as expected

**Performance Validation**:
- [ ] Index usage is optimized for common queries
- [ ] Entity Framework change tracking works efficiently
- [ ] Bulk operations complete within performance targets
- [ ] Database queries execute within acceptable time limits

This specification provides complete, implementable entity definitions that require no human interpretation. Every property, relationship, constraint, and validation rule is explicitly defined with exact data types, lengths, and behaviors.