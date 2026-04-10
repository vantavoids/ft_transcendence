using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Hubs;

public sealed class ChatHub : Hub
{
	public Task Echo(string message)
		=> Clients.Caller.SendAsync("ReceiveMessage", message);
}
