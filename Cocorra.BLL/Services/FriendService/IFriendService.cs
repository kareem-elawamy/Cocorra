using Cocorra.DAL.DTOS.FriendDto;
using Cocorra.DAL.DTOS.NotificationDto;
using Core.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cocorra.BLL.Services.FriendService;

public interface IFriendService
{
    // دالة السيرش على يوزر بالـ ID
    Task<Response<UserSearchDto>> SearchUserByIdAsync(Guid currentUserId, Guid targetUserId);

    // دالة إرسال طلب الصداقة (وبتتضمن إرسال الإشعار)
    Task<Response<string>> SendFriendRequestAsync(Guid currentUserId, Guid targetUserId);
    Task<Response<IEnumerable<NotificationResponseDto>>> GetMyNotificationsAsync(Guid userId);

    // الرد على طلب الصداقة
    Task<Response<string>> RespondToFriendRequestAsync(Guid currentUserId, Guid senderId, bool accept);

    // (اختياري) تحديد الإشعار كمقروء
    Task<Response<string>> MarkNotificationAsReadAsync(Guid notificationId, Guid userId);
    Task<Response<string>> RemoveFriendOrCancelRequestAsync(Guid currentUserId, Guid targetUserId);
}