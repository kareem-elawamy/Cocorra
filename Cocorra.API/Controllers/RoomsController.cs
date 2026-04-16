using Cocorra.BLL.Services.RoomService;
using Cocorra.DAL.AppMetaData;
using Cocorra.DAL.DTOS.RoomDto;
using Cocorra.DAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cocorra.API.Controllers
{
    [ApiController]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomsController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        [HttpPost(Router.RoomRouting.Create)]
        public async Task<IActionResult> Create([FromForm] CreateRoomDto dto, IFormFile? roomImage)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid hostId))
            {
                return Unauthorized("User ID is invalid or missing.");
            }

            var result = await _roomService.CreateRoomAsync(dto, hostId, roomImage);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost(Router.RoomRouting.Join)]
        public async Task<IActionResult> Join([FromRoute] Guid roomId)
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
        public async Task<IActionResult> Approve([FromRoute] Guid roomId, [FromRoute] Guid userId)
        {
            var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(currentUserIdString, out Guid hostId))
                return Unauthorized();

            var result = await _roomService.ApproveUserAsync(roomId, userId, hostId);

            if (!result.Succeeded) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet(Router.RoomRouting.State)]
        public async Task<IActionResult> GetRoomState([FromRoute] Guid roomId)
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

        [HttpGet(Router.RoomRouting.Feed)]
        public async Task<IActionResult> GetRoomsFeed([FromQuery] RoomCategory? categoryId = null, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId)) return Unauthorized();

            if (pageSize > 50) pageSize = 50;
            if (pageNumber < 1) pageNumber = 1;

            var result = await _roomService.GetRoomsFeedAsync(userId, categoryId, pageNumber, pageSize);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost(Router.RoomRouting.ToggleReminder)]
        public async Task<IActionResult> ToggleReminder([FromRoute] Guid roomId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId)) return Unauthorized();

            var result = await _roomService.ToggleReminderAsync(roomId, userId);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost(Router.RoomRouting.Start)]
        public async Task<IActionResult> StartScheduledRoom([FromRoute] Guid roomId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid hostId)) return Unauthorized();

            var result = await _roomService.StartScheduledRoomAsync(roomId, hostId);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost(Router.RoomRouting.End)]
        public async Task<IActionResult> EndRoom([FromRoute] Guid roomId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid hostId)) return Unauthorized();

            var result = await _roomService.EndRoomAsync(roomId, hostId);
            return StatusCode((int)result.StatusCode, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet(Router.RoomRouting.AdminHistory)]
        public async Task<IActionResult> GetRoomHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            if (pageSize > 50) pageSize = 50;
            if (pageNumber < 1) pageNumber = 1;

            var result = await _roomService.GetEndedRoomsHistoryAsync(pageNumber, pageSize);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}