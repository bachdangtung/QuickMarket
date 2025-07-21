using BussinessLogic.Models;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Implementations
{
    public class MessageRepository : IMessageRepository
    {
        private readonly QuickMarketContext _context;

        public MessageRepository(QuickMarketContext context)
        {
            _context = context;
        }

        public async Task<Message> CreateMessage(Message message)
        {
            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<List<Message>> GetMessagesBetweenUsers(int user1Id, int user2Id)
        {
            return await _context.Messages
                .Where(m => 
                    (m.FromUserId == user1Id && m.ToUserId == user2Id) || 
                    (m.FromUserId == user2Id && m.ToUserId == user1Id))
                .OrderBy(m => m.SentTime)
                .Include(m => m.FromUser)
                .Include(m => m.ToUser)
                .ToListAsync();
        }

        public async Task<List<int>> GetUniqueChatterIds(int userId)
        {
            // Get all users that current user has chatted with
            var fromUserIds = await _context.Messages
                .Where(m => m.ToUserId == userId)
                .Select(m => m.FromUserId)
                .Distinct()
                .ToListAsync();

            var toUserIds = await _context.Messages
                .Where(m => m.FromUserId == userId)
                .Select(m => m.ToUserId)
                .Distinct()
                .ToListAsync();

            // Combine the lists and remove duplicates
            return fromUserIds.Union(toUserIds).ToList();
        }

        public async Task<User> GetUserById(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<ChatProduct> AddChatProduct(ChatProduct chatProduct)
        {
            await _context.ChatProducts.AddAsync(chatProduct);
            await _context.SaveChangesAsync();
            return chatProduct;
        }
    }
}
