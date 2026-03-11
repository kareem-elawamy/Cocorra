using Cocorra.BLL.Services.RoomService;
using Cocorra.DAL.AppMetaData;
using Cocorra.DAL.DTOS.RoomDto;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cocorra.API.Controllers;

public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpPost(Router.RoomRouting.Create)] 
    public async Task<IActionResult> Create([FromBody] CreateRoomDto dto)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid hostId))
        {
            return Unauthorized("User ID is invalid or missing.");
        }

        var result = await _roomService.CreateRoomAsync(dto, hostId);

        if (!result.Succeeded)
            return BadRequest(result);

        return Ok(result);
    }
    [HttpPost(Router.RoomRouting.Join)] 
    public async Task<IActionResult> Join(Guid roomId)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
        {
            return Unauthorized("User ID is invalid or missing.");
        }

        var result = await _roomService.JoinRoomAsync(roomId, userId);

        if (!result.Succeeded)
            return BadRequest(result);

        return Ok(result);
    }
    [HttpPost(Router.RoomRouting.Approve)] 
    public async Task<IActionResult> Approve([FromQuery] Guid roomId, [FromQuery] Guid userId)
    {
        var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(currentUserIdString, out Guid hostId))
            return Unauthorized();

        var result = await _roomService.ApproveUserAsync(roomId, userId, hostId);

        if (!result.Succeeded) return BadRequest(result);
        return Ok(result);
    }
    [HttpGet(Router.RoomRouting.State)] 
    public async Task<IActionResult> GetRoomState(Guid roomId)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
        {
            return Unauthorized("User ID is invalid or missing.");
        }

        var result = await _roomService.GetRoomStateAsync(roomId, userId);

        if (!result.Succeeded)
            return BadRequest(result);

        return Ok(result);
    }
}
