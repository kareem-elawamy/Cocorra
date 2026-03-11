using System.ComponentModel.DataAnnotations;

namespace Cocorra.DAL.DTOS.RoomDto;

public class CreateRoomDto
{
    [Required]
    [MaxLength(100)]
    public string RoomTitle { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime? ScheduledStartDate { get; set; }

    public bool IsPrivate { get; set; } = false;
    public int TotalCapacity { get; set; } = 50;
    public int StageCapacity { get; set; } = 5;
    public int DefaultSpeakerDurationMinutes { get; set; } = 5;
    public RoomSelectionMode SelectionMode { get; set; } = RoomSelectionMode.Manual_CoachDecision;
}