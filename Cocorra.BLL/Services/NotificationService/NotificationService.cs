using Cocorra.DAL.DTOS.NotificationDto;
using Cocorra.DAL.Repository.NotificationRepository;
using Cocorra.BLL.Base;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cocorra.BLL.Services.NotificationService
{
    public class NotificationService : ResponseHandler, INotificationService
    {
        private readonly INotificationRepository _notificationRepo;

        public NotificationService(INotificationRepository notificationRepo)
        {
            _notificationRepo = notificationRepo;
        }

        public async Task<Response<IEnumerable<NotificationResponseDto>>> GetMyNotificationsAsync(Guid userId, int pageNumber = 1, int pageSize = 20)
        {
            var userNotifications = await _notificationRepo.GetTableNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
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
                .ToListAsync();

            return Success<IEnumerable<NotificationResponseDto>>(userNotifications);
        }

        public async Task<Response<string>> MarkNotificationAsReadAsync(Guid notificationId, Guid userId)
        {
            var updatedRows = await _notificationRepo.GetTableAsTracking()
                .Where(n => n.Id == notificationId && n.UserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

            if (updatedRows == 0)
                return NotFound<string>("Notification not found.");

            return Success("Notification marked as read.");
        }

        public async Task<Response<string>> MarkAllAsReadAsync(Guid userId)
        {
            await _notificationRepo.GetTableAsTracking()
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

            return Success("All notifications marked as read.");
        }
    }
}