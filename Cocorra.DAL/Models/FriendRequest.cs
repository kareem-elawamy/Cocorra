using System;
using System.Collections.Generic;
using System.Text;

namespace Cocorra.DAL.Models
{
    public class FriendRequest :BaseEntity
    {

        public Guid SenderId { get; set; }
        public ApplicationUser? Sender { get; set; }

        public Guid ReceiverId { get; set; }
        public ApplicationUser? Receiver { get; set; }

        public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;

    }
}
