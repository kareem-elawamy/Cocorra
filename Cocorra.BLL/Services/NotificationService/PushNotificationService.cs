using System;
using System.Threading.Tasks;

namespace Cocorra.BLL.Services.NotificationService
{
    public class PushNotificationService : IPushNotificationService
    {
        public Task SendPushNotificationAsync(Guid receiverId, string title, string body, string? chatFriendId = null)
        {
            // Push notifications are currently scoped out for future phases.
            // Keeping stub active to satisfy IPushNotificationService dependencies in BLL without external SDK calls.
            return Task.CompletedTask;
        }
    }
}