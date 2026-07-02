using Chat.Application.Abstractions;
using Chat.Application.Features.DirectMessages.Common;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Hubs;

internal sealed class SignalRConversationUnicast(IHubContext<ChatHub, IChatClient> hub)
	: IConversationUnicast
{
	public Task UnicastMessageAsync(
		long recipientId,
		DirectMessageResponse message,
		CancellationToken ct)
	{
		return hub.Clients
			.User(recipientId.ToString())
			.ReceiveDirectMessage(message);
	}

	public Task UnicastMessageEditedAsync(long recipientId, DirectMessageEditedEvent evt, CancellationToken ct) =>
		hub.Clients.User(recipientId.ToString()).DirectMessageEdited(evt);

	public Task UnicastMessageDeletedAsync(long recipientId, DirectMessageDeletedEvent evt, CancellationToken ct) =>
		hub.Clients.User(recipientId.ToString()).DirectMessageDeleted(evt);
}
