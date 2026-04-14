using System;
using System.ComponentModel.DataAnnotations;
using Cocorra.DAL.Enums;

namespace Cocorra.DAL.DTOS.ReportDto
{
    public class SubmitReportDto
    {
        public ReportCategory Category { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public Guid? ReportedUserId { get; set; }
        public Guid? ReportedRoomId { get; set; }
    }
}
