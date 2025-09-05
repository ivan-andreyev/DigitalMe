# Week 2: MCP Integration & LLM Services

**Родительский план**: [../00-MAIN_PLAN-Phase-Execution.md](../00-MAIN_PLAN-Phase-Execution.md)

> **Week 2 Focus**: MCP protocol implementation, LLM integration, personality engine, agent behavior adaptation

---

## 📅 **Daily Implementation Plan** *(5 дней)*

### **Day 1: Microsoft.SemanticKernel Setup** *(4 hours)*
- **Tasks**:
  - Install Microsoft.SemanticKernel v1.26.0
  - Configure Anthropic connector for Claude 3.5 Sonnet
  - Setup dependency injection for Semantic Kernel services
  - Create basic kernel configuration and memory store
- **Deliverables**: Working Semantic Kernel integration with Anthropic API
- **Success Criteria**: `curl` to Anthropic API returns HTTP 200 with valid response

### **Day 2: Personality Engine Core** *(4 hours)*
- **Tasks**:
  - Implement PersonalityService with trait calculation
  - Create dynamic system prompt generation engine
  - Implement mood analysis and personality adaptation
  - Setup personality template system with A/B testing capability
- **Deliverables**: Personality-aware system prompt generation
- **Success Criteria**: System prompts generated with calculated trait values

### **Day 3: Claude API Integration** *(3 hours)*
- **Tasks**:
  - Implement Claude API client with retry policies
  - Setup streaming response handling for real-time chat
  - Configure rate limiting and usage tracking
  - Implement conversation context management
- **Deliverables**: Working Claude API integration with streaming
- **Success Criteria**: Real-time chat responses with <2s latency

### **Day 4: WebSocket Chat Implementation** *(4 hours)*
- **Tasks**:
  - Setup SignalR for real-time WebSocket communication
  - Implement chat hub with authentication
  - Create message queuing and delivery system
  - Add typing indicators and connection status
- **Deliverables**: Real-time chat system with WebSocket support
- **Success Criteria**: WebSocket connection established with <500ms latency

### **Day 5: Behavioral Adaptation System** *(3 hours)*
- **Tasks**:
  - Implement conversation history analysis
  - Create mood detection from message patterns
  - Setup personality trait modifiers based on context
  - Add learning mechanism for personality adaptation
- **Deliverables**: Adaptive personality system with mood detection
- **Success Criteria**: Agent behavior adapts based on mood modifiers

---

## ✅ **Week 2 Success Criteria**

### **MCP Integration Requirements**:
- [ ] **Anthropic API**: Connection established and authenticated
- [ ] **Semantic Kernel**: Configured with Anthropic connector
- [ ] **API Response**: Claude 3.5 Sonnet returns valid completions
- [ ] **Rate Limiting**: Proper throttling and usage tracking implemented

### **Personality Engine Requirements**:
- [ ] **Trait Calculation**: Dynamic personality trait computation
- [ ] **System Prompts**: Context-aware prompt generation with personality
- [ ] **Mood Analysis**: NLP-based mood detection from messages
- [ ] **Template Engine**: Flexible prompt templates with A/B testing

### **Real-Time Communication Requirements**:
- [ ] **WebSocket Connection**: SignalR hub with authentication
- [ ] **Message Streaming**: Real-time response streaming from Claude API
- [ ] **Connection Management**: Proper connection lifecycle handling
- [ ] **Performance**: <500ms WebSocket latency, <2s LLM response time

### **Behavioral Adaptation Requirements**:
- [ ] **Context Analysis**: Conversation history pattern recognition
- [ ] **Personality Modifiers**: Mood-based trait adjustments
- [ ] **Learning System**: Adaptation based on interaction patterns
- [ ] **Consistency**: Personality coherence across conversations

---

## 🔧 **Technology Stack Extensions**

### **LLM Integration**:
- **SemanticKernel**: Microsoft.SemanticKernel v1.26.0
- **Anthropic Connector**: Microsoft.SemanticKernel.Connectors.Anthropic
- **Model**: Claude 3.5 Sonnet (claude-3-5-sonnet-20241022)
- **Memory Store**: In-memory with Redis option for production

### **Real-Time Communication**:
- **SignalR**: Microsoft.AspNetCore.SignalR for WebSocket support
- **Message Queue**: In-memory queue with RabbitMQ option
- **Connection Manager**: Custom hub with authentication and state management

### **Configuration Requirements**:
```json
{
  "Anthropic": {
    "ApiKey": "<anthropic-api-key>",
    "BaseUrl": "https://api.anthropic.com/v1",
    "DefaultModel": "claude-3-5-sonnet-20241022",
    "MaxTokens": 4000,
    "Temperature": 0.7
  },
  "SemanticKernel": {
    "ServiceId": "anthropic-claude",
    "MemoryStore": "InMemory",
    "LogLevel": "Information"
  },
  "PersonalityEngine": {
    "TemplateUpdateInterval": "00:05:00",
    "MoodAnalysisEnabled": true,
    "AdaptationLearningRate": 0.1
  }
}
```

---

## 🚨 **Prerequisites & Dependencies**

### **Week 1 Completion**:
- [ ] Functional API with authentication
- [ ] Database with PersonalityProfile entities
- [ ] Dependency injection container configured
- [ ] Health checks and logging operational

### **API Keys & External Services**:
- [ ] Anthropic API key configured and verified
- [ ] Rate limits and billing understood
- [ ] Network access to api.anthropic.com verified
- [ ] SSL certificates for HTTPS API calls

---

## 📊 **Implementation Architecture**

### **PersonalityService Implementation**:
```csharp
public interface IPersonalityService
{
    Task<string> GenerateSystemPromptAsync(string profileName, IEnumerable<Message> history);
    Task<PersonalityMood> AnalyzeMoodFromMessageAsync(string message);
    Task<PersonalityTraits> CalculateAdaptedTraitsAsync(PersonalityTraits base, PersonalityMood mood);
    Task UpdatePersonalityFromInteractionAsync(string profileName, ConversationAnalysis analysis);
}

public enum PersonalityMood
{
    Calm = 0, Focused = 1, Tired = 2, Irritated = 3, Excited = 4
}
```

### **Chat Hub Implementation**:
```csharp
[Authorize]
public class ChatHub : Hub
{
    public async Task SendMessage(string message);
    public async Task JoinPersonalitySession(string profileName);
    public async Task StartTyping();
    public async Task StopTyping();
    public override async Task OnConnectedAsync();
    public override async Task OnDisconnectedAsync(Exception exception);
}
```

---

## 🧪 **Testing Strategy**

### **Unit Tests**:
- [ ] **PersonalityService**: Trait calculation and prompt generation
- [ ] **Mood Analysis**: Pattern recognition and sentiment analysis
- [ ] **Template Engine**: Prompt template rendering and validation
- [ ] **API Client**: Anthropic API integration with mocking

### **Integration Tests**:
- [ ] **End-to-End Chat**: Complete message flow from WebSocket to LLM
- [ ] **Personality Adaptation**: Trait modification based on conversation
- [ ] **Error Handling**: API failures and recovery mechanisms
- [ ] **Performance**: Response time and throughput under load

### **Manual Testing Checklist**:
- [ ] Connect to WebSocket chat endpoint
- [ ] Send message and receive personality-aware response
- [ ] Verify mood detection affects response style
- [ ] Test connection recovery and error handling

---

## 🔄 **Next Steps**

- **Week 3**: [External Integrations](./Week-3-Integrations.md)
- **Dependencies**: MCP integration and LLM services fully functional
- **Handoff Criteria**: Real-time personality-aware chat system operational

---

**⏱️ Estimated Time**: 18 hours total (3-4 hours per day)  
**🎯 Key Milestone**: Personality-aware LLM integration with real-time chat