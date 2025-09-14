using Microsoft.EntityFrameworkCore;
using DigitalMe.Data;
using DigitalMe.Models;

namespace DigitalMe.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly DigitalMeDbContext _context;

    public MessageRepository(DigitalMeDbContext context)
    {
        _context = context;
    }

    public async Task<Message?> GetMessageAsync(Guid id)
    {
        return await _context.Messages
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<Message>> GetConversationMessagesAsync(Guid conversationId, int skip = 0, int take = 50)
    {
        return await _context.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Message> AddMessageAsync(Message message)
    {
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<Message> UpdateMessageAsync(Message message)
    {
        _context.Messages.Update(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<bool> DeleteMessageAsync(Guid id)
    {
        var message = await _context.Messages.FindAsync(id);
        if (message == null)
        {
            return false;
        }

        _context.Messages.Remove(message);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetMessageCountAsync(Guid conversationId)
    {
        return await _context.Messages
            .Where(m => m.ConversationId == conversationId)
            .CountAsync();
    }
}
