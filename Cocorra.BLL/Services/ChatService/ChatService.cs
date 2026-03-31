using Cocorra.BLL.Services.NotificationService;
using Cocorra.DAL.DTOS.ChatDto;
using Cocorra.DAL.Models;
using Cocorra.DAL.Repository.FriendRepository;
using Cocorra.DAL.Repository.MessageRepository;
using Cocorra.BLL.Base;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Cocorra.BLL.Services.Events.ChatEvents;

namespace Cocorra.BLL.Services.ChatService
{
    public class ChatService : ResponseHandler, IChatService
    {
        private readonly IFriendRepository _friendRepo;
        private readonly IMessageRepository _messageRepo;
        private readonly IPushNotificationService _pushService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMediator _mediator; 

        public ChatService(IFriendRepository friendRepo, IMessageRepository messageRepo, IPushNotificationService pushService, UserManager<ApplicationUser> userManager, IMediator mediator)
        {
            _friendRepo = friendRepo;
            _messageRepo = messageRepo;
            _pushService = pushService;
            _userManager = userManager;
            _mediator = mediator;
        }

        public async Task<Response<IEnumerable<MessageDto>>> GetChatHistoryAsync(Guid currentUserId, Guid friendId, int pageNumber, int pageSize)
        {
            var friendship = await _friendRepo.GetFriendshipRelationAsync(currentUserId, friendId);
            if (friendship == null || friendship.Status != Cocorra.DAL.Enums.FriendRequestStatus.Accepted)
                return BadRequest<IEnumerable<MessageDto>>("You can only view chat history with confirmed friends.");

            var messages = await _messageRepo.GetChatHistoryAsync(currentUserId, friendId, pageNumber, pageSize);

            var dtoList = messages.Select(m => new MessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                Content = m.Content,
                IsRead = m.IsRead,
                CreatedAt = m.CreatedAt
            }).ToList();

            return Success<IEnumerable<MessageDto>>(dtoList);
        }

        public async Task<Response<MessageDto>> SaveMessageAsync(Guid senderId, Guid receiverId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return BadRequest<MessageDto>("Message cannot be empty.");

            content = content.Trim();

            if (senderId == receiverId)
                return BadRequest<MessageDto>("You cannot send messages to yourself.");

            var friendship = await _friendRepo.GetFriendshipRelationAsync(senderId, receiverId);
            if (friendship == null || friendship.Status != Cocorra.DAL.Enums.FriendRequestStatus.Accepted)
                return BadRequest<MessageDto>("You can only send messages to confirmed friends.");

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
                IsRead = message.IsRead,
                CreatedAt = message.CreatedAt
            };

            try
            {
                var sender = await _userManager.FindByIdAsync(senderId.ToString());
                string senderName = sender != null ? $"{sender.FirstName} {sender.LastName}" : "New Message";
                await _pushService.SendPushNotificationAsync(receiverId, senderName, content, senderId.ToString());
            }
            catch { }

            return Success(dto);
        }

        public async Task<Response<IEnumerable<ChatFriendDto>>> GetChatFriendsListAsync(Guid currentUserId, int pageNumber = 1, int pageSize = 20)
        {
            var friends = await _friendRepo.GetAcceptedFriendsAsync(currentUserId, pageNumber, pageSize);
            var dtoList = await _messageRepo.GetFriendsChatSummariesAsync(currentUserId, friends.ToList());
            return Success<IEnumerable<ChatFriendDto>>(dtoList);
        }

        public async Task<Response<string>> MarkMessagesAsReadAsync(Guid currentUserId, Guid friendId)
        {
            await _messageRepo.MarkMessagesAsReadAsync(senderId: friendId, receiverId: currentUserId);

            await _mediator.Publish(new MessagesReadEvent(currentUserId, friendId));

            return Success("Messages marked as read.");
        }
    }
}