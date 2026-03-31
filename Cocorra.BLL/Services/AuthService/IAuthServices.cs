using Cocorra.BLL.DTOS.Auth;
using Cocorra.DAL.DTOS.Auth;
using Cocorra.BLL.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cocorra.BLL.Services.Auth
{
    public interface IAuthServices
    {
        Task<Response<object>> LoginAsync(LoginDto dto);
        Task<Response<string>> RegisterAsync(RegisterDto dto);
        Task<Response<string>> SubmitMbtiAsync(Guid userId, SubmitMbtiDto dto);
        Task<Response<string>> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<Response<string>> UpdateFcmTokenAsync(Guid userId, string fcmToken);
        Task<Response<string>> ResetPasswordAsync(ResetPasswordDto dto);
        Task<Response<string>> ReRecordVoiceAsync(string email, Microsoft.AspNetCore.Http.IFormFile voiceFile);
        Task<Response<string>> UpdatePasswordAsync(Guid userId, string currentPassword, string newPassword);
    }
}