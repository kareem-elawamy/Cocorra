using Cocorra.BLL.Services.ChatService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Cocorra.API.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task SendMessage(string receiverIdString, string content)
        {
            try
            {
                var senderIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(senderIdString, out Guid senderId) ||
                    !Guid.TryParse(receiverIdString, out Guid receiverId))
                {
                    await Clients.Caller.SendAsync("SendMessageError", new { Error = "Invalid User IDs." });
                    return;
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    await Clients.Caller.SendAsync("SendMessageError", new { Error = "Message cannot be empty." });
                    return;
                }

                var result = await _chatService.SaveMessageAsync(senderId, receiverId, content);

                if (!result.Succeeded)
                {
                    await Clients.Caller.SendAsync("SendMessageError", new { Error = result.Message });
                    return;
                }

                var messageDto = result.Data;

                await Clients.User(receiverIdString).SendAsync("ReceiveMessage", messageDto);

                await Clients.Caller.SendAsync("MessageSent", messageDto);
            }
            catch (Exception)
            {
                await Clients.Caller.SendAsync("SendMessageError", new { Error = "An unexpected error occurred. Please try again." });
            }
        }
    }
}