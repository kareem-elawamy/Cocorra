using Cocorra.DAL.DTOS.ProfileDto;
using Cocorra.BLL.Base;
using Microsoft.AspNetCore.Http;

namespace Cocorra.BLL.Services.ProfileService
{
    public interface IProfileService
    {
        Task<Response<MyProfileDto>> GetMyProfileAsync(Guid userId);
        Task<Response<PublicProfileDto>> GetUserProfileAsync(Guid currentUserId, Guid targetUserId);
        Task<Response<MyProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
        Task<Response<string>> UploadProfilePictureAsync(Guid userId, IFormFile imageFile);
        Task<Response<string>> UpdateAvatarPresetAsync(Guid userId, UpdateAvatarPresetDto dto);
    }
}