using System;
using System.Threading.Tasks;
using Cocorra.API.Hubs;
using Cocorra.BLL.Services.AdminService;
using Cocorra.DAL.AppMetaData;
using Cocorra.DAL.DTOS.AdminDto;
using Cocorra.DAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Cocorra.API.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin,Coach")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IHubContext<RoomHub> _roomHubContext;

        public AdminController(IAdminService adminService, IHubContext<RoomHub> roomHubContext)
        {
            _adminService = adminService;
            _roomHubContext = roomHubContext;
        }

        [HttpGet(Router.AdminRouting.GetAll)]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        )
        {
            var result = await _adminService.GetAllUsersAsync(search, page, pageSize);
            return Ok(result);
        }

        [HttpGet(Router.AdminRouting.GetById)]
        public async Task<IActionResult> GetUserById([FromRoute] Guid id)
        {
            var result = await _adminService.GetUserByIdAsync(id);
            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut(Router.AdminRouting.ChangeStatus)]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeStatus(
            [FromRoute] Guid id,
            [FromBody] ChangeStatusDto model
        )
        {
            var result = await _adminService.ChangeUserStatusAsync(id, model.NewStatus);
            if (!result.Succeeded)
                return BadRequest(result);

            // SECURITY: If the user was banned, force-abort all their active SignalR connections.
            // This is the server-side enforcement layer — the JWT is stateless and cannot be
            // revoked, so we must sever the transport to instantly boot them from any active room.
            if (model.NewStatus == UserStatus.Banned)
            {
                var connectionIds = RoomHub.GetConnectionsForUser(id);
                foreach (var connId in connectionIds)
                {
                    // Notify the client before severing so the mobile app can show a reason
                    await _roomHubContext.Clients.Client(connId)
                        .SendAsync("ForceDisconnect", new { Reason = "Your account has been banned." });
                }
                // Purge from the static connection tracker
                RoomHub.PurgeUserConnections(id);
            }

            return Ok(result);
        }

        [HttpGet(Router.AdminRouting.Stats)]
        public async Task<IActionResult> GetDashboardStats()
        {
            var result = await _adminService.GetDashboardStatsAsync();
            return Ok(result);
        }
    }
}
