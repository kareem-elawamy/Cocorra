using System.ComponentModel.DataAnnotations;

namespace Cocorra.DAL.DTOS.SupportChatDto
{
    public class SendMessageDto
    {
        [Required(ErrorMessage = "Message content is required.")]
        [MaxLength(2000, ErrorMessage = "Message cannot exceed 2000 characters.")]
        public string Content { get; set; } = string.Empty;
    }
}
