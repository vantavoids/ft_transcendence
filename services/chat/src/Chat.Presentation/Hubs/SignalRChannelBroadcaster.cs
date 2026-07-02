using Chat.Application.Abstractions;
using Chat.Application.Features.Channels.Common;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Hubs;

internal sealed class SignalRChannelBroadcaster(IHubContext<ChatHub, IChatClient> hub)
	: IChannelBroadcaster
{
	public Task BroadcastMessageAsync(long channelId, ChannelMessageResponse message, CancellationToken ct) =>
		hub.Clients.Group($"channel:{channelId}").ReceiveMessage(message);

	public Task BroadcastMessageEditedAsync(long channelId, ChannelMessageEditedEvent evt, CancellationToken ct) =>
		hub.Clients.Group($"channel:{channelId}").MessageEdited(evt);

	public Task BroadcastMessageDeletedAsync(long channelId, long messageId, CancellationToken ct) =>
		hub.Clients.Group($"channel:{channelId}").MessageDeleted(new ChannelMessageDeletedEvent(
			MessageId: messageId.ToString(),
			ChannelId: channelId.ToString()));
}
