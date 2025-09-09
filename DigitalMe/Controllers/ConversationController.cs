using Microsoft.AspNetCore.Mvc;
using DigitalMe.Services;
using DigitalMe.DTOs;
using System.Text.Json;

namespace DigitalMe.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConversationController : ControllerBase
{
    private readonly IConversationService _conversationService;

    public ConversationController(IConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    [HttpPost]
    public async Task<ActionResult<ConversationDto>> StartConversation([FromBody] CreateConversationDto dto)
    {
        var conversation = await _conversationService.StartConversationAsync(dto.Platform, dto.UserId, dto.Title);
        
        return Ok(new ConversationDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            Platform = conversation.Platform,
            UserId = conversation.UserId,
            StartedAt = conversation.StartedAt,
            EndedAt = conversation.EndedAt,
            IsActive = conversation.IsActive,
            Messages = new List<MessageDto>()
        });
    }

    [HttpGet("active")]
    public async Task<ActionResult<ConversationDto>> GetActiveConversation([FromQuery] string platform, [FromQuery] string userId)
    {
        var conversation = await _conversationService.GetActiveConversationAsync(platform, userId);
        if (conversation == null)
        {
            return NotFound("No active conversation found");
        }

        var messages = conversation.Messages.Select(m => new MessageDto
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            Role = m.Role,
            Content = m.Content,
            Timestamp = m.Timestamp,
            Metadata = string.IsNullOrEmpty(m.Metadata) ? new Dictionary<string, object>() : JsonSerializer.Deserialize<Dictionary<string, object>>(m.Metadata) ?? new Dictionary<string, object>()
        }).ToList();

        return Ok(new ConversationDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            Platform = conversation.Platform,
            UserId = conversation.UserId,
            StartedAt = conversation.StartedAt,
            EndedAt = conversation.EndedAt,
            IsActive = conversation.IsActive,
            Messages = messages
        });
    }

    [HttpGet("{conversationId}/messages")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessages(Guid conversationId, [FromQuery] int limit = 50)
    {
        var messages = await _conversationService.GetConversationHistoryAsync(conversationId, limit);
        
        var messageDtos = messages.Select(m => new MessageDto
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            Role = m.Role,
            Content = m.Content,
            Timestamp = m.Timestamp,
            Metadata = string.IsNullOrEmpty(m.Metadata) ? new Dictionary<string, object>() : JsonSerializer.Deserialize<Dictionary<string, object>>(m.Metadata) ?? new Dictionary<string, object>()
        });

        return Ok(messageDtos);
    }

    [HttpPost("{conversationId}/messages")]
    public async Task<ActionResult<MessageDto>> AddMessage(Guid conversationId, [FromBody] CreateMessageDto dto)
    {
        try
        {
            var message = await _conversationService.AddMessageAsync(conversationId, dto.Role, dto.Content, dto.Metadata);
            
            return Ok(new MessageDto
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                Role = message.Role,
                Content = message.Content,
                Timestamp = message.Timestamp,
                Metadata = dto.Metadata ?? new Dictionary<string, object>()
            });
        }
        catch (ArgumentException ex) when (ex.ParamName == "conversationId")
        {
            return NotFound($"Conversation with ID {conversationId} does not exist.");
        }
    }

    [HttpPost("{conversationId}/end")]
    public async Task<ActionResult<ConversationDto>> EndConversation(Guid conversationId)
    {
        try
        {
            var conversation = await _conversationService.EndConversationAsync(conversationId);
            
            return Ok(new ConversationDto
            {
                Id = conversation.Id,
                Title = conversation.Title,
                Platform = conversation.Platform,
                UserId = conversation.UserId,
                StartedAt = conversation.StartedAt,
                EndedAt = conversation.EndedAt,
                IsActive = conversation.IsActive,
                Messages = new List<MessageDto>()
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("user")]
    public async Task<ActionResult<IEnumerable<ConversationDto>>> GetUserConversations([FromQuery] string platform, [FromQuery] string userId)
    {
        var conversations = await _conversationService.GetUserConversationsAsync(platform, userId);
        
        var conversationDtos = conversations.Select(c => new ConversationDto
        {
            Id = c.Id,
            Title = c.Title,
            Platform = c.Platform,
            UserId = c.UserId,
            StartedAt = c.StartedAt,
            EndedAt = c.EndedAt,
            IsActive = c.IsActive,
            Messages = new List<MessageDto>()
        });

        return Ok(conversationDtos);
    }
}