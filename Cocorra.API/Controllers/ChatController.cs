using Cocorra.BLL.Services.ChatService;
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
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpGet("friends-list")]
        public async Task<IActionResult> GetFriendsList()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid currentUserId)) return Unauthorized();

            var result = await _chatService.GetChatFriendsListAsync(currentUserId);
            return StatusCode((int)result.StatusCode, result);
        }
        [HttpGet("history/{friendId:guid}")]
        public async Task<IActionResult> GetChatHistory([FromRoute] Guid friendId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid currentUserId)) return Unauthorized();

            if (pageSize > 100) pageSize = 100;
            if (pageNumber < 1) pageNumber = 1;

            var result = await _chatService.GetChatHistoryAsync(currentUserId, friendId, pageNumber, pageSize);
            return StatusCode((int)result.StatusCode, result);
        }
        [HttpPut("mark-read/{friendId:guid}")]
        public async Task<IActionResult> MarkMessagesAsRead(Guid friendId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid currentUserId)) return Unauthorized();

            var result = await _chatService.MarkMessagesAsReadAsync(currentUserId, friendId);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}