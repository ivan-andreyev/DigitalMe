# Integration Tests Setup 🔗

> **Parent Plan**: [03-02-05-testing-implementation.md](../03-02-05-testing-implementation.md) | **Plan Type**: TESTING PLAN | **LLM Ready**: ✅ YES  
> **Prerequisites**: Application setup complete | **Execution Time**: 1 day

📍 **Architecture** → **Implementation** → **Testing** → **Integration Tests**

## Integration Testing Architecture

### Test Infrastructure Setup
**Target File**: `Tests/Integration/WebApplicationFactory.cs`
**Strategy**: TestServer with real database and mocked external services

### Database Integration Tests

#### Repository Integration Tests
**File**: `Tests/Integration/Repositories/ConversationRepositoryIntegrationTests.cs`

```csharp
[TestClass]
public class ConversationRepositoryIntegrationTests : IDisposable
{
    private readonly DigitalMeDbContext _context;
    private readonly ConversationRepository _repository;

    public ConversationRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<DigitalMeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new DigitalMeDbContext(options);
        _repository = new ConversationRepository(_context, Mock.Of<ILogger<ConversationRepository>>());
    }

    [TestMethod]
    public async Task AddAsync_ValidConversation_SavesSuccessfully()
    {
        // Test implementation with real database operations
    }
}
```

### API Integration Tests
**File**: `Tests/Integration/Controllers/PersonalityControllerIntegrationTests.cs`
**Strategy**: Full HTTP request/response testing

### External Service Integration
**MCP Service Tests**: Mock external MCP API responses
**Database Tests**: Real database operations with test data cleanup

### Implementation Success Criteria

✅ **End-to-End Testing**: Complete API workflows tested
✅ **Database Integration**: Real database operations verified
✅ **External Service Mocking**: MCP service responses mocked
✅ **Test Data Management**: Setup and cleanup strategies

---

## 📊 PLAN METADATA

- **Type**: TESTING PLAN  
- **LLM Ready**: ✅ YES
- **Implementation Depth**: Integration test structure and setup
- **Execution Time**: 1 day
- **Lines**: 75 (under 400 limit)