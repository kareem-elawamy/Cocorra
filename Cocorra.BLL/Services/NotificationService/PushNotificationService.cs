using System;
using System.Threading.Tasks;

using FirebaseAdmin.Messaging;
using System.Collections.Generic;

namespace Cocorra.BLL.Services.NotificationService
{
    public class PushNotificationService : IPushNotificationService
    {
        public async Task SendPushNotificationAsync(string fcmToken, string title, string body, Dictionary<string, string> data)
        {
            if (string.IsNullOrWhiteSpace(fcmToken)) return;

            var message = new Message()
            {
                Token = fcmToken,
                Data = data
            };

            // CRITICAL: Only attach Notification when title/body are non-empty.
            // Firebase treats ANY Notification object (even with empty strings) as a
            // "display" notification, which can cause blank pop-ups on Android 13+
            // and prevents silent background handling on iOS.
            if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(body))
            {
                message.Notification = new Notification()
                {
                    Title = title,
                    Body = body
                };
            }

            try
            {
                await FirebaseMessaging.DefaultInstance.SendAsync(message);
            }
            catch (FirebaseMessagingException)
            {
                // Optionally log inactive token or mapping issues. 
                // Swallow so caller doesn't crash on invalid/expired tokens.
            }
        }
    }
}