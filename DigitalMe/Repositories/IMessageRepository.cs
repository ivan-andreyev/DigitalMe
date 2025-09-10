using DigitalMe.Models;

namespace DigitalMe.Repositories;

public interface IMessageRepository
{
    Task<Message?> GetMessageAsync(Guid id);
    Task<IEnumerable<Message>> GetConversationMessagesAsync(Guid conversationId, int skip = 0, int take = 50);
    Task<Message> AddMessageAsync(Message message);
    Task<Message> UpdateMessageAsync(Message message);
    Task<bool> DeleteMessageAsync(Guid id);
    Task<int> GetMessageCountAsync(Guid conversationId);
}
