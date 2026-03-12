using Cocorra.DAL.DTOS.ChatDto;
using Cocorra.DAL.Models;
using Cocorra.DAL.Repository.FriendRepository;
using Cocorra.DAL.Repository.MessageRepository;
using Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cocorra.BLL.Services.ChatService
{
    public class ChatService : ResponseHandler, IChatService
    {
        private readonly IFriendRepository _friendRepo;
        private readonly IMessageRepository _messageRepo;

        public ChatService(IFriendRepository friendRepo, IMessageRepository messageRepo)
        {
            _friendRepo = friendRepo;
            _messageRepo = messageRepo;
        }
        public async Task<Response<IEnumerable<MessageDto>>> GetChatHistoryAsync(Guid currentUserId, Guid friendId, int pageNumber, int pageSize)
        {
            // 1. نتأكد إنهم أصدقاء أصلاً
            var friendship = await _friendRepo.GetFriendshipRelationAsync(currentUserId, friendId);
            if (friendship == null || friendship.Status != Cocorra.DAL.Enums.FriendRequestStatus.Accepted)
                return BadRequest<IEnumerable<MessageDto>>("You can only view chat history with confirmed friends.");

            // 2. نجيب الرسايل من الريبو بالصفحات
            var messages = await _messageRepo.GetChatHistoryAsync(currentUserId, friendId, pageNumber, pageSize);

            // 3. نعملها Map للـ DTO
            var dtoList = messages.Select(m => new MessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            }).ToList();

            return Success<IEnumerable<MessageDto>>(dtoList);
        }

        public async Task<Response<MessageDto>> SaveMessageAsync(Guid senderId, Guid receiverId, string content)
        {
            // 1. حماية: هل هما أصدقاء؟ (عشان لو حد حاول يبعت API بـ Postman لواحد مش صاحبه)
            var friendship = await _friendRepo.GetFriendshipRelationAsync(senderId, receiverId);
            if (friendship == null || friendship.Status != Cocorra.DAL.Enums.FriendRequestStatus.Accepted)
                return BadRequest<MessageDto>("You can only send messages to confirmed friends.");

            // 2. نحفظ الرسالة
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _messageRepo.AddAsync(message);

            var dto = new MessageDto
            {
                Id = message.Id,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Content = message.Content,
                CreatedAt = message.CreatedAt
            };

            return Success(dto);
        }
        // 1. تعديل دالة لستة الأصدقاء بالكامل
        public async Task<Response<IEnumerable<ChatFriendDto>>> GetChatFriendsListAsync(Guid currentUserId)
        {
            var friends = await _friendRepo.GetAcceptedFriendsAsync(currentUserId);
            var dtoList = new List<ChatFriendDto>();

            foreach (var friend in friends)
            {
                var lastMsg = await _messageRepo.GetLastMessageAsync(currentUserId, friend.Id);
                var unreadCount = await _messageRepo.GetUnreadCountAsync(senderId: friend.Id, receiverId: currentUserId);

                dtoList.Add(new ChatFriendDto
                {
                    FriendId = friend.Id,
                    FullName = $"{friend.FirstName} {friend.LastName}",
                    LastMessage = lastMsg?.Content,
                    LastMessageDate = lastMsg?.CreatedAt,
                    UnreadCount = unreadCount
                });
            }

            // بنرتب اللستة بحيث اللي باعتلي رسالة أحدث يظهر فوق (زي الواتساب)
            var sortedList = dtoList.OrderByDescending(d => d.LastMessageDate ?? DateTime.MinValue).ToList();

            return Success<IEnumerable<ChatFriendDto>>(sortedList);
        }

        public async Task<Response<string>> MarkMessagesAsReadAsync(Guid currentUserId, Guid friendId)
        {
            // currentUserId هو اللي بيقرأ، يعني هو الـ receiver، والـ friendId هو الـ sender
            await _messageRepo.MarkMessagesAsReadAsync(senderId: friendId, receiverId: currentUserId);
            return Success("Messages marked as read.");
        }
    }
}