using DigitalMe.Models;

namespace DigitalMe.Repositories;

public interface IConversationRepository
{
    Task<Conversation?> GetConversationAsync(Guid id);
    Task<Conversation?> GetActiveConversationAsync(string platform, string userId);
    Task<IEnumerable<Conversation>> GetUserConversationsAsync(string platform, string userId);
    Task<Conversation> CreateConversationAsync(Conversation conversation);
    Task<Conversation> UpdateConversationAsync(Conversation conversation);
    Task<bool> DeleteConversationAsync(Guid id);
}