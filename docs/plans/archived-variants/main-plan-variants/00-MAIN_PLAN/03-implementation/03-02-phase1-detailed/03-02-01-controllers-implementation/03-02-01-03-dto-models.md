# DTO Models Architecture 📝

> **Parent Plan**: [03-02-01-controllers-implementation.md](../03-02-01-controllers-implementation.md) | **Plan Type**: DTO ARCHITECTURE | **LLM Ready**: ✅ YES  
> **Prerequisites**: System.ComponentModel.DataAnnotations | **Execution Time**: 1-2 days

📍 **Architecture** → **Implementation** → **Controllers** → **DTOs**

## DTO Models Architecture Overview

### Core Responsibilities
- **Request Validation**: Input data validation with DataAnnotations
- **Response Serialization**: Consistent JSON output format
- **Data Transfer**: Clean separation between API and domain models
- **Type Safety**: Strongly-typed request/response contracts
- **Documentation**: Support for OpenAPI/Swagger documentation

### DTO Architecture Patterns

#### Request DTOs - Input Validation Architecture
```csharp
// Pattern for all request DTOs:
public class RequestDto
{
    [Required(ErrorMessage = "Field is required")]
    [StringLength(MaxLength, ErrorMessage = "Field cannot exceed {1} characters")]
    public string Field { get; set; } = default!;
    
    // Optional fields nullable
    public string? OptionalField { get; set; }
    
    // Complex types allowed
    public Dictionary<string, object>? Metadata { get; set; }
}
```

#### Response DTOs - Output Serialization Architecture
```csharp
// Pattern for all response DTOs:
public class ResponseDto
{
    // Always include ID for entity responses
    public Guid Id { get; set; }
    
    // Essential fields non-nullable
    public string Name { get; set; } = default!;
    
    // Optional fields nullable
    public string? Description { get; set; }
    
    // Metadata and timestamps
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}
```

## Request DTO Models

### 1. ChatRequest Architecture
**Purpose**: Chat message input validation
**Architecture Balance**: 85% validation design, 15% implementation stub

```csharp
namespace DigitalMe.API.Models;

public class ChatRequest
{
    [Required(ErrorMessage = "Profile name is required")]
    [StringLength(100, ErrorMessage = "Profile name cannot exceed 100 characters")]
    public string ProfileName { get; set; } = default!;

    [Required(ErrorMessage = "Message is required")]  
    [StringLength(10000, ErrorMessage = "Message cannot exceed 10000 characters")]
    public string Message { get; set; } = default!;

    // TODO: Add validation for Guid format if provided
    public Guid? ConversationId { get; set; }

    [StringLength(50, ErrorMessage = "Platform name cannot exceed 50 characters")]  
    public string? Platform { get; set; } = "Web";

    [Required(ErrorMessage = "User ID is required")]
    [StringLength(100, ErrorMessage = "User ID cannot exceed 100 characters")]
    public string UserId { get; set; } = default!;

    // TODO: Add custom validation for metadata structure
    public Dictionary<string, object>? Metadata { get; set; }
}
```

### 2. CreatePersonalityProfileRequest Architecture  
**Purpose**: Personality profile creation validation
**Architecture Balance**: 85% validation design, 15% implementation stub

```csharp
namespace DigitalMe.API.Models;

public class CreatePersonalityProfileRequest
{
    [Required(ErrorMessage = "Profile name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be 1-100 characters")]
    // TODO: Add regex validation for valid profile name format
    public string Name { get; set; } = default!;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    // TODO: Add custom validation for trait structure
    // TODO: Validate trait keys against allowed trait names
    public Dictionary<string, object>? CoreTraits { get; set; }

    // TODO: Add validation for communication style format
    public Dictionary<string, object>? CommunicationStyle { get; set; }
}

public class UpdateTraitRequest
{
    [Required(ErrorMessage = "Trait value is required")]
    // TODO: Add custom validation based on trait type
    public object Value { get; set; } = default!;
}
```

## Response DTO Models

### 1. ChatResponse Architecture
**Purpose**: Chat response with metadata
**Architecture Balance**: 85% structure design, 15% implementation stub

```csharp
namespace DigitalMe.API.Models;

public class ChatResponse  
{
    public Guid ConversationId { get; set; }
    public Guid MessageId { get; set; }
    public string Message { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public string PersonalityName { get; set; } = default!;
    
    // TODO: Add validation range for confidence (0.0 - 1.0)
    public double Confidence { get; set; }
    
    public TimeSpan ProcessingTime { get; set; }
    
    // TODO: Add additional response metadata
    // public string? Mood { get; set; }
    // public Dictionary<string, object>? Context { get; set; }
}
```

### 2. PersonalityProfileDto Architecture
**Purpose**: Personality profile response
**Architecture Balance**: 85% structure design, 15% implementation stub

```csharp
namespace DigitalMe.API.Models;

public class PersonalityProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    
    public Dictionary<string, object> CoreTraits { get; set; } = new();
    public Dictionary<string, object> CommunicationStyle { get; set; } = new();
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // TODO: Add computed properties for profile statistics
    public int TraitsCount { get; set; }
    // public int ConversationsCount { get; set; }  
    // public DateTime? LastInteraction { get; set; }
}
```

### 3. ConversationHistoryResponse Architecture
**Purpose**: Conversation history with pagination
**Architecture Balance**: 85% structure design, 15% implementation stub

```csharp
namespace DigitalMe.API.Models;

public class ConversationHistoryResponse
{
    public Guid ConversationId { get; set; }
    public string? Title { get; set; }
    public int MessagesCount { get; set; }
    
    public List<MessageDto> Messages { get; set; } = new();
    
    // TODO: Add pagination metadata
    // public int TotalMessages { get; set; }
    // public bool HasMore { get; set; }
    // public DateTime? OldestMessage { get; set; }
}

public class MessageDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = default!;
    public string Role { get; set; } = default!; // "user" | "assistant"
    public DateTime Timestamp { get; set; }
    public string? UserId { get; set; }
    
    // TODO: Add message metadata
    // public double? Confidence { get; set; }
    // public string? Mood { get; set; }
}
```

## Error Response Architecture

### ErrorResponse DTO
**Purpose**: Consistent error response format
**Architecture Balance**: 85% error handling design, 15% implementation stub

```csharp
namespace DigitalMe.API.Models;

public class ErrorResponse
{
    public string Message { get; set; } = default!;
    public string ErrorCode { get; set; } = default!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // TODO: Add structured error details
    public Dictionary<string, object>? Details { get; set; }
    
    // TODO: Add validation error support
    // public List<ValidationError>? ValidationErrors { get; set; }
}

// TODO: Add validation error structure
// public class ValidationError  
// {
//     public string Field { get; set; } = default!;
//     public string Message { get; set; } = default!;
//     public object? AttemptedValue { get; set; }
// }
```

## Custom Validation Architecture

### Trait Validation Design
```csharp
// TODO: Implement custom validation attributes
// [ValidTraitValue] - Validates trait values based on trait type
// [ValidProfileName] - Validates profile name format
// [ValidConversationId] - Validates conversation ID format

// Example custom validation attribute architecture:
public class ValidTraitValueAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        // TODO: Implement trait-specific validation logic
        // TODO: Check trait value types and ranges
        // TODO: Validate against allowed trait categories
        throw new NotImplementedException("Custom trait validation pending");
    }
}
```

### Request Validation Pipeline
```csharp
// Validation pipeline architecture for controllers:
public async Task<IActionResult> Action([FromBody] RequestDto request)
{
    // 1. Automatic model validation via DataAnnotations
    if (!ModelState.IsValid)
    {
        // TODO: Transform ModelState to structured error response
        return BadRequest(CreateValidationErrorResponse(ModelState));
    }
    
    // 2. Custom business validation
    // TODO: Implement business rule validation
    // TODO: Cross-field validation logic
    
    // 3. Proceed with business logic
    throw new NotImplementedException("Business logic implementation pending");
}
```

## Success Criteria

✅ **Validation Architecture**: Complete DataAnnotations validation design
✅ **Response Structure**: Consistent response format across all DTOs
✅ **Error Handling**: Structured error response format
✅ **Type Safety**: Strongly-typed contracts for all endpoints
✅ **Extensibility**: Architecture supports future additions
✅ **Documentation**: OpenAPI-compatible attribute design

### Implementation Guidance

1. **Start with Core DTOs**: Implement ChatRequest and ChatResponse first
2. **Add Validation**: Implement all DataAnnotations validation
3. **Custom Validation**: Add business-specific validation attributes
4. **Error Responses**: Implement consistent error response format
5. **Documentation**: Add XML comments for Swagger generation

---

## 🔗 NAVIGATION & DEPENDENCIES

### Prerequisites  
- **System.ComponentModel.DataAnnotations**: For validation attributes
- **Newtonsoft.Json or System.Text.Json**: For serialization configuration
- **FluentValidation (Optional)**: For complex validation scenarios

### Next Steps
- **Implement**: Fill in NotImplementedException stubs  
- **Validation Testing**: Create comprehensive validation tests
- **Serialization**: Configure JSON serialization options

### Related Plans
- **Parent**: [03-02-01-controllers-implementation.md](../03-02-01-controllers-implementation.md) 
- **Controllers**: [03-02-01-01-personality-controller.md](03-02-01-01-personality-controller.md)
- **Services**: Service layer interfaces for DTO mapping

---

## 📊 PLAN METADATA

- **Type**: DTO ARCHITECTURE PLAN
- **LLM Ready**: ✅ YES
- **Implementation Depth**: 85% architecture / 15% implementation stubs  
- **Execution Time**: 1-2 days
- **Code Coverage**: ~400 lines architectural guidance
- **Balance Compliance**: ✅ ARCHITECTURAL FOCUS maintained

### 🎯 DTO ARCHITECTURE INDICATORS
- **✅ Validation Design**: Comprehensive DataAnnotations architecture
- **✅ Type Safety**: Strongly-typed request/response contracts
- **✅ Error Handling**: Consistent error response structure
- **✅ Implementation Stubs**: NotImplementedException placeholders
- **✅ Extensibility**: Architecture supports future enhancements  
- **✅ Documentation**: OpenAPI/Swagger compatibility
- **✅ Serialization**: JSON serialization considerations