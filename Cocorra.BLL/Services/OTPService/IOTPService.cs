using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Base;

namespace Cocorra.BLL.Services.OTPService
{
    public interface IOTPService
    {
        Task<Response<string>> VerifyOtpAsync(string email, string otpCode);
        Task<Response<string>> ResendOtpAsync(string email);
    }
}