using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cocorra.DAL.Enums;

namespace Cocorra.DAL.Models
{
    public class SupportTicket : BaseEntity
    {
        public Guid? UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        public SupportTicketType Type { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        public string? ContactEmail { get; set; }

        public string? ScreenshotPath { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Open";
    }
}
