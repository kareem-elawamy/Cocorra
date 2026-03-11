using Cocorra.BLL.DTOS.Auth;
using Cocorra.DAL.DTOS.Auth;
using Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cocorra.BLL.Services.Auth
{
    public interface IAuthServices
    {
        Task<Response<AuthModel>> LoginAsync(LoginDto dto);
        Task<Response<string>> RegisterAsync(RegisterDto dto);
        Task<Response<string>> SubmitMbtiAsync(Guid userId, SubmitMbtiDto dto);
        Task<Response<string>> ForgotPasswordAsync(ForgotPasswordDto dto);
    }
}