using Cocorra.BLL.Services.ChatService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

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
            var senderIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(senderIdString, out Guid senderId) ||
                !Guid.TryParse(receiverIdString, out Guid receiverId))
            {
                throw new HubException("Invalid User IDs.");
            }

            if (string.IsNullOrWhiteSpace(content))
                throw new HubException("Message cannot be empty.");

            // 1. نحفظ الرسالة في الداتابيز ونتأكد إنهم أصدقاء عن طريق السيرفيس
            var result = await _chatService.SaveMessageAsync(senderId, receiverId, content);

            if (!result.Succeeded)
            {
                throw new HubException(result.Message); // هيضرب إيرور لو مش أصدقاء
            }

            var messageDto = result.Data;

            // 2. نبعت الرسالة للطرف التاني (لو هو أونلاين SignalR هيوصلهاله فوراً)
            await Clients.User(receiverIdString).SendAsync("ReceiveMessage", messageDto);

            // 3. نرد على اللي بعت نقوله "تم الإرسال" عشان الفرونت إند يرسم علامة الصح ✔️
            await Clients.Caller.SendAsync("MessageSent", messageDto);
        }
    }
}