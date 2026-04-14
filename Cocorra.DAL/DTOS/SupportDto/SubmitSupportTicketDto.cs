using System.ComponentModel.DataAnnotations;
using Cocorra.DAL.Enums;

namespace Cocorra.DAL.DTOS.SupportDto
{
    public class SubmitSupportTicketDto
    {
        public SupportTicketType Type { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        public string? ContactEmail { get; set; }
    }
}
