# 03-01-mcp-tool-definitions.md

## Complete MCP Tool Definitions for DigitalMe Platform

### Overview
Exact MCP tool implementations with complete method signatures, parameter validation, error handling, and resource schemas. Each tool provides programmatic access to DigitalMe backend services through Claude MCP protocol.

---

## 1. Profile Management Tools

### 1.1 GetPersonalityTraits Tool

```csharp
[MCPTool("get_personality_traits")]
[Description("Retrieve personality traits from a user profile with optional filtering")]
public async Task<PersonalityTraitsResponse> GetPersonalityTraitsAsync(
    [Description("Profile identifier (required)")] string profileName,
    [Description("Trait categories to filter (optional). Valid values: cognitive, behavioral, values, preferences, communication")]
    string[] categories = null,
    [Description("Include confidence scores (optional, default: false)")] bool includeConfidence = false)
{
    // Input validation
    if (string.IsNullOrWhiteSpace(profileName))
        throw new MCPValidationException("Profile name is required and cannot be empty");
    
    if (categories != null)
    {
        var validCategories = new[] { "cognitive", "behavioral", "values", "preferences", "communication" };
        var invalidCategories = categories.Except(validCategories);
        if (invalidCategories.Any())
            throw new MCPValidationException($"Invalid categories: {string.Join(", ", invalidCategories)}");
    }
    
    try 
    {
        var profile = await _profileService.GetByNameAsync(profileName);
        if (profile == null)
            throw new MCPResourceNotFoundException($"Profile '{profileName}' not found");
            
        var traits = await _personalityService.GetTraitsAsync(profile.Id, categories, includeConfidence);
        
        return new PersonalityTraitsResponse
        {
            ProfileName = profileName,
            Traits = traits.Select(t => new TraitDto
            {
                Category = t.Category,
                Name = t.Name,
                Value = t.Value,
                ConfidenceScore = includeConfidence ? t.ConfidenceScore : null,
                LastUpdated = t.LastUpdated
            }).ToList(),
            TotalCount = traits.Count,
            Timestamp = DateTime.UtcNow
        };
    }
    catch (Exception ex) when (!(ex is MCPException))
    {
        throw new MCPInternalException("Failed to retrieve personality traits", ex);
    }
}
```

**Response Schema**:
```json
{
  "type": "object",
  "properties": {
    "profileName": { "type": "string" },
    "traits": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "category": { "type": "string", "enum": ["cognitive", "behavioral", "values", "preferences", "communication"] },
          "name": { "type": "string" },
          "value": { "type": "string" },
          "confidenceScore": { "type": "number", "minimum": 0, "maximum": 1 },
          "lastUpdated": { "type": "string", "format": "date-time" }
        },
        "required": ["category", "name", "value", "lastUpdated"]
      }
    },
    "totalCount": { "type": "integer", "minimum": 0 },
    "timestamp": { "type": "string", "format": "date-time" }
  },
  "required": ["profileName", "traits", "totalCount", "timestamp"]
}
```

### 1.2 UpdatePersonalityTrait Tool

```csharp
[MCPTool("update_personality_trait")]
[Description("Update a specific personality trait value with validation")]
public async Task<UpdateTraitResponse> UpdatePersonalityTraitAsync(
    [Description("Profile identifier (required)")] string profileName,
    [Description("Trait category (required)")] string category,
    [Description("Trait name (required)")] string traitName,
    [Description("New trait value (required)")] string value,
    [Description("Confidence score (optional, 0.0-1.0)")] double? confidenceScore = null,
    [Description("Source of update (optional)")] string source = "mcp_tool")
{
    // Input validation
    if (string.IsNullOrWhiteSpace(profileName))
        throw new MCPValidationException("Profile name is required");
        
    if (string.IsNullOrWhiteSpace(category))
        throw new MCPValidationException("Category is required");
        
    if (string.IsNullOrWhiteSpace(traitName))
        throw new MCPValidationException("Trait name is required");
        
    if (string.IsNullOrWhiteSpace(value))
        throw new MCPValidationException("Value is required");
        
    if (confidenceScore.HasValue && (confidenceScore < 0.0 || confidenceScore > 1.0))
        throw new MCPValidationException("Confidence score must be between 0.0 and 1.0");
    
    var validCategories = new[] { "cognitive", "behavioral", "values", "preferences", "communication" };
    if (!validCategories.Contains(category))
        throw new MCPValidationException($"Invalid category. Must be one of: {string.Join(", ", validCategories)}");
    
    try
    {
        var profile = await _profileService.GetByNameAsync(profileName);
        if (profile == null)
            throw new MCPResourceNotFoundException($"Profile '{profileName}' not found");
            
        var updateRequest = new UpdateTraitRequest
        {
            ProfileId = profile.Id,
            Category = category,
            TraitName = traitName,
            Value = value,
            ConfidenceScore = confidenceScore ?? 0.8,
            Source = source,
            UpdatedBy = "mcp_system",
            UpdatedAt = DateTime.UtcNow
        };
        
        var result = await _personalityService.UpdateTraitAsync(updateRequest);
        
        return new UpdateTraitResponse
        {
            Success = result.Success,
            ProfileName = profileName,
            Category = category,
            TraitName = traitName,
            OldValue = result.OldValue,
            NewValue = value,
            ConfidenceScore = confidenceScore ?? 0.8,
            Timestamp = DateTime.UtcNow,
            Version = result.Version
        };
    }
    catch (Exception ex) when (!(ex is MCPException))
    {
        throw new MCPInternalException("Failed to update personality trait", ex);
    }
}
```

---

## 2. Memory Management Tools

### 2.1 StoreMemory Tool

```csharp
[MCPTool("store_memory")]
[Description("Store a new memory entry with automatic categorization and indexing")]
public async Task<StoreMemoryResponse> StoreMemoryAsync(
    [Description("Profile identifier (required)")] string profileName,
    [Description("Memory content (required, max 10000 characters)")] string content,
    [Description("Memory type (optional). Valid values: experience, preference, skill, relationship, goal")]
    string memoryType = "experience",
    [Description("Importance level (optional, 1-10)")] int importance = 5,
    [Description("Tags for categorization (optional)")] string[] tags = null,
    [Description("Associated timestamp (optional, ISO 8601 format)")] DateTime? timestamp = null)
{
    // Input validation
    if (string.IsNullOrWhiteSpace(profileName))
        throw new MCPValidationException("Profile name is required");
        
    if (string.IsNullOrWhiteSpace(content))
        throw new MCPValidationException("Content is required");
        
    if (content.Length > 10000)
        throw new MCPValidationException("Content cannot exceed 10000 characters");
        
    if (importance < 1 || importance > 10)
        throw new MCPValidationException("Importance must be between 1 and 10");
    
    var validMemoryTypes = new[] { "experience", "preference", "skill", "relationship", "goal" };
    if (!validMemoryTypes.Contains(memoryType))
        throw new MCPValidationException($"Invalid memory type. Must be one of: {string.Join(", ", validMemoryTypes)}");
    
    if (tags != null && tags.Length > 20)
        throw new MCPValidationException("Maximum 20 tags allowed");
    
    try
    {
        var profile = await _profileService.GetByNameAsync(profileName);
        if (profile == null)
            throw new MCPResourceNotFoundException($"Profile '{profileName}' not found");
            
        var memoryRequest = new CreateMemoryRequest
        {
            ProfileId = profile.Id,
            Content = content,
            MemoryType = memoryType,
            Importance = importance,
            Tags = tags?.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray() ?? Array.Empty<string>(),
            Timestamp = timestamp ?? DateTime.UtcNow,
            CreatedBy = "mcp_system",
            Source = "mcp_tool"
        };
        
        var memory = await _memoryService.CreateAsync(memoryRequest);
        
        return new StoreMemoryResponse
        {
            Success = true,
            MemoryId = memory.Id,
            ProfileName = profileName,
            Content = content,
            MemoryType = memoryType,
            Importance = importance,
            Tags = tags,
            Timestamp = memory.Timestamp,
            CreatedAt = memory.CreatedAt,
            EmbeddingGenerated = memory.EmbeddingVector != null
        };
    }
    catch (Exception ex) when (!(ex is MCPException))
    {
        throw new MCPInternalException("Failed to store memory", ex);
    }
}
```

### 2.2 SearchMemories Tool

```csharp
[MCPTool("search_memories")]
[Description("Search memories using semantic similarity and filters")]
public async Task<SearchMemoriesResponse> SearchMemoriesAsync(
    [Description("Profile identifier (required)")] string profileName,
    [Description("Search query (required)")] string query,
    [Description("Maximum number of results (optional, 1-50)")] int limit = 10,
    [Description("Memory types to filter (optional)")] string[] memoryTypes = null,
    [Description("Minimum importance level (optional, 1-10)")] int? minImportance = null,
    [Description("Date range start (optional, ISO 8601 format)")] DateTime? dateFrom = null,
    [Description("Date range end (optional, ISO 8601 format)")] DateTime? dateTo = null,
    [Description("Include similarity scores (optional, default: false)")] bool includeSimilarity = false)
{
    // Input validation
    if (string.IsNullOrWhiteSpace(profileName))
        throw new MCPValidationException("Profile name is required");
        
    if (string.IsNullOrWhiteSpace(query))
        throw new MCPValidationException("Query is required");
        
    if (limit < 1 || limit > 50)
        throw new MCPValidationException("Limit must be between 1 and 50");
        
    if (minImportance.HasValue && (minImportance < 1 || minImportance > 10))
        throw new MCPValidationException("Minimum importance must be between 1 and 10");
        
    if (dateFrom.HasValue && dateTo.HasValue && dateFrom > dateTo)
        throw new MCPValidationException("Date from cannot be after date to");
    
    if (memoryTypes != null)
    {
        var validTypes = new[] { "experience", "preference", "skill", "relationship", "goal" };
        var invalidTypes = memoryTypes.Except(validTypes);
        if (invalidTypes.Any())
            throw new MCPValidationException($"Invalid memory types: {string.Join(", ", invalidTypes)}");
    }
    
    try
    {
        var profile = await _profileService.GetByNameAsync(profileName);
        if (profile == null)
            throw new MCPResourceNotFoundException($"Profile '{profileName}' not found");
            
        var searchRequest = new SearchMemoriesRequest
        {
            ProfileId = profile.Id,
            Query = query,
            Limit = limit,
            MemoryTypes = memoryTypes,
            MinImportance = minImportance,
            DateFrom = dateFrom,
            DateTo = dateTo,
            IncludeSimilarityScores = includeSimilarity
        };
        
        var results = await _memoryService.SearchAsync(searchRequest);
        
        return new SearchMemoriesResponse
        {
            Query = query,
            ProfileName = profileName,
            Results = results.Select(r => new MemorySearchResult
            {
                MemoryId = r.Memory.Id,
                Content = r.Memory.Content,
                MemoryType = r.Memory.MemoryType,
                Importance = r.Memory.Importance,
                Tags = r.Memory.Tags,
                Timestamp = r.Memory.Timestamp,
                CreatedAt = r.Memory.CreatedAt,
                SimilarityScore = includeSimilarity ? r.SimilarityScore : null
            }).ToList(),
            TotalResults = results.Count,
            SearchTimestamp = DateTime.UtcNow
        };
    }
    catch (Exception ex) when (!(ex is MCPException))
    {
        throw new MCPInternalException("Failed to search memories", ex);
    }
}
```

---

## 3. Conversation Tools

### 3.1 GenerateResponse Tool

```csharp
[MCPTool("generate_response")]
[Description("Generate a response in the user's personality style based on context")]
public async Task<GenerateResponseResponse> GenerateResponseAsync(
    [Description("Profile identifier (required)")] string profileName,
    [Description("Input message/context (required)")] string inputMessage,
    [Description("Conversation context (optional, previous messages)")] ConversationMessage[] context = null,
    [Description("Response style (optional). Valid values: casual, formal, technical, emotional")]
    string responseStyle = "casual",
    [Description("Maximum response length (optional, 1-2000)")] int maxLength = 500,
    [Description("Include personality reasoning (optional, default: false)")] bool includeReasoning = false)
{
    // Input validation
    if (string.IsNullOrWhiteSpace(profileName))
        throw new MCPValidationException("Profile name is required");
        
    if (string.IsNullOrWhiteSpace(inputMessage))
        throw new MCPValidationException("Input message is required");
        
    if (maxLength < 1 || maxLength > 2000)
        throw new MCPValidationException("Max length must be between 1 and 2000");
    
    var validStyles = new[] { "casual", "formal", "technical", "emotional" };
    if (!validStyles.Contains(responseStyle))
        throw new MCPValidationException($"Invalid response style. Must be one of: {string.Join(", ", validStyles)}");
    
    if (context != null && context.Length > 50)
        throw new MCPValidationException("Context cannot contain more than 50 messages");
    
    try
    {
        var profile = await _profileService.GetByNameAsync(profileName);
        if (profile == null)
            throw new MCPResourceNotFoundException($"Profile '{profileName}' not found");
            
        var generateRequest = new GenerateResponseRequest
        {
            ProfileId = profile.Id,
            InputMessage = inputMessage,
            Context = context?.Select(c => new ContextMessage
            {
                Role = c.Role,
                Content = c.Content,
                Timestamp = c.Timestamp
            }).ToList() ?? new List<ContextMessage>(),
            ResponseStyle = responseStyle,
            MaxLength = maxLength,
            IncludeReasoning = includeReasoning
        };
        
        var response = await _conversationService.GenerateResponseAsync(generateRequest);
        
        return new GenerateResponseResponse
        {
            ProfileName = profileName,
            InputMessage = inputMessage,
            GeneratedResponse = response.Content,
            ResponseStyle = responseStyle,
            ActualLength = response.Content.Length,
            PersonalityReasoning = includeReasoning ? response.Reasoning : null,
            ConfidenceScore = response.ConfidenceScore,
            GeneratedAt = DateTime.UtcNow,
            ProcessingTimeMs = response.ProcessingTimeMs
        };
    }
    catch (Exception ex) when (!(ex is MCPException))
    {
        throw new MCPInternalException("Failed to generate response", ex);
    }
}
```

---

## 4. Profile Analytics Tools

### 4.1 GetProfileSummary Tool

```csharp
[MCPTool("get_profile_summary")]
[Description("Get comprehensive profile summary with statistics and insights")]
public async Task<ProfileSummaryResponse> GetProfileSummaryAsync(
    [Description("Profile identifier (required)")] string profileName,
    [Description("Include memory statistics (optional, default: true)")] bool includeMemoryStats = true,
    [Description("Include trait evolution (optional, default: false)")] bool includeTraitEvolution = false,
    [Description("Analysis depth (optional). Valid values: basic, detailed, comprehensive")]
    string analysisDepth = "detailed")
{
    // Input validation
    if (string.IsNullOrWhiteSpace(profileName))
        throw new MCPValidationException("Profile name is required");
    
    var validDepths = new[] { "basic", "detailed", "comprehensive" };
    if (!validDepths.Contains(analysisDepth))
        throw new MCPValidationException($"Invalid analysis depth. Must be one of: {string.Join(", ", validDepths)}");
    
    try
    {
        var profile = await _profileService.GetByNameAsync(profileName);
        if (profile == null)
            throw new MCPResourceNotFoundException($"Profile '{profileName}' not found");
            
        var summaryRequest = new ProfileSummaryRequest
        {
            ProfileId = profile.Id,
            IncludeMemoryStats = includeMemoryStats,
            IncludeTraitEvolution = includeTraitEvolution,
            AnalysisDepth = analysisDepth
        };
        
        var summary = await _analyticsService.GetProfileSummaryAsync(summaryRequest);
        
        return new ProfileSummaryResponse
        {
            ProfileName = profileName,
            ProfileId = profile.Id,
            CreatedAt = profile.CreatedAt,
            LastUpdated = profile.LastUpdated,
            TotalTraits = summary.TotalTraits,
            TotalMemories = includeMemoryStats ? summary.TotalMemories : null,
            MemoryStats = includeMemoryStats ? summary.MemoryStats : null,
            TraitDistribution = summary.TraitDistribution,
            TraitEvolution = includeTraitEvolution ? summary.TraitEvolution : null,
            PersonalityInsights = analysisDepth != "basic" ? summary.PersonalityInsights : null,
            Recommendations = analysisDepth == "comprehensive" ? summary.Recommendations : null,
            GeneratedAt = DateTime.UtcNow
        };
    }
    catch (Exception ex) when (!(ex is MCPException))
    {
        throw new MCPInternalException("Failed to get profile summary", ex);
    }
}
```

---

## 5. MCP Exception Classes

```csharp
public abstract class MCPException : Exception
{
    public string MCPErrorCode { get; }
    
    protected MCPException(string mcpErrorCode, string message) : base(message)
    {
        MCPErrorCode = mcpErrorCode;
    }
    
    protected MCPException(string mcpErrorCode, string message, Exception innerException) 
        : base(message, innerException)
    {
        MCPErrorCode = mcpErrorCode;
    }
}

public class MCPValidationException : MCPException
{
    public MCPValidationException(string message) 
        : base("VALIDATION_ERROR", message) { }
}

public class MCPResourceNotFoundException : MCPException
{
    public MCPResourceNotFoundException(string message) 
        : base("RESOURCE_NOT_FOUND", message) { }
}

public class MCPInternalException : MCPException
{
    public MCPInternalException(string message, Exception innerException = null) 
        : base("INTERNAL_ERROR", message, innerException) { }
}
```

---

## 6. MCP Error Response Format

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Profile name is required and cannot be empty",
    "details": {
      "parameter": "profileName",
      "provided": null,
      "expected": "non-empty string"
    },
    "timestamp": "2024-01-20T10:30:00.000Z",
    "requestId": "req_123456789"
  }
}
```

---

## 7. Service Dependencies

Each MCP tool requires the following service injections:

```csharp
public class MCPToolService
{
    private readonly IProfileService _profileService;
    private readonly IPersonalityService _personalityService;
    private readonly IMemoryService _memoryService;
    private readonly IConversationService _conversationService;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<MCPToolService> _logger;
    
    public MCPToolService(
        IProfileService profileService,
        IPersonalityService personalityService,
        IMemoryService memoryService,
        IConversationService conversationService,
        IAnalyticsService analyticsService,
        ILogger<MCPToolService> logger)
    {
        _profileService = profileService;
        _personalityService = personalityService;
        _memoryService = memoryService;
        _conversationService = conversationService;
        _analyticsService = analyticsService;
        _logger = logger;
    }
}
```

---

## 8. Registration and Configuration

```csharp
// Program.cs or Startup.cs
services.AddMCPTools(options =>
{
    options.ServerName = "DigitalMe";
    options.Version = "1.0.0";
    options.EnableLogging = true;
    options.EnableValidation = true;
    options.MaxConcurrentRequests = 10;
    options.RequestTimeoutMs = 30000;
});

services.Configure<MCPToolOptions>(configuration.GetSection("MCPTools"));
```

**Configuration Schema**:
```json
{
  "MCPTools": {
    "ServerName": "DigitalMe",
    "Version": "1.0.0",
    "EnableLogging": true,
    "EnableValidation": true,
    "MaxConcurrentRequests": 10,
    "RequestTimeoutMs": 30000,
    "AllowedOrigins": ["claude.ai"],
    "RateLimiting": {
      "RequestsPerMinute": 60,
      "BurstLimit": 10
    }
  }
}
```

---

## Success Criteria

**Tool Registration**:
- [ ] All 6 MCP tools register successfully with Claude runtime
- [ ] Tool metadata validates against MCP schema
- [ ] Parameter validation works for all inputs

**Functional Validation**:
- [ ] Each tool executes successfully with valid inputs  
- [ ] Error handling returns proper MCP error responses
- [ ] Response schemas validate against JSON schema definitions

**Performance Requirements**:
- [ ] Tool response time <500ms p95
- [ ] Memory usage <100MB per tool instance
- [ ] Concurrent request handling up to configured limit

**Integration Testing**:
- [ ] Tools successfully call backend services
- [ ] Database operations complete successfully
- [ ] Error propagation works correctly from services to MCP

This specification provides concrete, implementable MCP tool definitions that eliminate all human interpretation requirements. Each tool has exact method signatures, complete validation logic, error handling, and measurable success criteria.