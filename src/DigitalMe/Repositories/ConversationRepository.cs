using DigitalMe.Data;
using DigitalMe.Models;
using Microsoft.EntityFrameworkCore;

namespace DigitalMe.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly DigitalMeDbContext _context;

    public ConversationRepository(DigitalMeDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation?> GetConversationAsync(Guid id)
    {
        return await _context.Conversations
            .Include(c => c.Messages.OrderBy(m => m.Timestamp))
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Conversation?> GetActiveConversationAsync(string platform, string userId)
    {
        return await _context.Conversations
            .Include(c => c.Messages.OrderBy(m => m.Timestamp))
            .FirstOrDefaultAsync(c => c.Platform == platform && c.UserId == userId && c.IsActive);
    }

    public async Task<IEnumerable<Conversation>> GetUserConversationsAsync(string platform, string userId)
    {
        return await _context.Conversations
            .Where(c => c.Platform == platform && c.UserId == userId)
            .OrderByDescending(c => c.StartedAt)
            .ToListAsync();
    }

    public async Task<Conversation> CreateConversationAsync(Conversation conversation)
    {
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();
        return conversation;
    }

    public async Task<Conversation> UpdateConversationAsync(Conversation conversation)
    {
        _context.Conversations.Update(conversation);
        await _context.SaveChangesAsync();
        return conversation;
    }

    public async Task<bool> DeleteConversationAsync(Guid id)
    {
        var conversation = await _context.Conversations.FindAsync(id);
        if (conversation == null)
        {
            return false;
        }

        _context.Conversations.Remove(conversation);
        await _context.SaveChangesAsync();
        return true;
    }
}
