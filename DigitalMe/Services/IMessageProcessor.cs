using DigitalMe.DTOs;
using DigitalMe.Models;
using DigitalMe.Services.AgentBehavior;

namespace DigitalMe.Services;

public interface IMessageProcessor
{
    Task<ProcessMessageResult> ProcessUserMessageAsync(ChatRequestDto request);
    Task<ProcessAgentResponseResult> ProcessAgentResponseAsync(ChatRequestDto request, Guid conversationId);
}

public record ProcessMessageResult(
    Conversation Conversation,
    Message UserMessage,
    string GroupName);

public record ProcessAgentResponseResult(
    Message AssistantMessage,
    AgentResponse AgentResponse);