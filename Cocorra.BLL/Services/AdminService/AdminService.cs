using Cocorra.DAL.DTOS.AdminDto;
using Cocorra.DAL.Enums;
using Cocorra.DAL.Models;
using Core.Base;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cocorra.BLL.Services.AdminService
{
    public class AdminService : ResponseHandler, IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public AdminService(UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _env = env;
        }

        public async Task<Response<string>> ChangeUserStatusAsync(Guid userId, UserStatus newStatus)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return BadRequest<string>("User not found");

            if (user.Status == newStatus)
                return BadRequest<string>($"User is already {newStatus}");

            var oldStatus = user.Status;
            user.Status = newStatus;
            switch (newStatus)
            {
                case UserStatus.Active:
                    await _userManager.SetLockoutEndDateAsync(user, null);
                    if (!string.IsNullOrEmpty(user.VoiceVerificationPath))
                    {
                        DeleteVoiceFile(user.VoiceVerificationPath);
                        user.VoiceVerificationPath = null;
                    }
                    break;

                case UserStatus.Banned:
                    await _userManager.SetLockoutEnabledAsync(user, true);
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                    if (!string.IsNullOrEmpty(user.VoiceVerificationPath))
                    {
                        DeleteVoiceFile(user.VoiceVerificationPath);
                        user.VoiceVerificationPath = null;
                    }
                    break;

                case UserStatus.Pending:
                case UserStatus.Rejected:
                    break;
            }

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
                return Success($"User status changed from {oldStatus} to {newStatus}");

            return BadRequest<string>("Failed to change status");
        }

        public async Task<Response<IEnumerable<UserDto>>> GetAllUsersAsync(string? search, int page = 1, int pageSize = 10)
        {
            var query = _userManager.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Email!.Contains(search) ||
                                         u.FirstName!.Contains(search) ||
                                         u.LastName!.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.Id) 
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDto
                {
                    Id = u.Id.ToString(),
                    FullName = $"{u.FirstName} {u.LastName}",
                    Email = u.Email ?? "",
                    Age = u.Age,
                    MBTI = u.MBTI ?? "N/A",
                    Status = u.Status.ToString(),
                    VoicePath = u.VoiceVerificationPath
                })
                .ToListAsync();

            var response = Success<IEnumerable<UserDto>>(users);
            response.Meta = new { TotalCount = totalCount, CurrentPage = page, PageSize = pageSize };

            return response;
        }
        public async Task<Response<UserDto>> GetUserByIdAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return BadRequest<UserDto>("User not found");

            var userDto = new UserDto
            {
                Id = user.Id.ToString(),
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email!,
                Age = user.Age,
                MBTI = user.MBTI ?? "N/A",
                Status = user.Status.ToString(),
                VoicePath = user.VoiceVerificationPath
            };

            return Success(userDto);
        }

        public async Task<Response<UserDto>> UpdateUserAsync(Guid userId, UpdateUserDto model)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return BadRequest<UserDto>("User not found");

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Age = model.Age;
            user.MBTI = model.MBTI;

            if (user.Email != model.Email)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    return BadRequest<UserDto>("Email is already taken by another user.");
                }

                user.Email = model.Email;
                user.UserName = model.Email;
                user.NormalizedEmail = model.Email.ToUpper();
                user.NormalizedUserName = model.Email.ToUpper();
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest<UserDto>($"Failed to update user: {errors}");
            }

            var updatedDto = new UserDto
            {
                Id = user.Id.ToString(),
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email!,
                Age = user.Age,
                MBTI = user.MBTI ?? "N/A",
                Status = user.Status.ToString(),
                VoicePath = user.VoiceVerificationPath
            };

            return Success(updatedDto, "User updated successfully");
        }

        public async Task<Response<string>> DeleteUserAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return BadRequest<string>("User not found");

            var voicePath = user.VoiceVerificationPath;

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                DeleteVoiceFile(voicePath);
                return Success("User deleted permanently.");
            }

            return BadRequest<string>("Failed to delete user.");
        }

        public async Task<Response<string>> ResetUserPasswordAsync(Guid userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return BadRequest<string>("User not found");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
                return Success("Password has been reset successfully.");

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest<string>($"Failed to reset password: {errors}");
        }

        public async Task<Response<DashboardStatsDto>> GetDashboardStatsAsync()
        {
            var stats = new DashboardStatsDto
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                ActiveUsers = await _userManager.Users.CountAsync(u => u.Status == UserStatus.Active),
                PendingUsers = await _userManager.Users.CountAsync(u => u.Status == UserStatus.Pending),
                BannedUsers = await _userManager.Users.CountAsync(u => u.Status == UserStatus.Banned),
                RejectedUsers = await _userManager.Users.CountAsync(u => u.Status == UserStatus.Rejected)
            };

            return Success(stats);
        }

        private void DeleteVoiceFile(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;

            try
            {
                var absolutePath = Path.Combine(_env.WebRootPath, relativePath);

                if (File.Exists(absolutePath))
                {
                    File.Delete(absolutePath);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}