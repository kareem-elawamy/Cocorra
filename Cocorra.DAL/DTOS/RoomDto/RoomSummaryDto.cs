    using System;
using System.Collections.Generic;
using System.Text;

namespace Cocorra.DAL.DTOS.RoomDto
{
    public class RoomSummaryDto
    {
        public Guid Id { get; set; }
        public string RoomTitle { get; set; } = string.Empty;
        public string? Description { get; set; }

        public RoomStatus Status { get; set; } // Live أو Scheduled
        public DateTime? ScheduledStartDate { get; set; }

        public int ListenersCount { get; set; }

        // هل اليوزر الحالي عامل Reminder للروم دي ولا لأ؟ (عشان شكل الزرار)
        public bool IsReminderSetByMe { get; set; }
    }
}
