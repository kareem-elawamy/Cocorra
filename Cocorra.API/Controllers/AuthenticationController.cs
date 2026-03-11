using Cocorra.BLL.DTOS.Auth;
using Cocorra.BLL.Services.Auth;
using Cocorra.DAL.AppMetaData;
using Cocorra.DAL.DTOS.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cocorra.API.Controllers
{
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthServices _authServices;

        public AuthenticationController(IAuthServices authServices)
        {
            _authServices = authServices;
        }

        [HttpPost(Router.AuthenticationRouting.Register)]
        public async Task<IActionResult> Register([FromForm] RegisterDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authServices.RegisterAsync(dto);
            if (!result.Succeeded) return StatusCode((int)result.StatusCode, result);

            return Ok(result);
        }

        [HttpPost(Router.AuthenticationRouting.Login)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto) 
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authServices.LoginAsync(dto);
            if (!result.Succeeded) return StatusCode((int)result.StatusCode, result);

            return Ok(result);
        }

        [Authorize]
        [HttpPost("submit-mbti")] 
        public async Task<IActionResult> SubmitMbti([FromBody] SubmitMbtiDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId)) return Unauthorized();

            var result = await _authServices.SubmitMbtiAsync(userId, dto);
            if (!result.Succeeded) return StatusCode((int)result.StatusCode, result);

            return Ok(result);
        }

        [HttpPost("forgot-password")] 
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authServices.ForgotPasswordAsync(dto);
            return Ok(result);
        }
    }
}