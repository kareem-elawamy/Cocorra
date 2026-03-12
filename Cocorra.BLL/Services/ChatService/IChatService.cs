using Cocorra.DAL.DTOS.ChatDto;
using Core.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cocorra.BLL.Services.ChatService
{
    public interface IChatService
    {
        Task<Response<IEnumerable<ChatFriendDto>>> GetChatFriendsListAsync(Guid currentUserId);
        Task<Response<IEnumerable<MessageDto>>> GetChatHistoryAsync(Guid currentUserId, Guid friendId, int pageNumber, int pageSize);
        Task<Response<MessageDto>> SaveMessageAsync(Guid senderId, Guid receiverId, string content);
        Task<Response<string>> MarkMessagesAsReadAsync(Guid currentUserId, Guid friendId);
    }
}
