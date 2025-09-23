using System.Text.Json;
using DigitalMe.Data.Entities;
using DigitalMe.Models;
using DigitalMe.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services;

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IMvpPersonalityService _personalityService;
    private readonly ILogger<ConversationService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ConversationService(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IMvpPersonalityService personalityService,
        ILogger<ConversationService> logger,
        IServiceProvider serviceProvider)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _personalityService = personalityService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<Conversation> StartConversationAsync(string platform, string userId, string title = "")
    {
        var existingConversation = await _conversationRepository.GetActiveConversationAsync(platform, userId);
        if (existingConversation != null)
        {
            return existingConversation;
        }

        // Get Ivan's personality profile for the conversation
        var ivanProfile = await _personalityService.GetIvanProfileAsync();
        if (ivanProfile == null)
        {
            _logger.LogError("❌ Cannot create conversation: Ivan's personality profile not found");
            throw new InvalidOperationException("Ivan's personality profile not found. Cannot create conversation.");
        }

        var conversation = new Conversation
        {
            Platform = platform,
            UserId = userId,
            Title = string.IsNullOrEmpty(title) ? $"Conversation {DateTime.UtcNow:yyyy-MM-dd HH:mm}" : title,
            PersonalityProfileId = ivanProfile.Id // 🔧 FIX: Set required PersonalityProfileId
        };

        try
        {
            return await _conversationRepository.CreateConversationAsync(conversation);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("FOREIGN KEY constraint failed") == true)
        {
            _logger.LogError(ex, "🔥 FOREIGN KEY constraint failed when creating conversation. PersonalityProfile {ProfileId} may not exist in database.", ivanProfile.Id);

            // Graceful fallback: Try to create PersonalityProfile on-demand
            await EnsurePersonalityProfileExistsAsync(ivanProfile);

            // Retry conversation creation after ensuring PersonalityProfile exists
            return await _conversationRepository.CreateConversationAsync(conversation);
        }
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

    /// <summary>
    /// Ensures that PersonalityProfile exists in database to prevent Foreign Key constraint failures.
    /// This is a graceful fallback for handling data inconsistency issues.
    /// </summary>
    private async Task EnsurePersonalityProfileExistsAsync(PersonalityProfile profile)
    {
        try
        {
            _logger.LogInformation("🔧 Attempting to ensure PersonalityProfile {ProfileId} exists in database", profile.Id);

            // Try to get the repository through the service provider
            // For now, we'll try a simple approach - re-create the profile
            var personalityRepository = _serviceProvider.GetService<IPersonalityRepository>();
            if (personalityRepository == null)
            {
                _logger.LogWarning("⚠️ PersonalityRepository not available for PersonalityProfile recovery");
                return;
            }

            // Check if profile exists
            var existingProfile = await personalityRepository.GetProfileByIdAsync(profile.Id);
            if (existingProfile != null)
            {
                _logger.LogInformation("✅ PersonalityProfile {ProfileId} already exists in database", profile.Id);
                return;
            }

            // Create the missing profile
            _logger.LogWarning("🚨 PersonalityProfile {ProfileId} missing from database. Creating on-demand to prevent FK constraint failure.", profile.Id);
            await personalityRepository.CreateProfileAsync(profile);
            _logger.LogInformation("✅ Successfully created missing PersonalityProfile {ProfileId}", profile.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to ensure PersonalityProfile {ProfileId} exists. FK constraint may still fail.", profile.Id);
            // Don't throw - let the original FK constraint error surface
        }
    }
}
