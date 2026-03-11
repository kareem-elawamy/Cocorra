using System;
using System.Collections.Generic;
using System.Text;

namespace Cocorra.DAL.Models;



    public class RoomReminder
    {
        public Guid UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public Guid RoomId { get; set; }
        public Room? Room { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

