using Cocorra.DAL.Data;
using Cocorra.DAL.Models;
using Cocorra.DAL.Repository.GenericRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cocorra.DAL.Repository.MessageRepository
{
    public class MessageRepository : GenericRepositoryAsync<Message>, IMessageRepository
    {
        public MessageRepository(AppDbContext dbContext) : base(dbContext)
        {
        }
        public async Task<List<Message>> GetChatHistoryAsync(Guid userId1, Guid userId2, int pageNumber, int pageSize)
        {
            var query = _dbContext.Messages
                .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                            (m.SenderId == userId2 && m.ReceiverId == userId1));

            var pagedMessages = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize) 
                .ToListAsync();

            return pagedMessages.OrderBy(m => m.CreatedAt).ToList();
        }
        public async Task<Message?> GetLastMessageAsync(Guid userId1, Guid userId2)
        {
            return await _dbContext.Messages
                .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                            (m.SenderId == userId2 && m.ReceiverId == userId1))
                .OrderByDescending(m => m.CreatedAt) // بنرتب من الأحدث للأقدم
                .FirstOrDefaultAsync(); // بناخد أول واحدة بس
        }

        public async Task<int> GetUnreadCountAsync(Guid senderId, Guid receiverId)
        {
            // بنعد الرسايل اللي مبعوتالي أنا (receiverId) ومقرتهاش
            return await _dbContext.Messages
                .CountAsync(m => m.SenderId == senderId && m.ReceiverId == receiverId && !m.IsRead);
        }

        public async Task MarkMessagesAsReadAsync(Guid senderId, Guid receiverId)
        {
            // بنجيب كل الرسايل اللي مبعوتالي من الشخص ده ولسه False
            var unreadMessages = await _dbContext.Messages
                .Where(m => m.SenderId == senderId && m.ReceiverId == receiverId && !m.IsRead)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true; // تحويل لـ Seen
                }
                _dbContext.Messages.UpdateRange(unreadMessages);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
