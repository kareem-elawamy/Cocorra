using Cocorra.BLL.DTOS.Auth;
using Cocorra.BLL.Services.Auth;
using Cocorra.BLL.Services.OTPService;
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
        private readonly IOTPService _otpService;

        public AuthenticationController(IAuthServices authServices, IOTPService otpService)
        {
            _otpService = otpService;
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

        [HttpPost(Router.AuthenticationRouting.SubmitMbti)]
        public async Task<IActionResult> SubmitMbti([FromBody] SubmitMbtiDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId)) return Unauthorized();

            var result = await _authServices.SubmitMbtiAsync(userId, dto);
            if (!result.Succeeded) return StatusCode((int)result.StatusCode, result);

            return Ok(result);
        }

        [HttpPost(Router.AuthenticationRouting.ForgotPassword)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authServices.ForgotPasswordAsync(dto);
            return Ok(result);
        }
        [Authorize]
        [HttpPut(Router.AuthenticationRouting.UpdateFcmToken)]
        public async Task<IActionResult> UpdateFcmToken([FromBody] string fcmToken)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId)) return Unauthorized();

            var result = await _authServices.UpdateFcmTokenAsync(userId, fcmToken);
            return StatusCode((int)result.StatusCode, result);
        }
        [HttpPost(Router.AuthenticationRouting.ResendOtp)]
        public async Task<IActionResult> ResendOtp([FromBody] string email)
        {
            var result = await _otpService.ResendOtpAsync(email);
            return StatusCode((int)result.StatusCode, result);
        }
        [HttpGet(Router.AuthenticationRouting.ConfirmEmail)]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string email, [FromQuery] string otpCode)
        {
            var result = await _otpService.VerifyOtpAsync(email, otpCode);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}