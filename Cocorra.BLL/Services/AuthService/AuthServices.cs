using Cocorra.BLL.DTOS.Auth;
using Cocorra.BLL.Services.Auth;
using Cocorra.BLL.Services.Upload;
using Cocorra.DAL.Data;
using Cocorra.DAL.DTOS.Auth;
using Cocorra.DAL.Enums;
using Cocorra.DAL.Models;
using Core.Base;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Cocorra.BLL.Services.AuthServices
{
    public class AuthServices : ResponseHandler, IAuthServices
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IUploadVoice _uploadVoice;
        private readonly AppDbContext _context;

        public AuthServices(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IConfiguration configuration,
            IUploadVoice uploadVoice,
            AppDbContext context)
        {
            _context = context;
            _uploadVoice = uploadVoice;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        // 1. التسجيل: بيستقبل الداتا + الصوت، ويكريت اليوزر كـ Pending، ومبيرجعش توكن
        public async Task<Response<string>> RegisterAsync(RegisterDto dto)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                string? voicePathToDelete = null;

                try
                {
                    var existingUser = await _userManager.FindByEmailAsync(dto.Email!);
                    if (existingUser is not null)
                        return BadRequest<string>("Email is already registered");

                    var voicePath = await _uploadVoice.SaveVoice(dto.VoiceVerification!);
                    if (voicePath.StartsWith("Error"))
                        return BadRequest<string>(voicePath);

                    voicePathToDelete = voicePath;

                    var user = new ApplicationUser
                    {
                        UserName = dto.Email,
                        Email = dto.Email,
                        FirstName = dto.FirstName!,
                        LastName = dto.LastName!,
                        Age = dto.Age,
                        Status = UserStatus.Pending, // إجباري Pending
                        VoiceVerificationPath = voicePath
                    };

                    var result = await _userManager.CreateAsync(user, dto.Password!);
                    if (!result.Succeeded)
                    {
                        DeleteFile(voicePathToDelete);
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        return BadRequest<string>($"Registration failed: {errors}");
                    }

                    if (!await _roleManager.RoleExistsAsync("User"))
                        await _roleManager.CreateAsync(new IdentityRole<Guid>("User"));

                    var roleResult = await _userManager.AddToRoleAsync(user, "User");
                    if (!roleResult.Succeeded)
                        throw new Exception("Failed to assign role");

                    await transaction.CommitAsync();

                    // التعديل المهم: بنرجع رسالة بدل التوكن عشان الديزاين (We usually respond within 24 hours)
                    return Created("Registration successful. Your account is pending verification. We usually respond within 24 hours.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    DeleteFile(voicePathToDelete);
                    return BadRequest<string>("Something went wrong: " + ex.Message);
                }
            });
        }

        // 2. تسجيل الدخول: زي ما هو ممتاز ومبيسمحش للـ Pending يدخل
        public async Task<Response<AuthModel>> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email!);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password!))
            {
                return BadRequest<AuthModel>("Invalid Email or Password");
            }

            switch (user.Status)
            {
                case UserStatus.Pending:
                    return BadRequest<AuthModel>("Your account is still pending approval. We usually respond within 24 hours.");
                case UserStatus.Rejected:
                    return BadRequest<AuthModel>("Your account has been rejected.");
                case UserStatus.Banned:
                    return BadRequest<AuthModel>("Your account has been banned.");
                case UserStatus.Active:
                    break;
                default:
                    return BadRequest<AuthModel>("Invalid user status.");
            }

            var jwtToken = await GenerateJwtToken(user);
            var roles = await _userManager.GetRolesAsync(user);
            var authModel = new AuthModel
            {
                Email = user.Email,
                Username = user.UserName,
                Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                ExpiresOn = jwtToken.ValidTo,
                IsAuthenticated = true,
                Roles = roles.ToList()
            };
            return Success(authModel);
        }

        public async Task<Response<string>> SubmitMbtiAsync(Guid userId, SubmitMbtiDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return BadRequest<string>("User not found.");

            user.MBTI = dto.MBTI;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
                return Success("MBTI updated successfully.");

            return BadRequest<string>("Failed to update MBTI.");
        }

        // 4. الدالة الجديدة: نسيت كلمة السر (عشان الديزاين)
        public async Task<Response<string>> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email!);
            if (user == null)
                return Success("If your email is registered, you will receive a reset link."); // بنرجع Success دايماً كـ Security Best Practice

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // هنا في المستقبل هتحط كود يبعت إيميل لليوزر بالـ token ده 
            // (emailService.SendResetEmail(user.Email, token))

            return Success("If your email is registered, you will receive a password reset link shortly.");
        }

        // --- Helper Methods ---
        private void DeleteFile(string? path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", path);
                    if (File.Exists(fullPath)) File.Delete(fullPath);
                }
                catch { }
            }
        }

        private async Task<JwtSecurityToken> GenerateJwtToken(ApplicationUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            foreach (var role in userRoles) claims.Add(new Claim(ClaimTypes.Role, role));

            var key = Encoding.ASCII.GetBytes(_configuration["JWTSetting:securityKey"]!);
            var authKey = new SymmetricSecurityKey(key);

            var token = new JwtSecurityToken(
                audience: _configuration["JWTSetting:ValidAudience"],
                issuer: _configuration["JWTSetting:ValidIssuer"],
                expires: DateTime.UtcNow.AddDays(15),
                claims: claims,
                signingCredentials: new SigningCredentials(authKey, SecurityAlgorithms.HmacSha256)
            );

            return token;
        }
    }
}