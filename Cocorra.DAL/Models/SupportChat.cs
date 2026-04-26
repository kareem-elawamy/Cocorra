using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cocorra.DAL.Enums;

namespace Cocorra.DAL.Models
{
    public class SupportChat
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? AdminId { get; set; }
        public SupportChatStatus Status { get; set; } = SupportChatStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;

        public virtual ICollection<SupportMessage> Messages { get; set; } = new List<SupportMessage>();
    }
}
