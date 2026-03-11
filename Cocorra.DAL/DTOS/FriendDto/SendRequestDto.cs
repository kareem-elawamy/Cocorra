using System;
using System.Collections.Generic;
using System.Text;

namespace Cocorra.DAL.DTOS.FriendDto;

public class SendRequestDto
{
    public Guid TargetUserId { get; set; }
}