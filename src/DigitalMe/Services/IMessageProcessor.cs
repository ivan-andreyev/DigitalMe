using DigitalMe.Common;
using DigitalMe.DTOs;
using DigitalMe.Models;
using DigitalMe.Services.AgentBehavior;

namespace DigitalMe.Services;

public interface IMessageProcessor
{
    Task<Result<ProcessMessageResult>> ProcessUserMessageAsync(ChatRequestDto request);
    Task<Result<ProcessAgentResponseResult>> ProcessAgentResponseAsync(ChatRequestDto request, Guid conversationId);
}

public record ProcessMessageResult(
    Conversation Conversation,
    Message UserMessage,
    string GroupName);

public record ProcessAgentResponseResult(
    Message AssistantMessage,
    AgentResponse AgentResponse);
