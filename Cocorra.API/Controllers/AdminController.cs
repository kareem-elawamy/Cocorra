using Cocorra.BLL.Services.AdminService;
using Cocorra.DAL.AppMetaData;
using Cocorra.DAL.DTOS.AdminDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Cocorra.API.Controllers
{
    [ApiController]
    //[Authorize(Roles = "Admin")] 
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet(Router.AdminRouting.GetAll)]
        public async Task<IActionResult> GetAllUsers([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _adminService.GetAllUsersAsync(search, page, pageSize);
            return Ok(result);
        }

        [HttpGet(Router.AdminRouting.GetById)]
        public async Task<IActionResult> GetUserById([FromRoute] Guid id)
        {
            var result = await _adminService.GetUserByIdAsync(id);
            if (!result.Succeeded) return BadRequest(result);

            return Ok(result);
        }

        [HttpPut(Router.AdminRouting.ChangeStatus)]
        public async Task<IActionResult> ChangeStatus([FromRoute] Guid id, [FromBody] ChangeStatusDto model)
        {
            var result = await _adminService.ChangeUserStatusAsync(id, model.NewStatus);
            if (!result.Succeeded) return BadRequest(result);

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