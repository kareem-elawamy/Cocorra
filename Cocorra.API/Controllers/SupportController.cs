using Cocorra.BLL.Services.SupportService;
using Cocorra.DAL.AppMetaData;
using Cocorra.DAL.DTOS.ReportDto;
using Cocorra.DAL.DTOS.SupportDto;
using Cocorra.DAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cocorra.API.Controllers
{
    [ApiController]
    public class SupportController : ControllerBase
    {
        private readonly ISupportService _supportService;

        public SupportController(ISupportService supportService)
        {
            _supportService = supportService;
        }

        [AllowAnonymous]
        [HttpPost(Router.SupportRouting.SubmitTicket)]
        public async Task<IActionResult> SubmitTicket([FromForm] SubmitSupportTicketDto dto)
        {
            Guid? userId = null;
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out Guid parsedId))
            {
                userId = parsedId;
            }

            var result = await _supportService.SubmitTicketAsync(userId, dto);
            return StatusCode((int)result.StatusCode, result);
        }

        [Authorize]
        [HttpPost(Router.SupportRouting.SubmitReport)]
        public async Task<IActionResult> SubmitReport([FromForm] SubmitReportDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid reporterId)) return Unauthorized();

            var result = await _supportService.SubmitReportAsync(reporterId, dto);
            return StatusCode((int)result.StatusCode, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet(Router.SupportRouting.AdminReports)]
        public async Task<IActionResult> GetReports([FromQuery] ReportCategory? category, [FromQuery] string? status)
        {
            var result = await _supportService.GetFilteredReportsAsync(category, status);
            return StatusCode((int)result.StatusCode, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut(Router.SupportRouting.AdminUpdateReportStatus)]
        public async Task<IActionResult> UpdateReportStatus(Guid id, [FromBody] UpdateReportStatusDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _supportService.UpdateReportStatusAsync(id, dto.Status);
            return StatusCode((int)result.StatusCode, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost(Router.SupportRouting.AdminTakeReportAction)]
        public async Task<IActionResult> TakeReportAction(Guid id, [FromBody] TakeReportActionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _supportService.TakeActionOnReportAsync(id, dto);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
