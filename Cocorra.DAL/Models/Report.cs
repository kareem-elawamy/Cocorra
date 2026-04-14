using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cocorra.DAL.Enums;

namespace Cocorra.DAL.Models
{
    public class Report : BaseEntity
    {
        public Guid ReporterId { get; set; }
        [ForeignKey(nameof(ReporterId))]
        public virtual ApplicationUser? Reporter { get; set; }

        public Guid? ReportedUserId { get; set; }
        [ForeignKey(nameof(ReportedUserId))]
        public virtual ApplicationUser? ReportedUser { get; set; }

        public Guid? ReportedRoomId { get; set; }
        [ForeignKey(nameof(ReportedRoomId))]
        public virtual Room? ReportedRoom { get; set; }

        public ReportCategory Category { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public string? ScreenshotPath { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Open";
    }
}
