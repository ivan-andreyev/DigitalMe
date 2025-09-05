# 02-01-service-interfaces.md

## Complete Service Interface Contracts

### Overview
Exact service interface definitions with complete method signatures, parameter validation, return types, and error handling. Each interface provides concrete contracts for business logic implementation with no ambiguity.

---

## 1. Profile Management Services

### 1.1 IProfileService Interface

```csharp
public interface IProfileService
{
    /// <summary>
    /// Creates a new profile with validation and initialization
    /// </summary>
    /// <param name="request">Profile creation request with required fields</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created profile with generated ID and timestamps</returns>
    /// <exception cref="ProfileValidationException">Invalid profile data</exception>
    /// <exception cref="ProfileAlreadyExistsException">Profile name already exists</exception>
    Task<ProfileDto> CreateAsync(CreateProfileRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves profile by unique name
    /// </summary>
    /// <param name="name">Profile name (case-insensitive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Profile data or null if not found</returns>
    /// <exception cref="ArgumentException">Invalid name parameter</exception>
    Task<ProfileDto?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves profile by unique identifier
    /// </summary>
    /// <param name="id">Profile unique identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Profile data or null if not found</returns>
    /// <exception cref="ArgumentException">Invalid ID parameter</exception>
    Task<ProfileDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates profile with optimistic concurrency control
    /// </summary>
    /// <param name="request">Profile update request with version for concurrency</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated profile data with incremented version</returns>
    /// <exception cref="ProfileNotFoundException">Profile does not exist</exception>
    /// <exception cref="ConcurrencyException">Version mismatch detected</exception>
    /// <exception cref="ProfileValidationException">Invalid update data</exception>
    Task<ProfileDto> UpdateAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Archives profile (soft delete) preserving all related data
    /// </summary>
    /// <param name="id">Profile unique identifier</param>
    /// <param name="archivedBy">User performing the archive operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully archived</returns>
    /// <exception cref="ProfileNotFoundException">Profile does not exist</exception>
    /// <exception cref="ProfileAlreadyArchivedException">Profile already archived</exception>
    Task<bool> ArchiveAsync(Guid id, string archivedBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves paginated list of profiles with optional filtering
    /// </summary>
    /// <param name="request">Pagination and filtering parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated profile list with metadata</returns>
    /// <exception cref="ArgumentException">Invalid pagination parameters</exception>
    Task<PagedResult<ProfileSummaryDto>> GetPagedAsync(GetProfilesRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates profile name availability and format
    /// </summary>
    /// <param name="name">Proposed profile name</param>
    /// <param name="excludeId">Profile ID to exclude from uniqueness check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with availability and format checks</returns>
    Task<ProfileNameValidationResult> ValidateNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves profile statistics for analytics dashboard
    /// </summary>
    /// <param name="id">Profile unique identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive profile statistics</returns>
    /// <exception cref="ProfileNotFoundException">Profile does not exist</exception>
    Task<ProfileStatisticsDto> GetStatisticsAsync(Guid id, CancellationToken cancellationToken = default);
}
```

### 1.2 Request/Response DTOs for IProfileService

```csharp
public record CreateProfileRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; init; }
    
    [Required]
    public string CreatedBy { get; init; } = string.Empty;
}

public record UpdateProfileRequest
{
    [Required]
    public Guid Id { get; init; }
    
    [StringLength(500)]
    public string? Description { get; init; }
    
    [Required]
    public int Version { get; init; }
    
    [Required]
    public string UpdatedBy { get; init; } = string.Empty;
}

public record GetProfilesRequest
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;
    
    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
    
    public string? SearchTerm { get; init; }
    
    public string? Status { get; init; } // Active, Inactive, Archived
    
    public DateTime? CreatedFrom { get; init; }
    
    public DateTime? CreatedTo { get; init; }
    
    public string? SortBy { get; init; } = "Name"; // Name, CreatedAt, LastUpdated
    
    public string? SortDirection { get; init; } = "ASC"; // ASC, DESC
}

public record ProfileDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime LastUpdated { get; init; }
    public string? CreatedBy { get; init; }
    public string? LastUpdatedBy { get; init; }
    public int Version { get; init; }
}

public record ProfileSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime LastUpdated { get; init; }
    public int TraitCount { get; init; }
    public int MemoryCount { get; init; }
    public int ConversationCount { get; init; }
}

public record ProfileNameValidationResult
{
    public bool IsValid { get; init; }
    public bool IsAvailable { get; init; }
    public string[] ValidationErrors { get; init; } = Array.Empty<string>();
}

public record ProfileStatisticsDto
{
    public Guid ProfileId { get; init; }
    public string ProfileName { get; init; } = string.Empty;
    public int TotalTraits { get; init; }
    public int TotalMemories { get; init; }
    public int TotalConversations { get; init; }
    public int TotalInterviewSessions { get; init; }
    public DateTime? LastActivityAt { get; init; }
    public Dictionary<string, int> TraitsByCategory { get; init; } = new();
    public Dictionary<string, int> MemoriesByType { get; init; } = new();
    public Dictionary<string, int> ConversationsByType { get; init; } = new();
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}
```

---

## 2. Personality Management Services

### 2.1 IPersonalityService Interface

```csharp
public interface IPersonalityService
{
    /// <summary>
    /// Creates or updates a personality trait with validation and history tracking
    /// </summary>
    /// <param name="request">Trait creation/update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created or updated trait data</returns>
    /// <exception cref="ProfileNotFoundException">Profile does not exist</exception>
    /// <exception cref="TraitValidationException">Invalid trait data</exception>
    Task<PersonalityTraitDto> UpsertTraitAsync(UpsertTraitRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves personality traits with optional filtering by category
    /// </summary>
    /// <param name="profileId">Profile unique identifier</param>
    /// <param name="categories">Optional category filter</param>
    /// <param name="includeHistory">Include trait change history</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered list of personality traits</returns>
    /// <exception cref="ProfileNotFoundException">Profile does not exist</exception>
    Task<List<PersonalityTraitDto>> GetTraitsAsync(Guid profileId, string[]? categories = null, bool includeHistory = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves specific trait by category and name
    /// </summary>
    /// <param name="profileId">Profile unique identifier</param>
    /// <param name="category">Trait category</param>
    /// <param name="traitName">Trait name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Trait data or null if not found</returns>
    /// <exception cref="ArgumentException">Invalid parameters</exception>
    Task<PersonalityTraitDto?> GetTraitAsync(Guid profileId, string category, string traitName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates existing trait with version control and history tracking
    /// </summary>
    /// <param name="request">Trait update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Update result with old and new values</returns>
    /// <exception cref="TraitNotFoundException">Trait does not exist</exception>
    /// <exception cref="ConcurrencyException">Version mismatch detected</exception>
    Task<TraitUpdateResult> UpdateTraitAsync(UpdateTraitRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes trait and archives its history
    /// </summary>
    /// <param name="profileId">Profile unique identifier</param>
    /// <param name="category">Trait category</param>
    /// <param name="traitName">Trait name</param>
    /// <param name="deletedBy">User performing the deletion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully deleted</returns>
    /// <exception cref="TraitNotFoundException">Trait does not exist</exception>
    Task<bool> DeleteTraitAsync(Guid profileId, string category, string traitName, string deletedBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves trait change history with pagination
    /// </summary>
    /// <param name="profileId">Profile unique identifier</param>
    /// <param name="category">Optional category filter</param>
    /// <param name="traitName">Optional trait name filter</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Items per page (1-100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated trait history</returns>
    /// <exception cref="ArgumentException">Invalid pagination parameters</exception>
    Task<PagedResult<TraitHistoryDto>> GetTraitHistoryAsync(Guid profileId, string? category = null, string? traitName = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Analyzes trait distribution and provides personality insights
    /// </summary>
    /// <param name="profileId">Profile unique identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive personality analysis</returns>
    /// <exception cref="ProfileNotFoundException">Profile does not exist</exception>
    Task<PersonalityAnalysisDto> AnalyzePersonalityAsync(Guid profileId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates trait data before persistence
    /// </summary>
    /// <param name="category">Trait category</param>
    /// <param name="traitName">Trait name</param>
    /// <param name="value">Trait value</param>
    /// <param name="confidenceScore">Confidence score (0.0-1.0)</param>
    /// <returns>Validation result with any errors</returns>
    Task<TraitValidationResult> ValidateTraitAsync(string category, string traitName, string value, decimal confidenceScore);
    
    /// <summary>
    /// Bulk imports traits from structured data with validation
    /// </summary>
    /// <param name="request">Bulk import request with trait data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import results with success/failure counts</returns>
    /// <exception cref="ProfileNotFoundException">Profile does not exist</exception>
    /// <exception cref="BulkImportException">Critical import errors</exception>
    Task<BulkTraitImportResult> BulkImportTraitsAsync(BulkTraitImportRequest request, CancellationToken cancellationToken = default);
}
```

### 2.2 Request/Response DTOs for IPersonalityService

```csharp
public record UpsertTraitRequest
{
    [Required]
    public Guid ProfileId { get; init; }
    
    [Required]
    [ValidCategory]
    public string Category { get; init; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;
    
    [Required]
    [StringLength(2000)]
    public string Value { get; init; } = string.Empty;
    
    [Range(0.0, 1.0)]
    public decimal ConfidenceScore { get; init; } = 0.8m;
    
    [StringLength(100)]
    public string? Source { get; init; }
    
    [Required]
    public string UpdatedBy { get; init; } = string.Empty;
}

public record UpdateTraitRequest
{
    [Required]
    public Guid ProfileId { get; init; }
    
    [Required]
    public string Category { get; init; } = string.Empty;
    
    [Required]
    public string TraitName { get; init; } = string.Empty;
    
    [Required]
    public string Value { get; init; } = string.Empty;
    
    [Range(0.0, 1.0)]
    public decimal ConfidenceScore { get; init; } = 0.8m;
    
    [StringLength(100)]
    public string? Source { get; init; }
    
    [Required]
    public string UpdatedBy { get; init; } = string.Empty;
    
    [Required]
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

public record PersonalityTraitDto
{
    public Guid Id { get; init; }
    public Guid ProfileId { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public decimal ConfidenceScore { get; init; }
    public string? Source { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastUpdated { get; init; }
    public string? UpdatedBy { get; init; }
    public int Version { get; init; }
    public List<TraitHistoryDto>? History { get; init; }
}

public record TraitUpdateResult
{
    public bool Success { get; init; }
    public string? OldValue { get; init; }
    public string NewValue { get; init; } = string.Empty;
    public decimal OldConfidenceScore { get; init; }
    public decimal NewConfidenceScore { get; init; }
    public int Version { get; init; }
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

public record TraitHistoryDto
{
    public Guid Id { get; init; }
    public string? OldValue { get; init; }
    public string NewValue { get; init; } = string.Empty;
    public decimal? OldConfidenceScore { get; init; }
    public decimal NewConfidenceScore { get; init; }
    public string? Source { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public string? ChangeReason { get; init; }
}

public record PersonalityAnalysisDto
{
    public Guid ProfileId { get; init; }
    public Dictionary<string, int> TraitDistribution { get; init; } = new();
    public Dictionary<string, decimal> CategoryConfidence { get; init; } = new();
    public List<PersonalityInsightDto> Insights { get; init; } = new();
    public decimal OverallCompleteness { get; init; }
    public DateTime AnalysisDate { get; init; } = DateTime.UtcNow;
}

public record PersonalityInsightDto
{
    public string Type { get; init; } = string.Empty; // strength, weakness, pattern, contradiction
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Confidence { get; init; }
    public string[] SupportingTraits { get; init; } = Array.Empty<string>();
}

public record TraitValidationResult
{
    public bool IsValid { get; init; }
    public string[] ValidationErrors { get; init; } = Array.Empty<string>();
    public string[] Warnings { get; init; } = Array.Empty<string>();
    public Dictionary<string, string> Suggestions { get; init; } = new();
}
```

---

## 3. Memory Management Services

### 3.1 IMemoryService Interface

```csharp
public interface IMemoryService
{
    /// <summary>
    /// Creates new memory with automatic embedding generation and tagging
    /// </summary>
    /// <param name="request">Memory creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created memory with generated embeddings</returns>
    /// <exception cref="ProfileNotFoundException">Profile does not exist</exception>
    /// <exception cref="MemoryValidationException">Invalid memory data</exception>
    Task<MemoryDto> CreateAsync(CreateMemoryRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves memory by unique identifier
    /// </summary>
    /// <param name="id">Memory unique identifier</param>
    /// <param name="includeTags">Include associated tags</param>
    /// <param name="includeReferences">Include memory references</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Memory data or null if not found</returns>
    Task<MemoryDto?> GetByIdAsync(Guid id, bool includeTags = true, bool includeReferences = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches memories using semantic similarity and filters
    /// </summary>
    /// <param name="request">Search request with query and filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ranked search results with similarity scores</returns>
    /// <exception cref="ProfileNotFoundException">Profile does not exist</exception>
    /// <exception cref="SearchValidationException">Invalid search parameters</exception>
    Task<List<MemorySearchResult>> SearchAsync(SearchMemoriesRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves paginated memories with filtering and sorting
    /// </summary>
    /// <param name="request">Pagination and filtering request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated memory results</returns>
    /// <exception cref="ArgumentException">Invalid pagination parameters</exception>
    Task<PagedResult<MemoryDto>> GetPagedAsync(GetMemoriesRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates memory content and regenerates embeddings if needed
    /// </summary>
    /// <param name="request">Memory update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated memory data</returns>
    /// <exception cref="MemoryNotFoundException">Memory does not exist</exception>
    /// <exception cref="MemoryValidationException">Invalid update data</exception>
    Task<MemoryDto> UpdateAsync(UpdateMemoryRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Soft deletes memory and removes from search index
    /// </summary>
    /// <param name="id">Memory unique identifier</param>
    /// <param name="deletedBy">User performing the deletion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully deleted</returns>
    /// <exception cref="MemoryNotFoundException">Memory does not exist</exception>
    Task<bool> DeleteAsync(Guid id, string deletedBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds tags to memory with duplicate prevention
    /// </summary>
    /// <param name="memoryId">Memory unique identifier</param>
    /// <param name="tags">Tags to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated memory with new tags</returns>
    /// <exception cref="MemoryNotFoundException">Memory does not exist</exception>
    Task<MemoryDto> AddTagsAsync(Guid memoryId, string[] tags, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes tags from memory
    /// </summary>
    /// <param name="memoryId">Memory unique identifier</param>
    /// <param name="tags">Tags to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated memory without removed tags</returns>
    /// <exception cref="MemoryNotFoundException">Memory does not exist</exception>
    Task<MemoryDto> RemoveTagsAsync(Guid memoryId, string[] tags, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates bidirectional reference between memories
    /// </summary>
    /// <param name="request">Reference creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created reference data</returns>
    /// <exception cref="MemoryNotFoundException">Source or target memory does not exist</exception>
    /// <exception cref="CircularReferenceException">Reference would create a cycle</exception>
    Task<MemoryReferenceDto> CreateReferenceAsync(CreateMemoryReferenceRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Analyzes memories for patterns, themes, and insights
    /// </summary>
    /// <param name="profileId">Profile unique identifier</param>
    /// <param name="analysisType">Type of analysis to perform</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive memory analysis</returns>
    /// <exception cref="ProfileNotFoundException">Profile does not exist</exception>
    Task<MemoryAnalysisDto> AnalyzeMemoriesAsync(Guid profileId, string analysisType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Regenerates embedding vectors for memories (batch operation)
    /// </summary>
    /// <param name="profileId">Profile unique identifier</param>
    /// <param name="forceRegenerate">Force regeneration even if embeddings exist</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Embedding generation results</returns>
    /// <exception cref="ProfileNotFoundException">Profile does not exist</exception>
    Task<EmbeddingGenerationResult> RegenerateEmbeddingsAsync(Guid profileId, bool forceRegenerate = false, CancellationToken cancellationToken = default);
}
```

### 3.2 Request/Response DTOs for IMemoryService

```csharp
public record CreateMemoryRequest
{
    [Required]
    public Guid ProfileId { get; init; }
    
    [Required]
    [StringLength(10000, MinimumLength = 10)]
    public string Content { get; init; } = string.Empty;
    
    [Required]
    [ValidMemoryType]
    public string MemoryType { get; init; } = "experience";
    
    [Range(1, 10)]
    public int Importance { get; init; } = 5;
    
    [MaxLength(20)]
    public string[] Tags { get; init; } = Array.Empty<string>();
    
    public DateTime? Timestamp { get; init; }
    
    [Required]
    public string CreatedBy { get; init; } = string.Empty;
    
    [StringLength(100)]
    public string? Source { get; init; }
}

public record SearchMemoriesRequest
{
    [Required]
    public Guid ProfileId { get; init; }
    
    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Query { get; init; } = string.Empty;
    
    [Range(1, 50)]
    public int Limit { get; init; } = 10;
    
    public string[]? MemoryTypes { get; init; }
    
    [Range(1, 10)]
    public int? MinImportance { get; init; }
    
    public DateTime? DateFrom { get; init; }
    
    public DateTime? DateTo { get; init; }
    
    public string[]? Tags { get; init; }
    
    public bool IncludeSimilarityScores { get; init; } = false;
    
    [Range(0.0, 1.0)]
    public decimal? MinSimilarityScore { get; init; }
}

public record GetMemoriesRequest
{
    [Required]
    public Guid ProfileId { get; init; }
    
    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;
    
    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
    
    public string? MemoryType { get; init; }
    
    [Range(1, 10)]
    public int? MinImportance { get; init; }
    
    public DateTime? DateFrom { get; init; }
    
    public DateTime? DateTo { get; init; }
    
    public string[]? Tags { get; init; }
    
    public string? SearchTerm { get; init; }
    
    public string? SortBy { get; init; } = "Timestamp"; // Timestamp, Importance, CreatedAt
    
    public string? SortDirection { get; init; } = "DESC"; // ASC, DESC
    
    public bool IncludeTags { get; init; } = true;
}

public record UpdateMemoryRequest
{
    [Required]
    public Guid Id { get; init; }
    
    [StringLength(10000, MinimumLength = 10)]
    public string? Content { get; init; }
    
    [ValidMemoryType]
    public string? MemoryType { get; init; }
    
    [Range(1, 10)]
    public int? Importance { get; init; }
    
    public DateTime? Timestamp { get; init; }
    
    [Required]
    public string UpdatedBy { get; init; } = string.Empty;
}

public record CreateMemoryReferenceRequest
{
    [Required]
    public Guid SourceMemoryId { get; init; }
    
    [Required]
    public Guid TargetMemoryId { get; init; }
    
    [Required]
    [StringLength(50)]
    public string ReferenceType { get; init; } = string.Empty; // related, contradicts, confirms, follows, precedes
    
    [Range(0.0, 1.0)]
    public decimal? Strength { get; init; }
    
    [Required]
    public string CreatedBy { get; init; } = string.Empty;
}

public record MemoryDto
{
    public Guid Id { get; init; }
    public Guid ProfileId { get; init; }
    public string Content { get; init; } = string.Empty;
    public string MemoryType { get; init; } = string.Empty;
    public int Importance { get; init; }
    public DateTime Timestamp { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public string? Source { get; init; }
    public bool IsEmbeddingGenerated { get; init; }
    public List<string> Tags { get; init; } = new();
    public List<MemoryReferenceDto>? References { get; init; }
}

public record MemorySearchResult
{
    public MemoryDto Memory { get; init; } = null!;
    public decimal? SimilarityScore { get; init; }
    public string[] MatchingTags { get; init; } = Array.Empty<string>();
    public string? ReasonForMatch { get; init; }
}

public record MemoryReferenceDto
{
    public Guid Id { get; init; }
    public Guid SourceMemoryId { get; init; }
    public Guid TargetMemoryId { get; init; }
    public string ReferenceType { get; init; } = string.Empty;
    public decimal? Strength { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
}

public record MemoryAnalysisDto
{
    public Guid ProfileId { get; init; }
    public string AnalysisType { get; init; } = string.Empty;
    public Dictionary<string, int> TypeDistribution { get; init; } = new();
    public Dictionary<string, int> ImportanceDistribution { get; init; } = new();
    public Dictionary<string, int> TagFrequency { get; init; } = new();
    public List<string> CommonThemes { get; init; } = new();
    public List<string> IdentifiedPatterns { get; init; } = new();
    public DateTime AnalysisDate { get; init; } = DateTime.UtcNow;
    public int ProcessingTimeMs { get; init; }
}

public record EmbeddingGenerationResult
{
    public int TotalMemories { get; init; }
    public int ProcessedMemories { get; init; }
    public int SuccessfulEmbeddings { get; init; }
    public int FailedEmbeddings { get; init; }
    public string[] Errors { get; init; } = Array.Empty<string>();
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
    public int ProcessingTimeMs { get; init; }
}
```

---

## 4. Generic Helper Types

### 4.1 Common Response Types

```csharp
public record PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}

public record ServiceResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string[] Errors { get; init; } = Array.Empty<string>();
    public string[] Warnings { get; init; } = Array.Empty<string>();
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public record ValidationError
{
    public string PropertyName { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public string ErrorCode { get; init; } = string.Empty;
    public object? AttemptedValue { get; init; }
}
```

---

## 5. Custom Exceptions

### 5.1 Service-Specific Exceptions

```csharp
public abstract class ServiceException : Exception
{
    public string ErrorCode { get; }
    public Dictionary<string, object> Context { get; }
    
    protected ServiceException(string errorCode, string message, Dictionary<string, object>? context = null) 
        : base(message)
    {
        ErrorCode = errorCode;
        Context = context ?? new Dictionary<string, object>();
    }
}

public class ProfileNotFoundException : ServiceException
{
    public ProfileNotFoundException(Guid profileId) 
        : base("PROFILE_NOT_FOUND", $"Profile with ID '{profileId}' not found", new Dictionary<string, object> { ["ProfileId"] = profileId }) { }
    
    public ProfileNotFoundException(string profileName) 
        : base("PROFILE_NOT_FOUND", $"Profile with name '{profileName}' not found", new Dictionary<string, object> { ["ProfileName"] = profileName }) { }
}

public class ProfileAlreadyExistsException : ServiceException
{
    public ProfileAlreadyExistsException(string profileName) 
        : base("PROFILE_ALREADY_EXISTS", $"Profile with name '{profileName}' already exists", new Dictionary<string, object> { ["ProfileName"] = profileName }) { }
}

public class ProfileValidationException : ServiceException
{
    public ValidationError[] ValidationErrors { get; }
    
    public ProfileValidationException(ValidationError[] validationErrors) 
        : base("PROFILE_VALIDATION_ERROR", "Profile validation failed", new Dictionary<string, object> { ["ValidationErrors"] = validationErrors })
    {
        ValidationErrors = validationErrors;
    }
}

public class ConcurrencyException : ServiceException
{
    public int ExpectedVersion { get; }
    public int ActualVersion { get; }
    
    public ConcurrencyException(int expectedVersion, int actualVersion) 
        : base("CONCURRENCY_ERROR", $"Concurrency conflict detected. Expected version {expectedVersion}, actual version {actualVersion}", 
               new Dictionary<string, object> { ["ExpectedVersion"] = expectedVersion, ["ActualVersion"] = actualVersion })
    {
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}

public class TraitNotFoundException : ServiceException
{
    public TraitNotFoundException(Guid profileId, string category, string traitName) 
        : base("TRAIT_NOT_FOUND", $"Trait '{traitName}' in category '{category}' not found for profile '{profileId}'",
               new Dictionary<string, object> { ["ProfileId"] = profileId, ["Category"] = category, ["TraitName"] = traitName }) { }
}

public class TraitValidationException : ServiceException
{
    public TraitValidationException(string message, Dictionary<string, object>? context = null) 
        : base("TRAIT_VALIDATION_ERROR", message, context) { }
}

public class MemoryNotFoundException : ServiceException
{
    public MemoryNotFoundException(Guid memoryId) 
        : base("MEMORY_NOT_FOUND", $"Memory with ID '{memoryId}' not found", new Dictionary<string, object> { ["MemoryId"] = memoryId }) { }
}

public class MemoryValidationException : ServiceException
{
    public MemoryValidationException(string message, Dictionary<string, object>? context = null) 
        : base("MEMORY_VALIDATION_ERROR", message, context) { }
}

public class SearchValidationException : ServiceException
{
    public SearchValidationException(string message, Dictionary<string, object>? context = null) 
        : base("SEARCH_VALIDATION_ERROR", message, context) { }
}

public class CircularReferenceException : ServiceException
{
    public CircularReferenceException(Guid sourceId, Guid targetId) 
        : base("CIRCULAR_REFERENCE_ERROR", $"Creating reference from '{sourceId}' to '{targetId}' would create a circular reference",
               new Dictionary<string, object> { ["SourceId"] = sourceId, ["TargetId"] = targetId }) { }
}

public class BulkImportException : ServiceException
{
    public int SuccessCount { get; }
    public int FailureCount { get; }
    
    public BulkImportException(int successCount, int failureCount, string message) 
        : base("BULK_IMPORT_ERROR", message, new Dictionary<string, object> { ["SuccessCount"] = successCount, ["FailureCount"] = failureCount })
    {
        SuccessCount = successCount;
        FailureCount = failureCount;
    }
}
```

---

## Success Criteria

**Interface Compilation**:
- [ ] All service interfaces compile without errors
- [ ] All method signatures are complete and unambiguous
- [ ] All parameter validation attributes work correctly
- [ ] All return types are properly defined

**DTO Validation**:
- [ ] All request/response DTOs compile successfully
- [ ] Validation attributes work as expected
- [ ] Serialization/deserialization functions correctly
- [ ] Required field validation prevents invalid requests

**Exception Handling**:
- [ ] All custom exceptions provide meaningful error information
- [ ] Exception hierarchy allows proper catch blocks
- [ ] Error context provides debugging information
- [ ] Exception messages are user-friendly

**Dependency Injection**:
- [ ] All services register successfully in DI container
- [ ] Service lifetimes are configured appropriately
- [ ] Circular dependencies are avoided
- [ ] Interface implementations can be easily mocked for testing

This specification provides complete, implementable service contracts that eliminate all human interpretation requirements. Every method has exact signatures, complete parameter validation, explicit return types, and comprehensive error handling patterns.