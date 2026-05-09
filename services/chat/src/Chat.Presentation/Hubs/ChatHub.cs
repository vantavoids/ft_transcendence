using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Hubs;

public sealed class ChatHub : Hub<IChatClient>
{
	public Task Echo(string message) =>
		Clients.Caller.ReceiveMessage(message);
}
