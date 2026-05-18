using Chat.Application.Abstractions;
using Chat.Application.Features.Messages.Common;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Hubs;

/// <summary>
/// fans out <see cref="MessageResponse"/> to every connection subscribed to a
/// channel group. lives in Presentation because <c>IHubContext</c> is a SignalR
/// abstraction and <c>Chat.Infrastructure</c> must stay free of ASP.NET deps
///
/// note: users are not yet auto-joined to channel groups on connect (issue
/// #66). until that lands, broadcasts are effectively a no-op in dev. the
/// call site is correct so it light up the moment group membership wires up
/// </summary>
internal sealed class SignalRChannelBroadcaster(IHubContext<ChatHub, IChatClient> hub)
	: IChannelBroadcaster
{
	public Task BroadcastMessageAsync(long channelId, MessageResponse message, CancellationToken ct) =>
		hub.Clients.Group($"channel:{channelId}").ReceiveMessage(message);
}
