using Cocorra.BLL.Services.FriendService;
using Cocorra.DAL.DTOS.FriendDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cocorra.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FriendsController : ControllerBase
    {
        private readonly IFriendService _friendService;

        public FriendsController(IFriendService friendService)
        {
            _friendService = friendService;
        }

        [HttpGet("search/{targetId:guid}")]
        public async Task<IActionResult> SearchUser(Guid targetId)
        {
            var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(currentUserIdString, out Guid currentUserId)) return Unauthorized();

            var result = await _friendService.SearchUserByIdAsync(currentUserId, targetId);
            if (!result.Succeeded) return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("send-request")]
        public async Task<IActionResult> SendRequest([FromBody] SendRequestDto dto)
        {
            var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(currentUserIdString, out Guid currentUserId)) return Unauthorized();

            var result = await _friendService.SendFriendRequestAsync(currentUserId, dto.TargetUserId);
            if (!result.Succeeded) return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("my-notifications")]
        public async Task<IActionResult> GetMyNotifications()
        {
            var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(currentUserIdString, out Guid currentUserId)) return Unauthorized();

            var result = await _friendService.GetMyNotificationsAsync(currentUserId);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost("respond-request/{senderId:guid}")]
        public async Task<IActionResult> RespondRequest(Guid senderId, [FromQuery] bool accept)
        {
            var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(currentUserIdString, out Guid currentUserId)) return Unauthorized();

            var result = await _friendService.RespondToFriendRequestAsync(currentUserId, senderId, accept);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpDelete("remove/{targetId:guid}")]
        public async Task<IActionResult> RemoveFriendOrCancelRequest(Guid targetId)
        {
            var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(currentUserIdString, out Guid currentUserId)) return Unauthorized();

            var result = await _friendService.RemoveFriendOrCancelRequestAsync(currentUserId, targetId);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("read-notification/{notificationId:guid}")]
        public async Task<IActionResult> MarkNotificationRead(Guid notificationId)
        {
            var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(currentUserIdString, out Guid currentUserId)) return Unauthorized();

            var result = await _friendService.MarkNotificationAsReadAsync(notificationId, currentUserId);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}