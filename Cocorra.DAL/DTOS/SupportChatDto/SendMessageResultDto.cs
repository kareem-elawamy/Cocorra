namespace Cocorra.DAL.DTOS.SupportChatDto
{
    public class SendMessageResultDto
    {
        public Guid ChatId { get; set; }
        public SupportMessageDto Message { get; set; } = new();
        public bool IsNewChat { get; set; }
    }
}
