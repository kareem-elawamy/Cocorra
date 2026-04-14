using Cocorra.BLL.Services.SupportService;
using Cocorra.DAL.AppMetaData;
using Cocorra.DAL.DTOS.ReportDto;
using Cocorra.DAL.DTOS.SupportDto;
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
        public async Task<IActionResult> SubmitTicket([FromBody] SubmitSupportTicketDto dto)
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
        public async Task<IActionResult> SubmitReport([FromBody] SubmitReportDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid reporterId)) return Unauthorized();

            var result = await _supportService.SubmitReportAsync(reporterId, dto);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
