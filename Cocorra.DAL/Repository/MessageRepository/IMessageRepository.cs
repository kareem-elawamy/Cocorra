using Cocorra.DAL.Models;
using Cocorra.DAL.Repository.GenericRepository;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cocorra.DAL.Repository.MessageRepository
{
    public interface IMessageRepository : IGenericRepositoryAsync<Message>
    {
        Task<List<Message>> GetChatHistoryAsync(Guid userId1, Guid userId2, int pageNumber, int pageSize); Task<Message?> GetLastMessageAsync(Guid userId1, Guid userId2);
        Task<int> GetUnreadCountAsync(Guid senderId, Guid receiverId);
        Task MarkMessagesAsReadAsync(Guid senderId, Guid receiverId);
    }
}
