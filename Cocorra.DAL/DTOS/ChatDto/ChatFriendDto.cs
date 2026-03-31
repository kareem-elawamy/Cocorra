using System;

namespace Cocorra.DAL.DTOS.ChatDto
{
    public class ChatFriendDto
    {
        public Guid FriendId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string ProfilePicturePath { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime? LastMessageDate { get; set; }
        public int UnreadCount { get; set; }
    }
}