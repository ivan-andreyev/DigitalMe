using DigitalMe.Models;
using DigitalMe.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DigitalMe.Services;

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        ILogger<ConversationService> logger)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _logger = logger;
    }

    public async Task<Conversation> StartConversationAsync(string platform, string userId, string title = "")
    {
        var existingConversation = await _conversationRepository.GetActiveConversationAsync(platform, userId);
        if (existingConversation != null)
        {
            return existingConversation;
        }

        var conversation = new Conversation
        {
            Platform = platform,
            UserId = userId,
            Title = string.IsNullOrEmpty(title) ? $"Conversation {DateTime.UtcNow:yyyy-MM-dd HH:mm}" : title
        };

        return await _conversationRepository.CreateConversationAsync(conversation);
    }

    public async Task<Conversation?> GetActiveConversationAsync(string platform, string userId)
    {
        return await _conversationRepository.GetActiveConversationAsync(platform, userId);
    }

    public async Task<Message> AddMessageAsync(Guid conversationId, string role, string content, Dictionary<string, object>? metadata = null)
    {
        // Validate that the conversation exists before adding a message
        var conversation = await _conversationRepository.GetConversationAsync(conversationId);
        if (conversation == null)
        {
            throw new ArgumentException($"Conversation with ID {conversationId} does not exist.", nameof(conversationId));
        }

        var message = new Message
        {
            ConversationId = conversationId,
            Role = role,
            Content = content,
            Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : "{}"
        };

        return await _messageRepository.AddMessageAsync(message);
    }

    public async Task<IEnumerable<Message>> GetConversationHistoryAsync(Guid conversationId, int limit = 50)
    {
        return await _messageRepository.GetConversationMessagesAsync(conversationId, 0, limit);
    }

    public async Task<Conversation> EndConversationAsync(Guid conversationId)
    {
        var conversation = await _conversationRepository.GetConversationAsync(conversationId);
        if (conversation == null)
        {
            throw new ArgumentException($"Conversation with ID {conversationId} not found");
        }

        conversation.IsActive = false;
        conversation.EndedAt = DateTime.UtcNow;

        return await _conversationRepository.UpdateConversationAsync(conversation);
    }

    public async Task<IEnumerable<Conversation>> GetUserConversationsAsync(string platform, string userId)
    {
        return await _conversationRepository.GetUserConversationsAsync(platform, userId);
    }
}