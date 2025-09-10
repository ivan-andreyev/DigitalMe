using DigitalMe.Models;

namespace DigitalMe.Services;

public interface IConversationService
{
    Task<Conversation> StartConversationAsync(string platform, string userId, string title = "");
    Task<Conversation?> GetActiveConversationAsync(string platform, string userId);
    Task<Message> AddMessageAsync(Guid conversationId, string role, string content, Dictionary<string, object>? metadata = null);
    Task<IEnumerable<Message>> GetConversationHistoryAsync(Guid conversationId, int limit = 50);
    Task<Conversation> EndConversationAsync(Guid conversationId);
    Task<IEnumerable<Conversation>> GetUserConversationsAsync(string platform, string userId);
}
