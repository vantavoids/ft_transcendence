using Chat.Application.Abstractions;
using Chat.Application.Features.Messages.Common;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Hubs;

internal sealed class SignalRChannelBroadcaster(IHubContext<ChatHub, IChatClient> hub)
	: IChannelBroadcaster
{
	public Task BroadcastMessageAsync(long channelId, MessageResponse message, CancellationToken ct) =>
		hub.Clients.Group($"channel:{channelId}").ReceiveMessage(message);

	public Task BroadcastMessageEditedAsync(long channelId, MessageEditedEvent evt, CancellationToken ct) =>
		hub.Clients.Group($"channel:{channelId}").MessageEdited(evt);

	public Task BroadcastMessageDeletedAsync(long channelId, long messageId, CancellationToken ct) =>
		hub.Clients.Group($"channel:{channelId}").MessageDeleted(new MessageDeletedEvent(
			MessageId: messageId.ToString(),
			ChannelId: channelId.ToString()));
}
