using System;
using System.Collections.Generic;
using Cocorra.DAL.Enums;

namespace Cocorra.DAL.DTOS.SupportChatDto
{
    public class SupportChatDetailsDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? AdminId { get; set; }
        public SupportChatStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public List<SupportMessageDto> Messages { get; set; } = new();
    }
}
