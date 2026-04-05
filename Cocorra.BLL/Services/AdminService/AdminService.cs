using Cocorra.BLL.Services.Upload;
using Cocorra.DAL.DTOS.AdminDto;
using Cocorra.DAL.Enums;
using Cocorra.DAL.Models;
using Cocorra.BLL.Base;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cocorra.BLL.Services.AdminService
{
    public class AdminService : ResponseHandler, IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUploadVoice _uploadVoice;
        private readonly string _baseUrl;

        public AdminService(UserManager<ApplicationUser> userManager, IUploadVoice uploadVoice, IConfiguration configuration)
        {
            _userManager = userManager;
            _uploadVoice = uploadVoice;
            _baseUrl = configuration["AppSettings:BaseUrl"]?.TrimEnd('/') ?? "";
        }

        private string? BuildFullUrl(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return null;
            return $"{_baseUrl}/{relativePath.Replace("\\", "/").TrimStart('/')}";
        }

        public async Task<Response<string>> ChangeUserStatusAsync(Guid userId, UserStatus newStatus)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return BadRequest<string>("User not found");

            if (!Enum.IsDefined(typeof(UserStatus), newStatus))
                return BadRequest<string>("Invalid status value.");

            if (user.Status == newStatus)
                return BadRequest<string>($"User is already {newStatus}");

            var oldStatus = user.Status;
            user.Status = newStatus;
            switch (newStatus)
            {
                case UserStatus.Active:
                    await _userManager.SetLockoutEndDateAsync(user, null);
                    _uploadVoice.DeleteVoice(user.VoiceVerificationPath);
                    user.VoiceVerificationPath = null;
                    break;

                case UserStatus.Banned:
                    await _userManager.SetLockoutEnabledAsync(user, true);
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                    _uploadVoice.DeleteVoice(user.VoiceVerificationPath);
                    user.VoiceVerificationPath = null;
                    break;

                case UserStatus.Rejected:
                    _uploadVoice.DeleteVoice(user.VoiceVerificationPath);
                    user.VoiceVerificationPath = null;
                    break;

                case UserStatus.ReRecord:
                    _uploadVoice.DeleteVoice(user.VoiceVerificationPath);
                    user.VoiceVerificationPath = null;
                    break;

                case UserStatus.Pending:
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

            var rawUsers = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.Age,
                    u.MBTI,
                    u.Status,
                    u.VoiceVerificationPath,
                    u.CreatedAt
                })
                .ToListAsync();

            var users = rawUsers.Select(u => new UserDto
            {
                Id = u.Id.ToString(),
                FullName = $"{u.FirstName} {u.LastName}",
                Email = u.Email ?? "",
                Age = u.Age,
                MBTI = u.MBTI ?? "N/A",
                Status = u.Status.ToString(),
                CreatedAt = u.CreatedAt,
                VoicePath = BuildFullUrl(u.VoiceVerificationPath)
            }).ToList();

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
                CreatedAt = user.CreatedAt,
                VoicePath = BuildFullUrl(user.VoiceVerificationPath)
            };

            return Success(userDto);
        }

        public async Task<Response<DashboardStatsDto>> GetDashboardStatsAsync()
        {
            var statusCounts = await _userManager.Users
                .GroupBy(u => u.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var stats = new DashboardStatsDto
            {
                TotalUsers = statusCounts.Sum(s => s.Count),
                ActiveUsers = statusCounts.FirstOrDefault(s => s.Status == UserStatus.Active)?.Count ?? 0,
                PendingUsers = statusCounts.FirstOrDefault(s => s.Status == UserStatus.Pending)?.Count ?? 0,
                BannedUsers = statusCounts.FirstOrDefault(s => s.Status == UserStatus.Banned)?.Count ?? 0,
                RejectedUsers = statusCounts.FirstOrDefault(s => s.Status == UserStatus.Rejected)?.Count ?? 0,
                ReRecordUsers = statusCounts.FirstOrDefault(s => s.Status == UserStatus.ReRecord)?.Count ?? 0
            };

            return Success(stats);
        }
    }
}