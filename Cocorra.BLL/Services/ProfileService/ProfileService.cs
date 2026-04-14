using Cocorra.BLL.Services.Upload;
using Cocorra.DAL.DTOS.ProfileDto;
using Cocorra.DAL.Enums;
using Cocorra.DAL.Models;
using Cocorra.DAL.Repository.FriendRepository;
using Cocorra.BLL.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Cocorra.BLL.Services.ProfileService
{
    public class ProfileService : ResponseHandler, IProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFriendRepository _friendRepo;
        private readonly IUploadImage _uploadImage;

        public ProfileService(UserManager<ApplicationUser> userManager, IFriendRepository friendRepo, IUploadImage uploadImage)
        {
            _userManager = userManager;
            _friendRepo = friendRepo;
            _uploadImage = uploadImage;
        }

        public async Task<Response<MyProfileDto>> GetMyProfileAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return NotFound<MyProfileDto>("User not found.");

            var dto = new MyProfileDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                ProfilePicturePath = user.ProfilePicturePath ?? "",
                Bio = user.Bio,
                Age = user.Age,
                MBTI = user.MBTI
            };

            return Success(dto);
        }

        public async Task<Response<PublicProfileDto>> GetUserProfileAsync(Guid currentUserId, Guid targetUserId)
        {
            if (currentUserId == targetUserId)
                return BadRequest<PublicProfileDto>("Use the 'My Profile' endpoint for your own data.");

            var user = await _userManager.FindByIdAsync(targetUserId.ToString());
            if (user == null) return NotFound<PublicProfileDto>("User not found.");

            var friendship = await _friendRepo.GetFriendshipRelationAsync(currentUserId, targetUserId);

            bool isFriend = friendship?.Status == FriendRequestStatus.Accepted;

            var dto = new PublicProfileDto
            {
                UserId = user.Id,
                FullName = $"{user.FirstName} {user.LastName}",
                ProfilePicturePath = user.ProfilePicturePath ?? "",
                Bio = isFriend ? user.Bio : null,
                MBTI = isFriend ? user.MBTI : null,
                FriendshipStatus = friendship?.Status,
                IsFriend = isFriend
            };

            return Success(dto);
        }

        public async Task<Response<MyProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return NotFound<MyProfileDto>("User not found.");

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Bio = dto.Bio;
            user.Age = dto.Age;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest<MyProfileDto>("Failed to update profile.");

            var dtoToReturn = new MyProfileDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                ProfilePicturePath = user.ProfilePicturePath ?? "",
                Bio = user.Bio,
                Age = user.Age,
                MBTI = user.MBTI
            };

            return Success(dtoToReturn); 
        }
        public async Task<Response<string>> UploadProfilePictureAsync(Guid userId, IFormFile imageFile)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return NotFound<string>("User not found.");

            var newImagePath = await _uploadImage.SaveImageAsync(imageFile);

            if (newImagePath.StartsWith("Error"))
                return BadRequest<string>(newImagePath); 

            var oldImagePath = user.ProfilePicturePath;
            user.ProfilePicturePath = newImagePath;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                _uploadImage.DeleteImage(newImagePath);
                return BadRequest<string>("Failed to update profile picture in database.");
            }

            if (!string.IsNullOrEmpty(oldImagePath))
            {
                _uploadImage.DeleteImage(oldImagePath);
            }

            return Success(newImagePath, "Profile picture updated successfully.");
        }

        public async Task<Response<string>> UpdateAvatarPresetAsync(Guid userId, UpdateAvatarPresetDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return NotFound<string>("User not found.");

            // Update with preset key/path directly (no file upload)
            user.ProfilePicturePath = dto.AvatarPresetKey;
            
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest<string>("Failed to update avatar preset.");
            }

            return Success(user.ProfilePicturePath, "Avatar preset updated successfully.");
        }
    }
}