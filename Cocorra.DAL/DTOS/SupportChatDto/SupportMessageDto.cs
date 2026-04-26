using System;

namespace Cocorra.DAL.DTOS.SupportChatDto
{
    public class SupportMessageDto
    {
        public Guid Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsFromAdmin { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
