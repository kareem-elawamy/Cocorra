using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Cocorra.DAL.DTOS.Auth
{
    public class SubmitMbtiDto
    {
        [Required]
        public string? MBTI { get; set; }
    }
}
