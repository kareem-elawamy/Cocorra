namespace Cocorra.DAL.DTOS.SupportChatDto
{
    public class AdminReplyResultDto
    {
        public SupportMessageDto Message { get; set; } = new();
        public string UserId { get; set; } = string.Empty;
    }
}
