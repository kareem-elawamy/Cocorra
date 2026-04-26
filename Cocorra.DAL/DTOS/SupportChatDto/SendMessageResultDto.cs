namespace Cocorra.DAL.DTOS.SupportChatDto
{
    public class SendMessageResultDto
    {
        public SupportMessageDto Message { get; set; } = new();
        public bool IsNewChat { get; set; }
    }
}
