using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cocorra.DAL.Models;

public class Room : BaseEntity
{
    [Required(ErrorMessage = "Room title is required")]
    [MaxLength(100)]
    public string RoomTitle { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Description { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public RoomStatus status { get; set; } = RoomStatus.Scheduled;

    // --- إعدادات السعة (Capacity Settings) ---

    [Range(2, 1000, ErrorMessage = "Total capacity must be between 2 and 1000")]
    public int TotalCapacity { get; set; } = 50; // عدد الجمهور + الاستيدج

    [Range(1, 20, ErrorMessage = "Stage capacity must be between 1 and 20")]
    public int StageCapacity { get; set; } = 5; // عدد الكراسي اللي عالمنصة

    // --- إعدادات الوقت (Time Settings) ---

    // الوقت الافتراضي لأي حد يطلع الاستيدج (بالدقائق)
    [Range(1, 60)]
    public int DefaultSpeakerDurationMinutes { get; set; } = 5;

    // --- إعدادات النظام (Logic Settings) ---

    // هل النظام أوتوماتيك ولا الكوتش بيختار؟
    public RoomSelectionMode SelectionMode { get; set; } = RoomSelectionMode.Manual_CoachDecision;

    public Guid HostId { get; set; } 
    [ForeignKey(nameof(HostId))]
    public virtual ApplicationUser? Host { get; set; }
    public virtual ICollection<RoomParticipant> Participants { get; set; } = new List<RoomParticipant>();
    public bool IsPrivate { get; set; } = false;
}