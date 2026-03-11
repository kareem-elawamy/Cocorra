using System;
using System.Collections.Generic;
using System.Text;

namespace Cocorra.DAL.DTOS.RoomDto;

public class RoomStateDto
{
    public Guid RoomId { get; set; }
    public string RoomTitle { get; set; }=string.Empty;
    public Guid HostId { get; set; }
    public int TotalCapacity { get; set; }
    public int StageCapacity { get; set; }
    public List<ParticipantStateDto> Participants { get; set; } = new List<ParticipantStateDto>();
}
