using Cocorra.BLL.DTOS.Auth;
using Cocorra.BLL.Services.Auth;
using Cocorra.BLL.Services.Email;
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
        private readonly IEmailService _emailService;
        private readonly IUploadImage _uploadImage;

        public AuthServices(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IConfiguration configuration,
            IUploadVoice uploadVoice,
            IEmailService emailService,
            IUploadImage uploadImage,
            AppDbContext context)
        {
            _uploadImage = uploadImage;
            _context = context;
            _uploadVoice = uploadVoice;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<Response<string>> RegisterAsync(RegisterDto dto)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                string? voicePathToDelete = null;
                string? profilePicturePathToDelete = null;

                try
                {
                    var existingUser = await _userManager.FindByEmailAsync(dto.Email!);
                    if (existingUser is not null)
                        return BadRequest<string>("Email is already registered");

                    var voicePath = await _uploadVoice.SaveVoice(dto.VoiceVerification!);
                    if (voicePath.StartsWith("Error"))
                        return BadRequest<string>(voicePath);
                    voicePathToDelete = voicePath;
                    var profilePicturePath = await _uploadImage.SaveImageAsync(dto.ProfilePicture!);
                    if (profilePicturePath.StartsWith("Error"))
                    {
                        DeleteFile(voicePathToDelete);
                        return BadRequest<string>(profilePicturePath);
                    }
                    profilePicturePathToDelete = profilePicturePath;
                    var user = new ApplicationUser
                    {
                        UserName = dto.Email,
                        Email = dto.Email,
                        FirstName = dto.FirstName!,
                        LastName = dto.LastName!,
                        Age = dto.Age,
                        Status = UserStatus.Pending,
                        VoiceVerificationPath = voicePath,
                        EmailConfirmed = false,
                        CreateAt = DateTime.UtcNow,
                        ProfilePicturePath = profilePicturePath
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
                    var otpCode = await _userManager.GenerateTwoFactorTokenAsync(
                        user,
                        TokenOptions.DefaultEmailProvider
                    );
                    var baseUrl = _configuration["AppSettings:BaseUrl"];
                    var fullImagePath = $"{baseUrl}/System/388f7e03b835e6ca1f7c156816047a360bf18efe.png"; 
                    var emailBody = GetOtpHtmlTemplate(user.FirstName!, email: user.Email!, otpCode, fullImagePath);
                    await _emailService.SendEmailAsync(user.Email!, "Registration Received", emailBody);
                    await transaction.CommitAsync();

                    return Created("Registration successful! Check Your Email .");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    DeleteFile(voicePathToDelete);
                    DeleteFile(profilePicturePathToDelete);
                    return BadRequest<string>($"Error: {ex.Message} -- Internal: {ex.InnerException?.Message}");
                }
            });
        }

        public async Task<Response<AuthModel>> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email!);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password!))
            {
                return BadRequest<AuthModel>("Invalid Email or Password");
            }
            if (!user.EmailConfirmed)
            {
                return BadRequest<AuthModel>("Please confirm your email before logging in.");
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

        public async Task<Response<string>> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email!);
            if (user == null)
                return Success("If your email is registered, you will receive a reset link."); // بنرجع Success دايماً كـ Security Best Practice

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);



            return Success("If your email is registered, you will receive a password reset link shortly.");
        }

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
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName!),
                // profile Picture URL claim
                new Claim("profilePicture", string.IsNullOrEmpty(user.ProfilePicturePath) ? "" : $"{_configuration["AppSettings:BaseUrl"]}/{user.ProfilePicturePath.Replace("\\", "/")}")         };

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
        public async Task<Response<string>> UpdateFcmTokenAsync(Guid userId, string fcmToken)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return BadRequest<string>("User not found.");

            user.FcmToken = fcmToken;
            await _userManager.UpdateAsync(user);

            return Success("FCM Token updated successfully.");
        }
        private string GetOtpHtmlTemplate(string userName, string email, string otpCode, string baseUrl)
        {
            // استخدمنا $$""" لكي نتجاهل أقواس الـ CSS العادية { }
            // ونستخدم المتغيرات بين قوسين مزدوجين {{ }}
            return $$"""
    <!DOCTYPE html>
    <html lang="en">

    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>Email Verification</title>
        <style>
            body { margin: 0; padding: 0; background-color: #e0e0e0; display: flex; justify-content: center; align-items: center; min-height: 100vh; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }
            .container { width: 100%; max-width: 400px; text-align: center; box-shadow: 0 4px 10px rgba(0, 0, 0, 0.2); }
            .header { background-color: #4f5b49; padding: 30px 0; }
            .logo-container { width: 100px; height: 100px; margin: 0 auto; border: 2px solid white; border-radius: 8px; overflow: hidden; background-color: #c5d1ba; }
            .logo-container img { width: 100%; height: 100%; object-fit: cover; }
            .content { background-color: #a0b19d; padding: 25px 20px 40px; color: white; }
            .greeting { margin: 0 0 15px 0; font-size: 26px; font-weight: bold; }
            .email-box { background-color: white; color: #333; padding: 12px 20px; border-radius: 30px; display: inline-block; font-weight: bold; font-size: 18px; margin-bottom: 25px; width: 85%; box-sizing: border-box; }
            .instruction { margin: 0 0 25px 0; font-size: 16px; line-height: 1.4; font-weight: 600; }
            .code-box { background-color: white; color: black; font-size: 32px; font-weight: 900; letter-spacing: 8px; padding: 20px; border-radius: 15px; margin-bottom: 25px; display: inline-block; width: 85%; box-sizing: border-box; }
            .footer { margin: 0; font-size: 14px; font-weight: bold; }
        </style>
    </head>

    <body>
        <div class="container">
            <div class="header">
                <div class="logo-container">
                    <img src="{{baseUrl}}" alt="Cocorra">
                </div>
            </div>

            <div class="content">
                <h1 class="greeting">Hello {{userName}}</h1>

                <div class="email-box">
                    {{email}}
                </div>

                <p class="instruction">
                    The current code is for<br>
                    the verification process to complete<br>
                    your account registration.
                </p>

                <div class="code-box">
                    {{otpCode}}
                </div>

                <p class="footer">
                    This code is valid for 10 minutes.
                </p>
            </div>
        </div>
    </body>

    </html>
    """;
        }
    }
}