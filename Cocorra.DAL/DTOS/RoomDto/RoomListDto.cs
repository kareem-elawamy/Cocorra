using System;
using System.Collections.Generic;
using System.Text;

namespace Cocorra.DAL.DTOS.RoomDto
{
    public class RoomListDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ListenersCount { get; set; }
        public RoomStatus Status { get; set; }
        public DateTime? ScheduledStartDate { get; set; }
        public bool IsModerated { get; set; } = true; // دايماً true في تطبيقكم
        public string? ImageUrl { get; set; } // الصور اللي في الديزاين
    }
}
