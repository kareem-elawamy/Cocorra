using System;

namespace Cocorra.DAL.Models
{
    public class SupportMessage
    {
        public Guid Id { get; set; }
        public Guid SupportChatId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsFromAdmin { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual SupportChat SupportChat { get; set; } = null!;
    }
}
