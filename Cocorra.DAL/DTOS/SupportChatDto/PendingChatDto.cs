using System;
using Cocorra.DAL.Enums;

namespace Cocorra.DAL.DTOS.SupportChatDto
{
    public class PendingChatDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public SupportChatStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UnreadMessageCount { get; set; }
        public string LastMessageContent { get; set; } = string.Empty;
    }
}
