using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Cocorra.DAL.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        public int Age { get; set; }
        public string? VoiceVerificationPath { get; set; }
        public string? MBTI { get; set; }

        // هل الحساب متفعل ولا لسه
        public UserStatus Status { get; set; } = UserStatus.Pending;

        // علاقة بالرومات اللي دخلها
        public virtual ICollection<RoomParticipant> RoomParticipations { get; set; } = new List<RoomParticipant>();

        // علاقة بالرومات اللي هو الـ Host بتاعها
        public virtual ICollection<Room> OwnedRooms { get; set; }= new List<Room>();
        public DateTime CreateAt { get; set; }
    }
}