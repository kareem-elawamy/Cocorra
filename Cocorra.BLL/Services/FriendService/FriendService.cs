using Cocorra.BLL.Services.FriendService;
using Cocorra.DAL.DTOS.FriendDto;
using Cocorra.DAL.DTOS.NotificationDto;
using Cocorra.DAL.Enums;
using Cocorra.DAL.Models;
using Cocorra.DAL.Repository.FriendRepository;
using Cocorra.DAL.Repository.NotificationRepository;
using Core.Base;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cocorra.BLL.Services.FriendService
{
    public class FriendService : ResponseHandler, IFriendService
    {
        private readonly IFriendRepository _friendRepo;
        private readonly INotificationRepository _notificationRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public FriendService(IFriendRepository friendRepo, INotificationRepository notificationRepo, UserManager<ApplicationUser> userManager)
        {
            _friendRepo = friendRepo;
            _notificationRepo = notificationRepo;
            _userManager = userManager;
        }

        public async Task<Response<UserSearchDto>> SearchUserByIdAsync(Guid currentUserId, Guid targetUserId)
        {
            // 1. ندور على اليوزر
            var targetUser = await _userManager.FindByIdAsync(targetUserId.ToString());
            if (targetUser == null) return NotFound<UserSearchDto>("User not found.");

            // 2. نجيب حالة الصداقة بينهم من الريبو
            var friendRequest = await _friendRepo.GetFriendshipRelationAsync(currentUserId, targetUserId);

            string status = "None";
            if (friendRequest != null)
            {
                if (friendRequest.Status == FriendRequestStatus.Accepted) status = "Friends";
                else if (friendRequest.Status == FriendRequestStatus.Pending)
                {
                    status = friendRequest.SenderId == currentUserId ? "RequestSent" : "RequestReceived";
                }
            }

            // 3. نرجع الداتا لفلاتر
            var dto = new UserSearchDto
            {
                Id = targetUser.Id,
                FullName = $"{targetUser.FirstName} {targetUser.LastName}",
                FriendshipStatus = status
            };

            return Success(dto);
        }

        public async Task<Response<string>> SendFriendRequestAsync(Guid currentUserId, Guid targetUserId)
        {
            if (currentUserId == targetUserId)
                return BadRequest<string>("You cannot send a friend request to yourself.");

            var targetUser = await _userManager.FindByIdAsync(targetUserId.ToString());
            if (targetUser == null) return NotFound<string>("Target user not found.");

            var currentUser = await _userManager.FindByIdAsync(currentUserId.ToString());

            // 1. نتأكد إن مفيش طلب مبعوت قبل كده
            var existingRequest = await _friendRepo.GetFriendshipRelationAsync(currentUserId, targetUserId);

            if (existingRequest != null)
            {
                if (existingRequest.Status == FriendRequestStatus.Pending)
                    return BadRequest<string>("A friend request is already pending between you two.");
                if (existingRequest.Status == FriendRequestStatus.Accepted)
                    return BadRequest<string>("You are already friends.");
            }

            // 2. نعمل طلب الصداقة الجديد
            var newRequest = new FriendRequest
            {
                SenderId = currentUserId,
                ReceiverId = targetUserId,
                Status = FriendRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _friendRepo.AddAsync(newRequest);

            // 3. 🔔 نسجل الإشعار لليوزر التاني
            var notification = new Notification
            {
                UserId = targetUserId,
                Title = "New Friend Request",
                Message = $"{currentUser?.FirstName} sent you a friend request.",
                Type = NotificationType.FriendRequest,
                ReferenceId = newRequest.Id,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _notificationRepo.AddAsync(notification);

            // لو الريبو مش بيعمل Save أوتوماتيك في الـ AddAsync، استخدم دي:
            // await _friendRepo.SaveChangesAsync();

            return Success("Friend request sent successfully.");
        }

        public async Task<Response<IEnumerable<NotificationResponseDto>>> GetMyNotificationsAsync(Guid userId)
        {
            // بنجيب كل إشعارات اليوزر من الريبو
            var allNotifications = await _notificationRepo.GetTableNoTracking().ToListAsync();

            var userNotifications = allNotifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationResponseDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type.ToString(),
                    ReferenceId = n.ReferenceId,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToList();

            return Success<IEnumerable<NotificationResponseDto>>(userNotifications);
        }

        public async Task<Response<string>> RespondToFriendRequestAsync(Guid currentUserId, Guid senderId, bool accept)
        {
            // 1. نجيب الطلب من الريبو
            var request = await _friendRepo.GetPendingRequestAsync(senderId, currentUserId);

            if (request == null)
                return BadRequest<string>("Friend request not found or already processed.");

            // 2. نغير الحالة 
            request.Status = accept ? FriendRequestStatus.Accepted : FriendRequestStatus.Rejected;
            await _friendRepo.UpdateAsync(request);

            // 3. لو وافق، نبعت إشعار للشخص اللي كان باعت الطلب
            if (accept)
            {
                var currentUser = await _userManager.FindByIdAsync(currentUserId.ToString());
                var notification = new Notification
                {
                    UserId = senderId,
                    Title = "Friend Request Accepted",
                    Message = $"{currentUser?.FirstName} accepted your friend request.",
                    Type = NotificationType.FriendAccept,
                    ReferenceId = request.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                await _notificationRepo.AddAsync(notification);
            }

            return Success(accept ? "Friend request accepted." : "Friend request rejected.");
        }

        public async Task<Response<string>> MarkNotificationAsReadAsync(Guid notificationId, Guid userId)
        {
            var notification = await _notificationRepo.GetByIdAsync(notificationId);

            if (notification == null || notification.UserId != userId)
                return NotFound<string>("Notification not found.");

            notification.IsRead = true;
            await _notificationRepo.UpdateAsync(notification);

            return Success("Notification marked as read.");
        }

        public async Task<Response<string>> RemoveFriendOrCancelRequestAsync(Guid currentUserId, Guid targetUserId)
        {
            // 1. ندور على العلاقة
            var existingRequest = await _friendRepo.GetFriendshipRelationAsync(currentUserId, targetUserId);

            if (existingRequest == null)
                return BadRequest<string>("No friendship or pending request found between you two.");

            // 2. نمسح الإشعار لو الطلب كان لسه معلق
            if (existingRequest.Status == FriendRequestStatus.Pending)
            {
                var allNotifications = await _notificationRepo.GetTableNoTracking().ToListAsync();
                var relatedNotification = allNotifications.FirstOrDefault(n => n.ReferenceId == existingRequest.Id && n.Type == NotificationType.FriendRequest);

                if (relatedNotification != null)
                {
                    await _notificationRepo.DeleteAsync(relatedNotification);
                }
            }

            // 3. نمسح العلاقة
            await _friendRepo.DeleteAsync(existingRequest);

            return Success("Action completed successfully.");
        }
    }
}