using Cocorra.BLL.Services.ProfileService;
using Cocorra.DAL.DTOS.ProfileDto;
using Cocorra.DAL.AppMetaData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cocorra.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId)) return Unauthorized();

            var result = await _profileService.GetMyProfileAsync(userId);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("{targetUserId:guid}")]
        public async Task<IActionResult> GetUserProfile(Guid targetUserId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid currentUserId)) return Unauthorized();

            var result = await _profileService.GetUserProfileAsync(currentUserId, targetUserId);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId)) return Unauthorized();

            var result = await _profileService.UpdateProfileAsync(userId, dto);
            return StatusCode((int)result.StatusCode, result);
        }
        [HttpPost("upload-picture")]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId)) return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var result = await _profileService.UploadProfilePictureAsync(userId, file);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut(Router.ProfileRouting.UpdateAvatarPreset)]
        public async Task<IActionResult> UpdateAvatarPreset([FromBody] UpdateAvatarPresetDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId)) return Unauthorized();

            var result = await _profileService.UpdateAvatarPresetAsync(userId, dto);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}