using System;
using System.Collections.Generic;
using System.Text;

namespace Cocorra.DAL.DTOS.NotificationDto
{
    public class NotificationResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // System, RoomReminder, FriendRequest, etc.
        public Guid? ReferenceId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
