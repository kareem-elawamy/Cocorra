using System;
using System.Collections.Generic;
using System.Text;

namespace Cocorra.DAL.DTOS.RoomDto;

public class ParticipantStateDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
    public bool IsOnStage { get; set; }
    public bool IsMuted { get; set; }
    public bool IsHandRaised { get; set; }
    public DateTime JoinedAt { get; set; }
}