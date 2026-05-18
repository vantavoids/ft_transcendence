using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Hubs;

[Authorize]
public sealed class ChatHub : Hub<IChatClient>
{
	public Task Echo(string message) =>
		Clients.Caller.ReceiveMessage(message);
}
