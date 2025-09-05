# Personality Controller Architecture 🎮

> **Parent Plan**: [03-02-01-controllers-implementation.md](../03-02-01-controllers-implementation.md) | **Plan Type**: CONTROLLER ARCHITECTURE | **LLM Ready**: ✅ YES  
> **Prerequisites**: IPersonalityService interface | **Execution Time**: 2-3 days

📍 **Architecture** → **Implementation** → **Controllers** → **Personality**

## PersonalityController Architecture Overview

### Core Responsibilities
- **Profile Management**: CRUD operations for personality profiles
- **Trait Management**: Update individual personality traits  
- **Validation**: Request/response data validation
- **Error Handling**: Type-specific exception handling
- **Logging**: Structured logging with correlation IDs

### Class Structure Design

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PersonalityController : ControllerBase
{
    private readonly IPersonalityService _personalityService;
    private readonly ILogger<PersonalityController> _logger;
    
    // Constructor with DI
    // GetProfile endpoint - HTTP GET
    // CreateProfile endpoint - HTTP POST  
    // UpdateTrait endpoint - HTTP PUT
    // DeleteProfile endpoint - HTTP DELETE
    // Private mapping methods
}
```

### Endpoint Architecture

#### 1. GET /api/personality/{name}
**Purpose**: Retrieve personality profile by name
**Architecture Balance**: 85% design patterns, 15% implementation stub

```csharp
[HttpGet("{name}")]
[ProducesResponseType(typeof(PersonalityProfileDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetProfile([Required] string name)
{
    _logger.LogInformation("Fetching personality profile for {ProfileName}", name);
    
    // TODO: Implement validation logic
    // TODO: Call _personalityService.GetProfileAsync(name)
    // TODO: Handle null response with 404
    // TODO: Map entity to DTO
    // TODO: Return Ok(dto) or appropriate error response
    
    throw new NotImplementedException("GetProfile endpoint implementation pending");
}
```

#### 2. POST /api/personality
**Purpose**: Create new personality profile  
**Architecture Balance**: 85% design patterns, 15% implementation stub

```csharp
[HttpPost]
[ProducesResponseType(typeof(PersonalityProfileDto), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> CreateProfile([FromBody] CreatePersonalityProfileRequest request)
{
    _logger.LogInformation("Creating personality profile {ProfileName}", request.Name);
    
    // TODO: Implement ModelState validation
    // TODO: Check for existing profile conflicts  
    // TODO: Map request to entity
    // TODO: Call _personalityService.CreateProfileAsync()
    // TODO: Return CreatedAtAction with location header
    
    throw new NotImplementedException("CreateProfile endpoint implementation pending");
}
```

#### 3. PUT /api/personality/{name}/traits/{traitName}
**Purpose**: Update specific personality trait
**Architecture Balance**: 85% design patterns, 15% implementation stub

```csharp
[HttpPut("{name}/traits/{traitName}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
public async Task<IActionResult> UpdateTrait(string name, string traitName, [FromBody] UpdateTraitRequest request)
{
    _logger.LogInformation("Updating trait {TraitName} for profile {ProfileName}", traitName, name);
    
    // TODO: Validate profile exists
    // TODO: Validate trait value format
    // TODO: Call _personalityService.UpdateTraitAsync()
    // TODO: Return NoContent() on success
    
    throw new NotImplementedException("UpdateTrait endpoint implementation pending");
}
```

#### 4. DELETE /api/personality/{name}
**Purpose**: Delete personality profile
**Architecture Balance**: 85% design patterns, 15% implementation stub

```csharp
[HttpDelete("{name}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
public async Task<IActionResult> DeleteProfile([Required] string name)
{
    _logger.LogInformation("Deleting personality profile {ProfileName}", name);
    
    // TODO: Verify profile exists
    // TODO: Call _personalityService.DeleteProfileAsync()
    // TODO: Return NoContent() on success
    
    throw new NotImplementedException("DeleteProfile endpoint implementation pending");
}
```

### Exception Handling Architecture

```csharp
// Exception handling patterns to implement:
try
{
    // Service call
}
catch (ArgumentException ex)
{
    _logger.LogWarning(ex, "Invalid argument: {ProfileName}", profileName);
    return BadRequest(new ErrorResponse { Message = ex.Message, ErrorCode = "INVALID_ARGUMENT" });
}
catch (Exception ex) 
{
    _logger.LogError(ex, "Unexpected error: {ProfileName}", profileName);
    return StatusCode(500, new ErrorResponse { Message = "Internal server error", ErrorCode = "INTERNAL_ERROR" });
}
```

### Dependency Injection Configuration

```csharp
// Required services in DI container:
services.AddScoped<IPersonalityService, PersonalityService>();
services.AddLogging();

// Controller registration (automatic with [ApiController])
services.AddControllers();
```

### Success Criteria

✅ **Architectural Completeness**: All endpoint signatures defined
✅ **Error Handling Design**: Exception handling patterns specified  
✅ **Logging Strategy**: Structured logging approach defined
✅ **Validation Architecture**: Request validation patterns established
✅ **Response Types**: All HTTP status codes and response types documented
✅ **Dependencies**: Service dependencies clearly specified

### Implementation Guidance

1. **Start with Constructor**: Implement DI constructor first
2. **Add Endpoints One by One**: Implement each endpoint incrementally  
3. **Test After Each Endpoint**: Verify functionality before moving to next
4. **Add Exception Handling**: Implement comprehensive error handling
5. **Verify Swagger Documentation**: Ensure all endpoints appear in OpenAPI docs

---

## 🔗 NAVIGATION & DEPENDENCIES

### Prerequisites
- **IPersonalityService**: Service layer interface must exist
- **DTOs**: Request/Response models must be defined
- **Exception Types**: Custom exception classes must be available

### Next Steps  
- **Implement**: Fill in NotImplementedException stubs
- **Test**: Create unit tests for each endpoint
- **Integration**: Test with actual PersonalityService implementation

### Related Plans
- **Parent**: [03-02-01-controllers-implementation.md](../03-02-01-controllers-implementation.md)
- **Sibling**: [03-02-01-02-agent-controller.md](03-02-01-02-agent-controller.md)
- **DTOs**: [03-02-01-03-dto-models.md](03-02-01-03-dto-models.md)

---

## 📊 PLAN METADATA

- **Type**: CONTROLLER ARCHITECTURE PLAN  
- **LLM Ready**: ✅ YES
- **Implementation Depth**: 85% architecture / 15% implementation stubs
- **Execution Time**: 2-3 days
- **Code Coverage**: ~300 lines architectural guidance
- **Balance Compliance**: ✅ ARCHITECTURAL FOCUS maintained

### 🎯 ARCHITECTURE FOCUS INDICATORS
- **✅ Design Patterns**: Clear MVC controller patterns
- **✅ Dependency Architecture**: DI and service layer separation  
- **✅ Exception Design**: Comprehensive error handling strategy
- **✅ Implementation Stubs**: NotImplementedException placeholders
- **✅ Documentation**: XML comments and OpenAPI attributes
- **✅ Logging Architecture**: Structured logging patterns
- **✅ Validation Design**: Model validation and error responses