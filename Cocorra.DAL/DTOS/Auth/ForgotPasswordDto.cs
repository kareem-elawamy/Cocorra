using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Cocorra.DAL.DTOS.Auth
{
    public class ForgotPasswordDto
    {
        [Required, EmailAddress]
        public string? Email { get; set; }
    }
}
