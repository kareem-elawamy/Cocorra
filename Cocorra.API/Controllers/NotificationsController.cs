using Cocorra.BLL.Services.NotificationService;
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
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("my-notifications")]
        public async Task<IActionResult> GetMyNotifications([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(currentUserIdString, out Guid currentUserId)) return Unauthorized();

            if (pageSize > 100) pageSize = 100;
            if (pageNumber < 1) pageNumber = 1;

            var result = await _notificationService.GetMyNotificationsAsync(currentUserId, pageNumber, pageSize);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("read-notification/{notificationId:guid}")]
        public async Task<IActionResult> MarkNotificationRead(Guid notificationId)
        {
            var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(currentUserIdString, out Guid currentUserId)) return Unauthorized();

            var result = await _notificationService.MarkNotificationAsReadAsync(notificationId, currentUserId);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllRead()
        {
            var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(currentUserIdString, out Guid currentUserId)) return Unauthorized();

            var result = await _notificationService.MarkAllAsReadAsync(currentUserId);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}