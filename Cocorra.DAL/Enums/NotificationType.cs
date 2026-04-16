using System;
using System.Collections.Generic;
using System.Text;

namespace Cocorra.DAL.Enums;

public enum NotificationType
{
    System = 0,        // رسائل من الإدارة
    RoomReminder = 1,  // تذكير بروم هتبدأ
    FriendRequest = 2, // طلب صداقة جديد
    FriendAccept = 3,  // حد وافق على طلب الصداقة
    AdminWarning = 4   // تحذير من الإدارة
}
